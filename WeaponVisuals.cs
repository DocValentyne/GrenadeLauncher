using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;

namespace GrenadeLauncherMod
{
    // This is deliberately a visual-only first pass.  It keeps the custom model isolated
    // from grenade mechanics while position, scale, and material behavior are calibrated.
    internal sealed class WeaponVisualRuntime : MonoBehaviour
    {
        private const string BundleResourceName = "GrenadeLauncherMod.visuals";
        private const string PrefabName = "GrenadeLauncher_CustomPreview";

        private static FloatSliderField modelScale;
        private static FloatSliderField offsetX;
        private static FloatSliderField offsetY;
        private static FloatSliderField offsetZ;
        private static FloatSliderField rotationX;
        private static FloatSliderField rotationY;
        private static FloatSliderField rotationZ;
        private static ColorField orangeColor;
        private static ColorField silverColor;
        private static ColorField blackColor;
        private static ColorField deepBlackColor;

        // Final cooldown-dial calibration, supplied during in-game testing. The dial is
        // now part of the custom viewmodel, so these do not need to be player settings.
        private static readonly Vector3 CooldownDialModelAnchor = new Vector3(-2.11f, 0.17f, 20.48f);
        private static readonly Vector3 CooldownDialOffset = new Vector3(0f, 4.5f, 0f);
        private static readonly Vector3 CooldownDialRotation = new Vector3(0f, -30f, 0f);
        private const float CooldownDialScale = 1.4f;
        private static readonly Color DefaultOrange = new Color(0.6406491f, 0.4972856f, 0.2595037f, 1f);
        private static readonly Color DefaultSilver = new Color(0.254902f, 0.254902f, 0.314481f, 1f);
        private static readonly Color DefaultBlack = new Color(0.0627451f, 0.18039216f, 0.2784314f, 1f);
        private static readonly Color DefaultDeepBlack = new Color(0.02745098f, 0.101960786f, 0.17254902f, 1f);
        // Final calibrated idle motion from the user's in-game testing. The authored
        // clip has a deliberately subtle midpoint, so this scale is baked rather than
        // exposed as a permanent player setting.
        private const float IdleVerticalMotionScale = 10f;
        private const float CustomShotVolume = 0.85f;
        // Approved red-gel shake tuning. These are deliberately authored behavior rather
        // than player settings; they are not weapon-balance controls.
        private const float RedGelShotGrace = 0.3f;
        private const float RedGelShakeBlendTime = 0.08f;
        private const float RedGelNoiseSpeed = 14f;
        private const float RedGelShakeHorizontal = 0.02f;
        private const float RedGelShakeVertical = 0.02f;
        private const float RedGelShakeDepth = 0.02f;
        private const float RedGelShakePitch = 0f;
        private const float RedGelShakeYaw = 0f;
        private const float RedGelShakeRoll = 0f;
        private const float RedGelBarrelSpinSpeed = 4f;
        private static BoolField useVanillaVisuals;
        private static readonly FieldInfo RocketShotAudioField = AccessTools.Field(typeof(RocketLauncher), "aud");

        private readonly Dictionary<RocketLauncher, WeaponVisualInstance> instances =
            new Dictionary<RocketLauncher, WeaponVisualInstance>();
        private static WeaponVisualRuntime activeRuntime;
        private AssetBundle visualBundle;
        private GameObject visualPrefab;
        private float nextCleanupScan;

        internal static void InitializeConfiguration(PluginConfigurator configurator)
        {
            ConfigPanel panel = new ConfigPanel(configurator.rootPanel,
                "Visual settings", "visualSettings");
            panel.headerText = "Custom Grenade Launcher model controls. These do not change grenade mechanics.";
            modelScale = Slider(panel, "Model scale", "visualModelScale", 0.001f, 1f, 0.09f, 3);
            offsetX = Slider(panel, "Model horizontal offset", "visualOffsetX", -10f, 10f, 0.5f, 3);
            offsetY = Slider(panel, "Model vertical offset", "visualOffsetY", -10f, 10f, 0f, 3);
            offsetZ = Slider(panel, "Model depth offset", "visualOffsetZ", -10f, 10f, -0.13f, 3);
            rotationX = Slider(panel, "Model rotation X", "visualRotationX", -180f, 180f, -90f, 1);
            rotationY = Slider(panel, "Model rotation Y", "visualRotationY", -180f, 180f, 0f, 1);
            rotationZ = Slider(panel, "Model rotation Z", "visualRotationZ", -180f, 180f, 180f, 1);

            ConfigPanel colors = new ConfigPanel(panel, "Colors", "visualColors");
            colors.headerText = "Repaint the custom grenade-launcher model.";
            orangeColor = new ColorField(colors, "Orange color", "visualOrangeColor", DefaultOrange);
            silverColor = new ColorField(colors, "Silver color", "visualSilverColor", DefaultSilver);
            blackColor = new ColorField(colors, "Black color", "visualBlackColor", DefaultBlack);
            deepBlackColor = new ColorField(colors, "Deep black color", "visualDeepBlackColor", DefaultDeepBlack);

            // Material and transform updates are event-driven. Do not rewrite every
            // material every game frame merely because a color page is available.
            orangeColor.onValueChange += _ => activeRuntime?.ApplyPaintConfigurationToInstances();
            silverColor.onValueChange += _ => activeRuntime?.ApplyPaintConfigurationToInstances();
            blackColor.onValueChange += _ => activeRuntime?.ApplyPaintConfigurationToInstances();
            deepBlackColor.onValueChange += _ => activeRuntime?.ApplyPaintConfigurationToInstances();
            modelScale.onValueChange += _ => activeRuntime?.ApplyCooldownDialSettingsToInstances();
            useVanillaVisuals = new BoolField(panel, "Use vanilla rocket visuals", "useVanillaVisuals", false);
            ApplyInitialCalibrationDefaults();
        }

        internal static bool UseVanillaVisuals => useVanillaVisuals?.value ?? false;
        internal const float ProjectileForwardOffset = 4.1f;

        internal static AudioSource GetVanillaShotAudio(RocketLauncher launcher)
        {
            return launcher != null ? RocketShotAudioField?.GetValue(launcher) as AudioSource : null;
        }

        internal static bool ShouldReplaceVanillaShotFeedback(RocketLauncher launcher)
        {
            return launcher != null && Plugin.Instance != null &&
                   Plugin.Instance.IsGrenadeModeEnabled(launcher) && !UseVanillaVisuals;
        }

        private static FloatSliderField Slider(ConfigPanel panel, string name, string guid,
            float minimum, float maximum, float defaultValue, int decimals)
        {
            return new FloatSliderField(panel, name, guid,
                new Tuple<float, float>(minimum, maximum), defaultValue, decimals);
        }

        private static void ApplyInitialCalibrationDefaults()
        {
            // This development-only migration replaces the initial guessed calibration with
            // the measured bounds of ULTRAKILL's vanilla RocketLauncher mesh. It will not
            // run again after this visual-test step.
            GrenadeLauncherState state = Plugin.Instance != null ? Plugin.Instance.State : null;
            if (state == null || state.VisualCalibrationDefaultsVersion >= 3)
                return;

            if (state.VisualCalibrationDefaultsVersion < 2)
            {
                modelScale.value = 0.1f;
                offsetX.value = 0.5f;
                offsetY.value = 0f;
                rotationX.value = -90f;
                rotationY.value = 0f;
                rotationZ.value = 180f;
            }

            // The original depth estimate left the custom mesh too near V1's face.
            // Only replace that exact old default; preserve player calibration choices.
            if (Mathf.Abs(offsetZ.value - (-0.01f)) < 0.0001f)
                offsetZ.value = -0.13f;
            state.VisualCalibrationDefaultsVersion = 3;
            state.Save();
        }

        private void Awake()
        {
            activeRuntime = this;
            GrenadeLauncherShotSoundLibrary.Preload();
            // The correctly-layered custom mesh is rendered by ULTRAKILL's viewmodel
            // camera. That camera can apply its final weapon pose after LateUpdate, so
            // authored root animation must also be rebased once immediately before draw.
            Camera.onPreCull += ApplyFinalViewmodelPoses;
            LoadVisualPrefab();
        }

        private void Update()
        {
            if (visualPrefab == null)
                return;

            // Creation is handled by RocketLauncher.OnEnable below. Keep only a low-frequency
            // fallback/cleanup scan in case another mod enables a launcher before our hook.
            if (Time.unscaledTime >= nextCleanupScan)
            {
                nextCleanupScan = Time.unscaledTime + 1f;
                ScanLaunchers();
            }

            foreach (WeaponVisualInstance instance in instances.Values)
                instance?.Refresh();
        }

