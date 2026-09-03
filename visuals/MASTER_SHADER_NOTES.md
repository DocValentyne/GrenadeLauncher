# ULTRAKILL/Master material notes

## Accidental "regal" skin recipe

Recorded during the 2.0.0 custom-model work on 2026-08-21.

The custom grenade-launcher mesh was given ULTRAKILL's weapon rendering model at runtime:

```csharp
Material replacement = new Material(vanillaRocketRenderer.sharedMaterial);
replacement.color = customPieceColor;
```

The resulting model correctly followed ULTRAKILL's lighting, but also displayed a dark,
ornate, highly reflective navy-and-gold appearance. This is a candidate future cosmetic skin.

Important reproduction details:

- Clone `RocketLauncher`'s `RocketLauncherCustom` material at runtime. Its shader is
  `ULTRAKILL/Master`.
- Preserve all of the clone's shader properties; only replace `_Color` with the color for
  the custom mesh piece.
- The custom model uses Roblox OBJ UVs, not the rocket launcher's UV layout.
- Setting only `_MainTex` to `Texture2D.whiteTexture` did **not** remove the appearance.
  Runtime logging identified the likely cause as `_IDTex=T_RocketLauncher_ID`: the Master
  shader uses this ID/mask texture to select painted material zones. The next neutral-material
  test sets both `_MainTex` and `_IDTex` to white.

Do not treat the current white-main-texture experiment as the eventual neutral material
solution. Before turning this into a selectable skin, identify the remaining relevant Master
shader properties and intentionally preserve them for a named cosmetic preset.

## Accidental "chrome-regal" finish

Recorded after neutralizing both `_MainTex` and `_IDTex`.

Keeping the cloned RocketLauncher material's original metal/smoothness values creates an
extremely reflective navy-and-bronze finish. It is much glossier than ULTRAKILL's normal
custom-colour shine and should not be the neutral default, but it is another candidate future
cosmetic preset.

To reproduce it, use the neutral ID-map setup above and **do not** override `_Metallic`,
`_Glossiness`, or `_Gloss` on the cloned `ULTRAKILL/Master` material.

## Accidental "pure chrome" finish

Recorded during the hard reflection A/B test.

With the neutral `_MainTex`/`_IDTex` setup, setting the temporary shine value to `0.00`:

- disables the `REFLECTION` shader keyword;
- sets `REFLECTION`, `_ReflectionStrength`, and `_SpecularStrength` to zero; and
- clears `_CubeTex`.

Counterintuitively, this produces an extremely bright silver/chrome appearance rather than a
matte finish. Preserve this exact state as a future cosmetic candidate. It demonstrates that
the Master shader's feature variants do not behave like simple Unity reflection toggles, so a
neutral default must be derived from a controlled material preset rather than a single slider.

## Vanilla material timing and neutral shine tuning

Runtime comparison showed that ULTRAKILL replaces/configures the Rocket Launcher material
after the launcher is enabled. The relevant reflection values change as follows:

| Property | Creation / soft state | Settled custom-colour state |
| --- | ---: | ---: |
| `_ReflectionStrength` | `1` | `0.25` |
| `_ReflectionFalloff` | `1` | `6` |

The settled material also enables `CUSTOM_COLORS` and replaces `_MainTex`/`_IDTex`, but the
custom grenade-launcher model deliberately uses white neutral textures and its own per-piece
colors.

The current development baseline deliberately clones the creation-time material, which gives a
stable subdued finish. No shine calibration is exposed to players. If this is revisited, use the
two recorded values to build a deliberate material preset rather than adding another live slider.
