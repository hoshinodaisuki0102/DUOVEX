#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DualSync.Editor
{
    /// <summary>
    /// Lightweight, non-mutating checks that can run before the Test Framework
    /// is introduced. Does not replace a real Unity Play Mode test.
    /// </summary>
    public static class DuovexBaselineCheck
    {
        const string Root = "Assets/Resources/Charts/";
        const string ThemePath = "Assets/Resources/Visual/DuovexTheme.asset";
        static int errors;
        static int warnings;

        [MenuItem("Tools/Duovex/Run v0.7.5 Baseline Check")]
        public static void Run()
        {
            errors = 0;
            warnings = 0;
            CheckChart(Root + "Tutorial.json");
            CheckChart(Root + "Level2.json");
            CheckChart(Root + "Level3.json");
            CheckChart(Root + "LightItUp.json");
            CheckChart(Root + "LightItUpNormal.json");
            CheckChart(Root + "LightItUpHard.json");
            if (AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Resources/Tracks/LightItUp.mp3") == null)
                Error("Missing or unimported featured audio: Assets/Resources/Tracks/LightItUp.mp3");
            CheckThemeAndScene();

            string report = "Errors: " + errors + "  Warnings: " + warnings +
                "\nSee the Console for individual details.\n\n" +
                "Next: enter Play Mode, test all levels, DUAL/HOLD, song import, " +
                "chart save/load, and pause/retry. This checker cannot test timing accuracy.";
            if (errors == 0) Debug.Log("DUOVEX v0.7.5 baseline checks completed. " + report);
            else Debug.LogError("DUOVEX v0.7.5 baseline checks failed. " + report);
            EditorUtility.DisplayDialog("DUOVEX v0.7.5 Baseline Check", report, "OK");
        }

        static void Error(string message)
        {
            errors++;
            Debug.LogError("[DUOVEX Baseline] " + message);
        }

        static void Warn(string message)
        {
            warnings++;
            Debug.LogWarning("[DUOVEX Baseline] " + message);
        }

        static void CheckChart(string path)
        {
            TextAsset file = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (file == null) { Error("Missing chart: " + path); return; }

            SongChart chart;
            try { chart = JsonUtility.FromJson<SongChart>(file.text); }
            catch (Exception e) { Error("Cannot parse " + path + ": " + e.Message); return; }
            if (chart == null || chart.notes == null || chart.notes.Count == 0)
            {
                Error("Chart is empty or invalid: " + path);
                return;
            }
            if (chart.schemaVersion != 1)
                Error("Unsupported chart schema: " + path);
            if (chart.bpm < 30f || chart.bpm > 350f || float.IsNaN(chart.bpm) || float.IsInfinity(chart.bpm))
                Error("Invalid BPM: " + path);
            if (chart.tickDivision < 1 || chart.tickDivision > 32)
                Error("Invalid tick division: " + path);

            var ids = new HashSet<int>();
            int dualPairs = 0;
            var centers = new HashSet<float>();
            foreach (ChartNote note in chart.notes)
            {
                if (note == null) { Error("Null note: " + path); continue; }
                if (!ids.Add(note.id)) Error("Duplicate note ID " + note.id + ": " + path);
                if (note.id <= 0 || note.input < 0 || note.input > 3 ||
                    float.IsNaN(note.beat) || float.IsInfinity(note.beat) ||
                    float.IsNaN(note.durationBeats) || float.IsInfinity(note.durationBeats) ||
                    note.beat < 0f || note.durationBeats < 0f)
                    Error("Invalid note " + note.id + ": " + path);
                if (note.input == 1) centers.Add(note.beat);
            }
            foreach (ChartNote note in chart.notes)
                if (note != null && note.input == 2 && centers.Contains(note.beat)) dualPairs++;
            Debug.Log("[DUOVEX Baseline] " + chart.title + ": " + chart.notes.Count +
                " logical notes, " + dualPairs + " shared-lane pairs.");
        }

        static void CheckThemeAndScene()
        {
            DuovexTheme theme = AssetDatabase.LoadAssetAtPath<DuovexTheme>(ThemePath);
            if (theme == null)
            {
                Warn("Theme is missing; generate DuovexV075.unity first.");
                return;
            }
            if (Mathf.Abs(theme.noteHeight - DuovexUIFactory.GameNoteHeight) > .01f ||
                Mathf.Abs(theme.noteSideInset - DuovexUIFactory.GameNoteInset) > .01f)
                Warn("Theme has older or custom note dimensions. Use Apply v0.7.2 Note Metrics if intended.");
            if (theme.whiteTap == null || theme.blackTap == null || theme.dualTap == null ||
                theme.whiteHold == null || theme.blackHold == null || theme.hitLine == null)
                Warn("One or more gameplay sprites are unassigned in DuovexTheme.");

            DuovexCanvasView view = UnityEngine.Object.FindAnyObjectByType<DuovexCanvasView>();
            if (view == null)
            {
                Warn("No editable DUOVEX_UI in the open scene; open DuovexV075.unity.");
                return;
            }
            for (int i = 0; i < 3; i++)
            {
                RectTransform lane = view.Get<RectTransform>("Game_Lane_" + i);
                if (lane == null) { Error("Missing gameplay lane " + i); continue; }
                float expectedX = DuovexUIFactory.GameGearX + i * DuovexUIFactory.GameLaneWidth;
                if (Mathf.Abs(lane.anchoredPosition.x - expectedX) > .5f ||
                    Mathf.Abs(lane.anchoredPosition.y + DuovexUIFactory.GameGearTop) > .5f ||
                    Mathf.Abs(lane.sizeDelta.x - DuovexUIFactory.GameLaneWidth) > .5f ||
                    Mathf.Abs(lane.sizeDelta.y - (DuovexUIFactory.GameHitCenterY - DuovexUIFactory.GameGearTop)) > .5f)
                    Warn("Lane " + i + " differs from the 288 px reference layout.");
            }
            RectTransform hitLine = view.Get<RectTransform>("Game_HitLine");
            if (hitLine == null) { Error("Missing Game_HitLine"); return; }
            float hitCenter = -hitLine.anchoredPosition.y + hitLine.sizeDelta.y * .5f;
            if (Mathf.Abs(hitCenter - DuovexUIFactory.GameHitCenterY) > .5f ||
                Mathf.Abs(hitLine.anchoredPosition.x - DuovexUIFactory.GameGearX) > .5f ||
                Mathf.Abs(hitLine.sizeDelta.x - 3f * DuovexUIFactory.GameLaneWidth) > .5f ||
                Mathf.Abs(hitLine.sizeDelta.y - 8f) > .5f)
                Warn("Hit line differs from the 288x8 px, Y=586 reference.");
            RectTransform core=view.Get<RectTransform>("Game_HitCore");
            if(core==null) Error("Missing bright Game_HitCore. Generate DuovexV075.unity.");
            else if (Mathf.Abs(-core.anchoredPosition.y+core.sizeDelta.y*.5f-hitCenter)>.5f ||
                     Mathf.Abs(core.sizeDelta.x-hitLine.sizeDelta.x)>.5f)
                Error("Game_HitCore is not centered on the gameplay judgement line.");
            for(int i=0;i<4;i++)
            {
                RectTransform key=view.Get<RectTransform>("Game_KeyBG_"+i);
                if(key==null) {Error("Missing Game_KeyBG_"+i);continue;}
                float keyTop=-key.anchoredPosition.y;
                if(keyTop-hitCenter<24f)
                    Error("Key "+i+" is too close to the judgement center (minimum 24 px gap).");
                if(Mathf.Abs(keyTop-DuovexUIFactory.GameKeyTop)>.5f)
                    Warn("Key "+i+" differs from the v0.7.4 reference vertical position.");
            }
            if(view.Get<UnityEngine.UI.Slider>("Slider_Music")==null)
                Error("Missing independent music volume slider; generate DuovexV075.unity.");
            foreach(string id in new[]{"B_Pause_Resume","B_Pause_Retry","B_Pause_Tracks"})
                if(view.Get<UnityEngine.UI.Button>(id)==null)
                    Error("Missing ESC pause menu button: "+id+"; regenerate DuovexV075.unity.");
        }
    }
}
#endif
