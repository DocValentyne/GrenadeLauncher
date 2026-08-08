using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PluginConfig.API;
using PluginConfig.API.Fields;
using PluginConfig.API.Functionals;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GrenadeLauncherMod
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("com.eternalUnion.pluginConfigurator")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "docvalentyne.ultrakill.grenadelauncher";
        public const string Name = "Grenade Launcher";
        public const string Version = "1.1.0";

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource LogSource { get; private set; }

        private readonly Dictionary<int, ConfigEntry<bool>> slotLoadouts = new Dictionary<int, ConfigEntry<bool>>();
        private readonly Dictionary<string, ConfigEntry<bool>> variantLoadouts = new Dictionary<string, ConfigEntry<bool>>();
        private readonly Dictionary<int, ConfigEntry<bool>> variantMigrations = new Dictionary<int, ConfigEntry<bool>>();
        private Harmony harmony;
        private GameObject runtimeHost;

        internal bool IsGrenadeModeEnabled(int variation)
        {
            if (variation < 0 || variation > 2)
                return false;

            int slot = Math.Max(0, GameProgressSaver.currentSlot);
            ConfigEntry<bool> migrated = GetVariantMigrationEntry(slot);
            if (!migrated.Value)
                return GetSlotEntry(slot).Value;
            return GetVariantEntry(slot, variation).Value;
        }

        internal bool IsGrenadeModeEnabled(RocketLauncher launcher) =>
            launcher != null && IsGrenadeModeEnabled(launcher.variation);

        internal bool AnyGrenadeModeEnabled
        {
            get
            {
                int slot = Math.Max(0, GameProgressSaver.currentSlot);
                ConfigEntry<bool> migrated = GetVariantMigrationEntry(slot);
                if (!migrated.Value)
                    return GetSlotEntry(slot).Value;
                return GetVariantEntry(slot, 0).Value || GetVariantEntry(slot, 1).Value || GetVariantEntry(slot, 2).Value;
            }
        }

        internal void SetGrenadeModeEnabled(int variation, bool enabled)
        {
            if (variation < 0 || variation > 2)
                return;
            int slot = Math.Max(0, GameProgressSaver.currentSlot);
            EnsureVariantMigration();
            GetVariantEntry(slot, variation).Value = enabled;
            Config.Save();
        }

        internal void EnsureVariantMigration()
        {
            int slot = Math.Max(0, GameProgressSaver.currentSlot);
            ConfigEntry<bool> migrated = GetVariantMigrationEntry(slot);
            if (migrated.Value)
                return;

            bool legacyEnabled = GetSlotEntry(slot).Value;
            for (int variation = 0; variation < 3; variation++)
                GetVariantEntry(slot, variation).Value = legacyEnabled;
            migrated.Value = true;
            Config.Save();
            Logger.LogInfo("Migrated the old all-rocket loadout to per-variant Grenade Launcher choices for save slot " + slot + ".");
        }

        private void Awake()
        {
            Instance = this;
            LogSource = Logger;

            PluginSettings.Initialize();

            harmony = new Harmony(Guid);
            harmony.PatchAll(typeof(Plugin).Assembly);

            runtimeHost = new GameObject("Grenade Launcher Runtime");
            DontDestroyOnLoad(runtimeHost);
            runtimeHost.hideFlags = HideFlags.HideAndDontSave;
            runtimeHost.AddComponent<TerminalIntegration>();
            runtimeHost.AddComponent<AlternateFireRuntime>();

            Logger.LogInfo("Grenade Launcher v" + Version + " loaded.");
        }

        private void OnDestroy()
        {
            harmony?.UnpatchSelf();
            GrenadeSpawnContext.Reset();
            GrenadeLauncherHookContext.Reset();
            AlternateFireRuntime.CleanupAll();
            TerminalIntegration.Cleanup();
            if (runtimeHost != null)
                Destroy(runtimeHost);
            if (Instance == this)
                Instance = null;
            if (LogSource == Logger)
                LogSource = null;
        }

        private ConfigEntry<bool> GetSlotEntry(int slot)
        {
            if (slot < 0)
                slot = 0;

            if (!slotLoadouts.TryGetValue(slot, out ConfigEntry<bool> entry))
            {
                entry = Config.Bind(
                    "Loadout",
                    "SaveSlot" + slot + "UsesGrenadeLauncher",
                    false,
                    "Equip Grenade Launcher primary fire for all three Rocket Launcher variants in this save slot.");
                slotLoadouts.Add(slot, entry);
            }

            return entry;
        }

        private ConfigEntry<bool> GetVariantEntry(int slot, int variation)
        {
            string key = slot + ":" + variation;
            if (!variantLoadouts.TryGetValue(key, out ConfigEntry<bool> entry))
            {
                entry = Config.Bind("Loadout", "SaveSlot" + slot + "RocketVariant" + variation + "UsesGrenadeLauncher",
                    false, "Use the alternate Grenade Launcher form for this Rocket Launcher variant.");
                variantLoadouts.Add(key, entry);
            }
            return entry;
        }

        private ConfigEntry<bool> GetVariantMigrationEntry(int slot)
        {
            if (!variantMigrations.TryGetValue(slot, out ConfigEntry<bool> entry))
            {
                entry = Config.Bind("Loadout", "SaveSlot" + slot + "PerVariantLoadoutMigrated", false,
                    "Whether the old all-rocket setting has been copied to the native per-variant terminal controls.");
                variantMigrations.Add(slot, entry);
            }
            return entry;
        }

    }

    // Grenade Launcher scores are still stored in the player's local save, but must not be
    // uploaded to the public Cyber Grind leaderboard. FinalCyberRank performs local-best
    // handling separately after this method returns, so suppressing this call affects only
    // the Steam upload and leaves ordinary progression intact.
    [HarmonyPatch(typeof(LeaderboardController), nameof(LeaderboardController.SubmitCyberGrindScore))]
    internal static class CyberGrindLeaderboardPatch
    {
        private static bool Prefix()
        {
            Plugin.LogSource?.LogInfo("Blocked Cyber Grind leaderboard submission while Grenade Launcher is loaded; local high score remains enabled.");
            return false;
        }
    }

    internal enum GrenadeProjectileProfile
    {
        Primary,
        GreenContact,
        BlueDelivery
    }

    internal static class GrenadeSpawnContext
    {
        private static int depth;
        internal static bool Active => depth > 0;
        internal static GrenadeProjectileProfile Profile { get; private set; }

        internal static void Enter(GrenadeProjectileProfile profile = GrenadeProjectileProfile.Primary)
        {
            if (depth == 0)
                Profile = profile;
            depth++;
        }
        internal static void Exit() => depth = Math.Max(0, depth - 1);
        internal static void Reset()
        {
            depth = 0;
            Profile = GrenadeProjectileProfile.Primary;
        }
    }

    internal static class RocketCooldownSync
    {
        private static readonly AccessTools.FieldRef<RocketLauncher, float> Cooldown =
            AccessTools.FieldRefAccess<RocketLauncher, float>("cooldown");
        private static float standardReadyAt;
        private static float grenadeReadyAt;

        internal static bool Ready(RocketLauncher launcher) =>
            CooldownRules.NoWeaponCooldown || launcher == null || Cooldown(launcher) <= 0f;

        internal static void ApplyAfterShot(RocketLauncher firedLauncher, bool grenadeShot)
        {
            if (CooldownRules.NoWeaponCooldown)
            {
                Reset();
                return;
            }

            float grenadeInterval = Mathf.Max(0f, PluginSettings.FireInterval);
            grenadeReadyAt = Time.time + grenadeInterval;
            standardReadyAt = Time.time + (grenadeShot
                ? grenadeInterval
                : Mathf.Max(0f, firedLauncher != null ? firedLauncher.rateOfFire : 1f));
        }

        internal static void ApplyWhenEquipped(RocketLauncher launcher)
        {
            if (launcher == null)
                return;

            if (CooldownRules.NoWeaponCooldown)
            {
                Reset();
                Cooldown(launcher) = 0f;
            }
            else
            {
                bool grenade = Plugin.Instance != null && Plugin.Instance.IsGrenadeModeEnabled(launcher);
                float readyAt = grenade ? grenadeReadyAt : standardReadyAt;
                Cooldown(launcher) = Mathf.Max(0f, readyAt - Time.time);
            }
        }

        internal static void Reset()
        {
            standardReadyAt = 0f;
            grenadeReadyAt = 0f;
        }
    }

    internal static class CooldownRules
    {
        internal static bool NoWeaponCooldown => ULTRAKILL.Cheats.NoWeaponCooldown.NoCooldown;
    }

    [HarmonyPatch(typeof(RocketLauncher), "OnEnable")]
    internal static class RocketLauncherEquipCooldownPatch
    {
        [HarmonyPriority(Priority.Last)]
        private static void Postfix(RocketLauncher __instance) => RocketCooldownSync.ApplyWhenEquipped(__instance);
    }

    [HarmonyPatch(typeof(RocketLauncher), nameof(RocketLauncher.Shoot))]
    internal static class RocketLauncherShootPatch
    {
        private static readonly AccessTools.FieldRef<RocketLauncher, WeaponIdentifier> WeaponId =
            AccessTools.FieldRefAccess<RocketLauncher, WeaponIdentifier>("wid");

        private struct State
        {
            internal bool Active;
            internal bool EnteredContext;
            internal bool DualWieldDuplicate;
            internal bool NotifyGreenPrimary;
            internal bool NotifyBluePrimary;
            internal float RateOfFire;
        }

        private static bool Prefix(RocketLauncher __instance, out State __state)
        {
            bool grenadeMode = Plugin.Instance != null && Plugin.Instance.IsGrenadeModeEnabled(__instance);
            WeaponIdentifier weaponId = WeaponId(__instance);
            bool dualWieldDuplicate = weaponId != null && weaponId.duplicate;
            __state = new State
            {
                Active = grenadeMode,
                DualWieldDuplicate = dualWieldDuplicate,
                RateOfFire = __instance.rateOfFire,
            };
            if (!__state.Active)
                return true;

            __instance.rateOfFire = PluginSettings.FireInterval;
            if (!GrenadeSpawnContext.Active)
            {
                __state.NotifyGreenPrimary = __instance.variation == 1;
                __state.NotifyBluePrimary = __instance.variation == 0;
                GrenadeSpawnContext.Enter();
                __state.EnteredContext = true;
            }
            return true;
        }

        private static Exception Finalizer(RocketLauncher __instance, State __state, Exception __exception)
        {
            Restore(__instance, __state);
            if (__exception == null && !__state.DualWieldDuplicate)
                RocketCooldownSync.ApplyAfterShot(__instance, __state.Active);
            if (__exception == null && __state.NotifyGreenPrimary)
                AlternateFireController.OnGreenPrimaryFired();
            if (__exception == null && __state.NotifyBluePrimary)
                AlternateFireController.OnBluePrimaryFired();
            return __exception;
        }

        private static void Restore(RocketLauncher launcher, State state)
        {
            if (!state.Active)
                return;

            launcher.rateOfFire = state.RateOfFire;
            if (state.EnteredContext)
                GrenadeSpawnContext.Exit();
        }
    }

    [HarmonyPatch(typeof(CameraController), nameof(CameraController.CameraShake), new[] { typeof(float) })]
    internal static class GrenadeLauncherCameraShakePatch
    {
        private static bool Prefix()
        {
            return !GrenadeSpawnContext.Active;
        }
    }

    [HarmonyPatch(typeof(ObjectTracker), nameof(ObjectTracker.GetGrenade))]
    internal static class GrenadeLauncherWhiplashPatch
    {
        private static void Postfix(ref Grenade __result)
        {
            if (__result == null)
                return;

            GrenadeLauncherProjectile marker = __result.GetComponent<GrenadeLauncherProjectile>();
            if (marker != null && !marker.CanBeWhiplashed)
                __result = null;
            if (__result != null && __result.GetComponent<BlueHookDeliveryProjectile>() != null)
                __result = null;
        }
    }

    internal static class GrenadeLauncherHookContext
    {
        private static int depth;
        internal static bool Active => depth > 0;
        internal static void Enter() => depth++;
        internal static void Exit() => depth = Math.Max(0, depth - 1);
        internal static void Reset() => depth = 0;
    }

    [HarmonyPatch(typeof(HookArm), "FixedUpdate")]
    internal static class GrenadeLauncherHookArmPatch
    {
        private static void Prefix()
        {
            GrenadeLauncherHookContext.Enter();
        }

        private static Exception Finalizer(Exception __exception)
        {
            GrenadeLauncherHookContext.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Explode), new[]
    {
        typeof(bool), typeof(bool), typeof(bool), typeof(float), typeof(bool), typeof(GameObject), typeof(bool)
    })]
    internal static class GrenadeLauncherHookExplosionPatch
    {
        private static bool Prefix(Grenade __instance)
        {
            if (__instance.GetComponent<BlueHookDeliveryProjectile>() != null)
                return false;

            GrenadeLauncherProjectile projectile = __instance.GetComponent<GrenadeLauncherProjectile>();
            if (projectile == null)
                return true;

            if (projectile.IsStuck)
            {
                if (StuckDetonationContext.CanCurrentBeamDetonate)
                    projectile.DetonateStuck();
                return false;
            }

            return !GrenadeLauncherHookContext.Active;
        }
    }

    internal static partial class PluginSettings
    {
        internal const string ArcOriginal = "Original TF2-scaled arc";
        internal const string ArcCustom = "Custom gravity";

        private static StringListField arcPreset;
        private static FloatSliderField damage;
        private static FloatSliderField airshotDamage;
        private static FloatSliderField airshotExplosionSize;
        private static FloatSliderField speed;
        private static FloatSliderField upwardVelocity;
        private static FloatSliderField customGravity;
        private static FloatSliderField fireInterval;
        private static FloatSliderField explosionSize;
        private static FloatSliderField knockback;
        private static FloatSliderField lifetime;
        private static FloatSliderField stuckLifetime;
        private static FloatSliderField surfaceFuse;
        private static FloatSliderField hookFuseGrace;
        private static FloatSliderField surfaceDamage;
        private static FloatSliderField bounceRetention;
        private static FloatSliderField parryDamage;
        private static FloatSliderField parryExplosionSize;
        private static FloatSliderField parrySpeed;
        private static FloatSliderField directSelfDamage;
        private static FloatSliderField surfaceSelfDamage;
        private static FloatSliderField parrySelfDamage;
        private static BoolField eyeHeightScaling;
        private static FloatSliderField fallbackEyeHeight;
        private static Texture2D configuratorIconTexture;
        private static Sprite configuratorIconSprite;
        private static readonly Dictionary<EnemyType, FloatSliderField> enemyDamageMultipliers =
            new Dictionary<EnemyType, FloatSliderField>();
        private static readonly Dictionary<EnemyType, float> releaseEnemyDamageDefaults =
            new Dictionary<EnemyType, float>
            {
                { EnemyType.Cerberus, 120f },
                { EnemyType.Gutterman, 110f },
                { EnemyType.HideousMass, 90f },
                { EnemyType.MaliciousFace, 100f },
                { EnemyType.Mannequin, 200f },
                { EnemyType.Providence, 175f },
                { EnemyType.Schism, 125f }
            };

        internal static float Damage => damage?.value ?? 4f;
        internal static float AirshotDamage => airshotDamage?.value ?? 6f;
        internal static float AirshotExplosionSize => airshotExplosionSize?.value ?? 1f;
        internal static float SpeedMultiplier => speed?.value ?? 1f;
        internal static float UpwardVelocityMultiplier => upwardVelocity?.value ?? 1f;
        internal static float FireInterval => fireInterval?.value ?? 0.6f;
        internal static float ExplosionSizeMultiplier => explosionSize?.value ?? 0.9f;
        internal static float KnockbackMultiplier => knockback?.value ?? 1f;
        internal static float Lifetime => lifetime?.value ?? 15f;
        internal static float StuckLifetime => stuckLifetime?.value ?? 60f;
        internal static float SurfaceFuse => surfaceFuse?.value ?? 1f;
        internal static float HookFuseGrace => hookFuseGrace?.value ?? 0.1f;
        internal static float SurfaceDamage => surfaceDamage?.value ?? 2.5f;
        internal static float BounceRetention => bounceRetention?.value ?? 0.1f;
        internal static float ParryDamage => parryDamage?.value ?? 4.5f;
        internal static float ParryExplosionSize => parryExplosionSize?.value ?? 1f;
        internal static float ParrySpeedMultiplier => parrySpeed?.value ?? 3f;
        internal static float DirectSelfDamage => directSelfDamage?.value ?? 35f;
        internal static float SurfaceSelfDamage => surfaceSelfDamage?.value ?? 25f;
        internal static float ParrySelfDamage => parrySelfDamage?.value ?? 30f;
        internal static bool EyeHeightScaling => eyeHeightScaling?.value ?? true;
        internal static float FallbackEyeHeight => fallbackEyeHeight?.value ?? 2.9f;

        internal static float GetEnemyDamageMultiplier(EnemyType enemyType)
        {
            return enemyDamageMultipliers.TryGetValue(enemyType, out FloatSliderField field)
                ? Mathf.Max(0f, field.value) / 100f
                : 1f;
        }

        internal static float GravityMultiplier
        {
            get
            {
                string preset = arcPreset?.value ?? ArcOriginal;
                if (preset == ArcOriginal)
                    return 1f;
                return customGravity?.value ?? 1f;
            }
        }

        internal static void Initialize()
        {
            PluginConfigurator configurator = PluginConfigurator.Create(Plugin.Name, Plugin.Guid);
            ApplyConfiguratorIcon(configurator);
            new PluginConfig.API.Decorators.ConfigHeader(
                configurator.rootPanel,
                "Get comfortable with the weapon before changing its settings.");
            AddPageResetButton(configurator.rootPanel, "Reset all pages to default", "resetAllPages", true, configurator);
            AddPageResetButton(configurator.rootPanel, "Reset this page to default", "resetMainPage", false, configurator);

            arcPreset = new StringListField(
                configurator.rootPanel,
                "Arc preset",
                "arcPreset",
                new[] { ArcOriginal, ArcCustom },
                ArcOriginal);

            damage = Slider(configurator, "Damage", "damage", 0f, 10f, 4f, 2);
            airshotDamage = Slider(configurator, "Airshot damage", "airshotDamage", 0f, 20f, 6f, 2);
            airshotExplosionSize = Slider(configurator, "Airshot explosion size", "airshotExplosionSize", 0.1f, 5f, 1f, 2);
            MigrateDamageDefault();
            speed = Slider(configurator, "Projectile speed multiplier", "speedMultiplier", 0.1f, 3f, 1f, 2);
            upwardVelocity = Slider(configurator, "Upward velocity multiplier", "upwardVelocityMultiplier", 0f, 3f, 1f, 2);
            customGravity = Slider(configurator, "Custom gravity multiplier", "customGravityMultiplier", 0.1f, 5f, 1f, 2);
            fireInterval = Slider(configurator, "Fire interval (seconds)", "fireInterval", 0.1f, 2f, 0.6f, 2);
            explosionSize = Slider(configurator, "Explosion size (rocket = 1)", "explosionSizeMultiplier", 0.25f, 3f, 0.9f, 2);
            knockback = Slider(configurator, "Explosion knockback (rocket = 1)", "knockbackMultiplier", 0f, 3f, 1f, 2);
            lifetime = Slider(configurator, "Silent projectile lifetime", "projectileLifetime", 1f, 60f, 15f, 1);
            stuckLifetime = Slider(configurator, "Stuck grenade lifetime (seconds)", "stuckLifetime", 1f, 300f, 60f, 1);
            surfaceFuse = Slider(configurator, "Surface fuse (seconds)", "surfaceFuse", 0.1f, 10f, 1f, 2);
            hookFuseGrace = Slider(configurator, "Fuse time added per Whiplash hook (seconds)", "hookFuseGrace", 0f, 2f, 0.1f, 2);
            surfaceDamage = Slider(configurator, "Surface detonation damage", "surfaceDamage", 0f, 10f, 2.5f, 2);
            bounceRetention = Slider(configurator, "Surface bounce speed retention", "bounceRetention", 0f, 1f, 0.1f, 2);
            parryDamage = Slider(configurator, "Parried grenade damage", "parryDamage", 0f, 20f, 4.5f, 2);
            parryExplosionSize = Slider(configurator, "Parried explosion size (rocket = 1)", "parryExplosionSize", 0.25f, 5f, 1f, 2);
            parrySpeed = Slider(configurator, "Parried projectile speed multiplier", "parrySpeedMultiplier", 0.1f, 5f, 3f, 2);
            directSelfDamage = Slider(configurator, "Direct explosion self damage (HP)", "directSelfDamage", 0f, 100f, 35f, 0);
            surfaceSelfDamage = Slider(configurator, "Timed explosion self damage (HP)", "surfaceSelfDamage", 0f, 100f, 25f, 0);
            parrySelfDamage = Slider(configurator, "Parried explosion self damage (HP)", "parrySelfDamage", 0f, 100f, 30f, 0);

            // Older builds exposed this as a second preset. Preserve existing configs by
            // migrating it to custom gravity at its old 2x value, while removing it from UI.
            if (arcPreset.value == "Current snappier arc")
            {
                arcPreset.value = ArcCustom;
                customGravity.value = 2f;
            }

            ConfigPanel calibration = new ConfigPanel(configurator.rootPanel, "Debug / trajectory calibration", "debugTrajectoryCalibration");
            calibration.headerText = "Only change these when diagnosing trajectory calibration.";
            AddPageResetButton(calibration, "Reset this page to default", "resetDebugTrajectoryCalibration");
            eyeHeightScaling = new BoolField(calibration, "Use eye-height calibration", "eyeHeightScaling", true);
            fallbackEyeHeight = Slider(calibration, "Fallback V1 eye height", "fallbackEyeHeight", 0.5f, 10f, 2.9f, 2);

            InitializeAlternateSettings(configurator);
            ConfigPanel enemyPanel = new ConfigPanel(
                configurator.rootPanel,
                "Enemy grenade damage",
                "enemyDamageMultipliers");
            enemyPanel.headerText = "Damage percentage is applied after ULTRAKILL's normal explosive weakness or resistance.";
            AddPageResetButton(enemyPanel, "Reset this page to default", "resetEnemyDamageMultipliers");
            enemyDamageMultipliers.Clear();
            foreach (EnemyType enemyType in Enum.GetValues(typeof(EnemyType)).Cast<EnemyType>().OrderBy(value => value.ToString()))
            {
                enemyDamageMultipliers[enemyType] = Slider(
                    enemyPanel,
                    FriendlyEnemyName(enemyType) + " damage (%)",
                    "enemyDamage_" + enemyType,
                    0f,
                    500f,
                    releaseEnemyDamageDefaults.TryGetValue(enemyType, out float defaultPercent) ? defaultPercent : 100f,
                    0);
            }
        }

        private static void ApplyConfiguratorIcon(PluginConfigurator configurator)
        {
            using (Stream stream = typeof(Plugin).Assembly.GetManifestResourceStream("GrenadeLauncherMod.icon.png"))
            {
                if (stream == null)
                {
                    Plugin.LogSource?.LogWarning("Embedded Plugin Configurator icon was not found.");
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
                    Plugin.LogSource?.LogWarning("Embedded Plugin Configurator icon could not be read completely.");
                    return;
                }

                configuratorIconTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                configuratorIconTexture.name = "Grenade Launcher Icon";
                if (!ImageConversion.LoadImage(configuratorIconTexture, data, false))
                {
                    UnityEngine.Object.Destroy(configuratorIconTexture);
                    configuratorIconTexture = null;
                    Plugin.LogSource?.LogWarning("Embedded Plugin Configurator icon was not a valid image.");
                    return;
                }

                configuratorIconSprite = Sprite.Create(
                    configuratorIconTexture,
                    new Rect(0f, 0f, configuratorIconTexture.width, configuratorIconTexture.height),
                    new Vector2(0.5f, 0.5f));
                configuratorIconSprite.name = "Grenade Launcher Icon";
                configurator.icon = configuratorIconSprite;
            }
        }

        private static void AddPageResetButton(
            ConfigPanel panel,
            string text,
            string guid,
            bool allPages = false,
            PluginConfigurator configurator = null)
        {
            ButtonField button = new ButtonField(panel, text, guid);
            button.onClick += () =>
            {
                PluginConfigurator root = configurator ?? panel.rootConfig;
                if (allPages)
                    ResetAllPages(root);
                else
                    ResetPage(panel, root);
            };
        }

        private static void ResetPage(ConfigPanel panel, PluginConfigurator configurator)
        {
            foreach (ConfigField field in GetDirectFields(panel, configurator))
            {
                if (field == null || field is ConfigPanel || field is ButtonField)
                    continue;
                ReloadFieldDefault(field);
            }
            FlushConfigurator(configurator);
        }

        // GetAllFields is reliable for child panels, but the configurator's root panel does
        // not enumerate its direct fields in every PluginConfigurator build.  Reading the
        // configurator's field registry and filtering by owner keeps "Reset this page" from
        // silently doing nothing on the main page.
        private static IEnumerable<ConfigField> GetDirectFields(ConfigPanel panel, PluginConfigurator configurator)
        {
            FieldInfo fieldsField = AccessTools.Field(typeof(PluginConfigurator), "fields");
            System.Collections.IDictionary fields = fieldsField?.GetValue(configurator) as System.Collections.IDictionary;
            PropertyInfo parentPanel = AccessTools.Property(typeof(ConfigField), "parentPanel");
            if (fields != null && parentPanel != null)
            {
                foreach (object value in fields.Values)
                {
                    ConfigField field = value as ConfigField;
                    if (field != null && ReferenceEquals(parentPanel.GetValue(field, null), panel))
                        yield return field;
                }
                yield break;
            }

            foreach (ConfigField field in panel.GetAllFields())
                yield return field;
        }

        private static void ResetAllPages(PluginConfigurator configurator)
        {
            FieldInfo fieldsField = AccessTools.Field(typeof(PluginConfigurator), "fields");
            System.Collections.IDictionary fields = fieldsField?.GetValue(configurator) as System.Collections.IDictionary;
            if (fields != null)
            {
                foreach (object value in fields.Values)
                {
                    ConfigField field = value as ConfigField;
                    if (field == null || field is ConfigPanel || field is ButtonField)
                        continue;
                    ReloadFieldDefault(field);
                }
            }
            else
            {
                ResetPage(configurator.rootPanel, configurator);
            }
            FlushConfigurator(configurator);
        }

        private static void ReloadFieldDefault(ConfigField field)
        {
            AccessTools.Method(field.GetType(), "ReloadDefault")?.Invoke(field, null);
        }

        private static void FlushConfigurator(PluginConfigurator configurator)
        {
            if (configurator == null)
                return;
            AccessTools.Method(typeof(PluginConfigurator), "FlushAll")?.Invoke(configurator, null);
            Plugin.Instance?.Config.Save();
        }

        private static FloatSliderField Slider(
            PluginConfigurator configurator,
            string displayName,
            string guid,
            float minimum,
            float maximum,
            float defaultValue,
            int decimals)
        {
            return new FloatSliderField(
                configurator.rootPanel,
                displayName,
                guid,
                new Tuple<float, float>(minimum, maximum),
                defaultValue,
                decimals);
        }

        private static FloatSliderField Slider(
            ConfigPanel panel,
            string displayName,
            string guid,
            float minimum,
            float maximum,
            float defaultValue,
            int decimals)
        {
            return new FloatSliderField(
                panel,
                displayName,
                guid,
                new Tuple<float, float>(minimum, maximum),
                defaultValue,
                decimals);
        }

        private static string FriendlyEnemyName(EnemyType enemyType)
        {
            string name = enemyType.ToString();
            System.Text.StringBuilder result = new System.Text.StringBuilder(name.Length + 8);
            for (int index = 0; index < name.Length; index++)
            {
                char current = name[index];
                if (index > 0 && char.IsUpper(current) && !char.IsUpper(name[index - 1]))
                    result.Append(' ');
                result.Append(current);
            }
            return result.ToString();
        }

        private static void MigrateDamageDefault()
        {
            ConfigEntry<int> defaultsVersion = Plugin.Instance.Config.Bind(
                "Migration",
                "DefaultsVersion",
                0,
                "Internal version used to apply changed defaults once without overwriting later customization.");

            if (defaultsVersion.Value >= 1)
                return;

            if (Mathf.Approximately(damage.value, 3f))
                damage.value = 4f;

            defaultsVersion.Value = 1;
            Plugin.Instance.Config.Save();
        }

    }

    [HarmonyPatch(typeof(Grenade), "Awake")]
    internal static class GrenadeAwakePatch
    {
        private static void Postfix(Grenade __instance)
        {
            if (!GrenadeSpawnContext.Active)
                return;

            if (GrenadeSpawnContext.Profile == GrenadeProjectileProfile.BlueDelivery)
            {
                if (__instance.GetComponent<BlueHookDeliveryProjectile>() == null)
                {
                    BlueHookDeliveryProjectile delivery = __instance.gameObject.AddComponent<BlueHookDeliveryProjectile>();
                    delivery.Grenade = __instance;
                    delivery.Target = BlueHookDeliveryContext.Target;
                }
                return;
            }

            if (__instance.GetComponent<GrenadeLauncherProjectile>() == null)
            {
                GrenadeLauncherProjectile projectile = __instance.gameObject.AddComponent<GrenadeLauncherProjectile>();
                projectile.Grenade = __instance;
                projectile.Profile = GrenadeSpawnContext.Profile;
            }
        }
    }

    [HarmonyPatch(typeof(Grenade), "FixedUpdate")]
    internal static class GrenadeFixedUpdatePatch
    {
        private static bool Prefix(Grenade __instance)
        {
            return __instance.GetComponent<GrenadeLauncherProjectile>() == null &&
                   __instance.GetComponent<BlueHookDeliveryProjectile>() == null;
        }
    }

    [HarmonyPatch(typeof(CustomGravity), "FixedUpdate")]
    internal static class GrenadeCustomGravityPatch
    {
        private static bool Prefix(CustomGravity __instance)
        {
            return __instance.GetComponent<GrenadeLauncherProjectile>() == null &&
                   __instance.GetComponent<BlueHookDeliveryProjectile>() == null;
        }
    }

    [HarmonyPatch(typeof(Grenade), "Start")]
    internal static class GrenadeStartPatch
    {
        private static void Postfix(Grenade __instance)
        {
            GrenadeLauncherProjectile marker = __instance.GetComponent<GrenadeLauncherProjectile>();
            marker?.Launch();
            __instance.GetComponent<BlueHookDeliveryProjectile>()?.Launch();
        }
    }

    [HarmonyPatch(typeof(Grenade), "OnCollisionEnter")]
    internal static class GrenadeOnCollisionEnterPatch
    {
        private static bool Prefix(Grenade __instance, Collision collision)
        {
            if (__instance.GetComponent<BlueHookDeliveryProjectile>() != null)
                return false;
            if (collision != null && collision.collider != null &&
                collision.collider.GetComponentInParent<PinkHookPointMarker>() != null)
                return false;

            GrenadeLauncherProjectile marker = __instance.GetComponent<GrenadeLauncherProjectile>();
            if (marker == null)
                return true;

            ContactPoint contact = collision.contactCount > 0 ? collision.GetContact(0) : default;
            Vector3 normal = collision.contactCount > 0 ? contact.normal : Vector3.zero;
            Vector3 point = collision.contactCount > 0 ? contact.point : __instance.transform.position;
            marker.HandleCollision(collision.collider, -collision.relativeVelocity, normal, point);
            return false;
        }
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.Collision), new[] { typeof(Collider), typeof(Vector3) })]
    internal static class GrenadeCollisionPatch
    {
        private static bool Prefix(Grenade __instance, Collider other, Vector3 velocity)
        {
            if (__instance.GetComponent<BlueHookDeliveryProjectile>() != null)
                return false;
            if (other != null && other.GetComponentInParent<PinkHookPointMarker>() != null)
                return false;

            GrenadeLauncherProjectile marker = __instance.GetComponent<GrenadeLauncherProjectile>();
            if (marker == null)
                return true;

            marker.HandleCollision(other, velocity, Vector3.zero, __instance.transform.position);
            return false;
        }
    }

    [HarmonyPatch(typeof(Grenade), "OnTriggerEnter")]
    internal static class PinkHookGrenadeTriggerIgnorePatch
    {
        private static bool Prefix(Collider other) =>
            other == null || other.GetComponentInParent<PinkHookPointMarker>() == null;
    }

    [HarmonyPatch(typeof(Explosion), "Collide")]
    internal static class GrenadeLauncherExplosionPatch
    {
        private struct State
        {
            internal bool ChangedMultiplier;
            internal float EnemyDamageMultiplier;
            internal bool ChangedDamage;
            internal int Damage;
            internal GrenadeLauncherExplosionMarker Marker;
            internal EnemyIdentifier Enemy;
        }

        private static bool Prefix(Explosion __instance, Collider other, out State __state)
        {
            __state = new State();
            GrenadeLauncherExplosionMarker marker = __instance.GetComponentInParent<GrenadeLauncherExplosionMarker>();
            GrenadeLauncherProjectile stuckProjectile = other != null
                ? other.GetComponentInParent<GrenadeLauncherProjectile>()
                : null;
            if (stuckProjectile != null && stuckProjectile.IsStuck)
            {
                if (marker != null && marker.Mode == GrenadeExplosionMode.Surface && !PluginSettings.TimedFuseDetonatesStuck)
                    return false;
                if (IsPlayerExplosion(__instance))
                    stuckProjectile.DetonateStuck();
                return false;
            }

            if (marker == null)
                return true;

            if (marker.BlueHookReplacement)
            {
                // This is deliberately not a grenade explosion.  It must not inherit the
                // grenade's per-enemy damage settings, rocket behavior, or burn behavior.
                if (other != null && other.CompareTag("Player"))
                    return false;
                EnemyIdentifier replacementEnemy = GrenadeLauncherProjectile.TryGetLivingEnemy(other);
                return replacementEnemy == null;
            }

            EnemyIdentifier enemy = GrenadeLauncherProjectile.TryGetLivingEnemy(other);
            if (enemy == null)
                return true;

            __state.Marker = marker;
            __state.Enemy = enemy;

            if (PluginSettings.EnemyCarrierTakesDirectDamage && marker.StuckHostEnemyId != 0 &&
                enemy.GetInstanceID() == marker.StuckHostEnemyId)
            {
                __state.ChangedDamage = true;
                __state.Damage = __instance.damage;
                __instance.damage = Mathf.RoundToInt(Mathf.Max(0f, marker.StuckCarrierDamage) * 10f);
            }

            if (marker.Parried && enemy.enemyType == EnemyType.MaliciousFace)
            {
                marker.DamageBlockedEnemy(enemy, other);
                return false;
            }

            __state.ChangedMultiplier = true;
            __state.EnemyDamageMultiplier = __instance.enemyDamageMultiplier;
            __instance.enemyDamageMultiplier *= PluginSettings.GetEnemyDamageMultiplier(enemy.enemyType);
            return true;
        }

        private static bool IsPlayerExplosion(Explosion explosion)
        {
            if (explosion == null || explosion.enemy)
                return false;

            // Revolver/rail beams execute synchronously inside this context, even when
            // their transient beam object is not parented under the player's model.
            if (StuckDetonationContext.Current != null && StuckDetonationContext.CanCurrentBeamDetonate)
                return true;
            if (explosion.sourceWeapon == null)
                return false;

            GameObject source = explosion.sourceWeapon;
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            return (movement != null &&
                    (source == movement.gameObject || source.transform.IsChildOf(movement.transform))) ||
                   source.GetComponentInParent<NewMovement>() != null;
        }

        private static Exception Finalizer(Explosion __instance, State __state, Exception __exception)
        {
            if (__state.ChangedMultiplier)
                __instance.enemyDamageMultiplier = __state.EnemyDamageMultiplier;
            if (__state.ChangedDamage)
                __instance.damage = __state.Damage;
            if (__exception == null && __state.Marker != null && __state.Enemy != null)
            {
                __state.Marker.TryAwardLongRangeStyle(__state.Enemy);
                __state.Marker.TryAwardWalkingBomb(__state.Enemy);
                __state.Marker.QueueDirectHitTelemetry(__state.Enemy);
            }
            return __exception;
        }
    }

    // The Providence hook-point visual contains a PhysicalShockwave for its launch
    // effect.  That component routes enemy damage through the normal Enemy hurt path,
    // which also spawns blood and plays hurt/splatter audio.  Keep the native damage and
    // knockback, but mark only our replacement shockwaves so those cosmetic responses can
    // be suppressed without changing ordinary explosions.
    internal sealed class GrenadeLauncherBlueShockwaveMarker : MonoBehaviour
    {
    }

    internal static class GrenadeLauncherBlueShockwaveContext
    {
        private static int depth;

        internal static bool Active => depth > 0;

        internal static void Enter() => depth++;

        internal static void Exit()
        {
            if (depth > 0)
                depth--;
        }
    }

    [HarmonyPatch(typeof(PhysicalShockwave), "CheckCollision")]
    internal static class GrenadeLauncherPhysicalShockwavePatch
    {
        private static bool Prefix(PhysicalShockwave __instance, out bool __state)
        {
            __state = __instance != null &&
                      __instance.GetComponentInParent<GrenadeLauncherBlueShockwaveMarker>() != null;
            if (__state)
                GrenadeLauncherBlueShockwaveContext.Enter();
            return true;
        }

        private static Exception Finalizer(bool __state, Exception __exception)
        {
            if (__state)
                GrenadeLauncherBlueShockwaveContext.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(Enemy), "HandleBloodSelection")]
    internal static class GrenadeLauncherBloodSelectionPatch
    {
        private static bool Prefix(ref GameObject __result)
        {
            if (!GrenadeLauncherBlueShockwaveContext.Active)
                return true;
            __result = null;
            return false;
        }
    }

    [HarmonyPatch(typeof(Enemy), "ProcessBloodEffects")]
    internal static class GrenadeLauncherBloodEffectsPatch
    {
        private static bool Prefix() => !GrenadeLauncherBlueShockwaveContext.Active;
    }

    [HarmonyPatch(typeof(Enemy), "PlayHurtSound")]
    internal static class GrenadeLauncherHurtSoundPatch
    {
        private static bool Prefix() => !GrenadeLauncherBlueShockwaveContext.Active;
    }

    [HarmonyPatch(typeof(Enemy), "BloodExplosion")]
    internal static class GrenadeLauncherBloodExplosionPatch
    {
        private static bool Prefix() => !GrenadeLauncherBlueShockwaveContext.Active;
    }

    [HarmonyPatch(typeof(BloodsplatterManager), "PlayBloodSound")]
    internal static class GrenadeLauncherBloodSoundPatch
    {
        private static bool Prefix() => !GrenadeLauncherBlueShockwaveContext.Active;
    }

    internal enum GrenadeExplosionMode
    {
        Direct,
        Surface,
        Parried,
        GreenDirect,
        GreenSurface,
        Stuck
    }

    internal enum GrenadeLongRangeStyle
    {
        None,
        PipeDream,
        MoonShot
    }

    internal static class GrenadeStyleAwards
    {
        private const string PipeDreamId = "grenadelauncher.pipe_dream";
        private const string MoonShotId = "grenadelauncher.moon_shot";
        private const string OutSnipedId = "grenadelauncher.out_sniped";
        private const string WalkingBombId = "grenadelauncher.walking_bomb";
        private static StyleHUD registeredHud;
        private static readonly HashSet<int> walkingBombAwardedChains = new HashSet<int>();

        internal static void Award(GrenadeLongRangeStyle style, EnemyIdentifier enemy, GameObject sourceWeapon)
        {
            StyleHUD hud = EnsureRegistered(enemy);
            if (hud == null)
                return;

            bool outSniped = style == GrenadeLongRangeStyle.MoonShot &&
                              enemy.enemyType == EnemyType.Turret && enemy.dead;
            string id = outSniped ? OutSnipedId :
                        style == GrenadeLongRangeStyle.MoonShot ? MoonShotId : PipeDreamId;
            int points = outSniped
                ? PluginSettings.OutSnipedStylePoints
                : style == GrenadeLongRangeStyle.MoonShot
                    ? PluginSettings.MoonShotStylePoints
                    : PluginSettings.PipeDreamStylePoints;
            hud.AddPoints(points, id, sourceWeapon, enemy, -1, "", "");
        }

        internal static void AwardWalkingBomb(int chainId, EnemyIdentifier enemy, GameObject sourceWeapon)
        {
            if (chainId <= 0 || !walkingBombAwardedChains.Add(chainId))
                return;
            StyleHUD hud = EnsureRegistered(enemy);
            if (hud != null)
                hud.AddPoints(PluginSettings.WalkingBombStylePoints, WalkingBombId, sourceWeapon, enemy, -1, "", "");
        }

        internal static void ResetWalkingBombChains() => walkingBombAwardedChains.Clear();

        private static StyleHUD EnsureRegistered(EnemyIdentifier enemy)
        {
            StyleHUD hud = MonoSingleton<StyleHUD>.Instance;
            if (hud == null || enemy == null)
                return null;
            if (registeredHud != hud)
            {
                hud.RegisterStyleItem(PipeDreamId, "<color=#00ffffff>PIPE DREAM</color>");
                hud.RegisterStyleItem(MoonShotId, "<color=#FFD700>MOON SHOT</color>");
                hud.RegisterStyleItem(OutSnipedId, "<color=#FFD700>OUT-SNIPED</color>");
                hud.RegisterStyleItem(WalkingBombId, "<color=#00ffffff>WALKING BOMB</color>");
                registeredHud = hud;
            }
            return hud;
        }
    }

    internal sealed class GrenadeLauncherExplosionMarker : MonoBehaviour
    {
        private readonly HashSet<int> manuallyDamagedEnemies = new HashSet<int>();
        private readonly HashSet<int> styleAwardedEnemies = new HashSet<int>();
        private bool walkingBombAwarded;
        private bool telemetryQueued;
        private EnemyIdentifier telemetryEnemy;

        internal bool Parried;
        internal GrenadeExplosionMode Mode;
        internal float Damage;
        internal GameObject SourceWeapon;
        internal int DirectEnemyId;
        internal GrenadeLongRangeStyle LongRangeStyle;
        internal int StuckHostEnemyId;
        internal float StuckCarrierDamage;
        internal int StuckChainGroupId;
        internal GrenadeProjectileProfile DirectProfile;
        internal float DirectPlanarDistance;
        internal float DirectStraightDistance;
        internal float TheoreticalSameHeightRange;
        internal float LaunchElevationDegrees;
        internal float ConfiguredStyleThreshold;
        internal bool Airshot;
        internal bool BlueHookReplacement;

        internal void TryAwardLongRangeStyle(EnemyIdentifier enemy)
        {
            if (enemy == null || LongRangeStyle == GrenadeLongRangeStyle.None ||
                enemy.GetInstanceID() != DirectEnemyId || !styleAwardedEnemies.Add(DirectEnemyId))
                return;
            GrenadeStyleAwards.Award(LongRangeStyle, enemy, SourceWeapon);
        }

        internal void TryAwardWalkingBomb(EnemyIdentifier enemy)
        {
            if (walkingBombAwarded || enemy == null || !enemy.dead || StuckHostEnemyId == 0)
                return;
            int enemyId = enemy.GetInstanceID();
            if (enemyId == StuckHostEnemyId)
                return;
            walkingBombAwarded = true;
            GrenadeStyleAwards.AwardWalkingBomb(StuckChainGroupId, enemy, SourceWeapon);
        }

        internal void QueueDirectHitTelemetry(EnemyIdentifier enemy)
        {
            if (!PluginSettings.RangeTelemetryEnabled || telemetryQueued || enemy == null || DirectEnemyId == 0 ||
                enemy.GetInstanceID() != DirectEnemyId)
                return;
            telemetryQueued = true;
            telemetryEnemy = enemy;
            StartCoroutine(LogDirectHitTelemetryAtEndOfFrame());
        }

        private IEnumerator LogDirectHitTelemetryAtEndOfFrame()
        {
            yield return new WaitForEndOfFrame();
            string enemyType = telemetryEnemy != null ? telemetryEnemy.enemyType.ToString() : "Destroyed";
            bool killed = telemetryEnemy == null || telemetryEnemy.dead;
            float ratio = TheoreticalSameHeightRange > 0.001f
                ? DirectPlanarDistance / TheoreticalSameHeightRange
                : 0f;
            string profile = DirectProfile == GrenadeProjectileProfile.GreenContact ? "GREEN" : "PRIMARY";
            Plugin.LogSource?.LogInfo(
                $"[RangeTelemetry] {profile} direct hit | " +
                $"planar={DirectPlanarDistance:0.###}u | straight={DirectStraightDistance:0.###}u | " +
                $"launchElevation={LaunchElevationDegrees:0.###}deg | " +
                $"calculatedSameHeightMax={TheoreticalSameHeightRange:0.###}u | ratio={ratio:0.###} | " +
                $"styleThreshold={ConfiguredStyleThreshold:0.###}u | " +
                $"enemy={enemyType} | killed={killed}");
        }

        internal void DamageBlockedEnemy(EnemyIdentifier enemy, Collider hitCollider)
        {
            if (enemy == null || enemy.dead || !manuallyDamagedEnemies.Add(enemy.GetInstanceID()))
                return;

            enemy.hitter = "grenadelauncherparry";
            float damage = Damage * PluginSettings.GetEnemyDamageMultiplier(enemy.enemyType);
            Vector3 hitPoint = hitCollider != null
                ? hitCollider.bounds.ClosestPoint(transform.position)
                : transform.position;
            GameObject target = hitCollider != null ? hitCollider.gameObject : enemy.gameObject;
            enemy.DeliverDamage(target, Vector3.zero, hitPoint, damage, false, 0f, SourceWeapon, false, true);
        }

    }

    [DefaultExecutionOrder(10000)]
    internal sealed class GrenadeLauncherProjectile : MonoBehaviour
    {
        private const float Tf2DemomanEyeHeightHu = 68f;
        private const float Tf2ForwardSpeedHu = 1200f;
        private const float Tf2UpwardSpeedHu = 200f;
        private const float Tf2GravityHu = 800f;
        private static int nextChainGroupId;

        internal Grenade Grenade;
        internal GrenadeProjectileProfile Profile;
        internal bool CanBeWhiplashed => Profile == GrenadeProjectileProfile.Primary && bounced && !stuck && !finished;
        internal bool IsStuck => stuck && !finished;
        internal int ChainGroupId => chainGroupId;

        private Rigidbody body;
        private readonly List<CustomGravity> customGravityComponents = new List<CustomGravity>(1);
        private Collider[] ownColliders = Array.Empty<Collider>();
        private ParryReceiver parryReceiver;
        private bool launched;
        private bool finished;
        private bool bounced;
        private bool parried;
        private bool stuck;
        private int stuckHostEnemyId;
        private int chainGroupId;
        private bool wasHooked;
        private GelStainMarker stuckGelStain;
        private GelCoverage stuckGelCoverage;
        private float launchForwardSpeed;
        private float launchElevationDegrees;
        private float sameHeightMaximumRange;
        private Vector3 launchPosition;
        private Vector3 capturedGravity;
        private Collider lastBounceSurface;
        private Vector3 lastBounceNormal;
        private Vector3 lastBouncePoint;
        private bool hasLastBouncePoint;
        private float bornAt;
        private float stuckBornAt;
        private float fuseRemaining = -1f;
        private float chainDetonationAt = -1f;
        private Vector3 simulatedVelocity;
        private Vector3 lastVelocity;
        private Quaternion lastFixedRotation;
        private static bool loggedTrajectory;
        private static bool loggedExplosion;
        private static bool loggedPortalRotation;
        private static bool loggedRejectedExternalChange;

        internal void Launch()
        {
            if (launched || Grenade == null)
                return;

            launched = true;
            bornAt = Time.time;
            body = Grenade.rb != null ? Grenade.rb : GetComponent<Rigidbody>();
            if (body == null)
            {
                Plugin.LogSource.LogWarning("Grenade projectile had no Rigidbody; removing it.");
                Destroy(gameObject);
                return;
            }
            ownColliders = GetComponentsInChildren<Collider>(true);

            float eyeHeight = TrajectoryCalibration.GetEyeHeight();
            float unitsPerHu = eyeHeight / Tf2DemomanEyeHeightHu;
            float speedMultiplier = Profile == GrenadeProjectileProfile.GreenContact
                ? PluginSettings.SpeedMultiplier * PluginSettings.GreenSpeedMultiplier
                : PluginSettings.SpeedMultiplier;
            float upwardMultiplier = Profile == GrenadeProjectileProfile.GreenContact
                ? PluginSettings.UpwardVelocityMultiplier * PluginSettings.GreenUpwardVelocityMultiplier
                : PluginSettings.UpwardVelocityMultiplier;
            float gravityMultiplier = Profile == GrenadeProjectileProfile.GreenContact
                ? PluginSettings.GreenGravityMultiplier * PluginSettings.GravityMultiplier
                : PluginSettings.GravityMultiplier;
            launchForwardSpeed = Tf2ForwardSpeedHu * unitsPerHu * Mathf.Max(0.05f, speedMultiplier);
            float upwardSpeed = Tf2UpwardSpeedHu * unitsPerHu * Mathf.Max(0f, upwardMultiplier);
            float gravityMagnitude = Tf2GravityHu * unitsPerHu * Mathf.Max(0.05f, gravityMultiplier);
            launchPosition = transform.position;

            Vector3 playerGravity = Physics.gravity;
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            if (movement != null && movement.rb != null)
                playerGravity = PhysicsExtensions.GetGravityVector(movement.rb);

            float standardGravityMagnitude = Mathf.Max(0.01f, Physics.gravity.magnitude);
            float relativeGravityMagnitude = playerGravity.magnitude / standardGravityMagnitude;
            Vector3 gravityDirection = playerGravity.sqrMagnitude > 0.0001f ? playerGravity.normalized : Vector3.down;
            capturedGravity = gravityDirection * gravityMagnitude * relativeGravityMagnitude;

            LockNativeGravity();
            // TF2's FirePipeBomb uses view-relative forward and up vectors.
            // Using world-up here only matches at zero pitch and gives steep shots
            // too much vertical velocity (and therefore too much hang time).
            body.velocity = transform.forward * launchForwardSpeed + transform.up * upwardSpeed;
            float capturedGravityMagnitude = capturedGravity.magnitude;
            float launchVerticalSpeed = Vector3.Dot(body.velocity, -gravityDirection);
            float launchPlanarSpeed = Vector3.ProjectOnPlane(body.velocity, gravityDirection).magnitude;
            launchElevationDegrees = Mathf.Atan2(launchVerticalSpeed, Mathf.Max(0.0001f, launchPlanarSpeed)) * Mathf.Rad2Deg;
            sameHeightMaximumRange = capturedGravityMagnitude > 0.001f && launchVerticalSpeed > 0f
                ? launchPlanarSpeed * (2f * launchVerticalSpeed / capturedGravityMagnitude)
                : 0f;
            simulatedVelocity = body.velocity;
            lastVelocity = simulatedVelocity;
            lastFixedRotation = transform.rotation;

            if (Profile == GrenadeProjectileProfile.Primary)
            {
                parryReceiver = GetComponent<ParryReceiver>();
                if (parryReceiver == null)
                    parryReceiver = gameObject.AddComponent<ParryReceiver>();
                if (parryReceiver.onParry == null)
                    parryReceiver.onParry = new UnityEngine.Events.UnityEvent();
                parryReceiver.parryHeal = false;
                parryReceiver.disappearOnParry = false;
                parryReceiver.onParry.AddListener(Parry);
                parryReceiver.enabled = false;
            }
            else
            {
                parryReceiver = GetComponent<ParryReceiver>();
                if (parryReceiver != null)
                    parryReceiver.enabled = false;
            }

            if (!loggedTrajectory)
            {
                loggedTrajectory = true;
                Plugin.LogSource.LogInfo(
                    $"TF2 trajectory converted from V1 eye height {eyeHeight:0.###}: " +
                    $"forward {launchForwardSpeed:0.###} u/s, up {upwardSpeed:0.###} u/s, " +
                    $"captured gravity {capturedGravity} u/s².");
            }
        }

        private void OnDestroy()
        {
            if (parryReceiver != null && parryReceiver.onParry != null)
                parryReceiver.onParry.RemoveListener(Parry);
            GelSystem.UnregisterStuck(this);
        }

        private void FixedUpdate()
        {
            if (!launched || finished || body == null)
                return;

            if (stuck)
            {
                if (Time.time - stuckBornAt >= Mathf.Max(1f, PluginSettings.StuckLifetime))
                {
                    FinishSilently();
                    return;
                }
                if (chainDetonationAt >= 0f && Time.time >= chainDetonationAt)
                    DetonateStuck();
                return;
            }

            float projectileLifetime = Profile == GrenadeProjectileProfile.GreenContact
                ? PluginSettings.GreenLifetime
                : PluginSettings.Lifetime;
            if (Time.time - bornAt >= Mathf.Max(1f, projectileLifetime))
            {
                FinishSilently();
                return;
            }

            if (Grenade != null && Grenade.hooked)
            {
                Grenade.CanCollideWithPlayer(false);
                if (!wasHooked && bounced && !parried && fuseRemaining >= 0f)
                    fuseRemaining += Mathf.Max(0f, PluginSettings.HookFuseGrace);
                wasHooked = true;
                lastFixedRotation = transform.rotation;
                return;
            }

            if (wasHooked)
            {
                wasHooked = false;
                if (!body.isKinematic)
                {
                    simulatedVelocity = body.velocity;
                    lastVelocity = simulatedVelocity;
                }
            }

            if (Grenade != null && Grenade.frozen)
                return;

            if (bounced && !parried && fuseRemaining >= 0f)
            {
                fuseRemaining -= Time.fixedDeltaTime;
                if (fuseRemaining <= 0f)
                {
                    Explode(GrenadeExplosionMode.Surface);
                    return;
                }
            }

            if (body.isKinematic)
            {
                lastFixedRotation = transform.rotation;
                return;
            }

            Vector3 actualVelocity = body.velocity;
            Vector3 expectedVelocity = lastVelocity;
            bool externalChange =
                (actualVelocity - expectedVelocity).sqrMagnitude > 4f &&
                Time.time - bornAt > Time.fixedDeltaTime * 2f;
            bool portalRotation = externalChange && IsMagnitudePreservingRotation(expectedVelocity, actualVelocity);
            if (portalRotation)
            {
                // FRAUD's gravity switchers are seamless spatial portals. Rotate every
                // launch-time vector through that same coordinate transform: this keeps
                // the original perceived gravity without sampling the destination zone.
                Quaternion portalDelta = transform.rotation * Quaternion.Inverse(lastFixedRotation);
                simulatedVelocity = actualVelocity;
                lastVelocity = actualVelocity;
                capturedGravity = portalDelta * capturedGravity;
            }

            if (portalRotation && !loggedPortalRotation)
            {
                loggedPortalRotation = true;
                Plugin.LogSource.LogInfo(
                    "Detected a seamless portal transform; rotating the captured launch trajectory and gravity. " +
                    $"expected {expectedVelocity}, actual {actualVelocity}, " +
                    $"rigidbody gravity={body.useGravity}, world gravity={Physics.gravity}.");
            }
            else if (externalChange && !portalRotation && !loggedRejectedExternalChange)
            {
                loggedRejectedExternalChange = true;
                Plugin.LogSource.LogWarning(
                    "Rejected an external grenade velocity change to preserve the custom trajectory: " +
                    $"expected {expectedVelocity}, actual {actualVelocity}, " +
                    $"rigidbody gravity={body.useGravity}, world gravity={Physics.gravity}.");
            }

            LockNativeGravity();
            if (!parried)
                simulatedVelocity += capturedGravity * Time.fixedDeltaTime;
            body.velocity = simulatedVelocity;
            lastVelocity = simulatedVelocity;
            if (simulatedVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 visualUp = capturedGravity.sqrMagnitude > 0.001f ? -capturedGravity.normalized : Vector3.up;
                transform.rotation = Quaternion.LookRotation(simulatedVelocity.normalized, visualUp);
            }
            lastFixedRotation = transform.rotation;
        }

        private static bool IsMagnitudePreservingRotation(Vector3 before, Vector3 after)
        {
            float beforeSpeed = before.magnitude;
            float afterSpeed = after.magnitude;
            if (beforeSpeed < 0.5f || afterSpeed < 0.5f)
                return false;

            float tolerance = Mathf.Max(0.35f, beforeSpeed * 0.015f);
            return Mathf.Abs(beforeSpeed - afterSpeed) <= tolerance &&
                   Vector3.Angle(before, after) >= 2f;
        }

        private void LockNativeGravity()
        {
            if (body == null)
                return;

            body.useGravity = false;
            PhysicsExtensions.SetGravityMode(body, false);
            // Gravity-changing levels and other mods can add or re-enable this component
            // after the projectile is launched, so do not rely only on the launch-time list.
            body.GetComponents(customGravityComponents);

            foreach (CustomGravity customGravity in customGravityComponents)
            {
                if (customGravity != null)
                    customGravity.useGravity = false;
            }
        }

        internal void HandleCollision(
            Collider other,
            Vector3 incomingVelocity,
            Vector3 collisionNormal,
            Vector3 collisionPoint)
        {
            if (finished || other == null)
                return;

            if (stuck)
                return;

            if (other.CompareTag("Player") || other.gameObject.layer == 14 || other.gameObject.layer == 20)
                return;

            Grenade otherGrenade = other.GetComponentInParent<Grenade>();
            if (otherGrenade != null)
            {
                foreach (Collider ownCollider in ownColliders)
                {
                    if (ownCollider != null && ownCollider != other)
                        Physics.IgnoreCollision(ownCollider, other, true);
                }
                if (body != null && !body.isKinematic)
                    body.velocity = simulatedVelocity;
                return;
            }

            if (Profile == GrenadeProjectileProfile.Primary)
            {
                Glass glass = other.GetComponentInParent<Glass>();
                if (glass != null)
                {
                    glass.Shatter();
                    foreach (Collider ownCollider in ownColliders)
                    {
                        if (ownCollider != null && ownCollider != other)
                            Physics.IgnoreCollision(ownCollider, other, true);
                    }
                    if (body != null && !body.isKinematic)
                        body.velocity = simulatedVelocity;
                    return;
                }
            }

            EnemyIdentifier enemy = TryGetLivingEnemy(other);
            if (other.isTrigger && enemy == null)
                return;

            if (GelSystem.TryGetStickTarget(other, enemy, collisionPoint, out GelStainMarker gelStain, out GelCoverage gelCoverage))
            {
                StickToGel(other, collisionNormal, collisionPoint, enemy, gelStain, gelCoverage);
                return;
            }

            if (Profile == GrenadeProjectileProfile.GreenContact)
            {
                Explode(enemy != null ? GrenadeExplosionMode.GreenDirect : GrenadeExplosionMode.GreenSurface, enemy);
                return;
            }

            if (parried)
            {
                Explode(GrenadeExplosionMode.Parried);
                return;
            }

            if (!bounced && enemy != null && enemy.enemyType != EnemyType.MaliciousFace)
            {
                Explode(GrenadeExplosionMode.Direct, enemy);
                return;
            }

            Bounce(other, incomingVelocity, collisionNormal, collisionPoint);
        }

        internal static EnemyIdentifier TryGetLivingEnemy(Collider other)
        {
            if (other == null)
                return null;

            EnemyIdentifierIdentifier link = null;
            if (other.attachedRigidbody != null)
                link = other.attachedRigidbody.GetComponent<EnemyIdentifierIdentifier>();
            if (link == null)
                link = other.GetComponentInParent<EnemyIdentifierIdentifier>();

            if (link != null && link.eid != null && !link.eid.dead)
                return link.eid;

            EnemyIdentifier enemy = other.GetComponentInParent<EnemyIdentifier>();
            return enemy != null && !enemy.dead ? enemy : null;
        }

        private void Bounce(
            Collider surface,
            Vector3 incomingVelocity,
            Vector3 collisionNormal,
            Vector3 collisionPoint)
        {
            if (body == null)
                return;

            if (!bounced)
            {
                bounced = true;
                fuseRemaining = Mathf.Max(0.1f, PluginSettings.SurfaceFuse);
                if (parryReceiver != null)
                    parryReceiver.enabled = true;
            }

            Vector3 incoming = lastVelocity.sqrMagnitude > 0.01f ? lastVelocity : incomingVelocity;
            Vector3 normal = collisionNormal;
            if (normal.sqrMagnitude < 0.01f)
            {
                Vector3 closestPoint = surface.bounds.ClosestPoint(transform.position);
                normal = transform.position - closestPoint;
            }
            if (normal.sqrMagnitude < 0.01f)
                normal = incoming.sqrMagnitude > 0.01f ? -incoming.normalized : -capturedGravity.normalized;

            normal.Normalize();
            // ContactPoint.normal can face either way depending on which collider Unity
            // reports as the first body. Keep it as the surface normal opposing impact.
            if (incoming.sqrMagnitude > 0.01f && Vector3.Dot(normal, incoming) > 0f)
                normal = -normal;

            lastBounceSurface = surface;
            lastBounceNormal = normal;
            lastBouncePoint = collisionPoint;
            hasLastBouncePoint = true;

            float retention = Mathf.Clamp01(PluginSettings.BounceRetention);
            body.constraints = RigidbodyConstraints.None;
            simulatedVelocity = Vector3.Reflect(incoming, normal) * retention;
            if (!body.isKinematic)
            {
                body.velocity = simulatedVelocity;
                body.angularVelocity *= retention;
            }
            lastVelocity = simulatedVelocity;
        }

        private void Parry()
        {
            if (finished || !bounced || body == null)
                return;

            parried = true;
            fuseRemaining = -1f;
            if (parryReceiver != null)
                parryReceiver.enabled = false;

            Grenade.CanCollideWithPlayer(false);

            CameraController cameraController = MonoSingleton<CameraController>.Instance;
            Vector3 direction = cameraController != null ? cameraController.transform.forward : transform.forward;
            body.constraints = RigidbodyConstraints.None;
            simulatedVelocity = direction.normalized * launchForwardSpeed * Mathf.Max(0.1f, PluginSettings.ParrySpeedMultiplier);
            if (body.isKinematic)
                body.isKinematic = false;
            body.velocity = simulatedVelocity;
            body.angularVelocity = Vector3.zero;
            lastVelocity = simulatedVelocity;
            transform.rotation = Quaternion.LookRotation(direction.normalized, transform.up);

            if (IsParriedIntoLastSurface(direction.normalized))
                Explode(GrenadeExplosionMode.Parried);
        }

        private bool IsParriedIntoLastSurface(Vector3 direction)
        {
            if (lastBounceSurface == null)
                return false;

            float clearance = GetFullProjectileClearance();
            bool directSurfaceHit = lastBounceSurface.Raycast(
                new Ray(transform.position, direction),
                out RaycastHit _,
                clearance);

            // If physics resolution left the grenade center microscopically inside the
            // floor, a ray beginning at the center cannot see that collider. Cast across
            // the center and accept only a hit at or ahead of the parry direction.
            Vector3 backedUpOrigin = transform.position - direction * clearance;
            bool crossedSurfaceHit = lastBounceSurface.Raycast(
                    new Ray(backedUpOrigin, direction),
                    out RaycastHit crossedSurface,
                    clearance * 2f);
            float crossedSignedDistance = crossedSurfaceHit
                ? Vector3.Dot(crossedSurface.point - transform.position, direction)
                : float.PositiveInfinity;
            bool crossedSurfaceAhead = crossedSurfaceHit && crossedSignedDistance >= -0.05f;

            bool globalRayHit = Physics.Raycast(
                    transform.position,
                    direction,
                    out RaycastHit nearbyHit,
                    clearance,
                    ~0,
                    QueryTriggerInteraction.Ignore);
            bool usableGlobalHit = globalRayHit && nearbyHit.collider != null &&
                !nearbyHit.collider.transform.IsChildOf(transform) &&
                !nearbyHit.collider.CompareTag("Player");

            // Bounds remain valid for non-convex mesh colliders (unlike Collider.ClosestPoint)
            // and cover the case where the projectile starts just inside the contact plane.
            Vector3 boundsPoint = lastBounceSurface.bounds.ClosestPoint(transform.position);
            float boundsDistance = Vector3.Distance(transform.position, boundsPoint);
            float storedPointDistance = hasLastBouncePoint
                ? Vector3.Distance(transform.position, lastBouncePoint)
                : float.PositiveInfinity;
            float normalDot = lastBounceNormal.sqrMagnitude >= 0.01f
                ? Vector3.Dot(direction, lastBounceNormal)
                : float.NaN;
            bool pointsIntoStoredNormal = lastBounceNormal.sqrMagnitude >= 0.01f && normalDot < -0.05f;
            Vector3 contactOffset = hasLastBouncePoint
                ? lastBouncePoint - transform.position
                : Vector3.zero;
            float contactPlaneDistance = hasLastBouncePoint
                ? Mathf.Abs(Vector3.Dot(contactOffset, lastBounceNormal))
                : float.PositiveInfinity;
            float contactLateralDistance = hasLastBouncePoint
                ? Vector3.ProjectOnPlane(contactOffset, lastBounceNormal).magnitude
                : float.PositiveInfinity;
            bool nearLastContactPlane = hasLastBouncePoint &&
                                        contactPlaneDistance <= clearance + 0.25f &&
                                        contactLateralDistance <= clearance;
            bool normalProximityHit = pointsIntoStoredNormal &&
                                      (boundsDistance <= clearance ||
                                       storedPointDistance <= clearance ||
                                       nearLastContactPlane);
            return directSurfaceHit || crossedSurfaceAhead || usableGlobalHit || normalProximityHit;
        }

        private float GetFullProjectileClearance()
        {
            float clearance = 0.75f;
            foreach (Collider ownCollider in ownColliders)
            {
                if (ownCollider == null || !ownCollider.enabled || ownCollider.isTrigger)
                    continue;

                Bounds bounds = ownCollider.bounds;
                float reach = Vector3.Distance(transform.position, bounds.center) + bounds.extents.magnitude;
                clearance = Mathf.Max(clearance, reach + 0.2f);
            }
            return clearance;
        }

        private void StickToGel(
            Collider surface,
            Vector3 collisionNormal,
            Vector3 collisionPoint,
            EnemyIdentifier hostEnemy,
            GelStainMarker gelStain,
            GelCoverage gelCoverage)
        {
            if (finished || stuck || body == null)
                return;

            stuck = true;
            stuckBornAt = Time.time;
            stuckHostEnemyId = hostEnemy != null ? hostEnemy.GetInstanceID() : 0;
            stuckGelStain = gelStain;
            stuckGelCoverage = gelCoverage;
            bounced = false;
            parried = false;
            fuseRemaining = -1f;
            if (parryReceiver != null)
                parryReceiver.enabled = false;
            if (Grenade != null)
            {
                Grenade.hooked = false;
                Grenade.ignoreExplosions = true;
                Grenade.CanCollideWithPlayer(false);
            }

            Vector3 normal = collisionNormal.sqrMagnitude > 0.01f
                ? collisionNormal.normalized
                : -simulatedVelocity.normalized;
            Vector3 point = collisionPoint;
            if (point == Vector3.zero && surface != null)
                point = surface.bounds.ClosestPoint(transform.position);
            transform.position = point + normal * 0.05f;
            Transform parent = surface != null && surface.attachedRigidbody != null
                ? surface.attachedRigidbody.transform
                : surface != null ? surface.transform : null;
            if (parent != null)
                transform.SetParent(parent, true);

            if (!body.isKinematic)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            body.useGravity = false;
            body.constraints = RigidbodyConstraints.FreezeAll;
            body.isKinematic = true;
            GelSystem.RegisterStuck(this);
        }

        internal void DetonateStuck()
        {
            if (!IsStuck)
                return;
            if (chainGroupId <= 0)
            {
                nextChainGroupId++;
                if (nextChainGroupId <= 0)
                    nextChainGroupId = 1;
                chainGroupId = nextChainGroupId;
            }
            Explode(GrenadeExplosionMode.Stuck);
        }

        internal void ScheduleChainDetonation(float delay, int incomingChainGroupId)
        {
            if (!IsStuck)
                return;
            float requestedTime = Time.time + Mathf.Max(0f, delay);
            if (chainDetonationAt < 0f || requestedTime < chainDetonationAt)
            {
                chainDetonationAt = requestedTime;
                if (incomingChainGroupId > 0)
                    chainGroupId = incomingChainGroupId;
            }
        }

        private void Explode(GrenadeExplosionMode mode, EnemyIdentifier directEnemy = null)
        {
            if (finished)
                return;
            finished = true;

            float damage;
            float sizeMultiplier;
            float selfDamage;
            float knockbackMultiplier;
            GrenadeLongRangeStyle longRangeStyle = GetLongRangeStyle(mode, directEnemy);
            bool airshot = (mode == GrenadeExplosionMode.Direct || mode == GrenadeExplosionMode.GreenDirect) &&
                           directEnemy != null && IsVanillaRocketAirshot(directEnemy);
            switch (mode)
            {
                case GrenadeExplosionMode.Direct:
                    damage = PluginSettings.Damage;
                    sizeMultiplier = PluginSettings.ExplosionSizeMultiplier;
                    selfDamage = PluginSettings.DirectSelfDamage;
                    knockbackMultiplier = PluginSettings.KnockbackMultiplier;
                    break;
                case GrenadeExplosionMode.Surface:
                    damage = PluginSettings.SurfaceDamage;
                    sizeMultiplier = PluginSettings.ExplosionSizeMultiplier;
                    selfDamage = PluginSettings.SurfaceSelfDamage;
                    knockbackMultiplier = PluginSettings.KnockbackMultiplier;
                    break;
                case GrenadeExplosionMode.Parried:
                    damage = PluginSettings.ParryDamage;
                    sizeMultiplier = PluginSettings.ParryExplosionSize;
                    selfDamage = PluginSettings.ParrySelfDamage;
                    knockbackMultiplier = PluginSettings.KnockbackMultiplier;
                    break;
                case GrenadeExplosionMode.GreenDirect:
                    damage = PluginSettings.GreenDirectDamage;
                    sizeMultiplier = PluginSettings.GreenDirectExplosionSize;
                    selfDamage = PluginSettings.GreenDirectSelfDamage;
                    knockbackMultiplier = PluginSettings.GreenKnockbackMultiplier;
                    break;
                case GrenadeExplosionMode.GreenSurface:
                    damage = PluginSettings.GreenSurfaceDamage;
                    sizeMultiplier = PluginSettings.GreenSurfaceExplosionSize;
                    selfDamage = PluginSettings.GreenSurfaceSelfDamage;
                    knockbackMultiplier = PluginSettings.GreenKnockbackMultiplier;
                    break;
                default:
                    damage = Profile == GrenadeProjectileProfile.GreenContact
                        ? PluginSettings.GreenStuckDamage
                        : PluginSettings.StuckDamage;
                    sizeMultiplier = PluginSettings.StuckExplosionSize;
                    selfDamage = PluginSettings.StuckSelfDamage;
                    knockbackMultiplier = PluginSettings.StuckKnockbackMultiplier;
                    break;
            }

            if (airshot)
            {
                damage = Profile == GrenadeProjectileProfile.GreenContact
                    ? PluginSettings.GreenAirshotDamage
                    : PluginSettings.AirshotDamage;
                sizeMultiplier = Profile == GrenadeProjectileProfile.GreenContact
                    ? PluginSettings.GreenAirshotExplosionSize
                    : PluginSettings.AirshotExplosionSize;
            }

            if (longRangeStyle == GrenadeLongRangeStyle.PipeDream)
            {
                damage *= PluginSettings.PipeDreamDamageMultiplier;
                sizeMultiplier *= PluginSettings.PipeDreamExplosionSizeMultiplier;
            }
            else if (longRangeStyle == GrenadeLongRangeStyle.MoonShot)
            {
                damage *= PluginSettings.MoonShotDamageMultiplier;
                sizeMultiplier *= PluginSettings.MoonShotExplosionSizeMultiplier;
            }

            float visibleRadius = 5f * sizeMultiplier;
            if (Grenade != null && Grenade.explosion != null)
            {
                GameObject blast = Instantiate(Grenade.explosion, transform.position, Quaternion.identity);
                GrenadeLauncherExplosionMarker blastMarker = blast.AddComponent<GrenadeLauncherExplosionMarker>();
                blastMarker.Parried = mode == GrenadeExplosionMode.Parried;
                blastMarker.Mode = mode;
                blastMarker.Damage = Mathf.Max(0f, damage);
                blastMarker.SourceWeapon = Grenade.sourceWeapon;
                blastMarker.DirectEnemyId = directEnemy != null ? directEnemy.GetInstanceID() : 0;
                blastMarker.LongRangeStyle = longRangeStyle;
                blastMarker.StuckHostEnemyId = mode == GrenadeExplosionMode.Stuck ? stuckHostEnemyId : 0;
                blastMarker.StuckCarrierDamage = Profile == GrenadeProjectileProfile.GreenContact
                    ? PluginSettings.GreenDirectDamage
                    : PluginSettings.Damage;
                blastMarker.StuckChainGroupId = mode == GrenadeExplosionMode.Stuck ? chainGroupId : 0;
                blastMarker.Airshot = airshot;
                if (directEnemy != null)
                {
                    Vector3 displacement = transform.position - launchPosition;
                    blastMarker.DirectProfile = Profile;
                    blastMarker.DirectPlanarDistance = new Vector2(displacement.x, displacement.z).magnitude;
                    blastMarker.DirectStraightDistance = displacement.magnitude;
                    blastMarker.TheoreticalSameHeightRange = sameHeightMaximumRange;
                    blastMarker.LaunchElevationDegrees = launchElevationDegrees;
                    blastMarker.ConfiguredStyleThreshold = blastMarker.LongRangeStyle == GrenadeLongRangeStyle.MoonShot
                        ? PluginSettings.MoonShotMinimumDistance
                        : PluginSettings.PipeDreamMinimumDistance;
                }
                Explosion[] explosions = blast.GetComponentsInChildren<Explosion>(true);
                foreach (Explosion explosion in explosions)
                {
                    if (!loggedExplosion)
                    {
                        loggedExplosion = true;
                        Plugin.LogSource.LogInfo(
                            $"Vanilla rocket explosion baseline: damage {explosion.damage}, " +
                            $"enemy multiplier {explosion.enemyDamageMultiplier:0.###}, " +
                            $"push {explosion.pushForceMultiplier:0.###}, radius {explosion.maxSize:0.###}.");
                    }
                    explosion.sourceWeapon = Grenade.sourceWeapon;
                    if (explosion.damage > 0)
                        explosion.damage = Mathf.RoundToInt(Mathf.Max(0f, damage) * 10f);
                    explosion.playerDamageOverride = Mathf.RoundToInt(Mathf.Max(0f, selfDamage));
                    explosion.maxSize *= sizeMultiplier;
                    visibleRadius = Mathf.Max(visibleRadius, explosion.maxSize);
                    explosion.speed *= sizeMultiplier;
                    explosion.pushForceMultiplier *= knockbackMultiplier;
                    explosion.rocketExplosion = false;
                    explosion.isFup = false;
                    explosion.boosted = false;
                    explosion.unblockable = mode == GrenadeExplosionMode.Parried;
                }
                blast.transform.localScale *= sizeMultiplier;

                // Vanilla's super flag changes explosion mechanics (2.5x size/speed).
                // Keep configured grenade mechanics, but show its red super-explosion visual.
                if (airshot || longRangeStyle == GrenadeLongRangeStyle.MoonShot)
                    SpawnRedExplosionVisual();
            }
            else
            {
                Plugin.LogSource.LogWarning("Grenade explosion prefab missing; projectile removed without a blast.");
            }

            if (mode == GrenadeExplosionMode.Stuck)
            {
                GelSystem.Consume(stuckGelStain, stuckGelCoverage, transform.position, visibleRadius);
                GelSystem.TriggerChain(transform.position, this, visibleRadius);
            }
            GelSystem.UnregisterStuck(this);
            Destroy(gameObject);
        }

        private static bool IsVanillaRocketAirshot(EnemyIdentifier enemy)
        {
            if (enemy == null || enemy.dead || enemy.flying)
                return false;

            // Match Grenade.Collision: grounded enemies do not airshot; permanently
            // airborne enemy types are excluded by flying, while freshly spawned
            // enemies retain vanilla's short grace window.
            if (enemy.gce != null && !enemy.gce.onGround)
                return true;
            return enemy.timeSinceSpawned <= 0.15f;
        }

        private void SpawnRedExplosionVisual()
        {
            if (Grenade == null || Grenade.superExplosion == null)
                return;

            GameObject visual = Instantiate(Grenade.superExplosion, transform.position, Quaternion.identity);
            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
            {
                if (collider != null)
                    collider.enabled = false;
            }
            foreach (Explosion explosion in visual.GetComponentsInChildren<Explosion>(true))
            {
                if (explosion == null)
                    continue;
                explosion.sourceWeapon = null;
                explosion.damage = 0;
                explosion.playerDamageOverride = -1;
                explosion.pushForceMultiplier = 0f;
                explosion.rocketExplosion = false;
                explosion.isFup = false;
                explosion.boosted = false;
                explosion.unblockable = false;
            }
            Destroy(visual, 5f);
        }

        private GrenadeLongRangeStyle GetLongRangeStyle(GrenadeExplosionMode mode, EnemyIdentifier directEnemy)
        {
            if (directEnemy == null || (mode != GrenadeExplosionMode.Direct && mode != GrenadeExplosionMode.GreenDirect))
                return GrenadeLongRangeStyle.None;

            Vector3 displacement = transform.position - launchPosition;
            float planarDistance = new Vector2(displacement.x, displacement.z).magnitude;
            if (planarDistance >= Mathf.Max(0f, PluginSettings.MoonShotMinimumDistance))
                return GrenadeLongRangeStyle.MoonShot;
            if (planarDistance >= Mathf.Max(0f, PluginSettings.PipeDreamMinimumDistance))
                return GrenadeLongRangeStyle.PipeDream;
            return GrenadeLongRangeStyle.None;
        }

        private void FinishSilently()
        {
            if (finished)
                return;
            finished = true;
            GelSystem.UnregisterStuck(this);
            Destroy(gameObject);
        }

    }

    internal static class TrajectoryCalibration
    {
        private static float cachedEyeHeight = -1f;

        internal static void CaptureStandingEyeHeight()
        {
            if (cachedEyeHeight > 0f || Plugin.Instance == null || !PluginSettings.EyeHeightScaling)
                return;

            float measured = TryMeasure();
            if (measured > 0f)
            {
                cachedEyeHeight = measured;
                Plugin.LogSource.LogInfo($"Calibrated standing V1 eye height once: {cachedEyeHeight:0.###} Unity units.");
            }
        }

        internal static float GetEyeHeight()
        {
            float fallback = Mathf.Clamp(PluginSettings.FallbackEyeHeight, 0.5f, 10f);
            if (!PluginSettings.EyeHeightScaling)
                return fallback;

            if (cachedEyeHeight > 0f)
                return cachedEyeHeight;

            float measured = TryMeasure();
            if (measured > 0f)
            {
                cachedEyeHeight = measured;
                return cachedEyeHeight;
            }

            return fallback;
        }

        private static float TryMeasure()
        {
            try
            {
                NewMovement movement = MonoSingleton<NewMovement>.Instance;
                CameraController cameraController = MonoSingleton<CameraController>.Instance;
                if (movement == null || movement.playerCollider == null || cameraController == null)
                    return -1f;

                if (movement.gc != null && !movement.gc.onGround)
                    return -1f;

                float measured = cameraController.transform.position.y - movement.playerCollider.bounds.min.y;
                return measured >= 0.5f && measured <= 10f ? measured : -1f;
            }
            catch (Exception exception)
            {
                Plugin.LogSource.LogDebug("Could not measure V1 eye height: " + exception.Message);
                return -1f;
            }
        }
    }

    internal sealed class TerminalIntegration : MonoBehaviour
    {
        private static readonly List<RocketVariantTerminalController> Controllers = new List<RocketVariantTerminalController>();
        private float nextScan;

        private void Update()
        {
            if (Time.unscaledTime < nextScan)
                return;
            nextScan = Time.unscaledTime + 1f;

            Controllers.RemoveAll(controller => controller == null);
            GameObject windowObject = GameObject.Find("Rocket Launcher Window");
            if (windowObject == null || !windowObject.scene.IsValid())
                return;

            Transform window = windowObject.transform;
            TrajectoryCalibration.CaptureStandingEyeHeight();

            Plugin.Instance?.EnsureVariantMigration();
            foreach (VariationInfo variation in window.GetComponentsInChildren<VariationInfo>(true))
            {
                if (variation.weaponName == null || !variation.weaponName.StartsWith("rock", StringComparison.Ordinal) ||
                    variation.weaponName.Length != 5 || variation.GetComponent<RocketVariantTerminalController>() != null)
                    continue;
                RocketVariantTerminalController controller = variation.gameObject.AddComponent<RocketVariantTerminalController>();
                controller.Initialize(variation, variation.weaponName[4] - '0');
                Controllers.Add(controller);
            }
        }

        internal static void Cleanup()
        {
            foreach (RocketVariantTerminalController controller in Controllers)
            {
                if (controller != null)
                    UnityEngine.Object.Destroy(controller);
            }
            Controllers.Clear();
        }
    }

    internal sealed class RocketVariantTerminalController : MonoBehaviour
    {
        private static readonly System.Reflection.FieldInfo EquipStatusField =
            AccessTools.Field(typeof(VariationInfo), "equipStatus");
        private static readonly System.Reflection.FieldInfo GunSetterField =
            AccessTools.Field(typeof(VariationInfo), "gs");
        private static readonly System.Reflection.MethodInfo SetEquipStatusTextMethod =
            AccessTools.Method(typeof(VariationInfo), "SetEquipStatusText");

        private readonly List<Tuple<UnityEngine.Events.UnityEvent, UnityEngine.Events.UnityAction>> listeners =
            new List<Tuple<UnityEngine.Events.UnityEvent, UnityEngine.Events.UnityAction>>();
        private VariationInfo variationInfo;
        private int variation;
        private int displayedStatus;

        internal void Initialize(VariationInfo info, int variant)
        {
            variationInfo = info;
            variation = variant;
            AddPointerListener(info.transform.Find("Equipment/Equipment Status"), 1);
            AddPointerListener(info.transform.Find("Equipment/Buttons/Previous Button"), -1);
            AddPointerListener(info.transform.Find("Equipment/Buttons/Next Button"), 1);
            RefreshDisplay();
        }

        private void AddPointerListener(Transform button, int direction)
        {
            if (button == null)
                return;
            Component pointer = button.GetComponents<Component>()
                .FirstOrDefault(component => component != null && component.GetType().Name == "ControllerPointer");
            if (pointer == null)
                return;
            UnityEngine.Events.UnityEvent pressed = (UnityEngine.Events.UnityEvent)
                AccessTools.Property(pointer.GetType(), "OnPressed").GetValue(pointer);
            UnityEngine.Events.UnityAction action = () => AfterNativeChange(direction);
            pressed.AddListener(action);
            listeners.Add(Tuple.Create(pressed, action));
        }

        private void AfterNativeChange(int direction)
        {
            if (Plugin.Instance == null || variationInfo == null)
                return;

            int next = direction > 0 ? displayedStatus + 1 : displayedStatus - 1;
            if (next > 2)
                next = 0;
            else if (next < 0)
                next = 2;

            displayedStatus = next;
            PrefsManager prefs = MonoSingleton<PrefsManager>.Instance;
            if (prefs != null)
                prefs.SetInt("weapon.rock" + variation, next == 0 ? 0 : 1);
            Plugin.Instance.SetGrenadeModeEnabled(variation, next == 2);

            GunSetter setter = (GunSetter)GunSetterField.GetValue(variationInfo);
            if (setter != null)
                setter.ResetWeapons(false);
            ApplyDisplay(next);
        }

        private void LateUpdate()
        {
            if (variationInfo != null && gameObject.activeInHierarchy)
                RefreshDisplay();
        }

        internal void RefreshDisplay()
        {
            if (Plugin.Instance == null || variationInfo == null)
                return;
            PrefsManager prefs = MonoSingleton<PrefsManager>.Instance;
            int nativeStatus = prefs != null ? prefs.GetInt("weapon.rock" + variation, 1) : 1;
            displayedStatus = nativeStatus <= 0 ? 0 : Plugin.Instance.IsGrenadeModeEnabled(variation) ? 2 : 1;
            ApplyDisplay(displayedStatus);
        }

        private void ApplyDisplay(int status)
        {
            EquipStatusField.SetValue(variationInfo, status);
            SetEquipStatusTextMethod.Invoke(variationInfo, new object[] { status });

            bool equipped = status != 0;
            if (variationInfo.orderButtons != null)
                variationInfo.orderButtons.SetActive(equipped);
            if (variationInfo.icon != null)
            {
                variationInfo.icon.rectTransform.anchoredPosition = equipped ? new Vector2(25f, 0f) : Vector2.zero;
                variationInfo.icon.rectTransform.sizeDelta = equipped ? new Vector2(75f, 75f) : new Vector2(100f, 100f);
            }
        }

        private void OnDestroy()
        {
            foreach (Tuple<UnityEngine.Events.UnityEvent, UnityEngine.Events.UnityAction> listener in listeners)
                listener.Item1.RemoveListener(listener.Item2);
            listeners.Clear();
        }
    }
}
