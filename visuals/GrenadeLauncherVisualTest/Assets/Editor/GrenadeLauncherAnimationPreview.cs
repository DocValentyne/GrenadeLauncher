using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GrenadeLauncherVisualTest.Editor
{
    // Builds an artist-facing scene from one measured ULTRAKILL viewmodel capture.
    // This controls framing only; game lighting is deliberately not reproduced here.
    internal static class GrenadeLauncherAnimationPreview
    {
        private const string PrefabPath = "Assets/Generated/GrenadeLauncher_CustomPreview.prefab";
        private const string ScenePath = "Assets/Generated/GrenadeLauncher_AnimationPreview.unity";

        [MenuItem("Grenade Launcher/Animation/Open or Refresh In-Game Animation Preview")]
        private static void OpenOrRefresh()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Debug.LogError("Missing preview prefab: " + PrefabPath);
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera(scene);
            CreateLighting(scene);

            GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            rig.name = "GrenadeLauncher_AnimationRig";
            rig.transform.position = new Vector3(0.30952f, -0.58484f, 0.71270f);
            rig.transform.rotation = Quaternion.Euler(0.00001f, 85f, 355f);
            rig.transform.localScale = Vector3.one * 0.1f;

            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = rig;
            Debug.Log("Opened calibrated animation preview. Animate the rig's child pivots; do not move the rig root.");
        }

        private static void CreateCamera(Scene scene)
        {
            GameObject cameraObject = new GameObject("In-Game Viewmodel Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.fieldOfView = 105f;
            camera.nearClipPlane = 0.01f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.11f, 0.13f, 0.16f);
        }

        private static void CreateLighting(Scene scene)
        {
            RenderSettings.ambientLight = new Color(0.28f, 0.3f, 0.35f);
            GameObject lightObject = new GameObject("Preview Key Light");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(35f, -35f, 0f);
        }
    }
}
