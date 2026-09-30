# Grenade Launcher

Detailed weapon documentation is available in the Wiki tab on the Thunderstore mod page.

[![See the Thunderstore Wiki tab for detailed weapon documentation.](https://raw.githubusercontent.com/DocValentyne/GrenadeLauncher/3b87a19/Wiki%20tab%20notice.png)](https://thunderstore.io/c/ultrakill/p/DocValentyne/Grenade_Launcher/wiki/)

Grenade Launcher is a BepInEx 5 mod for ULTRAKILL by DocValentyne. It adds a terminal-selectable alternate form for all three Rocket Launcher variants, with new primary and alternate fires.

The primary fire uses an arcing grenade inspired by the Team Fortress 2 Demoman's stock Grenade Launcher. Its behavior and balance can be extensively customized through Plugin Configurator.

## Installation

Install **Grenade Launcher** through r2modman or Thunderstore Mod Manager. BepInEx and Plugin Configurator are installed automatically as dependencies.

For a manual installation, install BepInEx 5 and Plugin Configurator, then place `GrenadeLauncher.dll` in `ULTRAKILL/BepInEx/plugins/DocValentyne-GrenadeLauncher`.

## Usage

Open ULTRAKILL's weapon terminal and select the alternate form for any Rocket Launcher variant. Standard and alternate Rocket Launchers can be mixed in the same loadout.

Configuration is available under **Options -> Plugin Configurator -> Grenade Launcher**.

## Compatibility API

Grenade Launcher 2.0.2 adds a per-weapon integration API for other mods. External mods can opt one exact `RocketLauncher` instance into Grenade Launcher primary-fire behavior (or force it to stay native), hand secondary-fire ownership back to the external mod, provide a per-instance custom-model paint palette, drive the custom cooldown dial/AltFire animation, and select grenade projectile appearances per-primary or per-shot. This does not change the player's terminal selection for other Rocket Launchers of the same variation.

## Leaderboards

While the mod is loaded, Cyber Grind scores are kept in your local save but are not submitted to the public Steam leaderboard.

## Building

Copy `Directory.Build.props.example` to `Directory.Build.props`, update the two local paths, then run:

```powershell
dotnet build -c Release
```

You can instead pass the `GameDir` and `BepInExDir` MSBuild properties directly. Do not redistribute ULTRAKILL, BepInEx, or Plugin Configurator assemblies with the source.

## License

Grenade Launcher is released under the [MIT License](LICENSE).
