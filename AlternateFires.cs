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
        private static BoolField terrainStuckDetonatesOnEnemyWalkover;
        private static FloatSliderField gelEnemyFireRadius;
        private static FloatSliderField gelEnemyFireBaseDuration;
        private static FloatSliderField gelEnemyFireExtraDuration;
        private static FloatSliderField gelEnemyFireMaximumDuration;

        private static FloatSliderField redBurstDirectDamage;
        private static FloatSliderField redBurstTerrainDamage;
        private static FloatSliderField redBurstEnemyDamage;
        private static FloatSliderField redBurstAirshotDamage;
        private static FloatSliderField redBurstShotCount;
        private static FloatSliderField redBurstDuration;
        private static FloatSliderField redBurstCooldown;
        private static FloatSliderField redBurstKnockback;

        private static FloatSliderField blueDistance;
        private static FloatSliderField blueCooldown;
        private static FloatSliderField blueSlingshotForce;
        private static FloatSliderField bluePointSize;
        private static FloatSliderField blueReplacementDamage;
        private static FloatSliderField blueReplacementExplosionSize;
        private static FloatSliderField blueReplacementForce;
        private static FloatSliderField pinkArmingDelay;
        private static FloatSliderField pinkConductionArmingDelay;
        private static FloatSliderField pinkConductionRearmDelay;
        private static FloatSliderField pinkPullDelay;
        private static FloatSliderField pinkPullRange;
        private static FloatSliderField pinkPullSpeed;
        private static FloatSliderField blueMaximumHookPoints;
        private static FloatSliderField blueGreenHookPoints;
        private static BoolField blueRequireGroundAfterUse;
        private static BoolField blueKnuckleblasterRefundsCooldown;
        private static readonly Dictionary<EnemyType, FloatSliderField> pinkBlastEnemyDamageMultipliers =
            new Dictionary<EnemyType, FloatSliderField>();
        private static FloatSliderField pipeDreamDistance;
        private static FloatSliderField moonShotDistance;
        private static FloatSliderField pipeDreamStylePoints;
        private static FloatSliderField moonShotStylePoints;
        private static FloatSliderField outSnipedStylePoints;
        private static FloatSliderField walkingBombStylePoints;
        private static FloatSliderField directHitStylePoints;
        private static FloatSliderField pipeDreamDamageMultiplier;
        private static FloatSliderField pipeDreamExplosionSizeMultiplier;
        private static FloatSliderField moonShotDamageMultiplier;
        private static FloatSliderField moonShotExplosionSizeMultiplier;

        internal static float GreenCooldown => greenCooldown?.value ?? 6f;
        internal static float GreenSpeedMultiplier => greenSpeed?.value ?? 1.5f;
        internal static float GreenUpwardVelocityMultiplier => greenUpwardVelocity?.value ?? 1f;
        internal static float GreenGravityMultiplier => greenGravity?.value ?? 1f;
        internal static float GreenDirectDamage => greenDirectDamage?.value ?? 6f;
        internal static float GreenAirshotDamage => greenAirshotDamage?.value ?? 8f;
        internal static float GreenSurfaceDamage => greenSurfaceDamage?.value ?? 4.5f;
        internal static float GreenDirectExplosionSize => greenDirectExplosionSize?.value ?? 1.4f;
        internal static float GreenAirshotExplosionSize => greenAirshotExplosionSize?.value ?? 1.5f;
        internal static float GreenSurfaceExplosionSize => greenSurfaceExplosionSize?.value ?? 1f;
        internal static float GreenDirectSelfDamage => greenDirectSelfDamage?.value ?? 35f;
        internal static float GreenSurfaceSelfDamage => greenSurfaceSelfDamage?.value ?? 35f;
        internal static float GreenKnockbackMultiplier => greenKnockback?.value ?? 1f;
        internal static float GreenLifetime => greenLifetime?.value ?? 15f;

        internal static float GelProjectileSpeedMultiplier => gelProjectileSpeed?.value ?? 1f;
        internal static float GelCoveragePerDroplet => (gelCoveragePerDroplet?.value ?? 4f) / 100f;
        internal static float GelCoverageRequired => (gelCoverageRequired?.value ?? 75f) / 100f;
        internal static float GelSpotSize => gelSpotSize?.value ?? 1.25f;
        internal static float StuckDamage => stuckDamage?.value ?? 4.5f;
        internal static float GreenStuckDamage => greenStuckDamage?.value ?? 6f;
        internal static float StuckExplosionSize => stuckExplosionSize?.value ?? 1.3f;
        internal static float StuckSelfDamage => stuckSelfDamage?.value ?? 35f;
        internal static float StuckKnockbackMultiplier => stuckKnockback?.value ?? 1f;
        internal static float StuckChainRadiusMultiplier => stuckChainRadius?.value ?? 2f;
        internal static float StuckChainPropagationSpeed => stuckChainPropagationSpeed?.value ?? 50f;
        internal static bool EnemyCarrierTakesDirectDamage => enemyCarrierDirectDamage?.value ?? false;
        internal static bool TimedFuseDetonatesStuck => timedFuseDetonatesStuck?.value ?? false;
        internal static bool TerrainStuckDetonatesOnEnemyWalkover => terrainStuckDetonatesOnEnemyWalkover?.value ?? false;
        internal static float GelEnemyFireRadiusMultiplier => gelEnemyFireRadius?.value ?? 2.5f;
        internal static float GelEnemyFireBaseDuration => gelEnemyFireBaseDuration?.value ?? 3f;
        internal static float GelEnemyFireExtraDuration => gelEnemyFireExtraDuration?.value ?? 1f;
        internal static float GelEnemyFireMaximumDuration => gelEnemyFireMaximumDuration?.value ?? 8f;

        internal static float RedBurstDirectDamage => redBurstDirectDamage?.value ?? 2.5f;
        internal static float RedBurstTerrainDamage => redBurstTerrainDamage?.value ?? 3f;
        internal static float RedBurstEnemyDamage => redBurstEnemyDamage?.value ?? 2.5f;
        internal static float RedBurstAirshotDamage => redBurstAirshotDamage?.value ?? 3.5f;
        internal static int RedBurstShotCount => Mathf.Max(1, Mathf.RoundToInt(redBurstShotCount?.value ?? 3f));
        internal static float RedBurstDuration => redBurstDuration?.value ?? 0.3f;
        internal static float RedBurstCooldown => redBurstCooldown?.value ?? 4.5f;
        internal static float RedBurstKnockbackMultiplier => redBurstKnockback?.value ?? 0.4f;

        internal static float BlueDistance => blueDistance?.value ?? 20f;
        internal static float BlueCooldown => blueCooldown?.value ?? 4f;
        internal static float BlueSlingshotForce => blueSlingshotForce?.value ?? 0f;
        internal static float BluePointSize => bluePointSize?.value ?? 1f;
        internal static float BlueReplacementDamage => blueReplacementDamage?.value ?? 3.5f;
        internal static float BlueReplacementExplosionSize => blueReplacementExplosionSize?.value ?? 1.2f;
        internal static float BlueReplacementForce => blueReplacementForce?.value ?? 20000f;
        internal static float PinkFirstArmDelay => pinkArmingDelay?.value ?? 2.25f;
        internal static float PinkRearmDelay => pinkConductionArmingDelay?.value ?? 3.5f;
        internal static float PinkConductionRearmDelay => pinkConductionRearmDelay?.value ?? 5f;
        internal static int BlueMaximumHookPoints => Mathf.Max(1, Mathf.RoundToInt(blueMaximumHookPoints?.value ?? 1f));
        internal static int BlueGreenHookPoints => Mathf.Max(0, Mathf.RoundToInt(blueGreenHookPoints?.value ?? 0f));
        internal static bool BlueRequireGroundAfterUse => blueRequireGroundAfterUse?.value ?? false;
        internal static bool BlueKnuckleblasterRefundsCooldown => blueKnuckleblasterRefundsCooldown?.value ?? true;
        internal static float PinkPullDelay => pinkPullDelay?.value ?? 0.5f;
        internal static float PinkPullRange => pinkPullRange?.value ?? 30f;
        internal static float PinkPullSpeed => pinkPullSpeed?.value ?? 50f;
        internal static float GetPinkBlastEnemyDamageMultiplier(EnemyType enemyType)
        {
            return pinkBlastEnemyDamageMultipliers.TryGetValue(enemyType, out FloatSliderField field)
                ? Mathf.Max(0f, field.value) / 100f
                : GetPinkBlastDefaultDamagePercent(enemyType) / 100f;
        }

        internal static float GetPinkBlastDefaultDamagePercent(EnemyType enemyType)
        {
            switch (enemyType)
            {
                case EnemyType.Drone: return 50f;
                case EnemyType.Soldier: return 60f;
                case EnemyType.Stalker: return 75f;
                case EnemyType.Stray: return 35f;
                default: return 100f;
            }
        }
        internal static float PipeDreamMinimumDistance => pipeDreamDistance?.value ?? 56f;
        internal static float MoonShotMinimumDistance => moonShotDistance?.value ?? 125f;
        internal static int PipeDreamStylePoints => Mathf.RoundToInt(pipeDreamStylePoints?.value ?? 150f);
        internal static int MoonShotStylePoints => Mathf.RoundToInt(moonShotStylePoints?.value ?? 550f);
        internal static int OutSnipedStylePoints => Mathf.RoundToInt(outSnipedStylePoints?.value ?? 550f);
        internal static int WalkingBombStylePoints => Mathf.RoundToInt(walkingBombStylePoints?.value ?? 90f);
        internal static int DirectHitStylePoints => Mathf.RoundToInt(directHitStylePoints?.value ?? 35f);
        internal static float PipeDreamDamageMultiplier => pipeDreamDamageMultiplier?.value ?? 1f;
        internal static float PipeDreamExplosionSizeMultiplier => pipeDreamExplosionSizeMultiplier?.value ?? 1f;
        internal static float MoonShotDamageMultiplier => moonShotDamageMultiplier?.value ?? 1.15f;
        internal static float MoonShotExplosionSizeMultiplier => moonShotExplosionSizeMultiplier?.value ?? 1.15f;
        private static void InitializeAlternateSettings(PluginConfigurator configurator)
        {
            ConfigPanel green = new ConfigPanel(configurator.rootPanel, "Green contact grenade", "greenContactGrenade");
            AddPageResetButton(green, "Reset this page to default", "resetGreenContactGrenade");
            greenCooldown = Slider(green, "Cooldown (seconds)", "greenCooldown", 0.05f, 10f, 6f, 2);
            greenSpeed = Slider(green, "Projectile speed multiplier", "greenSpeedMultiplier", 0.1f, 5f, 1.5f, 2);
            greenUpwardVelocity = Slider(green, "Upward velocity multiplier", "greenUpwardVelocityMultiplier", 0f, 5f, 1f, 2);
            greenGravity = Slider(green, "Gravity multiplier", "greenGravityMultiplier", 0.1f, 5f, 1f, 2);
            greenDirectDamage = Slider(green, "Direct hit damage", "greenDirectDamage", 0f, 20f, 6f, 2);
            greenAirshotDamage = Slider(green, "Airshot damage", "greenAirshotDamage", 0f, 25f, 8f, 2);
            greenSurfaceDamage = Slider(green, "Surface contact damage", "greenSurfaceDamage", 0f, 20f, 4.5f, 2);
            greenDirectExplosionSize = Slider(green, "Direct blast size (Rocket = 1)", "greenDirectExplosionSize", 0.1f, 5f, 1.4f, 2);
            greenAirshotExplosionSize = Slider(green, "Airshot explosion size", "greenAirshotExplosionSize", 0.1f, 5f, 1.5f, 2);
            greenSurfaceExplosionSize = Slider(green, "Surface blast size (Rocket = 1)", "greenSurfaceExplosionSize", 0.1f, 5f, 1f, 2);
            greenDirectSelfDamage = Slider(green, "Direct self damage (HP)", "greenDirectSelfDamage", 0f, 100f, 35f, 0);
            greenSurfaceSelfDamage = Slider(green, "Surface self damage (HP)", "greenSurfaceSelfDamage", 0f, 100f, 35f, 0);
            greenKnockback = Slider(green, "Blast knockback (Rocket = 1)", "greenKnockbackMultiplier", 0f, 5f, 1f, 2);
            greenLifetime = Slider(green, "Silent projectile lifetime", "greenLifetime", 1f, 60f, 15f, 1);

            ConfigPanel gel = new ConfigPanel(configurator.rootPanel, "Red blue-gel system", "redGelSystem");
            AddPageResetButton(gel, "Reset this page to default", "resetRedGelSystem");
            gelProjectileSpeed = Slider(gel, "Gel projectile speed multiplier", "gelProjectileSpeedMultiplier", 0.1f, 5f, 1f, 2);
            gelCoveragePerDroplet = Slider(gel, "Enemy coverage per droplet (%)", "gelCoveragePerDroplet", 0.1f, 100f, 4f, 1);
            gelCoverageRequired = Slider(gel, "Enemy coverage required (%)", "gelCoverageRequired", 0f, 100f, 75f, 1);
            gelSpotSize = Slider(gel, "Terrain gel size (normal = 1)", "gelSpotSize", 0.1f, 5f, 1.25f, 2);
            stuckDamage = Slider(gel, "Primary stuck grenade damage", "stuckDamage", 0f, 20f, 4.5f, 2);
            greenStuckDamage = Slider(gel, "Green stuck grenade damage", "greenStuckDamage", 0f, 20f, 6f, 2);
            stuckExplosionSize = Slider(gel, "Stuck blast size (Rocket = 1)", "stuckExplosionSize", 0.1f, 5f, 1.3f, 2);
            stuckSelfDamage = Slider(gel, "Stuck self-damage (HP)", "stuckSelfDamage", 0f, 100f, 35f, 0);
            stuckKnockback = Slider(gel, "Stuck knockback (Rocket = 1)", "stuckKnockbackMultiplier", 0f, 5f, 1f, 2);
            stuckChainRadius = Slider(gel, "Stuck chain range mult.", "stuckChainRadiusMultiplier", 0f, 10f, 2f, 2);
            stuckChainPropagationSpeed = Slider(gel, "Chain speed (units/s)", "stuckChainPropagationSpeed", 1f, 200f, 50f, 1);
            enemyCarrierDirectDamage = new BoolField(gel, "Carrier takes direct damage", "enemyCarrierDirectDamage", false);
            timedFuseDetonatesStuck = new BoolField(gel, "Non-stuck nades detonate stuck", "timedFuseDetonatesStuck", false);
            terrainStuckDetonatesOnEnemyWalkover = new BoolField(gel, "Enemy walk triggers traps", "terrainStuckDetonatesOnEnemyWalkover", false);
            gelEnemyFireRadius = Slider(gel, "Enemy fire radius (blast = 1)", "gelEnemyFireRadius", 0f, 10f, 2.5f, 2);
            gelEnemyFireBaseDuration = Slider(gel, "Enemy fire base time (s)", "gelEnemyFireBaseDuration", 0f, 20f, 3f, 2);
            gelEnemyFireExtraDuration = Slider(gel, "Extra time per grenade (s)", "gelEnemyFireExtraDuration", 0f, 20f, 1f, 2);
            gelEnemyFireMaximumDuration = Slider(gel, "Enemy fire max time (s)", "gelEnemyFireMaximumDuration", 0f, 60f, 8f, 2);

            ConfigPanel redBurst = new ConfigPanel(gel, "Red gel burst", "redGelBurst");
            AddPageResetButton(redBurst, "Reset this page to default", "resetRedGelBurst");
            redBurstDirectDamage = Slider(redBurst, "Direct hit damage", "redBurstDirectDamage", 0f, 20f, 2.5f, 2);
            redBurstTerrainDamage = Slider(redBurst, "Terrain-stuck grenade damage", "redBurstTerrainDamage", 0f, 20f, 3f, 2);
            redBurstEnemyDamage = Slider(redBurst, "Enemy-stuck grenade damage", "redBurstEnemyDamage", 0f, 20f, 2.5f, 2);
            redBurstAirshotDamage = Slider(redBurst, "Airshot damage", "redBurstAirshotDamage", 0f, 20f, 3.5f, 2);
            redBurstShotCount = Slider(redBurst, "Shot count", "redBurstShotCount", 1f, 10f, 3f, 0);
            redBurstDuration = Slider(redBurst, "Burst duration (seconds)", "redBurstDuration", 0f, 5f, 0.3f, 2);
            redBurstCooldown = Slider(redBurst, "Primary cooldown (seconds)", "redBurstCooldown", 0f, 30f, 4.5f, 2);
            redBurstKnockback = Slider(redBurst, "Blast knockback (Rocket = 1)", "redBurstKnockback", 0f, 5f, 0.4f, 2);

            ConfigPanel blue = new ConfigPanel(configurator.rootPanel, "Blue slingshot point", "blueSlingshotPoint");
            AddPageResetButton(blue, "Reset this page to default", "resetBlueSlingshotPoint");
            blueDistance = Slider(blue, "Placement distance", "bluePlacementDistance", 5.5f, 200f, 20f, 1);
            blueCooldown = Slider(blue, "Cooldown (seconds)", "blueCooldown", 0.05f, 10f, 4f, 2);
            blueSlingshotForce = Slider(blue, "Extra slingshot force", "blueSlingshotForce", -50f, 200f, 0f, 1);
            bluePointSize = Slider(blue, "Hook point size multiplier", "bluePointSize", 0.25f, 4f, 1f, 2);
            blueMaximumHookPoints = Slider(blue, "Maximum created hook points", "blueMaximumHookPoints", 1f, 10f, 1f, 0);
            blueGreenHookPoints = Slider(blue, "Created green hook points", "blueGreenHookPoints", 0f, 10f, 0f, 0);
            blueRequireGroundAfterUse = new BoolField(blue, "Must land after hook use", "blueRequireGroundAfterUse", false);
            blueKnuckleblasterRefundsCooldown = new BoolField(blue, "Knuckleblast refunds cooldown", "blueKnuckleblasterRefundsCooldown", true);
            // Keep the existing field IDs so players' released settings migrate in place.
            pinkArmingDelay = Slider(blue, "Pink first arm time (s)", "pinkArmingDelay", 0f, 30f, 2.25f, 2);
            pinkConductionArmingDelay = Slider(blue, "Pink re-arm time (s)", "pinkConductionArmingDelay", 0f, 30f, 3.5f, 2);
            pinkConductionRearmDelay = Slider(blue, "Pink re-arm after zap (s)", "pinkConductionRearmDelay", 0f, 30f, 5f, 2);
            pinkPullDelay = Slider(blue, "Pink pull delay (seconds)", "pinkPullDelay", 0f, 5f, 0.5f, 2);
            pinkPullRange = Slider(blue, "Pink pull range (units)", "pinkPullRange", 0f, 200f, 30f, 1);
            pinkPullSpeed = Slider(blue, "Pink pull speed (units/second)", "pinkPullSpeed", 0f, 200f, 50f, 1);
            blueReplacementDamage = Slider(blue, "Pink explosion damage", "blueReplacementDamage", 0f, 20f, 3.5f, 2);
            blueReplacementExplosionSize = Slider(blue, "Pink blast size (Providence = 1)", "blueReplacementExplosionSize", 0.1f, 5f, 1.2f, 2);
            blueReplacementForce = Slider(blue, "Pink enemy launch force", "blueReplacementForceRaw", 0f, 50000f, 20000f, 0);

            ConfigPanel pinkEnemies = new ConfigPanel(blue, "Pink blast enemy damage", "pinkBlastEnemyDamage");
            pinkEnemies.headerText = "Extra damage percentage for pink explosion only. 100% = unchanged. Native explosive resistance remains active.";
            AddPageResetButton(pinkEnemies, "Reset this page to default", "resetPinkBlastEnemyDamage");
            pinkBlastEnemyDamageMultipliers.Clear();
            foreach (EnemyType enemyType in Enum.GetValues(typeof(EnemyType)).Cast<EnemyType>().OrderBy(value => value.ToString()))
            {
                pinkBlastEnemyDamageMultipliers[enemyType] = Slider(pinkEnemies,
                    FriendlyEnemyName(enemyType) + " damage (%)", "pinkBlastDamage_" + enemyType,
                    0f, 500f, GetPinkBlastDefaultDamagePercent(enemyType), 0);
            }

            ConfigPanel style = new ConfigPanel(configurator.rootPanel, "Style bonuses", "styleBonuses");
            AddPageResetButton(style, "Reset this page to default", "resetStyleBonuses");
            pipeDreamDistance = Slider(style, "PIPE DREAM minimum distance", "pipeDreamMinimumDistance", 1f, 250f, 56f, 1);
            moonShotDistance = Slider(style, "MOON/OUT-SNIPED range", "moonShotMinimumDistance", 1f, 400f, 125f, 1);
            pipeDreamStylePoints = Slider(style, "PIPE DREAM style points", "pipeDreamStylePoints", 0f, 10000f, 150f, 0);
            moonShotStylePoints = Slider(style, "MOON SHOT style points", "moonShotStylePoints", 0f, 10000f, 550f, 0);
            outSnipedStylePoints = Slider(style, "OUT-SNIPED style points", "outSnipedStylePoints", 0f, 10000f, 550f, 0);
            walkingBombStylePoints = Slider(style, "WALKING BOMB style points", "walkingBombStylePoints", 0f, 5000f, 90f, 0);
            directHitStylePoints = Slider(style, "Direct-hit base style points", "directHitStylePoints", 0f, 5000f, 35f, 0);
            pipeDreamDamageMultiplier = Slider(style, "PIPE DREAM damage multiplier", "pipeDreamDamageMultiplier", 0f, 10f, 1f, 2);
            pipeDreamExplosionSizeMultiplier = Slider(style, "PIPE DREAM blast mult.", "pipeDreamExplosionSizeMultiplier", 0.1f, 10f, 1f, 2);
            moonShotDamageMultiplier = Slider(style, "MOON SHOT damage multiplier", "moonShotDamageMultiplier", 0f, 10f, 1.15f, 2);
            moonShotExplosionSizeMultiplier = Slider(style, "MOON SHOT blast mult.", "moonShotExplosionSizeMultiplier", 0.1f, 10f, 1.15f, 2);

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

    internal static class RedBurstInputContext
    {
        internal static int Depth;
        internal static InputActionState SuppressedAction;
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
            if ((AlternateFireInputContext.Active && ReferenceEquals(__instance, AlternateFireInputContext.SuppressedAction)) ||
                (RedBurstInputContext.Active && ReferenceEquals(__instance, RedBurstInputContext.SuppressedAction)))
                __result = false;
        }
    }

    [HarmonyPatch(typeof(InputActionState), "get_WasPerformedThisFrame")]
    internal static class AlternateFirePerformedPatch
    {
        private static void Postfix(InputActionState __instance, ref bool __result)
        {
            if ((AlternateFireInputContext.Active && ReferenceEquals(__instance, AlternateFireInputContext.SuppressedAction)) ||
                (RedBurstInputContext.Active && ReferenceEquals(__instance, RedBurstInputContext.SuppressedAction)))
                __result = false;
        }
    }

    // Red's normal alt remains the vanilla gel spray.  Only a primary press while that
    // spray is held is replaced; its input is hidden from the original Update so it
    // cannot also fire a normal grenade on the burst's first frame.
    [HarmonyPatch(typeof(RocketLauncher), "Update")]
    [HarmonyPriority(Priority.First)]
    internal static class GrenadeRedBurstUpdatePatch
    {
        private struct State
        {
            internal bool Suppressed;
        }

        private static void Prefix(RocketLauncher __instance, out State __state)
        {
            __state = new State();
            if (Plugin.Instance == null || !Plugin.Instance.IsGrenadeModeEnabled(__instance) || __instance.variation != 2)
                return;

            PlayerInput input = MonoSingleton<InputManager>.Instance?.InputSource;
            if (input == null || input.Fire1 == null)
                return;

            // Match the game's normal input buffering: if the player begins holding
            // primary, then starts spraying gel before the primary cooldown ends, retain
            // that held primary input until red burst is ready instead of letting the
            // next vanilla Update turn it into a normal grenade.
            bool wantsBurst = input.Fire1.IsPressed && input.Fire2 != null && input.Fire2.IsPressed;
            bool blockPrimary = RedBurstController.IsBlockingPrimary(__instance);
            if (!wantsBurst && !blockPrimary)
                return;

            if (wantsBurst)
                RedBurstController.TryStart(__instance);
            RedBurstInputContext.Enter(input.Fire1);
            __state.Suppressed = true;
        }

        private static Exception Finalizer(State __state, Exception __exception)
        {
            if (__state.Suppressed)
                RedBurstInputContext.Exit();
            return __exception;
        }
    }

    internal static class RedBurstController
    {
        private static RocketLauncher activeLauncher;
        private static int shotsFired;
        private static float nextShotAt = -1f;
        private static float cooldownReadyAt;
        private static float cooldownStartedAt;

        internal static bool IsBlockingPrimary(RocketLauncher launcher) =>
            launcher != null && launcher == activeLauncher ||
            (launcher != null && launcher.variation == 2 && !CooldownRules.NoWeaponCooldown && Time.time < cooldownReadyAt);

        internal static void TryStart(RocketLauncher launcher)
        {
            if (launcher == null || launcher.variation != 2 || activeLauncher != null ||
                (!CooldownRules.NoWeaponCooldown && Time.time < cooldownReadyAt) ||
                !RocketCooldownSync.Ready(launcher))
                return;

            activeLauncher = launcher;
            shotsFired = 0;
            nextShotAt = Time.time;
            FireDueShots();
        }

        internal static void Update()
        {
            if (activeLauncher == null)
                return;
            if (!activeLauncher.gameObject.activeInHierarchy || Plugin.Instance == null ||
                !Plugin.Instance.IsGrenadeModeEnabled(activeLauncher) || activeLauncher.variation != 2)
            {
                FinishBurst();
                return;
            }
            FireDueShots();
        }

        private static void FireDueShots()
        {
            if (activeLauncher == null || Time.time + 0.0001f < nextShotAt)
                return;

            int total = PluginSettings.RedBurstShotCount;
            GrenadeSpawnContext.Enter(GrenadeProjectileProfile.RedBurst);
            try
            {
                activeLauncher.Shoot();
            }
            finally
            {
                GrenadeSpawnContext.Exit();
            }
            shotsFired++;
            if (shotsFired >= total)
            {
                FinishBurst();
                return;
            }

            float interval = total <= 1 ? 0f : Mathf.Max(0f, PluginSettings.RedBurstDuration) / (total - 1);
            nextShotAt = Time.time + interval;
        }

        private static void FinishBurst()
        {
            if (activeLauncher != null && !CooldownRules.NoWeaponCooldown)
            {
                cooldownStartedAt = Time.time;
                cooldownReadyAt = Time.time + Mathf.Max(0f, PluginSettings.RedBurstCooldown);
            }
            activeLauncher = null;
            shotsFired = 0;
            nextShotAt = -1f;
        }

        internal static float CooldownProgress => CooldownRules.NoWeaponCooldown || Time.time >= cooldownReadyAt
            ? 1f
            : 1f - Mathf.Clamp01((cooldownReadyAt - Time.time) / Mathf.Max(0.01f, PluginSettings.RedBurstCooldown));

        internal static bool IsCoolingDown => !CooldownRules.NoWeaponCooldown && Time.time < cooldownReadyAt;
        internal static float CooldownStartedAt => cooldownStartedAt;

        internal static void Reset()
        {
            activeLauncher = null;
            shotsFired = 0;
            nextShotAt = -1f;
            cooldownReadyAt = 0f;
            cooldownStartedAt = 0f;
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
                    WeaponVisualRuntime.SyncCooldownDial(__instance, meter, TimerArm(__instance));
                }
            }
            else if (Plugin.Instance != null && Plugin.Instance.IsGrenadeModeEnabled(__instance) && __instance.variation == 2)
            {
                // Red gel still uses the native fuel meter, but the visible dial must be
                // the custom-model copy rather than the rocket rig's animated display.
                UnityEngine.UI.Image meter = TimerMeter(__instance);
                if (meter != null)
                    WeaponVisualRuntime.SyncCooldownDial(__instance, meter, TimerArm(__instance));
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

        internal static void ResetAudioTracking()
        {
            blueLastProgress.Clear();
        }
    }

    internal static class AlternateFireController
    {
        private static readonly AccessTools.FieldRef<RocketLauncher, WeaponIdentifier> WeaponId =
            AccessTools.FieldRefAccess<RocketLauncher, WeaponIdentifier>("wid");
        private static readonly AccessTools.FieldRef<RocketLauncher, TimeSince> SinceEquipped =
            AccessTools.FieldRefAccess<RocketLauncher, TimeSince>("sinceEquipped");
        private const float EquipHeldAltDelay = 0.25f;
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

            // Match the rocket launcher's held-alt behavior: an input held before this
            // launcher finishes drawing cannot fire its alternate immediately. A fresh
            // press after equipping is still allowed through at once.
            if (!altPressedThisFrame && (float)SinceEquipped(launcher) < EquipHeldAltDelay)
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
            // Vanilla allows a newly pressed alternate input during draw-out. Its
            // primary-shot cooldown still starts normally after that shot.
            bool freshAltDuringDraw = altPressedThisFrame && (float)SinceEquipped(launcher) < EquipHeldAltDelay;
            if (!RocketCooldownSync.Ready(launcher) && !continuingGreenVolley && !freshAltDuringDraw)
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
            ClearCooldowns();
            greenVolleyFrame = -1;
            DelayedGreenShot.CancelAll();
            GrenadeAlternateUpdatePatch.ResetAudioTracking();
        }

        internal static void ClearCooldowns()
        {
            greenReadyAt = 0f;
            greenDisplayReadyAt = 0f;
            greenDisplayStartedAt = 0f;
            greenDisplayDuration = 0f;
            blueReadyAt = 0f;
            blueDisplayReadyAt = 0f;
            blueDisplayStartedAt = 0f;
            blueDisplayDuration = 0f;
        }

        // A deliberate Knuckleblaster clear is an optional reward. Reset both the
        // mechanical and UI timer states so the dial immediately agrees with the shot.
        internal static void RefundBlueCooldown()
        {
            blueReadyAt = 0f;
            blueDisplayReadyAt = 0f;
            blueDisplayStartedAt = 0f;
            blueDisplayDuration = 0f;
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

        private static Exception Finalizer(RocketLauncher __instance, bool __state, Exception __exception)
        {
            if (__state)
            {
                GelSpawnContext.Depth = Math.Max(0, GelSpawnContext.Depth - 1);
                if (__exception == null)
                    WeaponVisualRuntime.NotifyRedGelFired(__instance);
            }
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

    // Surface gel deliberately has no gasoline voxel while it is blue gel. This prevents
    // unrelated explosions from lighting it. A real gasoline copy is created only when a
    // grenade stuck in that surface-gel detonates.
    internal static class GelNativeIgnitionContext
    {
        private static int depth;
        internal static bool Active => depth > 0;
        internal static void Enter() => depth++;
        internal static void Exit() => depth = Math.Max(0, depth - 1);
    }

    [HarmonyPatch(typeof(GasolineStain), nameof(GasolineStain.AttachTo))]
    internal static class GelStainAttachPatch
    {
        private static bool Prefix(GasolineStain __instance, Collider other)
        {
            GelStainMarker marker = __instance.GetComponent<GelStainMarker>();
            if (marker == null || GelNativeIgnitionContext.Active)
                return true;

            __instance.transform.SetParent(other.transform, true);
            // Gasoline's normal compute-shader path is intentionally bypassed for blue
            // gel. Keep its one mesh decal a hair above its contact plane so it cannot
            // fight the terrain depth buffer.
            __instance.transform.position -= __instance.transform.forward * 0.02f;
            __instance.SetSize(Mathf.Max(0.1f, PluginSettings.GelSpotSize));
            marker.Surface = other;
            marker.EnemyVisual = GrenadeLauncherProjectile.TryGetLivingEnemy(other) != null;
            marker.Radius = 0.75f * Mathf.Max(0.1f, PluginSettings.GelSpotSize);
            if (!marker.EnemyVisual)
                GelSystem.CacheSurfaceFuelTemplate(__instance);
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
        // Surface stains are flat decals. Collision points can be slightly above/below
        // their visual plane (especially on uneven meshes), so test their tangent-plane
        // distance and give the visible gel a forgiving edge instead of a tiny 3D sphere.
        private const float SurfaceGelStickEdgeAllowance = 0.65f;
        private static readonly List<GelStainMarker> stains = new List<GelStainMarker>();
        private static readonly HashSet<GrenadeLauncherProjectile> stuckGrenades = new HashSet<GrenadeLauncherProjectile>();
        private static GasolineStain surfaceFuelTemplate;

        internal static void CacheSurfaceFuelTemplate(GasolineStain source)
        {
            if (source == null || surfaceFuelTemplate != null)
                return;
            surfaceFuelTemplate = UnityEngine.Object.Instantiate(source);
            GelStainMarker copiedMarker = surfaceFuelTemplate.GetComponent<GelStainMarker>();
            if (copiedMarker != null)
                UnityEngine.Object.Destroy(copiedMarker);
            surfaceFuelTemplate.name = "Grenade Launcher Native Fuel Template";
            surfaceFuelTemplate.transform.SetParent(null, false);
            surfaceFuelTemplate.gameObject.SetActive(false);
            UnityEngine.Object.DontDestroyOnLoad(surfaceFuelTemplate.gameObject);
        }

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
                Vector3 planarOffset = Vector3.ProjectOnPlane(point - stain.transform.position, stain.transform.forward);
                float allowedRadius = stain.Radius + Mathf.Max(SurfaceGelStickEdgeAllowance, stain.Radius * 0.35f);
                if (planarOffset.sqrMagnitude <= allowedRadius * allowedRadius)
                    return stain;
            }
            return null;
        }

        internal static void Consume(
            GelStainMarker stain,
            GelCoverage coverage,
            Vector3 origin,
            float visibleRadius,
            bool preserveIgnitedTerrainStain = false)
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
                    // Native gasoline fire owns its source stain until its burn ends.
                    // Destroying it here immediately deletes BurningVoxel and leaves only
                    // the fire visual. Keep this one stain alive for its normal fire life.
                    if (preserveIgnitedTerrainStain && candidate == stain)
                    {
                        stains.Remove(candidate);
                        continue;
                    }
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

        internal static bool ApplyStuckDetonationFire(
            GelStainMarker stain,
            Collider stuckSurface,
            GelCoverage coverage,
            int hostEnemyId,
            Vector3 origin,
            float explosionRadius)
        {
            Collider surface = stain != null ? stain.Surface : stuckSurface;
            if (surface != null)
            {
                // Create a normal gasoline stain only now, at grenade's position. Blue gel
                // itself is never registered as gasoline and cannot be lit by other blasts.
                GasolineStain source = stain != null ? stain.GetComponent<GasolineStain>() : null;
                if (source == null)
                    source = surfaceFuelTemplate;
                if (source != null)
                {
                    Quaternion rotation = stain != null ? stain.transform.rotation : Quaternion.identity;
                    GasolineStain fuel = UnityEngine.Object.Instantiate(source, origin, rotation);
                    fuel.gameObject.SetActive(true);
                    GelNativeIgnitionContext.Enter();
                    try
                    {
                        fuel.AttachTo(surface, true);
                    }
                    finally
                    {
                        GelNativeIgnitionContext.Exit();
                    }
                    foreach (Renderer renderer in fuel.GetComponentsInChildren<Renderer>(true))
                        renderer.enabled = false;
                    if (MonoSingleton<StainVoxelManager>.Instance != null &&
                        MonoSingleton<StainVoxelManager>.Instance.TryIgniteAt(fuel.transform.position, 3))
                        return true;
                }

                // Fallback for unusual scenes where the voxel manager is unavailable.
                GameObject terrainFireHost = new GameObject("Grenade Launcher Gel Terrain Fire");
                terrainFireHost.transform.SetPositionAndRotation(origin, Quaternion.identity);
                SphereCollider trigger = terrainFireHost.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = Mathf.Max(0.1f, PluginSettings.GelSpotSize);
                Rigidbody rigidbody = terrainFireHost.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;
                rigidbody.useGravity = false;
                Flammable flammable = terrainFireHost.AddComponent<Flammable>();
                GelTerrainFlame flame = terrainFireHost.AddComponent<GelTerrainFlame>();
                flame.Begin(flammable, PluginSettings.GelSpotSize, 3f);
                return false;
            }

            if (coverage == null || hostEnemyId == 0)
                return false;
            EnemyIdentifier host = UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>()
                .FirstOrDefault(enemy => enemy != null && enemy.GetInstanceID() == hostEnemyId && !enemy.dead);
            if (host == null)
                return false;

            // Explode marks its own projectile finished before reaching this method, so
            // it has already fallen out of `IsStuck`. Count it explicitly; three bombs
            // in one enemy must be 3s base + 2s extra = five seconds, not four.
            int grenadeCount = 1 + stuckGrenades.Count(projectile => projectile != null && projectile.IsStuck &&
                projectile.StuckHostEnemyId == hostEnemyId);
            float duration = Mathf.Min(PluginSettings.GelEnemyFireMaximumDuration,
                PluginSettings.GelEnemyFireBaseDuration + (grenadeCount - 1) * PluginSettings.GelEnemyFireExtraDuration);
            float range = Mathf.Max(0f, explosionRadius * PluginSettings.GelEnemyFireRadiusMultiplier);
            foreach (EnemyIdentifier enemy in UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>())
            {
                if (enemy == null || enemy.dead || Vector3.Distance(enemy.transform.position, origin) > range)
                    continue;
                GelBurnTimer.Apply(enemy, duration);
            }
            return false;
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
            if (surfaceFuelTemplate != null)
                UnityEngine.Object.Destroy(surfaceFuelTemplate.gameObject);
            surfaceFuelTemplate = null;
        }
    }

    internal sealed class GelBurnTimer : MonoBehaviour
    {
        private static readonly FieldInfo FuelField = AccessTools.Field(typeof(Flammable), "fuel");
        private static readonly FieldInfo HeatField = AccessTools.Field(typeof(Flammable), "heat");
        private static readonly FieldInfo FlammableEnemyField = AccessTools.Field(typeof(Flammable), "enemy");
        private static readonly FieldInfo FlammableEidField = AccessTools.Field(typeof(Flammable), "eidid");
        private static readonly FieldInfo EnemyFlammablesField = AccessTools.Field(typeof(EnemyIdentifier), "flammables");
        private float expiresAt;
        private Flammable[] flammables;
        private EnemyIdentifier enemy;
        private bool fallbackDirectFire;
        private float nativeBurnCheckAt;
        private float lastNativeDamageAt = float.NegativeInfinity;
        private float nextFallbackDamageAt;
        private static int fallbackDamageDepth;

        internal static void Apply(EnemyIdentifier enemy, float duration)
        {
            if (enemy == null || duration <= 0f)
                return;
            GelBurnTimer timer = enemy.GetComponent<GelBurnTimer>();
            if (timer == null)
                timer = enemy.gameObject.AddComponent<GelBurnTimer>();
            timer.enemy = enemy;
            bool newBurnWindow = Time.time >= timer.expiresAt;
            if (newBurnWindow)
            {
                timer.fallbackDirectFire = false;
                timer.lastNativeDamageAt = float.NegativeInfinity;
                timer.nativeBurnCheckAt = Time.time + 0.75f;
                // Fire damage is native 0.5-damage ticks. Preserve this original start
                // time so a broken native fire path can catch up without losing its
                // first tick during the short detection window.
                timer.nextFallbackDamageAt = Time.time;
            }
            // All grenades already stuck in one carrier determine one burn duration.
            // Do not add a fresh duration once per simultaneous grenade explosion.
            timer.expiresAt = Mathf.Min(Time.time + Mathf.Max(0f, PluginSettings.GelEnemyFireMaximumDuration),
                Mathf.Max(timer.expiresAt, Time.time + duration));
            // AddFlammable is ULTRAKILL's gasoline-on-enemy path. TryIgniteGasoline
            // only checks terrain stains, which is why the previous version did no fire.
            enemy.AddFlammable(1f);
            timer.flammables = FindFlammables(enemy).ToArray();
            if (timer.flammables.Length == 0)
            {
                Flammable fallback = EnsureFallbackFlammable(enemy);
                timer.flammables = FindFlammables(enemy).ToArray();
                if (fallback != null && !timer.flammables.Contains(fallback))
                    timer.flammables = timer.flammables.Concat(new[] { fallback }).ToArray();
            }
            // Flame particles can exist even when the linked EnemyIdentifier never gets
            // native fire damage. Detect actual native fire ticks below, not visuals.
            timer.fallbackDirectFire = timer.flammables.Length == 0;
            enemy.StartBurning(100f);
        }

        private void Update()
        {
            if (Time.time < expiresAt)
            {
                foreach (Flammable flammable in flammables ?? new Flammable[0])
                {
                    if (flammable != null && FuelField != null)
                        FuelField.SetValue(flammable, 1f);
                }
                if (!fallbackDirectFire && Time.time >= nativeBurnCheckAt &&
                    Time.time - lastNativeDamageAt >= 0.75f)
                    fallbackDirectFire = true;
                if (fallbackDirectFire)
                    DeliverFallbackDamageTicks(Time.time);
                return;
            }
            // Cover the last scheduled half-second tick when a native burn never began
            // or stopped early. This is what previously made a 5s fire feel like ~4s.
            if (!fallbackDirectFire && Time.time - lastNativeDamageAt >= 0.75f)
                fallbackDirectFire = true;
            if (fallbackDirectFire)
                DeliverFallbackDamageTicks(expiresAt);
            foreach (Flammable flammable in flammables ?? new Flammable[0])
            {
                if (flammable == null)
                    continue;
                // End damage on exact configured time, then let native Flammable.Pulse
                // transition into its normal visual fade instead of abruptly deleting it.
                FuelField?.SetValue(flammable, 0f);
                HeatField?.SetValue(flammable, 0f);
                flammable.Pulse();
            }
            Destroy(this);
        }

        private void DeliverFallbackDamageTicks(float throughTime)
        {
            if (enemy == null || enemy.dead)
                return;
            const float TickInterval = 0.5f;
            const float TimeEpsilon = 0.001f;
            while (nextFallbackDamageAt < expiresAt - TimeEpsilon &&
                   nextFallbackDamageAt <= throughTime + TimeEpsilon)
            {
                // Tell the damage observer this is our replacement tick, not a real
                // ULTRAKILL native burn tick that should disable fallback mode.
                fallbackDamageDepth++;
                try
                {
                    enemy.hitter = "fire";
                    enemy.DeliverDamage(enemy.gameObject, Vector3.zero, enemy.transform.position,
                        0.5f, false);
                }
                finally
                {
                    fallbackDamageDepth = Math.Max(0, fallbackDamageDepth - 1);
                }
                nextFallbackDamageAt += TickInterval;
            }
        }

        internal static void NotifyNativeFireDamage(EnemyIdentifier target)
        {
            if (fallbackDamageDepth > 0 || target == null)
                return;
            GelBurnTimer timer = target.GetComponent<GelBurnTimer>();
            if (timer == null)
                return;
            timer.lastNativeDamageAt = Time.time;
            // Native ticking resumed. It owns future ticks, while the cursor preserves
            // the point where fallback must resume if it breaks again.
            timer.fallbackDirectFire = false;
            timer.nextFallbackDamageAt = Mathf.Max(timer.nextFallbackDamageAt, Time.time + 0.5f);
        }

        private static IEnumerable<Flammable> FindFlammables(EnemyIdentifier enemy)
        {
            HashSet<Flammable> result = new HashSet<Flammable>(enemy.GetComponentsInChildren<Flammable>(true));
            if (EnemyFlammablesField?.GetValue(enemy) is System.Collections.IEnumerable attached)
            {
                foreach (object item in attached)
                {
                    if (item is Flammable flammable)
                        result.Add(flammable);
                }
            }
            return result;
        }

        private static Flammable EnsureFallbackFlammable(EnemyIdentifier enemy)
        {
            Collider collider = enemy != null ? enemy.GetComponent<Collider>() : null;
            if (collider == null)
                return null;
            GameObject host = collider.gameObject;
            EnemyIdentifierIdentifier identifier = host.GetComponent<EnemyIdentifierIdentifier>();
            if (identifier == null)
                identifier = host.AddComponent<EnemyIdentifierIdentifier>();
            identifier.eid = enemy;
            Flammable flammable = host.GetComponent<Flammable>();
            if (flammable == null)
                flammable = host.AddComponent<Flammable>();
            flammable.fuelOnly = true;
            // Added components have not reached Start yet, but native Burn needs these
            // cached values immediately for this first ignition tick.
            FlammableEnemyField?.SetValue(flammable, true);
            FlammableEidField?.SetValue(flammable, identifier);
            if (EnemyFlammablesField?.GetValue(enemy) is System.Collections.IList values && !values.Contains(flammable))
                values.Add(flammable);
            return flammable;
        }
    }

    // `Flammable.burning` only proves a particle host exists. Hook ULTRAKILL's actual
    // enemy damage path so nearby enemies with a broken native link still receive the
    // same fire ticks as normal gasoline.
    [HarmonyPatch]
    internal static class GelBurnNativeDamagePatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            return typeof(EnemyIdentifier).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(method => method.Name == "DeliverDamage");
        }

        private static void Prefix(EnemyIdentifier __instance)
        {
            if (__instance != null && __instance.hitter == "fire")
                GelBurnTimer.NotifyNativeFireDamage(__instance);
        }
    }

    internal sealed class GelTerrainFireReturn : MonoBehaviour
    {
        private FireObjectPool pool;
        private bool simpleFire;
        private float returnAt;

        internal void Begin(FireObjectPool value, bool simple, float duration)
        {
            pool = value;
            simpleFire = simple;
            returnAt = Time.time + Mathf.Max(0.1f, duration);
        }

        private void Update()
        {
            if (Time.time < returnAt)
                return;
            FireObjectPool targetPool = pool;
            Destroy(this);
            if (targetPool != null)
                targetPool.ReturnFire(gameObject, simpleFire);
            else
                gameObject.SetActive(false);
        }
    }

    // Flammable is ULTRAKILL's normal Firestarter carrier. A short-lived invisible host
    // makes terrain gel use that system rather than leaving a raw pooled particle behind.
    internal sealed class GelTerrainFlame : MonoBehaviour
    {
        private static readonly FieldInfo FuelField = AccessTools.Field(typeof(Flammable), "fuel");
        private static readonly FieldInfo OverrideSizeField = AccessTools.Field(typeof(Flammable), "overrideSize");
        private static readonly FieldInfo UseOverrideSizeField = AccessTools.Field(typeof(Flammable), "useOverrideSize");
        private Flammable flammable;
        private float lifetime;
        private float startedAt;
        private float ignitionRadius;
        private float nextIgnitionCheck;

        internal void Begin(Flammable value, float size, float duration)
        {
            flammable = value;
            lifetime = Mathf.Max(0.1f, duration);
            ignitionRadius = Mathf.Max(0.1f, size * 2f);
            OverrideSizeField?.SetValue(flammable, Vector3.one * Mathf.Max(0.1f, size * 2f));
            UseOverrideSizeField?.SetValue(flammable, true);
        }

        private void Start()
        {
            startedAt = Time.time;
            flammable?.Burn(100f, true);
        }

        private void Update()
        {
            if (flammable != null && FuelField != null)
                FuelField.SetValue(flammable, 1f);
            if (Time.time >= nextIgnitionCheck)
            {
                nextIgnitionCheck = Time.time + 0.15f;
                foreach (EnemyIdentifier enemy in UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>())
                {
                    if (enemy == null || enemy.dead ||
                        Vector3.Distance(enemy.transform.position, transform.position) > ignitionRadius)
                        continue;
                    // Terrain fire is not attached to an EnemyIdentifier, so ULTRAKILL's
                    // pooled flame is visual-only by itself. Ignite nearby enemies through
                    // the same gasoline carrier used by enemy-coated gel.
                    GelBurnTimer.Apply(enemy, 0.5f);
                }
            }
            if (Time.time < startedAt + lifetime)
                return;
            flammable?.PutOut(false);
            Destroy(gameObject);
        }
    }

    internal static class StuckDetonationContext
    {
        private static readonly Stack<RevolverBeam> beams = new Stack<RevolverBeam>();
        internal static RevolverBeam Current => beams.Count > 0 ? beams.Peek() : null;

        internal static bool CanCurrentBeamDetonate
        {
            get { return PlayerHitscanRules.CanDetonateGrenade(Current); }
        }

        internal static void Enter(RevolverBeam beam) => beams.Push(beam);
        internal static void Exit()
        {
            if (beams.Count > 0)
                beams.Pop();
        }
    }

    internal static class PlayerHitscanRules
    {
        internal static bool CanDetonateGrenade(RevolverBeam beam)
        {
            if (beam == null)
                return false;
            if (beam.beamType == BeamType.Revolver)
                return true;
            if (beam.beamType != BeamType.Railgun)
                return false;
            Railcannon rail = beam.sourceWeapon != null
                ? beam.sourceWeapon.GetComponentInParent<Railcannon>()
                : null;
            return rail != null && rail.variation != 1;
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
            HookPointManager.CompleteDelivery(Target, Grenade != null ? Grenade.sourceWeapon : null);
            Destroy(gameObject);
        }
    }

    internal static class HookPointManager
    {
        private const string SlingshotAddress = "Assets/Prefabs/Levels/Interactive/GrapplePointSlingshot Variant.prefab";
        private const string NormalHookAddress = "Sandbox GrapplePoint Variant";
        private const string ProvidenceSlingshotAddress = "Assets/Prefabs/Levels/Interactive/GrapplePointSlingshotProvidence.prefab";
        private const string PlayerShockwaveAddress = "Assets/Prefabs/Attacks and Projectiles/PhysicalShockwavePlayer.prefab";
        private const string DeleteEffectAddress = "Assets/Particles/SandboxDeleterEffect.prefab";
        private const string SandboxArmAddress = "Assets/Prefabs/Weapons/Special/Spawner Arm.prefab";
        private static GameObject current;
        private static readonly List<GameObject> createdHooks = new List<GameObject>();
        private static bool hookRulesInitialized;
        private static int sceneMaximumHooks;
        private static int sceneGreenHooks;
        private static float pinkRearmReadyAt;
        private static GameObject slingshotPrefab;
        private static GameObject normalHookPrefab;
        private static GameObject providenceSlingshotPrefab;
        private static GameObject providenceExplosionEffectPrefab;
        private static GameObject playerShockwavePrefab;
        private static GameObject rocketExplosionPrefab;
        private static GameObject deleteEffectPrefab;
        private static AudioClip deleteSound;
        private static GameObject fleshPrisonHealingTargetEffectPrefab;
        private static AudioClip fleshPrisonHealingClip;
        private static float fleshPrisonHealingVolume = 1f;
        private static bool searchedFleshPrisonHealingAssets;
        private static GameObject secretSoulOrbCollectionEffectPrefab;
        private static bool searchedSecretSoulOrbCollectionEffect;
        private static bool loggedMissingPrefab;
        private static bool loggedMissingReplacementExplosion;
        private static bool creationLockedUntilGround;
        private static bool currentHookUsed;
        private static bool leftGroundSinceHookUse;
        private static HookPoint currentHook;
        private static HookArm currentArm;
        private static float nextArmLookupAt;
        private static float nextHookStateCheckAt;
        private static readonly FieldInfo CaughtHookField = AccessTools.Field(typeof(HookArm), "caughtHook");
        private static readonly FieldInfo FleshPrisonHealingTargetEffectField = AccessTools.Field(typeof(FleshPrison), "healingTargetEffect");
        private static readonly FieldInfo FleshPrisonAudioField = AccessTools.Field(typeof(FleshPrison), "aud");

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
            createdHooks.RemoveAll(item => item == null);
            if (createdHooks.Count == 0)
            {
                creationLockedUntilGround = false;
                currentHookUsed = false;
                leftGroundSinceHookUse = false;
                currentHook = null;
                currentArm = null;
                return;
            }

            if (!PluginSettings.BlueRequireGroundAfterUse)
            {
                creationLockedUntilGround = false;
                currentHookUsed = false;
                return;
            }

            if (currentHookUsed)
            {
                if (creationLockedUntilGround && movement != null && movement.gc != null)
                {
                    if (!movement.gc.onGround)
                        leftGroundSinceHookUse = true;
                    else if (leftGroundSinceHookUse)
                        creationLockedUntilGround = false;
                }
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
                // A generated hook is a one-use vertical-movement tool.  Do not decide
                // its lock from a single frame of grounded state: players can begin a
                // hook from the floor, immediately leave it, and otherwise create a new
                // point while still climbing.  Require a genuine leave-ground/land cycle.
                creationLockedUntilGround = true;
                leftGroundSinceHookUse = movement == null || movement.gc == null || !movement.gc.onGround;
            }
        }

        // HookArm only retains caughtHook for a short slingshot window. Mark usage from
        // HookPoint.Hooked as well as polling it, so a pink point cannot slip through
        // the poll and restore unlimited vertical placement.
        internal static void MarkGeneratedHookUsed(HookPoint hook)
        {
            if (hook == null || hook.GetComponentInParent<GeneratedBlueHookOwnership>() == null)
                return;

            currentHook = hook;
            currentHookUsed = true;
            creationLockedUntilGround = PluginSettings.BlueRequireGroundAfterUse;
            NewMovement movement = MonoSingleton<NewMovement>.Instance;
            leftGroundSinceHookUse = movement == null || movement.gc == null || !movement.gc.onGround;
        }

        internal static void CompleteDelivery(Vector3 point, GameObject sourceWeapon = null)
        {
            PlayDeleteEffect(point);
            EnsureSceneHookRules();
            int maximum = sceneMaximumHooks;
            int greenMaximum = sceneGreenHooks;
            int blueMaximum = maximum - greenMaximum;
            // Remove first, then decide point type. Otherwise a fourth placement sees the
            // old two-blue/one-green mix, removes a blue, and incorrectly creates green.
            while (createdHooks.Count >= maximum)
            {
                // A pink pull owns rigidbodies and temporary visuals for a moment. Do
                // not replace that point mid-pull: wait for its scheduled detonation.
                if (!DestroyGeneratedHook(createdHooks[0]))
                    return;
            }
            int blueCurrent = createdHooks.Count(item => item != null && item.GetComponent<PinkHookPointMarker>() != null);
            bool green = blueCurrent >= blueMaximum;
            GameObject prefab = green ? ResolveNormalHookPrefab() : ResolveSlingshotPrefab();
            if (prefab == null)
                return;
            current = UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity);
            current.name = green ? "Grenade Launcher Green Hook Point" : "Grenade Launcher Blue Slingshot Point";
            GeneratedBlueHookOwnership ownership = current.AddComponent<GeneratedBlueHookOwnership>();
            ownership.SourceWeapon = sourceWeapon;
            if (!green)
                current.AddComponent<PinkHookPointMarker>();
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
            hook.type = green ? hookPointType.Normal : hookPointType.Slingshot;
            if (!green)
                hook.slingShotForce += PluginSettings.BlueSlingshotForce;
            hook.healPlayer = false;
            hook.Activate();
            createdHooks.Add(current);
            currentHook = hook;
            currentArm = UnityEngine.Object.FindObjectOfType<HookArm>();
            nextArmLookupAt = Time.unscaledTime + 0.5f;
            nextHookStateCheckAt = Time.unscaledTime;
            currentHookUsed = false;
            creationLockedUntilGround = false;
            leftGroundSinceHookUse = false;
        }

        internal static void DetonatePinkHook(PinkHookPointMarker marker)
        {
            if (marker == null || !marker.IsPink || !marker.TryConsume())
                return;
            PinkHookDetonation detonation = marker.GetComponent<PinkHookDetonation>();
            if (detonation == null)
                detonation = marker.gameObject.AddComponent<PinkHookDetonation>();
            detonation.Begin(marker);
        }

        internal static void CompletePinkDetonation(PinkHookPointMarker marker)
        {
            if (marker == null)
                return;
            bool conducted = SpawnReplacementExplosion(marker.transform.position, marker.GetComponent<GeneratedBlueHookOwnership>()?.SourceWeapon);
            // Every pink detonation begins the same re-arm lock. A newly placed point
            // reads this shared timestamp too, preventing a replacement from skipping it.
            float rearmTime = conducted ? PluginSettings.PinkConductionRearmDelay : PluginSettings.PinkRearmDelay;
            pinkRearmReadyAt = Mathf.Max(pinkRearmReadyAt, Time.time + Mathf.Max(0f, rearmTime));
            marker.ReturnToBlue(rearmTime);
        }

        internal static float PinkRearmReadyAt => pinkRearmReadyAt;

        private static bool SpawnReplacementExplosion(Vector3 point, GameObject sourceWeapon)
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
                    return false;
            }

            float sizeMultiplier = Mathf.Max(0.1f, PluginSettings.BlueReplacementExplosionSize);
            GameObject blast = UnityEngine.Object.Instantiate(prefab, point, Quaternion.identity);
            // Instantiate active gives Unity one frame before Start; disable immediately
            // so Providence's native blast never starts its own damage/style pipeline.
            blast.SetActive(false);
            blast.name = "Grenade Launcher Providence Replacement Explosion";
            PinkHookConductionRunner conduction = blast.AddComponent<PinkHookConductionRunner>();
            GrenadeLauncherExplosionMarker marker = blast.AddComponent<GrenadeLauncherExplosionMarker>();
            blast.AddComponent<GrenadeLauncherBlueShockwaveMarker>();
            marker.BlueHookReplacement = true;
            marker.Mode = GrenadeExplosionMode.Surface;
            marker.Damage = Mathf.Max(0f, PluginSettings.BlueReplacementDamage);

            float launchForce = Mathf.Max(0f, PluginSettings.BlueReplacementForce);
            float providenceRadius = 0f;
            PhysicalShockwave[] nativeShockwaves = blast.GetComponentsInChildren<PhysicalShockwave>(true);
            foreach (PhysicalShockwave shockwave in nativeShockwaves)
            {
                if (shockwave == null)
                    continue;
                shockwave.damage = Mathf.RoundToInt(marker.Damage * 10f);
                shockwave.maxSize *= sizeMultiplier;
                providenceRadius = Mathf.Max(providenceRadius, shockwave.maxSize);
                // Keep this child visual-only. Its boss-specific shockwave has unreliable
                // close-range damage for player-spawned instances; a player shockwave below
                // provides the actual constant-radius mechanics.
                shockwave.damage = 0;
                shockwave.force = 0f;
                shockwave.hasHurtPlayer = true;
                shockwave.enemy = false;
                shockwave.noDamageToEnemy = true;
            }

            foreach (Explosion explosion in blast.GetComponentsInChildren<Explosion>(true))
            {
                if (explosion == null)
                    continue;
                providenceRadius = Mathf.Max(providenceRadius, explosion.maxSize * sizeMultiplier);
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
                // Explosion.Start owns the Providence visual's material swap, expansion,
                // light, and fade. Keep it enabled but permanently harmless; damage is
                // handled exactly once by DamagePinkHookEnemies below.
                explosion.harmless = true;
            }
            // PhysicalShockwave remains enabled to drive the authored Providence visual.
            // GrenadeLauncherPhysicalShockwavePatch skips its collision method, keeping
            // its native damage and force from running alongside our flat custom blast.

            // Keep Providence's small blue inner sphere, but hide the large white outer
            // sphere.  The prefab names are counterintuitive: the visual named
            // "Sphere_8" is the inner blue layer in-game, while "Sphere_8 (1)" is the
            // larger white ring.
            Transform outerSphere = blast.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item != null && item.name == "Sphere_8 (1)");
            if (outerSphere != null)
                outerSphere.gameObject.SetActive(false);

            // Providence's effect also carries a separate "BS Head" particle/audio child.
            // It is the blood burst used by the original boss effect, rather than part of
            // the blue hook-point explosion, so omit it from the player-created version.
            Transform bloodBurst = blast.GetComponentsInChildren<Transform>(true)
                .FirstOrDefault(item => item != null && item.name == "BS Head");
            if (bloodBurst != null)
                bloodBurst.gameObject.SetActive(false);

            float gameplayRadius = Mathf.Max(providenceRadius, 25f * sizeMultiplier);

            // Run one flat, weapon-owned blast. Native Providence components remain visual
            // only: no second collision, damage event, knockback, or +EXPLODED entry.
            DamagePinkHookEnemies(point, gameplayRadius, marker.Damage, sourceWeapon);
            // Use ULTRAKILL's own ground-slam shockwave only for launch. It supplies
            // the game's grounded-enemy handling, instead of relying on DeliverDamage's
            // force vector, which ground contact was cancelling for most targets.
            if (launchForce > 0f)
                SpawnInvisibleGroundSlamShockwave(point, gameplayRadius, launchForce);

            // This is a disabled child inside the Providence hookpoint prefab. Instantiate it
            // inactive so all damage fields can be neutralized, then activate only the effect.
            blast.SetActive(true);
            return conduction.Trigger(point, gameplayRadius);
        }

        private static void DamagePinkHookEnemies(Vector3 point, float radius, float damage, GameObject sourceWeapon)
        {
            if (damage <= 0f)
                return;
            foreach (EnemyIdentifier enemy in UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>())
            {
                if (enemy == null || enemy.dead || Vector3.Distance(enemy.transform.position, point) > radius)
                    continue;
                // Pink explosion is not a grenade, but it is an explosion: use the
                // normal explosion hitter so enemies' own explosion resistance/weakness
                // applies, while bypassing every grenade-specific modifier.
                // Enemy.GetHurt adds a hard 1.5x bonus to airborne Husks. Pink's pull
                // intentionally creates that airborne state, but it is not an airshot.
                // Temporarily report grounded so normal explosion weakness/resistance
                // remains, while pull height cannot change configured pink damage.
                Enemy enemyController = enemy.GetComponent<Enemy>();
                GroundCheckEnemy groundCheck = enemyController != null ? enemyController.gc : null;
                bool wasGrounded = groundCheck != null && groundCheck.onGround;
                if (groundCheck != null)
                    groundCheck.onGround = true;
                try
                {
                    // The Streetcleaner's dodge is a separate response to a nearby blast,
                    // rather than an explosion damage resistance.  Suppress only that short
                    // response window, so it cannot sidestep a fired pink detonation.
                    Streetcleaner streetcleaner = enemy.GetComponent<Streetcleaner>();
                    if (streetcleaner != null)
                    {
                        PinkHookStreetcleanerDodgeBlock block = streetcleaner.GetComponent<PinkHookStreetcleanerDodgeBlock>();
                        if (block == null)
                            block = streetcleaner.gameObject.AddComponent<PinkHookStreetcleanerDodgeBlock>();
                        block.BlockFor(0.75f);
                    }
                    enemy.hitter = "explosion";
                    enemy.DeliverDamage(enemy.gameObject, Vector3.zero, enemy.transform.position,
                        damage * PluginSettings.GetPinkBlastEnemyDamageMultiplier(enemy.enemyType),
                        false, 0f, sourceWeapon, false, true);
                }
                finally
                {
                    if (groundCheck != null)
                        groundCheck.onGround = wasGrounded;
                }
            }
        }

        internal static List<GameObject> CreateFleshPrisonPullVisuals(Transform hookPoint, IReadOnlyList<Rigidbody> pulledBodies)
        {
            List<GameObject> visuals = new List<GameObject>();
            if (hookPoint == null || !ResolveFleshPrisonHealingAssets())
                return visuals;

            // Flesh Prison parents one of these to each drone and points LineToPoint's
            // second target back at itself. Reuse that exact authored beam for each enemy.
            foreach (Rigidbody body in pulledBodies ?? Enumerable.Empty<Rigidbody>())
            {
                if (body == null)
                    continue;
                GameObject beam = UnityEngine.Object.Instantiate(fleshPrisonHealingTargetEffectPrefab, body.transform);
                beam.name = "Grenade Launcher Pink Pull Beam (Flesh Prison)";
                // This emitter must remain at the enemy's start position. Flesh Prison
                // normally heals stationary drones, but our enemies are being dragged.
                // Detach after capturing world space so LineToPoint no longer follows them.
                beam.transform.SetParent(null, true);
                LineToPoint line = beam.GetComponentInChildren<LineToPoint>(true);
                if (line != null && line.targets != null)
                {
                    if (line.targets.Length > 0)
                        line.targets[0] = beam.transform;
                    if (line.targets.Length > 1)
                        line.targets[1] = hookPoint;
                }
                beam.SetActive(true);
                visuals.Add(beam);
            }

            if (fleshPrisonHealingClip != null)
            {
                AudioSource audio = hookPoint.gameObject.AddComponent<AudioSource>();
                audio.clip = fleshPrisonHealingClip;
                audio.volume = fleshPrisonHealingVolume;
                audio.spatialBlend = 1f;
                audio.rolloffMode = AudioRolloffMode.Logarithmic;
                audio.minDistance = 4f;
                audio.maxDistance = 45f;
                audio.Play();
                UnityEngine.Object.Destroy(audio, Mathf.Max(0.1f, fleshPrisonHealingClip.length + 0.1f));
            }

            return visuals;
        }

        private static bool ResolveFleshPrisonHealingAssets()
        {
            if (fleshPrisonHealingTargetEffectPrefab != null)
                return true;
            if (searchedFleshPrisonHealingAssets)
                return false;
            searchedFleshPrisonHealingAssets = true;

            try
            {
                // Use the runtime-loaded prefab only. Loading gameprefabs again while the
                // game already owns it creates a duplicate-bundle error and a frame hitch.
                FleshPrison source = Resources.FindObjectsOfTypeAll<FleshPrison>().FirstOrDefault(item => item != null);
                if (source != null)
                {
                    fleshPrisonHealingTargetEffectPrefab = FleshPrisonHealingTargetEffectField?.GetValue(source) as GameObject;
                    AudioSource sourceAudio = FleshPrisonAudioField?.GetValue(source) as AudioSource;
                    if (sourceAudio != null)
                    {
                        fleshPrisonHealingClip = sourceAudio.clip;
                        fleshPrisonHealingVolume = sourceAudio.volume;
                    }
                }
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not resolve Flesh Prison pull visuals: " + exception.Message);
            }

            if (fleshPrisonHealingTargetEffectPrefab == null)
            {
                Plugin.LogSource?.LogWarning("Could not find Flesh Prison healing visual prefabs; pink pull remains functional without those visuals.");
                return false;
            }
            return true;
        }

        private static GameObject ResolveProvidenceExplosionEffectPrefab()
        {
            if (providenceExplosionEffectPrefab != null)
                return providenceExplosionEffectPrefab;
            try
            {
                GameObject prefab = ResolveProvidenceSlingshotPrefab();
                Transform effect = prefab != null
                    ? prefab.GetComponentsInChildren<Transform>(true)
                        .FirstOrDefault(item => item != null && item.name == "Explosion Lightning - No Lightning")
                    : null;
                providenceExplosionEffectPrefab = effect != null ? effect.gameObject : null;
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not resolve Providence hook-point explosion effect: " + exception.Message);
            }
            return providenceExplosionEffectPrefab;
        }

        private static void SpawnInvisibleGroundSlamShockwave(Vector3 point, float targetSize, float launchForce)
        {
            if (playerShockwavePrefab == null)
            {
                try
                {
                    playerShockwavePrefab = Addressables.LoadAssetAsync<GameObject>(PlayerShockwaveAddress).WaitForCompletion();
                }
                catch (Exception exception)
                {
                    Plugin.LogSource?.LogWarning("Could not load the replacement shockwave prefab: " + exception.Message);
                }
            }
            if (playerShockwavePrefab == null)
                return;
            GameObject mechanics = UnityEngine.Object.Instantiate(playerShockwavePrefab, point, Quaternion.identity);
            foreach (PhysicalShockwave shockwave in mechanics.GetComponentsInChildren<PhysicalShockwave>(true))
            {
                // Damage is delivered manually at a fixed radius above. Keep this native
                // shockwave purely for its constant-height launch force.
                shockwave.damage = 0;
                shockwave.maxSize = Mathf.Max(0.1f, targetSize);
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
            mechanics.SetActive(true);
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

        private static GameObject ResolveNormalHookPrefab()
        {
            if (normalHookPrefab != null)
                return normalHookPrefab;
            try
            {
                normalHookPrefab = Addressables.LoadAssetAsync<GameObject>(NormalHookAddress).WaitForCompletion();
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not load the green HookPoint addressable: " + exception.Message);
            }
            if (normalHookPrefab == null)
            {
                HookPoint loaded = Resources.FindObjectsOfTypeAll<HookPoint>()
                    .FirstOrDefault(point => point != null && point.type == hookPointType.Normal &&
                                             point.GetComponentInParent<GeneratedBlueHookOwnership>() == null);
                if (loaded != null)
                    normalHookPrefab = loaded.gameObject;
            }
            return normalHookPrefab ?? ResolveSlingshotPrefab();
        }

        internal static bool DestroyGeneratedHook(GameObject hook, bool playSecretSoulOrbCollectionEffect = false)
        {
            if (hook == null)
                return true;
            // Destroying a hook mid-pull also destroys its PinkHookDetonation component.
            // Explicitly cancel first so its rigidbodies regain gravity and its beams are
            // cleaned up instead of being stranded until a scene reload.
            PinkHookDetonation detonation = hook.GetComponent<PinkHookDetonation>();
            if (detonation != null && detonation.IsPulling)
                return false;
            if (playSecretSoulOrbCollectionEffect)
            {
                PlaySecretSoulOrbCollectionEffect(hook.transform.position);
                if (PluginSettings.BlueKnuckleblasterRefundsCooldown)
                    AlternateFireController.RefundBlueCooldown();
            }
            createdHooks.Remove(hook);
            if (ReferenceEquals(current, hook))
                current = createdHooks.LastOrDefault(item => item != null);
            UnityEngine.Object.Destroy(hook);
            return true;
        }

        internal static void TryDestroyKnuckleblasterTarget(Punch punch)
        {
            if (punch == null || punch.type != FistType.Heavy)
                return;
            CameraController camera = MonoSingleton<CameraController>.Instance;
            if (camera == null)
                return;

            const float range = 5f;
            Vector3 origin = camera.transform.position;
            Vector3 direction = camera.transform.forward;
            foreach (GameObject hook in createdHooks.ToArray())
            {
                if (hook == null)
                    continue;
                Vector3 target = hook.transform.position;
                Vector3 offset = target - origin;
                float distance = offset.magnitude;
                if (distance > range || distance < 0.01f || Vector3.Dot(direction, offset / distance) < 0.975f)
                    continue;

                int environmentMask = LayerMaskDefaults.Get(LMD.Environment);
                if (Physics.Raycast(origin, offset / distance, out RaycastHit wall, distance - 0.1f,
                    environmentMask, QueryTriggerInteraction.Ignore))
                    continue;
                DestroyGeneratedHook(hook, true);
                return;
            }
        }

        private static void PlaySecretSoulOrbCollectionEffect(Vector3 position)
        {
            GameObject effectPrefab = ResolveSecretSoulOrbCollectionEffectPrefab();
            if (effectPrefab != null)
            {
                // This is Bonus.breakEffect: the exact prefab ULTRAKILL instantiates
                // when the player picks up a blue secret soul orb.
                GameObject effect = UnityEngine.Object.Instantiate(effectPrefab, position, Quaternion.identity);
                // The stock secret pickup is a small 3D sound near the collectible.
                // A deleted player hook can be much farther from the listener, so keep
                // its authored volume/rolloff but extend its audible max distance 5x.
                foreach (AudioSource audio in effect.GetComponentsInChildren<AudioSource>(true))
                    audio.maxDistance = Mathf.Max(0.01f, audio.maxDistance) * 5f;
            }
        }

        private static GameObject ResolveSecretSoulOrbCollectionEffectPrefab()
        {
            if (secretSoulOrbCollectionEffectPrefab != null)
                return secretSoulOrbCollectionEffectPrefab;
            if (searchedSecretSoulOrbCollectionEffect)
                return null;
            searchedSecretSoulOrbCollectionEffect = true;

            try
            {
                // Blue BonusParticle is not necessarily resident after a level starts.
                // Load ULTRAKILL's original Bonus through its own Addressables helper,
                // rather than falling back to the red ghost object in the scene.
                Bonus source = null;
                try
                {
                    // Verified from ULTRAKILL's Addressables catalogue. AssetHelper is
                    // the persistent global loader; PrefabReplacer is absent in several
                    // scenes (including Sandbox), which is why the previous lookup did
                    // nothing without an exception.
                    const string BlueSecretParticleAddress = "Assets/Particles/Breaks/BonusParticle.prefab";
                    secretSoulOrbCollectionEffectPrefab = AssetHelper.LoadPrefab(BlueSecretParticleAddress);
                }
                catch (Exception exception)
                {
                    Plugin.LogSource?.LogWarning("Could not load blue secret pickup prefab: " + exception.Message);
                }
                if (secretSoulOrbCollectionEffectPrefab == null)
                    source = Resources.FindObjectsOfTypeAll<Bonus>()
                        .Where(item => item != null && item.breakEffect != null && !item.ghost)
                        .OrderByDescending(item => item.secretNumber >= 0)
                        .FirstOrDefault();
                if (secretSoulOrbCollectionEffectPrefab == null && source != null)
                    secretSoulOrbCollectionEffectPrefab = source.breakEffect;
                if (secretSoulOrbCollectionEffectPrefab == null)
                {
                    // Last-resort live lookup: useful immediately after a player has
                    // collected a blue orb in this session.
                    source = Resources.FindObjectsOfTypeAll<Bonus>()
                        .Where(item => item != null && item.breakEffect != null && !item.ghost)
                        .OrderByDescending(item => item.secretNumber >= 0)
                        .FirstOrDefault();
                    secretSoulOrbCollectionEffectPrefab = source != null ? source.breakEffect : null;
                }
                if (secretSoulOrbCollectionEffectPrefab == null)
                    Plugin.LogSource?.LogWarning("Could not find Bonus.breakEffect for secret soul-orb pickup visuals.");
            }
            catch (Exception exception)
            {
                Plugin.LogSource?.LogWarning("Could not resolve secret soul-orb collection effect: " + exception.Message);
            }
            return secretSoulOrbCollectionEffectPrefab;
        }

        private static void EnsureSceneHookRules()
        {
            if (hookRulesInitialized)
                return;
            sceneMaximumHooks = Mathf.Max(1, PluginSettings.BlueMaximumHookPoints);
            sceneGreenHooks = Mathf.Clamp(PluginSettings.BlueGreenHookPoints, 0, sceneMaximumHooks);
            hookRulesInitialized = true;
        }

        internal static void Cleanup()
        {
            foreach (GameObject hookObject in createdHooks.ToArray())
            {
                if (hookObject == null)
                    continue;
                HookPoint point = hookObject.GetComponentInChildren<HookPoint>(true);
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
                UnityEngine.Object.Destroy(hookObject);
            }
            createdHooks.Clear();
            hookRulesInitialized = false;
            sceneMaximumHooks = 0;
            sceneGreenHooks = 0;
            pinkRearmReadyAt = 0f;
            current = null;
            creationLockedUntilGround = false;
            currentHookUsed = false;
            leftGroundSinceHookUse = false;
            currentHook = null;
            currentArm = null;
            nextArmLookupAt = 0f;
            nextHookStateCheckAt = 0f;
        }
    }

    internal sealed class PinkHookConductionRunner : MonoBehaviour
    {
        private static readonly FieldInfo EnemyNailsField = AccessTools.Field(typeof(EnemyIdentifier), "nails");
        private static readonly FieldInfo EnemyMagnetsField = AccessTools.Field(typeof(EnemyIdentifier), "stuckMagnets");
        private static readonly FieldInfo NailCurrentEnemyField = AccessTools.Field(typeof(Nail), "currentHitEnemy");

        internal bool Trigger(Vector3 origin, float radius)
        {
            Magnet nearestMagnet = null;
            Nail nearestNail = null;
            HashSet<Magnet> allMagnets = new HashSet<Magnet>();
            float nearestMagnetDistance = float.PositiveInfinity;
            float nearestNailDistance = float.PositiveInfinity;
            foreach (EnemyIdentifier enemy in UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>())
            {
                if (enemy == null || enemy.dead || Vector3.Distance(enemy.transform.position, origin) > radius)
                    continue;
                HashSet<Nail> nails = new HashSet<Nail>(enemy.GetComponentsInChildren<Nail>(true));
                HashSet<Magnet> magnets = new HashSet<Magnet>(enemy.GetComponentsInChildren<Magnet>(true));
                AddAttachedComponents(EnemyNailsField, enemy, nails);
                AddAttachedComponents(EnemyMagnetsField, enemy, magnets);
                if (nails.Count == 0 && magnets.Count == 0)
                    continue;
                foreach (Magnet magnet in magnets)
                {
                    if (magnet == null)
                        continue;
                    allMagnets.Add(magnet);
                    float distance = Vector3.Distance(magnet.transform.position, origin);
                    if (distance < nearestMagnetDistance)
                    {
                        nearestMagnetDistance = distance;
                        nearestMagnet = magnet;
                    }
                }
                foreach (Nail nail in nails)
                {
                    if (nail == null)
                        continue;
                    float distance = Vector3.Distance(nail.transform.position, origin);
                    if (distance < nearestNailDistance)
                    {
                        nearestNailDistance = distance;
                        nearestNail = nail;
                    }
                }
            }

            // Pink blast damage and conduction are independent. Use vanilla Zap's own
            // default damage (2), but pre-mark magnets so one hookpoint blast produces one
            // chain wave instead of recursively reseeding all nearby magnets.
            if (nearestMagnet != null || nearestNail != null)
            {
                List<GameObject> alreadyHit = allMagnets
                    .Where(magnet => magnet != null)
                    .Select(magnet => magnet.gameObject)
                    .ToList();
                if (nearestNail != null)
                    nearestNail.Zap();
                EnemyIdentifier sourceEnemy = nearestNail != null
                    ? NailCurrentEnemyField?.GetValue(nearestNail) as EnemyIdentifier
                    : null;
                EnemyIdentifier.Zap(origin, 2f, alreadyHit, gameObject, sourceEnemy, null, false);
                return true;
            }
            return false;
        }

        private static void AddAttachedComponents<T>(FieldInfo field, EnemyIdentifier enemy, HashSet<T> result)
            where T : Component
        {
            if (!(field?.GetValue(enemy) is System.Collections.IEnumerable values))
                return;
            foreach (object item in values)
            {
                if (item is T component)
                    result.Add(component);
            }
        }
    }

    internal sealed class GeneratedBlueHookOwnership : MonoBehaviour
    {
        internal GameObject SourceWeapon;
    }

    internal sealed class PinkHookPointMarker : MonoBehaviour
    {
        private static readonly List<PinkHookPointMarker> activeMarkers = new List<PinkHookPointMarker>(4);
        private static readonly Color Pink = new Color(1f, 0f, 0.55f, 1f);
        private static readonly Color PinkEmission = new Color(1f, 0f, 0.2f, 1f) * 3f;
        private bool pink;
        private bool consumed;
        private float createdAt;
        private float armDelay;
        private SphereCollider hitscanCollider;
        private SphereCollider piercingHitscanCollider;
        private HookPoint[] hookPoints = Array.Empty<HookPoint>();
        private readonly List<Renderer> originalRenderers = new List<Renderer>();
        private readonly List<Material[]> originalMaterialSets = new List<Material[]>();
        private readonly Dictionary<SpriteRenderer, Color> originalSpriteColors = new Dictionary<SpriteRenderer, Color>();
        private readonly List<Material> pinkMaterials = new List<Material>();
        private readonly List<Light> originalLights = new List<Light>();
        private readonly List<Color> originalLightColors = new List<Color>();

        internal bool IsPink => pink && !consumed;
        internal bool IsCommitted => consumed;

        private void Awake()
        {
            createdAt = Time.time;
            armDelay = PluginSettings.PinkFirstArmDelay;
            hookPoints = GetComponentsInChildren<HookPoint>(true);
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                originalRenderers.Add(renderer);
                originalMaterialSets.Add(renderer.sharedMaterials.ToArray());
                if (renderer is SpriteRenderer sprite)
                    originalSpriteColors[sprite] = sprite.color;
            }
            foreach (Light light in GetComponentsInChildren<Light>(true))
            {
                if (light == null)
                    continue;
                originalLights.Add(light);
                originalLightColors.Add(light.color);
            }
            // Hook points live on their own layer, so give hitscan weapons a separate,
            // trigger-only target on the normal enemy layer without changing hookshot.
            GameObject hitbox = new GameObject("Pink Hookpoint Hitscan Target");
            hitbox.layer = 11; // ULTRAKILL's raycastable enemy-trigger layer.
            hitbox.transform.SetParent(transform, false);

            SphereCollider source = GetComponent<SphereCollider>();
            hitscanCollider = CreateHitscanCollider(hitbox, source);

            // Charged/slab revolver shots and railcannons cast against ULTRAKILL's
            // piercing mask instead of the ordinary enemy-trigger mask. Give them a
            // second trigger-only target; layer 24 is included by that vanilla mask.
            GameObject piercingHitbox = new GameObject("Pink Hookpoint Piercing Hitscan Target");
            piercingHitbox.layer = 24;
            piercingHitbox.transform.SetParent(transform, false);
            piercingHitscanCollider = CreateHitscanCollider(piercingHitbox, source);
        }

        private void OnEnable()
        {
            if (!activeMarkers.Contains(this))
                activeMarkers.Add(this);
        }

        private void OnDisable()
        {
            activeMarkers.Remove(this);
        }

        private static SphereCollider CreateHitscanCollider(GameObject target, SphereCollider source)
        {
            SphereCollider collider = target.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = source != null ? source.radius : 1f;
            collider.center = source != null ? source.center : Vector3.zero;
            collider.enabled = false;
            return collider;
        }

        private void Update()
        {
            // HookArm reads HookPoint.type again at the end of a pull. A scene/prefab
            // update can otherwise reset the field during travel and turn a pink point
            // into a green stop-point despite it starting as a slingshot.
            if (!consumed)
                EnsureSlingshotState();
            if (!pink && Time.time >= Mathf.Max(createdAt + Mathf.Max(0f, armDelay), HookPointManager.PinkRearmReadyAt))
                TurnPink();
        }

        internal bool TryConsume()
        {
            if (!IsPink)
                return false;
            consumed = true;
            if (hitscanCollider != null)
                hitscanCollider.enabled = false;
            if (piercingHitscanCollider != null)
                piercingHitscanCollider.enabled = false;
            return true;
        }

        // During the pull, reuse the hookpoint's own two pink rings rather than spawning
        // another object on top. They return to the normal blue setup in ReturnToBlue.
        internal void SetPullVisualGreen()
        {
            Color green = new Color(0.12f, 1f, 0.25f, 1f);
            Color emission = green * 3f;
            // TurnPink already created exactly one runtime material for each visible
            // hook ring. Reuse that cache; Renderer.materials would clone again on every
            // pull and caused the little recurring hitch while Unity uploads materials.
            foreach (Material material in pinkMaterials)
            {
                if (material == null)
                    continue;
                if (material.HasProperty("_Color"))
                    material.color = green;
                if (material.HasProperty("_EmissionColor"))
                    material.SetColor("_EmissionColor", emission);
            }
            foreach (SpriteRenderer sprite in originalSpriteColors.Keys)
            {
                if (sprite != null)
                    sprite.color = green;
            }
            foreach (Light light in originalLights)
            {
                if (light != null)
                    light.color = green;
            }
        }

        internal void ReturnToBlue(float rearmTime)
        {
            pink = false;
            consumed = false;
            createdAt = Time.time;
            armDelay = Mathf.Max(0f, rearmTime);
            if (hitscanCollider != null)
                hitscanCollider.enabled = false;
            if (piercingHitscanCollider != null)
                piercingHitscanCollider.enabled = false;
            for (int index = 0; index < originalRenderers.Count; index++)
            {
                Renderer renderer = originalRenderers[index];
                if (renderer != null)
                    renderer.sharedMaterials = originalMaterialSets[index];
            }
            foreach (KeyValuePair<SpriteRenderer, Color> pair in originalSpriteColors)
            {
                if (pair.Key != null)
                    pair.Key.color = pair.Value;
            }
            foreach (Material material in pinkMaterials)
            {
                if (material != null)
                    Destroy(material);
            }
            pinkMaterials.Clear();
            for (int index = 0; index < originalLights.Count; index++)
            {
                Light light = originalLights[index];
                if (light != null)
                    light.color = originalLightColors[index];
            }
            EnsureSlingshotState();
        }

        private void TurnPink()
        {
            pink = true;
            EnsureSlingshotState();
            if (hitscanCollider != null)
                hitscanCollider.enabled = true;
            if (piercingHitscanCollider != null)
                piercingHitscanCollider.enabled = true;
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                    continue;
                renderer.enabled = true;
                if (renderer is SpriteRenderer sprite)
                {
                    sprite.color = Pink;
                    continue;
                }

                Shader shader = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
                Material material = new Material(shader);
                material.color = Pink;
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", PinkEmission);
                }
                renderer.material = material;
                pinkMaterials.Add(material);
            }
            foreach (Light light in GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    light.color = Pink;
            }
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particles == null)
                    continue;
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

        }

        internal void EnsureSlingshotState()
        {
            foreach (HookPoint hook in hookPoints)
            {
                if (hook == null)
                    continue;
                hook.active = true;
                hook.type = hookPointType.Slingshot;
            }
        }

        private void DisableWhiplashBlockingHitboxes()
        {
            // Normal revolver shots need Layer 11, but HookArm's broad throw mask also
            // includes it. During that one cast, hide only our auxiliary hitscan target;
            // original blue HookPoint collider remains and HookArm catches its Slingshot.
            if (hitscanCollider != null && hitscanCollider.enabled)
                hitscanCollider.enabled = false;
            if (piercingHitscanCollider != null && piercingHitscanCollider.enabled)
                piercingHitscanCollider.enabled = false;
        }

        private void RestoreWhiplashBlockingHitboxes()
        {
            bool enabled = IsPink;
            if (hitscanCollider != null)
                hitscanCollider.enabled = enabled;
            if (piercingHitscanCollider != null)
                piercingHitscanCollider.enabled = enabled;
        }

        internal static void DisableActiveWhiplashBlockingHitboxes()
        {
            for (int index = activeMarkers.Count - 1; index >= 0; index--)
            {
                PinkHookPointMarker marker = activeMarkers[index];
                if (marker == null)
                {
                    activeMarkers.RemoveAt(index);
                    continue;
                }
                marker.DisableWhiplashBlockingHitboxes();
            }
        }

        internal static void RestoreActiveWhiplashBlockingHitboxes()
        {
            for (int index = activeMarkers.Count - 1; index >= 0; index--)
            {
                PinkHookPointMarker marker = activeMarkers[index];
                if (marker == null)
                {
                    activeMarkers.RemoveAt(index);
                    continue;
                }
                marker.RestoreWhiplashBlockingHitboxes();
            }
        }
    }

    internal sealed class PinkHookDetonation : MonoBehaviour
    {
        private static readonly FieldInfo EnemyBodyField = AccessTools.Field(typeof(EnemyIdentifier), "rb");
        private static readonly FieldInfo EnemyGroundCheckField = AccessTools.Field(typeof(EnemyIdentifier), "gce");
        private PinkHookPointMarker marker;
        private float finishAt;
        private readonly List<Rigidbody> pulledBodies = new List<Rigidbody>();
        private readonly List<Vector3> pullOffsets = new List<Vector3>();
        private readonly List<bool> previousGravity = new List<bool>();
        private readonly List<object> forcedAirGroundChecks = new List<object>();
        private readonly List<GameObject> pullVisuals = new List<GameObject>();
        private bool restored;

        internal void Begin(PinkHookPointMarker value)
        {
            marker = value;
            marker?.SetPullVisualGreen();
            finishAt = Time.time + Mathf.Max(0f, PluginSettings.PinkPullDelay);
            float range = Mathf.Max(0f, PluginSettings.PinkPullRange);
            foreach (EnemyIdentifier enemy in UnityEngine.Object.FindObjectsOfType<EnemyIdentifier>())
            {
                Rigidbody body = enemy != null
                    ? EnemyBodyField?.GetValue(enemy) as Rigidbody ?? enemy.GetComponentInChildren<Rigidbody>(true)
                    : null;
                if (enemy == null || enemy.dead || body == null || enemy.bigEnemy || enemy.stationary ||
                    IsPinkPullExcluded(enemy) ||
                    Vector3.Distance(enemy.transform.position, transform.position) > range)
                    continue;
                pulledBodies.Add(body);
                previousGravity.Add(body.useGravity);
                body.useGravity = false;
                object groundCheck = EnemyGroundCheckField?.GetValue(enemy);
                if (groundCheck != null)
                {
                    AccessTools.Method(groundCheck.GetType(), "ForceOff")?.Invoke(groundCheck, null);
                    forcedAirGroundChecks.Add(groundCheck);
                }
            }
            BuildPullOffsets();
            pullVisuals.AddRange(HookPointManager.CreateFleshPrisonPullVisuals(transform, pulledBodies));
        }

        private static bool IsPinkPullExcluded(EnemyIdentifier enemy)
        {
            switch (enemy.enemyType)
            {
                // The Earthmover defence system's tower, mortar, and rocket launcher
                // are all Centaur variants, despite their distinct prefab names.
                case EnemyType.Centaur:
                case EnemyType.Gutterman:
                case EnemyType.Guttertank:
                case EnemyType.Idol:
                case EnemyType.Deathcatcher:
                case EnemyType.Providence:
                    return true;
            }

            // Earthmover defense units do not have their own EnemyType entries. Their
            // identifiers live under the named defense-system hierarchy, so exclude the
            // whole system without accidentally disabling ordinary Turrets or Virtues.
            for (Transform current = enemy.transform; current != null; current = current.parent)
            {
                string name = current.name;
                if (name.IndexOf("Earthmover", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Defense System", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("Defence System", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private void Update()
        {
            if (marker == null)
            {
                RestorePulledEnemyState();
                Destroy(this);
                return;
            }
            if (Time.time < finishAt)
                return;
            RestorePulledEnemyState();
            HookPointManager.CompletePinkDetonation(marker);
            Destroy(this);
        }

        private void RestorePulledEnemyState()
        {
            if (restored)
                return;
            restored = true;
            for (int index = 0; index < pulledBodies.Count; index++)
            {
                if (pulledBodies[index] != null)
                    pulledBodies[index].useGravity = previousGravity[index];
            }
            foreach (object groundCheck in forcedAirGroundChecks)
                AccessTools.Method(groundCheck?.GetType(), "StopForceOff")?.Invoke(groundCheck, null);
            forcedAirGroundChecks.Clear();
            foreach (GameObject visual in pullVisuals)
            {
                if (visual != null)
                    Destroy(visual);
            }
            pullVisuals.Clear();
        }

        internal void Cancel()
        {
            RestorePulledEnemyState();
        }

        internal bool IsPulling => !restored && marker != null && Time.time < finishAt;

        private void OnDestroy()
        {
            // Covers non-manager destruction too (scene unload, cleanup, etc.).
            RestorePulledEnemyState();
        }

        private void FixedUpdate()
        {
            if (marker == null || Time.time >= finishAt)
                return;
            float speed = Mathf.Max(0f, PluginSettings.PinkPullSpeed);
            for (int index = 0; index < pulledBodies.Count; index++)
            {
                Rigidbody body = pulledBodies[index];
                if (body == null)
                    continue;
                Vector3 target = transform.position + pullOffsets[index];
                body.velocity = Vector3.zero;
                // GroundCheckEnemy is force-disabled in Begin, so grounded enemies can be
                // moved vertically and carried cleanly over an edge as well as airborne ones.
                body.MovePosition(Vector3.MoveTowards(body.position, target, speed * Time.fixedDeltaTime));
            }
        }

        private void BuildPullOffsets()
        {
            pullOffsets.Clear();
            int count = pulledBodies.Count;
            if (count <= 1)
            {
                if (count == 1)
                    pullOffsets.Add(Vector3.zero);
                return;
            }

            // Do not collapse a group into a single rigidbody pile before the blast.
            // Eight targets fit per ring; extra targets get a wider, staggered ring.
            const int PerRing = 8;
            for (int index = 0; index < count; index++)
            {
                int ring = index / PerRing;
                int ringStart = ring * PerRing;
                int ringCount = Mathf.Min(PerRing, count - ringStart);
                int ringIndex = index - ringStart;
                float angle = (ringIndex / (float)ringCount) * Mathf.PI * 2f + (ring % 2) * Mathf.PI * 0.125f;
                float radius = 1.45f + ring * 1.25f;
                pullOffsets.Add(new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
            }
        }
    }

    // Short, per-enemy gate. It is armed only when a pink blast includes this
    // Streetcleaner, so normal combat dodge behavior remains untouched.
    internal sealed class PinkHookStreetcleanerDodgeBlock : MonoBehaviour
    {
        private float blockedUntil;

        internal void BlockFor(float seconds)
        {
            blockedUntil = Mathf.Max(blockedUntil, Time.time + Mathf.Max(0f, seconds));
        }

        internal bool IsBlocking => Time.time < blockedUntil;
    }

    [HarmonyPatch(typeof(Streetcleaner), nameof(Streetcleaner.Dodge))]
    internal static class PinkHookStreetcleanerDodgePatch
    {
        private static bool Prefix(Streetcleaner __instance)
        {
            PinkHookStreetcleanerDodgeBlock block = __instance != null
                ? __instance.GetComponent<PinkHookStreetcleanerDodgeBlock>()
                : null;
            return block == null || !block.IsBlocking;
        }
    }

    // HookArm checks this field after calling HookPoint.Hooked.  Enforce the generated
    // point's native blue/slingshot type at that exact boundary so pink visual work can
    // never leave it behaving like a normal (green) hook point.
    [HarmonyPatch(typeof(HookPoint), nameof(HookPoint.Hooked))]
    internal static class GeneratedPinkHookSlingshotPatch
    {
        private static bool Prefix(HookPoint __instance)
        {
            PinkHookPointMarker marker = __instance != null
                ? __instance.GetComponentInParent<PinkHookPointMarker>()
                : null;
            if (marker != null && marker.IsCommitted)
                return false;
            marker?.EnsureSlingshotState();
            HookPointManager.MarkGeneratedHookUsed(__instance);
            return true;
        }

        private static void Postfix(HookPoint __instance)
        {
            __instance?.GetComponentInParent<PinkHookPointMarker>()?.EnsureSlingshotState();
        }
    }

    // The type used for blue-vs-green behavior is checked in HookArm.FixedUpdate when
    // the player reaches the point. Enforce it immediately before ULTRAKILL performs
    // that check; an Update-order race can no longer turn an armed pink point green.
    [HarmonyPatch(typeof(HookArm), "FixedUpdate")]
    internal static class GeneratedPinkHookReachSlingshotPatch
    {
        private static readonly FieldInfo CaughtHookField = AccessTools.Field(typeof(HookArm), "caughtHook");
        private static readonly FieldInfo HookTypeField = AccessTools.Field(typeof(HookPoint), nameof(HookPoint.type));
        private static readonly MethodInfo GetGeneratedHookTypeMethod =
            AccessTools.Method(typeof(GeneratedPinkHookReachSlingshotPatch), nameof(GetGeneratedHookType));

        private static void Prefix(HookArm __instance, out bool __state)
        {
            // Auxiliary hitscan colliders can only interfere while the hook is flying.
            // Avoid scene scans, allocations, and collider broadphase churn on every
            // ordinary physics tick.
            __state = __instance != null && __instance.state == HookState.Throwing;
            if (__state)
                PinkHookPointMarker.DisableActiveWhiplashBlockingHitboxes();
            HookPoint hook = CaughtHookField?.GetValue(__instance) as HookPoint;
            hook?.GetComponentInParent<PinkHookPointMarker>()?.EnsureSlingshotState();
        }

        private static Exception Finalizer(bool __state, Exception __exception)
        {
            if (__state)
                PinkHookPointMarker.RestoreActiveWhiplashBlockingHitboxes();
            return __exception;
        }

        // HookArm reads HookPoint.type inside its own FixedUpdate after the hook raycast.
        // Replacing that exact read removes frame/order and approach-side races entirely.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldfld && Equals(instruction.operand, HookTypeField))
                    yield return new CodeInstruction(OpCodes.Call, GetGeneratedHookTypeMethod);
                else
                    yield return instruction;
            }
        }

        private static hookPointType GetGeneratedHookType(HookPoint hook)
        {
            PinkHookPointMarker marker = hook != null
                ? hook.GetComponentInParent<PinkHookPointMarker>()
                : null;
            if (marker != null)
            {
                marker.EnsureSlingshotState();
                return hookPointType.Slingshot;
            }
            return hook != null ? hook.type : hookPointType.Normal;
        }
    }

    [HarmonyPatch(typeof(RevolverBeam), nameof(RevolverBeam.ExecuteHits))]
    internal static class PinkHookPointHitscanPatch
    {
        private static bool Prefix(RevolverBeam __instance, PhysicsCastResult currentHit)
        {
            PinkHookPointMarker point = currentHit.collider != null
                ? currentHit.collider.GetComponentInParent<PinkHookPointMarker>()
                : null;
            if (point == null || !point.IsPink || !PlayerHitscanRules.CanDetonateGrenade(__instance))
                return true;

            HookPointManager.DetonatePinkHook(point);
            return false;
        }

    }

    // Red revolver's normal shot reaches this earlier private path on some current builds.
    // Handle it too; coins remain completely ignored and cannot auto-target pink points.
    [HarmonyPatch(typeof(RevolverBeam), "HitSomething")]
    internal static class PinkHookPointEarlyHitscanPatch
    {
        private static bool Prefix(RevolverBeam __instance, PhysicsCastResult hit)
        {
            PinkHookPointMarker point = hit.collider != null
                ? hit.collider.GetComponentInParent<PinkHookPointMarker>()
                : null;
            if (point == null || !point.IsPink || !PlayerHitscanRules.CanDetonateGrenade(__instance))
                return true;
            HookPointManager.DetonatePinkHook(point);
            return false;
        }
    }

    // Pink points need a trigger target for the specific revolver/rail hitscan paths,
    // but ordinary projectile collisions must pass through it. Otherwise shotgun pellets
    // and the Screwdriver collide with an invisible wall.
    [HarmonyPatch(typeof(Projectile), "OnTriggerEnter")]
    internal static class PinkHookProjectilePassThroughPatch
    {
        private static bool Prefix(Collider other)
        {
            return other == null || other.GetComponentInParent<PinkHookPointMarker>() == null;
        }
    }

    // Some projectile families use Projectile.Collided rather than Unity's trigger message.
    // Skip only pink hookpoint contacts: magnets still stick through their own Magnet path.
    [HarmonyPatch(typeof(Projectile), "Collided")]
    internal static class PinkHookProjectileCollidedPassThroughPatch
    {
        private static bool Prefix(Collider other)
        {
            return other == null || other.GetComponentInParent<PinkHookPointMarker>() == null;
        }
    }


    // PunchSuccess is the direct fist ray hit. BlastCheck is the Knuckleblaster shockwave,
    // intentionally left alone so only a deliberate melee hit clears a player-made point.
    [HarmonyPatch(typeof(Punch), "PunchSuccess")]
    internal static class GeneratedHookKnuckleblasterPatch
    {
        private static void Postfix(Punch __instance, Transform target)
        {
            if (__instance == null || __instance.type != FistType.Heavy || target == null)
                return;
            GeneratedBlueHookOwnership ownership = target.GetComponentInParent<GeneratedBlueHookOwnership>();
            if (ownership != null)
                HookPointManager.DestroyGeneratedHook(ownership.gameObject, true);
        }
    }

    // Native blue/green hook points intentionally do not expose a hitscan collider. Use the
    // direct Heavy-fist frame instead of adding one: normal shots and coins can still pass by.
    [HarmonyPatch(typeof(Punch), "ActiveFrame")]
    internal static class GeneratedHookKnuckleblasterAimPatch
    {
        private static void Postfix(Punch __instance)
        {
            HookPointManager.TryDestroyKnuckleblasterTarget(__instance);
        }
    }

    [HarmonyPatch(typeof(NewMovement), nameof(NewMovement.Respawn))]
    internal static class GrenadeLauncherRespawnCooldownPatch
    {
        private static void Postfix()
        {
            AlternateFireController.Reset();
            RocketCooldownSync.Reset();
        }
    }

    internal sealed class AlternateFireRuntime : MonoBehaviour
    {
        private bool lastGrenadeMode;
        private bool noCooldownWasActive;

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            lastGrenadeMode = Plugin.Instance != null && Plugin.Instance.AnyGrenadeModeEnabled;
            noCooldownWasActive = CooldownRules.NoWeaponCooldown;
        }

        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void Update()
        {
            HookPointManager.UpdateUsageLock();
            RedBurstController.Update();
            bool noCooldown = CooldownRules.NoWeaponCooldown;
            if (noCooldown && !noCooldownWasActive)
            {
                RocketCooldownSync.Reset();
                AlternateFireController.ClearCooldowns();
            }
            noCooldownWasActive = noCooldown;
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
            RedBurstController.Reset();
            RocketCooldownSync.Reset();
            AlternateFireInputContext.Depth = 0;
            AlternateFireInputContext.SuppressedAction = null;
            RedBurstInputContext.Depth = 0;
            RedBurstInputContext.SuppressedAction = null;
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
