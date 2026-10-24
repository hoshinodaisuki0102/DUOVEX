#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.IO;

namespace DualSync.Editor
{
    // Generates real editable Unity scene objects, not IMGUI surfaces.
    public static class DuovexSceneGenerator
    {
        const string ScenePath = "Assets/Scenes/DuovexV0885_EasyNormalHard.unity";
        const string ThemePath = "Assets/Resources/Visual/DuovexTheme.asset";

        [MenuItem("Tools/Duovex/Apply v0.7.2 Note Metrics to Theme")]
        public static void ApplyV072NoteMetrics()
        {
            DuovexTheme theme=AssetDatabase.LoadAssetAtPath<DuovexTheme>(ThemePath);
            if(theme==null)
            {
                EditorUtility.DisplayDialog("DUOVEX", "Generate the v0.7.2 scene first to create the theme asset.", "OK");
                return;
            }
            if(!EditorUtility.DisplayDialog("DUOVEX v0.7.2",
                "Update the current theme to 16 px note height and 6 px inset? Other sprite/theme settings will be preserved.",
                "Apply", "Cancel")) return;
            Undo.RecordObject(theme,"DUOVEX v0.7.2 note metrics");
            theme.noteHeight=DuovexUIFactory.GameNoteHeight;
            theme.noteSideInset=DuovexUIFactory.GameNoteInset;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("DUOVEX", "Note dimensions updated. Existing scenes and artwork were not overwritten.", "OK");
        }

        [MenuItem("Tools/Duovex/Generate Editable UI Scene")]
        public static void Generate()
        {
            if(EditorApplication.isPlaying) { EditorUtility.DisplayDialog("Duovex", "Exit Play Mode first.", "OK"); return; }
            Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
            string[] names={"Background","Panel","Button","Lane","HitLine","WhiteTap","BlackTap","DualTap","WhiteHold","BlackHold","Glow","MenuOrbital","DuovexMark"};
            foreach(string name in names)
            {
                string path="Assets/Resources/Visual/"+name+".png";
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null && importer.textureType!=TextureImporterType.Sprite)
                {
                    importer.textureType=TextureImporterType.Sprite;
                    importer.spriteImportMode=SpriteImportMode.Single;
                    importer.alphaIsTransparency=true;
                    importer.mipmapEnabled=false;
                    importer.SaveAndReimport();
                }
            }
            // Ensure imported UI artwork is genuinely Sprite type, even if
            // texture imports occurred before AssetPostprocessor compilation.
            foreach(string guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/Visual082","Assets/Resources/Visual083","Assets/Resources/Visual086","Assets/Resources/Visual087"}))
            {
                string path=AssetDatabase.GUIDToAssetPath(guid);
                var importer=AssetImporter.GetAtPath(path) as TextureImporter;
                if(importer!=null && importer.textureType!=TextureImporterType.Sprite)
                {
                    importer.textureType=TextureImporterType.Sprite;
                    importer.spriteImportMode=SpriteImportMode.Single;
                    importer.alphaIsTransparency=true;
                    importer.mipmapEnabled=false;
                    importer.SaveAndReimport();
                }
            }
            AssetDatabase.Refresh();
            DuovexTheme theme=AssetDatabase.LoadAssetAtPath<DuovexTheme>(ThemePath);
            if(theme==null)
            {
                theme=ScriptableObject.CreateInstance<DuovexTheme>();
                AssetDatabase.CreateAsset(theme,ThemePath);
                theme.background=Load("Background");theme.panel=Load("Panel");
                theme.menuOrbital=Load("MenuOrbital");theme.duovexMark=Load("DuovexMark");
                theme.button=Load("Button");theme.lane=Load("Lane");theme.hitLine=Load("HitLine");
                theme.whiteTap=Load("WhiteTap");theme.blackTap=Load("BlackTap");
                theme.dualTap=Load("DualTap");theme.whiteHold=Load("WhiteHold");
                theme.blackHold=Load("BlackHold");theme.glow=Load("Glow");
                try {theme.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");}catch{ }
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssets();
            }
            // UI geometry is serialized: always generate a NEW scene for v0.8.85.
            // Older customized scenes and their notes/layout remain untouched.
            if(File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Duovex", "DuovexV0885_EasyNormalHard.unity already exists. No edits were overwritten. Duplicate or rename it before generating a new copy.", "OK");
                return;
            }
            if(theme.menuOrbital==null)theme.menuOrbital=Load("MenuOrbital");
            if(theme.duovexMark==null)theme.duovexMark=Load("DuovexMark");
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            // Do not overwrite the pre-existing Main scene or user projects.
            Scene scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var camObj=new GameObject("Main Camera",typeof(Camera),typeof(AudioListener));
            camObj.tag="MainCamera";
            Camera camera=camObj.GetComponent<Camera>();
            camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.015f,.013f,.025f);
            new GameObject("Duovex Controller").AddComponent<DualSyncGame>();
            DuovexUIFactory.Build(theme);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Selection.activeGameObject=GameObject.Find("DUOVEX_UI");
            EditorUtility.DisplayDialog("DUOVEX", "Scene saved: Assets/Scenes/DuovexV0885_EasyNormalHard.unity.\nButtons fixed, countdown resumes, clear/fail results, and complete 1-20 difficulty chart selection.\nPrevious charts, game logic, and custom scenes are preserved. Older scenes are preserved.", "OK");
        }
        static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Visual/"+name+".png");
    }
}
#endif
