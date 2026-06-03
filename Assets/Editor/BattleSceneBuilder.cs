using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Sommoje.Battle;

/// <summary>
/// Battle Scene을 코드로 구성/저장한다. (메뉴: Sommoje ▸ Build Battle Scene)
/// 카메라를 격자에 맞춰 배치하고, BattleGrid 오브젝트를 둔다.
/// 실제 타일은 BattleGrid가 Play 시 생성한다.
/// </summary>
public static class BattleSceneBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/Battle.unity";

    [MenuItem("Sommoje/Build Battle Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- Camera ---
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.10f, 0.12f, 0.15f);

        // --- Battle grid ---
        var gridGo = new GameObject("BattleGrid");
        var bg = gridGo.AddComponent<BattleGrid>();
        bg.width = 12;
        bg.height = 8;
        bg.climate = Climate.Temperate;
        bg.latitudeGradient = true;   // 1단계 데모: 지구 느낌 위도 그라데이션을 한눈에

        // --- Frame camera over the grid (실제 맞춤은 CameraFitter가 화면비에 따라 처리) ---
        cam.transform.position = new Vector3(bg.width / 2f, bg.height / 2f, -10f);
        camGo.AddComponent<CameraFitter>();

        // --- Save ---
        if (!AssetDatabase.IsValidFolder(SceneDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        // 빌드 설정에 등록(첫 씬으로)
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log($"[Sommoje] Battle scene built at {ScenePath}");
    }

    /// <summary>Battle 씬을 열어 카메라를 PNG로 렌더링(편집 모드에서 검증/미리보기용).</summary>
    [MenuItem("Sommoje/Capture Battle Preview")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var bg = Object.FindFirstObjectByType<BattleGrid>();
        if (bg != null) bg.Rebuild();            // 타일 강제 생성(편집 모드)

        // 미리보기: 유닛 배치 + 플레이어 이동범위 강조
        if (bg != null)
        {
            var units = new System.Collections.Generic.List<Unit>();
            BattleController.SpawnDemo(bg, units);
            var player = units[0];
            var blocked = new System.Collections.Generic.HashSet<Vector2Int>();
            for (int i = 1; i < units.Count; i++) blocked.Add(units[i].Cell);
            var reach = BattleController.Reachable(bg, blocked, player.Cell, player.moveRange);
            bg.Highlight(reach, BattleController.MoveHighlight);
        }

        var cam = Object.FindFirstObjectByType<Camera>();
        const int W = 960, H = 640;              // 가로 화면 비율
        cam.aspect = (float)W / H;
        var fitter = cam.GetComponent<CameraFitter>();
        if (fitter != null) fitter.Fit();        // 맞춤 적용 후 렌더

        var rt = new RenderTexture(W, H, 24);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();

        cam.targetTexture = prevTarget;
        RenderTexture.active = prevActive;

        var bytes = tex.EncodeToPNG();
        var outPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Application.dataPath), "battle_preview.png");
        System.IO.File.WriteAllBytes(outPath, bytes);
        Debug.Log($"[Sommoje] Preview saved to {outPath}");
    }

    /// <summary>모바일 가로(Landscape) 방향으로 고정한다.</summary>
    [MenuItem("Sommoje/Configure Mobile (Landscape)")]
    public static void ConfigureMobile()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        Debug.Log("[Sommoje] Mobile landscape orientation configured.");
    }

    /// <summary>헤드리스 일괄: 씬 빌드 + 모바일 설정 + 미리보기 캡처.</summary>
    public static void SetupAndPreview()
    {
        Build();
        ConfigureMobile();
        Capture();
    }
}
