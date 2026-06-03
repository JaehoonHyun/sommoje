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
}
