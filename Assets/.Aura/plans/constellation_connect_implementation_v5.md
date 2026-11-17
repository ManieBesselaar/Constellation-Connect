# Constellation Connect — Scene-by-Scene Implementation Plan

## Objective
Build the 4-scene, 5-theme scripture-connection game on the Lifestories framework, targeting WebGL, with Unity Localization wired for `en-WEB` and a prefab-first, reskinnable UI.

## Changes

**Rename fallout (must fix — currently broken)**
- `Assets/_core game Framework/Demo/Assets/Scene_{MainMenu,Gameplay,LoadingScreen}.asset` - (modify) `_sceneName` still reads `2_MainMenu` / `3_Gameplay` / `1_LoadingScreen`; update to the `*_framework_demo` names
- `ProjectSettings/EditorBuildSettings.asset` - (modify) stale paths `0_Bootstrap.unity`…`3_Gameplay.unity`; correct to the renamed files (GUIDs already valid)

**Content pipeline**
- `Assets/_project/Data/JSON/constellation_connect_verses_web.json` - (create) valid JSON from `Bible Data/verse_data.json`: strip the `import json` header and `json.dump` footer, fix `"isBridge": true` on ISA_40_031, add a bridge chunk to MATT_05_016
- `Assets/_project/Scripts/Data/ConstellationSO.cs` - (create) `ConstellationSO` + `[Serializable] VerseDefinition` + `PhraseChunk` (chunkId, order, text, isBridge, bridgeTag)
- `Assets/_project/Scripts/Editor/ConstellationImporter.cs` - (create) menu `Tools/Constellation/Import Verse Data`: JSON → 5 `ConstellationSO` in `Data/Constellations/` + populate the Localization table
- `Assets/_project/Scripts/Editor/SpriteSheetFixer.cs` - (create) rename all 74 sub-sprites across the three sheets to semantic names via a declared index→name table; set ~48px 9-slice borders on the three card panels
- `Assets/_project/Localization/` - (create) `LocalizationSettings`, `en-WEB` Locale, `ConstellationStrings` table via `LocalizationEditorSettings`

**Runtime assembly** (`Constellation.Game.asmdef`, refs: Lifestories.Core/Events/Navigation/Pause/SaveSystem/UI, Unity.Localization, Unity.InputSystem, Unity.TextMeshPro)
- `Scripts/Bootstrap/BootstrapManager.cs` - (create) facade over `PersistentManager` + `BootstrapInitializer`; inits SaveService, locale, AudioManager, then fades to menu
- `Scripts/Audio/AudioManager.cs` - (create) master/BGM/SFX channels, `PlaySfx`, `PlayBgm`, volumes from save
- `Scripts/Save/SaveService.cs`, `Scripts/Save/ConstellationSaveData.cs` - (create) per-theme stars/best score/study duration
- `Assets/_core game Framework/SaveSystem/SaveData.cs` - (modify) add one opaque `_gameplayPayloadJson` string so a game can store its own payload without forking the framework
- `Scripts/Menu/MainMenuViewController.cs`, `ConstellationCardView.cs`, `SettingsModalView.cs` - (create) 5 cards from SOs, 1.05x hover + cyan glow, timer/volume/attribution modal
- `Scripts/Gameplay/GamePhaseStateMachine.cs` - (create) Study → Connect → Victory
- `Scripts/Gameplay/StudyPhaseController.cs`, `Scripts/UI/CountdownBar.cs` - (create) 2x3 grid, 30/60/90/Zen, final-10s pulse, skip bonus
- `Scripts/Gameplay/IDraggableTile.cs`, `IDropTarget.cs`, `PhraseTileView.cs`, `AnchorNodeView.cs` - (create) drag-drop with +85px touch Y-offset
- `Scripts/Gameplay/StarLinkRenderer.cs`, `HintService.cs`, `VictoryViewController.cs` - (create) connectors, hint pulse, gold bloom + summary
- `Scripts/UI/UIImageTween.cs` - (create) scale/alpha/glow tweens
- `Scripts/Editor/SceneBootstrapper.cs` - (create) builds all 4 scenes + build settings

**Prefab library** — `Assets/_project/Prefabs/UI/`: `PFB_UiPillButton`, `PFB_UiThemeCard`, `PFB_UiHexTile`, `PFB_UiAnchorNode`, `PFB_UiBridgeHub`, `PFB_UiProgressBar`, `PFB_UiStarRating`, `PFB_UiModalPanel`, `PFB_UiStatRow`

