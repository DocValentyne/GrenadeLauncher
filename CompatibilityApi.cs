using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GrenadeLauncherMod
{
    /// <summary>
    /// Four paint groups used by the Grenade Launcher's custom viewmodel.
    /// External mods can override these per RocketLauncher instance without
    /// touching the player's normal Grenade Launcher visual settings.
    /// </summary>
    public struct GrenadeLauncherPaintPalette
    {
        public Color Orange;
        public Color Silver;
        public Color Black;
        public Color DeepBlack;

        public GrenadeLauncherPaintPalette(Color orange, Color silver, Color black, Color deepBlack)
        {
            Orange = orange;
            Silver = silver;
            Black = black;
            DeepBlack = deepBlack;
        }
    }

    /// <summary>
    /// Small compatibility surface for mods that create their own RocketLauncher instances
    /// but want Grenade Launcher's primary projectile/visual implementation.
    ///
    /// Registering one exact RocketLauncher does NOT change the terminal-selected state for
    /// other launchers of the same vanilla variation.
    /// </summary>
    public enum GrenadeLauncherPrimaryMode
    {
        Grenade,
        Native
    }

    /// <summary>
    /// Visual skin used by a grenade projectile. Default follows the shot's normal GL
    /// gameplay profile. Orange/Blue/Green/Red select the four authored projectile textures.
    /// Blue is embedded directly from the source texture because the original runtime bundle
    /// never needed a blue grenade prefab.
    /// </summary>
    public enum GrenadeLauncherProjectileAppearance
    {
        Default,
        Orange,
        Blue,
        Green,
        Red
    }

    public static class GrenadeLauncherIntegration
    {
        private sealed class ExternalRegistration
        {
            internal RocketLauncher Launcher;
            internal GrenadeLauncherPrimaryMode PrimaryMode;
            internal bool ExternalSecondary;
            internal bool HasPalette;
            internal GrenadeLauncherPaintPalette Palette;
            internal bool HasSecondaryCooldownProgress;
            internal float SecondaryCooldownProgress = 1f;
            internal GrenadeLauncherProjectileAppearance PrimaryProjectileAppearance = GrenadeLauncherProjectileAppearance.Default;
            internal bool HasNextProjectileAppearance;
            internal GrenadeLauncherProjectileAppearance NextProjectileAppearance = GrenadeLauncherProjectileAppearance.Default;
        }

        private static readonly Dictionary<int, ExternalRegistration> Registrations =
            new Dictionary<int, ExternalRegistration>();
        private static readonly AccessTools.FieldRef<RocketLauncher, Image> TimerMeter =
            AccessTools.FieldRefAccess<RocketLauncher, Image>("timerMeter");
        private static readonly AccessTools.FieldRef<RocketLauncher, RectTransform> TimerArm =
            AccessTools.FieldRefAccess<RocketLauncher, RectTransform>("timerArm");

        /// <summary>
        /// Makes this exact launcher use Grenade Launcher primary-fire behavior.
        /// When externalSecondary is true, Grenade Launcher suppresses the vanilla Rocket
        /// Launcher secondary and does not run its own blue/green/red alternate mechanics;
        /// the registering mod owns Fire2 instead.
        /// </summary>
        public static void RegisterExternalLauncher(RocketLauncher launcher, bool externalSecondary = true)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;

            registration.PrimaryMode = GrenadeLauncherPrimaryMode.Grenade;
            registration.ExternalSecondary = externalSecondary;
            RefreshRegistration(launcher);
        }

        /// <summary>
        /// Registers an external launcher and gives it a per-instance custom-model palette.
        /// </summary>
        public static void RegisterExternalLauncher(
            RocketLauncher launcher,
            GrenadeLauncherPaintPalette palette,
            bool externalSecondary = true)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;

            registration.PrimaryMode = GrenadeLauncherPrimaryMode.Grenade;
            registration.ExternalSecondary = externalSecondary;
            registration.HasPalette = true;
            registration.Palette = palette;
            RefreshRegistration(launcher);
        }


        /// <summary>
        /// Forces this exact launcher to remain a native Rocket Launcher even when the
        /// player's terminal setting selects Grenade Launcher for the same variation.
        /// This is useful for external arsenals that clone vanilla rocket prefabs for
        /// their own mechanics. externalSecondary may still suppress native Fire2 so the
        /// registering mod can own the secondary while retaining native rocket primary.
        /// </summary>
        public static void RegisterExternalNativeLauncher(RocketLauncher launcher, bool externalSecondary = false)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;

            registration.PrimaryMode = GrenadeLauncherPrimaryMode.Native;
            registration.ExternalSecondary = externalSecondary;
            registration.HasPalette = false;
            RefreshRegistration(launcher);
        }

        public static void SetExternalPrimaryMode(RocketLauncher launcher, GrenadeLauncherPrimaryMode primaryMode)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;

            registration.PrimaryMode = primaryMode;
            RefreshRegistration(launcher);
        }

        public static void SetExternalSecondaryOwnership(RocketLauncher launcher, bool externalSecondary)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;

            registration.ExternalSecondary = externalSecondary;
        }

        public static void SetExternalPalette(RocketLauncher launcher, GrenadeLauncherPaintPalette palette)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;

            registration.HasPalette = true;
            registration.Palette = palette;
            WeaponVisualRuntime.RefreshLauncherPaint(launcher);
        }

        public static void ClearExternalPalette(RocketLauncher launcher)
        {
            ExternalRegistration registration = Find(launcher);
            if (registration == null)
                return;

            registration.HasPalette = false;
            WeaponVisualRuntime.RefreshLauncherPaint(launcher);
        }

        /// <summary>
        /// Changes the texture profile used by ordinary primary grenades from this exact
        /// externally-registered launcher. Default restores profile-driven behavior.
        /// </summary>
        public static void SetExternalPrimaryProjectileAppearance(
            RocketLauncher launcher, GrenadeLauncherProjectileAppearance appearance)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;
            registration.PrimaryProjectileAppearance = appearance;
        }

        /// <summary>
        /// Overrides only the next grenade fired by this exact launcher. This is intended for
        /// external alternate fires that call RocketLauncher.Shoot but want a different grenade
        /// texture without changing the launcher's normal primary appearance.
        /// </summary>
        public static void SetNextExternalProjectileAppearance(
            RocketLauncher launcher, GrenadeLauncherProjectileAppearance appearance)
        {
            ExternalRegistration registration = GetOrCreate(launcher);
            if (registration == null)
                return;
            registration.HasNextProjectileAppearance = true;
            registration.NextProjectileAppearance = appearance;
        }

        /// <summary>
        /// Plays the custom Grenade Launcher viewmodel's authored blue/green alternate-fire
        /// animation for an externally-owned secondary, without firing a GL projectile or
        /// playing one of Grenade Launcher's alternate projectile sounds.
        /// </summary>
        public static void PlayExternalSecondaryAnimation(RocketLauncher launcher)
        {
            if (launcher == null || !UsesExternalSecondary(launcher))
                return;
            WeaponVisualRuntime.NotifyExternalAltFired(launcher);
        }

        /// <summary>
        /// Lets an external secondary drive the Grenade Launcher cooldown dial while preserving
        /// the custom model's calibrated dial mounting/position. progress is 0 = empty/cooling,
        /// 1 = ready.
        /// </summary>
        public static void SetExternalSecondaryCooldownProgress(RocketLauncher launcher, float progress)
        {
            ExternalRegistration registration = Find(launcher);
            if (registration == null || !registration.ExternalSecondary)
                return;
            registration.HasSecondaryCooldownProgress = true;
            registration.SecondaryCooldownProgress = Mathf.Clamp01(progress);
            ApplyExternalSecondaryCooldown(launcher, registration);
        }

        internal static void RefreshExternalSecondaryCooldown(RocketLauncher launcher)
        {
            ExternalRegistration registration = Find(launcher);
            if (registration == null || !registration.ExternalSecondary || !registration.HasSecondaryCooldownProgress)
                return;
            ApplyExternalSecondaryCooldown(launcher, registration);
        }

        private static void ApplyExternalSecondaryCooldown(RocketLauncher launcher, ExternalRegistration registration)
        {
            Image meter = TimerMeter(launcher);
            if (meter == null)
                return;
            float progress = Mathf.Clamp01(registration.SecondaryCooldownProgress);
            meter.fillAmount = progress;
            RectTransform arm = TimerArm(launcher);
            if (arm != null)
                arm.localRotation = Quaternion.Euler(Vector3.forward * (-360f * progress));
            WeaponVisualRuntime.SyncCooldownDial(launcher, meter, arm);
        }

        public static void UnregisterExternalLauncher(RocketLauncher launcher)
        {
            if (ReferenceEquals(launcher, null))
                return;

            int id = launcher.GetInstanceID();
            if (Registrations.TryGetValue(id, out ExternalRegistration registration) &&
                ReferenceEquals(registration.Launcher, launcher))
            {
                Registrations.Remove(id);
                WeaponVisualRuntime.RefreshLauncherPaint(launcher);
            }
        }

        public static bool IsExternalLauncher(RocketLauncher launcher) => Find(launcher) != null;

        internal static bool TryGetExternalGrenadeMode(RocketLauncher launcher, out bool grenadeMode)
        {
            ExternalRegistration registration = Find(launcher);
            if (registration != null)
            {
                grenadeMode = registration.PrimaryMode == GrenadeLauncherPrimaryMode.Grenade;
                return true;
            }

            grenadeMode = false;
            return false;
        }

        internal static bool UsesExternalSecondary(RocketLauncher launcher)
        {
            ExternalRegistration registration = Find(launcher);
            return registration != null && registration.ExternalSecondary;
        }

        internal static bool TryGetExternalPalette(RocketLauncher launcher, out GrenadeLauncherPaintPalette palette)
        {
            ExternalRegistration registration = Find(launcher);
            if (registration != null && registration.HasPalette)
            {
                palette = registration.Palette;
                return true;
            }

            palette = default(GrenadeLauncherPaintPalette);
            return false;
        }

        internal static GrenadeLauncherProjectileAppearance ConsumeProjectileAppearance(
            RocketLauncher launcher, GrenadeProjectileProfile gameplayProfile)
        {
            ExternalRegistration registration = Find(launcher);
            if (registration != null)
            {
                if (registration.HasNextProjectileAppearance)
                {
                    registration.HasNextProjectileAppearance = false;
                    GrenadeLauncherProjectileAppearance next = registration.NextProjectileAppearance;
                    registration.NextProjectileAppearance = GrenadeLauncherProjectileAppearance.Default;
                    if (next != GrenadeLauncherProjectileAppearance.Default)
                        return next;
                }

                if (gameplayProfile == GrenadeProjectileProfile.Primary &&
                    registration.PrimaryProjectileAppearance != GrenadeLauncherProjectileAppearance.Default)
                    return registration.PrimaryProjectileAppearance;
            }

            switch (gameplayProfile)
            {
                case GrenadeProjectileProfile.GreenContact:
                    return GrenadeLauncherProjectileAppearance.Green;
                case GrenadeProjectileProfile.RedBurst:
                    return GrenadeLauncherProjectileAppearance.Red;
                case GrenadeProjectileProfile.BlueDelivery:
                    // Preserve the existing Blue delivery behavior: it intentionally has no
                    // custom grenade mesh/texture because the mechanic is visually represented
                    // by the hook-point delivery path instead.
                    return GrenadeLauncherProjectileAppearance.Default;
                default:
                    return GrenadeLauncherProjectileAppearance.Orange;
            }
        }

        internal static void CopyExternalRegistration(RocketLauncher source, RocketLauncher destination)
        {
            if (source == null || destination == null || ReferenceEquals(source, destination))
                return;

            ExternalRegistration sourceRegistration = Find(source);
            if (sourceRegistration == null)
                return;

            ExternalRegistration destinationRegistration = GetOrCreate(destination);
            if (destinationRegistration == null)
                return;

            destinationRegistration.PrimaryMode = sourceRegistration.PrimaryMode;
            destinationRegistration.ExternalSecondary = sourceRegistration.ExternalSecondary;
            destinationRegistration.HasPalette = sourceRegistration.HasPalette;
            destinationRegistration.Palette = sourceRegistration.Palette;
            destinationRegistration.HasSecondaryCooldownProgress = sourceRegistration.HasSecondaryCooldownProgress;
            destinationRegistration.SecondaryCooldownProgress = sourceRegistration.SecondaryCooldownProgress;
            destinationRegistration.PrimaryProjectileAppearance = sourceRegistration.PrimaryProjectileAppearance;
            destinationRegistration.HasNextProjectileAppearance = false;
            destinationRegistration.NextProjectileAppearance = GrenadeLauncherProjectileAppearance.Default;
            RefreshRegistration(destination);
            if (destinationRegistration.HasSecondaryCooldownProgress)
                ApplyExternalSecondaryCooldown(destination, destinationRegistration);
        }

        private static ExternalRegistration GetOrCreate(RocketLauncher launcher)
        {
            if (launcher == null)
                return null;

            CleanupDeadRegistrations();
            int id = launcher.GetInstanceID();
            if (!Registrations.TryGetValue(id, out ExternalRegistration registration) ||
                !ReferenceEquals(registration.Launcher, launcher))
            {
                registration = new ExternalRegistration
                {
                    Launcher = launcher,
                    PrimaryMode = GrenadeLauncherPrimaryMode.Grenade,
                    ExternalSecondary = true
                };
                Registrations[id] = registration;
            }
            return registration;
        }

        private static ExternalRegistration Find(RocketLauncher launcher)
        {
            if (launcher == null)
                return null;

            int id = launcher.GetInstanceID();
            if (!Registrations.TryGetValue(id, out ExternalRegistration registration) ||
                !ReferenceEquals(registration.Launcher, launcher))
                return null;
            return registration;
        }

        private static void RefreshRegistration(RocketLauncher launcher)
        {
            // Registration can happen after RocketLauncher.OnEnable when a mod creates and
            // annotates a clone in the same frame. Re-run the two registration-time pieces
            // that Grenade Launcher normally performs from its OnEnable postfix.
            RocketCooldownSync.ApplyWhenEquipped(launcher);
            WeaponVisualRuntime.RegisterLauncher(launcher);
            WeaponVisualRuntime.RefreshLauncherPaint(launcher);
        }

        private static void CleanupDeadRegistrations()
        {
            if (Registrations.Count == 0)
                return;

            List<int> dead = null;
            foreach (KeyValuePair<int, ExternalRegistration> pair in Registrations)
            {
                if (pair.Value == null || pair.Value.Launcher != null)
                    continue;
                if (dead == null)
                    dead = new List<int>();
                dead.Add(pair.Key);
            }

            if (dead == null)
                return;
            foreach (int id in dead)
                Registrations.Remove(id);
        }
    }

    internal static class GrenadeLauncherProjectileAppearanceContext
    {
        private static readonly Stack<GrenadeLauncherProjectileAppearance> Appearances =
            new Stack<GrenadeLauncherProjectileAppearance>();

        internal static bool Active => Appearances.Count > 0;
        internal static GrenadeLauncherProjectileAppearance Current =>
            Appearances.Count > 0 ? Appearances.Peek() : GrenadeLauncherProjectileAppearance.Default;

        internal static void Enter(GrenadeLauncherProjectileAppearance appearance)
        {
            Appearances.Push(appearance);
        }

        internal static void Exit()
        {
            if (Appearances.Count > 0)
                Appearances.Pop();
        }

        internal static void Reset()
        {
            Appearances.Clear();
        }
    }

    /// <summary>
    /// ULTRAKILL dual wield creates a brand-new clone of the currently held weapon.
    /// Per-instance integration state lives outside the GameObject, so copy it explicitly
    /// from the original launcher onto that clone after DualWield.UpdateWeapon finishes.
    /// </summary>
    [HarmonyPatch(typeof(DualWield), "UpdateWeapon")]
    internal static class GrenadeLauncherExternalDualWieldPatch
    {
        private static readonly AccessTools.FieldRef<DualWield, GameObject> CopyTarget =
            AccessTools.FieldRefAccess<DualWield, GameObject>("copyTarget");
        private static readonly AccessTools.FieldRef<DualWield, GameObject> CurrentWeapon =
            AccessTools.FieldRefAccess<DualWield, GameObject>("currentWeapon");

        private static void Postfix(DualWield __instance)
        {
            if (__instance == null)
                return;

            GameObject sourceObject = CopyTarget(__instance);
            GameObject duplicateObject = CurrentWeapon(__instance);
            if (sourceObject == null || duplicateObject == null)
                return;

            RocketLauncher source = sourceObject.GetComponent<RocketLauncher>();
            RocketLauncher duplicate = duplicateObject.GetComponent<RocketLauncher>();
            if (source == null || duplicate == null)
                return;

            GrenadeLauncherIntegration.CopyExternalRegistration(source, duplicate);
            // Unity clones the already-created runtime GL child model along with the weapon.
            // Remove that inherited copy after WeaponIdentifier.duplicate has been assigned,
            // leaving only WeaponVisualRuntime's authoritative visual for the duplicate.
            WeaponVisualRuntime.CleanupDualWieldCloneVisual(duplicate);
        }
    }

    /// <summary>
    /// Keeps a custom EB-style secondary from also leaking through to the native Rocket
    /// Launcher Fire2 behavior. Suppression exists only for the duration of that launcher's
    /// own Update call, so an external controller can still read the real Fire2 input in its
    /// own Update.
    /// </summary>
    [HarmonyPatch(typeof(RocketLauncher), "Update")]
    [HarmonyPriority(Priority.First)]
    internal static class GrenadeLauncherExternalSecondaryUpdatePatch
    {
        private struct State
        {
            internal bool Suppressed;
        }

        private static void Prefix(RocketLauncher __instance, out State __state)
        {
            __state = new State();
            if (!GrenadeLauncherIntegration.UsesExternalSecondary(__instance))
                return;

            PlayerInput input = MonoSingleton<InputManager>.Instance?.InputSource;
            if (input == null || input.Fire2 == null)
                return;

            AlternateFireInputContext.Enter(input.Fire2);
            __state.Suppressed = true;
        }

        private static void Postfix(RocketLauncher __instance, State __state)
        {
            if (__state.Suppressed)
                GrenadeLauncherIntegration.RefreshExternalSecondaryCooldown(__instance);
        }

        private static Exception Finalizer(State __state, Exception __exception)
        {
            if (__state.Suppressed)
                AlternateFireInputContext.Exit();
            return __exception;
        }
    }

    [HarmonyPatch(typeof(RocketLauncher), "OnDestroy")]
    internal static class GrenadeLauncherExternalRegistrationCleanupPatch
    {
        private static void Prefix(RocketLauncher __instance)
        {
            GrenadeLauncherIntegration.UnregisterExternalLauncher(__instance);
        }
    }

    /// <summary>
    /// Performance guards for unusually large gel-stuck grenade chain reactions.
    /// Gameplay explosions remain intact; only redundant same-frame feedback/ignition work
    /// is coalesced.
    /// </summary>
    internal static class GrenadeLauncherPerformance
    {
        private const int FullStuckBlastFeedbackPerFrame = 4;
        private const float TerrainIgnitionCellSize = 4f;

        private struct TerrainIgnitionKey : IEquatable<TerrainIgnitionKey>
        {
            internal int Surface;
            internal int X;
            internal int Y;
            internal int Z;

            public bool Equals(TerrainIgnitionKey other) =>
                Surface == other.Surface && X == other.X && Y == other.Y && Z == other.Z;

            public override bool Equals(object obj) => obj is TerrainIgnitionKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 31 + Surface;
                    hash = hash * 31 + X;
                    hash = hash * 31 + Y;
                    hash = hash * 31 + Z;
                    return hash;
                }
            }
        }

        private static readonly Dictionary<TerrainIgnitionKey, int> TerrainIgnitionFrames =
            new Dictionary<TerrainIgnitionKey, int>();
        private static int feedbackFrame = -1;
        private static int fullFeedbackCount;
        private static int lastTerrainCleanupFrame = -1;

        internal static bool AllowFullStuckBlastFeedback()
        {
            int frame = Time.frameCount;
            if (feedbackFrame != frame)
            {
                feedbackFrame = frame;
                fullFeedbackCount = 0;
            }

            fullFeedbackCount++;
            return fullFeedbackCount <= FullStuckBlastFeedbackPerFrame;
        }

        internal static void ReduceExplosionFeedback(GameObject blast)
        {
            if (blast == null)
                return;

            foreach (ParticleSystem particles in blast.GetComponentsInChildren<ParticleSystem>(true))
            {
                if (particles != null)
                    particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            foreach (Renderer renderer in blast.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer != null)
                    renderer.enabled = false;
            }
            foreach (AudioSource audio in blast.GetComponentsInChildren<AudioSource>(true))
            {
                if (audio == null)
                    continue;
                audio.Stop();
                audio.enabled = false;
            }
            foreach (Light light in blast.GetComponentsInChildren<Light>(true))
            {
                if (light != null)
                    light.enabled = false;
            }
        }

        internal static bool TryBeginTerrainIgnition(Collider surface, Vector3 origin)
        {
            if (surface == null)
                return true;

            int frame = Time.frameCount;
            TerrainIgnitionKey key = new TerrainIgnitionKey
            {
                Surface = surface.GetInstanceID(),
                X = Mathf.FloorToInt(origin.x / TerrainIgnitionCellSize),
                Y = Mathf.FloorToInt(origin.y / TerrainIgnitionCellSize),
                Z = Mathf.FloorToInt(origin.z / TerrainIgnitionCellSize)
            };

            if (TerrainIgnitionFrames.TryGetValue(key, out int previousFrame) && previousFrame == frame)
                return false;
            TerrainIgnitionFrames[key] = frame;

            // Keep this tiny bookkeeping dictionary bounded during long Cyber Grind runs.
            if (TerrainIgnitionFrames.Count > 256 && frame - lastTerrainCleanupFrame >= 120)
            {
                lastTerrainCleanupFrame = frame;
                List<TerrainIgnitionKey> stale = null;
                foreach (KeyValuePair<TerrainIgnitionKey, int> pair in TerrainIgnitionFrames)
                {
                    if (pair.Value >= frame - 2)
                        continue;
                    if (stale == null)
                        stale = new List<TerrainIgnitionKey>();
                    stale.Add(pair.Key);
                }
                if (stale != null)
                {
                    foreach (TerrainIgnitionKey oldKey in stale)
                        TerrainIgnitionFrames.Remove(oldKey);
                }
            }

            return true;
        }
    }
}
