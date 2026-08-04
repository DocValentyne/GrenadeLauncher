using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Fields;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace GrenadeLauncherMod
{
    internal static partial class PluginSettings
    {
        private static FloatSliderField greenCooldown;
        private static FloatSliderField greenSpeed;
        private static FloatSliderField greenUpwardVelocity;
        private static FloatSliderField greenGravity;
        private static FloatSliderField greenDirectDamage;
        private static FloatSliderField greenAirshotDamage;
        private static FloatSliderField greenSurfaceDamage;
        private static FloatSliderField greenDirectExplosionSize;
        private static FloatSliderField greenAirshotExplosionSize;
        private static FloatSliderField greenSurfaceExplosionSize;
        private static FloatSliderField greenDirectSelfDamage;
        private static FloatSliderField greenSurfaceSelfDamage;
        private static FloatSliderField greenKnockback;
        private static FloatSliderField greenLifetime;

        private static FloatSliderField gelProjectileSpeed;
        private static FloatSliderField gelCoveragePerDroplet;
        private static FloatSliderField gelCoverageRequired;
        private static FloatSliderField gelSpotSize;
        private static FloatSliderField stuckDamage;
        private static FloatSliderField greenStuckDamage;
        private static FloatSliderField stuckExplosionSize;
        private static FloatSliderField stuckSelfDamage;
        private static FloatSliderField stuckKnockback;
        private static FloatSliderField stuckChainRadius;
        private static FloatSliderField stuckChainPropagationSpeed;
        private static BoolField enemyCarrierDirectDamage;
        private static BoolField timedFuseDetonatesStuck;

        private static FloatSliderField blueDistance;
        private static FloatSliderField blueCooldown;
        private static FloatSliderField blueSlingshotForce;
        private static FloatSliderField bluePointSize;
        private static FloatSliderField blueReplacementDamage;
        private static FloatSliderField blueReplacementExplosionSize;
        private static FloatSliderField blueReplacementForce;
        private static FloatSliderField pipeDreamDistance;
        private static FloatSliderField moonShotDistance;
        private static FloatSliderField pipeDreamStylePoints;
        private static FloatSliderField moonShotStylePoints;
        private static FloatSliderField outSnipedStylePoints;
        private static FloatSliderField walkingBombStylePoints;
        private static FloatSliderField pipeDreamDamageMultiplier;
        private static FloatSliderField pipeDreamExplosionSizeMultiplier;
        private static FloatSliderField moonShotDamageMultiplier;
        private static FloatSliderField moonShotExplosionSizeMultiplier;
        private static BoolField rangeTelemetry;

        internal static float GreenCooldown => greenCooldown?.value ?? 6f;
        internal static float GreenSpeedMultiplier => greenSpeed?.value ?? 1.5f;
        internal static float GreenUpwardVelocityMultiplier => greenUpwardVelocity?.value ?? 1f;
        internal static float GreenGravityMultiplier => greenGravity?.value ?? 1f;
        internal static float GreenDirectDamage => greenDirectDamage?.value ?? 6f;
        internal static float GreenAirshotDamage => greenAirshotDamage?.value ?? 7.5f;
        internal static float GreenSurfaceDamage => greenSurfaceDamage?.value ?? 4f;
        internal static float GreenDirectExplosionSize => greenDirectExplosionSize?.value ?? 1.4f;
        internal static float GreenAirshotExplosionSize => greenAirshotExplosionSize?.value ?? 1.4f;
        internal static float GreenSurfaceExplosionSize => greenSurfaceExplosionSize?.value ?? 1f;
        internal static float GreenDirectSelfDamage => greenDirectSelfDamage?.value ?? 35f;
        internal static float GreenSurfaceSelfDamage => greenSurfaceSelfDamage?.value ?? 35f;
        internal static float GreenKnockbackMultiplier => greenKnockback?.value ?? 1f;
        internal static float GreenLifetime => greenLifetime?.value ?? 15f;

        internal static float GelProjectileSpeedMultiplier => gelProjectileSpeed?.value ?? 1f;
        internal static float GelCoveragePerDroplet => (gelCoveragePerDroplet?.value ?? 4f) / 100f;
        internal static float GelCoverageRequired => (gelCoverageRequired?.value ?? 75f) / 100f;
        internal static float GelSpotSize => gelSpotSize?.value ?? 1.25f;
        internal static float StuckDamage => stuckDamage?.value ?? 4f;
        internal static float GreenStuckDamage => greenStuckDamage?.value ?? 6f;
        internal static float StuckExplosionSize => stuckExplosionSize?.value ?? 1.3f;
        internal static float StuckSelfDamage => stuckSelfDamage?.value ?? 45f;
        internal static float StuckKnockbackMultiplier => stuckKnockback?.value ?? 1f;
        internal static float StuckChainRadiusMultiplier => stuckChainRadius?.value ?? 2f;
        internal static float StuckChainPropagationSpeed => stuckChainPropagationSpeed?.value ?? 50f;
        internal static bool EnemyCarrierTakesDirectDamage => enemyCarrierDirectDamage?.value ?? true;
        internal static bool TimedFuseDetonatesStuck => timedFuseDetonatesStuck?.value ?? false;

        internal static float BlueDistance => blueDistance?.value ?? 27f;
        internal static float BlueCooldown => blueCooldown?.value ?? 7f;
        internal static float BlueSlingshotForce => blueSlingshotForce?.value ?? 0f;
        internal static float BluePointSize => bluePointSize?.value ?? 1f;
        internal static float BlueReplacementDamage => blueReplacementDamage?.value ?? 2f;
        internal static float BlueReplacementExplosionSize => blueReplacementExplosionSize?.value ?? 1f;
        internal static float BlueReplacementForceMultiplier => blueReplacementForce?.value ?? 1.25f;
        internal static float PipeDreamMinimumDistance => pipeDreamDistance?.value ?? 56f;
        internal static float MoonShotMinimumDistance => moonShotDistance?.value ?? 125f;
        internal static int PipeDreamStylePoints => Mathf.RoundToInt(pipeDreamStylePoints?.value ?? 150f);
        internal static int MoonShotStylePoints => Mathf.RoundToInt(moonShotStylePoints?.value ?? 550f);
        internal static int OutSnipedStylePoints => Mathf.RoundToInt(outSnipedStylePoints?.value ?? 550f);
        internal static int WalkingBombStylePoints => Mathf.RoundToInt(walkingBombStylePoints?.value ?? 90f);
        internal static float PipeDreamDamageMultiplier => pipeDreamDamageMultiplier?.value ?? 1f;
        internal static float PipeDreamExplosionSizeMultiplier => pipeDreamExplosionSizeMultiplier?.value ?? 1f;
        internal static float MoonShotDamageMultiplier => moonShotDamageMultiplier?.value ?? 1.15f;
        internal static float MoonShotExplosionSizeMultiplier => moonShotExplosionSizeMultiplier?.value ?? 1.15f;
        internal static bool RangeTelemetryEnabled => rangeTelemetry?.value ?? false;

        private static void InitializeAlternateSettings(PluginConfigurator configurator)
        {
            ConfigPanel green = new ConfigPanel(configurator.rootPanel, "Green contact grenade", "greenContactGrenade");
            AddPageResetButton(green, "Reset this page to default", "resetGreenContactGrenade");
            greenCooldown = Slider(green, "Cooldown (seconds)", "greenCooldown", 0.05f, 10f, 6f, 2);
            greenSpeed = Slider(green, "Projectile speed multiplier", "greenSpeedMultiplier", 0.1f, 5f, 1.5f, 2);
            greenUpwardVelocity = Slider(green, "Upward velocity multiplier", "greenUpwardVelocityMultiplier", 0f, 5f, 1f, 2);
            greenGravity = Slider(green, "Gravity multiplier", "greenGravityMultiplier", 0.1f, 5f, 1f, 2);
            greenDirectDamage = Slider(green, "Direct hit damage", "greenDirectDamage", 0f, 20f, 6f, 2);
            greenAirshotDamage = Slider(green, "Airshot damage", "greenAirshotDamage", 0f, 25f, 7.5f, 2);
            greenSurfaceDamage = Slider(green, "Surface contact damage", "greenSurfaceDamage", 0f, 20f, 4f, 2);
            greenDirectExplosionSize = Slider(green, "Direct explosion size (rocket = 1)", "greenDirectExplosionSize", 0.1f, 5f, 1.4f, 2);
            greenAirshotExplosionSize = Slider(green, "Airshot explosion size", "greenAirshotExplosionSize", 0.1f, 5f, 1.4f, 2);
            greenSurfaceExplosionSize = Slider(green, "Surface explosion size (rocket = 1)", "greenSurfaceExplosionSize", 0.1f, 5f, 1f, 2);
            greenDirectSelfDamage = Slider(green, "Direct self damage (HP)", "greenDirectSelfDamage", 0f, 100f, 35f, 0);
            greenSurfaceSelfDamage = Slider(green, "Surface self damage (HP)", "greenSurfaceSelfDamage", 0f, 100f, 35f, 0);
            greenKnockback = Slider(green, "Explosion knockback (rocket = 1)", "greenKnockbackMultiplier", 0f, 5f, 1f, 2);
            greenLifetime = Slider(green, "Silent projectile lifetime", "greenLifetime", 1f, 60f, 15f, 1);

            ConfigPanel gel = new ConfigPanel(configurator.rootPanel, "Red blue-gel system", "redGelSystem");
            AddPageResetButton(gel, "Reset this page to default", "resetRedGelSystem");
            gelProjectileSpeed = Slider(gel, "Gel projectile speed multiplier", "gelProjectileSpeedMultiplier", 0.1f, 5f, 1f, 2);
            gelCoveragePerDroplet = Slider(gel, "Enemy coverage per droplet (%)", "gelCoveragePerDroplet", 0.1f, 100f, 4f, 1);
            gelCoverageRequired = Slider(gel, "Enemy coverage required (%)", "gelCoverageRequired", 0f, 100f, 75f, 1);
            gelSpotSize = Slider(gel, "Terrain gel size (normal = 1)", "gelSpotSize", 0.1f, 5f, 1.25f, 2);
            stuckDamage = Slider(gel, "Primary stuck grenade damage", "stuckDamage", 0f, 20f, 4f, 2);
            greenStuckDamage = Slider(gel, "Green stuck grenade damage", "greenStuckDamage", 0f, 20f, 6f, 2);
            stuckExplosionSize = Slider(gel, "Stuck explosion size (rocket = 1)", "stuckExplosionSize", 0.1f, 5f, 1.3f, 2);
            stuckSelfDamage = Slider(gel, "Stuck explosion self damage (HP)", "stuckSelfDamage", 0f, 100f, 45f, 0);
            stuckKnockback = Slider(gel, "Stuck explosion knockback (rocket = 1)", "stuckKnockbackMultiplier", 0f, 5f, 1f, 2);
            stuckChainRadius = Slider(gel, "Stuck grenade chain radius multiplier", "stuckChainRadiusMultiplier", 0f, 10f, 2f, 2);
            stuckChainPropagationSpeed = Slider(gel, "Chain propagation speed (units/second)", "stuckChainPropagationSpeed", 1f, 200f, 50f, 1);
            enemyCarrierDirectDamage = new BoolField(gel, "Enemy carrier takes direct-hit damage", "enemyCarrierDirectDamage", true);
            timedFuseDetonatesStuck = new BoolField(gel, "Timed-fuse explosions detonate stuck grenades", "timedFuseDetonatesStuck", false);

            ConfigPanel blue = new ConfigPanel(configurator.rootPanel, "Blue slingshot point", "blueSlingshotPoint");
            AddPageResetButton(blue, "Reset this page to default", "resetBlueSlingshotPoint");
            blueDistance = Slider(blue, "Placement distance", "bluePlacementDistance", 5.5f, 200f, 27f, 1);
            blueCooldown = Slider(blue, "Cooldown (seconds)", "blueCooldown", 0.05f, 10f, 7f, 2);
            blueSlingshotForce = Slider(blue, "Extra slingshot force", "blueSlingshotForce", -50f, 200f, 0f, 1);
            bluePointSize = Slider(blue, "Hook point size multiplier", "bluePointSize", 0.25f, 4f, 1f, 2);
            blueReplacementDamage = Slider(blue, "Replacement explosion damage", "blueReplacementDamage", 0f, 20f, 2f, 2);
            blueReplacementExplosionSize = Slider(blue, "Replacement explosion size (Providence = 1)", "blueReplacementExplosionSize", 0.1f, 5f, 1f, 2);
            blueReplacementForce = Slider(blue, "Replacement enemy launch height (ground slam = 1)", "blueReplacementForce", 0f, 5f, 1.25f, 2);

            ConfigPanel style = new ConfigPanel(configurator.rootPanel, "Style bonuses", "styleBonuses");
            AddPageResetButton(style, "Reset this page to default", "resetStyleBonuses");
            pipeDreamDistance = Slider(style, "PIPE DREAM minimum distance", "pipeDreamMinimumDistance", 1f, 250f, 56f, 1);
            moonShotDistance = Slider(style, "MOON SHOT / OUT-SNIPED minimum distance", "moonShotMinimumDistance", 1f, 400f, 125f, 1);
            pipeDreamStylePoints = Slider(style, "PIPE DREAM style points", "pipeDreamStylePoints", 0f, 10000f, 150f, 0);
            moonShotStylePoints = Slider(style, "MOON SHOT style points", "moonShotStylePoints", 0f, 10000f, 550f, 0);
            outSnipedStylePoints = Slider(style, "OUT-SNIPED style points", "outSnipedStylePoints", 0f, 10000f, 550f, 0);
            walkingBombStylePoints = Slider(style, "WALKING BOMB style points", "walkingBombStylePoints", 0f, 5000f, 90f, 0);
            pipeDreamDamageMultiplier = Slider(style, "PIPE DREAM damage multiplier", "pipeDreamDamageMultiplier", 0f, 10f, 1f, 2);
            pipeDreamExplosionSizeMultiplier = Slider(style, "PIPE DREAM explosion size multiplier", "pipeDreamExplosionSizeMultiplier", 0.1f, 10f, 1f, 2);
            moonShotDamageMultiplier = Slider(style, "MOON SHOT damage multiplier", "moonShotDamageMultiplier", 0f, 10f, 1.15f, 2);
            moonShotExplosionSizeMultiplier = Slider(style, "MOON SHOT explosion size multiplier", "moonShotExplosionSizeMultiplier", 0.1f, 10f, 1.15f, 2);

            ConfigPanel debug = new ConfigPanel(configurator.rootPanel, "Debug logging", "debugLogging");
            debug.headerText = "These settings only write diagnostic information to the BepInEx log.";
            AddPageResetButton(debug, "Reset this page to default", "resetDebugLogging");
            rangeTelemetry = new BoolField(debug, "Log direct-hit ranges", "rangeTelemetry", false);
        }
    }

    internal static class AlternateFireInputContext
    {
        internal static InputActionState SuppressedAction;
        internal static int Depth;
        internal static bool Active => Depth > 0 && SuppressedAction != null;

        internal static void Enter(InputActionState action)
        {
            SuppressedAction = action;
            Depth++;
        }

        internal static void Exit()
        {
            Depth = Math.Max(0, Depth - 1);
            if (Depth == 0)
                SuppressedAction = null;
        }
    }

    [HarmonyPatch(typeof(InputActionState), "get_IsPressed")]
    internal static class AlternateFirePressedPatch
    {
        private static void Postfix(InputActionState __instance, ref bool __result)
        {
            if (AlternateFireInputContext.Active && ReferenceEquals(__instance, AlternateFireInputContext.SuppressedAction))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(InputActionState), "get_WasPerformedThisFrame")]
    internal static class AlternateFirePerformedPatch
    {
        private static void Postfix(InputActionState __instance, ref bool __result)
        {
            if (AlternateFireInputContext.Active && ReferenceEquals(__instance, AlternateFireInputContext.SuppressedAction))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(RocketLauncher), "Update")]
    internal static class GrenadeAlternateUpdatePatch
    {
        private static readonly AccessTools.FieldRef<RocketLauncher, UnityEngine.UI.Image> TimerMeter =
            AccessTools.FieldRefAccess<RocketLauncher, UnityEngine.UI.Image>("timerMeter");
        private static readonly AccessTools.FieldRef<RocketLauncher, RectTransform> TimerArm =
            AccessTools.FieldRefAccess<RocketLauncher, RectTransform>("timerArm");
        private static readonly AccessTools.FieldRef<RocketLauncher, AudioSource> TimerWindupSound =
            AccessTools.FieldRefAccess<RocketLauncher, AudioSource>("timerWindupSound");
        private static readonly Dictionary<int, float> blueLastProgress = new Dictionary<int, float>();

        private struct State
        {
            internal bool Active;
            internal bool AltHeld;
            internal bool AltPressedThisFrame;
        }

        private static void Prefix(RocketLauncher __instance, out State __state)
        {
            __state = new State();
            if (Plugin.Instance == null || !Plugin.Instance.IsGrenadeModeEnabled(__instance) || (__instance.variation != 0 && __instance.variation != 1))
                return;

            PlayerInput input = MonoSingleton<InputManager>.Instance?.InputSource;
            if (input == null || input.Fire2 == null)
                return;

            __state.Active = true;
            __state.AltHeld = input.Fire2.IsPressed;
            __state.AltPressedThisFrame = input.Fire2.WasPerformedThisFrame;
            WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
            if (charges != null)
            {
                if (__instance.variation == 1)
                    charges.rocketCannonballCharge = AlternateFireController.GreenCooldownProgress;
                else if (__instance.variation == 0)
                {
                    charges.rocketFrozen = false;
                    charges.rocketFreezeTime = 0f;
                }
            }
            AlternateFireInputContext.Enter(input.Fire2);
        }

        private static void Postfix(RocketLauncher __instance, State __state)
        {
            if (__state.Active)
            {
                UnityEngine.UI.Image meter = TimerMeter(__instance);
                if (meter != null)
                {
                    if (__instance.variation == 0)
                    {
                        float progress = AlternateFireController.BlueCooldownProgress;
                        meter.fillAmount = progress;
                        RectTransform arm = TimerArm(__instance);
                        if (arm != null)
                            arm.localRotation = Quaternion.Euler(Vector3.forward * (-360f * progress));
                        PlayBlueMilestoneSounds(__instance, progress);
                    }
                    else if (__instance.variation == 1)
                        meter.fillAmount = AlternateFireController.GreenCooldownProgress;
                }
            }
            if (__state.Active && __state.AltHeld)
                AlternateFireController.TryFire(__instance, __state.AltPressedThisFrame);
        }

        private static Exception Finalizer(State __state, Exception __exception)
        {
            if (__state.Active)
                AlternateFireInputContext.Exit();
            return __exception;
        }

        private static void PlayBlueMilestoneSounds(RocketLauncher launcher, float progress)
        {
            int id = launcher.GetInstanceID();
            if (!blueLastProgress.TryGetValue(id, out float previous))
                previous = progress;
            if (progress + 0.001f < previous)
                previous = progress;

            int previousQuarter = Mathf.Clamp(Mathf.FloorToInt(previous * 4f + 0.001f), 0, 4);
            int currentQuarter = Mathf.Clamp(Mathf.FloorToInt(progress * 4f + 0.001f), 0, 4);
            if (currentQuarter > previousQuarter)
            {
                AudioSource template = TimerWindupSound(launcher);
                for (int quarter = previousQuarter + 1; quarter <= currentQuarter; quarter++)
                {
                    if (template == null)
                        break;
                    AudioSource sound = UnityEngine.Object.Instantiate(template);
                    sound.pitch = 0.6f + quarter * 0.1f;
                }
            }
            blueLastProgress[id] = progress;
        }

        internal static void ResetAudioTracking() => blueLastProgress.Clear();
    }

    internal static class AlternateFireController
    {
        private static readonly AccessTools.FieldRef<RocketLauncher, WeaponIdentifier> WeaponId =
            AccessTools.FieldRefAccess<RocketLauncher, WeaponIdentifier>("wid");
        private static float greenReadyAt;
        private static float greenDisplayReadyAt;
        private static float greenDisplayStartedAt;
        private static float greenDisplayDuration;
        private static float blueReadyAt;
        private static float blueDisplayReadyAt;
        private static float blueDisplayStartedAt;
        private static float blueDisplayDuration;
        private static int greenVolleyFrame = -1;

        internal static float GreenCooldownProgress
        {
            get
            {
                if (CooldownRules.NoWeaponCooldown)
                    return 1f;
                if (Time.time >= greenDisplayReadyAt || greenDisplayDuration <= 0f)
                    return 1f;
                return Mathf.Clamp01((Time.time - greenDisplayStartedAt) / greenDisplayDuration);
            }
        }

        internal static float BlueCooldownProgress
        {
            get
            {
                if (CooldownRules.NoWeaponCooldown)
                    return 1f;
                if (Time.time >= blueDisplayReadyAt || blueDisplayDuration <= 0f)
                    return 1f;
                return Mathf.Clamp01((Time.time - blueDisplayStartedAt) / blueDisplayDuration);
            }
        }

        internal static void OnGreenPrimaryFired()
        {
            if (CooldownRules.NoWeaponCooldown)
            {
                greenReadyAt = 0f;
                return;
            }
            float duration = Mathf.Max(0.05f, PluginSettings.FireInterval);
            float requiredReadyAt = Time.time + duration;
            if (greenReadyAt >= requiredReadyAt)
                return;
            greenReadyAt = requiredReadyAt;
        }

        internal static void OnBluePrimaryFired()
        {
            if (CooldownRules.NoWeaponCooldown)
            {
                blueReadyAt = 0f;
                return;
            }
            float duration = Mathf.Max(0.05f, PluginSettings.FireInterval);
            float requiredReadyAt = Time.time + duration;
            if (blueReadyAt < requiredReadyAt)
                blueReadyAt = requiredReadyAt;
        }

        internal static void TryFire(RocketLauncher launcher, bool altPressedThisFrame)
        {
            if (launcher == null || (GameStateManager.Instance != null && GameStateManager.Instance.PlayerInputLocked))
                return;

            // The no-cooldown cheat makes the normal held-input loop ready every frame.
            // Require a fresh press in that mode so alternate grenades do not become an
            // accidental frame-rate-dependent automatic weapon.
            if (CooldownRules.NoWeaponCooldown && !altPressedThisFrame)
                return;

            WeaponIdentifier weaponId = WeaponId(launcher);
            bool dualWieldDuplicate = weaponId != null && weaponId.duplicate;
            if (launcher.variation == 0 && dualWieldDuplicate)
                return;

            bool continuingGreenVolley = launcher.variation == 1 && Time.frameCount == greenVolleyFrame;
            bool duplicateSharedCooldownException = continuingGreenVolley && dualWieldDuplicate;
            if (!SharedRocketFireCooldown.Ready && !duplicateSharedCooldownException)
                return;
            if (launcher.variation == 1 && (CooldownRules.NoWeaponCooldown || Time.time >= greenReadyAt || continuingGreenVolley))
            {
                greenVolleyFrame = Time.frameCount;
                if (dualWieldDuplicate)
                {
                    DelayedGreenShot.Schedule(launcher, weaponId != null ? weaponId.delay : 0f);
                    return;
                }

                GrenadeSpawnContext.Enter(GrenadeProjectileProfile.GreenContact);
                try
                {
                    launcher.Shoot();
                    if (CooldownRules.NoWeaponCooldown)
                    {
                        greenDisplayDuration = 0f;
                        greenDisplayReadyAt = 0f;
                        greenReadyAt = 0f;
                    }
                    else
                    {
                        greenDisplayStartedAt = Time.time;
                        greenDisplayDuration = Mathf.Max(0.05f, PluginSettings.GreenCooldown);
                        greenDisplayReadyAt = Time.time + greenDisplayDuration;
                        greenReadyAt = greenDisplayReadyAt;
                    }
                    WeaponCharges charges = MonoSingleton<WeaponCharges>.Instance;
                    if (charges != null)
                        charges.rocketCannonballCharge = 0f;
                }
                finally
                {
                    GrenadeSpawnContext.Exit();
                }
            }
            else if (launcher.variation == 0 && (CooldownRules.NoWeaponCooldown || Time.time >= blueReadyAt) &&
                     HookPointManager.TryGetDeliveryTarget(out Vector3 target))
            {
                BlueHookDeliveryContext.Enter(target);
                GrenadeSpawnContext.Enter(GrenadeProjectileProfile.BlueDelivery);
                try
                {
                    launcher.Shoot();
                    if (CooldownRules.NoWeaponCooldown)
                    {
                        blueDisplayDuration = 0f;
                        blueDisplayReadyAt = 0f;
                        blueReadyAt = 0f;
                    }
                    else
                    {
                        blueDisplayStartedAt = Time.time;
                        blueDisplayDuration = Mathf.Max(0.05f, PluginSettings.BlueCooldown);
                        blueDisplayReadyAt = Time.time + blueDisplayDuration;
                        blueReadyAt = blueDisplayReadyAt;
                    }
                }
                finally
                {
                    GrenadeSpawnContext.Exit();
                    BlueHookDeliveryContext.Exit();
                }
            }
        }

        internal static void Reset()
        {
            greenReadyAt = 0f;
            greenDisplayReadyAt = 0f;
            greenDisplayStartedAt = 0f;
            greenDisplayDuration = 0f;
            blueReadyAt = 0f;
            blueDisplayReadyAt = 0f;
            blueDisplayStartedAt = 0f;
            blueDisplayDuration = 0f;
            greenVolleyFrame = -1;
            DelayedGreenShot.CancelAll();
            GrenadeAlternateUpdatePatch.ResetAudioTracking();
            SharedRocketFireCooldown.Reset();
        }

        internal static void FireDelayedGreen(RocketLauncher launcher)
        {
            if (launcher == null || !launcher.gameObject.activeInHierarchy || Plugin.Instance == null ||
                !Plugin.Instance.IsGrenadeModeEnabled(launcher) || launcher.variation != 1)
                return;

            GrenadeSpawnContext.Enter(GrenadeProjectileProfile.GreenContact);
            try
            {
                launcher.Shoot();
            }
            finally
            {
                GrenadeSpawnContext.Exit();
            }
        }
    }

    internal sealed class DelayedGreenShot : MonoBehaviour
    {
        private static readonly HashSet<DelayedGreenShot> instances = new HashSet<DelayedGreenShot>();

        private RocketLauncher launcher;
        private float fireAt = -1f;

        private void Awake() => instances.Add(this);

        private void OnDestroy() => instances.Remove(this);

        internal static void Schedule(RocketLauncher launcher, float delay)
        {
            if (launcher == null)
                return;
            DelayedGreenShot shot = launcher.GetComponent<DelayedGreenShot>();
            if (shot == null)
                shot = launcher.gameObject.AddComponent<DelayedGreenShot>();
            if (shot.fireAt >= 0f)
                return;
            shot.launcher = launcher;
            shot.fireAt = Time.time + Mathf.Max(0f, delay);
        }

        internal static void CancelAll()
        {
            instances.RemoveWhere(shot => shot == null);
            foreach (DelayedGreenShot shot in instances.ToArray())
            {
                if (shot != null)
                    shot.fireAt = -1f;
            }
        }

        private void Update()
        {
            if (fireAt < 0f || Time.time < fireAt)
                return;
            fireAt = -1f;
            AlternateFireController.FireDelayedGreen(launcher);
        }
    }

    internal static class BlueHookDeliveryContext
    {
        private static int depth;
        internal static Vector3 Target { get; private set; }

        internal static void Enter(Vector3 target)
        {
            if (depth == 0)
                Target = target;
            depth++;
        }

        internal static void Exit()
        {
            depth = Math.Max(0, depth - 1);
            if (depth == 0)
                Target = Vector3.zero;
        }

        internal static void Reset()
        {
            depth = 0;
            Target = Vector3.zero;
        }
    }

    internal static class GelSpawnContext
    {
        internal static int Depth;
        internal static bool Active => Depth > 0;
    }

    [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.ShootNapalm))]
    internal static class GelNapalmPatch
    {
        private static void Prefix(RocketLauncher __instance, out bool __state)
        {
            __state = Plugin.Instance != null && Plugin.Instance.IsGrenadeModeEnabled(__instance) && __instance.variation == 2;
            if (__state)
                GelSpawnContext.Depth++;
        }

        private static Exception Finalizer(bool __state, Exception __exception)
        {
            if (__state)
                GelSpawnContext.Depth = Math.Max(0, GelSpawnContext.Depth - 1);
            return __exception;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo replacement = AccessTools.Method(typeof(GelNapalmPatch), nameof(InstantiateProjectile));
            bool replaced = false;
            foreach (CodeInstruction instruction in instructions)
            {
                if (!replaced && instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method &&
                    method.Name == nameof(UnityEngine.Object.Instantiate) && method.IsGenericMethod &&
                    method.GetGenericArguments()[0] == typeof(Rigidbody))
                {
                    instruction.operand = replacement;
                    replaced = true;
                }
                yield return instruction;
            }
            if (!replaced)
                Plugin.LogSource.LogError("Could not install the red gel projectile factory; ShootNapalm changed.");
        }

        private static Rigidbody InstantiateProjectile(Rigidbody original, Vector3 position, Quaternion rotation)
        {
            Rigidbody result = UnityEngine.Object.Instantiate(original, position, rotation);
            if (GelSpawnContext.Active && result != null)
            {
                if (result.GetComponent<GelProjectileMarker>() == null)
                    result.gameObject.AddComponent<GelProjectileMarker>();
            }
            return result;
        }
    }

    [HarmonyPatch(typeof(GasolineProjectile), "OnTriggerEnter")]
    internal static class GelProjectileCollisionPatch
    {
        private static bool Prefix(GasolineProjectile __instance, Collider other, out bool __state)
        {
            __state = false;
            GelProjectileMarker marker = __instance.GetComponentInParent<GelProjectileMarker>();
            if (marker == null)
                return true;

            EnemyIdentifier enemy = GrenadeLauncherProjectile.TryGetLivingEnemy(other);
            if (enemy != null)
            {
                marker.HitEnemy(__instance, other, enemy);
                return false;
            }
            if (other == null || other.CompareTag("Player") || other.gameObject.layer == 14 ||
                !LayerMaskDefaults.IsMatchingLayer(other.gameObject.layer, LMD.Environment))
                return false;

            __state = true;
            GelStainSpawnContext.Enter();
            return true;
        }

        private static Exception Finalizer(bool __state, Exception __exception)
        {
            if (__state)
                GelStainSpawnContext.Exit();
            return __exception;
        }
    }

    internal static class GelStainSpawnContext
    {
        private static int depth;
        internal static bool Active => depth > 0;
        internal static void Enter() => depth++;
        internal static void Exit() => depth = Math.Max(0, depth - 1);
        internal static void Reset() => depth = 0;
    }

    [HarmonyPatch(typeof(GasolineStain), "Awake")]
    internal static class GelStainAwakePatch
    {
        private static void Postfix(GasolineStain __instance)
        {
            if (GelStainSpawnContext.Active && __instance.GetComponent<GelStainMarker>() == null)
                __instance.gameObject.AddComponent<GelStainMarker>();
        }
    }

    [HarmonyPatch(typeof(GasolineStain), nameof(GasolineStain.AttachTo))]
    internal static class GelStainAttachPatch
    {
        private static bool Prefix(GasolineStain __instance, Collider other)
        {
            GelStainMarker marker = __instance.GetComponent<GelStainMarker>();
            if (marker == null)
                return true;

            // The vanilla compute-shader path hides this mesh and renders a voxel proxy.
            // We keep the textured mesh visible, so lift it slightly along the hit normal
            // to prevent it occupying the exact same depth as the struck surface.
            __instance.transform.position -= __instance.transform.forward * 0.02f;
            __instance.transform.SetParent(other.transform, true);
            __instance.SetSize(Mathf.Max(0.1f, PluginSettings.GelSpotSize));
            marker.Surface = other;
            marker.EnemyVisual = GrenadeLauncherProjectile.TryGetLivingEnemy(other) != null;
            marker.Radius = 0.75f * Mathf.Max(0.1f, PluginSettings.GelSpotSize);
            GelSystem.PrepareAndRegisterStain(marker);
            return false;
        }
    }

    internal sealed class GelProjectileMarker : MonoBehaviour
    {
        private bool hit;

        private void Start()
        {
            Rigidbody body = GetComponent<Rigidbody>();
            if (body != null)
                body.velocity *= Mathf.Max(0.1f, PluginSettings.GelProjectileSpeedMultiplier);

            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                Color blue = new Color(0.05f, 0.4f, 1f, 1f);
                block.SetColor("_Color", blue);
                block.SetColor("_BaseColor", blue);
                block.SetColor("_EmissionColor", blue * 2f);
                renderer.SetPropertyBlock(block);
            }
        }

        internal void HitEnemy(GasolineProjectile projectile, Collider other, EnemyIdentifier enemy)
        {
            if (hit || projectile == null || other == null || enemy == null)
                return;
            hit = true;
            GelSystem.AddCoverage(enemy);
            Destroy(gameObject);
        }
    }

    internal sealed class GelCoverage : MonoBehaviour
    {
        internal float Amount;

        internal void ApplyVisual()
        {
            SetOiledAmount(Amount);
        }

        private void OnDestroy()
        {
            SetOiledAmount(0f);
        }

        private void SetOiledAmount(float amount)
        {
            EnemyIdentifier enemy = GetComponent<EnemyIdentifier>();
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null || renderer is ParticleSystemRenderer ||
                    (enemy != null && enemy.buffUnaffectedRenderers != null && enemy.buffUnaffectedRenderers.Contains(renderer)))
                    continue;
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetFloat("_OiledAmount", Mathf.Clamp01(amount));
                renderer.SetPropertyBlock(block);
            }
        }
    }

    internal sealed class GelStainMarker : MonoBehaviour
    {
        internal Collider Surface;
        internal float Radius;
        internal bool EnemyVisual;
    }

    internal static class GelSystem
    {
        private static readonly List<GelStainMarker> stains = new List<GelStainMarker>();
        private static readonly HashSet<GrenadeLauncherProjectile> stuckGrenades = new HashSet<GrenadeLauncherProjectile>();

        internal static void AddCoverage(EnemyIdentifier enemy)
        {
            if (enemy == null)
                return;
            GelCoverage coverage = enemy.GetComponent<GelCoverage>();
            if (coverage == null)
                coverage = enemy.gameObject.AddComponent<GelCoverage>();
            coverage.Amount = Mathf.Clamp01(coverage.Amount + PluginSettings.GelCoveragePerDroplet);
            coverage.ApplyVisual();
        }

        internal static void PrepareAndRegisterStain(GelStainMarker marker)
        {
            if (marker == null)
                return;
            Color blue = new Color(0.05f, 0.4f, 1f, 1f);
            foreach (Renderer renderer in marker.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                renderer.enabled = true;
                Material material = renderer.material;
                if (material != null)
                {
                    if (material.HasProperty("_Color")) material.SetColor("_Color", blue);
                    if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", blue);
                    if (material.HasProperty("_TintColor")) material.SetColor("_TintColor", blue);
                    if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", blue * 1.25f);
                }
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", blue);
                block.SetColor("_BaseColor", blue);
                block.SetColor("_TintColor", blue);
                block.SetColor("_EmissionColor", blue * 1.25f);
                renderer.SetPropertyBlock(block);
            }
            stains.Add(marker);
        }

        internal static bool TryGetStickTarget(
            Collider surface,
            EnemyIdentifier enemy,
            Vector3 point,
            out GelStainMarker stain,
            out GelCoverage coverage)
        {
            stain = null;
            coverage = null;
            if (enemy != null)
            {
                coverage = enemy.GetComponent<GelCoverage>();
                return coverage != null && coverage.Amount >= PluginSettings.GelCoverageRequired;
            }

            stain = FindStain(surface, point);
            return stain != null;
        }

        internal static GelStainMarker FindStain(Collider surface, Vector3 point)
        {
            stains.RemoveAll(stain => stain == null);
            foreach (GelStainMarker stain in stains)
            {
                if (stain.EnemyVisual || stain.Surface == null)
                    continue;
                bool sameSurface = surface == stain.Surface || surface.transform == stain.Surface.transform ||
                    (surface.attachedRigidbody != null && stain.Surface.attachedRigidbody == surface.attachedRigidbody);
                if (!sameSurface)
                    continue;
                if (Vector3.Distance(point, stain.transform.position) <= stain.Radius + 0.3f)
                    return stain;
            }
            return null;
        }

        internal static void Consume(
            GelStainMarker stain,
            GelCoverage coverage,
            Vector3 origin,
            float visibleRadius)
        {
            if (stain != null)
            {
                stains.RemoveAll(candidate => candidate == null);
                float radius = Mathf.Max(stain.Radius + 0.3f, visibleRadius);
                GelStainMarker[] snapshot = stains.ToArray();
                foreach (GelStainMarker candidate in snapshot)
                {
                    if (candidate == null || candidate.EnemyVisual || !SameSurface(candidate.Surface, stain.Surface))
                        continue;
                    if (Vector3.Distance(origin, candidate.transform.position) > radius + candidate.Radius)
                        continue;
                    stains.Remove(candidate);
                    UnityEngine.Object.Destroy(candidate.gameObject);
                }
            }
            if (coverage != null)
                UnityEngine.Object.Destroy(coverage);
        }

        private static bool SameSurface(Collider first, Collider second)
        {
            if (first == null || second == null)
                return false;
            return first == second || first.transform == second.transform ||
                (first.attachedRigidbody != null && first.attachedRigidbody == second.attachedRigidbody);
        }

        internal static void RegisterStuck(GrenadeLauncherProjectile projectile)
        {
            if (projectile != null)
                stuckGrenades.Add(projectile);
        }

        internal static void UnregisterStuck(GrenadeLauncherProjectile projectile)
        {
            if (projectile != null)
                stuckGrenades.Remove(projectile);
        }

        internal static void TriggerChain(Vector3 origin, GrenadeLauncherProjectile source, float visibleRadius)
        {
            float radius = Mathf.Max(0f, visibleRadius * PluginSettings.StuckChainRadiusMultiplier);
            if (radius <= 0f)
                return;
            float propagationSpeed = Mathf.Max(1f, PluginSettings.StuckChainPropagationSpeed);
            GrenadeLauncherProjectile[] snapshot = new GrenadeLauncherProjectile[stuckGrenades.Count];
            stuckGrenades.CopyTo(snapshot);
            foreach (GrenadeLauncherProjectile projectile in snapshot)
            {
                if (projectile == null || projectile == source || !projectile.IsStuck)
                    continue;
                float distance = Vector3.Distance(origin, projectile.transform.position);
                if (distance <= radius)
                    projectile.ScheduleChainDetonation(distance / propagationSpeed, source.ChainGroupId);
            }
        }

        internal static void Cleanup()
        {
            GrenadeLauncherProjectile[] stuckSnapshot = new GrenadeLauncherProjectile[stuckGrenades.Count];
            stuckGrenades.CopyTo(stuckSnapshot);
            foreach (GrenadeLauncherProjectile projectile in stuckSnapshot)
            {
                if (projectile != null)
                    UnityEngine.Object.Destroy(projectile.gameObject);
            }
            foreach (GelStainMarker stain in stains)
            {
                if (stain != null)
                    UnityEngine.Object.Destroy(stain.gameObject);
            }
            foreach (GelCoverage coverage in UnityEngine.Object.FindObjectsOfType<GelCoverage>())
            {
                if (coverage != null)
                    UnityEngine.Object.Destroy(coverage);
            }
            stains.Clear();
            stuckGrenades.Clear();
        }
    }

    internal static class StuckDetonationContext
    {
        private static readonly Stack<RevolverBeam> beams = new Stack<RevolverBeam>();
        internal static RevolverBeam Current => beams.Count > 0 ? beams.Peek() : null;

        internal static bool CanCurrentBeamDetonate
        {
            get
            {
                RevolverBeam beam = Current;
                if (beam == null)
                    return false;
                if (beam.beamType == BeamType.Revolver)
                    return true;
                if (beam.beamType != BeamType.Railgun)
                    return false;
                Railcannon rail = beam.sourceWeapon != null ? beam.sourceWeapon.GetComponentInParent<Railcannon>() : null;
                return rail != null && rail.variation != 1;
            }
        }

        internal static void Enter(RevolverBeam beam) => beams.Push(beam);
        internal static void Exit()
        {
            if (beams.Count > 0)
                beams.Pop();
        }
    }

    [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.ExecuteHits))]
    internal static class StuckBeamContextPatch
    {
        private static void Prefix(RevolverBeam __instance) => StuckDetonationContext.Enter(__instance);
        private static Exception Finalizer(Exception __exception)
        {
            StuckDetonationContext.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Coin), "ExplosiveReflectCheck")]
    internal static class StuckGrenadeCoinTargetPatch
    {
        private static bool Prefix(ULTRAKILL.Enemy.TargetDataRef data, ref bool __result)
        {
            GameObject target = data.target != null ? data.target.GameObject : null;
            GrenadeLauncherProjectile projectile = target != null
                ? target.GetComponentInParent<GrenadeLauncherProjectile>()
                : null;
            if (projectile == null || !projectile.IsStuck)
                return true;
            __result = false;
            return false;
        }
    }

    // Kept as a reusable visual recipe for a future feature. This was the attractive
    // placeholder from v3.0.0, but it is deliberately no longer used as a HookPoint.
    internal static class BlueSphereVisualFactory
    {
        internal static GameObject Create(Vector3 position, float size)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "Grenade Launcher Generated Blue Sphere";
            sphere.transform.position = position;
            sphere.transform.localScale = Vector3.one * Mathf.Max(0.25f, size);
            Renderer renderer = sphere.GetComponent<Renderer>();
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Unlit/Color");
            if (renderer != null && shader != null)
            {
                renderer.material = new Material(shader);
                renderer.material.color = new Color(0.05f, 0.45f, 1f, 1f);
                if (renderer.material.HasProperty("_EmissionColor"))
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", new Color(0f, 0.2f, 1f) * 2f);
                }
            }
            Light light = sphere.AddComponent<Light>();
            light.color = new Color(0.05f, 0.4f, 1f);
            light.range = 10f;
            light.intensity = 2.5f;
            return sphere;
        }
    }

    internal sealed class BlueHookDeliveryProjectile : MonoBehaviour
    {
        private const float TravelDuration = 0.25f;

        internal Grenade Grenade;
        internal Vector3 Target;

        private Vector3 start;
        private float startedAt;
        private bool launched;
        private bool completed;

        internal void Launch()
        {
            if (launched || Grenade == null)
                return;
            launched = true;
            start = transform.position;
            startedAt = Time.time;

            Rigidbody body = Grenade.rb != null ? Grenade.rb : GetComponent<Rigidbody>();
            if (body != null)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.useGravity = false;
                PhysicsExtensions.SetGravityMode(body, false);
                body.isKinematic = true;
            }
            foreach (CustomGravity gravity in GetComponents<CustomGravity>())
            {
                if (gravity != null)
                    gravity.useGravity = false;
            }
            foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = false;
            }
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
            foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
            {
                if (trail != null)
                    trail.enabled = false;
            }
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particles != null)
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (Light light in GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    light.enabled = false;
            }

            GameObject sphere = BlueSphereVisualFactory.Create(start, PluginSettings.BluePointSize);
            Collider sphereCollider = sphere.GetComponent<Collider>();
            if (sphereCollider != null)
            {
                sphereCollider.enabled = false;
                Destroy(sphereCollider);
            }
            sphere.transform.SetParent(transform, true);
        }

        private void Update()
        {
            if (!launched || completed)
                return;
            float progress = Mathf.Clamp01((Time.time - startedAt) / TravelDuration);
            transform.position = Vector3.Lerp(start, Target, progress);
            if (progress < 1f)
                return;

            completed = true;
            HookPointManager.CompleteDelivery(Target);
            Destroy(gameObject);
        }
    }

    internal static class HookPointManager
    {
        private const string SlingshotAddress = "Assets/Prefabs/Levels/Interactive/GrapplePointSlingshot Variant.prefab";
        private const string ProvidenceSlingshotAddress = "Assets/Prefabs/Levels/Interactive/GrapplePointSlingshotProvidence.prefab";
        private const string PlayerShockwaveAddress = "Assets/Prefabs/Attacks and Projectiles/PhysicalShockwavePlayer.prefab";
        private const string DeleteEffectAddress = "Assets/Particles/SandboxDeleterEffect.prefab";
        private const string SandboxArmAddress = "Assets/Prefabs/Weapons/Special/Spawner Arm.prefab";
        private static GameObject current;
        private static GameObject slingshotPrefab;
        private static GameObject providenceSlingshotPrefab;
        private static GameObject providenceExplosionEffectPrefab;
        private static GameObject playerShockwavePrefab;
        private static GameObject rocketExplosionPrefab;
        private static GameObject deleteEffectPrefab;
        private static AudioClip deleteSound;
        private static bool loggedMissingPrefab;
        private static bool loggedMissingReplacementExplosion;
        private static bool creationLockedUntilGround;
        private static bool currentHookUsed;
        private static HookPoint currentHook;
        private static HookArm currentArm;
        private static float nextArmLookupAt;
        private static float nextHookStateCheckAt;
        private static readonly FieldInfo CaughtHookField = AccessTools.Field(typeof(HookArm), "caughtHook");

        internal static bool TryGetDeliveryTarget(out Vector3 point)
        {
            point = Vector3.zero;
            UpdateUsageLock();
            if (creationLockedUntilGround)
                return false;
            CameraController camera = MonoSingleton<CameraController>.Instance;
            if (camera == null)
                return false;

            Vector3 origin = camera.transform.position;
            Vector3 direction = camera.transform.forward;
            float maximum = Mathf.Max(5.5f, PluginSettings.BlueDistance);
            point = origin + direction * maximum;
            int environmentMask = LayerMaskDefaults.Get(LMD.Environment);
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maximum, environmentMask, QueryTriggerInteraction.Ignore))
                point = hit.point + hit.normal * 0.25f;
            if (Vector3.Distance(origin, point) < 5.5f)
                return false;

            GameObject prefab = ResolveSlingshotPrefab();
            if (prefab == null)
            {
                if (!loggedMissingPrefab)
                {
                    loggedMissingPrefab = true;
                    Plugin.LogSource?.LogError("Could not find ULTRAKILL's blue slingshot HookPoint prefab in the loaded spawnable database.");
                }
                return false;
            }

            return true;
        }

        internal static void UpdateUsageLock()
        {
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (current == null)
            {
                creationLockedUntilGround = false;
                currentHookUsed = false;
                currentHook = null;
                currentArm = null;
                return;
            }

            if (currentHookUsed)
            {
                if (creationLockedUntilGround && movement != null && movement.gc != null && movement.gc.onGround)
                    creationLockedUntilGround = false;
                return;
            }

            if (Time.unscaledTime < nextHookStateCheckAt)
                return;
            nextHookStateCheckAt = Time.unscaledTime + 0.05f;

            if (currentHook == null)
                currentHook = current.GetComponentInChildren<HookPoint>(true);
            if (currentArm == null && Time.unscaledTime >= nextArmLookupAt)
            {
                nextArmLookupAt = Time.unscaledTime + 0.5f;
                currentArm = UnityEngine.Object.FindObjectOfType<HookArm>();
            }

            if (currentHook != null && currentArm != null && CaughtHookField != null &&
                ReferenceEquals(CaughtHookField.GetValue(currentArm), currentHook))
            {
                currentHookUsed = true;
                creationLockedUntilGround = movement == null || movement.gc == null || !movement.gc.onGround;
            }
        }

        internal static void CompleteDelivery(Vector3 point)
        {
            Vector3 replacedPoint = current != null ? current.transform.position : point;
            if (current != null && current.GetComponent<GeneratedBlueHookOwnership>() != null)
                SpawnReplacementExplosion(replacedPoint);
            PlayDeleteEffect(point);
            GameObject prefab = ResolveSlingshotPrefab();
            if (prefab == null)
                return;
            Cleanup();
            current = UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity);
            current.name = "Grenade Launcher Blue Slingshot Point";
            current.AddComponent<GeneratedBlueHookOwnership>();
            current.transform.localScale *= Mathf.Max(0.25f, PluginSettings.BluePointSize);
            HookPoint hook = current.GetComponentInChildren<HookPoint>(true);
            if (hook == null)
            {
                UnityEngine.Object.Destroy(current);
                current = null;
                Plugin.LogSource?.LogError("The resolved blue hook-point prefab did not contain a HookPoint component.");
                return;
            }
            hook.active = true;
            hook.type = hookPointType.Slingshot;
            hook.slingShotForce += PluginSettings.BlueSlingshotForce;
            hook.healPlayer = false;
            hook.Activate();
            currentHook = hook;
            currentArm = UnityEngine.Object.FindObjectOfType<HookArm>();
            nextArmLookupAt = Time.unscaledTime + 0.5f;
            nextHookStateCheckAt = Time.unscaledTime;
            currentHookUsed = false;
            creationLockedUntilGround = false;
        }

        private static void SpawnReplacementExplosion(Vector3 point)
        {
            GameObject prefab = ResolveProvidenceExplosionEffectPrefab();
            if (prefab == null)
            {
                if (!loggedMissingReplacementExplosion)
                {
                    loggedMissingReplacementExplosion = true;
                    Plugin.LogSource?.LogWarning("Could not resolve the Providence hook-point explosion effect; using the fallback blast visual.");
                }
                prefab = ResolveRocketExplosionPrefab();
                if (prefab == null)
                    return;
            }

            float sizeMultiplier = Mathf.Max(0.1f, PluginSettings.BlueReplacementExplosionSize);
            GameObject blast = UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity);
            GrenadeLauncherExplosionMarker marker = blast.AddComponent<GrenadeLauncherExplosionMarker>();
            marker.BlueHookReplacement = true;
            marker.Mode = GrenadeExplosionMode.Surface;
            marker.Damage = Mathf.Max(0f, PluginSettings.BlueReplacementDamage);

            float groundSlamForce = ResolveGroundSlamForce();
            // Projectile height is proportional to launch velocity squared, so sqrt(1.25)
            // produces 1.25 times the final height rather than 1.25 times the velocity.
            float heightMultiplier = Mathf.Max(0f, PluginSettings.BlueReplacementForceMultiplier);
            float launchForce = groundSlamForce * Mathf.Sqrt(heightMultiplier);
            bool configuredNativeShockwave = false;
            PhysicalShockwave[] nativeShockwaves = blast.GetComponentsInChildren<PhysicalShockwave>(true);
            foreach (PhysicalShockwave shockwave in nativeShockwaves)
            {
                if (shockwave == null)
                    continue;
                configuredNativeShockwave = true;
                shockwave.damage = Mathf.RoundToInt(marker.Damage * 10f);
                shockwave.maxSize *= sizeMultiplier;
                shockwave.force = launchForce;
                shockwave.hasHurtPlayer = true;
                shockwave.enemy = false;
                shockwave.noDamageToEnemy = false;
            }

            foreach (Explosion explosion in blast.GetComponentsInChildren<Explosion>(true))
            {
                if (explosion == null)
                    continue;
                explosion.sourceWeapon = null;
                // Providence's PhysicalShockwave owns the mechanics.  Any Explosion on the
                // visual object is kept cosmetic to avoid duplicate damage and radial force.
                explosion.damage = 0;
                explosion.playerDamageOverride = 0;
                explosion.maxSize *= sizeMultiplier;
                explosion.speed *= sizeMultiplier;
                explosion.pushForceMultiplier = 0f;
                explosion.playerProjectileForceDirection = Vector3.zero;
                explosion.enemyDamageMultiplier = 1f;
                explosion.ignite = false;
                explosion.rocketExplosion = false;
                explosion.isFup = false;
                explosion.boosted = false;
                explosion.unblockable = false;
            }

            if (!configuredNativeShockwave)
                SpawnInvisibleGroundSlamShockwave(point, marker.Damage, sizeMultiplier, launchForce);
        }

        private static GameObject ResolveProvidenceExplosionEffectPrefab()
        {
            if (providenceExplosionEffectPrefab != null)
                return providenceExplosionEffectPrefab;
            try
            {
                GameObject prefab = ResolveProvidenceSlingshotPrefab();
                HookPoint hook = prefab != null ? prefab.GetComponentInChildren<HookPoint>(true) : null;
                if (hook == null || hook.onReach == null)
                    return null;

                List<GameObject> candidates = new List<GameObject>();
                if (hook.onReach.toActivateObjects != null)
                    candidates.AddRange(hook.onReach.toActivateObjects.Where(item => item != null));
                if (hook.onReach.onActivate != null)
                {
                    int count = hook.onReach.onActivate.GetPersistentEventCount();
                    for (int i = 0; i < count; i++)
                    {
                        UnityEngine.Object target = hook.onReach.onActivate.GetPersistentTarget(i);
                        Component component = target as Component;
                        GameObject targetObject = target as GameObject;
                        if (component != null)
                            candidates.Add(component.gameObject);
                        else if (targetObject != null)
                            candidates.Add(targetObject);
                    }
                }

                List<GameObject> expanded = new List<GameObject>(candidates);
                FieldInfo instantiateSource = AccessTools.Field(typeof(InstantiateObject), "source");
                foreach (GameObject candidate in candidates)
                foreach (InstantiateObject instantiator in candidate.GetComponentsInChildren<InstantiateObject>(true))
                {
                    GameObject source = instantiator != null
                        ? instantiateSource?.GetValue(instantiator) as GameObject
                        : null;
                    if (source != null)
                        expanded.Add(source);
                }

                providenceExplosionEffectPrefab = expanded
                    .Distinct()
                    .OrderByDescending(ScoreProvidenceEffectCandidate)
                    .FirstOrDefault(candidate => ScoreProvidenceEffectCandidate(candidate) > 0);
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not resolve Providence hook-point explosion effect: " + exception.Message);
            }
            return providenceExplosionEffectPrefab;
        }

        private static int ScoreProvidenceEffectCandidate(GameObject candidate)
        {
            if (candidate == null)
                return 0;
            int score = candidate.GetComponentsInChildren<PhysicalShockwave>(true).Length * 1000;
            score += candidate.GetComponentsInChildren<Explosion>(true).Length * 500;
            score += candidate.GetComponentsInChildren<ParticleSystem>(true).Length * 10;
            score += candidate.GetComponentsInChildren<AudioSource>(true).Length;
            return score;
        }

        private static float ResolveGroundSlamForce()
        {
            try
            {
                if (playerShockwavePrefab == null)
                    playerShockwavePrefab = Addressables.LoadAssetAsync<GameObject>(PlayerShockwaveAddress).WaitForCompletion();
                PhysicalShockwave shockwave = playerShockwavePrefab != null
                    ? playerShockwavePrefab.GetComponentInChildren<PhysicalShockwave>(true)
                    : null;
                if (shockwave != null && shockwave.force > 0f)
                    return shockwave.force;
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not read the player ground-slam force: " + exception.Message);
            }
            return 100f;
        }

        private static void SpawnInvisibleGroundSlamShockwave(Vector3 point, float damage, float sizeMultiplier, float launchForce)
        {
            if (playerShockwavePrefab == null)
                return;
            GameObject mechanics = UnityEngine.Object.Instantiate(playerShockwavePrefab, point, Quaternion.identity);
            foreach (PhysicalShockwave shockwave in mechanics.GetComponentsInChildren<PhysicalShockwave>(true))
            {
                shockwave.damage = Mathf.RoundToInt(Mathf.Max(0f, damage) * 10f);
                shockwave.maxSize *= sizeMultiplier;
                shockwave.force = launchForce;
                shockwave.hasHurtPlayer = true;
                shockwave.enemy = false;
                shockwave.noDamageToEnemy = false;
            }
            foreach (Renderer renderer in mechanics.GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;
            foreach (ParticleSystem particles in mechanics.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (Light light in mechanics.GetComponentsInChildren<Light>(true))
                light.enabled = false;
            foreach (AudioSource audio in mechanics.GetComponentsInChildren<AudioSource>(true))
                audio.mute = true;
        }

        private static GameObject ResolveProvidenceSlingshotPrefab()
        {
            if (providenceSlingshotPrefab != null)
                return providenceSlingshotPrefab;
            try
            {
                providenceSlingshotPrefab = Addressables.LoadAssetAsync<GameObject>(ProvidenceSlingshotAddress).WaitForCompletion();
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load Providence hook-point visual: " + exception.Message);
            }
            return providenceSlingshotPrefab;
        }

        private static GameObject ResolveRocketExplosionPrefab()
        {
            if (rocketExplosionPrefab != null)
                return rocketExplosionPrefab;

            Grenade active = UnityEngine.Object.FindObjectsOfType<Grenade>()
                .FirstOrDefault(grenade => grenade != null && grenade.explosion != null);
            if (active != null)
                rocketExplosionPrefab = active.explosion;
            if (rocketExplosionPrefab == null)
            {
                Grenade loaded = Resources.FindObjectsOfTypeAll<Grenade>()
                    .FirstOrDefault(grenade => grenade != null && grenade.explosion != null);
                if (loaded != null)
                    rocketExplosionPrefab = loaded.explosion;
            }
            return rocketExplosionPrefab;
        }

        private static void PlayDeleteEffect(Vector3 point)
        {
            try
            {
                if (deleteEffectPrefab == null)
                    deleteEffectPrefab = Addressables.LoadAssetAsync<GameObject>(DeleteEffectAddress).WaitForCompletion();
                if (deleteEffectPrefab != null)
                {
                    GameObject effect = UnityEngine.Object.Instantiate(deleteEffectPrefab, point, Quaternion.identity);
                    UnityEngine.Object.Destroy(effect, 5f);
                }

                if (deleteSound == null)
                {
                    GameObject armPrefab = Addressables.LoadAssetAsync<GameObject>(SandboxArmAddress).WaitForCompletion();
                    Sandbox.Arm.SandboxArm arm = armPrefab != null
                        ? armPrefab.GetComponentInChildren<Sandbox.Arm.SandboxArm>(true)
                        : null;
                    if (arm != null && arm.destroySound != null)
                        deleteSound = arm.destroySound.clip;
                }
                if (deleteSound != null)
                {
                    CameraController camera = MonoSingleton<CameraController>.Instance;
                    GameObject soundHost = new GameObject("Grenade Launcher Sandbox Delete Sound");
                    if (camera != null)
                    {
                        soundHost.transform.SetParent(camera.transform, false);
                        soundHost.transform.localPosition = Vector3.zero;
                    }
                    else
                    {
                        soundHost.transform.position = point;
                    }
                    AudioSource source = soundHost.AddComponent<AudioSource>();
                    source.clip = deleteSound;
                    source.volume = 0.5f;
                    source.spatialBlend = 0f;
                    source.playOnAwake = false;
                    source.loop = false;
                    source.Play();
                    UnityEngine.Object.Destroy(soundHost, Mathf.Max(0.1f, deleteSound.length + 0.1f));
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not play the sandbox deletion effect: " + exception.Message);
            }
        }

        private static GameObject ResolveSlingshotPrefab()
        {
            if (slingshotPrefab != null)
                return slingshotPrefab;

            try
            {
                slingshotPrefab = Addressables.LoadAssetAsync<GameObject>(SlingshotAddress).WaitForCompletion();
                if (slingshotPrefab != null)
                    return slingshotPrefab;
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load the blue HookPoint addressable: " + exception.Message);
            }

            HookPoint loaded = Resources.FindObjectsOfTypeAll<HookPoint>()
                .FirstOrDefault(point => point != null && point.type == hookPointType.Slingshot &&
                                         point.GetComponentInParent<GeneratedBlueHookOwnership>() == null);
            if (loaded != null)
                slingshotPrefab = loaded.gameObject;
            return slingshotPrefab;
        }

        internal static void Cleanup()
        {
            if (current != null)
            {
                HookPoint point = current.GetComponentInChildren<HookPoint>(true);
                HookArm arm = UnityEngine.Object.FindObjectOfType<HookArm>();
                FieldInfo caughtHook = AccessTools.Field(typeof(HookArm), "caughtHook");
                if (arm != null && point != null && caughtHook != null && ReferenceEquals(caughtHook.GetValue(arm), point))
                {
                    try
                    {
                        AccessTools.Method(typeof(HookArm), "StopThrow")?.Invoke(arm, new object[] { 0f, false });
                    }
                    catch (Exception exception)
                    {
                        Plugin.LogSource?.LogWarning("Could not release the old generated hook point cleanly: " + exception.Message);
                    }
                }
                UnityEngine.Object.Destroy(current);
            }
            current = null;
            creationLockedUntilGround = false;
            currentHookUsed = false;
            currentHook = null;
            currentArm = null;
            nextArmLookupAt = 0f;
            nextHookStateCheckAt = 0f;
        }
    }

    internal sealed class GeneratedBlueHookOwnership : MonoBehaviour
    {
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
    internal static class GrenadeLauncherRespawnCooldownPatch
    {
        private static void Postfix() => AlternateFireController.Reset();
    }

    internal sealed class AlternateFireRuntime : MonoBehaviour
    {
        private bool lastGrenadeMode;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            lastGrenadeMode = Plugin.Instance != null && Plugin.Instance.AnyGrenadeModeEnabled;
        }

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void Update()
        {
            HookPointManager.UpdateUsageLock();
            bool enabled = Plugin.Instance != null && Plugin.Instance.AnyGrenadeModeEnabled;
            if (lastGrenadeMode && !enabled)
                CleanupAll();
            lastGrenadeMode = enabled;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => CleanupAll();

        internal static void CleanupAll()
        {
            HookPointManager.Cleanup();
            GelSystem.Cleanup();
            AlternateFireController.Reset();
            AlternateFireInputContext.Depth = 0;
            AlternateFireInputContext.SuppressedAction = null;
            GelSpawnContext.Depth = 0;
            GelStainSpawnContext.Reset();
            BlueHookDeliveryContext.Reset();
            GrenadeStyleAwards.ResetWalkingBombChains();
            foreach (BlueHookDeliveryProjectile delivery in UnityEngine.Object.FindObjectsOfType<BlueHookDeliveryProjectile>())
            {
                if (delivery != null)
                    UnityEngine.Object.Destroy(delivery.gameObject);
            }
        }
    }
}
