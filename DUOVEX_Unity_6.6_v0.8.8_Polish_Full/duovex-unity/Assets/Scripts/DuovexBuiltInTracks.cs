using UnityEngine;

namespace DualSync
{
    /// <summary>
    /// Read-only bundled charts. Indices are contiguous and are reused by the
    /// song picker. The local/imported library lives after the built-in entries.
    /// Keep old chart levels 1–5 unchanged so existing maps retain their IDs.
    /// </summary>
    public static class DuovexBuiltInTracks
    {
        public const int Count = 18;
        public const int SilentPracticeCount = 3;
        static readonly string[] Paths = {
            "Charts/Tutorial", "Charts/Level2", "Charts/Level3",
            "Charts/LightItUpNormal", "Charts/LightItUpHard",
            "Charts/BossMoment",
            "Charts/EuphorianicNormal", "Charts/EuphorianicHard",
            "Charts/DesaparecerNormal", "Charts/DesaparecerHard",
            "Charts/OverwriteNormal", "Charts/OverwriteHard",
            "Charts/EuphorianicBoss",
            "Charts/LightItUpBoss", "Charts/KetsudanNormal", "Charts/KetsudanHard",
            "Charts/DesaparecerBoss", "Charts/OverwriteBoss"
        };
        static readonly string[] AudioPaths = {
            null, null, null,
            "Tracks/LightItUp", "Tracks/LightItUp",
            "BossMoment",
            "Tracks/Euphorianic", "Tracks/Euphorianic",
            "Tracks/Desaparecer", "Tracks/Desaparecer",
            "Tracks/Overwrite", "Tracks/Overwrite",
            "Tracks/Euphorianic",
            "Tracks/LightItUp", "BossMoment", "BossMoment",
            "Tracks/Desaparecer", "Tracks/Overwrite"
        };
        static readonly string[] Titles = {
            "LV.1  TUTORIAL", "LV.2  INTERLOCK", "LV.3  OVERDRIVE",
            "LIGHT IT UP / NORMAL", "LIGHT IT UP / HARD",
            "BOSS / KETSUDAN",
            "EUPHORIANIC / NORMAL", "EUPHORIANIC / HARD",
            "DESAPARECER / NORMAL", "DESAPARECER / HARD",
            "OVERWRITE / NORMAL", "OVERWRITE / HARD",
            "EUPHORIANIC / BOSS",
            "LIGHT IT UP / BOSS", "KETSUDAN / NORMAL", "KETSUDAN / HARD",
            "DESAPARECER / BOSS", "OVERWRITE / BOSS"
        };
        static readonly string[] Metadata = {
            "BPM 110 / SILENT", "BPM 125 / SILENT", "BPM 150 / SILENT",
            "BPM 128 / NORMAL", "BPM 128 / HARD",
            "BPM 193-198 / BOSS / TIMING MAP",
            "BPM 140 / NORMAL", "BPM 140 / HARD",
            "BPM 174 / NORMAL", "BPM 174 / HARD",
            "BPM 180 / NORMAL", "BPM 180 / HARD",
            "BPM 140 / BOSS",
            "BPM 128 / BOSS", "BPM 195 / NORMAL", "BPM 195 / HARD",
            "BPM 174 / BOSS", "BPM 180 / BOSS"
        };
        // Euphorianic source is already an official 69-second SHORT CUT.
        // Never label that file as a full recording; full-length audio has not been supplied.
        public static bool HasFullAudio(int level) => IsValid(level) &&
            level > SilentPracticeCount && level != 7 && level != 8 && level != 13;
        public static string ShortAudioPath(int level)
        {
            switch (level)
            {
                case 4: case 5: case 14: return "Tracks/Short/LightItUp";
                case 6: case 15: case 16: return "Tracks/Short/BossMoment";
                case 9: case 10: case 17: return "Tracks/Short/Desaparecer";
                case 11: case 12: case 18: return "Tracks/Short/Overwrite";
                // Existing supplied Euphorianic audio is itself a short cut.
                case 7: case 8: case 13: return "Tracks/Euphorianic";
                default: return AudioPath(level);
            }
        }
        public static string ShortChartPath(int level)
        {
            string path = ChartPath(level);
            if (path == null) return null;
            if (!HasFullAudio(level)) return path;
            return "Charts/Short/" + path.Substring("Charts/".Length);
        }
        public static bool IsValid(int oneBasedLevel) => oneBasedLevel >= 1 && oneBasedLevel <= Count;
        public static string ChartPath(int level) => IsValid(level) ? Paths[level-1] : null;
        public static string AudioPath(int level) => IsValid(level) ? AudioPaths[level-1] : null;
        public static string DisplayTitle(int index) => index>=0 && index<Count ? Titles[index] : "UNKNOWN";
        public static string DisplayMeta(int index) => index>=0 && index<Count ? Metadata[index] : "UNKNOWN";
    }
}
