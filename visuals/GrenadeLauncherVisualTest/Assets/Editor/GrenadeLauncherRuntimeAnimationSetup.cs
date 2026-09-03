using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace GrenadeLauncherVisualTest.Editor
{
    // Preserves artist-authored clips/pivots while adding the minimal Animator component
    // required for the BepInEx runtime to play them by state name.
    internal static class GrenadeLauncherRuntimeAnimationSetup
    {
        private const string PrefabPath = "Assets/Generated/GrenadeLauncher_CustomPreview.prefab";
        private const string ControllerPath = "Assets/Generated/Animations/GrenadeLauncher_AnimationRig.controller";
        private const string EquipClipPath = "Assets/Generated/Animations/Equip.anim";
        private const string IdleClipPath = "Assets/Generated/Animations/Idle.anim";
        private const string PrimaryClipPath = "Assets/Generated/Animations/PrimaryFire.anim";
        private const string AltClipPath = "Assets/Generated/Animations/AltFire.anim";
        private const string RedGelClipPath = "Assets/Generated/Animations/RedGel.anim";
        private const string OverheatClipPath = "Assets/Generated/Animations/Overheat.anim";
        private static readonly string[] OneShotClipPaths =
        {
            "Assets/Generated/Animations/PrimaryFire.anim",
            "Assets/Generated/Animations/AltFire.anim",
            "Assets/Generated/Animations/Overheat.anim",
        };

        [MenuItem("Grenade Launcher/Animation/Prepare Prefab for Runtime")]
        internal static void PreparePrefabForRuntime()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError("Missing animation controller: " + ControllerPath);
                return;
            }

            EnsureIdleState(controller);
            EnsureState(controller, "Equip", EquipClipPath);
            EnsureState(controller, "Overheat", OverheatClipPath);
            // RedGel is kept in the controller as an artist-authored fallback. Runtime
            // currently uses a procedural shake instead, but building must never erase it.
            EnsureState(controller, "RedGel", RedGelClipPath);
            EnsureOneShotClipSettings();
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Animator animator = root.GetComponent<Animator>();
                if (animator == null)
                    animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Prepared Grenade Launcher prefab for all runtime animation states.");
        }

        // Copies clip data in Unity rather than through Explorer. PrimaryFire keeps its own
        // asset identity/controller reference, so edits to either clip stay independent.
        internal static void DuplicateAltFireAsPrimary()
        {
            AnimationClip source = AssetDatabase.LoadAssetAtPath<AnimationClip>(AltClipPath);
            AnimationClip destination = AssetDatabase.LoadAssetAtPath<AnimationClip>(PrimaryClipPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (source == null || destination == null || controller == null)
            {
                Debug.LogError("Cannot copy AltFire into PrimaryFire: required animation asset is missing.");
                return;
            }

            EditorUtility.CopySerialized(source, destination);
            destination.name = "PrimaryFire";
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == "PrimaryFire")
                    child.state.motion = destination;
            }
            EditorUtility.SetDirty(destination);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("Copied AltFire animation into independent PrimaryFire asset.");
        }

        private static void EnsureIdleState(AnimatorController controller)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            if (layers == null || layers.Length == 0)
                return;

            AnimatorStateMachine machine = layers[0].stateMachine;
            AnimatorState idle = null;
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == "Idle")
                {
                    idle = child.state;
                    break;
                }
            }
            if (idle == null)
                idle = machine.AddState("Idle", new Vector3(80f, 180f, 0f));
            AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleClipPath);
            idle.motion = idleClip;
            machine.defaultState = idle;
            EditorUtility.SetDirty(controller);
        }

        private static void EnsureState(AnimatorController controller, string stateName, string clipPath)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
            if (clip == null)
                return;
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state != null && child.state.name == stateName)
                {
                    child.state.motion = clip;
                    EditorUtility.SetDirty(controller);
                    return;
                }
            }
            AnimatorState state = machine.AddState(stateName, new Vector3(280f, 180f, 0f));
            state.motion = clip;
            EditorUtility.SetDirty(controller);
        }

        private static void EnsureOneShotClipSettings()
        {
            foreach (string path in OneShotClipPaths)
            {
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                if (clip == null)
                    continue;

                SerializedObject serialized = new SerializedObject(clip);
                SerializedProperty loopTime = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
                if (loopTime != null && loopTime.boolValue)
                {
                    loopTime.boolValue = false;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                // Viewmodel root is positioned by BepInEx calibration. A root transform
                // curve from Unity's scene editor moves whole weapon into camera/shoulder.
                // Keep authored child-pivot animation only.
                foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (string.IsNullOrEmpty(binding.path))
                        AnimationUtility.SetEditorCurve(clip, binding, null);
                }
                EditorUtility.SetDirty(clip);
            }
        }
    }
}