        private void LateUpdate()
        {
            // Animator evaluation can happen after Update on some ULTRAKILL viewmodel
            // paths. Reapply authored barrel-only loop after it, without touching pads
            // or body transforms from primary-fire animation.
            foreach (WeaponVisualInstance instance in instances.Values)
                instance?.LateRefresh();
        }

        private void OnDestroy()
        {
            Camera.onPreCull -= ApplyFinalViewmodelPoses;
            foreach (WeaponVisualInstance instance in instances.Values)
                instance?.Dispose();
            instances.Clear();
            if (visualBundle != null)
                visualBundle.Unload(false);
            if (activeRuntime == this)
                activeRuntime = null;
        }

        private void ApplyFinalViewmodelPoses(Camera camera)
        {
            foreach (WeaponVisualInstance instance in instances.Values)
                instance?.PrepareForRender();
        }

        internal static void RegisterLauncher(RocketLauncher launcher)
        {
            activeRuntime?.AttachLauncher(launcher);
        }

        internal static void NotifyShot(RocketLauncher launcher, GrenadeProjectileProfile profile)
        {
            activeRuntime?.PlayShot(launcher, profile);
        }

        internal static void NotifyRedGelFired(RocketLauncher launcher)
        {
            activeRuntime?.PlayRedGelFired(launcher);
        }

        internal static void SyncCooldownDial(RocketLauncher launcher, UnityEngine.UI.Image meter, RectTransform arm)
        {
            activeRuntime?.SyncCooldownDialInternal(launcher, meter, arm);
        }

        internal static void AttachProjectileVisual(Grenade grenade, GrenadeProjectileProfile profile)
        {
            activeRuntime?.AttachProjectileVisualInternal(grenade, profile);
        }

