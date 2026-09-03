using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GrenadeLauncherVisualTest.Editor
{
    // Deliberately editor-only. This project produces art assets; gameplay remains in the BepInEx mod.
    internal static class GrenadeLauncherVisualSetup
    {
        private const string ModelPath = "Assets/Source/FixedOne/Grenade launcher.obj";
        private const string GeneratedRoot = "Assets/Generated";
        private const string MaterialRoot = GeneratedRoot + "/Materials";
        private const string PrefabPath = GeneratedRoot + "/GrenadeLauncher_CustomPreview.prefab";

        [MenuItem("Grenade Launcher/Select Imported Model")]
        private static void SelectImportedModel()
        {
            Object model = AssetDatabase.LoadMainAssetAtPath(ModelPath);
            if (model == null)
            {
                Debug.LogWarning("Grenade Launcher model has not imported yet: " + ModelPath);
                return;
            }

            Selection.activeObject = model;
            EditorGUIUtility.PingObject(model);
        }

        [MenuItem("Grenade Launcher/Open Source Folder")]
        private static void OpenSourceFolder()
        {
            string path = Path.Combine(Application.dataPath, "Source");
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem("Grenade Launcher/Rebuild Colored Preview Prefab")]
        public static void RebuildColoredPreviewPrefab()
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
            if (source == null)
            {
                Debug.LogError("Model has not imported: " + ModelPath);
                return;
            }

            EnsureFolder(GeneratedRoot);
            EnsureFolder(MaterialRoot);
            GameObject preview = new GameObject("GrenadeLauncher_CustomPreview");
            GameObject importedModel = Object.Instantiate(source, preview.transform, false);
            importedModel.name = "Model";

            Dictionary<string, Color> palette = new Dictionary<string, Color>
            {
                // Reconstructed from the approved Roblox appearance, not from the OBJ's
                // broken white material conversion. Adjusting these does not touch the mesh.
                { "Backside1", Hex("071B2D") },
                { "Barrelveryblack1", Hex("071A2C") },
                { "Barrelveryblackstucktobody1", Hex("071A2C") },
                { "Barrellightblack1", Hex("102E47") },
                { "Bodygray1", Hex("74778D") },
                { "Part1", Hex("74778D") },
                { "Part2", Hex("74778D") },
                { "Bodyorange1", Hex("E9A84B") },
                { "Pannelrightorange1", Hex("E9A84B") },
                { "Pannelleftorange1", Hex("E9A84B") },
                { "Pannelbottomrightorange1", Hex("E9A84B") },
                { "Pannelrightgray1", Hex("697087") },
                { "Pannelleftgray1", Hex("697087") },
                { "Pannelbottomrightgray1", Hex("697087") },
                { "Cooldowndisplaybox1", Hex("F2F5F6") },
                { "Cooldowndisplayanchor1", Hex("202B3A") },
                { "Ironsightbottom1", Hex("E9A84B") },
                { "Ironsighttop1", Hex("E9A84B") },
            };

            foreach (Renderer renderer in preview.GetComponentsInChildren<Renderer>(true))
            {
                if (!palette.TryGetValue(renderer.name, out Color color))
                    continue;
                renderer.sharedMaterial = GetOrCreateMaterial(renderer.name, color);
            }

            // Roblox's OBJ export leaves vertices in its world-model coordinates rather
            // than placing the weapon around its object pivot. The vanilla RocketLauncher
            // mesh is centered around its own local X axis, so center this model as well;
            // runtime then maps it directly to the vanilla renderer's local bounds.
            Bounds bounds = GetBounds(importedModel);
            importedModel.transform.localPosition = -bounds.center;

            PrefabUtility.SaveAsPrefabAsset(preview, PrefabPath);
            Object.DestroyImmediate(preview);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Colored grenade-launcher preview created: " + PrefabPath);
        }

        [MenuItem("Grenade Launcher/Animation/Create Centered Pivot From Selected Chunks")]
        private static void CreateCenteredPivotFromSelectedChunks()
        {
            Transform[] selected = Selection.transforms;
            if (selected == null || selected.Length == 0)
            {
                Debug.LogWarning("Select one or more direct mesh chunks under Model first.");
                return;
            }

            Transform parent = selected[0].parent;
            if (parent == null)
            {
                Debug.LogWarning("Selected chunks need a shared Model parent.");
                return;
            }

            foreach (Transform transform in selected)
            {
                if (transform == null || transform.parent != parent)
                {
                    Debug.LogWarning("Select only sibling mesh chunks from the same Model.");
                    return;
                }
            }

            Bounds bounds = new Bounds();
            bool haveBounds = false;
            foreach (Transform transform in selected)
            {
                foreach (Renderer renderer in transform.GetComponentsInChildren<Renderer>(true))
                {
                    if (!haveBounds)
                    {
                        bounds = renderer.bounds;
                        haveBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            if (!haveBounds)
            {
                Debug.LogWarning("Selected chunks contain no renderers.");
                return;
            }

            GameObject pivot = new GameObject("AnimationPivot");
            Undo.RegisterCreatedObjectUndo(pivot, "Create grenade launcher animation pivot");
            pivot.transform.SetParent(parent, true);
            pivot.transform.position = bounds.center;
            pivot.transform.rotation = parent.rotation;

            foreach (Transform transform in selected)
                Undo.SetTransformParent(transform, pivot.transform, "Group grenade launcher animation chunks");

            Selection.activeGameObject = pivot;
            EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("Created " + pivot.name + " at the selected chunks' center. Rename it before animating.");
        }

        // The original white cooldown screen is no longer used: ULTRAKILL's dial will
        // be placed independently at runtime. Replace both screen meshes with a second
        // copy of the existing left plate, retaining the screen's parent pivot so all
        // authored weapon animations continue to move it with the body.
        [MenuItem("Grenade Launcher/Model/Replace Cooldown Box With Left Plate")]
        public static void ReplaceCooldownBoxWithLeftPlate()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform screen = FindDescendant(root.transform, "Cooldowndisplaybox1");
                Transform anchor = FindDescendant(root.transform, "Cooldowndisplayanchor1");
                Transform leftGray = FindDescendant(root.transform, "Pannelleftgray1");
                Transform leftOrange = FindDescendant(root.transform, "Pannelleftorange1");
                Transform existingReplacement = FindDescendant(root.transform, "CooldownBoxLeftPlate");
                if (screen == null && anchor == null && existingReplacement != null)
                {
                    // Idempotent after a successful replacement, including automated
                    // asset-bundle rebuilds on a later development pass.
                    Debug.Log("Cooldown box has already been replaced with the left plate.");
                    return;
                }
                if (screen == null || anchor == null || leftGray == null || leftOrange == null)
                {
                    Debug.LogError("Cooldown-box replacement needs Cooldowndisplaybox1, Cooldowndisplayanchor1, Pannelleftgray1, and Pannelleftorange1.");
                    return;
                }

                Transform parent = screen.parent;
                if (parent == null)
                {
                    Debug.LogError("Cooldown display has no parent pivot.");
                    return;
                }

                Vector3 screenLocalPosition = screen.localPosition;
                Quaternion screenLocalRotation = screen.localRotation;
                Vector3 screenLocalScale = screen.localScale;
                Transform oldReplacement = FindDescendant(parent, "CooldownBoxLeftPlate");
                if (oldReplacement != null)
                    Object.DestroyImmediate(oldReplacement.gameObject);

                GameObject replacementRoot = new GameObject("CooldownBoxLeftPlate");
                replacementRoot.transform.SetParent(parent, false);
                replacementRoot.transform.localPosition = Vector3.zero;
                replacementRoot.transform.localRotation = Quaternion.identity;
                replacementRoot.transform.localScale = Vector3.one;

                ClonePartAtScreen(leftGray, replacementRoot.transform, screenLocalPosition,
                    screenLocalRotation, screenLocalScale, "CooldownBoxLeftPlateGray");
                ClonePartAtScreen(leftOrange, replacementRoot.transform, screenLocalPosition,
                    screenLocalRotation, screenLocalScale, "CooldownBoxLeftPlateOrange");

                Object.DestroyImmediate(screen.gameObject);
                Object.DestroyImmediate(anchor.gameObject);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Replaced cooldown display with left plate at the original cooldown-box transform.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // The first automated replacement preserved the source parts in world space.
        // These chunks sit under different authored pivots, so their local coordinates
        // must instead be the old screen's local coordinates under the screen pivot.
        // Keep this repair command for the currently generated prefab; new replacements
        // use the correct local-space method above.
        [MenuItem("Grenade Launcher/Model/Repair Cooldown Left Plate Position")]
        public static void RepairCooldownLeftPlatePosition()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                Transform replacement = FindDescendant(root.transform, "CooldownBoxLeftPlate");
                Transform gray = FindDescendant(root.transform, "CooldownBoxLeftPlateGray");
                Transform orange = FindDescendant(root.transform, "CooldownBoxLeftPlateOrange");
                if (replacement == null || gray == null || orange == null)
                {
                    Debug.LogError("Cooldown left-plate repair needs the generated replacement objects.");
                    return;
                }

                // This OBJ stores each part's actual location in its mesh vertices while
                // its Transform is cancelled by an animation pivot. The old display mesh
                // is centred at X=+2.25; the source left plate is centred at X=-4.05.
                // With the BackLeftPad pivot at +2.25, a child local X of +4.05 gives the
                // plate its required +6.30 geometry shift and puts it exactly in the old
                // display position. Reusing the screen Transform here leaves it invisible
                // at the original left-panel location.
                Vector3 screenLocalPosition = new Vector3(4.04998f, -4.15683f, -18.5248f);
                Quaternion screenLocalRotation = Quaternion.identity;
                replacement.localPosition = Vector3.zero;
                replacement.localRotation = Quaternion.identity;
                replacement.localScale = Vector3.one;
                ApplyRecordedScreenTransform(gray, screenLocalPosition, screenLocalRotation);
                ApplyRecordedScreenTransform(orange, screenLocalPosition, screenLocalRotation);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("Repositioned cooldown left plate at the original cooldown-box transform.");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ApplyRecordedScreenTransform(Transform part, Vector3 localPosition,
            Quaternion localRotation)
        {
            part.localPosition = localPosition;
            part.localRotation = localRotation;
            part.localScale = Vector3.one;
        }

        private static void ClonePartAtScreen(Transform source, Transform replacementParent,
            Vector3 localPosition, Quaternion localRotation, Vector3 localScale, string name)
        {
            GameObject clone = Object.Instantiate(source.gameObject);
            clone.name = name;
            clone.transform.SetParent(replacementParent, false);
            clone.transform.localPosition = localPosition;
            clone.transform.localRotation = localRotation;
            clone.transform.localScale = localScale;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
                return null;
            foreach (Transform item in root.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == name)
                    return item;
            }
            return null;
        }

        private static Material GetOrCreateMaterial(string name, Color color)
        {
            string path = MaterialRoot + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = color;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0.18f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString("#" + hex, out Color color);
            return color;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static Bounds GetBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(Vector3.zero, Vector3.one);

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }
    }
}
