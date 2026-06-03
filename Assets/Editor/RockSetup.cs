using UnityEditor;
using UnityEngine;

public static class RockSetup
{
    const string FbxPath = "Assets/Art/Rocks/StonePack.fbx";

    [MenuItem("Sommoje/Setup Rocks")]
    public static void Setup()
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath(FbxPath);
        if (imp == null) { Debug.LogError("[Sommoje] StonePack.fbx not found"); return; }

        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.materialLocation = ModelImporterMaterialLocation.External;
        imp.materialSearch = ModelImporterMaterialSearch.Everywhere;
        imp.SaveAndReimport();

        const string texDir = "Assets/Art/Rocks/embedded";
        if (!AssetDatabase.IsValidFolder(texDir)) AssetDatabase.CreateFolder("Assets/Art/Rocks", "embedded");
        bool ok = imp.ExtractTextures(texDir);
        AssetDatabase.Refresh();
        imp.SaveAndReimport();

        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(FbxPath);
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        Debug.Log($"[Sommoje] ROCK children={inst.transform.childCount} extractTex={ok}");
        foreach (Transform c in inst.transform)
        {
            var r = c.GetComponentInChildren<Renderer>();
            Vector3 size = r != null ? r.bounds.size : Vector3.zero;
            Debug.Log($"[Sommoje] ROCK '{c.name}' size={size:F2}");
        }
        Object.DestroyImmediate(inst);
    }

    const string MatDir = "Assets/Art/Rocks/Materials";
    const string TexDir = "Assets/Art/Rocks/textures";

    [MenuItem("Sommoje/Assign Rock Materials")]
    public static void AssignMaterials()
    {
        // 머티리얼명 → (컬러, 노멀) 텍스처
        var map = new (string mat, string clr, string nrm)[]
        {
            ("SMall",                  "Small_clr.tga.png",              "small_nrm.png"),
            ("MID",                    "Mid_color.tga.png",              "MID_normal.png"),
            ("Big_02___Default_color", "Big_02___Default_color.tga.png", "02___Default_Normal_OpenGL.png"),
            ("Runic",                  "Runic_clr.tga.png",              "RUnic_normal.png"),
        };

        var urpLit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var (matName, clr, nrm) in map)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{matName}.mat");
            if (mat == null) { Debug.LogWarning($"[Sommoje] rock mat 없음: {matName}"); continue; }

            AsNormal($"{TexDir}/{nrm}");
            mat.shader = urpLit;
            mat.SetColor("_BaseColor", Color.white);
            mat.SetTexture("_BaseMap", Load($"{TexDir}/{clr}"));
            mat.SetTexture("_BumpMap", Load($"{TexDir}/{nrm}"));
            mat.EnableKeyword("_NORMALMAP");
            mat.SetFloat("_Smoothness", 0.12f);   // 바위는 거칠게
            EditorUtility.SetDirty(mat);
        }

        // p1, p2 같은 잔여 머티리얼도 URP로(마젠타 방지)
        foreach (var extra in new[] { "p1", "p2" })
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{extra}.mat");
            if (mat == null) continue;
            mat.shader = urpLit;
            EditorUtility.SetDirty(mat);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[Sommoje] rock materials → URP done");
    }

    static Texture2D Load(string p) => AssetDatabase.LoadAssetAtPath<Texture2D>(p);

    static void AsNormal(string p)
    {
        if (AssetImporter.GetAtPath(p) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
        { ti.textureType = TextureImporterType.NormalMap; ti.SaveAndReimport(); }
    }
}
