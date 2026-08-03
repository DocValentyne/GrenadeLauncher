# Grenade Launcher

Detailed weapon documentation is available in the Wiki tab on the Thunderstore mod page.

Grenade Launcher is a BepInEx 5 mod for ULTRAKILL by DocValentyne. It adds a terminal-selectable alternate form for all three Rocket Launcher variants, with new primary and alternate fires.

The primary fire uses an arcing grenade inspired by the Team Fortress 2 Demoman's stock Grenade Launcher. Its behavior and balance can be extensively customized through Plugin Configurator.

## Installation

Install **Grenade Launcher** through r2modman or Thunderstore Mod Manager. BepInEx and Plugin Configurator are installed automatically as dependencies.

For a manual installation, install BepInEx 5 and Plugin Configurator, then place `GrenadeLauncher.dll` in `ULTRAKILL/BepInEx/plugins/DocValentyne-GrenadeLauncher`.

## Usage

Open ULTRAKILL's weapon terminal and select the alternate form for any Rocket Launcher variant. Standard and alternate Rocket Launchers can be mixed in the same loadout.

Configuration is available under **Options -> Plugin Configurator -> Grenade Launcher**.

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
