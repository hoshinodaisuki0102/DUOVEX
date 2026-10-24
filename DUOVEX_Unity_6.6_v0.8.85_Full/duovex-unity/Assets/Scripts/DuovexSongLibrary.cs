using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace DualSync
{
    [Serializable]
    public sealed class DuovexSongInfo
    {
        public string id;
        public string title;
        public string audioFile;
    }

    /// <summary>
    /// User-imported songs live outside Assets. One folder per song under
    /// Application.persistentDataPath/Songs. The tutorial and legacy chart are untouched.
    /// </summary>
    public static class DuovexSongLibrary
    {
        public static string Root => Path.Combine(Application.persistentDataPath, "Songs");
        static string SongFolder(DuovexSongInfo song)
        {
            // Never construct a persistent path from untrusted song title / audio name.
            Guid parsed;
            if (song == null || !Guid.TryParseExact(song.id, "N", out parsed))
                throw new InvalidDataException("Invalid song ID");
            return Path.Combine(Root, song.id);
        }
        public static string AudioPath(DuovexSongInfo song)
        {
            string name = Path.GetFileName(song.audioFile ?? "");
            if (string.IsNullOrEmpty(name) || name != song.audioFile)
                throw new InvalidDataException("Invalid audio file name");
            return Path.Combine(SongFolder(song), name);
        }
        static string ChartPath(DuovexSongInfo song) => Path.Combine(SongFolder(song), "chart.json");
        static string InfoPath(DuovexSongInfo song) => Path.Combine(SongFolder(song), "song.json");

        public static List<DuovexSongInfo> Scan()
        {
            Directory.CreateDirectory(Root);
            var songs = new List<DuovexSongInfo>();
            foreach (string folder in Directory.GetDirectories(Root))
            {
                string infoPath = Path.Combine(folder, "song.json");
                if (!File.Exists(infoPath)) continue;
                try
                {
                    var song = JsonUtility.FromJson<DuovexSongInfo>(File.ReadAllText(infoPath));
                    if (song == null || Path.GetFullPath(SongFolder(song)) != Path.GetFullPath(folder)) continue;
                    if (!File.Exists(AudioPath(song))) continue;
                    songs.Add(song);
                }
                catch (Exception exception) { Debug.LogWarning("Skipped song: " + exception.Message); }
            }
            songs.Sort((a, b) => string.Compare(a.title, b.title, StringComparison.OrdinalIgnoreCase));
            return songs;
        }

        public static DuovexSongInfo Import(string sourceFile)
        {
            string ext = Path.GetExtension(sourceFile).ToLowerInvariant();
            if (ext != ".ogg" && ext != ".wav" && ext != ".mp3")
                throw new InvalidDataException("Choose an OGG, WAV or MP3 audio file");
            if (!File.Exists(sourceFile)) throw new FileNotFoundException("Audio file not found", sourceFile);
            var original = new FileInfo(sourceFile);
            if (original.Length <= 0 || original.Length > 250L * 1024L * 1024L)
                throw new InvalidDataException("Audio must be between 1 byte and 250 MB");
            var song = new DuovexSongInfo
            {
                id = Guid.NewGuid().ToString("N"),
                title = Path.GetFileNameWithoutExtension(sourceFile),
                audioFile = "audio" + ext
            };
            string folder = SongFolder(song);
            Directory.CreateDirectory(folder);
            try
            {
                File.Copy(sourceFile, AudioPath(song), false);
                var empty = new SongChart { title = song.title, bpm = 120f, tickDivision = 4 };
                File.WriteAllText(ChartPath(song), JsonUtility.ToJson(empty, true));
                File.WriteAllText(InfoPath(song), JsonUtility.ToJson(song, true));
            }
            catch
            {
                try { Directory.Delete(folder, true); } catch { }
                throw;
            }
            return song;
        }

        public static SongChart LoadChart(DuovexSongInfo song)
        {
            string path = ChartPath(song);
            SongChart chart = File.Exists(path)
                ? JsonUtility.FromJson<SongChart>(File.ReadAllText(path))
                : new SongChart { title = song.title, bpm = 120f, tickDivision = 4 };
            if (chart == null) throw new InvalidDataException("Chart JSON is invalid");
            if (chart.notes == null) chart.notes = new List<ChartNote>();
            chart.bpm = Mathf.Clamp(chart.bpm, 30f, 350f);
            chart.tickDivision = Mathf.Clamp(chart.tickDivision, 1, 32);
            chart.Sort();
            return chart;
        }
        public static void SaveChart(DuovexSongInfo song, SongChart chart)
        {
            if (song == null || chart == null) throw new ArgumentNullException("Song or chart");
            chart.Sort();
            string target = ChartPath(song);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            string tmp = target + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(chart, true));
            if (File.Exists(target)) File.Delete(target);
            File.Move(tmp, target);
        }
    }
}
