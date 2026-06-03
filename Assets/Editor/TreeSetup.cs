using UnityEditor;
using UnityEngine;

public static class TreeSetup
{
    const string FbxPath = "Assets/Art/Trees/AcerTreePack.fbx";

    [MenuItem("Sommoje/Setup Trees")]
    public static void SetupMaterials()
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        if (imp == null) { Debug.LogError("[Sommoje] tree fbx importer null"); return; }
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.materialLocation = ModelImporterMaterialLocation.External;
        imp.materialSearch = ModelImporterMaterialSearch.Everywhere;
        imp.SaveAndReimport();

        const string texDir = "Assets/Art/Trees/embedded";
        if (!AssetDatabase.IsValidFolder(texDir)) AssetDatabase.CreateFolder("Assets/Art/Trees", "embedded");
        bool ok = imp.ExtractTextures(texDir);
        AssetDatabase.Refresh();
        imp.SaveAndReimport();
        Debug.Log($"[Sommoje] trees materialSearch=Everywhere, ExtractTextures={ok}");
    }

    const string TexDir = "Assets/Art/Trees/textures";
    const string MatDir = "Assets/Art/Trees/Materials";

    [MenuItem("Sommoje/Assign Tree Materials")]
    public static void AssignMaterials()
    {
        AsNormal($"{TexDir}/bark_normal.png");
        AsNormal($"{TexDir}/Cluster_Normal.png");
        AsTransparent($"{TexDir}/Cluster_leaf_RGBA.png");

        var barkCol = Load($"{TexDir}/bark_basecolor.png");
        var barkNrm = Load($"{TexDir}/bark_normal.png");
        var leafCol = Load($"{TexDir}/Cluster_leaf_RGBA.png");
        var leafNrm = Load($"{TexDir}/Cluster_Normal.png");

        // 줄기 (불투명)
        var bark = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Bark_Mat.mat");
        if (bark != null)
        {
            bark.SetTexture("_MainTex", barkCol);
            bark.SetTexture("_BumpMap", barkNrm);
            bark.EnableKeyword("_NORMALMAP");
            bark.SetFloat("_Glossiness", 0.1f);
            EditorUtility.SetDirty(bark);
        }

        // 잎 클러스터 (알파 컷아웃)
        var leaf = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/Cluster_Mat.mat");
        if (leaf != null)
        {
            leaf.SetFloat("_Mode", 1f);
            leaf.SetOverrideTag("RenderType", "TransparentCutout");
            leaf.EnableKeyword("_ALPHATEST_ON");
            leaf.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
            leaf.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
            leaf.SetInt("_ZWrite", 1);
            leaf.renderQueue = 2450;
            leaf.SetFloat("_Cutoff", 0.4f);
            leaf.SetTexture("_MainTex", leafCol);
            leaf.SetTexture("_BumpMap", leafNrm);
            leaf.EnableKeyword("_NORMALMAP");
            leaf.SetFloat("_Glossiness", 0.15f);
            EditorUtility.SetDirty(leaf);
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[Sommoje] tree materials assigned. bark={bark != null} leaf={leaf != null} " +
                  $"barkTex={barkCol != null} leafTex={leafCol != null}");
    }

    static Texture2D Load(string p) => AssetDatabase.LoadAssetAtPath<Texture2D>(p);

    static void AsNormal(string p)
    {
        if (AssetImporter.GetAtPath(p) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
        { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
    }

    static void AsTransparent(string p)
    {
        if (AssetImporter.GetAtPath(p) is TextureImporter ti)
        { ti.alphaIsTransparency = true; ti.SaveAndReimport(); }
    }

    [MenuItem("Sommoje/Inspect Tree Pack")]
    public static void Inspect()
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        if (fbx == null) { Debug.LogError("[Sommoje] AcerTreePack.fbx not found"); return; }

        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        Debug.Log($"[Sommoje] TREE root children = {inst.transform.childCount}");
        foreach (Transform c in inst.transform)
        {
            var r = c.GetComponentInChildren<Renderer>();
            var lod = c.GetComponent<LODGroup>();
            Vector3 size = r != null ? r.bounds.size : Vector3.zero;
            Debug.Log($"[Sommoje] TREE child '{c.name}' lodGroup={lod != null} hasRenderer={r != null} size={size:F2}");
        }
        Object.DestroyImmediate(inst);
    }
}
