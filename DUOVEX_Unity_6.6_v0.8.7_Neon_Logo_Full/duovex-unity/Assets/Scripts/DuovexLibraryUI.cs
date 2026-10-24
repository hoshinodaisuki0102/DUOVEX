using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace DualSync
{
    public sealed partial class DualSyncGame
    {
        readonly List<DuovexSongInfo> importedSongs = new List<DuovexSongInfo>();
        DuovexSongInfo activeImported;
        AudioClip importedAudioClip;
        string importedAudioId;
        // Three silent maps, Light It Up N/H, KETSUDAN, and three new songs N/H.
        // Song picker indexes groups, not individual difficulty chart assets.
        const int BuiltinSongCount = 8;
        const int LocalChartIndex = BuiltinSongCount;
        int selectedDifficultySlot;
        int lastDifficultyGroup = -1;
        void SyncSelectedDifficulty()
        {
            if (lastDifficultyGroup != songSelection)
            {
                lastDifficultyGroup = songSelection;
                selectedDifficultySlot = 0;
            }
            int count = songSelection < BuiltinSongCount
                ? DuovexSongGroups.DifficultyCount(songSelection) : 1;
            selectedDifficultySlot = Mathf.Clamp(selectedDifficultySlot,0,count-1);
        }
        void MoveDifficulty(int delta)
        {
            if (songSelectForEditing) return;
            SyncSelectedDifficulty();
            int count = songSelection < BuiltinSongCount
                ? DuovexSongGroups.DifficultyCount(songSelection) : 1;
            selectedDifficultySlot = Mathf.Clamp(selectedDifficultySlot+delta,0,count-1);
        }
        // Length is orthogonal to difficulty: song -> difficulty -> SHORT/FULL.
        int selectedLengthSlot; // 0 SHORT (default), 1 FULL
        int lastLengthGroup = -1;
        void SyncSelectedLength()
        {
            if (lastLengthGroup != songSelection)
            {
                lastLengthGroup = songSelection;
                selectedLengthSlot = 0;
            }
            if (songSelection >= 3 && songSelection < BuiltinSongCount)
            {
                int level = DuovexSongGroups.ChartLevel(songSelection,selectedDifficultySlot);
                if (!DuovexBuiltInTracks.HasFullAudio(level)) selectedLengthSlot = 0;
            }
        }
        void SetSongLength(int slot)
        {
            SyncSelectedDifficulty();
            SyncSelectedLength();
            if (songSelectForEditing || songSelection < 3 || songSelection >= BuiltinSongCount) return;
            int level=DuovexSongGroups.ChartLevel(songSelection,selectedDifficultySlot);
            if (slot == 1 && !DuovexBuiltInTracks.HasFullAudio(level)) return;
            selectedLengthSlot=Mathf.Clamp(slot,0,1);
        }
        void MoveSongLength()
        {
            SyncSelectedDifficulty();
            SyncSelectedLength();
            SetSongLength(1 - selectedLengthSlot);
        }
        const int ImportedStartIndex = LocalChartIndex + 1;
        int songSelection;
        int libraryPage;
        bool songSelectForEditing;
        bool loadingAudio;
        string loadingText = "";
        string browserFolder = "";
        string browserPath = "";
        int browserOffset;
        readonly List<string> browserEntries = new List<string>();
        string browserError = "";

        void RefreshSongLibrary()
        {
            try
            {
                importedSongs.Clear();
                importedSongs.AddRange(DuovexSongLibrary.Scan());
                songSelection = Mathf.Clamp(songSelection, 0, importedSongs.Count + ImportedStartIndex - 1);
            }
            catch (Exception ex) { Notify("LIBRARY: " + ex.Message); }
        }

        void OpenSongSelect(bool forEditing)
        {
            OpenSongSelect();
            songSelectForEditing = forEditing;
            // Built-in practice charts are playable but read-only in Chart Studio.
            if (forEditing && songSelection < BuiltinSongCount)
            {
                songSelection = LocalChartIndex;
                libraryPage = Mathf.Max(0, LocalChartIndex - 6);
            }
        }

        void SelectLibraryAction(bool editing)
        {
            if (loadingAudio) return;
            PausePlaying();
            if (songSelection < BuiltinSongCount)
            {
                if (!editing)
                {
                    SyncSelectedDifficulty();
                    SyncSelectedLength();
                    int level = DuovexSongGroups.ChartLevel(songSelection,selectedDifficultySlot);
                    if (level > DuovexBuiltInTracks.SilentPracticeCount)
                        StartFeaturedTrack(level);
                    else StartTutorial(level);
                }
                return;
            }
            if (songSelection == LocalChartIndex)
            {
                activeImported = null;
                song = Resources.Load<AudioClip>("song");
                audioSource.clip = song;
                if (song != null) BuildWaveform(); else { waveReady = false; waveform = null; }
                try { editableChart = ChartStorage.Load(); }
                catch (Exception ex) { Notify("CHART: " + ex.Message); editableChart = null; }
                if (editableChart == null) editableChart = ChartStorage.Sample();
                chart = editableChart;
                songCursor = 0f;
                BuildStates();
                if (editing) EnterEditor();
                else { tutorialActive = false; page = Page.Gameplay; Restart(true); }
                return;
            }
            int i = songSelection - ImportedStartIndex;
            if (i < 0 || i >= importedSongs.Count) return;
            DuovexSongInfo selected = importedSongs[i];
            try
            {
                chart = DuovexSongLibrary.LoadChart(selected);
                editableChart = chart;
                activeImported = selected;
                tutorialActive = false;
                songCursor = 0f;
                BuildStates();
                if (selected.id == importedAudioId && importedAudioClip != null)
                {
                    song = importedAudioClip;
                    audioSource.clip = song;
                    if (editing) EnterEditor();
                    else { page = Page.Gameplay; Restart(true); }
                }
                else StartCoroutine(LoadImportedClip(selected, editing));
            }
            catch (Exception ex) { Notify("OPEN FAILED: " + ex.Message); }
        }

        // Featured music lives in Resources so no manual import is required.
        // It stays read-only in the in-game editor, like other built-in maps.
        void StartFeaturedTrack(int chartLevel)
        {
            bool useFull = selectedLengthSlot == 1 && DuovexBuiltInTracks.HasFullAudio(chartLevel);
            string chartPath=useFull ? DuovexBuiltInTracks.ChartPath(chartLevel)
                : DuovexBuiltInTracks.ShortChartPath(chartLevel);
            TextAsset chartAsset=Resources.Load<TextAsset>(chartPath);
            SongChart featuredChart = null;
            if (chartAsset != null)
            {
                try
                {
                    featuredChart=JsonUtility.FromJson<SongChart>(chartAsset.text);
                    if (featuredChart == null || featuredChart.notes == null || featuredChart.notes.Count == 0)
                        featuredChart=null;
                    else featuredChart.Sort();
                }
                catch (Exception e) { Debug.LogError("Invalid built-in " + chartPath + ": " + e.Message); }
            }
            string audioPath=useFull ? DuovexBuiltInTracks.AudioPath(chartLevel)
                : DuovexBuiltInTracks.ShortAudioPath(chartLevel);
            AudioClip featuredAudio = string.IsNullOrEmpty(audioPath) ? null : Resources.Load<AudioClip>(audioPath);
            if (featuredChart == null || featuredAudio == null)
            {
                Notify("BUILT-IN TRACK: CHART OR AUDIO MISSING (" + chartLevel + ")");
                return;
            }
            tutorialActive = false;
            activeImported = null;
            chart = featuredChart;
            song = featuredAudio;
            audioSource.clip = song;
            songCursor = 0f;
            page = Page.Gameplay;
            Restart(true);
        }

        IEnumerator LoadImportedClip(DuovexSongInfo entry, bool editing)
        {
            loadingAudio = true;
            loadingText = "LOADING AUDIO";
            song = null;
            audioSource.clip = null;
            if (importedAudioClip != null) { Destroy(importedAudioClip); importedAudioClip = null; }
            importedAudioId = null;
            string path;
            try { path = DuovexSongLibrary.AudioPath(entry); }
            catch (Exception ex)
            {
                Notify("AUDIO: " + ex.Message);
                loadingAudio = false;
                yield break;
            }
            string ext = Path.GetExtension(path).ToLowerInvariant();
            AudioType type = ext == ".wav" ? AudioType.WAV : ext == ".mp3" ? AudioType.MPEG : AudioType.OGGVORBIS;
            // Uri handles spaces, Korean filenames, and special characters in file paths.
            using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, type))
            {
                yield return request.SendWebRequest();
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Notify("AUDIO LOAD FAILED: " + request.error);
                    activeImported = null;
                }
                else
                {
                    importedAudioClip = DownloadHandlerAudioClip.GetContent(request);
                    if (importedAudioClip == null)
                    {
                        Notify("AUDIO FORMAT NOT SUPPORTED");
                        activeImported = null;
                    }
                    else
                    {
                        importedAudioId = entry.id;
                        song = importedAudioClip;
                        audioSource.clip = song;
                        BuildWaveform();
                        if (editing) EnterEditor();
                        else { page = Page.Gameplay; Restart(true); }
                    }
                }
            }
            loadingAudio = false;
            loadingText = "";
        }

        void StartFileBrowser()
        {
            PausePlaying();
            string music = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            browserFolder = Directory.Exists(music) ? music : home;
            browserOffset = 0;
            browserError = "";
            MoveBrowserTo(browserFolder);
            page = Page.FileBrowser;
        }

        void MoveBrowserTo(string folder)
        {
            try
            {
                folder = Path.GetFullPath(folder);
                if (!Directory.Exists(folder)) throw new DirectoryNotFoundException("Folder not found");
                browserFolder = folder;
                browserPath = folder;
                browserOffset = 0;
                browserError = "";
                browserEntries.Clear();
                string[] folders = Directory.GetDirectories(browserFolder);
                string[] tracks = Directory.GetFiles(browserFolder);
                Array.Sort(folders, StringComparer.OrdinalIgnoreCase);
                Array.Sort(tracks, StringComparer.OrdinalIgnoreCase);
                browserEntries.AddRange(folders);
                foreach (string track in tracks)
                {
                    string ext = Path.GetExtension(track).ToLowerInvariant();
                    if (ext == ".ogg" || ext == ".wav" || ext == ".mp3") browserEntries.Add(track);
                }
            }
            catch (Exception ex) { browserError = ex.Message; }
        }

        void ImportBrowserFile(string path)
        {
            try
            {
                DuovexSongInfo newSong = DuovexSongLibrary.Import(Path.GetFullPath(path));
                RefreshSongLibrary();
                int i = importedSongs.FindIndex(x => x.id == newSong.id);
                songSelection = i + ImportedStartIndex;
                browserError = "";
                page = Page.SongSelect;
                // The newly imported song opens in the editor, ready for note placement.
                SelectLibraryAction(true);
            }
            catch (Exception ex) { browserError = ex.Message; }
        }

    }
}