**Shader & VFX**
- `Assets/_project/Shaders/SHD_CosmicRipple2D.shader` - (create) URP 2D unlit, radial UV distortion from drop coordinates
- `Assets/_project/Prefabs/VFX/PFB_VFX_StardustBurst.prefab`, `PFB_VFX_VictoryBloom.prefab` - (create)

**Audio** — `Assets/_project/Audio/Music/BGM_CosmicMeditation_Loop` + `Audio/SFX/` ×7 (generated)

**Scenes** — `Assets/_project/Scenes/`: `00_Bootstrap`, `01_LoadingScreen`, `02_MainMenu`, `03_Gameplay`

## Relevant Assets and Quirks

- **Framework demo is currently broken by the rename.** `Scene_MainMenu`/`Scene_Gameplay`/`Scene_LoadingScreen` still carry `_sceneName: 2_MainMenu` / `3_Gameplay` / `1_LoadingScreen`, and `EditorBuildSettings` still lists `0_Bootstrap.unity`…`3_Gameplay.unity`. `SceneLoaderManager` resolves by scene name, so this fails at runtime, not at compile.
- **TMP essentials confirmed imported** — `Assets/TextMesh Pro/{Fonts,Resources,Shaders,Sprites}` present.
- **Art inventory is complete — no placeholders required.** Three sliced sheets, **74** sub-sprites total (`spriteMode: 2` / `textureType: 8`), all still generically named. **All pivots are now 0.5/0.5 across all three sheets.**
- **Main sheet** `Constellation UI.png`: **38** sub-sprites (`_17–_21`, `_26`, `_27`, `_31`, `_34`, `_36` removed). Six are 9-sliced: `_14` (26,29,23,23), `_15` (32,28,36,31), `_16` (31,18,24,21), `_22` (28,19,29,19), `_24` (6,7,6,6), `_35` (24,22,24,18).
- **Icons sheet** `Constellation UI icons.png`: 26 sub-sprites. Index → `iconId`: `_7` scroll, `_0` dove, `_1` shield, `_2` anchor, `_3` heart_cross, `_4` crown, `_9` olive_branch, `_21` lamp, `_22` shepherd, `_23` mountain, `_24` star, `_25` open_bible. **Rows 3–4 duplicate rows 1–2** (`_30`–`_38`, `_56`–`_60`) — spares, not new art.
- **Additional sheet** `Constellation UI additional icons.png`: 10 sub-sprites. `_0` star_empty, `_1` star_bronze, `_2` star_silver, `_3` star_gold, `_12` bridge_hub_inactive, `_13` bridge_hub_active, `_14` bridge_hub_completed, `_18` card_panel_study, `_19` card_panel_active, `_20` card_panel_complete.
- **Extra-icon groups have zero 9-slice borders.** Card panels are 288–296 × 180–188 with rounded corners and a glow — set ~48px borders or draw them at native size only.
- **Star rating has 4 states, brief asks for 0–3.** Render three slots (`star_empty` unfilled, `star_gold` filled) and reuse `star_bronze`/`star_silver`/`star_gold` as the single quality badge for 1★/2★/3★.
- **Icons ship one colour variant per glyph** (both duplicates `_active`), plus `icon_scroll_gold` (`icons_26`) and `icon_dove_inactive` (`icons_27`). Drive state by tinting `Image.color` on the cyan/gold medallion frames rather than expecting per-state art.
- **Labels baked into the sheets contain typos** — `icon_author_active` is the anchor, `icon_crosss_active` has three s's, `icon_oliven_active`, `icon_open bible_active` has a space. Map by rect index, never by label text.
- **Hex tiles and medallions have no 9-slice borders** — render at native size in fixed-size `Image`s; diagonals can't 9-slice cleanly.
- **Source JSON is Python, not JSON** — `import json` on line 1, trailing `json.dump(...)`; line 49 has lowercase `"isBridge": true` (invalid as both). `MATT_05_016` has no bridge chunk so it can never link into Wisdom & Guidance.
- **Folder case** — brief says `Assets/_Project/`; the real folder is `Assets/_project/`. Use lowercase. Sprites currently sit loose in `Sprites/`.
- **`SceneLoaderManager` must sit under the `PersistentManager` root** — `Single`-mode loads destroy the bootstrap scene and an unparented manager loses its coroutine.
- **WebGL save** — keep `SaveSystemManager`'s `PlayerPrefs` (IndexedDB-backed) rather than file I/O, so a closing tab can't lose progress.
- **`AudioManager` lives in the game assembly** so the exported `LifestoriesCoreFramework.unitypackage` stays authoritative.
- **Shader is handwritten, not Shader Graph** — a `.shader` is reproducible from code; a `.shadergraph` asset can't be authored reliably blind. Same name and uniforms.
- **One locale only (`en-WEB`)**, so `LocalizedString` bindings stay cheap; the settings asset must exist before any table.
- If any UI element later turns out to have no matching sprite, generate one, suffix it `_PLACEHOLDER`, and log it once — nothing currently needs this.
- `Debug.Log`/`LogWarning`/`LogError` on every phase transition, tile snap/reject, hint, save write and scene load.

