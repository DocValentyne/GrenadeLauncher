# Changelog

## 2.0.1

- Fixed the Wiki banner image not displaying on the Thunderstore README.

## 2.0.0

Ignore the rant from the last changelog I found out how to make the weapons good and interesting. Who knew it would be adding tech in a game where 99% of the weapons have tech.

- Added the custom Grenade Launcher viewmodel, authored animations, projectile models, and custom firing sounds. A visual setting can restore vanilla Rocket Launcher visuals and sounds.
- Added Red burst system, and Pink slingshot-point system pulling andconducting enemies, and Knuckleblaster destroying it.
- Added more plugin configurator settings such as color settings for the new model
- Cerberus and Guttertank grenade damage defaults are now 100% (previously 120%).
- Existing configurations migrate only values that exactly matched a previous default; customized values remain unchanged.
- All the other things i forgot about. Many bug fixes im sure.

## 1.2.0

(This rant is now outdated and WRONG!!!)
This mod before this update did 4 damage every 0.6s with the primary fire. This was the same as the shotgun hot-swap dps so i thought it was good. Then i realized you could make the damage on a shotgun turn into 9.75 instead of 3. Hoo boy the purpose of this weapon is begining to be questioned. Im sure some playstyles (shotgunless) would probably still benifit from the weapon so i boosted the damage across the board slightly to help make it more in-line, even though its probably still pretty pointless when you look at everything else you could do. Of course not everyone has your 10,000 hours of experience and game-knoladge.

- Fixed pink blue-hook points occasionally behaving as normal/green hook points when hooked.
- Blue and green alternates can now fire from a fresh right-click during draw-out, while an already-held right-click still waits for the draw animation.
- Added configurable direct-hit base style points, defaulting to 35.
- Rebalanced default grenade damage, airshot damage, surface damage, gel-stuck damage, self damage, blue placement distance, and Gutterman/Guttertank/Hideous Mass damage values.
- Existing configurations migrate only values that exactly matched a released default; customized values stay unchanged.

## 1.1.1

- Fixed generated blue hook points being reused before the player leaves the ground and lands, preventing infinite vertical movement.

## 1.1.0

- Added direct-hit airshot bonuses for primary and green grenades, with configurable damage and explosion size. Parried grenades cannot earn the bonus.
- Blue hook points now arm after a configurable delay, turn pink, and can be detonated by supported player hitscan attacks for a configurable Providence-style explosion.
- Added reset buttons for each Plugin Configurator page and a reset-all-pages button.
- Increased the default stuck-grenade lifetime to 60 seconds and prevented enemy attacks from detonating stuck grenades.
- Fixed mixed Rocket Launcher/Grenade Launcher swap cooldowns, primary fire occurring before the weapon finished drawing, held alternate-fire input, death resets, and no-cooldown cheat behavior.
- Removed the obsolete snappier arc preset and improved dual-wield alternate-fire timing.
- Updated tested defaults, including a 4-second blue cooldown, 3.5 pink explosion damage, 1.2 pink explosion size, 90% Hideous Mass damage, 150 PIPE DREAM style, and 550 MOON SHOT/OUT-SNIPED style.

## 1.0.2

- Corrected the default enemy damage values: Hideous Mass now takes 85% damage, while Malicious Face uses 100%.
- Existing configurations may need these values changed manually after updating.

## 1.0.1

- Prevented Grenade Launcher runs from uploading scores to the public Cyber Grind leaderboard while preserving local Cyber Grind high scores.

## 1.0.0

- First public release.
- Added terminal-selectable alternate Grenade Launchers for all three Rocket Launcher variants.
- Added configurable ballistic primary grenades with direct hits, bouncing, timed explosions, parrying, and Whiplash interaction.
- Added unique green contact grenades, red adhesive gel, and blue deployable hook points.
- Added dual-wield support, enemy-specific damage settings, configurable self-damage, and custom style bonuses.
