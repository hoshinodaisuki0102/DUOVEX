using System;
namespace DualSync
{
    /// <summary>
    /// Built-in song selection groups. Chart levels remain 1-based legacy IDs,
    /// so resource loading, replay and older score IDs stay unchanged.
    /// Import/local tracks still have a single user-editable chart.
    /// </summary>
    public static class DuovexSongGroups
    {
        public sealed class SongGroup
        {
            public readonly string Title, Meta;
            public readonly int Cover;
            public readonly int[] ChartLevels;
            public readonly string[] DifficultyLabels;
            public SongGroup(string title, string meta, int cover, int[] levels, string[] labels)
            { Title=title; Meta=meta; Cover=cover; ChartLevels=levels; DifficultyLabels=labels; }
        }
        public static readonly SongGroup[] Builtins = {
            new SongGroup("LEVEL 1 / TUTORIAL", "110 BPM  /  SILENT", 0, new[]{1}, new[]{"PRACTICE  1"}),
            new SongGroup("LEVEL 2 / INTERLOCK", "125 BPM  /  SILENT", 1, new[]{2}, new[]{"PRACTICE  2"}),
            new SongGroup("LEVEL 3 / OVERDRIVE", "150 BPM  /  SILENT", 2, new[]{3}, new[]{"PRACTICE  3"}),
            new SongGroup("LIGHT IT UP", "128 BPM", 3, new[]{4,5,14}, new[]{"NORMAL  Lv.5","HARD  Lv.15","BOSS  Lv.19"}),
            new SongGroup("KETSUDAN", "BOSS CHART", 5, new[]{15,16,6}, new[]{"NORMAL  Lv.7","HARD  Lv.15","BOSS  Lv.20"}),
            new SongGroup("EUPHORIANIC", "140 BPM", 6, new[]{7,8,13}, new[]{"NORMAL  Lv.4","HARD  Lv.13","BOSS  Lv.18"}),
            new SongGroup("DESAPARECER", "174 BPM", 8, new[]{9,10,17}, new[]{"NORMAL  Lv.6","HARD  Lv.16","BOSS  Lv.20"}),
            new SongGroup("OVERWRITE", "180 BPM", 10, new[]{11,12,18}, new[]{"NORMAL  Lv.6","HARD  Lv.14","BOSS  Lv.19"}),
        };
        public static int Count => Builtins.Length;
        public static int ChartLevel(int songGroup, int difficulty)
        {
            if (songGroup < 0 || songGroup >= Count) return -1;
            var levels=Builtins[songGroup].ChartLevels;
            return levels[Math.Max(0,Math.Min(levels.Length-1,difficulty))];
        }
        public static int DifficultyCount(int songGroup) =>
            songGroup >= 0 && songGroup < Count ? Builtins[songGroup].ChartLevels.Length : 1;
        public static int CoverIndex(int songGroup) => songGroup >= 0 && songGroup < Count ? Builtins[songGroup].Cover : 12;
    }
}
