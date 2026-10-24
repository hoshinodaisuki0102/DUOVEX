#if UNITY_EDITOR
using UnityEditor;

namespace DualSync.Editor
{
    public sealed class DuovexSpriteImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith("Assets/Resources/Visual/") &&
                !assetPath.StartsWith("Assets/Resources/Visual082/") &&
                !assetPath.StartsWith("Assets/Resources/Visual083/") &&
                !assetPath.StartsWith("Assets/Resources/Visual086/") &&
                !assetPath.StartsWith("Assets/Resources/Visual087/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
        }
    }
}
#endif
