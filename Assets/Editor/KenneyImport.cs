using UnityEditor;
using UnityEngine;

/// <summary>
/// Assets/Resources/Kenney 아래 PNG들을 픽셀퍼펙트 스프라이트로 자동 임포트.
/// (Sprite 모드, Point 필터, 16 PPU, 압축/밉맵 끔)
/// </summary>
public class KenneyImport : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if (!assetPath.Replace("\\", "/").Contains("Resources/Kenney")) return;

        var ti = (TextureImporter)assetImporter;
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Single;
        ti.filterMode = FilterMode.Point;
        ti.spritePixelsPerUnit = 16;
        ti.mipmapEnabled = false;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.alphaIsTransparency = true;
    }
}
