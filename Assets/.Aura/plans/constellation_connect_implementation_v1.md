# Constellation Connect — Scene-by-Scene Implementation Plan

## Objective
Build the 4-scene, 5-theme scripture-connection game on the Lifestories framework, targeting WebGL, with Unity Localization wired for `en-WEB`.

## Changes

**Content pipeline**
- `Assets/_project/Data/JSON/constellation_connect_verses_web.json` - (create) valid JSON extracted from `Bible Data/verse_data.json`; strip the `import json` header and `json.dump` footer, fix `"isBridge": true` on ISA_40_031, give MATT_05_016 a bridge chunk
- `Assets/_project/Scripts/Data/ConstellationSO.cs` - (create) `ConstellationSO` + `[Serializable] VerseDefinition` + `PhraseChunk` (chunkId, order, text, isBridge, bridgeTag)
- `Assets/_project/Scripts/Editor/ConstellationImporter.cs` - (create) menu `Tools/Constellation/Import Verse Data`: JSON → 5 `ConstellationSO` in `Data/Constellations/` + populate Localization table
- `Assets/_project/Scripts/Editor/SpriteSheetFixer.cs` - (create) one-shot: rename `Constellation UI_0..47` to semantic names, set 9-slice borders, reset 6 corrupted pivots

**Runtime assembly** (`Constellation.Game.asmdef`, refs: Lifestories.Core/Events/Navigation/Pause/SaveSystem/UI, Unity.Localization, Unity.InputSystem, Unity.TextMeshPro)
- `Scripts/Bootstrap/BootstrapManager.cs` - (create) facade over `PersistentManager` + `BootstrapInitializer`; inits SaveService, locale, AudioManager, then fades to menu
- `Scripts/Audio/AudioManager.cs` - (create) master/BGM/SFX mixer channels, `PlaySfx`, `PlayBgm`, volumes from save
- `Scripts/Save/SaveService.cs`, `Scripts/Save/ConstellationSaveData.cs` - (create) per-theme stars/best score/study duration; JSON into the framework save
- `Assets/_core game Framework/SaveSystem/SaveData.cs` - (modify) add one opaque `_gameplayPayloadJson` string so games can store their own payload without forking the framework
- `Scripts/Menu/MainMenuViewController.cs`, `ConstellationCardView.cs`, `SettingsModalView.cs` - (create) 5 cards from SOs, 1.05x hover + cyan glow, timer/volume/attribution modal
- `Scripts/Gameplay/GamePhaseStateMachine.cs` - (create) Study → Connect → Victory
- `Scripts/Gameplay/StudyPhaseController.cs`, `Scripts/UI/CountdownBar.cs` - (create) 2x3 study grid, 30/60/90/Zen, final-10s warning pulse, skip bonus
- `Scripts/Gameplay/IDraggableTile.cs`, `IDropTarget.cs`, `PhraseTileView.cs`, `AnchorNodeView.cs` - (create) drag-drop with +85px touch Y-offset
- `Scripts/Gameplay/StarLinkRenderer.cs`, `Scripts/Gameplay/HintService.cs` - (create) starlight connectors, gold hint pulse
- `Scripts/Gameplay/VictoryViewController.cs` - (create) cyan→gold `#FFD670` bloom, summary, save
- `Scripts/UI/UIImageTween.cs` - (create) scale/alpha/glow tweens
- `Scripts/Editor/SceneBootstrapper.cs` - (create) builds all 4 scenes + build settings

**Shader & VFX**
- `Assets/_project/Shaders/SHD_CosmicRipple2D.shader` - (create) URP 2D unlit, radial UV distortion from drop coordinates
- `Assets/_project/Prefabs/VFX/PFB_VFX_StardustBurst.prefab`, `PFB_VFX_VictoryBloom.prefab` - (create) ParticleSystems

**Audio & Localization**
- `Assets/_project/Audio/Music/BGM_CosmicMeditation_Loop` + `Audio/SFX/` ×7 - (create) generated clips
- `Assets/_project/Localization/` - (create) LocalizationSettings, `en-WEB` locale, `ConstellationStrings` table

**Scenes** — `Assets/_project/Scenes/`: `00_Bootstrap`, `01_LoadingScreen`, `02_MainMenu`, `03_Gameplay`

## Relevant Assets and Quirks

