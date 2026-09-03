using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using System.IO;
using System.Collections.Generic;

namespace GrenadeLauncherVisualTest.Editor
{
    public static class GrenadeLauncherVisualDiagnostics
    {
        private const string ModelPath = "Assets/Source/FixedOne/Grenade launcher.obj";

        public static void PrintImportDiagnostics()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
            {
                Debug.LogError("Could not load " + ModelPath);
                return;
            }

            Debug.Log("Grenade Launcher OBJ root: " + model.name);
            PrintTransform(model.transform, 0);
        }

        public static void PrintDisconnectedMeshComponents()
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (model == null)
                return;

            foreach (MeshFilter filter in model.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                    continue;
                Debug.Log(filter.name + " has " + CountDisconnectedComponents(mesh) +
                          " disconnected mesh components (" + mesh.vertexCount + " vertices).");
            }
        }

        public static void PrintUltrakillHookAssets()
        {
            const string bundlePath = "D:/Games/Steam/steamapps/common/ULTRAKILL/ULTRAKILL_Data/StreamingAssets/aa/StandaloneWindows64/gameprefabs_assets_all.bundle";
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogError("Could not load ULTRAKILL gameprefabs bundle.");
                return;
            }
            foreach (string asset in bundle.GetAllAssetNames())
            {
                if (asset.IndexOf("grapple", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    asset.IndexOf("hookpoint", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    Debug.Log("ULTRAKILL hook asset: " + asset);
            }
            foreach (GameObject asset in bundle.LoadAllAssets<GameObject>())
            {
                if (asset != null && (asset.name.IndexOf("grapple", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    asset.name.IndexOf("hook", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    Debug.Log("ULTRAKILL hook prefab: " + asset.name);
            }
            bundle.Unload(false);
        }

        // One-shot editor diagnostic used to identify exact Flesh Prison heal assets.
        // Keep output small: only effect-capable children and audio are relevant.
        public static void PrintFleshPrisonHealAssets()
        {
            const string bundlePath = "D:/Games/Steam/steamapps/common/ULTRAKILL/ULTRAKILL_Data/StreamingAssets/aa/StandaloneWindows64/gameprefabs_assets_all.bundle";
            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);
            if (bundle == null)
            {
                Debug.LogError("Could not load ULTRAKILL gameprefabs bundle.");
                return;
            }

            foreach (string assetName in bundle.GetAllAssetNames())
            {
                if (assetName.IndexOf("fleshprison", System.StringComparison.OrdinalIgnoreCase) < 0 &&
                    assetName.IndexOf("flesh prison", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                GameObject prefab = bundle.LoadAsset<GameObject>(assetName);
                if (prefab == null)
                    continue;
                Debug.Log("Flesh Prison prefab: " + assetName + " / " + prefab.name);
                foreach (ParticleSystem particles in prefab.GetComponentsInChildren<ParticleSystem>(true))
                    Debug.Log("  particles: " + GetPath(particles.transform));
                foreach (LineRenderer line in prefab.GetComponentsInChildren<LineRenderer>(true))
                    Debug.Log("  line: " + GetPath(line.transform) + " material=" + (line.sharedMaterial != null ? line.sharedMaterial.name : "<none>"));
                foreach (AudioSource audio in prefab.GetComponentsInChildren<AudioSource>(true))
                    Debug.Log("  audio: " + GetPath(audio.transform) + " clip=" + (audio.clip != null ? audio.clip.name : "<none>"));
            }
            bundle.Unload(false);
        }

        public static void PrintProjectileVisualDiagnostics()
        {
            foreach (string path in new[]
            {
                "Assets/Generated/Projectiles/GrenadeProjectilePrimary.prefab",
                "Assets/Generated/Projectiles/GrenadeProjectileGreen.prefab",
                "Assets/Generated/Projectiles/GrenadeProjectileRed.prefab"
            })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                MeshFilter filter = prefab != null ? prefab.GetComponentInChildren<MeshFilter>(true) : null;
                Debug.Log("Projectile " + path + " mesh=" + (filter?.sharedMesh != null ? filter.sharedMesh.bounds.ToString() : "<missing>") +
                    " rootRotation=" + (prefab != null ? prefab.transform.localEulerAngles.ToString() : "<missing>"));
            }
        }

        public static void RenderPreview()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null)
            {
                Debug.LogError("Could not load " + ModelPath);
                return;
            }

            Scene scene = EditorSceneManager.NewPreviewScene();
            GameObject weapon = Object.Instantiate(source);
            SceneManager.MoveGameObjectToScene(weapon, scene);
            Bounds bounds = GetBounds(weapon);
            Vector3 center = bounds.center;
            float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
            Debug.Log("Grenade Launcher preview bounds: center=" + center + " size=" + bounds.size + " radius=" + radius);
            foreach (Renderer renderer in weapon.GetComponentsInChildren<Renderer>())
            {
                string materialInfo = renderer.sharedMaterial == null
                    ? "<none>"
                    : renderer.sharedMaterial.name + " shader=" + renderer.sharedMaterial.shader.name;
                Debug.Log("Preview renderer " + renderer.name + " bounds=" + renderer.bounds + " material=" + materialInfo);
            }

            CreateDirectionalLight(scene, new Vector3(35f, -30f, 0f), 1.15f, Color.white);
            CreateDirectionalLight(scene, new Vector3(45f, 145f, 0f), 0.45f, new Color(0.6f, 0.75f, 1f));

            GameObject cameraObject = new GameObject("Preview Camera");
            SceneManager.MoveGameObjectToScene(cameraObject, scene);
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.backgroundColor = new Color(0.055f, 0.07f, 0.1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.fieldOfView = 38f;
            camera.nearClipPlane = 0.001f;
            camera.farClipPlane = Mathf.Max(1000f, radius * 20f);

            Vector3 viewDirection = new Vector3(-1.7f, 0.8f, -2.3f).normalized;
            float distance = radius / Mathf.Sin(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * 1.12f;
            camera.transform.position = center + viewDirection * distance;
            camera.transform.LookAt(center);

            const int width = 1280;
            const int height = 720;
            RenderTexture target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            Texture2D image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();

            string outputDirectory = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PreviewRenders");
            Directory.CreateDirectory(outputDirectory);
            string outputPath = Path.Combine(outputDirectory, "grenade-launcher-unity-preview.png");
            File.WriteAllBytes(outputPath, image.EncodeToPNG());
            Debug.Log("Grenade Launcher preview written to: " + outputPath);

            Object.DestroyImmediate(image);
            Object.DestroyImmediate(target);
            EditorSceneManager.ClosePreviewScene(scene);
        }

        private static Bounds GetBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(root.transform.position, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        private static void CreateDirectionalLight(Scene scene, Vector3 rotation, float intensity, Color color)
        {
            GameObject lightObject = new GameObject("Preview Light");
            SceneManager.MoveGameObjectToScene(lightObject, scene);
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = intensity;
            light.color = color;
            lightObject.transform.rotation = Quaternion.Euler(rotation);
        }

        private static int CountDisconnectedComponents(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            List<int>[] adjacency = new List<int>[mesh.vertexCount];
            for (int i = 0; i < adjacency.Length; i++)
                adjacency[i] = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                adjacency[a].Add(b); adjacency[a].Add(c);
                adjacency[b].Add(a); adjacency[b].Add(c);
                adjacency[c].Add(a); adjacency[c].Add(b);
            }

            bool[] visited = new bool[mesh.vertexCount];
            int count = 0;
            Stack<int> pending = new Stack<int>();
            for (int start = 0; start < adjacency.Length; start++)
            {
                if (visited[start] || adjacency[start].Count == 0)
                    continue;
                count++;
                visited[start] = true;
                pending.Push(start);
                while (pending.Count > 0)
                {
                    int current = pending.Pop();
                    foreach (int next in adjacency[current])
                    {
                        if (visited[next])
                            continue;
                        visited[next] = true;
                        pending.Push(next);
                    }
                }
            }
            return count;
        }

        private static void PrintTransform(Transform node, int depth)
        {
            Renderer renderer = node.GetComponent<Renderer>();
            MeshFilter meshFilter = node.GetComponent<MeshFilter>();
            string indent = new string(' ', depth * 2);
            string rendererInfo = renderer == null
                ? ""
                : " | renderer=" + renderer.GetType().Name + " materials=" + renderer.sharedMaterials.Length;
            string meshInfo = meshFilter == null || meshFilter.sharedMesh == null
                ? ""
                : " | meshCenter=" + meshFilter.sharedMesh.bounds.center + " meshSize=" + meshFilter.sharedMesh.bounds.size;
            Debug.Log(indent + node.name + " | position=" + node.localPosition + " scale=" + node.localScale + rendererInfo + meshInfo);

            for (int i = 0; i < node.childCount; i++)
                PrintTransform(node.GetChild(i), depth + 1);
        }

        private static string GetPath(Transform target)
        {
            List<string> parts = new List<string>();
            for (Transform current = target; current != null; current = current.parent)
                parts.Add(current.name);
            parts.Reverse();
            return string.Join("/", parts.ToArray());
        }
    }
}
