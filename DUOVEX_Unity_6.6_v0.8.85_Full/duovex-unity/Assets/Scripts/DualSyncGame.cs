using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace DualSync
{
    /// <summary>
    /// Gameplay logic, timing and chart state. Visuals are uGUI sprites in DuovexVisual.cs.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed partial class DualSyncGame : MonoBehaviour
    {
        const float ScreenW = 1280f;
        const float ScreenH = 720f;
        // Lane geometry now follows the saved UI RectTransforms in the Unity scene.
        // Changing the first lane and hit line in the Inspector moves the note renderer,
        // and Editor geometry also updates mouse placement bounds.
        float UIX(string id,float fallback)
        {
            var r=visual!=null?visual.Get<RectTransform>(id):null;
            return r!=null?r.anchoredPosition.x:fallback;
        }
        float UIY(string id,float fallback)
        {
            var r=visual!=null?visual.Get<RectTransform>(id):null;
            return r!=null?-r.anchoredPosition.y:fallback;
        }
        float UIWidth(string id,float fallback)
        {
            var r=visual!=null?visual.Get<RectTransform>(id):null;
            return r!=null?r.sizeDelta.x:fallback;
        }
        // HitY is the *center* of the hit line, not its lower edge.
        // Use serialized RectTransforms as the only source of geometry.
        float LaneX => UIX("Game_Lane_0",DuovexUIFactory.GameGearX);
        float LaneW => UIWidth("Game_Lane_0",DuovexUIFactory.GameLaneWidth);
        float LaneTop => UIY("Game_Lane_0",DuovexUIFactory.GameGearTop);
        float HitY => UIY("Game_HitLine",DuovexUIFactory.GameHitCenterY-4f)
            +UIHeight("Game_HitLine",8f)*.5f;
        float EditorLaneX => UIX("Editor_Lane_0",319f);
        float EditorLaneW => UIWidth("Editor_Lane_0",178f);
        float EditorLaneTop => UIY("Editor_Lane_0",169f);
        float EditorHitY => UIY("Editor_HitLine",604f)+UIHeight("Editor_HitLine",8f)*.5f;
        float UIHeight(string id,float fallback)
        {
            var r=visual!=null?visual.Get<RectTransform>(id):null;
            return r!=null?r.sizeDelta.y:fallback;
        }
        float PhysicalLaneX(bool editing,int index)
        {
            string prefix=editing?"Editor":"Game";
            return UIX(prefix+"_Lane_"+index,(editing?319f:DuovexUIFactory.GameGearX)
                +index*(editing?178f:DuovexUIFactory.GameLaneWidth));
        }
        float PhysicalLaneW(bool editing,int index)
        {
            string prefix=editing?"Editor":"Game";
            return Mathf.Max(1f,UIWidth(prefix+"_Lane_"+index,
                editing?178f:DuovexUIFactory.GameLaneWidth));
        }
        const float ScrollPxPerSecond = 330f;
        const float EditorPanelX = 894f;
        const float EditorPanelW = 360f;
        const float DefaultEditorPxPerBeat = 58f;
        const float MinEditorPxPerBeat = 20f;
        const float MaxEditorPxPerBeat = 360f;
        const float MissWindow = 0.160f;
        const float SyncDelay = 0.15f;
        static readonly KeyCode[] HitKeys = { KeyCode.D, KeyCode.F, KeyCode.J, KeyCode.K };
        static readonly string[] PartNames = { "WHITE / D", "WHITE / F", "BLACK / J", "BLACK / K" };

        enum Tool { WhiteTap, BlackTap, DualTap, WhiteHold, BlackHold, DualHold, Select }
        enum Result { Perfect, Great, Good, Bad, Miss }
        enum Page { MainMenu, SongSelect, Gameplay, Editor, Results, Options, FileBrowser }
        enum InspectorTab { Audio, Timing, Edit }

        sealed class PlayNote
        {
            public ChartNote note;
            public bool headDone;
            public int nextTick = 1;
            public bool finished;
        }

        sealed class Burst
        {
            public int input;
            public float birth;
            public bool dual;
        }

        SongChart chart;
        SongChart editableChart;
        Page page = Page.MainMenu;
        bool tutorialActive;
        int tutorialLevel = 1;
        bool editorMode => page == Page.Editor;
        AudioClip CurrentSong => tutorialActive ? null : song;
        readonly List<PlayNote> playNotes = new List<PlayNote>();
        readonly List<Burst> bursts = new List<Burst>();
        readonly List<string> undo = new List<string>();
        readonly List<string> redo = new List<string>();
        readonly HashSet<int> selectedIds = new HashSet<int>();
        readonly List<ChartNote> clipboardNotes = new List<ChartNote>();
        float editorPxPerBeat = DefaultEditorPxPerBeat;
        float noteSpeed = 1f;
        InspectorTab inspectorTab = InspectorTab.Audio;
        bool confirmClear;
        bool loopEnabled;
        float loopStartBeat = 0f;
        float loopEndBeat = 8f;
        float statusVisibleUntil;
        AudioSource audioSource;
        AudioSource hitSoundSource;
        AudioClip song;
        float hitSoundVolume = .32f; // Persistent player-adjustable SFX level.
        float musicVolume = .80f; // Independently controls all song audio sources.
        bool playing;
        bool audioPausedForResume;
        bool resumeCountdownActive;
        float resumeCountdownEnd;
        const float ResumeCountdownSeconds = 3f;
        // A provisional pass rule until the dedicated life-gauge system is added.
        const float ClearAccuracyThreshold = .70f;
        float songCursor;
        double clockStartDsp;
        float editorBaseBeat;
        int snapDivision = 4;
        Tool currentTool = Tool.WhiteTap;
        bool holdDrag;
        int dragLane;
        float dragBeat;
        float[] waveform;
        bool waveReady;
        string status = "";
        string lastResult = "";
        string lastTiming = ""; // FAST above / SLOW below; hidden for PERFECT.
        Color lastResultColor = Color.white;
        float lastResultTime;
        readonly int[] resultCounts = new int[5];
        int fastCount;
        int slowCount;
        AudioClip whiteHitSound;
        AudioClip blackHitSound;
        bool generatedWhiteSound;
        bool generatedBlackSound;
        int combo;
        int maxCombo;
        int totalJudged;
        float earnedPoints;
        int missCount;
        bool[] held = new bool[4];
        float scale = 1f;
        Vector2 origin;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindAnyObjectByType<DualSyncGame>() != null) return;

            // The prototype uses a single scene. Keep runtime objects in that scene:
            // DontDestroyOnLoad unnecessarily moves them to Unity's internal scene,
            // which can trigger a SetActiveScene exception in Unity 6.6.
            Camera gameCamera = Camera.main;
            if (gameCamera == null)
            {
                var cameraObject = new GameObject("DUOVEX Camera");
                cameraObject.tag = "MainCamera";
                gameCamera = cameraObject.AddComponent<Camera>();
                gameCamera.orthographic = true;
                gameCamera.clearFlags = CameraClearFlags.SolidColor;
                gameCamera.backgroundColor = new Color(.035f, .052f, .10f);
            }

            // Unity needs exactly one active AudioListener for audio output.
            if (FindAnyObjectByType<AudioListener>() == null)
                gameCamera.gameObject.AddComponent<AudioListener>();

            var root = new GameObject("DUOVEX - Runtime");
            root.AddComponent<DualSyncGame>();
        }

        void Awake()
        {
            Application.targetFrameRate = 144;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;
            hitSoundSource = gameObject.AddComponent<AudioSource>();
            hitSoundSource.playOnAwake = false;
            hitSoundSource.loop = false;
            hitSoundVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("DUOVEX.HitSoundVolume", .32f));
            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("DUOVEX.MusicVolume", .80f));
            noteSpeed = Mathf.Clamp(PlayerPrefs.GetFloat("DUOVEX.NoteSpeed", 1f), .5f, 3f);
            hitSoundSource.volume = hitSoundVolume;
            audioSource.volume = musicVolume;
            song = Resources.Load<AudioClip>("song");
            audioSource.clip = song;
            try { editableChart = ChartStorage.Load(); }
            catch (Exception ex) { status = "Chart load error: " + ex.Message; }
            if (editableChart == null) editableChart = ChartStorage.Sample();
            chart = editableChart;
            snapDivision = Mathf.Clamp(chart.tickDivision, 1, 32);
            CreateHitSounds();
            BuildStates();
            if (song != null) BuildWaveform();
            RefreshSongLibrary();
        }

        void OnDestroy()
        {
            if (importedAudioClip != null) Destroy(importedAudioClip);
            if (generatedWhiteSound && whiteHitSound != null) Destroy(whiteHitSound);
            if (generatedBlackSound && blackHitSound != null) Destroy(blackHitSound);
            PlayerPrefs.SetFloat("DUOVEX.HitSoundVolume", hitSoundVolume);
            PlayerPrefs.SetFloat("DUOVEX.MusicVolume", musicVolume);
            PlayerPrefs.SetFloat("DUOVEX.NoteSpeed", noteSpeed);
            PlayerPrefs.Save();
        }

        static float ClampBpm(float bpm) => Mathf.Clamp(bpm, 30f, 350f);
        float SongTime => playing ? (float)(AudioSettings.dspTime - clockStartDsp) : songCursor;

        float ChartEndTime()
        {
            float lastBeat = 4f;
            foreach (var n in chart.notes)
                lastBeat = Mathf.Max(lastBeat, n.beat + n.durationBeats);
            return Mathf.Max(1f, chart.BeatToSeconds(lastBeat + 3f));
        }

        void BeginPlaying(float from)
        {
            AudioClip clip = CurrentSong;
            if (activeImported != null && clip == null) { Notify("AUDIO NOT LOADED"); return; }
            if (clip != null && from >= clip.length - 0.1f) from = 0f;
            if (clip == null && from >= ChartEndTime()) from = 0f;
            songCursor = Mathf.Max(0f, from);
            if (songCursor < 0.01f) BuildStates();
            clockStartDsp = AudioSettings.dspTime + SyncDelay - songCursor;
            audioPausedForResume = false;
            audioSource.Stop();
            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.time = Mathf.Clamp(songCursor, 0, Mathf.Max(0, clip.length - 0.15f));
                audioSource.PlayScheduled(AudioSettings.dspTime + SyncDelay);
            }
            playing = true;
            status = tutorialActive ? "TUTORIAL  /  NO MUSIC" :
                (clip != null ? "DSP-SYNC PLAYBACK" : "SILENT CHART - add Assets/Resources/song.ogg");
        }

        void PausePlaying()
        {
            resumeCountdownActive=false;
            if (!playing) return;
            songCursor = Mathf.Max(0f, SongTime);
            playing = false;
            // Keep the exact decoded sample position when pausing a running
            // music track. A Stop + MP3 seek may jump to a compression frame.
            audioPausedForResume = page == Page.Gameplay && CurrentSong != null &&
                AudioSettings.dspTime >= clockStartDsp &&
                audioSource != null && audioSource.isPlaying;
            if (audioSource != null)
            {
                if (audioPausedForResume) audioSource.Pause();
                else audioSource.Stop();
            }
            if (hitSoundSource != null) hitSoundSource.Stop();
            status = "PAUSED";
        }

        // Hold both audio and the gameplay clock during the resume countdown.
        // Existing note judgement flags, combo and song position are unchanged.
        void ToggleGameplayPause()
        {
            if(page!=Page.Gameplay)return;
            if(resumeCountdownActive)
            {
                resumeCountdownActive=false; // ESC cancels countdown, stays paused
                return;
            }
            if(playing) { PausePlaying(); return; }
            resumeCountdownActive=true;
            resumeCountdownEnd=Time.unscaledTime+ResumeCountdownSeconds;
            status="RESUMING";
        }
        void UpdateResumeCountdown()
        {
            if(!resumeCountdownActive || page!=Page.Gameplay)return;
            if(Time.unscaledTime < resumeCountdownEnd)return;
            resumeCountdownActive=false;
            if(audioPausedForResume && audioSource!=null && audioSource.clip==CurrentSong)
            {
                clockStartDsp=AudioSettings.dspTime-songCursor;
                audioSource.UnPause();
                playing=true;
                audioPausedForResume=false;
            }
            else BeginPlaying(songCursor);
        }

        void Restart(bool start)
        {
            PausePlaying();
            songCursor = 0f;
            BuildStates();
            if (start) BeginPlaying(0f);
        }

        void BuildStates()
        {
            playNotes.Clear();
            foreach (var note in chart.notes) playNotes.Add(new PlayNote { note = note });
            combo = 0; maxCombo = 0; totalJudged = 0; earnedPoints = 0; missCount = 0;
            bursts.Clear();
            lastResult = "";
            lastTiming = "";
            fastCount = 0;
            slowCount = 0;
            Array.Clear(resultCounts, 0, resultCounts.Length);
        }

        void OpenMainMenu()
        {
            PausePlaying();
            if (page == Page.Editor) editableChart = chart;
            page = Page.MainMenu;
            tutorialActive = false;
            status = "";
        }

        void OpenOptions()
        {
            PausePlaying();
            page = Page.Options;
            status = "OPTIONS  /  AUDIO";
        }

        void SetHitSoundVolume(float value)
        {
            float newValue = Mathf.Clamp01(value);
            if (Mathf.Abs(newValue - hitSoundVolume) < .0001f) return;
            hitSoundVolume = newValue;
            if (hitSoundSource != null) hitSoundSource.volume = hitSoundVolume;
            PlayerPrefs.SetFloat("DUOVEX.HitSoundVolume", hitSoundVolume);
        }

        void SetMusicVolume(float value)
        {
            float newValue = Mathf.Clamp01(value);
            if (Mathf.Abs(newValue - musicVolume) < .0001f) return;
            musicVolume = newValue;
            if (audioSource != null) audioSource.volume = musicVolume;
            PlayerPrefs.SetFloat("DUOVEX.MusicVolume", musicVolume);
        }

        void AdjustNoteSpeed(float delta)
        {
            float next = Mathf.Round(Mathf.Clamp(noteSpeed + delta, .5f, 3f) * 10f) / 10f;
            if (Mathf.Abs(noteSpeed - next) < .001f) return;
            noteSpeed = next;
            PlayerPrefs.SetFloat("DUOVEX.NoteSpeed", noteSpeed);
        }

        void PreviewHitSound(bool white)
        {
            AudioClip clip = white ? whiteHitSound : blackHitSound;
            if (hitSoundSource != null && clip != null && hitSoundVolume > 0f)
                hitSoundSource.PlayOneShot(clip);
        }

        void OpenSongSelect()
        {
            PausePlaying();
            if (page == Page.Editor) editableChart = chart;
            page = Page.SongSelect;
            tutorialActive = false;
            RefreshSongLibrary();
            songSelectForEditing = false;
            status = "";
        }

        void EnterEditor()
        {
            PausePlaying();
            tutorialActive = false;
            chart = editableChart ?? ChartStorage.Sample();
            page = Page.Editor;
            songCursor = 0f;
            editorBaseBeat = 0f;
            editorPxPerBeat = DefaultEditorPxPerBeat;
            selectedIds.Clear();
            inspectorTab = InspectorTab.Audio;
            confirmClear = false;
            loopEnabled = false;
            snapDivision = Mathf.Clamp(chart.tickDivision, 1, 32);
            BuildStates();
            status = "CHART STUDIO  /  ESC FOR MAIN MENU";
        }

        void StartTutorial(int level = 1)
        {
            PausePlaying();
            SongChart selectedChart = ChartStorage.PracticeLevel(level);
            if (selectedChart == null)
            {
                Notify("LEVEL " + level + " NOT FOUND");
                return;
            }
            tutorialLevel = level;
            tutorialActive = true;
            activeImported = null;
            chart = selectedChart;
            page = Page.Gameplay;
            songCursor = 0f;
            Restart(true);
        }

        void RetryTrack()
        {
            if (tutorialActive) StartTutorial(tutorialLevel);
            else { page = Page.Gameplay; Restart(true); }
        }

        float Accuracy01 => totalJudged>0 ? Mathf.Clamp01(earnedPoints/totalJudged) : 0f;
        int NormalizedScore => Mathf.Clamp(Mathf.RoundToInt(Accuracy01*1000000f),0,1000000);
        bool TrackCleared => totalJudged>0 && Accuracy01>=ClearAccuracyThreshold;
        string ResultRank
        {
            get
            {
                float a=Accuracy01;
                if(a>=.99f)return "SS";
                if(a>=.95f)return "S";
                if(a>=.90f)return "A";
                if(a>=.80f)return "B";
                if(a>=ClearAccuracyThreshold)return "C";
                return "F";
            }
        }

        void FinishTrack()
        {
            PausePlaying();
            if (page == Page.Gameplay)
            {
                page = Page.Results;
                status = TrackCleared ? "CLEAR" : "FAIL";
            }
            else status = "TRACK COMPLETE  /  PRESS REWIND";
        }

        void Update()
        {
            scale = Mathf.Min(Screen.width / ScreenW, Screen.height / ScreenH);
            origin = new Vector2((Screen.width - ScreenW * scale) * .5f, (Screen.height - ScreenH * scale) * .5f);
            UpdateResumeCountdown();
            // Scroll speed changes only how far notes travel on screen. Audio and
            // judgement timestamps stay untouched, including during live gameplay.
            if (page == Page.Gameplay)
            {
                if (KeyDown(KeyCode.F3)) AdjustNoteSpeed(-.1f);
                if (KeyDown(KeyCode.F4)) AdjustNoteSpeed(.1f);
            }
            if (KeyDown(KeyCode.Escape))
            {
                if (page == Page.FileBrowser) { page = Page.SongSelect; return; }
                if (page == Page.Gameplay) ToggleGameplayPause();
                else if (page == Page.Results) OpenSongSelect();
                else if (page == Page.Editor || page == Page.SongSelect || page == Page.Options) OpenMainMenu();
                return;
            }
            if (page == Page.MainMenu || page == Page.SongSelect || page == Page.Results || page == Page.Options || page == Page.FileBrowser)
            {
                if (page == Page.SongSelect)
                {
                    // Arrow-key navigation complements the clickable song cards.
                    // Always scroll the visible seven rows with the selection.
                    int max=Mathf.Max(0,ImportedStartIndex+importedSongs.Count-1);
                    if(KeyDown(KeyCode.DownArrow))
                        songSelection=Mathf.Min(max,songSelection+1);
                    if(KeyDown(KeyCode.UpArrow))
                        songSelection=Mathf.Max(0,songSelection-1);
                    if(songSelection<libraryPage)libraryPage=songSelection;
                    if(songSelection>=libraryPage+7)libraryPage=songSelection-6;
                    // TAB toggles song duration independently of chart difficulty.
                    if(KeyDown(KeyCode.Tab)) MoveSongLength();
                    if(KeyDown(KeyCode.LeftArrow) || KeyDown(KeyCode.A)) MoveDifficulty(-1);
                    if(KeyDown(KeyCode.RightArrow) || KeyDown(KeyCode.D)) MoveDifficulty(1);
                    if(KeyDown(KeyCode.Return))SelectLibraryAction(songSelectForEditing);
                }
                if (page == Page.Results && KeyDown(KeyCode.Return)) RetryTrack();
                return;
            }
            if (page == Page.Editor && KeyDown(KeyCode.F1))
            {
                OpenMainMenu();
                return;
            }
            if (KeyDown(KeyCode.Space))
            {
                if (page == Page.Gameplay) ToggleGameplayPause();
                else if (playing) PausePlaying();
                else BeginPlaying(songCursor);
            }
            if (page == Page.Gameplay && KeyDown(KeyCode.Return)) Restart(true);

            if (editorMode)
            {
                HandleEditorInput();
                if (playing && loopEnabled && loopEndBeat > loopStartBeat &&
                    SongTime >= chart.BeatToSeconds(loopEndBeat))
                    SeekEditorBeat(loopStartBeat);
                if (playing) editorBaseBeat = Mathf.Max(0f, chart.SecondsToBeat(SongTime) - 1.4f);
            }
            for (int i = 0; i < held.Length; i++) held[i] = KeyHeld(HitKeys[i]);
            if (playing && page == Page.Gameplay)
            {
                for (int i = 0; i < 4; i++) if (KeyDown(HitKeys[i])) Hit(i, SongTime);
                ResolveMissesAndTicks(SongTime);
            }
            for (int i = bursts.Count - 1; i >= 0; i--)
                if (Time.unscaledTime - bursts[i].birth > .27f) bursts.RemoveAt(i);

            if (playing && CurrentSong != null && SongTime >= CurrentSong.length)
                FinishTrack();
            else if (playing && CurrentSong == null && SongTime >= ChartEndTime())
                FinishTrack();
        }

        static Result Judge(float delta)
        {
            float ms = Mathf.Abs(delta) * 1000f;
            if (ms <= 50f) return Result.Perfect;
            if (ms <= 80f) return Result.Great;
            if (ms <= 120f) return Result.Good;
            if (ms <= 160f) return Result.Bad;
            return Result.Miss;
        }

        // Distinct hues for visual recognition. WHITE/BLACK note hues remain unchanged.
        static Color JudgmentColor(Result r)
        {
            switch (r)
            {
                case Result.Perfect: return new Color(.43f, .95f, 1f); // cyan
                case Result.Great: return new Color(.61f, 1f, .67f);   // mint green
                case Result.Good: return new Color(1f, .84f, .43f);   // gold
                case Result.Bad: return new Color(1f, .57f, .34f);    // orange
                default: return new Color(1f, .39f, .51f);           // pink/red
            }
        }

        static float Weight(Result r)
        {
            switch (r)
            {
                case Result.Perfect: return 1f;
                case Result.Great: return .8f;
                case Result.Good: return .5f;
                case Result.Bad: return .2f;
                default: return 0f;
            }
        }

        void Register(Result r, float delta, int input, bool tick)
        {
            totalJudged++;
            earnedPoints += Weight(r);
            resultCounts[(int)r]++;
            if (r == Result.Miss)
            {
                combo = 0;
                missCount++;
            }
            else
            {
                combo++;
                maxCombo = Mathf.Max(combo, maxCombo);
                bursts.Add(new Burst { input = input, birth = Time.unscaledTime });
                // Ticks use a gentle lane flash; sound only on actual key presses.
                if (!tick && hitSoundSource != null && hitSoundVolume > 0f)
                {
                    AudioClip keySound = IsWhite(input) ? whiteHitSound : blackHitSound;
                    if (keySound != null) hitSoundSource.PlayOneShot(keySound);
                }
            }

            // A sustained hold may score 8 ticks per second. Don't let those
            // replace the large, human-timed TAP / HEAD judgement constantly.
            if (tick && r != Result.Miss) return;
            lastResult = r.ToString().ToUpperInvariant();
            lastResultColor = JudgmentColor(r);
            lastTiming = "";
            if (!tick && r != Result.Perfect && r != Result.Miss)
            {
                if (delta < -0.0005f) { lastTiming = "FAST"; fastCount++; }
                else if (delta > 0.0005f) { lastTiming = "SLOW"; slowCount++; }
            }
            lastResultTime = Time.unscaledTime;
        }

        void Hit(int input, float time)
        {
            PlayNote nearest = null;
            float best = MissWindow + .0001f;
            foreach (var p in playNotes)
            {
                if (p.note.input != input || p.headDone) continue;
                float abs = Mathf.Abs(time - chart.BeatToSeconds(p.note.beat));
                if (abs <= MissWindow && abs < best) { nearest = p; best = abs; }
            }
            if (nearest == null) return;
            nearest.headDone = true;
            float delta = time - chart.BeatToSeconds(nearest.note.beat);
            Register(Judge(delta), delta, input, false);
            if (nearest.note.durationBeats <= 0) nearest.finished = true;
        }

        void ResolveMissesAndTicks(float time)
        {
            foreach (var p in playNotes)
            {
                if (!p.headDone && time > chart.BeatToSeconds(p.note.beat) + MissWindow)
                {
                    p.headDone = true;
                    Register(Result.Miss, 0, p.note.input, false);
                    if (p.note.durationBeats <= 0) p.finished = true;
                }
                if (p.note.durationBeats <= 0) continue;
                int count = Mathf.CeilToInt(p.note.durationBeats * Mathf.Max(1, chart.tickDivision) - .00001f);
                while (p.nextTick <= count)
                {
                    float beat = Mathf.Min(p.note.beat + p.nextTick / (float)Mathf.Max(1, chart.tickDivision), p.note.beat + p.note.durationBeats);
                    if (time < chart.BeatToSeconds(beat)) break;
                    bool hit = held[p.note.input] && p.headDone;
                    Register(hit ? Result.Perfect : Result.Miss, 0, p.note.input, true);
                    p.nextTick++;
                }
                if (p.nextTick > count && time >= chart.BeatToSeconds(p.note.beat + p.note.durationBeats)) p.finished = true;
            }
        }

        float BeatAtY(float y) => editorBaseBeat + (EditorHitY - y) / editorPxPerBeat;
        float YAtBeat(float beat) => EditorHitY - (beat - editorBaseBeat) * editorPxPerBeat;

        void SeekEditorBeat(float beat)
        {
            float target = Mathf.Max(0f, chart.BeatToSeconds(Mathf.Max(0, beat)));
            if (playing) { PausePlaying(); BeginPlaying(target); }
            else songCursor = target;
            editorBaseBeat = Mathf.Max(0, beat - 1f);
        }

        void ChangeEditorZoom(float factor, float anchorY)
        {
            float anchorBeat = BeatAtY(anchorY);
            float old = editorPxPerBeat;
            editorPxPerBeat = Mathf.Clamp(old * factor, MinEditorPxPerBeat, MaxEditorPxPerBeat);
            editorBaseBeat = Mathf.Max(0, anchorBeat - (EditorHitY - anchorY) / editorPxPerBeat);
        }

        ChartNote NoteNear(Vector2 pointer)
        {
            ChartNote nearest = null;
            float best = 22f;
            foreach (ChartNote note in chart.notes)
            {
                float x = EditorLaneX + PartLane(note.input) * EditorLaneW + 6f;
                if (note.input == 2 && pointer.x < EditorLaneX + 1.5f * EditorLaneW) continue;
                if (note.input == 1 && pointer.x >= EditorLaneX + 1.5f * EditorLaneW) continue;
                if (pointer.x < x - 4f || pointer.x > x + EditorLaneW - 8f) continue;
                float headY = YAtBeat(note.beat);
                float tailY = YAtBeat(note.beat + note.durationBeats);
                float delta = note.durationBeats > 0 && pointer.y >= tailY && pointer.y <= headY ? 0f :
                    Mathf.Min(Mathf.Abs(pointer.y - headY), Mathf.Abs(pointer.y - tailY));
                if (delta < best) { best = delta; nearest = note; }
            }
            return nearest;
        }

        void HandleEditorInput()
        {
            bool ctrl = KeyHeld(KeyCode.LeftControl) || KeyHeld(KeyCode.RightControl) ||
                        KeyHeld(KeyCode.LeftCommand) || KeyHeld(KeyCode.RightCommand);
            bool shift = KeyHeld(KeyCode.LeftShift) || KeyHeld(KeyCode.RightShift);
            if (ctrl && KeyDown(KeyCode.S)) SaveChart();
            if (ctrl && KeyDown(KeyCode.O)) LoadChart();
            if (ctrl && KeyDown(KeyCode.Z)) { if (shift) RedoEdit(); else UndoEdit(); }
            if (ctrl && KeyDown(KeyCode.Y)) RedoEdit();
            if (ctrl && KeyDown(KeyCode.A)) SelectAllNotes();
            if (ctrl && KeyDown(KeyCode.C)) CopySelection();
            if (ctrl && KeyDown(KeyCode.V)) PasteSelection();
            if (KeyDown(KeyCode.Delete) || KeyDown(KeyCode.Backspace)) DeleteSelection();
            if (KeyDown(KeyCode.Alpha1)) currentTool = Tool.Select;
            if (KeyDown(KeyCode.Alpha2)) currentTool = Tool.WhiteTap;
            if (KeyDown(KeyCode.Alpha3)) currentTool = Tool.BlackTap;
            if (KeyDown(KeyCode.Alpha4)) currentTool = Tool.DualTap;
            if (KeyDown(KeyCode.Alpha5)) currentTool = Tool.WhiteHold;
            if (KeyDown(KeyCode.Alpha6)) currentTool = Tool.BlackHold;
            if (KeyDown(KeyCode.Alpha7)) currentTool = Tool.DualHold;

            // Keyboard recording during playback is retained from v0.4.
            if (playing)
            {
                for (int i = 0; i < 4; i++)
                    if (KeyDown(HitKeys[i])) AddNotes(Snap(chart.SecondsToBeat(SongTime)), 0f, new[] { i });
            }

            Vector2 pointer = VirtualMouse();
            bool overTrack = pointer.x >= EditorLaneX && pointer.x < EditorLaneX + 3 * EditorLaneW &&
                pointer.y >= EditorLaneTop && pointer.y <= EditorHitY;
            float scroll = MouseScroll();
            if (Mathf.Abs(scroll) > .01f && pointer.x > EditorLaneX - 38f &&
                pointer.x < EditorLaneX + 3 * EditorLaneW + 25f)
            {
                float notches = Mathf.Abs(scroll) >= 10f ? scroll / 120f : scroll;
                if (ctrl)
                    ChangeEditorZoom(Mathf.Pow(1.18f, notches), Mathf.Clamp(pointer.y, EditorLaneTop, EditorHitY));
                else editorBaseBeat = Mathf.Max(0f, editorBaseBeat + Mathf.Sign(notches) * .6f);
            }

            // Audio waveform scrub, only while the Audio inspector is visible.
            if (inspectorTab == InspectorTab.Audio && MouseDown(0) &&
                pointer.x >= 912f && pointer.x <= 1234f && pointer.y >= 310f && pointer.y <= 542f)
            {
                float visibleBeats = (EditorHitY - EditorLaneTop) / editorPxPerBeat;
                float fraction = Mathf.Clamp01((540f - pointer.y) / 228f);
                SeekEditorBeat(editorBaseBeat + fraction * visibleBeats);
                return;
            }

            if (playing) return;
            float beat = Snap(Mathf.Max(0, BeatAtY(pointer.y)));
            int lane = (int)((pointer.x - EditorLaneX) / EditorLaneW);
            if (holdDrag && MouseUp(0))
            {
                holdDrag = false;
                if (TryToolParts(dragLane, out int[] releaseParts))
                {
                    float duration = Mathf.Max(1f / snapDivision, beat - dragBeat);
                    AddNotes(dragBeat, duration, releaseParts);
                }
                return;
            }
            if (!overTrack) return;
            if (MouseDown(1)) { RemoveClosest(lane, beat); return; }
            if (MouseDown(0))
            {
                if (currentTool == Tool.Select)
                {
                    ChartNote found = NoteNear(pointer);
                    if (!shift) selectedIds.Clear();
                    if (found != null)
                    {
                        if (shift && selectedIds.Contains(found.id)) selectedIds.Remove(found.id);
                        else selectedIds.Add(found.id);
                    }
                    return;
                }
                if (!TryToolParts(lane, out int[] parts)) return;
                if (IsHoldTool())
                {
                    holdDrag = true;
                    dragLane = lane;
                    dragBeat = beat;
                }
                else AddNotes(beat, 0f, parts);
            }
        }

        void SelectAllNotes()
        {
            selectedIds.Clear();
            foreach (var n in chart.notes) selectedIds.Add(n.id);
        }

        void CopySelection()
        {
            clipboardNotes.Clear();
            foreach (var n in chart.notes)
                if (selectedIds.Contains(n.id)) clipboardNotes.Add(n.Clone());
        }

        void PasteSelection()
        {
            if (clipboardNotes.Count == 0) return;
            float first = float.MaxValue;
            foreach (var n in clipboardNotes) first = Mathf.Min(first, n.beat);
            Vector2 mouse = VirtualMouse();
            bool inTrack = mouse.x >= EditorLaneX && mouse.x <= EditorLaneX + 3 * EditorLaneW &&
                mouse.y >= EditorLaneTop && mouse.y <= EditorHitY;
            float destination = Snap(Mathf.Max(0, inTrack ? BeatAtY(mouse.y) : editorBaseBeat));
            var proposed = new List<ChartNote>();
            foreach (var original in clipboardNotes)
            {
                ChartNote n = original.Clone();
                n.beat = Mathf.Max(0, destination + original.beat - first);
                if (Conflicts(n.input, n.beat, n.durationBeats)) { Notify("OVERLAP"); return; }
                foreach (var other in proposed)
                    if (other.input == n.input && n.beat <= other.beat + other.durationBeats + .0001f &&
                        n.beat + n.durationBeats >= other.beat - .0001f) return;
                proposed.Add(n);
            }
            Snapshot();
            selectedIds.Clear();
            int nextId = chart.NewId();
            foreach (var n in proposed)
            {
                n.id = nextId++;
                chart.notes.Add(n);
                selectedIds.Add(n.id);
            }
            chart.Sort();
        }

        void DeleteSelection()
        {
            if (selectedIds.Count == 0) return;
            Snapshot();
            chart.notes.RemoveAll(n => selectedIds.Contains(n.id));
            selectedIds.Clear();
        }

        float Snap(float beat) => Mathf.Round(beat * snapDivision) / snapDivision;
        bool IsHoldTool() => currentTool == Tool.WhiteHold || currentTool == Tool.BlackHold || currentTool == Tool.DualHold;
        bool TryToolParts(int lane, out int[] parts)
        {
            parts = null;
            bool white = currentTool == Tool.WhiteTap || currentTool == Tool.WhiteHold;
            bool black = currentTool == Tool.BlackTap || currentTool == Tool.BlackHold;
            if (white && lane <= 1) { parts = new[] { lane }; return true; }
            if (black && lane >= 1) { parts = new[] { lane == 1 ? 2 : 3 }; return true; }
            if (!white && !black && lane == 1) { parts = new[] { 1, 2 }; return true; }
            Notify("INVALID LANE");
            return false;
        }

        void Snapshot()
        {
            undo.Add(JsonUtility.ToJson(chart));
            if (undo.Count > 60) undo.RemoveAt(0);
            redo.Clear();
        }

        bool Conflicts(int input, float beat, float duration)
        {
            float end = beat + duration;
            foreach (var n in chart.notes)
            {
                if (n.input != input) continue;
                float oldEnd = n.beat + n.durationBeats;
                // Avoid same-key notes during an active hold; distinct keys are independent.
                if (beat <= oldEnd + .0001f && end >= n.beat - .0001f) return true;
            }
            return false;
        }

        void AddNotes(float beat, float duration, int[] parts)
        {
            foreach (int part in parts)
                if (Conflicts(part, beat, duration)) { Notify("OVERLAP"); return; }
            Snapshot();
            int nextId = chart.NewId();
            foreach (int part in parts) chart.notes.Add(new ChartNote
            {
                id = nextId++, input = part, beat = beat, durationBeats = duration
            });
            chart.Sort();
            status = parts.Length == 2 ? "DUAL NOTE ADDED" : "NOTE ADDED";
        }

        void RemoveClosest(int lane, float beat)
        {
            ChartNote candidate = null;
            float nearest = .28f;
            foreach (var n in chart.notes)
            {
                if (PartLane(n.input) != lane) continue;
                float d = Mathf.Abs(n.beat - beat);
                if (d < nearest) { nearest = d; candidate = n; }
            }
            if (candidate == null) return;
            Snapshot();
            chart.notes.Remove(candidate);
            // Right-click on one side of a dual removes just that logical key.
            status = "NOTE REMOVED";
        }

        void UndoEdit()
        {
            if (undo.Count == 0) return;
            redo.Add(JsonUtility.ToJson(chart));
            chart = JsonUtility.FromJson<SongChart>(undo[undo.Count - 1]);
            undo.RemoveAt(undo.Count - 1);
            if (chart.notes == null) chart.notes = new List<ChartNote>();
            selectedIds.Clear();
            BuildStates();
        }

        void RedoEdit()
        {
            if (redo.Count == 0) return;
            undo.Add(JsonUtility.ToJson(chart));
            chart = JsonUtility.FromJson<SongChart>(redo[redo.Count - 1]);
            redo.RemoveAt(redo.Count - 1);
            if (chart.notes == null) chart.notes = new List<ChartNote>();
            selectedIds.Clear();
            BuildStates();
        }

        void Notify(string message)
        {
            status = message;
            statusVisibleUntil = Time.unscaledTime + 3.5f;
        }

        void SaveChart()
        {
            try { if (activeImported != null) DuovexSongLibrary.SaveChart(activeImported, chart);
                else ChartStorage.Save(chart); editableChart = chart; Notify("SAVED"); }
            catch (Exception ex) { Notify("SAVE FAILED: " + ex.Message); }
        }

        void LoadChart()
        {
            try
            {
                SongChart loaded = activeImported != null ? DuovexSongLibrary.LoadChart(activeImported) : ChartStorage.Load();
                if (loaded == null) { Notify("NO SAVED CHART"); return; }
                PausePlaying(); chart = loaded; editableChart = chart; snapDivision = chart.tickDivision;
                songCursor = 0; BuildStates(); undo.Clear(); redo.Clear(); selectedIds.Clear(); Notify("LOADED");
            }
            catch (Exception ex) { Notify("LOAD FAILED: " + ex.Message); }
        }

        static int PartLane(int input) => input == 0 ? 0 : (input == 3 ? 2 : 1);
        static bool IsWhite(int input) => input <= 1;

        void CreateHitSounds()
        {
            whiteHitSound = Resources.Load<AudioClip>("SFX/HitWhite");
            blackHitSound = Resources.Load<AudioClip>("SFX/HitBlack");
            if (whiteHitSound == null)
            {
                whiteHitSound = MakeFallbackImpact("DUOVEX White Fallback", 193f, 11);
                generatedWhiteSound = true;
            }
            if (blackHitSound == null)
            {
                blackHitSound = MakeFallbackImpact("DUOVEX Black Fallback", 167f, 29);
                generatedBlackSound = true;
            }
        }

        static AudioClip MakeFallbackImpact(string name, float baseHz, int seed)
        {
            const int sampleRate = 44100;
            int count = Mathf.CeilToInt(sampleRate * .080f);
            float[] pcm = new float[count];
            uint random = (uint)(seed + 12345);
            float previous = 0f;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)sampleRate;
                random = random * 1664525u + 1013904223u;
                float noise = (random / 4294967295f) * 2f - 1f;
                float highpassed = noise - previous;
                previous += .20f * (noise - previous);
                float transient = highpassed * Mathf.Exp(-t * 115f) * .53f;
                float body = Mathf.Sin(2f * Mathf.PI * (baseHz * t - 175f * t * t)) *
                    Mathf.Exp(-t * 47f) * .21f;
                float attack = Mathf.Clamp01(t / .00045f);
                pcm[i] = Mathf.Clamp((transient + body) * attack, -.72f, .72f);
            }
            AudioClip clip = AudioClip.Create(name, count, 1, sampleRate, false);
            clip.SetData(pcm, 0);
            return clip;
        }

        void BuildWaveform()
        {
            waveReady = false;
            if (song == null || song.samples < 1) return;
            int buckets = Mathf.Min(1500, Mathf.Max(30, Mathf.RoundToInt(song.length * 12f)));
            waveform = new float[buckets];
            int channels = Mathf.Max(1, song.channels);
            int frames = 256;
            float[] values = new float[frames * channels];
            try
            {
                for (int i = 0; i < buckets; i++)
                {
                    int sample = Mathf.Clamp(Mathf.RoundToInt(i / (float)buckets * song.samples), 0, Mathf.Max(0, song.samples - frames));
                    if (!song.GetData(values, sample)) { waveform = null; return; }
                    float max = 0;
                    for (int k = 0; k < values.Length; k++) max = Mathf.Max(max, Mathf.Abs(values[k]));
                    waveform[i] = max;
                }
                waveReady = true;
            }
            catch { waveform = null; }
        }

        Vector2 VirtualMouse()
        {
            Vector2 px = MousePosition();
            return new Vector2((px.x - origin.x) / Mathf.Max(.0001f, scale),
                (Screen.height - px.y - origin.y) / Mathf.Max(.0001f, scale));
        }

        static bool KeyDown(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#elif ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (key)
            {
                case KeyCode.D: return Keyboard.current.dKey.wasPressedThisFrame;
                case KeyCode.F: return Keyboard.current.fKey.wasPressedThisFrame;
                case KeyCode.J: return Keyboard.current.jKey.wasPressedThisFrame;
                case KeyCode.K: return Keyboard.current.kKey.wasPressedThisFrame;
                case KeyCode.Space: return Keyboard.current.spaceKey.wasPressedThisFrame;
                case KeyCode.Return: return Keyboard.current.enterKey.wasPressedThisFrame;
                case KeyCode.UpArrow: return Keyboard.current.upArrowKey.wasPressedThisFrame;
                case KeyCode.DownArrow: return Keyboard.current.downArrowKey.wasPressedThisFrame;
                case KeyCode.F1: return Keyboard.current.f1Key.wasPressedThisFrame;
                case KeyCode.F3: return Keyboard.current.f3Key.wasPressedThisFrame;
                case KeyCode.F4: return Keyboard.current.f4Key.wasPressedThisFrame;
                case KeyCode.Alpha1: return Keyboard.current.digit1Key.wasPressedThisFrame;
                case KeyCode.Alpha2: return Keyboard.current.digit2Key.wasPressedThisFrame;
                case KeyCode.Alpha3: return Keyboard.current.digit3Key.wasPressedThisFrame;
                case KeyCode.Alpha4: return Keyboard.current.digit4Key.wasPressedThisFrame;
                case KeyCode.Alpha5: return Keyboard.current.digit5Key.wasPressedThisFrame;
                case KeyCode.Alpha6: return Keyboard.current.digit6Key.wasPressedThisFrame;
                case KeyCode.Alpha7: return Keyboard.current.digit7Key.wasPressedThisFrame;
                case KeyCode.Escape: return Keyboard.current.escapeKey.wasPressedThisFrame;
                case KeyCode.S: return Keyboard.current.sKey.wasPressedThisFrame;
                case KeyCode.O: return Keyboard.current.oKey.wasPressedThisFrame;
                case KeyCode.Z: return Keyboard.current.zKey.wasPressedThisFrame;
                case KeyCode.Y: return Keyboard.current.yKey.wasPressedThisFrame;
                case KeyCode.A: return Keyboard.current.aKey.wasPressedThisFrame;
                case KeyCode.C: return Keyboard.current.cKey.wasPressedThisFrame;
                case KeyCode.V: return Keyboard.current.vKey.wasPressedThisFrame;
                case KeyCode.Delete: return Keyboard.current.deleteKey.wasPressedThisFrame;
                case KeyCode.Backspace: return Keyboard.current.backspaceKey.wasPressedThisFrame;
                default: return false;
            }
#else
            return false;
#endif
        }

        static bool KeyHeld(KeyCode key)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#elif ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            switch (key)
            {
                case KeyCode.D: return Keyboard.current.dKey.isPressed;
                case KeyCode.F: return Keyboard.current.fKey.isPressed;
                case KeyCode.J: return Keyboard.current.jKey.isPressed;
                case KeyCode.K: return Keyboard.current.kKey.isPressed;
                case KeyCode.LeftControl: return Keyboard.current.leftCtrlKey.isPressed;
                case KeyCode.RightControl: return Keyboard.current.rightCtrlKey.isPressed;
                case KeyCode.LeftCommand: return Keyboard.current.leftMetaKey.isPressed;
                case KeyCode.RightCommand: return Keyboard.current.rightMetaKey.isPressed;
                case KeyCode.LeftShift: return Keyboard.current.leftShiftKey.isPressed;
                case KeyCode.RightShift: return Keyboard.current.rightShiftKey.isPressed;
                default: return false;
            }
#else
            return false;
#endif
        }

        static Vector2 MousePosition()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mousePosition;
#elif ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
            return Vector2.zero;
#endif
        }

        static bool MouseDown(int button)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(button);
#elif ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            return button == 0 ? Mouse.current.leftButton.wasPressedThisFrame : Mouse.current.rightButton.wasPressedThisFrame;
#else
            return false;
#endif
        }

        static bool MouseUp(int button)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonUp(button);
#elif ENABLE_INPUT_SYSTEM
            return Mouse.current != null && Mouse.current.leftButton.wasReleasedThisFrame;
#else
            return false;
#endif
        }

        static float MouseScroll()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.mouseScrollDelta.y;
#elif ENABLE_INPUT_SYSTEM
            return Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;
#else
            return 0;
#endif
        }
    }
}