- **Sprite sheet is already sliced but unusable as-is.** `Constellation UI.png` has `spriteMode: 2` / `textureType: 8` with 48 sub-sprites named `Constellation UI_0..47` — **none** match the JSON `iconId`s. **Every sprite has `spriteBorder: 0,0,0,0`**, so the hex tiles and pill buttons will distort when stretched. Six sprites (`_37 _39 _41 _42 _44 _45`) have corrupted auto-slice pivots (`alignment: 9`, `x: 1.4806327`) and render offset until reset to 0.5/0.5.
- **TMP Essential Resources are missing** — no `Assets/TextMesh Pro` folder. `com.unity.ugui 2.0.0` ships TMP in Unity 6, so import via `Window/TextMeshPro/Import TMP Essential Resources` **before** any TMP element is authored or it renders blank.
- **Localization is installed but cold** — `com.unity.localization 1.5.14` present, no `LocalizationSettings` asset, no tables.
- **Confirmed:** Input System `1.20.0`, URP `17.3.0` (Shader Graph ships inside URP 17 — no extra package), 2D Sprite/Shapes/Tilemap present.
- **Source JSON is Python, not JSON** — `import json` on line 1, `verse_data = {…}`, trailing `json.dump(...)`. Line 49 has lowercase `"isBridge": true` (invalid as both Python and JSON). `MATT_05_016` has no bridge chunk, so it can never link into Wisdom & Guidance.
- **Folder case** — the brief writes `Assets/_Project/`; the real folder is `Assets/_project/`. Use the existing lowercase one.
- **Scene name collision** — `0_Bootstrap`..`3_Gameplay` already exist in `_core game Framework/Demo/Scenes` from the framework build. Game scenes use `00_`..`03_` in `_project/Scenes/`.
- **`SceneLoaderManager` must sit under the `PersistentManager` root** — `Single`-mode loads destroy the bootstrap scene and an unparented manager loses its coroutine.
- **WebGL save** — framework `SaveSystemManager` uses `PlayerPrefs` (IndexedDB-backed, flushed lazily). Keep that rather than file I/O so a closing tab can't lose progress.
- **`AudioManager` goes in the game assembly, not the framework** — the exported `LifestoriesCoreFramework.unitypackage` stays authoritative and untouched.
- **Shader is handwritten, not Shader Graph** — a `.shader` is reproducible from code; a `.shadergraph` asset cannot be authored reliably blind. Same `SHD_CosmicRipple2D` name and uniforms.
- Runtime `Debug.Log`/`LogWarning`/`LogError` on every phase transition, tile snap/reject, hint, save write and scene load.

## Steps

- [ ] 1. Import TMP essentials; write and run `SpriteSheetFixer.cs` to name the 48 sub-sprites to the `iconId` scheme, set 9-slice borders, reset the 6 bad pivots [depends: none]
- [ ] 2. Create `constellation_connect_verses_web.json` (repaired), `ConstellationSO` + importer; generate 5 theme SOs and the `en-WEB` Localization table with `THEME_*`, `*_REF`, `*_FULL`, `*_C1..C4`, `TRANSLATION_ATTRIBUTION` [depends: 1]
- [ ] 3. Add `Constellation.Game.asmdef`, `SaveData._gameplayPayloadJson`, `SaveService`, `ConstellationSaveData`, `AudioManager`, `UIImageTween` [depends: 2]
- [ ] 4. Generate `BGM_CosmicMeditation_Loop` and the 7 SFX; write `SHD_CosmicRipple2D.shader`; build both VFX prefabs [depends: 3]
- [ ] 5. Build `00_Bootstrap` (PersistentManager + SceneLoaderManager + PauseManager + AudioManager + fade) and `01_LoadingScreen`; set build settings [depends: 3,4]
- [ ] 6. Build `02_MainMenu`: nebula bg, TMP title, 5 SO-driven cards with hover juice, `SettingsModalView` (30/60/90/Zen, volumes, WEB attribution), BGM loop [depends: 5]
- [ ] 7. Build `03_Gameplay`: `GamePhaseStateMachine`; Phase 1 study grid + timer + skip bonus; Phase 2 hex tiles, anchor nodes, bridge hubs, drag-drop, ripple reject, hints; Phase 3 gold bloom + summary + save [depends: 6]

## Verify
- Compile clean via `check_compile_errors` after each step; `unity_verification_agent` drives the full flow in play mode.
- Play from `00_Bootstrap`: menu appears with 5 cards showing 0 stars, BGM playing.
- Open Settings: timer options persist, volume sliders affect the mixer, attribution text reads the WEB notice.
- Play Faith & Strength: 60s study grid of 6 cards → START CONSTELLATION early awards +5/sec → 6 anchor nodes and scattered hex tiles appear.
- Drop a tile wrong: ripple distortion fires, tile springs back, no points lost. Drop right: `SFX_Tile_Snap_Correct`, star link draws, +100.
- Snap a bridge tile: extra gold lines connect every verse sharing that keyword, +150.
- HINT highlights the next phrase and the target node pulses, −75.
- Complete all 6 verses: everything turns `#FFD670`, victory SFX and bloom fire, summary shows time/accuracy/stars.
- Return to menu: the theme card shows the earned stars and best score (save round-trip through `PlayerPrefs`).
