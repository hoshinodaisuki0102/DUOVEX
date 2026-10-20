using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace DualSync
{
    [Serializable]
    public sealed class ChartNote
    {
        public int id;
        public float beat;
        // 0 = D/white-left; 1 = F/white-center; 2 = J/black-center; 3 = K/black-right
        public int input;
        // 0 = tap; positive = hold, in beats
        public float durationBeats;

        public ChartNote Clone() => new ChartNote { id = id, beat = beat, input = input, durationBeats = durationBeats };
    }

    [Serializable]
    public sealed class SongChart
    {
        public int schemaVersion = 1;
        public string title = "NEW TRACK";
        public float bpm = 120f;
        public float offsetMs = 0f;
        public int tickDivision = 4;
        public List<ChartNote> notes = new List<ChartNote>();

        public float BeatToSeconds(float beat) => (60f * beat / Mathf.Max(1f, bpm)) + offsetMs / 1000f;
        public float SecondsToBeat(float seconds) => (seconds - offsetMs / 1000f) * Mathf.Max(1f, bpm) / 60f;
        public int NewId()
        {
            int max = 0;
            foreach (var n in notes) if (n.id > max) max = n.id;
            return max + 1;
        }

        public void Sort() => notes.Sort((a, b) =>
        {
            int order = a.beat.CompareTo(b.beat);
            return order == 0 ? a.input.CompareTo(b.input) : order;
        });
    }

    public static class ChartStorage
    {
        public static string FilePath => Path.Combine(Application.persistentDataPath, "DualSyncChart.json");

        public static void Save(SongChart chart)
        {
            chart.Sort();
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(FilePath, JsonUtility.ToJson(chart, true));
        }

        public static SongChart Load()
        {
            if (!File.Exists(FilePath)) return null;
            SongChart chart = JsonUtility.FromJson<SongChart>(File.ReadAllText(FilePath));
            if (chart == null) return null;
            if (chart.notes == null) chart.notes = new List<ChartNote>();
            chart.bpm = Mathf.Clamp(chart.bpm, 30f, 350f);
            chart.tickDivision = Mathf.Clamp(chart.tickDivision, 1, 32);
            chart.Sort();
            return chart;
        }

        /// <summary>
        /// Built-in read-only tutorial. This chart never touches the user's saved JSON.
        /// Steps: white -> black -> shared lane -> dual -> holds -> finish.
        /// At 110 BPM the final note lands around 48 seconds.
        /// </summary>
        public static SongChart Tutorial()
        {
            // Editable chart asset: Assets/Resources/Charts/Tutorial.json
            // Kept separate from ChartStorage.FilePath so the editor cannot overwrite it.
            TextAsset asset = Resources.Load<TextAsset>("Charts/Tutorial");
            if (asset != null)
            {
                try
                {
                    SongChart loaded = JsonUtility.FromJson<SongChart>(asset.text);
                    if (loaded != null && loaded.notes != null && loaded.notes.Count > 0)
                    {
                        loaded.Sort();
                        return loaded;
                    }
                }
                catch (Exception e) { Debug.LogWarning("Tutorial chart JSON invalid: " + e.Message); }
            }
            // Built-in fallback if the JSON is missing or invalid.
            var c = new SongChart { title = "튜토리얼", bpm = 110f, tickDivision = 4 };
            int id = 1;
            void Tap(float b, int key) => c.notes.Add(new ChartNote
                { id = id++, beat = b, input = key, durationBeats = 0f });
            void Hold(float b, int key, float length) => c.notes.Add(new ChartNote
                { id = id++, beat = b, input = key, durationBeats = length });
            void Dual(float b) { Tap(b, 1); Tap(b, 2); }
            void DualHold(float b, float length) { Hold(b, 1, length); Hold(b, 2, length); }

            // 01: White notes (D, F) with generous gaps, then a small alternation.
            Tap(4, 0); Tap(6, 1); Tap(8, 0); Tap(10, 1);
            Tap(12, 0); Tap(13, 1); Tap(14, 0); Tap(15, 1);
            // 02: Black notes (J, K).
            Tap(20, 2); Tap(22, 3); Tap(24, 2); Tap(26, 3);
            Tap(28, 2); Tap(29, 3); Tap(30, 2); Tap(31, 3);
            // 03: Both colors share the second physical lane.
            Tap(36, 1); Tap(38, 2); Tap(40, 1); Tap(42, 2);
            Tap(44, 1); Tap(45, 2); Tap(46, 1); Tap(47, 2);
            // 04: Same-time F+J is a single diagonal visual; hits count separately.
            Dual(52); Dual(56); Dual(60); Dual(62);
            Tap(64, 0); Tap(64, 3);
            // 05: Holds score independent ticks, with no release timing judgment.
            Hold(68, 0, 4); Hold(74, 3, 4); DualHold(80, 4);
            // 06: Short closing phrase.
            Tap(86, 0); Tap(86, 3); Dual(88);
            c.Sort();
            return c;
        }

        // Built-in playable levels. LEVEL 1 reuses the original tutorial, unchanged.
        // Additional levels are read-only chart assets and cannot overwrite saved songs.
        public static SongChart PracticeLevel(int level)
        {
            if (level <= 1) return Tutorial();
            if (level > 5) return null;
            string path = level == 2 ? "Charts/Level2" :
                level == 3 ? "Charts/Level3" :
                level == 4 ? "Charts/LightItUpNormal" : "Charts/LightItUpHard";
            TextAsset asset = Resources.Load<TextAsset>(path);
            if (asset == null)
            {
                Debug.LogError("Missing Duovex chart asset: " + path);
                return null;
            }
            try
            {
                var loaded = JsonUtility.FromJson<SongChart>(asset.text);
                if (loaded == null || loaded.notes == null || loaded.notes.Count == 0)
                    throw new InvalidDataException("Empty chart data");
                loaded.bpm = Mathf.Clamp(loaded.bpm, 30f, 350f);
                loaded.tickDivision = Mathf.Clamp(loaded.tickDivision, 1, 32);
                loaded.Sort();
                return loaded;
            }
            catch (Exception e)
            {
                Debug.LogError("Invalid level " + level + ": " + e.Message);
                return null;
            }
        }

        public static SongChart Sample()
        {
            var c = new SongChart { title = "DEMO / SHARE THE LINE", bpm = 120f, tickDivision = 4 };
            int id = 1;
            void Tap(float b, int key) => c.notes.Add(new ChartNote { id = id++, beat = b, input = key });
            void Hold(float b, int key, float dur) => c.notes.Add(new ChartNote { id = id++, beat = b, input = key, durationBeats = dur });
            Tap(4, 0); Tap(5, 1); Tap(6, 2); Tap(7, 3);
            Tap(8, 1); Tap(8, 2); Tap(9, 0); Tap(9.5f, 3);
            Tap(10, 1); Tap(10.5f, 2); Tap(11, 0); Tap(11, 3);
            Hold(12, 0, 2); Tap(12.5f, 2); Tap(13, 3); Tap(13.5f, 1);
            Hold(15, 1, 3); Hold(15, 2, 3);
            Tap(19, 0); Tap(19.5f, 1); Tap(20, 2); Tap(20.5f, 3);
            Tap(21, 1); Tap(21, 2); Tap(22, 0); Tap(22, 3);
            Hold(24, 2, 3); Tap(24, 0); Tap(25, 1); Tap(26, 0);
            Tap(28, 1); Tap(28, 2); Tap(29, 0); Tap(29, 3);
            Tap(30, 1); Tap(30.5f, 2); Tap(31, 1); Tap(31.5f, 2);
            Tap(32, 0); Tap(32, 3);
            c.Sort();
            return c;
        }
    }
}
