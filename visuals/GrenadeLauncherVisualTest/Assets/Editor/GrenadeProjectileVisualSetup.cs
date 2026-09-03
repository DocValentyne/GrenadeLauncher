using System.IO;
using UnityEditor;
using UnityEngine;

namespace GrenadeLauncherVisualTest.Editor
{
    internal static class GrenadeProjectileVisualSetup
    {
        private const string ModelPath = "Assets/Source/GrenadeProjectiles/GrenadeProjectile.obj";
        private const string OutputRoot = "Assets/Generated/Projectiles";

        [MenuItem("Grenade Launcher/Rebuild Projectile Visual Prefabs")]
        public static void Rebuild()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null)
            {
                Debug.LogError("Projectile OBJ has not imported yet: " + ModelPath);
                return;
            }
            EnsureFolder(OutputRoot);
            Build(source, "GrenadeProjectilePrimary", "GrenadeProjectile_Orange");
            Build(source, "GrenadeProjectileGreen", "GrenadeProjectile_Green");
            Build(source, "GrenadeProjectileRed", "GrenadeProjectile_Red");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Created orange, green, and red Grenade Launcher projectile prefabs.");
        }

        private static void Build(GameObject source, string prefabName, string textureName)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Source/GrenadeProjectiles/" + textureName + ".bmp");
            if (texture == null)
            {
                Debug.LogError("Projectile texture missing: " + textureName);
                return;
            }
            GameObject root = new GameObject(prefabName);
            GameObject mesh = Object.Instantiate(source, root.transform, false);
            mesh.name = "Model";
            string materialPath = OutputRoot + "/" + prefabName + " Material.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.name = prefabName + " Material";
            material.mainTexture = texture;
            material.color = Color.white;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.18f);
            EditorUtility.SetDirty(material);
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(root, OutputRoot + "/" + prefabName + ".prefab");
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
