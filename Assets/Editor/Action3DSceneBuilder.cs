using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Sommoje.Action3D;

/// <summary>
/// 3D 액션 씬을 코드로 구성/저장. (메뉴: Sommoje ▸ Build Action3D Scene)
/// 펄린 노이즈 지형(Terrain) + 플레이어 + 적 + 풍경 + 3인칭 카메라 + 조명.
/// </summary>
public static class Action3DSceneBuilder
{
    const string SceneDir = "Assets/Scenes";
    const string ScenePath = SceneDir + "/Action3D.unity";

    static Material Mat(Color c) => new(Shader.Find("Universal Render Pipeline/Lit")) { color = c };

    static GameObject Prim(PrimitiveType t, Vector3 pos, Vector3 scale, Color col, string name)
    {
        var go = GameObject.CreatePrimitive(t);
        go.name = name;
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(col);
        return go;
    }

    static Vector3 OnTerrain(Terrain t, float x, float z, float lift)
    {
        float y = t.SampleHeight(new Vector3(x, 0, z)) + t.transform.position.y + lift;
        return new Vector3(x, y, z);
    }

    const float PropScale = 2.5f;   // native ~1.2~1.7유닛 → 3~4유닛 나무로

    // Sketchfab Acer 나무팩에서 개별 나무를 흩뿌린다(Ground 제외, 키 정규화).
    static void AttachTrees(Terrain terrain)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Trees/AcerTreePack.fbx");
        if (fbx == null) { Debug.LogWarning("[Sommoje] AcerTreePack.fbx 없음"); return; }

