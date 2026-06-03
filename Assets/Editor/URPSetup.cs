using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class URPSetup
{
    const string Dir = "Assets/Settings";

    [MenuItem("Sommoje/Setup URP Pipeline")]
    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets", "Settings");

        var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
        AssetDatabase.CreateAsset(rendererData, Dir + "/URP_Renderer.asset");
        AssetDatabase.SaveAssets();

        var urp = UniversalRenderPipelineAsset.Create(rendererData);
        AssetDatabase.CreateAsset(urp, Dir + "/URP_Asset.asset");
        AssetDatabase.SaveAssets();

        GraphicsSettings.defaultRenderPipeline = urp;
        int n = QualitySettings.count;
        for (int i = 0; i < n; i++)
        {
            QualitySettings.SetQualityLevel(i, false);
            QualitySettings.renderPipeline = urp;
        }
        AssetDatabase.SaveAssets();

        Debug.Log($"[Sommoje] URP pipeline created + assigned. active={GraphicsSettings.currentRenderPipeline != null}");
    }

    [MenuItem("Sommoje/Convert Materials To URP")]
    public static void ConvertMaterials()
    {
        // 1) 빌트인 머티리얼 에셋 → URP 일괄 변환
        try
        {
            UnityEditor.Rendering.Universal.Converters.RunInBatchMode(
                UnityEditor.Rendering.Universal.ConverterContainerId.BuiltInToURP);
        }
        catch (System.Exception e) { Debug.LogWarning("[Sommoje] converter: " + e.Message); }

        // 2) 모델 FBX 재임포트 (임베디드 머티리얼 URP화)
        foreach (var p in new[]
        {
            "Assets/Art/Character/Brute.fbx", "Assets/Art/Character/Erika.fbx",
            "Assets/Art/Trees/AcerTreePack.fbx", "Assets/Art/Rocks/StonePack.fbx",
        })
            AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);

        AssetDatabase.Refresh();
        Debug.Log("[Sommoje] URP material conversion done");
    }
}
