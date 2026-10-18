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
        const string ScenePath = "Assets/Scenes/DuovexV07.unity";
        const string ThemePath = "Assets/Resources/Visual/DuovexTheme.asset";

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
            // Preserve user-modified original scenes. Generate a new v0.7 scene once.
            if(File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Duovex", "DuovexV07.unity already exists. No edits were overwritten. Duplicate or rename it before generating a new copy.", "OK");
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
            camera.backgroundColor=new Color(.034f,.046f,.085f);
            new GameObject("Duovex Controller").AddComponent<DualSyncGame>();
            DuovexUIFactory.Build(theme);
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.SaveAssets();
            Selection.activeGameObject=GameObject.Find("DUOVEX_UI");
            EditorUtility.DisplayDialog("DUOVEX", "Scene saved: Assets/Scenes/DuovexV07.unity.\nEdit DUOVEX_UI > DesignRoot in Scene/Inspector.\nRe-running does NOT overwrite existing scene edits.", "OK");
        }
        static Sprite Load(string name)=>AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Visual/"+name+".png");
    }
}
#endif
