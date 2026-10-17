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
                songSelection = Mathf.Clamp(songSelection, 0, importedSongs.Count + 1);
            }
            catch (Exception ex) { Notify("LIBRARY: " + ex.Message); }
        }

        void OpenSongSelect(bool forEditing)
        {
            OpenSongSelect();
            songSelectForEditing = forEditing;
        }

        void SelectLibraryAction(bool editing)
        {
            if (loadingAudio) return;
            PausePlaying();
            if (songSelection == 0)
            {
                if (!editing) StartTutorial();
                return;
            }
            if (songSelection == 1)
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
            int i = songSelection - 2;
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
                songSelection = i + 2;
                browserError = "";
                page = Page.SongSelect;
                // The newly imported song opens in the editor, ready for note placement.
                SelectLibraryAction(true);
            }
            catch (Exception ex) { browserError = ex.Message; }
        }

        void DrawFileBrowser()
        {
            Label(new Rect(68, 76, 520, 47), "IMPORT AUDIO", titleStyle);
            Panel(new Rect(65, 145, 1150, 490));
            Label(new Rect(92, 168, 600, 22), "OGG   /   WAV   /   MP3", tinyStyle);
            browserPath = GUI.TextField(new Rect(91, 207, 831, 40), browserPath ?? "");
            if (EditorButton(new Rect(936, 207, 111, 40), "GO"))
            {
                if (File.Exists(browserPath)) ImportBrowserFile(browserPath);
                else MoveBrowserTo(browserPath);
            }
            if (EditorButton(new Rect(1057, 207, 124, 40), "CANCEL")) { page = Page.SongSelect; return; }
            if (EditorButton(new Rect(91, 259, 128, 33), "UP"))
            {
                string p = Path.GetDirectoryName(browserFolder);
                if (!string.IsNullOrEmpty(p)) MoveBrowserTo(p);
            }
            if (EditorButton(new Rect(232, 259, 159, 33), "MUSIC"))
                MoveBrowserTo(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic));
            if (EditorButton(new Rect(404, 259, 159, 33), "DOWNLOADS"))
                MoveBrowserTo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"));
            if (EditorButton(new Rect(575, 259, 143, 33), "HOME"))
                MoveBrowserTo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
            List<string> entries = browserEntries;
            int maxOffset = Mathf.Max(0, entries.Count - 7);
            browserOffset = Mathf.Clamp(browserOffset, 0, maxOffset);
            for (int row = 0; row < 7 && browserOffset + row < entries.Count; row++)
            {
                string file = entries[browserOffset + row];
                bool directory = Directory.Exists(file);
                string display = (directory ? "[DIR]   " : "[AUDIO]  ") + Path.GetFileName(file);
                if (display.Length > 95) display = display.Substring(0, 92) + "...";
                if (EditorButton(new Rect(92, 306 + row * 40, 895, 34), display))
                {
                    if (directory) MoveBrowserTo(file);
                    else { browserPath = file; ImportBrowserFile(file); }
                    return;
                }
            }
            if (EditorButton(new Rect(1011, 333, 157, 53), "PREV")) browserOffset = Mathf.Max(0, browserOffset - 7);
            if (EditorButton(new Rect(1011, 395, 157, 53), "NEXT")) browserOffset = Mathf.Min(maxOffset, browserOffset + 7);
            if (EditorButton(new Rect(1011, 466, 157, 53), "IMPORT PATH", true))
                ImportBrowserFile(browserPath);
            Label(new Rect(1011, 540, 156, 22), (browserOffset + 1) + " / " + Mathf.Max(1, entries.Count), tinyStyle);
            if (!string.IsNullOrEmpty(browserError))
                Label(new Rect(92, 599, 1095, 26), browserError, bodyStyle);
        }

        void DrawLibrarySongSelect()
        {
            Label(new Rect(67, 79, 560, 48), songSelectForEditing ? "CHART STUDIO" : "SONG SELECT", titleStyle);
            Panel(new Rect(66, 157, 713, 478));
            Panel(new Rect(799, 157, 414, 478));
            int total = importedSongs.Count + 2;
            const int visible = 7;
            libraryPage = Mathf.Clamp(libraryPage, 0, Mathf.Max(0, total - visible));
            if (songSelection < libraryPage) libraryPage = songSelection;
            if (songSelection >= libraryPage + visible) libraryPage = songSelection - visible + 1;
            for (int row = 0; row < visible && row + libraryPage < total; row++)
            {
                int index = row + libraryPage;
                string label = index == 0 ? "TUTORIAL" : index == 1 ? "LOCAL CHART" : importedSongs[index - 2].title;
                if (label.Length > 53) label = label.Substring(0, 50) + "...";
                Rect r = new Rect(87, 183 + row * 57, 668, 50);
                bool selected = index == songSelection;
                Fill(r, selected ? new Color(.10f, .29f, .40f) : new Color(.061f, .092f, .15f));
                Fill(new Rect(r.x, r.y, 4, r.height), selected ? Cyan : new Color(.19f, .26f, .40f));
                Label(new Rect(r.x + 23, r.y + 9, 595, 33), label, selected ? subtitleStyle : bodyStyle);
                if (GUI.Button(r, "", GUIStyle.none)) songSelection = index;
            }
            if (EditorButton(new Rect(96, 595, 130, 29), "UP")) libraryPage = Mathf.Max(0, libraryPage - 1);
            if (EditorButton(new Rect(239, 595, 130, 29), "DOWN")) libraryPage = Mathf.Min(Mathf.Max(0, total - visible), libraryPage + 1);
            string chosen = songSelection == 0 ? "TUTORIAL" : songSelection == 1 ? "LOCAL CHART" :
                importedSongs[Mathf.Clamp(songSelection - 2, 0, Mathf.Max(0, importedSongs.Count - 1))].title;
            var cTitle = new GUIStyle(titleStyle) { fontSize = 28, wordWrap = true };
            Label(new Rect(823, 199, 365, 100), chosen, cTitle);
            string meta = songSelection == 0 ? "BPM 110   /   SILENT" :
                songSelection == 1 ? "LEGACY CHART" : "IMPORTED TRACK";
            Label(new Rect(826, 301, 349, 25), meta, editorMonoStyle);
            if (songSelection == 0 && songSelectForEditing)
                Label(new Rect(826, 344, 351, 29), "READ-ONLY TUTORIAL", bodyStyle);
            bool canOpen = !loadingAudio && !(songSelection == 0 && songSelectForEditing);
            if (canOpen && NeonButton(new Rect(824, 392, 362, 57), songSelectForEditing ? "EDIT CHART" : "PLAY", true))
                SelectLibraryAction(songSelectForEditing);
            if (NeonButton(new Rect(824, 468, 362, 55), "ADD SONG", false)) StartFileBrowser();
            if (NeonButton(new Rect(824, 544, 362, 56), "BACK", false)) OpenMainMenu();
            if (loadingAudio)
            {
                Fill(new Rect(65, 625, 1148, 3), Cyan);
                Label(new Rect(826, 602, 365, 26), loadingText, tinyStyle);
            }
            else if (Time.unscaledTime < statusVisibleUntil)
                Label(new Rect(826, 603, 363, 25), status, tinyStyle);
        }

        void DrawMainMenuV05()
        {
            float t = Time.unscaledTime;
            // An abstract moving three-lane instrument. All motion is unscaled,
            // independent of song playback and frame rate.
            Texture(new Rect(35 + Mathf.Sin(t * .36f) * 65f, 88, 780, 570), glow,
                new Color(.13f, .79f, 1f, .16f));
            Texture(new Rect(815 + Mathf.Cos(t * .28f) * 45f, 50, 470, 560), glow,
                new Color(.67f, .36f, .95f, .15f));
            for (int i = 0; i < 10; i++)
            {
                float x = (i * 179f + t * (9f + i * 1.8f)) % 1320f;
                float y = 100 + i * 58f;
                Fill(new Rect(x, y, 24 + (i % 3) * 19, 1),
                    new Color(.31f, .65f, 1f, .08f + (i % 3) * .03f));
            }
            for (int lane = 0; lane < 3; lane++)
            {
                float x = 716 + lane * 103;
                Fill(new Rect(x, 96, 94, 503), lane == 1 ?
                    new Color(.090f, .100f, .19f, .82f) : new Color(.047f, .080f, .14f, .81f));
                Fill(new Rect(x, 96, 2, 503), lane == 1 ? Violet * .62f : Cyan * .35f);
                for (int j = 0; j < 4; j++)
                {
                    float speed = 55f + j * 16f;
                    float nY = 100f + ((j * 147f + lane * 79f + t * speed) % 468f);
                    Rect note = new Rect(x + 10, nY, 74, 8);
                    bool white = (lane == 0 || (lane == 1 && j % 2 == 0));
                    Fill(note, white ? WhiteNote : BlackNote);
                    Border(note, white ? Cyan : Violet);
                    if (lane == 1 && j == 2) Texture(note, diagonal, Color.white);
                }
            }
            Fill(new Rect(712, 601, 315, 4), new Color(.95f, .98f, 1f));
            Fill(new Rect(712, 605, 315, 3), Cyan * .45f);
            float scan = 101f + (t * 77f) % 488f;
            Fill(new Rect(712, scan, 315, 2), new Color(.46f, .84f, 1f, .20f));
            var big = new GUIStyle(heroStyle) { fontSize = 91 };
            Label(new Rect(88, 197, 590, 125), "DUOVEX", big);
            Fill(new Rect(100, 317, 470, 3), new Color(.32f, .40f, .55f));
            float pulse = .5f + .5f * Mathf.Sin(t * 1.4f);
            Fill(new Rect(100, 317, 222 + 40f * pulse, 3), Cyan);
            Fill(new Rect(362, 317, 208 - 40f * pulse, 3), Violet);
            if (NeonButton(new Rect(104, 387, 352, 62), "PLAY", true)) OpenSongSelect(false);
            if (NeonButton(new Rect(104, 463, 352, 62), "CHART STUDIO", false)) OpenSongSelect(true);
            if (NeonButton(new Rect(104, 539, 352, 62), "OPTIONS", false)) OpenOptions();
            // A subtle progress indicator rather than a paragraph of menu help.
            Fill(new Rect(106, 633, 350, 1), new Color(.24f, .34f, .54f, .5f));
            Fill(new Rect(106 + (t * 80f) % 310f, 632, 36, 3), Cyan);
        }
    }
}
