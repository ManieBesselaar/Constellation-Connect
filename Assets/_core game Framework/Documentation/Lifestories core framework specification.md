# Lifestories Core Unity Framework Specification

When scaffolding or writing code for any new Unity project, strictly adhere to the following modular, decoupled architecture.

## 1. Folder Structure & Assembly Definitions (`.asmdef`)
Isolate systems into strict namespace-matched folders under `Assets/_Project/`. Dependencies must flow **strictly downward** (never reference `Assembly-CSharp` or higher-tier assemblies to prevent cyclic dependencies).

- **`Assets/_Project/Events/`** (`Lifestories.Events.asmdef`)
  - **References:** None.
  - **Role:** Pure ScriptableObject broadcast channels.
- **`Assets/_Project/Core/`** (`Lifestories.Core.asmdef`)
  - **References:** `Lifestories.Events`, `Lifestories.Navigation`, `Lifestories.SaveSystem`.
  - **Role:** Persistent session management (`PersistentManager`) and startup orchestration (`BootstrapInitializer`).
- **`Assets/_Project/Navigation/`** (`Lifestories.Navigation.asmdef`)
  - **References:** `Lifestories.Events`.
  - **Role:** Asset-driven asynchronous scene loading (`GameSceneSO`, `SceneLoadEventChannelSO`, `SceneLoaderManager`).
- **`Assets/_Project/SaveSystem/`** (`Lifestories.SaveSystem.asmdef`)
  - **References:** None.
  - **Role:** Cross-platform JSON state persistence (`SaveData`, `ISaveable`, `SaveSystemManager`).
- **`Assets/_Project/UI/`** (`Lifestories.UI.asmdef`)
  - **References:** `Lifestories.Core`, `Lifestories.Events`, `Lifestories.Navigation`, `Lifestories.SaveSystem`, `Unity.TextMeshPro`, `Unity.Localization`.
  - **Role:** Menus, HUDs, and Options panels.

## 2. Core System Patterns

### A. Decoupled Event Channels (`Lifestories.Events`)
- Never use direct cross-system `GetComponent` or singleton calls for gameplay/UI communication.
- Use abstract generic `EventChannelSO<T>` (with `RaiseEvent(T)`, `Subscribe(Action<T>)`, `Unsubscribe(Action<T>)`) and `VoidEventChannelSO`.
- Create concrete typed channels (e.g., `FloatEventChannelSO`, `Vector3EventChannelSO`, `StringEventChannelSO`, or domain-specific channels like `BeaconEventChannelSO`) so publishers pass targeted payloads without global side effects.

### B. Persistent Bootstrap & Scene Flow (`Lifestories.Core` & `Lifestories.Navigation`)
- **Build Index 0 (`0_Bootstrap`):** Camera-less, light-less scene containing a `DontDestroyOnLoad` `PersistentManager` GameObject, `SceneLoaderManager`, and `BootstrapInitializer`.
- **Startup Flow:** `BootstrapInitializer` loads `SaveSystemManager.Load()` on `Start()`, then raises a `SceneLoadEventChannelSO` targeting `1_MainMenu` (`GameSceneSO`).
- **Async Loading:** `SceneLoaderManager` handles `SceneManager.LoadSceneAsync`, clamps `progress / 0.9f` onto a `FloatEventChannelSO`, and fires a `VoidEventChannelSO` upon activation.

### C. Cross-Platform Save System (`Lifestories.SaveSystem`)
- `SaveData`: `[System.Serializable]` class storing settings (`SelectedLanguageCode`, `MasterVolume`, `MusicVolume`, `DialogueVolume`, `SfxVolume`) and progression.
- `SaveSystemManager`: Static class serializing `SaveData` via `JsonUtility.ToJson` into `PlayerPrefs` (ensuring WebGL `localStorage` and Mobile compatibility).
- `ISaveable`: Interface (`PopulateSaveData(SaveData)` and `LoadSaveData(SaveData)`) implemented by stateful components like `OptionsMenuController`.

### D. Audio & Localization Standards (`Lifestories.UI`)
- **Audio Mixers:** Sliders operate on a `0.0001f` to `1.0f` range and convert to mixer decibels logarithmically: `Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1.0f)) * 20f`.
- **Localization:** `OptionsMenuController` binds a `TMP_Dropdown` asynchronously to `LocalizationSettings.AvailableLocales` and persists the selected locale code to `SaveData`.

## 3. C# Coding Conventions
- Wrap all scripts in explicit `Lifestories.<Module>` namespaces.
- Place `using` directives inside the namespace block.
- Enforce `PascalCase` for public properties/methods/classes, `_camelCase` for private/serialized fields, and `camelCase` for private helper methods.
- Include XML `/// <summary>` documentation on all public classes, properties, and methods.
- Every field marked with `[SerializeField]` must also carry a `[Tooltip("...")]` attribute describing what that variable is used for.