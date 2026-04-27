## GlooMeClasses v1.2.4 Changelog

a compatibility update for Vintage Story 1.22 has been released! thank you for your patience while we worked through all the API changes this update brought :>

---

### Updated
**Vintage Story 1.22 Compatibility**
- updated game dependency from `1.21.5` → `1.22.0`
- updated .NET target framework from `net8.0` → `net10.0` to match VS 1.22's runtime

---

### Fixed

**MetalBarrel Debug Spam**
- removed excessive server debug logging from `BlockEntityMetalBarrel`'s save/load methods
    - `ToTreeAttributes` and `FromTreeAttributes` are called on every autosave tick, causing log spam at a rate of ~2 messages/second per barrel
    - these log calls have been removed entirely as they produced no actionable information

**Crafting System (VS 1.22 API Changes)**
- `CrockCraftingPatch`: VS 1.22 removed `BlockCrock`'s override of `MatchesForCrafting` — it now inherits directly from `CollectibleObject`
    - patch now targets `CollectibleObject.MatchesForCrafting` with an instance guard instead of `BlockCrock`
    - updated parameter types: `GridRecipe` → `IRecipeBase`, `CraftingRecipeIngredient` → `IRecipeIngredient`
- `ToolkitPatches`: Harmony patch for `OnCreatedByCrafting` was failing due to a parameter name change
    - VS 1.22 renamed `allInputslots` → `allInputSlots` (capital S) on `CollectibleObject`
    - updated parameter type from `GridRecipe` → `IRecipeBase` to match new signature
- `GridRecipe` API renames applied across codebase:
    - `resolvedIngredients` → `ResolvedIngredients`
    - `ResolvedItemstack` → `ResolvedItemStack`

**Sound API (VS 1.22 API Changes)**
- `BlockSounds.Place` (and similar sound properties) now returns `SoundAttributes` instead of `AssetLocation`
    - `BlockAdvancedBloomery`: updated `PlaySoundAt` call to use `Sounds?.Place.Location`
    - `BlockEntityMetalBarrel`: `OpenSound` and `CloseSound` on the barrel GUI dialog now wrapped in `new SoundAttributes(...)`

**Interface Changes (VS 1.22 API Changes)**
- `BlockRefurbishedCrock.OnCreatedByCrafting`: updated override signature from `GridRecipe` → `IRecipeBase`
- `BlockEntityGassifier`: `ITemperatureSensitive.CoolNow` now requires a second `OnStackToCool` callback parameter
- `BlockMetalBarrel`: updated behavior `OnBlockBroken` call to pass `dropQuantityMultiplier` through (resolves deprecation warning)

**Assets**
- `merchantsbackpack` recipe: ingredient `game:lantern-up` → `game:lantern-*-up`
    - VS 1.22 added a `size` variant to lanterns (small/large), making the bare `lantern-up` code unresolvable
- `merchantsbackpack` shape: fixed missing texture warnings
    - `game:item/tool/material/handle` → `game:item/tool/handle` (VS 1.22 removed the `material/` subdirectory)
    - `game:item/tool/material/iron` → `game:block/metal/ingot/iron`
- `show-wallpaper-in-handbook.json`: changed JSON patch `op` from `replace` to `add`
    - VS 1.22's wallpaper blocktype no longer has a `handbook` key, causing the replace to fail silently
