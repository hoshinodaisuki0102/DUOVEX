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
    /// Self-starting, source-only prototype. No prefabs, scenes, textures or fonts required.
    /// Replace the IMGUI renderer with Unity UI/SpriteRenderer when moving to production.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed partial class DualSyncGame : MonoBehaviour
    {
        const float ScreenW = 1280f;
        const float ScreenH = 720f;
        const float LaneX = 496f;
        const float LaneW = 96f;
        const float LaneTop = 68f;
        const float HitY = 596f;
        const float ScrollPxPerSecond = 330f;
        const float EditorLaneX = 319f;
        const float EditorLaneW = 178f;
        const float EditorLaneTop = 169f;
        const float EditorHitY = 608f;
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
        GUIStyle flatButtonStyle;
        GUIStyle editorHeadingStyle;
        GUIStyle editorMonoStyle;
        bool loopEnabled;
        float loopStartBeat = 0f;
        float loopEndBeat = 8f;
        float statusVisibleUntil;
        AudioSource audioSource;
        AudioSource hitSoundSource;
        AudioClip song;
        float hitSoundVolume = .32f; // Persistent player-adjustable SFX level.
        bool playing;
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
        GUIStyle timingStyle;
        GUIStyle lessonStyle;
        int combo;
        int maxCombo;
        int totalJudged;
        float earnedPoints;
        int missCount;
        bool[] held = new bool[4];
        float scale = 1f;
        Vector2 origin;
        Texture2D whitePixel;
        Texture2D diagonal;
        Texture2D glow;
        GUIStyle titleStyle;
        GUIStyle subtitleStyle;
        GUIStyle bodyStyle;
        GUIStyle tinyStyle;
        GUIStyle centerStyle;
        GUIStyle judgmentStyle;
        GUIStyle buttonStyle;
        GUIStyle menuButtonStyle;
        GUIStyle heroStyle;
        GUIStyle resultNumberStyle;

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
            noteSpeed = Mathf.Clamp(PlayerPrefs.GetFloat("DUOVEX.NoteSpeed", 1f), .5f, 3f);
            hitSoundSource.volume = hitSoundVolume;
            song = Resources.Load<AudioClip>("song");
            audioSource.clip = song;
            try { editableChart = ChartStorage.Load(); }
            catch (Exception ex) { status = "Chart load error: " + ex.Message; }
            if (editableChart == null) editableChart = ChartStorage.Sample();
            chart = editableChart;
            snapDivision = Mathf.Clamp(chart.tickDivision, 1, 32);
            CreateTextures();
            CreateHitSounds();
            BuildStates();
            if (song != null) BuildWaveform();
            RefreshSongLibrary();
        }

        void OnDestroy()
        {
            if (importedAudioClip != null) Destroy(importedAudioClip);
            if (whitePixel != null) Destroy(whitePixel);
            if (diagonal != null) Destroy(diagonal);
            if (glow != null) Destroy(glow);
            if (generatedWhiteSound && whiteHitSound != null) Destroy(whiteHitSound);
            if (generatedBlackSound && blackHitSound != null) Destroy(blackHitSound);
            PlayerPrefs.SetFloat("DUOVEX.HitSoundVolume", hitSoundVolume);
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
            if (!playing) return;
            songCursor = Mathf.Max(0f, SongTime);
            playing = false;
            if (audioSource != null) audioSource.Stop();
            status = "PAUSED";
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

        void StartTutorial()
        {
            PausePlaying();
            tutorialActive = true;
            activeImported = null;
            chart = ChartStorage.Tutorial();
            page = Page.Gameplay;
            songCursor = 0f;
            Restart(true);
        }

        void RetryTrack()
        {
            if (tutorialActive) StartTutorial();
            else { page = Page.Gameplay; Restart(true); }
        }

        void FinishTrack()
        {
            PausePlaying();
            if (page == Page.Gameplay)
            {
                page = Page.Results;
                status = tutorialActive ? "TUTORIAL FINISHED  /  VIEW RESULTS" :
                    "CHART TEST FINISHED  /  VIEW RESULTS";
            }
            else status = "TRACK COMPLETE  /  PRESS REWIND";
        }

        void Update()
        {
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
                if (page == Page.Gameplay || page == Page.Results) OpenSongSelect();
                else if (page == Page.Editor || page == Page.SongSelect || page == Page.Options) OpenMainMenu();
                return;
            }
            if (page == Page.MainMenu || page == Page.SongSelect || page == Page.Results || page == Page.Options || page == Page.FileBrowser)
            {
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
                if (playing) PausePlaying();
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

        void CreateTextures()
        {
            whitePixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            whitePixel.SetPixel(0, 0, Color.white); whitePixel.Apply();
            diagonal = new Texture2D(64, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 32; y++) for (int x = 0; x < 64; x++)
            {
                float u = x / 63f, v = y / 31f;
                diagonal.SetPixel(x, y, u > 1f - v ? new Color(.035f, .045f, .085f) : new Color(.97f, .98f, 1f));
            }
            diagonal.Apply();
            glow = new Texture2D(64, 64, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
            for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
            {
                float dx = (x - 31.5f) / 31.5f, dy = (y - 31.5f) / 31.5f;
                float a = Mathf.Pow(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)), 1.5f);
                glow.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            glow.Apply();
        }

        // Short non-melodic percussion samples. WAV assets are intentionally separated
        // from code so they can be replaced later without recompiling the game.
        // A quiet synthesized fallback is included for partial patch installations.
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

        void EnsureStyles()
        {
            if (titleStyle != null) return;
            titleStyle = LabelStyle(23, FontStyle.Bold, new Color(.79f, .96f, 1f));
            subtitleStyle = LabelStyle(14, FontStyle.Bold, new Color(.75f, .83f, 1f));
            bodyStyle = LabelStyle(13, FontStyle.Normal, new Color(.74f, .8f, .93f));
            tinyStyle = LabelStyle(11, FontStyle.Normal, new Color(.57f, .66f, .83f));
            centerStyle = LabelStyle(14, FontStyle.Bold, new Color(.87f, .94f, 1f));
            centerStyle.alignment = TextAnchor.MiddleCenter;
            judgmentStyle = LabelStyle(28, FontStyle.Bold, Color.white);
            judgmentStyle.alignment = TextAnchor.MiddleCenter;
            timingStyle = LabelStyle(13, FontStyle.Bold, new Color(.89f, .93f, 1f));
            timingStyle.alignment = TextAnchor.MiddleCenter;
            lessonStyle = LabelStyle(16, FontStyle.Bold, Color.white);
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.normal.textColor = new Color(.86f, .91f, 1f);
            buttonStyle.hover.textColor = Color.white;
            buttonStyle.active.textColor = new Color(.4f, .95f, 1f);
            menuButtonStyle = new GUIStyle(buttonStyle);
            menuButtonStyle.alignment = TextAnchor.MiddleCenter;
            menuButtonStyle.fontSize = 17;
            menuButtonStyle.normal.background = null;
            menuButtonStyle.hover.background = null;
            menuButtonStyle.active.background = null;
            menuButtonStyle.focused.background = null;
            heroStyle = LabelStyle(78, FontStyle.Bold, new Color(.91f, .97f, 1f));
            resultNumberStyle = LabelStyle(57, FontStyle.Bold, Cyan);
            editorHeadingStyle = LabelStyle(16, FontStyle.Bold, new Color(.84f, .92f, 1f));
            editorMonoStyle = LabelStyle(12, FontStyle.Bold, new Color(.70f, .80f, .94f));
            flatButtonStyle = new GUIStyle(buttonStyle);
            flatButtonStyle.alignment = TextAnchor.MiddleCenter;
            flatButtonStyle.normal.background = null;
            flatButtonStyle.hover.background = null;
            flatButtonStyle.active.background = null;
            flatButtonStyle.focused.background = null;
        }

        static GUIStyle LabelStyle(int size, FontStyle style, Color color)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style };
            s.normal.textColor = color;
            s.clipping = TextClipping.Clip;
            return s;
        }

        void OnGUI()
        {
            EnsureStyles();
            scale = Mathf.Min(Screen.width / ScreenW, Screen.height / ScreenH);
            origin = new Vector2((Screen.width - ScreenW * scale) * .5f, (Screen.height - ScreenH * scale) * .5f);
            Matrix4x4 old = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(new Vector3(origin.x, origin.y), Quaternion.identity, new Vector3(scale, scale, 1f));
            DrawBackground();
            if (page == Page.MainMenu) DrawMainMenuV05();
            else if (page == Page.Options) DrawOptions();
            else if (page == Page.SongSelect) DrawLibrarySongSelect();
            else if (page == Page.FileBrowser) DrawFileBrowser();
            else if (page == Page.Results) DrawResults();
            else if (editorMode)
            {
                DrawEditorShell();
                DrawEditorGrid();
                DrawChartEditor();
            }
            else
            {
                DrawTrack();
                DrawGameplayNotes();
                DrawHitLineOverlay();
                DrawGameplayHud();
                if (!playing) DrawPauseOverlay();
            }
            DrawFooter();
            GUI.matrix = old;
        }

        void Fill(Rect r, Color c)
        {
            Color old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, whitePixel);
            GUI.color = old;
        }

        void Texture(Rect r, Texture2D t, Color color)
        {
            Color old = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(r, t, ScaleMode.StretchToFill);
            GUI.color = old;
        }

        void Border(Rect r, Color c, float thickness = 1f)
        {
            Fill(new Rect(r.x, r.y, r.width, thickness), c);
            Fill(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
            Fill(new Rect(r.x, r.y, thickness, r.height), c);
            Fill(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
        }

        void Label(Rect r, string value, GUIStyle style) => GUI.Label(r, value, style);
        static Color Cyan => new Color(.33f, .91f, 1f);
        static Color Violet => new Color(.7f, .48f, 1f);
        static Color WhiteNote => new Color(.95f, .98f, 1f);
        static Color BlackNote => new Color(.035f, .045f, .085f);

        void DrawBackground()
        {
            Fill(new Rect(0, 0, ScreenW, ScreenH), new Color(.035f, .052f, .10f));
            // Subtle moving 'data grid' outside the gear.
            float phase = (float)(AudioSettings.dspTime % 4.0);
            for (int i = 0; i < 15; i++)
            {
                float y = (i * 54f + phase * 13f) % ScreenH;
                Fill(new Rect(0, y, ScreenW, 1), new Color(.24f, .43f, .69f, .055f));
            }
            Texture(new Rect(375, 35, 525, 660), glow, new Color(.24f, .15f, .54f, .17f));
            if (page != Page.MainMenu)
            {
                Fill(new Rect(0, 0, ScreenW, 40), new Color(.025f, .034f, .07f, .88f));
                Label(new Rect(30, 8, 320, 33), "DUOVEX", titleStyle);
            }
            string section = page == Page.Editor ? "CHART STUDIO" :
                page == Page.SongSelect ? "SONG SELECT" : page == Page.Results ? "RESULT" :
                page == Page.Options ? "OPTIONS" : page == Page.FileBrowser ? "IMPORT AUDIO" :
                page == Page.MainMenu ? "MAIN MENU" : "RHYTHM SYSTEM";
            if (page != Page.Gameplay && page != Page.MainMenu) Label(new Rect(999, 12, 277, 20), section, subtitleStyle);
        }

        static int TutorialStage(float beat)
        {
            if (beat < 18f) return 0;
            if (beat < 34f) return 1;
            if (beat < 50f) return 2;
            if (beat < 68f) return 3;
            if (beat < 85f) return 4;
            return 5;
        }

        static bool TutorialKeyHighlighted(int input, int stage)
        {
            if (stage == 0) return input <= 1;
            if (stage == 1) return input >= 2;
            if (stage == 2 || stage == 3) return input == 1 || input == 2;
            if (stage == 4) return true;
            return false;
        }

        void Panel(Rect rect)
        {
            Fill(rect, new Color(.062f, .083f, .15f, .96f));
            Border(rect, new Color(.29f, .54f, .78f, .55f), 1.5f);
            Fill(new Rect(rect.x, rect.y, rect.width, 3), Cyan);
        }

        bool NeonButton(Rect rect, string caption, bool emphasized)
        {
            Fill(rect, emphasized ? new Color(.095f, .35f, .47f, .90f) : new Color(.12f, .15f, .26f, .97f));
            Border(rect, emphasized ? Cyan : new Color(.38f, .47f, .68f), 1.5f);
            bool click = GUI.Button(rect, caption, menuButtonStyle);
            if (click) status = caption;
            return click;
        }

        void DrawMainMenu()
        {
            // Restrained, animated cyber geometry. No images or external assets required.
            float sway = Mathf.Sin(Time.unscaledTime * .7f) * 12f;
            Texture(new Rect(65 + sway, 78, 660, 545), glow, new Color(.15f, .80f, 1f, .20f));
            Texture(new Rect(785 - sway, 78, 440, 550), glow, new Color(.58f, .32f, 1f, .22f));
            Fill(new Rect(78, 176, 6, 343), Cyan);
            Fill(new Rect(89, 176, 2, 343), Violet);
            
            Label(new Rect(106, 230, 660, 111), "DUOVEX", heroStyle);
            Fill(new Rect(117, 342, 562, 3), new Color(.29f, .36f, .54f));
            Fill(new Rect(117, 342, 281, 3), Cyan);
            Fill(new Rect(398, 342, 281, 3), Violet);
            


            // Abstract playable lanes as a logo motif.
            for (int i = 0; i < 3; i++)
            {
                float x = 127 + i * 120f;
                Fill(new Rect(x, 487, 109, 104), i == 1 ? new Color(.15f, .13f, .30f) : new Color(.08f, .13f, .20f));
                Border(new Rect(x, 487, 109, 104), i == 1 ? Violet : Cyan, 1.3f);
            }
            Fill(new Rect(143, 527, 79, 11), WhiteNote);
            Border(new Rect(143, 527, 79, 11), Cyan, 1f);
            Texture(new Rect(263, 510, 79, 15), diagonal, Color.white);
            Border(new Rect(263, 510, 79, 15), Violet, 1f);
            Fill(new Rect(384, 553, 79, 11), BlackNote);
            Border(new Rect(384, 553, 79, 11), Violet, 1f);
            Fill(new Rect(126, 592, 352, 3), Cyan);
            

            Panel(new Rect(790, 183, 390, 366));
            

            if (NeonButton(new Rect(824, 222, 322, 76), "PLAY", true)) OpenSongSelect();
            if (NeonButton(new Rect(824, 318, 322, 76), "CHART STUDIO", false)) EnterEditor();
            if (NeonButton(new Rect(824, 414, 322, 76), "OPTIONS", false)) OpenOptions();

        }

        void DrawSongSelect()
        {
            Label(new Rect(73, 91, 620, 48), "SELECT A TRACK", titleStyle);
            
            Panel(new Rect(76, 208, 1128, 342));
            Fill(new Rect(99, 230, 335, 294), new Color(.035f, .055f, .12f));
            Border(new Rect(99, 230, 335, 294), new Color(.4f, .67f, .9f, .65f), 2f);
            for (int i = 0; i < 3; i++)
            {
                float x = 147 + i * 80;
                Fill(new Rect(x, 265, 76, 210), i == 1 ?
                    new Color(.18f, .16f, .33f) : new Color(.10f, .13f, .22f));
                Border(new Rect(x, 265, 76, 210), i == 1 ? Violet : Cyan);
            }
            Fill(new Rect(147, 455, 236, 3), Cyan);
            Fill(new Rect(162, 315, 48, 15), WhiteNote);
            Fill(new Rect(242, 366, 48, 15), BlackNote);
            Border(new Rect(242, 366, 48, 15), Violet);
            Texture(new Rect(242, 408, 48, 17), diagonal, Color.white);
            Label(new Rect(488, 249, 638, 52), "TUTORIAL", titleStyle);
            Label(new Rect(489, 309, 650, 29), "BPM 110   ·   3 LANES / 4 KEYS", subtitleStyle);

            if (NeonButton(new Rect(489, 398, 310, 60), "PLAY", true)) StartTutorial();
            if (NeonButton(new Rect(813, 398, 282, 60), "BACK", false)) OpenMainMenu();
            
        }

        void DrawResults()
        {
            float accuracy = totalJudged > 0 ? earnedPoints / totalJudged * 100f : 0f;
            Panel(new Rect(225, 106, 830, 475));
            Label(new Rect(263, 144, 745, 40),
                tutorialActive ? "TUTORIAL COMPLETE" : "TRACK COMPLETE", titleStyle);

            Label(new Rect(264, 270, 500, 38), "ACCURACY", subtitleStyle);
            Label(new Rect(260, 306, 560, 90), accuracy.ToString("0.00") + "%", resultNumberStyle);
            Label(new Rect(788, 270, 210, 28), "MAX COMBO", subtitleStyle);
            Label(new Rect(788, 302, 225, 45), maxCombo.ToString("0000"), titleStyle);
            Label(new Rect(788, 359, 210, 28), "MISS", subtitleStyle);
            Label(new Rect(788, 391, 210, 45), missCount.ToString("0000"), titleStyle);
            Label(new Rect(264, 414, 470, 30), "SCORE  " + Mathf.RoundToInt(earnedPoints * 1000f), bodyStyle);
            DrawResultBreakdown();
            if (NeonButton(new Rect(263, 490, 225, 56), "RETRY", true)) RetryTrack();
            if (NeonButton(new Rect(501, 490, 232, 56), "SONG SELECT", false)) OpenSongSelect();
            if (NeonButton(new Rect(747, 490, 266, 56), "MAIN MENU", false)) OpenMainMenu();
            
        }

        void DrawOptions()
        {
            Label(new Rect(104, 96, 620, 40), "OPTIONS", titleStyle);
            
            Panel(new Rect(104, 205, 1071, 367));
            Label(new Rect(154, 248, 612, 38), "HIT SOUND VOLUME", titleStyle);

            float updated = GUI.HorizontalSlider(new Rect(154, 363, 560, 28), hitSoundVolume, 0f, 1f);
            SetHitSoundVolume(updated);
            Label(new Rect(735, 346, 160, 38), Mathf.RoundToInt(hitSoundVolume * 100f) + "%", titleStyle);
            if (NeonButton(new Rect(155, 415, 242, 55), "PREVIEW WHITE", false)) PreviewHitSound(true);
            if (NeonButton(new Rect(413, 415, 242, 55), "PREVIEW BLACK", false)) PreviewHitSound(false);
            Label(new Rect(154, 493, 280, 30), "NOTE SPEED", subtitleStyle);
            if (GUI.Button(new Rect(483, 487, 48, 35), "-", buttonStyle)) AdjustNoteSpeed(-.1f);
            Label(new Rect(542, 491, 120, 28), noteSpeed.ToString("0.0") + "x", editorHeadingStyle);
            if (GUI.Button(new Rect(665, 487, 48, 35), "+", buttonStyle)) AdjustNoteSpeed(.1f);
            if (NeonButton(new Rect(853, 491, 260, 58), "BACK", true))
            {
                PlayerPrefs.Save();
                OpenMainMenu();
            }

            
        }

        void DrawPauseOverlay()
        {
            Fill(new Rect(LaneX - 20, 231, 3 * LaneW + 40, 148), new Color(.022f, .034f, .073f, .92f));
            Fill(new Rect(LaneX - 20, 231, 3 * LaneW + 40, 3), Cyan);
            var big = new GUIStyle(titleStyle);
            big.alignment = TextAnchor.MiddleCenter;
            Label(new Rect(LaneX - 20, 248, 3 * LaneW + 40, 60), "PAUSED", big);
            Label(new Rect(LaneX - 20, 308, 3 * LaneW + 40, 36), "SPACE   RESUME", centerStyle);
            Label(new Rect(LaneX - 20, 344, 3 * LaneW + 40, 24), "F3 / F4   SPEED", timingStyle);
        }

        void DrawTrack()
        {
            for (int lane = 0; lane < 3; lane++)
            {
                float x = LaneX + lane * LaneW;
                Fill(new Rect(x, LaneTop, LaneW, HitY - LaneTop), lane == 1 ?
                    new Color(.13f, .13f, .24f, .94f) : new Color(.072f, .09f, .15f, .96f));
                // Lane color blocks distinguish the lanes. Avoid tall vertical
                // borders that distract from incoming notes and the horizontal hit-line.
            }
            // The bright judgement line is rendered *after* notes so it never disappears.
            Fill(new Rect(LaneX, HitY - 13, 3 * LaneW, 28), new Color(.20f, .54f, .80f, .12f));
            for (int lane = 0; lane < 4; lane++)
            {
                float x = lane == 0 ? LaneX : lane == 1 ? LaneX + LaneW : lane == 2 ? LaneX + LaneW * 1.5f : LaneX + LaneW * 2;
                float w = lane == 0 || lane == 3 ? LaneW : LaneW * .5f;
                Color c = IsWhite(lane) ? Cyan : Violet;
                bool on = held[lane] && !editorMode;
                bool guided = tutorialActive && page == Page.Gameplay &&
                    TutorialKeyHighlighted(lane, TutorialStage(chart.SecondsToBeat(Mathf.Max(0f, SongTime))));
                if (on)
                {
                    Fill(new Rect(x + 3, HitY + 5, w - 6, 38), new Color(c.r, c.g, c.b, .26f));
                    Texture(new Rect(x, HitY - 115, w, 155), glow, new Color(c.r, c.g, c.b, .36f));
                }
                Fill(new Rect(x + 3, HitY + 7, w - 6, 32), new Color(c.r, c.g, c.b, on ? .34f : guided ? .17f : .07f));
                Border(new Rect(x + 3, HitY + 7, w - 6, 32),
                    new Color(c.r, c.g, c.b, on ? 1f : guided ? .82f : .29f), on ? 2.5f : 1f);
                if (guided && !on) Fill(new Rect(x + 7, HitY + 5, w - 14, 2), new Color(c.r, c.g, c.b, .9f));
                Label(new Rect(x + 2, HitY + 8, w - 4, 26), "DFJK"[lane].ToString(), centerStyle);
            }

        }

        void DrawHitLineOverlay()
        {
            // Clear horizontal judgement line with no perpendicular markers.
            // Keep its v0.3 thickness and foreground draw order.
            float left = LaneX;
            float width = LaneW * 3f;
            Texture(new Rect(left - 22, HitY - 27, width + 44, 54), glow,
                new Color(.30f, .86f, 1f, .54f));
            Fill(new Rect(left - 5, HitY - 6, width + 10, 2), Cyan);
            Fill(new Rect(left - 6, HitY - 3, width + 12, 6), new Color(.91f, .97f, 1f));
            Fill(new Rect(left - 5, HitY + 4, width + 10, 2), Violet);
        }

        void DrawTap(float x, float y, float w, bool white, bool combined)
        {
            float visibleTop = editorMode ? EditorLaneTop : LaneTop;
            float visibleBottom = editorMode ? EditorHitY : HitY;
            if (y < visibleTop - 17 || y > visibleBottom + 17) return;
            Color color = combined ? new Color(.7f, .74f, 1f) : white ? Cyan : Violet;
            Texture(new Rect(x - 8, y - 11, w + 16, 36), glow, new Color(color.r, color.g, color.b, .53f));
            Fill(new Rect(x, y - 2, w, 2), color);
            if (combined) Texture(new Rect(x, y, w, 17), diagonal, Color.white);
            else Fill(new Rect(x, y, w, 17), white ? WhiteNote : BlackNote);
            Border(new Rect(x, y, w, 17), color, 1.6f);
            Fill(new Rect(x + 6, y + 20, w - 12, 2), new Color(color.r, color.g, color.b, .65f));
        }

        void DrawHold(float x, float headY, float endY, float width, bool white, bool combined, bool active)
        {
            float top = Mathf.Max(editorMode ? EditorLaneTop : LaneTop, endY);
            float bottom = Mathf.Min((editorMode ? EditorHitY : HitY) + 1f, headY);
            if (bottom > top)
            {
                Color c = white ? Cyan : Violet;
                float alpha = active ? .31f : .17f;
                Fill(new Rect(x, top, width, bottom - top), new Color(c.r, c.g, c.b, alpha));
                Fill(new Rect(x, top, 2f, bottom - top), new Color(c.r, c.g, c.b, active ? .85f : .42f));
                Fill(new Rect(x + width - 2, top, 2f, bottom - top), new Color(c.r, c.g, c.b, active ? .85f : .42f));
                for (float y = top + 15; y < bottom; y += 26)
                    Fill(new Rect(x + 5, y, width - 10, 1f), new Color(c.r, c.g, c.b, .34f));
            }
            if (headY <= (editorMode ? EditorHitY : HitY) + 3f) DrawTap(x, headY, width, white, combined);
        }

        void DrawGameplayNotes()
        {
            float time = SongTime;
            // Back-to-front: hold bodies followed by note heads.
            foreach (var p in playNotes)
            {
                if (p.finished || p.note.durationBeats <= 0) continue;
                float headY = HitY - (chart.BeatToSeconds(p.note.beat) - time) * (ScrollPxPerSecond * noteSpeed);
                float tailY = HitY - (chart.BeatToSeconds(p.note.beat + p.note.durationBeats) - time) * (ScrollPxPerSecond * noteSpeed);
                if (headY < LaneTop - 50 || tailY > HitY + 24) continue;
                float x = LaneX + PartLane(p.note.input) * LaneW + 6;
                bool isCenter = p.note.input == 1 || p.note.input == 2;
                float w = LaneW - 12;
                // Share the center lane visually during simultaneous HOLDs.
                bool pairedHold = isCenter && HasPair(p.note, true);
                if (pairedHold)
                {
                    w = (LaneW - 12) / 2f;
                    if (p.note.input == 2) x += w;
                }
                bool active = held[p.note.input] && p.headDone;
                DrawHold(x, headY, tailY, w, IsWhite(p.note.input), false, active);
            }
            // Shared-lane DUAL HOLD heads use the same diagonal split as DUAL TAP.
            foreach (var p in playNotes)
            {
                if (p.finished || p.headDone || p.note.input != 1 || p.note.durationBeats <= 0) continue;
                if (!HasPair(p.note, true)) continue;
                float y = HitY - (chart.BeatToSeconds(p.note.beat) - time) * (ScrollPxPerSecond * noteSpeed);
                DrawTap(LaneX + LaneW + 6f, y, LaneW - 12f, true, true);
            }
            foreach (var p in playNotes)
            {
                if (p.finished || p.note.durationBeats > 0 || p.headDone) continue;
                if (p.note.input == 2 && HasPair(p.note, false)) continue;
                float y = HitY - (chart.BeatToSeconds(p.note.beat) - time) * (ScrollPxPerSecond * noteSpeed);
                bool pair = p.note.input == 1 && HasPair(p.note, false);
                DrawTap(LaneX + PartLane(p.note.input) * LaneW + 6, y, LaneW - 12,
                    IsWhite(p.note.input), pair);
            }
            foreach (var b in bursts)
            {
                float age = Time.unscaledTime - b.birth;
                float t = Mathf.Clamp01(age / .27f);
                float x = LaneX + PartLane(b.input) * LaneW;
                float w = LaneW;
                if (b.input == 1) w *= .5f;
                if (b.input == 2) { x += LaneW * .5f; w *= .5f; }
                Color c = IsWhite(b.input) ? Cyan : Violet;
                Texture(new Rect(x - 20 - t * 10, HitY - 36 - t * 28, w + 40 + t * 20, 94 + t * 20),
                    glow, new Color(c.r, c.g, c.b, (1f - t) * .83f));
                Fill(new Rect(x + 7 - t * 8, HitY - 4 - t * 28, w - 14 + t * 16, 2),
                    new Color(c.r, c.g, c.b, (1f - t) * .7f));
            }
        }

        bool HasPair(ChartNote note, bool hold)
        {
            if (note.input != 1 && note.input != 2) return false;
            foreach (var p in playNotes)
            {
                if (p.note.input != (note.input == 1 ? 2 : 1)) continue;
                if (p.finished || p.headDone && !hold) continue;
                if (Mathf.Abs(p.note.beat - note.beat) < .0001f &&
                    Mathf.Abs(p.note.durationBeats - note.durationBeats) < .0001f &&
                    (p.note.durationBeats > 0f) == hold) return true;
            }
            return false;
        }

        void DrawGameplayHud()
        {
            float acc = totalJudged == 0 ? 100f : earnedPoints / totalJudged * 100f;
            Label(new Rect(50, 80, 400, 30), chart.title, subtitleStyle);

            Label(new Rect(50, 172, 250, 35), "ACCURACY", subtitleStyle);
            Label(new Rect(50, 199, 260, 44), acc.ToString("00.00") + "%", titleStyle);
            
            
            
            
            
            
            if (tutorialActive) DrawTutorialGuide();
            Label(new Rect(830, 160, 360, 30), "COMBO", subtitleStyle);
            Label(new Rect(830, 200, 360, 62), combo.ToString("0000"), titleStyle);
            Label(new Rect(830, 270, 340, 28), "SCORE  " + Mathf.RoundToInt(earnedPoints * 1000f), bodyStyle);
            Label(new Rect(830, 307, 340, 25), "SPEED  " + noteSpeed.ToString("0.0") + "x", subtitleStyle);
            if (Time.unscaledTime - lastResultTime < .52f && !string.IsNullOrEmpty(lastResult))
            {
                // The main grade never shares its line with FAST or SLOW.
                // PERFECT deliberately displays no timing subtitle.
                judgmentStyle.normal.textColor = lastResultColor;
                Label(new Rect(LaneX - 27, 419, 3 * LaneW + 54, 44), lastResult, judgmentStyle);
                if (lastTiming == "FAST")
                {
                    timingStyle.normal.textColor = new Color(.72f, .88f, 1f);
                    Label(new Rect(LaneX - 27, 400, 3 * LaneW + 54, 19), "FAST", timingStyle);
                }
                else if (lastTiming == "SLOW")
                {
                    timingStyle.normal.textColor = new Color(1f, .75f, .81f);
                    Label(new Rect(LaneX - 27, 460, 3 * LaneW + 54, 19), "SLOW", timingStyle);
                }
            }
            float duration = CurrentSong != null ? CurrentSong.length : ChartEndTime();
            float progress = Mathf.Clamp01(SongTime / Mathf.Max(.01f, duration));
            Fill(new Rect(50, 650, 1180, 3), new Color(.20f, .28f, .44f));
            Fill(new Rect(50, 650, 1180 * progress, 3), Cyan);
            Label(new Rect(975, 661, 290, 22), Mathf.Max(0, SongTime).ToString("0.0") +
                " / " + duration.ToString("0.0") + "s", tinyStyle);
        }

        void DrawTutorialGuide()
        {
            int step = TutorialStage(chart.SecondsToBeat(Mathf.Max(0f, SongTime)));
            string[] titles = { "WHITE NOTES", "BLACK NOTES", "SHARED LANE", "DUAL INPUT", "HOLD / TICKS", "FINAL PRACTICE" };
            string[] descriptions = {
                "D / F", "J / K", "F / J", "F + J", "HOLD", "D F J K"
            };
            Color accent = step == 1 ? Violet : step == 3 ? new Color(.84f, .77f, 1f) : Cyan;
            Fill(new Rect(46, 459, 408, 121), new Color(.055f, .085f, .15f, .9f));
            Border(new Rect(46, 459, 408, 121), new Color(accent.r, accent.g, accent.b, .6f), 1f);
            Fill(new Rect(46, 459, 408, 3), accent);
            Label(new Rect(61, 468, 265, 25), "STEP  " + (step + 1).ToString("00") + " / 06", tinyStyle);
            Label(new Rect(61, 492, 378, 27), titles[step], lessonStyle);
            Label(new Rect(61, 520, 374, 34), descriptions[step], subtitleStyle);
            for (int i = 0; i < 6; i++)
                Fill(new Rect(61 + i * 63, 564, 57, 4),
                    i <= step ? accent : new Color(.28f, .35f, .49f, .68f));
        }

        void DrawResultBreakdown()
        {
            // Show grade counts with matching grade hues. Hold ticks count as judgements.
            string[] names = { "PERFECT", "GREAT", "GOOD", "BAD", "MISS" };
            for (int i = 0; i < names.Length; i++)
            {
                float x = 262 + i * 147;
                Fill(new Rect(x, 456, 136, 27), new Color(.07f, .12f, .22f, .89f));
                Fill(new Rect(x, 456, 3, 27), JudgmentColor((Result)i));
                var style = new GUIStyle(tinyStyle);
                style.normal.textColor = JudgmentColor((Result)i);
                Label(new Rect(x + 10, 458, 127, 22), names[i] + "  " + resultCounts[i], style);
            }
            Label(new Rect(788, 431, 212, 23), "FAST " + fastCount + "   SLOW " + slowCount, tinyStyle);
        }

        void DrawEditorGrid()
        {
            float firstBeat = Mathf.Max(0f, Mathf.Floor(editorBaseBeat * snapDivision) / snapDivision);
            float lastBeat = editorBaseBeat + (EditorHitY - EditorLaneTop) / editorPxPerBeat;
            int stride = Mathf.Max(1, Mathf.CeilToInt(7f * snapDivision / editorPxPerBeat));
            int firstStep = Mathf.CeilToInt(firstBeat * snapDivision / stride) * stride;
            int lastStep = Mathf.Min(200000, Mathf.CeilToInt(lastBeat * snapDivision));
            for (int index = firstStep; index <= lastStep; index += stride)
            {
                float beat = index / (float)snapDivision;
                float y = YAtBeat(beat);
                if (y < EditorLaneTop || y > EditorHitY) continue;
                bool major = Mathf.Abs(beat - Mathf.Round(beat)) < .0002f;
                Fill(new Rect(EditorLaneX, y, 3 * EditorLaneW, major ? 1.4f : .7f),
                    new Color(.67f, .8f, 1f, major ? .33f : .10f));
                if (major) Label(new Rect(EditorLaneX - 43, y - 9, 36, 18), beat.ToString("0"), tinyStyle);
            }
            if (loopEnabled)
            {
                foreach (float b in new[] { loopStartBeat, loopEndBeat })
                {
                    float y = YAtBeat(b);
                    if (y >= EditorLaneTop && y <= EditorHitY)
                        Fill(new Rect(EditorLaneX, y, 3 * EditorLaneW, 2), new Color(1f, .63f, .39f, .95f));
                }
            }
            foreach (var n in chart.notes)
            {
                float y = EditorHitY - (n.beat - editorBaseBeat) * editorPxPerBeat;
                float x = EditorLaneX + PartLane(n.input) * EditorLaneW + 6f;
                float width = EditorLaneW - 12f;
                bool pairHold = n.durationBeats > 0 && (n.input == 1 || n.input == 2) &&
                    chart.notes.Exists(other => other.input == (n.input == 1 ? 2 : 1) &&
                    Mathf.Abs(other.beat - n.beat) < .00001f &&
                    Mathf.Abs(other.durationBeats - n.durationBeats) < .00001f);
                if (n.durationBeats > 0)
                {
                    float yEnd = EditorHitY - (n.beat + n.durationBeats - editorBaseBeat) * editorPxPerBeat;
                    if (pairHold)
                    {
                        width *= .5f;
                        if (n.input == 2) x += width;
                    }
                    DrawHold(x, y, yEnd, width, IsWhite(n.input), false, true);
                }
                else
                {
                    bool pair = n.input == 1 && chart.notes.Exists(other => other.input == 2 &&
                        other.durationBeats == 0 && Mathf.Abs(other.beat - n.beat) < .0001f);
                    if (n.input == 2 && chart.notes.Exists(other => other.input == 1 &&
                        other.durationBeats == 0 && Mathf.Abs(other.beat - n.beat) < .0001f)) continue;
                    DrawTap(x, y, width, IsWhite(n.input), pair);
                }
            }
            foreach (var n in chart.notes)
            {
                if (!selectedIds.Contains(n.id)) continue;
                float y = YAtBeat(n.beat);
                float yTail = YAtBeat(n.beat + n.durationBeats);
                if (y < EditorLaneTop - 30 || yTail > EditorHitY + 24) continue;
                float x = EditorLaneX + PartLane(n.input) * EditorLaneW + 5f;
                float w = EditorLaneW - 10f;
                if (n.input == 1 || n.input == 2) { w *= .5f; if (n.input == 2) x += w; }
                float top = Mathf.Max(EditorLaneTop, yTail - 4f);
                float bottom = Mathf.Min(EditorHitY + 19f, y + 21f);
                if (bottom > top) Border(new Rect(x, top, w, bottom - top), new Color(1f, .79f, .40f), 2f);
            }
            // Overlay a diagonal note head on paired dual holds.
            foreach (var n in chart.notes)
            {
                if (n.input != 1 || n.durationBeats <= 0) continue;
                if (!chart.notes.Exists(other => other.input == 2 &&
                    Mathf.Abs(other.beat - n.beat) < .00001f &&
                    Mathf.Abs(other.durationBeats - n.durationBeats) < .00001f)) continue;
                float y = EditorHitY - (n.beat - editorBaseBeat) * editorPxPerBeat;
                DrawTap(EditorLaneX + EditorLaneW + 6f, y, EditorLaneW - 12f, true, true);
            }
            if (holdDrag)
            {
                Vector2 m = VirtualMouse();
                float end = Snap(Mathf.Max(dragBeat + 1f / snapDivision,
                    editorBaseBeat + (EditorHitY - m.y) / editorPxPerBeat));
                float y0 = EditorHitY - (dragBeat - editorBaseBeat) * editorPxPerBeat;
                float y1 = EditorHitY - (end - editorBaseBeat) * editorPxPerBeat;
                Fill(new Rect(EditorLaneX + dragLane * EditorLaneW + 7, y1, EditorLaneW - 14, Mathf.Max(2, y0 - y1)),
                    new Color(.32f, .95f, 1f, .29f));
            }
            if (playing)
            {
                float beat = chart.SecondsToBeat(SongTime);
                float y = EditorHitY - (beat - editorBaseBeat) * editorPxPerBeat;
                if (y >= EditorLaneTop && y <= EditorHitY) Fill(new Rect(EditorLaneX - 8, y, 3 * EditorLaneW + 16, 3), new Color(1f, .44f, .52f));
            }
        }

        // One focused workspace: tools | notes | tabbed inspector.
        void EditorPanel(Rect rect)
        {
            Fill(rect, new Color(.058f, .078f, .133f, .97f));
            Border(rect, new Color(.24f, .34f, .49f, .64f));
        }

        bool EditorButton(Rect r, string label, bool selected = false, bool primary = false)
        {
            Color baseColor = primary ? new Color(.11f, .36f, .48f) :
                selected ? new Color(.19f, .26f, .41f) : new Color(.096f, .13f, .20f);
            Fill(r, baseColor);
            Border(r, primary ? Cyan : selected ? Violet : new Color(.29f, .37f, .52f), 1.1f);
            return GUI.Button(r, label, flatButtonStyle);
        }

        void DrawEditorShell()
        {
            Fill(new Rect(0, 40, 1280, 92), new Color(.038f, .053f, .088f, .97f));
            Fill(new Rect(0, 129, 1280, 1), new Color(.24f, .36f, .50f, .55f));
            Label(new Rect(27, 57, 290, 33), "CHART EDITOR", titleStyle);
            Label(new Rect(29, 100, 440, 19), activeImported != null ? activeImported.title : (song != null ? song.name : "UNTITLED CHART"), tinyStyle);
            Fill(new Rect(EditorLaneX - 2, EditorLaneTop - 33, EditorLaneW * 3f + 4, EditorHitY - EditorLaneTop + 36),
                new Color(.028f, .040f, .072f, .95f));
            for (int lane = 0; lane < 3; lane++)
            {
                float x = EditorLaneX + lane * EditorLaneW;
                Fill(new Rect(x, EditorLaneTop, EditorLaneW, EditorHitY - EditorLaneTop),
                    lane == 1 ? new Color(.10f, .12f, .22f) : new Color(.073f, .087f, .143f));
                Fill(new Rect(x, EditorLaneTop, 1f, EditorHitY - EditorLaneTop),
                    new Color(.29f, .37f, .53f, .52f));
                Label(new Rect(x, EditorLaneTop - 30, EditorLaneW, 26),
                    lane == 0 ? "01   WHITE" : lane == 1 ? "02   SHARED" : "03   BLACK", centerStyle);
            }
            Fill(new Rect(EditorLaneX + 3 * EditorLaneW - 1, EditorLaneTop, 1, EditorHitY - EditorLaneTop),
                new Color(.29f, .37f, .53f, .52f));
            Fill(new Rect(EditorLaneX - 2, EditorHitY, 3 * EditorLaneW + 4, 3f), Cyan);
            EditorPanel(new Rect(22, 144, 234, 489));
            EditorPanel(new Rect(EditorPanelX, 144, EditorPanelW, 489));
        }

        void DrawChartEditor()
        {
            // Slim transport at the top; never cover the editing canvas.
            if (EditorButton(new Rect(712, 58, 100, 42), playing ? "PAUSE" : "PLAY", primary: true))
            {
                if (playing) PausePlaying(); else BeginPlaying(songCursor);
            }
            if (EditorButton(new Rect(823, 58, 100, 42), "TEST"))
            {
                PausePlaying(); editableChart = chart; tutorialActive = false;
                page = Page.Gameplay; Restart(true);
            }
            if (EditorButton(new Rect(934, 58, 100, 42), "SAVE")) SaveChart();
            if (EditorButton(new Rect(1045, 58, 104, 42), "REWIND")) Restart(false);
            if (EditorButton(new Rect(1160, 58, 97, 42), "EXIT")) OpenMainMenu();

            Label(new Rect(38, 158, 188, 27), "TOOLS", editorHeadingStyle);
            string[] names = { "SELECT", "WHITE", "BLACK", "DUAL", "W HOLD", "B HOLD", "D HOLD" };
            Tool[] order = { Tool.Select, Tool.WhiteTap, Tool.BlackTap, Tool.DualTap,
                Tool.WhiteHold, Tool.BlackHold, Tool.DualHold };
            for (int i = 0; i < names.Length; i++)
            {
                float y = 193 + i * 43;
                if (EditorButton(new Rect(38, y, 201, 36), (i + 1) + "    " + names[i], currentTool == order[i]))
                    currentTool = order[i];
                if (currentTool == order[i])
                    Fill(new Rect(38, y + 3, 3, 30), i == 2 || i == 5 ? Violet : Cyan);
            }
            Fill(new Rect(37, 506, 202, 1), new Color(.27f, .35f, .49f, .7f));
            Label(new Rect(38, 514, 200, 22), "SNAP", editorMonoStyle);
            int[] snaps = { 1, 2, 4, 8, 16, 32 };
            for (int i = 0; i < snaps.Length; i++)
            {
                int col = i % 3, row = i / 3;
                if (EditorButton(new Rect(38 + col * 69, 542 + row * 39, 63, 33),
                    "1/" + (snaps[i] * 4), snapDivision == snaps[i])) snapDivision = snaps[i];
            }

            string[] tabs = { "AUDIO", "TIMING", "EDIT" };
            for (int i = 0; i < tabs.Length; i++)
            {
                Rect rect = new Rect(EditorPanelX + 12 + i * 112, 157, 105, 33);
                if (EditorButton(rect, tabs[i], (int)inspectorTab == i))
                {
                    inspectorTab = (InspectorTab)i;
                    confirmClear = false;
                }
            }
            Fill(new Rect(EditorPanelX + 14, 197, EditorPanelW - 28, 1f),
                new Color(.25f, .35f, .52f));
            if (inspectorTab == InspectorTab.Audio) DrawEditorAudioTab();
            else if (inspectorTab == InspectorTab.Timing) DrawEditorTimingTab();
            else DrawEditorEditTab();
        }

        void DrawEditorAudioTab()
        {
            float x = EditorPanelX + 18;
            Label(new Rect(x, 211, 320, 27), activeImported != null ? activeImported.title : (song != null ? song.name : "NO AUDIO"), editorHeadingStyle);
            Label(new Rect(x, 245, 320, 25), "TIME    " + Mathf.Max(0, SongTime).ToString("0.000") + " s", editorMonoStyle);
            Label(new Rect(x, 283, 320, 21), "WAVEFORM", editorMonoStyle);
            Rect wave = new Rect(912, 310, 322, 232);
            Fill(wave, new Color(.030f, .047f, .081f));
            Border(wave, new Color(.24f, .34f, .47f));
            const float midX = 1073f;
            Fill(new Rect(midX, 312, 1, 228), new Color(.29f, .40f, .60f, .65f));
            if (waveReady && waveform != null && song != null)
            {
                float visibleBeats = (EditorHitY - EditorLaneTop) / editorPxPerBeat;
                for (int p = 0; p <= 228; p += 2)
                {
                    float beat = editorBaseBeat + (p / 228f) * visibleBeats;
                    float time = chart.BeatToSeconds(beat);
                    int idx = Mathf.RoundToInt(time / Mathf.Max(.01f, song.length) * (waveform.Length - 1));
                    if (idx < 0 || idx >= waveform.Length) continue;
                    float width = Mathf.Clamp01(waveform[idx] * 2.1f) * 145f;
                    Fill(new Rect(midX - width, 540 - p, width * 2f, 2), new Color(.32f, .86f, 1f, .75f));
                }
            }
            else Label(new Rect(936, 412, 273, 24), "NO WAVEFORM", centerStyle);
            if (EditorButton(new Rect(912, 560, 75, 33), "IN"))
                loopStartBeat = Mathf.Max(0, Snap(chart.SecondsToBeat(SongTime)));
            if (EditorButton(new Rect(998, 560, 75, 33), "OUT"))
                loopEndBeat = Mathf.Max(0, Snap(chart.SecondsToBeat(SongTime)));
            if (EditorButton(new Rect(1085, 560, 149, 33), loopEnabled ? "LOOP ON" : "LOOP OFF", loopEnabled))
            {
                loopEnabled = !loopEnabled;
                if (loopEndBeat <= loopStartBeat) loopEndBeat = loopStartBeat + 4f;
                if (loopEnabled) SeekEditorBeat(loopStartBeat);
            }
            if (loopEnabled) Label(new Rect(912, 602, 320, 20),
                "BEATS   " + loopStartBeat.ToString("0.##") + " - " + loopEndBeat.ToString("0.##"), tinyStyle);
        }

        void DrawEditorTimingTab()
        {
            float x = EditorPanelX + 20;
            Label(new Rect(x, 220, 330, 24), "BPM", editorMonoStyle);
            Label(new Rect(x, 253, 147, 46), chart.bpm.ToString("0.##"), titleStyle);
            if (EditorButton(new Rect(x + 200, 252, 52, 36), "-")) { Snapshot(); chart.bpm = ClampBpm(chart.bpm - 1f); }
            if (EditorButton(new Rect(x + 260, 252, 52, 36), "+")) { Snapshot(); chart.bpm = ClampBpm(chart.bpm + 1f); }
            Fill(new Rect(x, 317, 320, 1), new Color(.24f, .34f, .49f));
            Label(new Rect(x, 337, 330, 24), "OFFSET (ms)", editorMonoStyle);
            Label(new Rect(x, 367, 164, 42), chart.offsetMs.ToString("0"), titleStyle);
            if (EditorButton(new Rect(x + 200, 370, 52, 36), "-")) { Snapshot(); chart.offsetMs -= 10f; }
            if (EditorButton(new Rect(x + 260, 370, 52, 36), "+")) { Snapshot(); chart.offsetMs += 10f; }
            Fill(new Rect(x, 437, 320, 1), new Color(.24f, .34f, .49f));
            Label(new Rect(x, 458, 320, 24), "TIMELINE ZOOM", editorMonoStyle);
            Label(new Rect(x, 487, 160, 35), Mathf.RoundToInt(editorPxPerBeat) + " px/beat", bodyStyle);
            if (EditorButton(new Rect(x + 200, 486, 52, 35), "-")) ChangeEditorZoom(1f / 1.3f, 366f);
            if (EditorButton(new Rect(x + 260, 486, 52, 35), "+")) ChangeEditorZoom(1.3f, 366f);
            Label(new Rect(x, 563, 320, 22), "TICKS    1/" + (snapDivision * 4), editorMonoStyle);
        }

        void DrawEditorEditTab()
        {
            float x = EditorPanelX + 19;
            Label(new Rect(x, 211, 325, 27), "NOTES  " + chart.notes.Count, editorHeadingStyle);
            Label(new Rect(x, 245, 325, 24), "SELECTED  " + selectedIds.Count, editorMonoStyle);
            if (EditorButton(new Rect(x, 299, 156, 39), "UNDO")) UndoEdit();
            if (EditorButton(new Rect(x + 168, 299, 156, 39), "REDO")) RedoEdit();
            if (EditorButton(new Rect(x, 351, 156, 39), "COPY")) CopySelection();
            if (EditorButton(new Rect(x + 168, 351, 156, 39), "PASTE")) PasteSelection();
            if (EditorButton(new Rect(x, 403, 156, 39), "DELETE")) DeleteSelection();
            if (EditorButton(new Rect(x + 168, 403, 156, 39), "LOAD")) LoadChart();
            Fill(new Rect(x, 461, 324, 1), new Color(.23f, .33f, .48f));
            if (EditorButton(new Rect(x, 483, 324, 40), confirmClear ? "CONFIRM CLEAR" : "CLEAR CHART", confirmClear))
            {
                if (confirmClear)
                {
                    PausePlaying(); Snapshot(); chart.notes.Clear(); selectedIds.Clear(); BuildStates(); songCursor = 0f;
                    confirmClear = false;
                }
                else confirmClear = true;
            }
            if (EditorButton(new Rect(x, 542, 324, 37), "LOAD DEMO"))
            {
                PausePlaying(); Snapshot(); chart = ChartStorage.Sample(); editableChart = chart;
                selectedIds.Clear(); snapDivision = chart.tickDivision;
                BuildStates(); songCursor = 0f; confirmClear = false;
            }
        }

        void DrawFooter()
        {
            if (page != Page.Editor) return;
            Fill(new Rect(0, 649, 1280, 71), new Color(.029f, .042f, .071f, .97f));
            Fill(new Rect(0, 649, 1280, 1), new Color(.26f, .37f, .49f, .8f));
            Label(new Rect(28, 660, 240, 26), "BEAT   " + chart.SecondsToBeat(Mathf.Max(0, SongTime)).ToString("0.00"), editorMonoStyle);
            Label(new Rect(293, 660, 274, 26), "GRID   1/" + (snapDivision * 4), editorMonoStyle);
            Label(new Rect(552, 660, 276, 26), "ZOOM   " + Mathf.RoundToInt(editorPxPerBeat) + " px", editorMonoStyle);
            Label(new Rect(905, 660, 329, 26), "NOTES   " + chart.notes.Count, editorMonoStyle);
            if (Time.unscaledTime <= statusVisibleUntil)
                Label(new Rect(28, 688, 1217, 21), status, tinyStyle);
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
