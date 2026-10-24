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
            new SongGroup("LIGHT IT UP", "128 BPM", 3, new[]{19,4,5}, new[]{"EASY  Lv.3","NORMAL  Lv.8","HARD  Lv.15"}),
            new SongGroup("KETSUDAN", "195 BPM", 5, new[]{20,15,16}, new[]{"EASY  Lv.4","NORMAL  Lv.9","HARD  Lv.15"}),
            new SongGroup("EUPHORIANIC", "140 BPM", 6, new[]{21,7,8}, new[]{"EASY  Lv.2","NORMAL  Lv.6","HARD  Lv.13"}),
            new SongGroup("DESAPARECER", "174 BPM", 8, new[]{22,9,10}, new[]{"EASY  Lv.3","NORMAL  Lv.8","HARD  Lv.16"}),
            new SongGroup("OVERWRITE", "180 BPM", 10, new[]{23,11,12}, new[]{"EASY  Lv.3","NORMAL  Lv.8","HARD  Lv.14"}),
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