        private void LoadVisualPrefab()
        {
            try
            {
                Assembly assembly = typeof(Plugin).Assembly;
                using (Stream stream = assembly.GetManifestResourceStream(BundleResourceName))
                {
                    if (stream == null)
                    {
                        Plugin.LogSource?.LogError("Custom weapon visual AssetBundle was not embedded in the mod DLL.");
                        return;
                    }

                    byte[] data = new byte[stream.Length];
                    int offset = 0;
                    while (offset < data.Length)
                    {
                        int read = stream.Read(data, offset, data.Length - offset);
                        if (read <= 0)
                            break;
                        offset += read;
                    }
                    if (offset != data.Length)
                    {
                        Plugin.LogSource?.LogError("Could not fully read the custom weapon visual AssetBundle.");
                        return;
                    }

                    visualBundle = AssetBundle.LoadFromMemory(data);
                    if (visualBundle == null)
                    {
                        Plugin.LogSource?.LogError("ULTRAKILL rejected the custom weapon visual AssetBundle.");
                        return;
                    }

                    foreach (GameObject asset in visualBundle.LoadAllAssets<GameObject>())
                    {
                        if (asset != null && asset.name == PrefabName)
                        {
                            visualPrefab = asset;
                            break;
                        }
                    }
                    if (visualPrefab == null)
                        Plugin.LogSource?.LogError("Custom weapon visual prefab was missing from the AssetBundle.");
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogError("Failed to load custom weapon visual: " + exception);
            }
        }

        private void ScanLaunchers()
        {
            foreach (RocketLauncher launcher in FindObjectsOfType<RocketLauncher>())
                AttachLauncher(launcher);

            List<RocketLauncher> missing = new List<RocketLauncher>();
            foreach (KeyValuePair<RocketLauncher, WeaponVisualInstance> pair in instances)
            {
                if (pair.Key != null)
                    continue;
                pair.Value?.Dispose();
                missing.Add(pair.Key);
            }
            foreach (RocketLauncher launcher in missing)
                instances.Remove(launcher);
        }

        private void AttachLauncher(RocketLauncher launcher)
        {
            if (launcher == null || visualPrefab == null || instances.ContainsKey(launcher))
                return;
            instances.Add(launcher, new WeaponVisualInstance(launcher, visualPrefab));
        }

        private void PlayShot(RocketLauncher launcher, GrenadeProjectileProfile profile)
        {
            if (launcher != null && instances.TryGetValue(launcher, out WeaponVisualInstance instance))
                instance.PlayShot(profile);
        }

        private void PlayRedGelFired(RocketLauncher launcher)
        {
            if (launcher != null && instances.TryGetValue(launcher, out WeaponVisualInstance instance))
                instance.PlayRedGelFired();
        }

        private void SyncCooldownDialInternal(RocketLauncher launcher, UnityEngine.UI.Image meter, RectTransform arm)
        {
            if (launcher != null && instances.TryGetValue(launcher, out WeaponVisualInstance instance))
                instance.SyncCooldownDial(meter, arm);
        }

        private void ApplyPaintConfigurationToInstances()
        {
            foreach (WeaponVisualInstance instance in instances.Values)
                instance?.ApplyPaintConfiguration();
        }

        private void ApplyCooldownDialSettingsToInstances()
        {
            foreach (WeaponVisualInstance instance in instances.Values)
                instance?.ApplyCooldownDialPose();
        }

        private void AttachProjectileVisualInternal(Grenade grenade, GrenadeProjectileProfile profile)
        {
            if (grenade == null || UseVanillaVisuals || profile == GrenadeProjectileProfile.BlueDelivery)
                return;
            string prefabName = profile == GrenadeProjectileProfile.GreenContact
                ? "GrenadeProjectileGreen"
                : profile == GrenadeProjectileProfile.RedBurst
                    ? "GrenadeProjectileRed"
                    : "GrenadeProjectilePrimary";
            GameObject prefab = visualBundle != null ? visualBundle.LoadAsset<GameObject>(prefabName) : null;
            if (prefab == null)
                return;
            if (grenade.GetComponent<GrenadeLauncherProjectileVisual>() == null)
            {
                GrenadeLauncherProjectileVisual marker = grenade.gameObject.AddComponent<GrenadeLauncherProjectileVisual>();
                marker.Initialize(prefab);
            }
        }

        private sealed class WeaponVisualInstance
        {
            private readonly RocketLauncher launcher;
            private readonly GameObject visual;
            private readonly Renderer[] nativeWeaponRenderers;
            private readonly Animator animator;
            private readonly List<Material> runtimeMaterials = new List<Material>();
            private readonly List<PaintBinding> paintBindings = new List<PaintBinding>();
            private readonly List<RestPose> restPoses = new List<RestPose>();
            private readonly float primaryFireDuration;
            private readonly float altFireDuration;
            private readonly float equipDuration;
            private readonly float overheatDuration;
            private readonly AnimationClip primaryFireClip;
            private readonly AnimationClip altFireClip;
            private readonly AnimationClip overheatClip;
            private readonly AnimationClip idleClip;
            private readonly AnimationClip equipClip;
            private readonly Transform modelRoot;
            private readonly Vector3 modelRestLocalPosition;
            private readonly Quaternion modelRestLocalRotation;
            private readonly Vector3 barrelRestLocalPosition;
            private readonly Quaternion barrelRestLocalRotation;
            private Transform barrelPivot;
            private GameObject nativeDialRoot;
            private Vector3 nativeDialOriginalLocalPosition;
            private Quaternion nativeDialOriginalLocalRotation;
            private Vector3 nativeDialOriginalLocalScale;
            private Quaternion dialBaseRotationInVisual;
            private bool dialBound;
            private bool dialMounted;
            private bool dialBaseCaptured;
            private Transform overheatSmokeMuzzle;
            private ParticleSystem overheatSmoke;
            private AudioSource overheatSound;
            private AudioSource shotSound;
            private Quaternion overheatSmokeRotationInModel = Quaternion.identity;
            private float recoilAmount;
            private float animationFreezeAt = -1f;
            private int playingAnimationState;
            private bool wasVisibleInHierarchy;
            private bool nativeHidden;
            private bool overheatActive;
            private float overheatStartedAt;
            private float overheatPoseBlend;
            private float equipEndsAt = -1f;
            private float redGelLastShotAt = -10f;
            private float redGelShakeBlend;
            private float redGelNoiseSeed;
            private bool rootAnimationActive;
            private Vector3 authoredRootPosition;
            private Quaternion authoredRootRotation = Quaternion.identity;
            private Vector3 calibratedRootPosition;
            private Quaternion calibratedRootRotation = Quaternion.identity;
            private bool manualEquipActive;
            private bool manualIdleActive;
            private bool idleTrackingPending;
            private float manualEquipStartedAt;
            private float manualIdleStartedAt;
            private AnimationClip activeOneShotClip;
            private float activeOneShotStartedAt;
            private float activeOneShotDuration;

            private enum PaintGroup
            {
                Orange,
                Silver,
                Black,
                DeepBlack
            }

            private sealed class PaintBinding
            {
                internal Material Material;
                internal PaintGroup Group;
                internal Color RelativeColor;
            }

            // These are intentionally code-driven while the visual is still a rigid model.
            // They can later become individual barrel/body animations without changing the
            // gameplay fire hooks that call PlayShot.
            private const float PrimaryRecoil = 0.045f;
            private const float GreenRecoil = 0.065f;
            private const float BlueRecoil = 0.025f;
            private const float RecoilRecoveryPerSecond = 0.32f;
            private static readonly int PrimaryFireState = Animator.StringToHash("Base Layer.PrimaryFire");
            private static readonly int AltFireState = Animator.StringToHash("Base Layer.AltFire");
            private static readonly int EquipState = Animator.StringToHash("Base Layer.Equip");
            private static readonly int IdleState = Animator.StringToHash("Base Layer.Idle");
            private const float OverheatDropDistance = 0.22f;
            private const float OverheatPitchDegrees = 15f;
            private const float OverheatYawDegrees = -8f;
            private const float OverheatRollDegrees = 6f;
            private const float OverheatSmokeScale = 1f;
            private static readonly Vector3 OverheatSmokeCameraOffset = new Vector3(0f, -1.5f, 5f);
            private static readonly Vector3 OverheatSmokeRotationOffset = new Vector3(0f, 150.5f, 0f);
            // Native Jackhammer transitions into / out of its cooldown pose instead of
            // snapping the complete weapon. Keep custom visual equally smooth.
            private const float OverheatPoseBlendDuration = 0.12f;
            private static readonly FieldInfo HammerOverheatParticleField = AccessTools.Field(typeof(ShotgunHammer), "overheatParticle");
            private static readonly FieldInfo HammerOverheatSoundField = AccessTools.Field(typeof(ShotgunHammer), "overheatAud");
            private static readonly FieldInfo HammerModelTransformField = AccessTools.Field(typeof(ShotgunHammer), "modelTransform");

            internal WeaponVisualInstance(RocketLauncher source, GameObject prefab)
            {
                launcher = source;
                Renderer[] allRenderers = source.GetComponentsInChildren<Renderer>(true);
                Renderer nativeWeaponRenderer = FindNativeWeaponRenderer(allRenderers);
                nativeWeaponRenderers = nativeWeaponRenderer != null
                    ? new[] { nativeWeaponRenderer }
                    : new Renderer[0];
                Transform anchor = nativeWeaponRenderer != null ? nativeWeaponRenderer.transform : source.transform;
                visual = Instantiate(prefab, anchor, false);
                visual.name = "Grenade Launcher Custom Visual";
                // Parenting preserves the vanilla weapon's camera motion, but layer is a
                // per-renderer GameObject property and does not inherit. Without copying
                // it, the custom mesh is drawn by the world camera and can clip through
                // walls instead of being drawn by ULTRAKILL's viewmodel camera.
                SetLayerRecursively(visual, nativeWeaponRenderer != null
                    ? nativeWeaponRenderer.gameObject.layer
                    : source.gameObject.layer);
                ApplyVanillaWeaponMaterialModel(visual, nativeWeaponRenderer);
                ApplyPaintConfiguration();
                animator = visual.GetComponent<Animator>();
                primaryFireClip = GetClip(animator, "PrimaryFire");
                altFireClip = GetClip(animator, "AltFire");
                primaryFireDuration = primaryFireClip != null ? primaryFireClip.length : 0f;
                altFireDuration = altFireClip != null ? altFireClip.length : 0f;
                equipDuration = GetClipDuration(animator, "Equip");
                overheatClip = GetClip(animator, "Overheat");
                idleClip = GetClip(animator, "Idle");
                equipClip = GetClip(animator, "Equip");
                overheatDuration = overheatClip != null ? overheatClip.length : 0f;
                barrelPivot = visual.transform.Find("Model/BarrelPivot");
                CreateOverheatSmokeMuzzle();
                CaptureRestPoses();
                modelRoot = visual.transform.Find("Model");
                modelRestLocalPosition = modelRoot != null ? modelRoot.localPosition : Vector3.zero;
                modelRestLocalRotation = modelRoot != null ? modelRoot.localRotation : Quaternion.identity;
                barrelRestLocalPosition = barrelPivot != null ? barrelPivot.localPosition : Vector3.zero;
                barrelRestLocalRotation = barrelPivot != null ? barrelPivot.localRotation : Quaternion.identity;
                // Do not let Unity's Animator keep an old weapon state alive after the
                // game swaps/fires a launcher. All authored clips are sampled explicitly
                // below, giving the custom viewmodel a single authoritative pose writer.
                if (animator != null)
                    animator.enabled = false;
                CreateShotSoundSource();
                visual.SetActive(false);
            }

            internal void Refresh()
            {
                if (launcher == null || visual == null)
                    return;

                recoilAmount = Mathf.MoveTowards(recoilAmount, 0f,
                    RecoilRecoveryPerSecond * Time.unscaledDeltaTime);
                // The custom visual is parented to the original viewmodel renderer. Its local
                // Z axis is the calibrated depth axis, so this is a brief kick away from the
                // player without fighting ULTRAKILL's walk sway on the parent.
                bool shouldShow = Plugin.Instance != null && Plugin.Instance.IsGrenadeModeEnabled(launcher) && !UseVanillaVisuals;
                bool cooling = shouldShow && launcher.variation == 2 && RedBurstController.IsCoolingDown;
                float overheatDelay = Mathf.Max(0f, primaryFireDuration);
                bool shouldOverheat = cooling && Time.time >= RedBurstController.CooldownStartedAt + overheatDelay;
                overheatPoseBlend = Mathf.MoveTowards(overheatPoseBlend, shouldOverheat ? 1f : 0f,
                    Time.unscaledDeltaTime / OverheatPoseBlendDuration);
                calibratedRootPosition = new Vector3(
                    Value(offsetX),
                    Value(offsetY),
                    Value(offsetZ) + recoilAmount);
                calibratedRootRotation = Quaternion.Euler(Value(rotationX), Value(rotationY), Value(rotationZ));
                // Authored Equip/Idle curves deliberately animate the visual root in the
                // Unity preview's coordinate system.  While one is active, leave that
                // raw pose alone until LateRefresh can convert its *delta* back onto the
                // calibrated in-game viewmodel pose.  Resetting it here every frame made
                // the delta include the entire preview offset, which is why Equip appeared
                // off-centre and Idle looked as though it moved sideways.
                if (!rootAnimationActive)
                {
                    visual.transform.localPosition = calibratedRootPosition;
                    visual.transform.localRotation = calibratedRootRotation;
                }
                visual.transform.localScale = Vector3.one * Mathf.Max(0.001f, Value(modelScale, 0.09f));
                if (visual.activeSelf != shouldShow)
                    visual.SetActive(shouldShow);
                SetNativeVisible(!shouldShow);
                SetCooldownDialVisible(shouldShow);

                // Swapping weapons often disables the vanilla parent instead of this child.
                // activeSelf therefore stays true; activeInHierarchy is the actual equip edge.
                bool visibleInHierarchy = shouldShow && visual.activeInHierarchy;
                if (visibleInHierarchy && !wasVisibleInHierarchy)
                {
                    StartEquipAnimation();
                }
                else if (!visibleInHierarchy)
                {
                    animationFreezeAt = -1f;
                    equipEndsAt = -1f;
                }
                wasVisibleInHierarchy = visibleInHierarchy;
                if (equipEndsAt >= 0f && Time.time >= equipEndsAt)
                    ResetAnimationToIdle();
                // Complete a one-shot before deciding whether red cooldown is allowed to
                // resume Idle. The previous order restarted Idle after suppression, making
                // it win over the red overheat state on the very next frame.
                FreezeCompletedAnimation();
                UpdateRedGelShakeState(shouldShow && !cooling);
                bool suppressAmbientIdle = shouldShow && launcher.variation == 2 &&
                    (cooling || Time.time - redGelLastShotAt <= RedGelShotGrace);
                if (suppressAmbientIdle)
                {
                    StopAmbientIdle();
                }
                else if (!manualIdleActive && !manualEquipActive && animationFreezeAt < 0f && equipEndsAt < 0f)
                {
                    BeginAmbientIdle();
                }
                // Red cooldown starts immediately on its final burst shot. Effects and
                // custom barrel loop belong to that edge; whole-weapon Jackhammer pose
                // remains delayed until primary-shot animation has finished.
                if (cooling)
                {
                    if (!overheatActive)
                        StartCustomOverheat(RedBurstController.CooldownStartedAt);
                    MaintainCustomOverheat();
                    overheatActive = true;
                }
                else if (overheatActive)
                {
                    StopJackhammerOverheatEffects();
                    if (overheatPoseBlend > 0f)
                    {
                        // Root pose fades out while idle animation regains the barrel.
                        // Do not keep spinning after the real cooldown has ended.
                    }
                    else
                    {
                        ResetAnimationToIdle();
                        return;
                    }
                }
            }

            internal void PlayShot(GrenadeProjectileProfile profile)
            {
                float amount = profile == GrenadeProjectileProfile.GreenContact
                    ? GreenRecoil
                    : profile == GrenadeProjectileProfile.BlueDelivery
                        ? BlueRecoil
                        : PrimaryRecoil;
                recoilAmount = Mathf.Max(recoilAmount, amount);

                bool primary = profile == GrenadeProjectileProfile.Primary || profile == GrenadeProjectileProfile.RedBurst;
                activeOneShotClip = primary ? primaryFireClip : altFireClip;
                activeOneShotDuration = activeOneShotClip != null ? activeOneShotClip.length : 0f;
                activeOneShotStartedAt = Time.time;
                playingAnimationState = primary ? PrimaryFireState : AltFireState;
                rootAnimationActive = false;
                manualEquipActive = false;
                manualIdleActive = false;
                idleTrackingPending = false;
                equipEndsAt = -1f;
                RestoreRestPoses();
                animationFreezeAt = activeOneShotDuration > 0f
                    ? Time.time + activeOneShotDuration
                    : -1f;
                PlayConfiguredShotSound(profile);
            }

            internal void SyncCooldownDial(UnityEngine.UI.Image nativeMeter, RectTransform nativeArm)
            {
                if (visual == null || nativeMeter == null)
                    return;
                EnsureCustomCooldownDial(nativeMeter);
            }

            internal void PlayRedGelFired()
            {
                // The gel itself may continue firing while Fire2 is held. Each successful
                // native ShootNapalm refreshes this short grace window, so the procedural
                // animation is active only while gel is actually being produced.
                redGelLastShotAt = Time.time;
                redGelNoiseSeed = UnityEngine.Random.Range(0f, 10000f);
                InterruptEquipForAction();
                // ShootNapalm can leave the visual Animator playing its previous primary
                // shot state. The red effect is code-driven, so discard that state instead
                // of allowing it to move Model offscreen while spray is held.
                animationFreezeAt = -1f;
                playingAnimationState = 0;
                StopAmbientIdle();
            }

            internal void LateRefresh()
            {
                if (launcher == null || visual == null || !visual.activeInHierarchy)
                    return;
                PrepareForRender();
                if (launcher.variation == 2 && RedBurstController.IsCoolingDown)
                {
                    MaintainCustomOverheat();
                    MaintainJackhammerOverheatEffects();
                }
                else if (redGelShakeBlend > 0.0001f)
                {
                    // Red gel borrows only the authored barrel-spin curves. It never starts
                    // the burst overheat's smoke or audio effects.
                    MaintainRedGelBarrelSpin();
                }
                else if (manualIdleActive)
                {
                    RestoreBarrelRestPose();
                }
            }

            internal void PrepareForRender()
            {
                if (launcher == null || visual == null || !visual.activeInHierarchy)
                    return;
                SampleAuthoredAmbientAnimation();
                ApplyPostAnimatorRootMotion();
            }

            private void StartEquipAnimation()
            {
                ResetAnimationToIdle();
                if (equipClip == null || equipDuration <= 0f)
                    return;

                // ULTRAKILL can evaluate its viewmodel after this Animator. Directly
                // sample our authored clip in LateRefresh, where it becomes the final
                // visual pose for the frame (the same proven approach as Overheat).
                manualIdleActive = false;
                manualEquipActive = true;
                rootAnimationActive = false;
                manualEquipStartedAt = Time.time;
                equipClip.SampleAnimation(visual, 0f);
                equipEndsAt = Time.time + equipDuration;
            }

            private void SampleAuthoredAmbientAnimation()
            {
                if (activeOneShotClip != null && activeOneShotDuration > 0f)
                {
                    float elapsedShot = Mathf.Clamp(Time.time - activeOneShotStartedAt, 0f, activeOneShotDuration);
                    activeOneShotClip.SampleAnimation(visual, elapsedShot);
                    return;
                }

                if (manualEquipActive && equipClip != null && equipDuration > 0f)
                {
                    float elapsed = Mathf.Clamp(Time.time - manualEquipStartedAt, 0f, equipDuration);
                    equipClip.SampleAnimation(visual, elapsed);
                    return;
                }

                if (!manualIdleActive || idleClip == null || idleClip.length <= 0f)
                    return;

                float elapsedIdle = Mathf.Repeat(Time.time - manualIdleStartedAt, idleClip.length);
                idleClip.SampleAnimation(visual, elapsedIdle);
                Vector3 sampledRootPosition = visual.transform.localPosition;
                Quaternion sampledRootRotation = visual.transform.localRotation;
                if (idleTrackingPending)
                {
                    // User-authored Idle starts and ends at its neutral pose. Capture the
                    // root sample as a reference only; the root itself belongs to
                    // ULTRAKILL's viewmodel rig and cannot be a reliable animation target.
                    authoredRootPosition = sampledRootPosition;
                    authoredRootRotation = sampledRootRotation;
                    idleTrackingPending = false;
                }
                Vector3 positionDelta = sampledRootPosition - authoredRootPosition;
                positionDelta.y *= IdleVerticalMotionScale;
                Quaternion rotationDelta = sampledRootRotation * Quaternion.Inverse(authoredRootRotation);

                // Re-target the authored root curves to Model. Primary, alt-fire, and
                // Overheat already use this layer successfully; this keeps the exact
                // authored idle delta while avoiding the game-owned viewmodel root.
                if (modelRoot != null)
                {
                    modelRoot.localPosition = modelRestLocalPosition + positionDelta;
                    modelRoot.localRotation = modelRestLocalRotation * rotationDelta;
                }
                visual.transform.localPosition = calibratedRootPosition;
                visual.transform.localRotation = calibratedRootRotation;
                rootAnimationActive = false;
            }

            private void BeginRootAnimationTracking()
            {
                if (visual == null)
                    return;
                authoredRootPosition = visual.transform.localPosition;
                authoredRootRotation = visual.transform.localRotation;
                rootAnimationActive = true;
            }

            private void BeginAmbientIdle()
            {
                if (idleClip == null || idleClip.length <= 0f)
                    return;
                RestoreIdleModelRestPose();
                manualIdleActive = true;
                idleTrackingPending = true;
                manualIdleStartedAt = Time.time;
                rootAnimationActive = false;
            }

            private void StopAmbientIdle()
            {
                if (!manualIdleActive && !idleTrackingPending)
                    return;
                manualIdleActive = false;
                idleTrackingPending = false;
                rootAnimationActive = false;
                RestoreIdleModelRestPose();
            }

            private void RestoreIdleModelRestPose()
            {
                if (modelRoot == null)
                    return;
                modelRoot.localPosition = modelRestLocalPosition;
                modelRoot.localRotation = modelRestLocalRotation;
            }

            private void UpdateRedGelShakeState(bool canShow)
            {
                bool held = false;
                PlayerInput input = MonoSingleton<InputManager>.Instance?.InputSource;
                if (input != null && input.Fire2 != null)
                    held = input.Fire2.IsPressed;
                bool firingGel = canShow && launcher != null && launcher.variation == 2 && held &&
                                  Time.time - redGelLastShotAt <= RedGelShotGrace;
                float blendTime = RedGelShakeBlendTime;
                redGelShakeBlend = Mathf.MoveTowards(redGelShakeBlend, firingGel ? 1f : 0f,
                    Time.unscaledDeltaTime / blendTime);
            }

            private void ApplyPostAnimatorRootMotion()
            {
                if (visual == null)
                    return;

                Vector3 positionDelta = Vector3.zero;
                Quaternion rotationDelta = Quaternion.identity;
                if (rootAnimationActive)
                {
                    positionDelta = visual.transform.localPosition - authoredRootPosition;
                    rotationDelta = visual.transform.localRotation * Quaternion.Inverse(authoredRootRotation);
                }

                Vector3 shakePosition = Vector3.zero;
                Vector3 shakeRotation = Vector3.zero;
                if (redGelShakeBlend > 0.0001f)
                {
                    float time = Time.unscaledTime * RedGelNoiseSpeed;
                    float seed = redGelNoiseSeed;
                    shakePosition = new Vector3(
                        SignedNoise(seed + 11.3f, time) * RedGelShakeHorizontal,
                        SignedNoise(seed + 37.7f, time * 1.07f) * RedGelShakeVertical,
                        SignedNoise(seed + 79.1f, time * 0.91f) * RedGelShakeDepth) * redGelShakeBlend;
                    shakeRotation = new Vector3(
                        SignedNoise(seed + 103.9f, time * 0.83f) * RedGelShakePitch,
                        SignedNoise(seed + 151.6f, time * 1.11f) * RedGelShakeYaw,
                        SignedNoise(seed + 211.2f, time * 0.97f) * RedGelShakeRoll) * redGelShakeBlend;
                }

                visual.transform.localPosition = calibratedRootPosition + positionDelta + shakePosition;
                visual.transform.localRotation = calibratedRootRotation * rotationDelta * Quaternion.Euler(shakeRotation);
                ApplyCooldownPoseOverlay();
                // One final UI-transform copy is required because the game-owned Canvas
                // must stay in its native viewmodel hierarchy to preserve its aspect ratio.
                // This is intentionally one dial, once per rendered frame—not a material
                // or scene scan—and lets the dial follow the final animated Model pose.
                ApplyCooldownDialPose();
            }

            private void ApplyCooldownPoseOverlay()
            {
                if (overheatPoseBlend <= 0f || visual == null)
                    return;

                // This must run after the final root pose is assigned. The viewmodel
                // camera path invokes PrepareForRender immediately before draw; applying
                // the pose only in Refresh caused that final assignment to erase it.
                Camera camera = Camera.main;
                if (camera != null)
                {
                    visual.transform.position -= camera.transform.up *
                        (OverheatDropDistance * overheatPoseBlend);
                    visual.transform.rotation = Quaternion.AngleAxis(
                        OverheatPitchDegrees * overheatPoseBlend, camera.transform.right) *
                        visual.transform.rotation;
                    visual.transform.rotation = Quaternion.AngleAxis(
                        OverheatYawDegrees * overheatPoseBlend, camera.transform.up) *
                        visual.transform.rotation;
                    visual.transform.rotation = Quaternion.AngleAxis(
                        OverheatRollDegrees * overheatPoseBlend, camera.transform.forward) *
                        visual.transform.rotation;
                }
                else
                {
                    visual.transform.localPosition += Vector3.down *
                        (OverheatDropDistance * overheatPoseBlend);
                }
            }

            private void EnsureCustomCooldownDial(UnityEngine.UI.Image nativeMeter)
            {
                if (!dialBound)
                {
                    Transform nativeRoot = FindDialRoot(nativeMeter.transform);
                    if (nativeRoot == null || visual == null)
                        return;

                    nativeDialRoot = nativeRoot.gameObject;
                    nativeDialOriginalLocalPosition = nativeRoot.localPosition;
                    nativeDialOriginalLocalRotation = nativeRoot.localRotation;
                    nativeDialOriginalLocalScale = nativeRoot.localScale;
                    Transform modelAnchor = modelRoot != null ? modelRoot : visual.transform;
                    dialBaseRotationInVisual = Quaternion.Inverse(modelAnchor.rotation) * nativeRoot.rotation;
                    dialBound = true;
                }

                MountCooldownDial();
            }

            // Keep the game's Canvas in its native viewmodel hierarchy. Parenting it to
            // the imported mesh can non-uniformly squash the UI. Its final world pose is
            // instead copied from our animated Model transform in LateRefresh.
            private void MountCooldownDial()
            {
                if (!dialBound || dialMounted || nativeDialRoot == null || visual == null)
                    return;

                Transform dial = nativeDialRoot.transform;
                if (!dialBaseCaptured)
                {
                    dialBaseCaptured = true;
                }

                dialMounted = true;
                ApplyCooldownDialPose();
            }

            private void SetCooldownDialVisible(bool showCustom)
            {
                if (showCustom)
                {
                    MountCooldownDial();
                    return;
                }

                RestoreNativeCooldownDial();
            }

            private void RestoreNativeCooldownDial()
            {
                if (!dialMounted || nativeDialRoot == null)
                    return;

                Transform dial = nativeDialRoot.transform;
                dial.localPosition = nativeDialOriginalLocalPosition;
                dial.localRotation = nativeDialOriginalLocalRotation;
                dial.localScale = nativeDialOriginalLocalScale;
                dialMounted = false;
            }

            internal void ApplyCooldownDialPose()
            {
                if (nativeDialRoot == null || !dialBound || visual == null || !visual.activeInHierarchy)
                    return;
                if (!dialMounted)
                    return;

                Transform dial = nativeDialRoot.transform;
                Transform modelAnchor = modelRoot != null ? modelRoot : visual.transform;
                // The anchor belongs to the scaled model, but the final lift is a UI
                // clearance measured at the original 0.10 model scale. Preserve that
                // clearance when a player uses a smaller/larger model.
                float currentModelScale = Mathf.Max(0.001f, Value(modelScale, 0.09f));
                Vector3 clearance = CooldownDialOffset * (0.1f / currentModelScale);
                dial.position = modelAnchor.TransformPoint(CooldownDialModelAnchor + clearance);
                dial.rotation = modelAnchor.rotation * dialBaseRotationInVisual * Quaternion.Euler(CooldownDialRotation);
                dial.localScale = nativeDialOriginalLocalScale * CooldownDialScale;
            }

            private static Transform FindDialRoot(Transform meter)
            {
                for (Transform current = meter; current != null; current = current.parent)
                {
                    if (current.name == "HologramDisplay")
                        return current;
                }
                return null;
            }

            private void MaintainRedGelBarrelSpin()
            {
                if (overheatClip == null || overheatDuration <= 0f)
                    return;
                ApplyBarrelSpin(Time.unscaledTime * RedGelBarrelSpinSpeed);
            }

            private void ApplyBarrelSpin(float elapsed)
            {
                if (barrelPivot == null || overheatDuration <= 0f)
                    return;
                // The authored Overheat clip is a uniform 0→360° barrel rotation. Do not
                // sample its absolute Transform keys: those came from the Unity preview
                // and can move a viewmodel pivot away from its live rest position.
                float degrees = Mathf.Repeat(elapsed, overheatDuration) / overheatDuration * 360f;
                barrelPivot.localPosition = barrelRestLocalPosition;
                barrelPivot.localRotation = barrelRestLocalRotation * Quaternion.AngleAxis(degrees, Vector3.right);
            }

            private void RestoreBarrelRestPose()
            {
                if (barrelPivot == null)
                    return;
                barrelPivot.localPosition = barrelRestLocalPosition;
                barrelPivot.localRotation = barrelRestLocalRotation;
            }

            private void CreateShotSoundSource()
            {
                if (visual == null)
                    return;
                shotSound = visual.AddComponent<AudioSource>();
                shotSound.playOnAwake = false;
                shotSound.spatialBlend = 0f;
            }

            private void PlayConfiguredShotSound(GrenadeProjectileProfile profile)
            {
                if (!ShouldReplaceVanillaShotFeedback(launcher) || shotSound == null)
                    return;

                AudioSource vanillaSource = GetVanillaShotAudio(launcher);
                if (vanillaSource != null)
                {
                    // Keep the game's weapon-volume route, player volume controls, and
                    // its small randomized pitch from the fired shot.
                    shotSound.outputAudioMixerGroup = vanillaSource.outputAudioMixerGroup;
                    shotSound.volume = vanillaSource.volume;
                    shotSound.pitch = vanillaSource.pitch;
                    shotSound.priority = vanillaSource.priority;
                }
                else
                {
                    shotSound.volume = 1f;
                    shotSound.pitch = 1f;
                }

                PlayBakedShotSound(shotSound, profile, vanillaSource != null ? vanillaSource.clip : null);
            }

            private static void PlayBakedShotSound(AudioSource source, GrenadeProjectileProfile profile, AudioClip rocketShot)
            {
                if (source == null)
                    return;

                // Final selections from the sound test:
                // primary = Grenade Launcher; green = Rocket + Grenade Launcher;
                // blue = Rocket; red burst = Grenade Launcher.
                switch (profile)
                {
                    case GrenadeProjectileProfile.GreenContact:
                        PlayOneShot(source, rocketShot, 1f);
                        PlayOneShot(source, GrenadeLauncherShotSoundLibrary.GrenadeLauncher, CustomShotVolume);
                        break;
                    case GrenadeProjectileProfile.BlueDelivery:
                        PlayOneShot(source, rocketShot, 1f);
                        break;
                    case GrenadeProjectileProfile.RedBurst:
                    case GrenadeProjectileProfile.Primary:
                    default:
                        PlayOneShot(source, GrenadeLauncherShotSoundLibrary.GrenadeLauncher, CustomShotVolume);
                        break;
                }
            }

            private static void PlayOneShot(AudioSource source, AudioClip clip, float volumeScale)
            {
                if (clip != null)
                    source.PlayOneShot(clip, Mathf.Max(0f, volumeScale));
            }

            private static float SignedNoise(float x, float y) => Mathf.PerlinNoise(x, y) * 2f - 1f;

            private void ResetAnimationToIdle()
            {
                // A hidden viewmodel stops rendering but its Animator retains whatever
                // mid-shot pose it had. Restore the captured prefab pose so weapon
                // swapping never restores a frozen barrel/pad frame from the previous use.
                RestoreRestPoses();
                activeOneShotClip = null;
                activeOneShotDuration = 0f;
                manualEquipActive = false;
                manualIdleActive = false;
                idleTrackingPending = false;
                BeginAmbientIdle();
                animationFreezeAt = -1f;
                playingAnimationState = 0;
                equipEndsAt = -1f;
                overheatActive = false;
                overheatPoseBlend = 0f;
                StopCustomOverheat();
            }

            private void StartCustomOverheat(float startedAt)
            {
                InterruptEquipForAction();
                overheatStartedAt = startedAt;
                StartJackhammerOverheatEffects();
            }

            private void InterruptEquipForAction()
            {
                if (!manualEquipActive && equipEndsAt < 0f)
                    return;
                manualEquipActive = false;
                equipEndsAt = -1f;
                rootAnimationActive = false;
                activeOneShotClip = null;
                activeOneShotDuration = 0f;
                RestoreRestPoses();
            }

            private void MaintainCustomOverheat()
            {
                if (overheatClip == null || visual == null || overheatDuration <= 0f)
                    return;
                ApplyBarrelSpin(Time.time - overheatStartedAt);
            }

            private void StopCustomOverheat()
            {
                RestoreBarrelRestPose();
                StopJackhammerOverheatEffects();
            }

            private void StopJackhammerOverheatEffects()
            {
                if (overheatSmoke != null)
                {
                    Destroy(overheatSmoke.gameObject);
                    overheatSmoke = null;
                }
                if (overheatSound != null)
                {
                    Destroy(overheatSound);
                    overheatSound = null;
                }
            }

            private void StartJackhammerOverheatEffects()
            {
                ShotgunHammer hammer = FindSceneJackhammer();
                if (hammer == null)
                {
                    Plugin.LogSource?.LogWarning("Red burst cooldown began before Jackhammer assets existed; no native effects this cooldown.");
                    return;
                }
                // Constructor runs before calibrated viewmodel rotation is applied.
                // Re-evaluate true muzzle now, while weapon is equipped and visible.
                UpdateOverheatSmokeMuzzle();
                ParticleSystem sourceSmoke = HammerOverheatParticleField?.GetValue(hammer) as ParticleSystem;
                if (sourceSmoke != null && overheatSmokeMuzzle != null)
                {
                    Transform sourceModel = HammerModelTransformField?.GetValue(hammer) as Transform;
                    overheatSmokeRotationInModel = sourceModel != null
                        ? Quaternion.Inverse(sourceModel.rotation) * sourceSmoke.transform.rotation
                        : Quaternion.identity;
                    // Barrel supplies position only. Root parent prevents spinning emitter
                    // from turning its smoke stream back into V1's camera.
                    overheatSmoke = Instantiate(sourceSmoke, visual.transform, false);
                    MaintainJackhammerOverheatEffects();
                    overheatSmoke.gameObject.SetActive(true);
                    foreach (Renderer renderer in overheatSmoke.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = true;
                    overheatSmoke.Clear(true);
                    overheatSmoke.Play(true);
                }
                AudioSource sourceSound = HammerOverheatSoundField?.GetValue(hammer) as AudioSource;
                if (sourceSound != null && sourceSound.clip != null)
                {
                    overheatSound = visual.AddComponent<AudioSource>();
                    overheatSound.clip = sourceSound.clip;
                    overheatSound.volume = sourceSound.volume;
                    overheatSound.pitch = sourceSound.pitch;
                    overheatSound.loop = sourceSound.loop;
                    // Native source goes through ULTRAKILL's SFX mixer. Without this,
                    // our duplicate bypasses that attenuation and sounds much louder.
                    overheatSound.outputAudioMixerGroup = sourceSound.outputAudioMixerGroup;
                    overheatSound.spatialBlend = 0f;
                    overheatSound.Play();
                }
            }

            private void MaintainJackhammerOverheatEffects()
            {
                if (overheatSmoke == null || overheatSmokeMuzzle == null || visual == null)
                    return;
                Camera camera = Camera.main;
                Vector3 position = overheatSmokeMuzzle.position;
                if (camera != null)
                {
                    position += camera.transform.right * OverheatSmokeCameraOffset.x;
                    position += camera.transform.up * OverheatSmokeCameraOffset.y;
                    position += camera.transform.forward * OverheatSmokeCameraOffset.z;
                }
                overheatSmoke.transform.position = position;
                overheatSmoke.transform.rotation = visual.transform.rotation * overheatSmokeRotationInModel *
                    Quaternion.Euler(OverheatSmokeRotationOffset);
                overheatSmoke.transform.localScale = Vector3.one * OverheatSmokeScale;
            }

            private static ShotgunHammer FindSceneJackhammer()
            {
                // FindObjectOfType ignores disabled weapons. Jackhammer normally remains
                // disabled until first equipped, exactly when red burst needs its effects.
                foreach (ShotgunHammer hammer in Resources.FindObjectsOfTypeAll<ShotgunHammer>())
                {
                    if (hammer != null && hammer.gameObject.scene.IsValid())
                        return hammer;
                }
                return null;
            }

            private void FreezeCompletedAnimation()
            {
                if (animationFreezeAt < 0f || Time.time < animationFreezeAt ||
                    activeOneShotClip == null || playingAnimationState == 0)
                    return;

                // Give the last authored frame one deterministic sample, then hand
                // control back to the authored Idle loop.
                activeOneShotClip.SampleAnimation(visual, activeOneShotDuration);
                animationFreezeAt = -1f;
                ResetAnimationToIdle();
            }

            internal void Dispose()
            {
                SetNativeVisible(true);
                RestoreNativeCooldownDial();
                if (visual != null)
                    Destroy(visual);
                foreach (Material material in runtimeMaterials)
                {
                    if (material != null)
                        Destroy(material);
                }
                runtimeMaterials.Clear();
                paintBindings.Clear();
            }

            private void SetNativeVisible(bool visible)
            {
                if (nativeHidden == !visible)
                    return;
                foreach (Renderer renderer in nativeWeaponRenderers)
                {
                    if (renderer != null)
                        renderer.enabled = visible;
                }
                nativeHidden = !visible;
            }

            private static float Value(FloatSliderField field, float fallback = 0f) => field?.value ?? fallback;

            private static float GetClipDuration(Animator source, string clipName)
            {
                AnimationClip clip = GetClip(source, clipName);
                return clip != null ? clip.length : 0f;
            }

            private static AnimationClip GetClip(Animator source, string clipName)
            {
                if (source == null || source.runtimeAnimatorController == null)
                    return null;

                foreach (AnimationClip clip in source.runtimeAnimatorController.animationClips)
                {
                    if (clip != null && clip.name == clipName)
                        return clip;
                }
                return null;
            }

            private void CreateOverheatSmokeMuzzle()
            {
                if (barrelPivot == null || visual == null || overheatSmokeMuzzle != null)
                    return;
                GameObject muzzle = new GameObject("Overheat Smoke Muzzle");
                muzzle.transform.SetParent(barrelPivot, false);
                overheatSmokeMuzzle = muzzle.transform;
                UpdateOverheatSmokeMuzzle();
            }

            private void UpdateOverheatSmokeMuzzle()
            {
                if (barrelPivot == null || visual == null || overheatSmokeMuzzle == null)
                    return;

                // BarrelPivot is centred for rotation. Build one child at barrel's end
                // furthest along camera-forward direction, so smoke leaves muzzle rather
                // than middle of spinning cylinder. This also keeps position correct for
                // every calibrated viewmodel orientation and middle-hold setting.
                bool haveBounds = false;
                Vector3 min = Vector3.zero;
                Vector3 max = Vector3.zero;
                foreach (Renderer renderer in barrelPivot.GetComponentsInChildren<Renderer>(true))
                {
                    if (renderer == null)
                        continue;
                    Bounds bounds = renderer.bounds;
                    for (int x = 0; x < 2; x++)
                    for (int y = 0; y < 2; y++)
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 point = barrelPivot.InverseTransformPoint(new Vector3(
                            x == 0 ? bounds.min.x : bounds.max.x,
                            y == 0 ? bounds.min.y : bounds.max.y,
                            z == 0 ? bounds.min.z : bounds.max.z));
                        if (!haveBounds)
                        {
                            min = max = point;
                            haveBounds = true;
                        }
                        else
                        {
                            min = Vector3.Min(min, point);
                            max = Vector3.Max(max, point);
                        }
                    }
                }
                if (!haveBounds)
                    return;

                Vector3 span = max - min;
                int axis = span.x >= span.y && span.x >= span.z ? 0 : span.y >= span.z ? 1 : 2;
                Vector3 endpointA = (min + max) * 0.5f;
                Vector3 endpointB = endpointA;
                endpointA[axis] = min[axis];
                endpointB[axis] = max[axis];
                Camera camera = Camera.main;
                Vector3 forward = camera != null ? camera.transform.forward : visual.transform.forward;
                // During live equipped calibration, endpoint furthest along camera-forward
                // is barrel's real muzzle. (Constructor-time direction is not valid.)
                Vector3 muzzlePosition = Vector3.Dot(barrelPivot.TransformPoint(endpointB), forward) >=
                                         Vector3.Dot(barrelPivot.TransformPoint(endpointA), forward)
                    ? endpointB
                    : endpointA;
                Vector3 outward = muzzlePosition - (min + max) * 0.5f;
                if (outward.sqrMagnitude > 0f)
                    muzzlePosition += outward.normalized * Mathf.Max(0.05f, span[axis] * 0.02f);

                overheatSmokeMuzzle.localPosition = muzzlePosition;
                // Native smoke exits forward; do not retain imported model's inverted
                // local axis or particles travel backward into camera.
                overheatSmokeMuzzle.rotation = Quaternion.LookRotation(forward);
            }

            private void CaptureRestPoses()
            {
                if (visual == null)
                    return;

                foreach (Transform transform in visual.GetComponentsInChildren<Transform>(true))
                {
                    // Root position/rotation is continuously calibrated from config in Refresh.
                    // Store only model descendants that animation clips are allowed to change.
                    if (transform != visual.transform)
                        restPoses.Add(new RestPose(transform));
                }
            }

            private void RestoreRestPoses()
            {
                foreach (RestPose pose in restPoses)
                    pose.Restore();
            }

            private readonly struct RestPose
            {
                private readonly Transform transform;
                private readonly Vector3 position;
                private readonly Quaternion rotation;
                private readonly Vector3 scale;

                internal RestPose(Transform source)
                {
                    transform = source;
                    position = source.localPosition;
                    rotation = source.localRotation;
                    scale = source.localScale;
                }

                internal void Restore()
                {
                    if (transform == null)
                        return;
                    transform.localPosition = position;
                    transform.localRotation = rotation;
                    transform.localScale = scale;
                }
            }

            private static Renderer FindNativeWeaponRenderer(Renderer[] renderers)
            {
                foreach (Renderer renderer in renderers)
                {
                    SkinnedMeshRenderer skinned = renderer as SkinnedMeshRenderer;
                    if (skinned != null && skinned.sharedMesh != null &&
                        skinned.sharedMesh.name == "RocketLauncher")
                        return skinned;
                }
                return null;
            }

            private static void SetLayerRecursively(GameObject target, int layer)
            {
                if (target == null)
                    return;
                target.layer = layer;
                foreach (Transform child in target.transform)
                    SetLayerRecursively(child.gameObject, layer);
            }

            private static string TransformPath(Transform transform)
            {
                if (transform == null)
                    return "<none>";
                string path = transform.name;
                for (Transform current = transform.parent; current != null; current = current.parent)
                    path = current.name + "/" + path;
                return path;
            }

            // The AssetBundle is built outside ULTRAKILL and therefore cannot directly
            // reference its private Master shader. Clone the loaded rocket material at
            // runtime instead: that retains all of the game's lighting/render settings,
            // then replace only the colour used by each custom model piece.
            private void ApplyVanillaWeaponMaterialModel(GameObject customVisual, Renderer nativeRenderer)
            {
                if (customVisual == null || nativeRenderer == null ||
                    nativeRenderer.sharedMaterial == null)
                    return;

                Material vanillaMaterial = nativeRenderer.sharedMaterial;
                foreach (Renderer customRenderer in customVisual.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] sourceMaterials = customRenderer.sharedMaterials;
                    Material[] replacementMaterials = new Material[sourceMaterials.Length];
                    for (int index = 0; index < sourceMaterials.Length; index++)
                    {
                        Material sourceMaterial = sourceMaterials[index];
                        if (sourceMaterial == null)
                            continue;

                        Color intendedColor = sourceMaterial.HasProperty("_Color")
                            ? sourceMaterial.color
                            : Color.white;
                        Material replacement = new Material(vanillaMaterial)
                        {
                            name = "GrenadeLauncher_" + sourceMaterial.name
                        };
                        // The vanilla launcher textures belong to its own UV layout. The
                        // Master shader's ID map selects its painted material zones, so both
                        // it and the albedo map must be neutralized for the Roblox-exported
                        // mesh. This preserves the game's lighting while allowing each custom
                        // renderer material to supply one deliberate colour.
                        if (replacement.HasProperty("_MainTex"))
                            replacement.mainTexture = Texture2D.whiteTexture;
                        if (replacement.HasProperty("_IDTex"))
                            replacement.SetTexture("_IDTex", Texture2D.whiteTexture);
                        if (replacement.HasProperty("_Color"))
                            replacement.color = intendedColor;
                        replacementMaterials[index] = replacement;
                        runtimeMaterials.Add(replacement);
                        PaintGroup group = ClassifyPaintGroup(sourceMaterial.name);
                        paintBindings.Add(new PaintBinding
                        {
                            Material = replacement,
                            Group = group,
                            // A colour group is deliberately one player-facing paint slot.
                            // Do not retain a hidden per-material multiplier: the selected
                            // colour must be the colour players actually see.
                            RelativeColor = Color.white
                        });
                    }
                    // Renderer.materials may instantiate another private set. Assign the
                    // exact instances we retain so live calibration edits what is rendered.
                    customRenderer.sharedMaterials = replacementMaterials;
                }
            }

            internal void ApplyPaintConfiguration()
            {
                foreach (PaintBinding binding in paintBindings)
                {
                    if (binding?.Material == null)
                        continue;

                    Color selected = SelectedColorFor(binding.Group);
                    Color finalColor = new Color(
                        Mathf.Clamp01(selected.r * binding.RelativeColor.r),
                        Mathf.Clamp01(selected.g * binding.RelativeColor.g),
                        Mathf.Clamp01(selected.b * binding.RelativeColor.b),
                        selected.a);
                    if (binding.Material.HasProperty("_Color"))
                        binding.Material.color = finalColor;

                }
            }

            private static PaintGroup ClassifyPaintGroup(string materialName)
            {
                string name = materialName ?? string.Empty;
                if (name.IndexOf("veryblack", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("barrel1", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("backside", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("displayanchor", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PaintGroup.DeepBlack;
                if (name.IndexOf("lightblack", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PaintGroup.Black;
                if (name.IndexOf("orange", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("ironsight", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("paddingbright", StringComparison.OrdinalIgnoreCase) >= 0)
                    return PaintGroup.Orange;
                return PaintGroup.Silver;
            }

            private static Color SelectedColorFor(PaintGroup group)
            {
                switch (group)
                {
                    case PaintGroup.Orange: return orangeColor?.value ?? DefaultOrange;
                    case PaintGroup.Black: return blackColor?.value ?? DefaultBlack;
                    case PaintGroup.DeepBlack: return deepBlackColor?.value ?? DefaultDeepBlack;
                    default: return silverColor?.value ?? DefaultSilver;
                }
            }

        }
    }
}

namespace GrenadeLauncherMod
{
    // The submitted clips are embedded in the DLL so a Thunderstore installation is
    // self-contained. Unity has no runtime WAV importer available to BepInEx mods, so
    // decode the small PCM files once during startup into ordinary AudioClips.
    internal static class GrenadeLauncherShotSoundLibrary
    {
        private const string GrenadeLauncherResource = "GrenadeLauncherMod.audio.grenade_launcher_shoot.wav";
        private const string LochNLoadResource = "GrenadeLauncherMod.audio.loch_n_load_shoot.wav";
        private const string LooseCannonResource = "GrenadeLauncherMod.audio.loose_cannon_shoot.wav";

        private static bool loaded;
        private static AudioClip grenadeLauncher;
        private static AudioClip lochNLoad;
        private static AudioClip looseCannon;

        internal static AudioClip GrenadeLauncher
        {
            get
            {
                Preload();
                return grenadeLauncher;
            }
        }

        internal static AudioClip LochNLoad
        {
            get
            {
                Preload();
                return lochNLoad;
            }
        }

        internal static AudioClip LooseCannon
        {
            get
            {
                Preload();
                return looseCannon;
            }
        }

        internal static void Preload()
        {
            if (loaded)
                return;
            loaded = true;
            grenadeLauncher = LoadPcmWave(GrenadeLauncherResource, "Grenade Launcher shot");
            lochNLoad = LoadPcmWave(LochNLoadResource, "Loch-N-Load shot");
            looseCannon = LoadPcmWave(LooseCannonResource, "Loose Cannon shot");
        }

        private static AudioClip LoadPcmWave(string resourceName, string clipName)
        {
            try
            {
                Assembly assembly = typeof(Plugin).Assembly;
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        Plugin.LogSource?.LogError("Missing embedded shot sound: " + resourceName);
                        return null;
                    }

                    byte[] wave = new byte[stream.Length];
                    int offset = 0;
                    while (offset < wave.Length)
                    {
                        int read = stream.Read(wave, offset, wave.Length - offset);
                        if (read <= 0)
                            break;
                        offset += read;
                    }
                    if (offset != wave.Length)
                    {
                        Plugin.LogSource?.LogError("Could not completely read shot sound: " + resourceName);
                        return null;
                    }
                    return DecodePcmWave(wave, clipName);
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogError("Could not load shot sound " + resourceName + ": " + exception.Message);
                return null;
            }
        }

        private static AudioClip DecodePcmWave(byte[] wave, string clipName)
        {
            if (wave == null || wave.Length < 44 || ReadTag(wave, 0) != "RIFF" || ReadTag(wave, 8) != "WAVE")
                throw new InvalidDataException("Not a RIFF/WAVE file.");

            int format = 0;
            int channels = 0;
            int sampleRate = 0;
            int bitsPerSample = 0;
            int dataStart = -1;
            int dataLength = 0;
            int position = 12;
            while (position + 8 <= wave.Length)
            {
                string chunk = ReadTag(wave, position);
                int length = ReadInt32(wave, position + 4);
                position += 8;
                if (length < 0 || position + length > wave.Length)
                    throw new InvalidDataException("Malformed " + chunk + " chunk.");

                if (chunk == "fmt ")
                {
                    if (length < 16)
                        throw new InvalidDataException("Incomplete fmt chunk.");
                    format = ReadInt16(wave, position);
                    channels = ReadInt16(wave, position + 2);
                    sampleRate = ReadInt32(wave, position + 4);
                    bitsPerSample = ReadInt16(wave, position + 14);
                }
                else if (chunk == "data")
                {
                    dataStart = position;
                    dataLength = length;
                }

                position += length;
                if ((length & 1) != 0)
                    position++;
            }

            if ((format != 1 && format != 3) || channels <= 0 || sampleRate <= 0 ||
                dataStart < 0 || dataLength <= 0 || bitsPerSample <= 0)
                throw new InvalidDataException("Unsupported WAV format.");

            int bytesPerSample = bitsPerSample / 8;
            if (bytesPerSample <= 0 || dataLength % bytesPerSample != 0)
                throw new InvalidDataException("Invalid WAV sample length.");
            int sampleCount = dataLength / bytesPerSample;
            int frames = sampleCount / channels;
            if (frames <= 0)
                throw new InvalidDataException("WAV contains no full frames.");

            float[] samples = new float[frames * channels];
            for (int index = 0; index < samples.Length; index++)
                samples[index] = ReadSample(wave, dataStart + index * bytesPerSample, format, bitsPerSample);

            AudioClip result = AudioClip.Create(clipName, frames, channels, sampleRate, false);
            result.SetData(samples, 0);
            return result;
        }

        private static float ReadSample(byte[] data, int offset, int format, int bits)
        {
            if (format == 3 && bits == 32)
                return Mathf.Clamp(BitConverter.ToSingle(data, offset), -1f, 1f);
            switch (bits)
            {
                case 8:
                    return (data[offset] - 128f) / 128f;
                case 16:
                    return ReadInt16(data, offset) / 32768f;
                case 24:
                    int raw24 = data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16;
                    if ((raw24 & 0x800000) != 0)
                        raw24 |= unchecked((int)0xFF000000);
                    return raw24 / 8388608f;
                case 32:
                    return ReadInt32(data, offset) / 2147483648f;
                default:
                    throw new InvalidDataException("Unsupported WAV bit depth: " + bits);
            }
        }

        private static string ReadTag(byte[] data, int offset)
        {
            return Encoding.ASCII.GetString(data, offset, 4);
        }

        private static short ReadInt16(byte[] data, int offset)
        {
            return (short)(data[offset] | data[offset + 1] << 8);
        }

        private static int ReadInt32(byte[] data, int offset)
        {
            return data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;
        }
    }

    internal sealed class GrenadeLauncherProjectileVisual : MonoBehaviour
    {
        private GameObject visual;
        private Renderer[] vanillaRenderers;
        private readonly List<Material> runtimeMaterials = new List<Material>();
        private bool customVisible;

        internal void Initialize(GameObject prefab)
        {
            vanillaRenderers = GetComponentsInChildren<Renderer>(true);
            visual = Instantiate(prefab, transform, false);
            visual.name = "Grenade Launcher Custom Projectile Visual";
            // Keep the grenade's nose aligned with the vanilla rocket's impact point.
            // The authored mesh is shorter than the hidden rocket visual, so its center
            // needs a small forward offset rather than appearing to hover before contact.
            visual.transform.localPosition = Vector3.forward * WeaponVisualRuntime.ProjectileForwardOffset;
            // Imported projectile OBJ is Y-forward; ULTRAKILL grenade travel is Z-forward.
            // The asset's authored nose faces negative Y, so use -90 rather than +90.
            visual.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            Renderer source = Array.Find(vanillaRenderers, renderer => renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                              ?? Array.Find(vanillaRenderers, renderer => renderer != null);
            Renderer custom = visual.GetComponentInChildren<Renderer>(true);
            ApplyVanillaProjectileMaterialModel(source);
            if (source != null && custom != null)
            {
                float sourceSize = Mathf.Max(0.001f, source.bounds.size.magnitude);
                float customSize = Mathf.Max(0.001f, custom.bounds.size.magnitude);
                // Vanilla rocket bounds are particle-small. Direct ratio turns this
                // authored model into a dot, so retain a sensible lower bound.
                visual.transform.localScale = Vector3.one * Mathf.Max(0.3f, sourceSize / customSize);
            }
        }

        private void LateUpdate()
        {
            if (visual != null)
            {
                GrenadeLauncherProjectile projectile = GetComponent<GrenadeLauncherProjectile>();
                visual.transform.localPosition = Vector3.forward *
                    (projectile != null && projectile.IsStuck ? 0f : WeaponVisualRuntime.ProjectileForwardOffset);
            }
            bool showCustom = !WeaponVisualRuntime.UseVanillaVisuals;
            if (showCustom == customVisible)
                return;
            customVisible = showCustom;
            if (visual != null)
                visual.SetActive(showCustom);
            foreach (Renderer renderer in vanillaRenderers ?? new Renderer[0])
            {
                if (renderer != null)
                    renderer.enabled = !showCustom;
            }
        }

        private void OnDestroy()
        {
            foreach (Renderer renderer in vanillaRenderers ?? new Renderer[0])
            {
                if (renderer != null)
                    renderer.enabled = true;
            }
            foreach (Material material in runtimeMaterials)
            {
                if (material != null)
                    Destroy(material);
            }
            runtimeMaterials.Clear();
        }

        private void ApplyVanillaProjectileMaterialModel(Renderer source)
        {
            if (visual == null)
                return;
            foreach (Renderer customRenderer in visual.GetComponentsInChildren<Renderer>(true))
            {
                Material[] originalMaterials = customRenderer.sharedMaterials;
                Material[] replacementMaterials = new Material[originalMaterials.Length];
                for (int index = 0; index < originalMaterials.Length; index++)
                {
                    Material original = originalMaterials[index];
                    if (original == null)
                        continue;
                    Texture intendedTexture = original.HasProperty("_MainTex") ? original.mainTexture : null;
                    // Projectile texture is a Source-engine albedo atlas. ULTRAKILL's
                    // viewmodel shader and Standard both alter it heavily under scene
                    // lighting. Unlit/Texture preserves its authored pixels one-for-one.
                    Shader unlitTexture = Shader.Find("Unlit/Texture");
                    Material replacement = new Material(unlitTexture ?? original.shader)
                    {
                        name = "GrenadeLauncherProjectile_" + original.name
                    };
                    if (replacement.HasProperty("_MainTex"))
                        replacement.mainTexture = intendedTexture ?? Texture2D.whiteTexture;
                    if (replacement.HasProperty("_Color"))
                        replacement.color = Color.white;
                    replacementMaterials[index] = replacement;
                    runtimeMaterials.Add(replacement);
                }
                customRenderer.sharedMaterials = replacementMaterials;
            }
        }
    }
}
