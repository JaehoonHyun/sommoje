using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Mixamo 캐릭터(Brute) + 애니메이션을 Humanoid로 임포트하고 Animator 컨트롤러를 만든다.
/// 메뉴: Sommoje ▸ Setup Mixamo Character
/// </summary>
public static class MixamoSetup
{
    const string Dir = "Assets/Art/Character";

    [MenuItem("Sommoje/Setup Erika")]
    public static void SetupErika()
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath($"{Dir}/Erika.fbx");
        if (imp == null) { Debug.LogError("[Sommoje] Erika.fbx not found"); return; }
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        imp.importAnimation = false;
        imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        imp.SaveAndReimport();

        const string texDir = Dir + "/ErikaTextures";
        if (!AssetDatabase.IsValidFolder(texDir)) AssetDatabase.CreateFolder(Dir, "ErikaTextures");
        bool ok = imp.ExtractTextures(texDir);
        AssetDatabase.Refresh();
        imp.SaveAndReimport();
        Debug.Log($"[Sommoje] Erika setup done. ExtractTextures={ok}");
    }

    [MenuItem("Sommoje/Setup Erika Locomotion")]
    public static void SetupErikaLocomotion()
    {
        Avatar avatar = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath($"{Dir}/Erika.fbx"))
            if (a is Avatar av) avatar = av;
        if (avatar == null) { Debug.LogError("[Sommoje] Erika avatar 없음 (Setup Erika 먼저)"); return; }

        SetupAnim("Erika_Idle", avatar, true);
        SetupAnim("Erika_Walk", avatar, true);
        SetupAnim("Erika_Run", avatar, true);
        SetupAnim("Erika_Attack", avatar, false);
        SetupAnim("Erika_Jump", avatar, false);

        const string ctrlPath = Dir + "/PlayerAnimator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        ctrl.AddParameter("Jump", AnimatorControllerParameterType.Trigger);

        var loco = ctrl.CreateBlendTreeInController("Locomotion", out var bt, 0);
        bt.blendType = BlendTreeType.Simple1D;
        bt.blendParameter = "Speed";
        bt.useAutomaticThresholds = false;
        bt.AddChild(GetClip("Erika_Idle"), 0f);
        bt.AddChild(GetClip("Erika_Walk"), 2f);
        bt.AddChild(GetClip("Erika_Run"), 5f);

        var sm = ctrl.layers[0].stateMachine;
        sm.defaultState = loco;

        var atk = sm.AddState("Attack");
        atk.motion = GetClip("Erika_Attack");
        var toAtk = sm.AddAnyStateTransition(atk);
        toAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAtk.hasExitTime = false; toAtk.duration = 0.05f; toAtk.canTransitionToSelf = false;
        var done = atk.AddTransition(loco);
        done.hasExitTime = true; done.exitTime = 0.7f; done.duration = 0.15f;

        var jump = sm.AddState("Jump");
        jump.motion = GetClip("Erika_Jump");
        var toJump = sm.AddAnyStateTransition(jump);
        toJump.AddCondition(AnimatorConditionMode.If, 0, "Jump");
        toJump.hasExitTime = false; toJump.duration = 0.05f; toJump.canTransitionToSelf = false;
        var jumpDone = jump.AddTransition(loco);
        jumpDone.hasExitTime = true; jumpDone.exitTime = 0.8f; jumpDone.duration = 0.15f;

        AssetDatabase.SaveAssets();
        Debug.Log($"[Sommoje] Erika locomotion built. idle={GetClip("Erika_Idle") != null} walk={GetClip("Erika_Walk") != null} " +
                  $"run={GetClip("Erika_Run") != null} atk={GetClip("Erika_Attack") != null}");
    }

    [MenuItem("Sommoje/Setup Mixamo Character")]
    public static void Setup()
    {
        // 1) 캐릭터: Humanoid, 이 모델에서 아바타 생성
        var charImp = (ModelImporter)AssetImporter.GetAtPath($"{Dir}/Brute.fbx");
        charImp.animationType = ModelImporterAnimationType.Human;
        charImp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        charImp.importAnimation = false;
        charImp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        charImp.SaveAndReimport();

        // 임베디드 텍스처/머티리얼 추출 (하얀 석고상 방지)
        const string texDir = Dir + "/Textures";
        if (!AssetDatabase.IsValidFolder(texDir)) AssetDatabase.CreateFolder(Dir, "Textures");
        bool texOk = charImp.ExtractTextures(texDir);
        AssetDatabase.Refresh();
        charImp.SaveAndReimport();
        Debug.Log($"[Sommoje] ExtractTextures={texOk}");

        Avatar charAvatar = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath($"{Dir}/Brute.fbx"))
            if (a is Avatar av) charAvatar = av;
        if (charAvatar == null) { Debug.LogError("[Sommoje] Brute avatar not created"); return; }

        // 2) 애니메이션: Humanoid + 아바타 복사 + 루프
        SetupAnim("Idle", charAvatar, true);
        SetupAnim("Walking", charAvatar, true);
        SetupAnim("Attack", charAvatar, false);

        // 3) Animator 컨트롤러
        const string ctrlPath = Dir + "/PlayerAnimator.controller";
        AssetDatabase.DeleteAsset(ctrlPath);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ctrlPath);
        ctrl.AddParameter("Moving", AnimatorControllerParameterType.Bool);
        ctrl.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        var sm = ctrl.layers[0].stateMachine;
        var idle = sm.AddState("Idle"); idle.motion = GetClip("Idle");
        var walk = sm.AddState("Walk"); walk.motion = GetClip("Walking");
        var atk = sm.AddState("Attack"); atk.motion = GetClip("Attack");
        sm.defaultState = idle;

        var toWalk = idle.AddTransition(walk);
        toWalk.AddCondition(AnimatorConditionMode.If, 0, "Moving");
        toWalk.hasExitTime = false; toWalk.duration = 0.1f;

        var toIdle = walk.AddTransition(idle);
        toIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "Moving");
        toIdle.hasExitTime = false; toIdle.duration = 0.1f;

        var toAtk = sm.AddAnyStateTransition(atk);
        toAtk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
        toAtk.hasExitTime = false; toAtk.duration = 0.05f; toAtk.canTransitionToSelf = false;

        var atkDone = atk.AddTransition(idle);
        atkDone.hasExitTime = true; atkDone.exitTime = 0.75f; atkDone.duration = 0.15f;

        AssetDatabase.SaveAssets();
        Debug.Log($"[Sommoje] Mixamo setup done. avatar={charAvatar.name}, controller={ctrlPath}, " +
                  $"clips: idle={GetClip("Idle") != null} walk={GetClip("Walking") != null} atk={GetClip("Attack") != null}");
    }

    static void SetupAnim(string name, Avatar src, bool loop)
    {
        var imp = (ModelImporter)AssetImporter.GetAtPath($"{Dir}/{name}.fbx");
        imp.animationType = ModelImporterAnimationType.Human;
        imp.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
        imp.sourceAvatar = src;

        var clips = imp.clipAnimations;
        if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
        for (int i = 0; i < clips.Length; i++) clips[i].loopTime = loop;
        imp.clipAnimations = clips;

        imp.SaveAndReimport();
    }

    static AnimationClip GetClip(string name)
    {
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath($"{Dir}/{name}.fbx"))
            if (a is AnimationClip c && !c.name.StartsWith("__preview")) return c;
        return null;
    }
}
