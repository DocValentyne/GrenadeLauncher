using System.IO;
using UnityEditor;

namespace GrenadeLauncherVisualTest.Editor
{
    internal static class GrenadeLauncherAssetBundleBuilder
    {
        private const string PrefabPath = "Assets/Generated/GrenadeLauncher_CustomPreview.prefab";
        private static readonly string[] ProjectilePrefabPaths =
        {
            "Assets/Generated/Projectiles/GrenadeProjectilePrimary.prefab",
            "Assets/Generated/Projectiles/GrenadeProjectileGreen.prefab",
            "Assets/Generated/Projectiles/GrenadeProjectileRed.prefab",
        };
        private const string BundleName = "grenadelauncher_visuals";

        [MenuItem("Grenade Launcher/Build ULTRAKILL Visual Bundle")]
        public static void BuildUltrakillVisualBundle()
        {
            // Projectile source can change independently of weapon model. Rebuild its
            // prefabs first so bundle never silently ships an older imported OBJ/UV map.
            GrenadeProjectileVisualSetup.Rebuild();
            GrenadeLauncherRuntimeAnimationSetup.PreparePrefabForRuntime();
            AssetImporter importer = AssetImporter.GetAtPath(PrefabPath);
            if (importer == null)
                throw new FileNotFoundException("Build the colored preview prefab before building its AssetBundle.", PrefabPath);

            importer.assetBundleName = BundleName;
            importer.assetBundleVariant = string.Empty;
            foreach (string projectilePath in ProjectilePrefabPaths)
            {
                AssetImporter projectileImporter = AssetImporter.GetAtPath(projectilePath);
                if (projectileImporter == null)
                    throw new FileNotFoundException("Create grenade projectile prefabs before building the AssetBundle.", projectilePath);
                projectileImporter.assetBundleName = BundleName;
                projectileImporter.assetBundleVariant = string.Empty;
            }
            AssetDatabase.SaveAssets();

            string projectRoot = Directory.GetParent(UnityEngine.Application.dataPath).FullName;
            string output = Path.Combine(projectRoot, "BuiltAssetBundles");
            Directory.CreateDirectory(output);
            BuildPipeline.BuildAssetBundles(output, BuildAssetBundleOptions.ChunkBasedCompression,
                BuildTarget.StandaloneWindows64);
        }

        internal static void DuplicateAltFireAsPrimaryAndBuild()
        {
            GrenadeLauncherRuntimeAnimationSetup.DuplicateAltFireAsPrimary();
            BuildUltrakillVisualBundle();
        }
    }
}
