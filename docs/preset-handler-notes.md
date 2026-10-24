# Should Riverheim Config become a Riverheim preset handler?

Findings from reading Riverheim 1.2.1's `Riverheim.dll` (IL via Mono.Cecil; no decompiler was available) while
preparing 3.0.0. Nothing here was verified in-game.

## How Riverheim 1.2.1 chooses, stores and checks a world's settings

- `WorldSettings` is `(Preset, PresetVersion, Features[], Parameters[])`. `ConfigManager.DefaultWorldSettings()`
  returns `("builtin.amur", 2, [], [])`; `PRESET_DEFAULT = "builtin.amur"`, `PRESET_EXPERIMENTAL =
  "builtin.experimental"`, `PRESET_DEFAULT_VERSION = 2` (it was 1 in 1.1.0 and in AtlasBuilder 1.0.0's bundled
  Riverheim).
- A world gets its settings lazily from `WorldExtensions.GetData(World)` (a `ConditionalWeakTable`). For a world
  with nothing loaded from disk this is `GetDefaultSettings(World)`: `builtin.experimental` v0 if the world name
  starts with `RHEXP`, otherwise `DefaultWorldSettings()`. So a new world is always created with the default preset
  at the current default version.
- Saving (`World.SaveWorldMetaData`, transpiled): after the ZPackage constructor Riverheim writes a header
  (`-1`, `"dev.gurebu.riverheim.stable"`), and after the world version it writes the settings through
  `WorldSettingSerialization.Serialize`, which stores only `preset` and `preset.version` and throws
  `NotImplementedException("custom features/parameters are not supported yet")` if `Features` or `Parameters` is
  non-empty.
- Loading (`World.LoadWorld`, transpiled): `ReadHeader` fails (world shown as `[BAD_VERSION]`) if the marker or
  worldgen id is wrong; `ReadWorldSettings` deserialises and then calls `ConfigManager.Validate`, which rejects
  an unknown preset name (`"Unknown preset 'x'"`), a version above the handler's `MaxSupportedVersion()`, and any
  feature or parameter at all. A rejected world is also shown as `[BAD_VERSION]`.
- Joining a server (`ZNet.SendPeerInfo` / `RPC_PeerInfo`, transpiled): the server sends the custom-worldgen flag,
  the worldgen id and the serialised settings; the client validates the same way and `RefuseWorld`s on failure
  (disconnect with an error). Riverheim also exchanges its own version and refuses mismatched major.minor.
- Generation (`WorldGenerator.Pregenerate` prefix): `ConfigManager.GetConfig(world.GetSettings())`. For
  `builtin.amur` that is `GetDefaultConfig(version)`; otherwise the registered `IPresetHandler` gets
  `GetConfig(version, GetDefaultConfig(handler.ParentPresetVersion(version)))`. `GetDefaultConfig(version)` starts
  from `GetMasterConfig()` and layers version 1 (1.1.0: meander amplitude 1.35, barrier margin 0.145, swamp
  roughness/neighbour-height curves) and version 2 (1.1.1: Black Forest weight 261 to 258.5, lowland Black Forest
  2.5) on top. This is how 1.2.0 fixed the "desync caused by preset version updates": the version stored in the
  world picks the config, not the installed Riverheim.
- `RegisterPresetHandler(name, handler)` returns false if the name is taken and throws `NotImplementedException`
  unless `handler.ParentPreset() == "builtin.amur"`. `GetCurrentVersion(name)` returns `MaxSupportedVersion()`.

## What a preset handler would and would not give us

A registered handler, say `lwackahg.riverheimconfig`, with `ParentPreset = builtin.amur`,
`ParentPresetVersion(v) = v` and `MaxSupportedVersion = PRESET_DEFAULT_VERSION`, plus a Harmony postfix on
`WorldExtensions.GetDefaultSettings` returning `(ourName, PRESET_DEFAULT_VERSION)` for newly created worlds:

- Worlds created with the mod would be tagged in their save and on the network. Loading or joining one without
  the mod gives `[BAD_VERSION]` / "Unknown preset" instead of silently generating different terrain. That is the
  behaviour Riverheim itself chooses for its worlds and is safer than today.
- The Riverheim default-version a world was created on is pinned the same way Riverheim pins it, so future
  Riverheim default changes would not touch tagged worlds.
- Worlds **not** tagged (created before the mod, or with it disabled) would generate stock terrain even while the
  mod is installed, which is what players expect.
- **The settings values themselves still could not be stored per world.** `Parameters` are rejected by
  `Validate` and `Serialize`, so the handler would still read the live cfg at generation time. Editing the cfg
  would still change existing tagged worlds, exactly like 2.0.0 with the postfix.
- The only way to carry values in the save today would be to encode them into the preset *name* (the name is
  stored verbatim and is a free string) and register a handler for each such name on the fly from a prefix on
  `ValidatePreset`/`GetConfig`/`GetCurrentVersion`. It would work and would even sync values to clients without
  ServerSync, but it leans on Riverheim never validating preset names, bloats the save header, and would be
  invisible to AtlasBuilder. Not recommended.

## The compatibility problem

Every world generated with Riverheim Config 1.x/2.0.0 is saved as plain `builtin.amur`. A 3.0.0 that only acts
through a preset handler would generate those worlds as stock Riverheim, changing their terrain under existing
players. The mod cannot tell a 2.0.0-configured `builtin.amur` world from a stock one. So any move to a handler
needs a cfg switch that keeps the postfix behaviour for untagged worlds, and it must default to **on** for at least
one release so that upgrading does not alter anyone's world. With that switch defaulting to on, the handler adds
little in practice until players turn it off, which they can only do safely when every one of their worlds was
created with 3.0.0 or later.

## Recommendation

Keep the `ConfigManager.GetConfig` postfix in 3.0.0 (it is proven in-game and AtlasBuilder confirms the mapping),
and do **not** register a handler yet. 3.0.0 already removes the worst sharp edge of 2.0.0: settings left at their
default are no longer written, so Riverheim's own defaults (and its per-version defaults) stay in control unless a
player changes a value.

Revisit when one of these happens:

- Riverheim starts honouring `Parameters` (or `Features`) in `Validate`/`Serialize`; then store every changed
  setting as a parameter, let the handler read them back, and drop ServerSync for world settings. That would give
  true per-world settings with Riverheim's own save and network path.
- The user is comfortable shipping the handler behind a `TagNewWorlds`/`ApplyToUntaggedWorlds` pair of settings,
  defaulting to tagging new worlds **and** still applying the cfg to untagged ones, with a documented path to
  turning the latter off. That needs in-game testing of: new world creation (tag stored), reload, joining a server,
  loading a tagged world without the mod (expect `[BAD_VERSION]`), and loading a 2.0.0 world (expect unchanged).

Useful entry points for the implementation, all public in 1.2.1 unless noted:
`Riverheim.Configuration.ConfigManager.RegisterPresetHandler(string, IPresetHandler)`,
`Riverheim.Configuration.Extensions.IPresetHandler` (`ParentPreset`, `ParentPresetVersion(int)`,
`MaxSupportedVersion`, `GetConfig(int, WorldGenerationConfig)`), `Riverheim.Plugin.WorldExtensions.GetSettings /
SetSettings / IsCustomWorldgen` (public static extension methods), `WorldExtensions.GetDefaultSettings(World)`
(private static; Harmony postfix target), `Riverheim.Configuration.WorldSettings(string preset, int version)`.