        var template = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        var variants = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in template.transform)
            if (c.name.StartsWith("Acer")) variants.Add(c);
        if (variants.Count == 0) { Object.DestroyImmediate(template); return; }

        // 호수를 둘러싼 빽빽한 숲 (지터 격자 + 노이즈 빈틈)
        int idx = 0;
        for (float gx = -72f; gx <= 72f; gx += 9f)
            for (float gz = -72f; gz <= 72f; gz += 9f)
            {
                float px = gx + (Mathf.PerlinNoise(gx * 0.3f + 0.1f, gz * 0.3f) - 0.5f) * 8f;
                float pz = gz + (Mathf.PerlinNoise(gx * 0.3f, gz * 0.3f + 0.7f) - 0.5f) * 8f;
                float r = Mathf.Sqrt(px * px + pz * pz);
                if (r < 27f || r > 72f) continue;                                     // 호숫가 빈터 + 높은산 제외
                if (Mathf.Sqrt(px * px + (pz + 28f) * (pz + 28f)) < 8f) continue;     // 플레이어 스폰 비움
                if (Mathf.PerlinNoise(px * 0.13f + 5f, pz * 0.13f) < 0.30f) continue; // 자연스런 빈틈

                var src = variants[idx % variants.Count];
                var tree = Object.Instantiate(src.gameObject);
                tree.name = "Tree";
                tree.transform.SetParent(null, false);
                tree.transform.localScale = Vector3.one;

                // LODGroup 제거: 항상 고해상 메시(LOD0)만 보이게 → 빌보드 LOD 미사용
                foreach (var lg in tree.GetComponentsInChildren<LODGroup>()) Object.DestroyImmediate(lg);
                var kill = new System.Collections.Generic.List<GameObject>();
                foreach (var rr in tree.GetComponentsInChildren<Renderer>())
                    foreach (var mm in rr.sharedMaterials)
                        if (mm != null && mm.name.Contains("Billboard")) { kill.Add(rr.gameObject); break; }
                foreach (var g in kill) Object.DestroyImmediate(g);

                var rends = tree.GetComponentsInChildren<Renderer>();
                if (rends.Length > 0)
                {
                    var b = rends[0].bounds;
                    for (int k = 1; k < rends.Length; k++) b.Encapsulate(rends[k].bounds);
                    float h = 20f + Mathf.PerlinNoise(px * 0.2f, pz * 0.2f) * 22f;     // 20~42 (×3 범위)
                    tree.transform.localScale = Vector3.one * (h / Mathf.Max(0.01f, b.size.y));

                    var cap = tree.AddComponent<CapsuleCollider>();   // 나무 줄기 = 벽
                    float s = h / Mathf.Max(0.01f, b.size.y);          // lossyScale
                    cap.height = b.size.y;
                    cap.radius = 1.3f / s;                             // 월드 반경 ≈1.3
                    cap.center = new Vector3(0f, b.size.y * 0.5f, 0f);
                    if (idx == 0)
                        Debug.Log($"[Sommoje] tree0 collider worldR={cap.radius * s:F2} worldH={cap.height * s:F2} pos={tree.transform.position:F1}");
                }
                tree.transform.position = OnTerrain(terrain, px, pz, 0f);
                tree.transform.rotation = Quaternion.Euler(0f, (idx * 47) % 360, 0f);
                idx++;
            }

        Object.DestroyImmediate(template);
        Debug.Log($"[Sommoje] forest trees = {idx}");
    }

    // Sketchfab StonePack에서 리얼 바위 배치 (메시 콜라이더 = 벽)
    static void AttachRocks(Terrain terrain)
    {
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Rocks/StonePack.fbx");
        if (fbx == null) { Debug.LogWarning("[Sommoje] StonePack.fbx 없음"); return; }

        var template = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        var variants = new System.Collections.Generic.List<Transform>();
        foreach (Transform c in template.transform)
        {
            string nm = c.name;
            if (nm.StartsWith("BIG") || nm.StartsWith("Big") || nm.StartsWith("Mid") || nm.StartsWith("Small")) variants.Add(c);
        }
        if (variants.Count == 0) { Object.DestroyImmediate(template); return; }

        var pos = new (float x, float z, float h)[]
        {
            (24, 6, 3f), (-22, 8, 2.2f), (20, -16, 2.6f), (-18, -14, 3.6f), (28, -2, 2.2f), (-30, 4, 3.2f),
            (8, 24, 1.8f), (-6, -22, 2.4f), (16, -10, 3.4f), (-26, -6, 2.2f), (31, 13, 3f), (-12, 26, 1.8f),
        };
        for (int i = 0; i < pos.Length; i++)
        {
            var src = variants[(i * 3) % variants.Count];
            var rock = Object.Instantiate(src.gameObject);
            rock.name = "Rock";
            rock.transform.SetParent(null, false);
            rock.transform.localScale = Vector3.one;

            var rends = rock.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int k = 1; k < rends.Length; k++) b.Encapsulate(rends[k].bounds);
                rock.transform.localScale = Vector3.one * (pos[i].h / Mathf.Max(0.01f, b.size.y));
            }
            rock.transform.position = OnTerrain(terrain, pos[i].x, pos[i].z, -0.35f);   // 약간 묻히게
            rock.transform.rotation = Quaternion.Euler(0f, (i * 61) % 360, 0f);

            foreach (var mf in rock.GetComponentsInChildren<MeshFilter>())
            {
                var mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
                mc.convex = true;
            }
        }

        Object.DestroyImmediate(template);
        Debug.Log("[Sommoje] rocks placed (StonePack)");
    }
    const float TerrainHeight = 60f;
    const float WaterY = 7.2f;       // 호수 수면 높이 (= 0.12 * TerrainHeight)

    static void PlaceModel(Terrain t, string file, float x, float z, float scale, float yRot)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/Art/Nature/{file}.fbx");
        if (asset == null) { Debug.LogWarning($"[Sommoje] missing model {file}"); return; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        go.transform.position = OnTerrain(t, x, z, 0f);
        go.transform.localScale = Vector3.one * scale * PropScale;
        go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

        // 바위는 리얼 바위 텍스처(Rock023)로 교체 (산이랑 통일감)
        if (file.StartsWith("rock"))
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.sharedMaterial = RealRockMat();
    }

    static Material _realRock;
    static Material RealRockMat()
    {
        if (_realRock != null) return _realRock;
        var col = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Terrain/Real/rock_color.jpg");
        const string normPath = "Assets/Art/Terrain/Real/rock_normal.jpg";
        if (AssetImporter.GetAtPath(normPath) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
        { ni.textureType = TextureImporterType.NormalMap; ni.SaveAndReimport(); }
        var norm = AssetDatabase.LoadAssetAtPath<Texture2D>(normPath);

        _realRock = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        _realRock.SetTexture("_BaseMap", col);
        if (norm != null) { _realRock.SetTexture("_BumpMap", norm); _realRock.EnableKeyword("_NORMALMAP"); }
        _realRock.SetFloat("_Smoothness", 0.12f);
        _realRock.SetFloat("_Metallic", 0f);
        _realRock.mainTextureScale = new Vector2(1.6f, 1.6f);
        return _realRock;
    }

    // 적에 Brute(도끼) 모델 부착 (걷기 애니, 플레이어 추격)
    static void AttachEnemyCharacter(GameObject enemy)
    {
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Character/Brute.fbx");
        if (modelAsset == null)
        {
            enemy.AddComponent<MeshFilter>();   // 폴백: 빨간 캡슐
            var cap = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cap.transform.SetParent(enemy.transform, false);
            return;
        }

        var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.transform.SetParent(enemy.transform, false);

        var rends = model.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            model.transform.localScale = Vector3.one * (1.9f / Mathf.Max(0.01f, b.size.y));
        }
        model.transform.localPosition = new Vector3(0f, -1f, 0f);

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Art/Character/PlayerAnimator.controller");
        animator.applyRootMotion = false;

        // 콜라이더 = 물리 충돌(통과 방지) + 우클릭 타겟 레이캐스트
        var col = enemy.AddComponent<CapsuleCollider>();
        col.height = 2.2f;
        col.radius = 0.7f;
        col.center = Vector3.zero;

        enemy.AddComponent<MixamoCharacter>().animator = animator;
    }

    // Mixamo 휴머노이드 모델을 플레이어에 부착 (Humanoid Animator 연결, 키 맞춤)
    static void AttachMixamoCharacter(GameObject player)
    {
        var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/Character/Erika.fbx");
        if (modelAsset == null)
        {
            Debug.LogWarning("[Sommoje] Brute.fbx 없음 → 도형 캐릭터 폴백");
            BuildCharacter(player.transform, new Color(0.25f, 0.5f, 0.95f), new Color(0.95f, 0.80f, 0.62f));
            return;
        }

        var model = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset);
        model.transform.SetParent(player.transform, false);

        var rends = model.GetComponentsInChildren<Renderer>();
        if (rends.Length > 0)
        {
            var b = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
            model.transform.localScale = Vector3.one * (1.9f / Mathf.Max(0.01f, b.size.y));
        }
        model.transform.localPosition = new Vector3(0f, -1f, 0f);   // 발이 컨트롤러 바닥에

        var animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Art/Character/PlayerAnimator.controller");
        animator.applyRootMotion = false;

        player.AddComponent<MixamoCharacter>().animator = animator;
    }

    // 도형 조립 캐릭터 (머리·몸통·팔·다리, CharacterAnimator로 애니메이션)
    static void BuildCharacter(Transform root, Color body, Color skin)
    {
        var vis = new GameObject("Visual").transform;
        vis.SetParent(root, false);

        var legCol = new Color(0.2f, 0.22f, 0.32f);
        MakePart(vis, "Torso", new Vector3(0, 0.10f, 0), new Vector3(0.55f, 0.7f, 0.34f), body);
        MakePart(vis, "Head", new Vector3(0, 0.72f, 0), new Vector3(0.46f, 0.46f, 0.46f), skin);

        var armL = MakeLimb(vis, "ArmL", new Vector3(-0.40f, 0.42f, 0), 0.55f, new Vector3(0.17f, 0.55f, 0.17f), body);
        var armR = MakeLimb(vis, "ArmR", new Vector3(0.40f, 0.42f, 0), 0.55f, new Vector3(0.17f, 0.55f, 0.17f), body);
        var legL = MakeLimb(vis, "LegL", new Vector3(-0.16f, -0.20f, 0), 0.68f, new Vector3(0.2f, 0.68f, 0.2f), legCol);
        var legR = MakeLimb(vis, "LegR", new Vector3(0.16f, -0.20f, 0), 0.68f, new Vector3(0.2f, 0.68f, 0.2f), legCol);

        var anim = root.gameObject.AddComponent<CharacterAnimator>();
        anim.armL = armL; anim.armR = armR; anim.legL = legL; anim.legR = legR;
    }

    static void MakePart(Transform parent, string name, Vector3 pos, Vector3 scale, Color col)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().sharedMaterial = Mat(col);
    }

    static Transform MakeLimb(Transform parent, string name, Vector3 pivotPos, float length, Vector3 scale, Color col)
    {
        var pivot = new GameObject(name).transform;
        pivot.SetParent(parent, false);
        pivot.localPosition = pivotPos;

        var mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Object.DestroyImmediate(mesh.GetComponent<Collider>());
        mesh.transform.SetParent(pivot, false);
        mesh.transform.localPosition = new Vector3(0, -length / 2f, 0);
        mesh.transform.localScale = scale;
        mesh.GetComponent<Renderer>().sharedMaterial = Mat(col);
        return pivot;
    }

    [MenuItem("Sommoje/Build Action3D Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // --- 환경/조명 ---
        SetupSky();

        var lightGo = new GameObject("Directional Light");
        var l = lightGo.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.5f;
        l.color = new Color(1f, 0.97f, 0.9f);
        l.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // --- 지형 + 호수 ---
        var terrain = GenerateTerrain();
        BuildWater();

        // --- 풍경: 리얼 나무(Acer) + 리얼 바위(StonePack) + 풀/꽃 ---
        AttachTrees(terrain);
        AttachRocks(terrain);
        var props = new (float x, float z, string model, float scale)[]
        {
            (18, 20, "grass_large", 2.5f), (-16, 14, "flower_redA", 2.5f), (12, -22, "grass_large", 2.5f), (-10, 18, "grass_large", 2.5f),
        };
        for (int i = 0; i < props.Length; i++)
            PlaceModel(terrain, props[i].model, props[i].x, props[i].z, props[i].scale, (i * 53) % 360);

        // --- 플레이어 (관절 캐릭터, 남쪽 물가) ---
        var player = new GameObject("Player");
        player.transform.position = OnTerrain(terrain, 0, -28, 1.2f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = Vector3.zero;
        player.AddComponent<PlayerController3D>();
        player.AddComponent<SkillSystem>();
        AttachMixamoCharacter(player);

        // --- 적 (도끼 든 Brute, 물가 근처) ---
        foreach (var p in new[] { new Vector2(7, -24), new Vector2(-8, -26), new Vector2(10, -33) })
        {
            var enemy = new GameObject("Enemy");
            enemy.transform.position = OnTerrain(terrain, p.x, p.y, 1f);
            enemy.AddComponent<Enemy3D>();
            AttachEnemyCharacter(enemy);
        }

        // --- 카메라 ---
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 55f;
        cam.enabled = true;
        cam.targetDisplay = 0;
        cam.targetTexture = null;
        camGo.AddComponent<AudioListener>();
        var pp = player.transform.position;
        camGo.transform.position = pp + new Vector3(0f, 6.5f, -9f);
        camGo.transform.LookAt(pp + Vector3.up * 1.5f);
        camGo.AddComponent<ThirdPersonCamera>().target = player.transform;
        SetupPostProcessing(cam);

        // --- 저장 ---
        if (!AssetDatabase.IsValidFolder(SceneDir))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (!list.Exists(s => s.path == ScenePath))
            list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();

        var cams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        Debug.Log($"[Sommoje] cameras={cams.Length} enabled={cam.enabled} active={camGo.activeInHierarchy} tag={camGo.tag} targetTex={(cam.targetTexture == null ? "null" : "SET")}");
        Debug.Log($"[Sommoje] Action3D scene built at {ScenePath}");
    }

    static void BuildWater()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Plane);
        go.name = "Lake";
        Object.DestroyImmediate(go.GetComponent<Collider>());
        go.transform.position = new Vector3(0f, WaterY, 0f);
        go.transform.localScale = Vector3.one * 8f;   // 80x80 (가장자리는 지형에 가려짐)

        var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        m.SetFloat("_Surface", 1f);                    // Transparent
        m.SetFloat("_Blend", 0f);                      // Alpha blend
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        m.color = new Color(0.15f, 0.38f, 0.55f, 0.82f);
        m.SetFloat("_Smoothness", 0.95f);              // 매끈 → 하늘 반사
        m.SetFloat("_Metallic", 0.1f);

        var normal = GenerateWaterNormal();
        m.SetTexture("_BumpMap", normal);
        m.EnableKeyword("_NORMALMAP");
        m.SetFloat("_BumpScale", 0.5f);
        m.SetTextureScale("_BumpMap", new Vector2(16f, 16f));   // 잔물결 타일링

        // 에셋으로 저장: 런타임 임베디드 머티리얼은 투명 URP/Lit 셰이더 참조가
        // 플레이모드에서 깨져 마젠타로 나올 수 있음 → .mat 에셋이면 안정적.
        if (!AssetDatabase.IsValidFolder("Assets/Art/Materials"))
            AssetDatabase.CreateFolder("Assets/Art", "Materials");
        const string matPath = "Assets/Art/Materials/Water.mat";
        AssetDatabase.DeleteAsset(matPath);
        AssetDatabase.CreateAsset(m, matPath);
        AssetDatabase.SaveAssets();

        go.GetComponent<Renderer>().sharedMaterial = m;
        go.AddComponent<WaterAnimator>();

        EnsureAlwaysIncluded("Universal Render Pipeline/Lit");   // 투명 변형 보장
    }

    // 셰이더를 Graphics ▸ Always Included Shaders 에 추가(투명 등 희귀 변형 누락 방지)
    static void EnsureAlwaysIncluded(string shaderName)
    {
        var shader = Shader.Find(shaderName);
        if (shader == null) return;

        var gs = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/GraphicsSettings.asset");
        if (gs == null) return;
        var so = new SerializedObject(gs);
        var arr = so.FindProperty("m_AlwaysIncludedShaders");

        for (int i = 0; i < arr.arraySize; i++)
            if (arr.GetArrayElementAtIndex(i).objectReferenceValue == shader) return;   // 이미 있음

        arr.InsertArrayElementAtIndex(arr.arraySize);
        arr.GetArrayElementAtIndex(arr.arraySize - 1).objectReferenceValue = shader;
        so.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    /// <summary>물결용 노멀맵을 코드로 생성(이음새 없는 사인파 합성).</summary>
    static Texture2D GenerateWaterNormal()
    {
        const int S = 128;
        var height = new float[S, S];
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float u = (float)x / S, v = (float)y / S;
                height[y, x] = Mathf.Sin(2f * Mathf.PI * (3f * u + 2f * v))
                             + 0.7f * Mathf.Sin(2f * Mathf.PI * (2f * u - 3f * v))
                             + 0.5f * Mathf.Sin(2f * Mathf.PI * (5f * u + 1f * v));
            }

        var tex = new Texture2D(S, S, TextureFormat.RGB24, false);
        const float strength = 1.3f;
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float hl = height[y, (x - 1 + S) % S];
                float hr = height[y, (x + 1) % S];
                float hd = height[(y - 1 + S) % S, x];
                float hu = height[(y + 1) % S, x];
                var n = new Vector3(-(hr - hl) * strength, -(hu - hd) * strength, 1f).normalized;
                tex.SetPixel(x, y, new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f));
            }
        tex.Apply();

        const string path = "Assets/Art/water_normal.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
        {
            ti.textureType = TextureImporterType.NormalMap;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    static void SetupPostProcessing(Camera cam)
    {
        cam.allowHDR = true;
        var camData = cam.GetUniversalAdditionalCameraData();
        if (camData != null) camData.renderPostProcessing = true;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.overrideState = true; tone.mode.value = TonemappingMode.ACES;

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.overrideState = true; ca.postExposure.value = 0.7f;   // ACES 보정(밝게)
        ca.saturation.overrideState = true; ca.saturation.value = 14f;
        ca.contrast.overrideState = true;   ca.contrast.value = 10f;

        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.overrideState = true; bloom.intensity.value = 0.7f;
        bloom.threshold.overrideState = true; bloom.threshold.value = 1.1f;

        var vig = profile.Add<Vignette>(true);
        vig.intensity.overrideState = true; vig.intensity.value = 0.18f;

        AssetDatabase.DeleteAsset("Assets/Art/pp_urp.asset");
        AssetDatabase.CreateAsset(profile, "Assets/Art/pp_urp.asset");

        var volGo = new GameObject("PostProcessVolume");
        var vol = volGo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.sharedProfile = profile;
    }

    static void SetupSky()
    {
        var hdr = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Sky/sky.hdr");
        if (hdr == null)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.57f, 0.62f);
            RenderSettings.skybox = new Material(Shader.Find("Skybox/Procedural"));
            return;
        }

        var sky = new Material(Shader.Find("Skybox/Panoramic"));
        sky.SetTexture("_MainTex", hdr);
        sky.SetFloat("_Mapping", 1f);    // Latitude-Longitude
        sky.SetFloat("_ImageType", 0f);  // 360도
        sky.SetFloat("_Exposure", 0.95f);

        AssetDatabase.DeleteAsset("Assets/Art/Sky/sky_mat.mat");
        AssetDatabase.CreateAsset(sky, "Assets/Art/Sky/sky_mat.mat");

        RenderSettings.skybox = sky;
        RenderSettings.ambientMode = AmbientMode.Skybox;   // 하늘에서 자연광(IBL)
        RenderSettings.ambientIntensity = 1.25f;           // 숲 그늘 밝게
        DynamicGI.UpdateEnvironment();
    }

    static Terrain GenerateTerrain()
    {
        const int res = 257;
        var td = new TerrainData { heightmapResolution = res, size = new Vector3(200f, TerrainHeight, 200f) };

        var h = new float[res, res];
        const float ox = 1234.5f, oy = 6789.5f;
        for (int y = 0; y < res; y++)
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1), ny = (float)y / (res - 1);
                float e = 0.5f * Mathf.PerlinNoise(ox + nx * 3f, oy + ny * 3f)
                        + 0.3f * Mathf.PerlinNoise(ox + nx * 6f, oy + ny * 6f)
                        + 0.2f * Mathf.PerlinNoise(ox + nx * 12f, oy + ny * 12f);   // 0..1
                float cx = nx - 0.5f, cz = ny - 0.5f;
                float dist = Mathf.Sqrt(cx * cx + cz * cz) * 2f;          // 0 중앙 .. ~1.4 모서리

                // 가운데 호수 분지 → 평지 → 가장자리 산
                if (dist < 0.22f)
                {
                    float k = Mathf.SmoothStep(0f, 1f, dist / 0.22f);
                    h[y, x] = Mathf.Lerp(0.035f, 0.17f, k);              // 호수 바닥 → 물가
                    continue;
                }
                if (dist < 0.55f)
                {
                    h[y, x] = Mathf.Clamp01(0.17f + (e - 0.5f) * 0.022f); // 평지(거의 평탄)
                    continue;
                }
                float m = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 1.25f, dist));
                h[y, x] = Mathf.Clamp01(Mathf.Lerp(0.18f, 0.97f, m) + (e - 0.5f) * 0.28f * m);  // 산
            }
        td.SetHeights(0, 0, h);
        td.terrainLayers = BuildTerrainLayers();   // 0=잔디 1=바위 2=흙
        PaintSplatmap(td);
        AddGrassDetail(td);

        var go = Terrain.CreateTerrainGameObject(td);
        go.name = "Terrain";
        go.transform.position = new Vector3(-100f, 0f, -100f);
        return go.GetComponent<Terrain>();
    }

    static TerrainLayer[] BuildTerrainLayers()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art")) AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder("Assets/Art/Terrain")) AssetDatabase.CreateFolder("Assets/Art", "Terrain");

        return new[]
        {
            MakeLayer("grass", new Vector2(7f, 7f)),
            MakeLayer("rock",  new Vector2(14f, 14f)),
            MakeLayer("dirt",  new Vector2(6f, 6f)),
        };
    }

    static TerrainLayer MakeLayer(string baseName, Vector2 tile)
    {
        const string dir = "Assets/Art/Terrain";
        string colorPath = $"{dir}/Real/{baseName}_color.jpg";
        string normalPath = $"{dir}/Real/{baseName}_normal.jpg";

        var color = AssetDatabase.LoadAssetAtPath<Texture2D>(colorPath);
        if (AssetImporter.GetAtPath(normalPath) is TextureImporter ni && ni.textureType != TextureImporterType.NormalMap)
        {
            ni.textureType = TextureImporterType.NormalMap;
            ni.SaveAndReimport();
        }
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);

        string layerPath = $"{dir}/layer_{baseName}.terrainlayer";
        AssetDatabase.DeleteAsset(layerPath);
        var layer = new TerrainLayer { diffuseTexture = color, normalMapTexture = normal, tileSize = tile, smoothness = 0f, metallic = 0f };
        AssetDatabase.CreateAsset(layer, layerPath);
        AssetDatabase.SaveAssets();
        return layer;
    }

    /// <summary>평지에 바람에 흔들리는 3D 풀(디테일 빌보드)을 심는다.</summary>
    static void AddGrassDetail(TerrainData td)
    {
        var grassTex = GenerateGrassBladeTexture();
        td.detailPrototypes = new[]
        {
            new DetailPrototype
            {
                prototypeTexture = grassTex,
                renderMode = DetailRenderMode.GrassBillboard,
                usePrototypeMesh = false,
                healthyColor = new Color(0.45f, 0.62f, 0.30f),
                dryColor = new Color(0.62f, 0.60f, 0.28f),
                minWidth = 0.8f, maxWidth = 1.8f,
                minHeight = 0.9f, maxHeight = 2.0f,
                noiseSpread = 0.3f,
            }
        };

        // 바람
        td.wavingGrassStrength = 0.4f;
        td.wavingGrassSpeed = 0.5f;
        td.wavingGrassAmount = 0.4f;
        td.wavingGrassTint = new Color(0.7f, 0.75f, 0.55f);

        const int dr = 256;
        td.SetDetailResolution(dr, 16);
        var density = new int[dr, dr];
        for (int dy = 0; dy < dr; dy++)
            for (int dx = 0; dx < dr; dx++)
            {
                float nx = (float)dx / (dr - 1);
                float nz = (float)dy / (dr - 1);
                float heightN = td.GetInterpolatedHeight(nx, nz) / TerrainHeight;
                float slope = td.GetSteepness(nx, nz) / 90f;
                bool grassy = slope < 0.34f && heightN > 0.14f && heightN < 0.50f;
                density[dy, dx] = grassy ? 18 : 0;
            }
        td.SetDetailLayer(0, 0, 0, density);
    }

    static Texture2D GenerateGrassBladeTexture()
    {
        const int S = 64;
        var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
        var clear = new Color(0f, 0f, 0f, 0f);
        for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
                tex.SetPixel(x, y, clear);

        const int blades = 8;
        for (int b = 0; b < blades; b++)
        {
            float bx = (b + 0.5f) / blades * S + (Mathf.PerlinNoise(b * 3.1f, 0.5f) - 0.5f) * 5f;
            float topY = S * (0.55f + Mathf.PerlinNoise(b * 1.7f, 2.3f) * 0.35f);
            float curve = (Mathf.PerlinNoise(b * 1.3f, 5.1f) - 0.5f) * 8f;
            for (float t = 0f; t < 1f; t += 0.01f)
            {
                float y = Mathf.Lerp(2f, topY, t);
                float x = bx + curve * t * t;
                float w = Mathf.Lerp(1.6f, 0.3f, t);
                var col = Color.Lerp(new Color(0.16f, 0.40f, 0.12f), new Color(0.48f, 0.72f, 0.26f), t);
                for (int o = -Mathf.CeilToInt(w); o <= Mathf.CeilToInt(w); o++)
                {
                    int px = Mathf.RoundToInt(x + o), py = Mathf.RoundToInt(y);
                    if (px >= 0 && px < S && py >= 0 && py < S) tex.SetPixel(px, py, col);
                }
            }
        }
        tex.Apply();

        const string path = "Assets/Art/grass_blade.png";
        System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(path);
        if (AssetImporter.GetAtPath(path) is TextureImporter ti)
        {
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    /// <summary>높이·경사에 따라 잔디/바위/흙을 자동 블렌딩(스플랫맵).</summary>
    static void PaintSplatmap(TerrainData td)
    {
        const int ar = 256;
        td.alphamapResolution = ar;
        var map = new float[ar, ar, 3];

        for (int ay = 0; ay < ar; ay++)
            for (int ax = 0; ax < ar; ax++)
            {
                float nx = (float)ax / (ar - 1);
                float nz = (float)ay / (ar - 1);
                float heightN = td.GetInterpolatedHeight(nx, nz) / TerrainHeight;
                float slope = td.GetSteepness(nx, nz) / 90f;

                float rock = Mathf.Max(Mathf.SmoothStep(0.45f, 0.68f, slope),
                                       Mathf.SmoothStep(0.62f, 0.85f, heightN));
                float shore = Mathf.Exp(-Mathf.Pow((heightN - 0.135f) / 0.02f, 2f));
                float dirt = (1f - rock) * shore;
                float grass = Mathf.Max(0f, 1f - rock - dirt);

                float sum = rock + dirt + grass;
                if (sum < 1e-4f) { grass = 1f; sum = 1f; }
                map[ay, ax, 0] = grass / sum;
                map[ay, ax, 1] = rock / sum;
                map[ay, ax, 2] = dirt / sum;
            }

        td.SetAlphamaps(0, 0, map);
    }

    static int _playFrames;

    /// <summary>Play 모드로 진입해 후처리·풀·물이 적용된 화면을 캡처(헤드리스).</summary>
    public static void CapturePlay()
    {
        EditorSettings.enterPlayModeOptionsEnabled = true;
        EditorSettings.enterPlayModeOptions = EnterPlayModeOptions.DisableDomainReload | EnterPlayModeOptions.DisableSceneReload;
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        _playFrames = 0;
        EditorApplication.update += PlayCaptureUpdate;
        EditorApplication.EnterPlaymode();
    }

    static void PlayCaptureUpdate()
    {
        if (!Application.isPlaying) return;
        _playFrames++;
        if (_playFrames < 50) return;
        EditorApplication.update -= PlayCaptureUpdate;

        var cam = Camera.main;
        if (cam != null)
        {
            const int W = 1024, H = 576;
            cam.aspect = (float)W / H;

            // 플레이어+적(Brute) 무리
            cam.transform.position = new Vector3(2f, 14f, -47f);
            cam.transform.LookAt(new Vector3(2f, 11f, -28f));

            var rt = new RenderTexture(W, H, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();
            cam.targetTexture = null;
            RenderTexture.active = null;
            var outPath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), "action3d_play.png");
            System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
            Debug.Log($"[Sommoje] play preview saved to {outPath}");
        }

        EditorApplication.Exit(0);
    }

    [MenuItem("Sommoje/Audit Scene Shaders")]
    public static void AuditShaders()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var seen = new System.Collections.Generic.HashSet<string>();
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            foreach (var m in r.sharedMaterials)
            {
                if (m == null) continue;
                string sh = m.shader != null ? m.shader.name : "<null>";
                if (sh.StartsWith("Universal Render Pipeline")) continue;   // URP는 정상
                string root = r.transform.root.name;
                string key = root + "|" + m.name + "|" + sh;
                if (seen.Add(key))
                    Debug.Log($"[Sommoje] NONURP root='{root}' go='{r.gameObject.name}' mat='{m.name}' shader='{sh}'");
            }
        Debug.Log("[Sommoje] shader audit done");
    }

    [MenuItem("Sommoje/Capture Action3D Preview")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var cam = Object.FindFirstObjectByType<Camera>();
        const int W = 1024, H = 576;
        cam.aspect = (float)W / H;

        // 지평선+하늘이 보이도록 거의 수평으로 프레이밍
        var pc = Object.FindFirstObjectByType<PlayerController3D>();
        if (pc != null)
        {
            var pp = pc.transform.position;
            cam.transform.position = pp + new Vector3(0f, 2.6f, -8.5f);
            cam.transform.rotation = Quaternion.Euler(6f, 0f, 0f);
        }

        var rt = new RenderTexture(W, H, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(W, H, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
        tex.Apply();
        cam.targetTexture = null;
        RenderTexture.active = null;

        var outPath = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(Application.dataPath), "action3d_preview.png");
        System.IO.File.WriteAllBytes(outPath, tex.EncodeToPNG());
        Debug.Log($"[Sommoje] Action3D preview saved to {outPath}");
    }
}