## Steps

- [x] 1. Update `_sceneName` in `Scene_MainMenu`/`Scene_Gameplay`/`Scene_LoadingScreen` to the `*_framework_demo` names and correct the stale `EditorBuildSettings` paths [depends: none]
- [x] 2. Move sprites into `Sprites/UI` + `Sprites/Backgrounds`; rename all 74 sub-sprites across the three sheets to semantic names; set ~48px 9-slice borders on the three card panels [depends: 1]
  - Done: 76 slots renamed (`SpriteSheetFixer` now uses `ISpriteEditorDataProvider`, since `TextureImporter.spritesheet` is a no-op in Unity 6.3); pivots and 9-slice borders preserved.
- [x] 3. Create the repaired JSON, `ConstellationSO` + importer; generate 5 theme SOs and the `en-WEB` Localization settings/locale/`ConstellationStrings` table [depends: 2]
- [x] 4. Add `Constellation.Game.asmdef`, `SaveData._gameplayPayloadJson`, `SaveService`, `ConstellationSaveData`, `AudioManager`, `UIImageTween` [depends: 3]
- [x] 5. Generate `BGM_CosmicMeditation_Loop` and the 7 SFX; write `SHD_CosmicRipple2D.shader`; build both VFX prefabs [depends: 4]
  - Partial: `SHD_CosmicRipple2D.shader` and both VFX prefabs are done. Audio generation is still outstanding — every clip request was refused for lack of a paid audio plan, so `AudioManager`'s BGM and SFX slots are empty and the game runs silent.
  - Audio now complete: the user supplied the 8 clips (`Assets/_project/Music`, `Assets/_project/SFX`); `SceneBootstrapper.WireAudio` mounts them on the persistent `AudioManager` (`Tools/Constellation/Wire Audio`) and `AudioManager.Play*` cues are fired from `PhraseTileView`, `StudyPhaseController`, `ConnectPhaseController` and `GamePhaseStateMachine`. Verified in play mode: BGM loops from boot through menu → gameplay, all 7 one-shot cues resolve.
- [x] 6. Build the 9 prefabs in `Prefabs/UI`, then build `00_Bootstrap` and `01_LoadingScreen` from them; set build settings [depends: 4,5]
- [x] 7. Build `02_MainMenu` and `03_Gameplay` from the prefab library — menu cards/settings modal, then Phase 1 study, Phase 2 connect, Phase 3 victory [depends: 6]
  - Done via `Tools/Constellation/Build Game Scenes` (`SceneBootstrapper.cs`). Verified in play mode: bootstrap → menu (5 cards) → settings modal → theme select → 6-card study grid → connect board (6 anchors, 3 hubs, 19 tiles).

## Verify
- `check_compile_errors` clean after each step; `unity_verification_agent` drives the full flow in play mode.
- Framework demo still plays from `0_Bootstrap_framework_demo` after step 1 (guards against the `_sceneName` regression).
- Every sprite reference resolves — no missing `Image.sprite` in either new scene — and a stretched card panel shows no corner distortion.
- Play from `00_Bootstrap`: menu appears with 5 cards at 0 stars, BGM looping.
- Settings: timer options persist, volume sliders drive the mixer, WEB attribution text renders.
- Play Faith & Strength: 60s study grid of 6 cards → early START CONSTELLATION awards +5/sec → 6 anchor nodes and scattered hex tiles appear.
- Wrong drop: ripple distortion fires, tile springs back, no penalty. Correct drop: `SFX_Tile_Snap_Correct`, link draws, +100.
- Bridge tile: extra gold lines connect every verse sharing the keyword, +150. HINT highlights the next phrase, target pulses, −75.
- All 6 verses: everything turns `#FFD670`, victory SFX and bloom fire, summary shows time/accuracy/stars.
- Return to menu: the theme card shows earned stars and best score (save round-trip through `PlayerPrefs`).
