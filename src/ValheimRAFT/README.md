# ValheimRAFT v4.3.2 (Valheim 1.0.12 Compatibility & Polish Update)

**ValheimRAFT** allows you to build custom, fully functional movable rafts, ships, and vehicles in Valheim. Expand your vessels with standard building pieces, craft custom sails and steering wheels, drop anchors, navigate open seas, and take flight!

> **Community Fork**: Maintained by **[JNDEV0](https://github.com/JNDEV0)** to update ValheimRAFT for **Valheim 1.0.12**, **[ValheimModding-Jotunn-2.30.0+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/v/2.30.0/)**, and **[denikson-BepInExPack_Valheim-5.4.2350+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)**.

---

## ☕ Support the Project

If you enjoy this update and want to support continued development and maintenance of ValheimRAFT for the community, you can support on Ko-fi:

👉 **[Support JNDEV0 on Ko-fi (https://ko-fi.com/jndev0)](https://ko-fi.com/jndev0)**

---

## ⚠️ Important Community Notice & "As-Is" Disclaimer

- **Community Maintained**: This fork was created to make ValheimRAFT playable again following Valheim's 1.0 release (Unity 6 engine upgrade).
- **Tested Functionality**: Core features have been thoroughly tested and verified: vehicle piece building, floating/sailing, sail propulsion, anchor toggling, flying, ballasting, smooth water landing transitions, steering wheel doodad control, and world saving/loading.
- **Untested & Experimental Features**: ValheimRAFT is a massive and complex codebase containing advanced mechanics (such as complex mechanical swivel contraptions, land vehicle nesting, and advanced toggle mechanism options). Not all extended features or edge-case interactions have been exhaustively tested.
- **Offered "As-Is"**: This release is provided freely and without warranty. Always back up your character and world saves before testing modded structures!
- **Feedback & Issues**: Players and modders are warmly invited to report issues, submit suggestions or Pull Requests, and help maintain this mod at our GitHub repository:  
  👉 **[https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12](https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12)**

---

## 📦 Requirements & Dependencies

To use this mod, ensure you have the following required dependencies installed:
1. **[denikson-BepInExPack_Valheim-5.4.2350](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)** (or newer)
2. **[ValheimModding-Jotunn-2.30.0](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/v/2.30.0/)** (or newer)

---

## 📜 Attribution & Open Source History

- **Original Creator**: **Sarcen** created the original ValheimRAFT mod that defined ship building in Valheim, and generously released it as open source in 2023 under the GPLv3 license.
- **Modern Rewrite & Architecture**: **Zolantris** ([zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)) completely overhauled the mod's architecture, adding modular vehicle systems, convex hull boundary physics, and expansive features.
- **1.0 Compatibility Update**: Because Zolantris has been inactive for several months while Valheim 1.0 broke existing builds, this fork was created by **JNDEV0** to address engine breakages introduced by the Valheim 1.0 / Unity 6 update, ensuring the mod remains accessible and functional for the community.
- **Telemetry Removed**: Unused external telemetry wrappers (such as Sentry tracking) and leftover debug spam hooks have been audited and removed/disabled, ensuring clean, local, offline-friendly execution.

*Licensed under the [GNU General Public License v3.0 (GPLv3)](LICENSE).*

---

## 🛠️ Changelog (v4.3.2)

### Stability & Multi-Zone Loading
- **Fast Travel / Portal Desync & Separated Parts Fixed**: Fixed a critical issue where portaling away from or loading near a ship caused pieces to detach, separate, or throw null references. Added safety timeouts and zone load checks to `Teleport_Patch`, protected vehicle pieces in `WearNTear_Patch` and `VehiclePiecesController` during sector initialization, and disabled violent origin recentering on teleport transitions.
- **FixedJoint & Rigidbody Removal Error Fixed**: Resolved the Unity error `Can't remove Rigidbody because FixedJoint depends on it` in `TargetController` and `VehiclePiecesController` by guaranteeing all attached joint dependencies and connected bodies are cleanly unhooked and destroyed before Rigidbody removal.
- **Multiplayer Session Initialization Guard**: Protected world UID lookups during player join/connect in `ZNet_WorldSession_Patches` before the remote server sends the world profile.
- **Corrected ZDO.Load Harmony Signature**: Updated patch signature for `Zdo_Patch` to match Valheim 1.0.12 / Unity 6 `Version.World` parameter changes.

### Sails, Propulsion & Visuals
- **Dynamic Sail Furling & Unfurling**: Sails now realistically furl and unfurl according to movement state:
  - **Anchored, Stop, or Speed 1 (Rowing)**: Sail is fully retracted/furled.
  - **Speed 2 (Half Sail)**: Sail extends to 50% height.
  - **Speed 3 (Full Sail)**: Sail extends to 100% full sail.
- **Top-Down Sail Extension & Crossbeam Offsets**: Corrected vertical sail scaling pivot so sails extend downward from the crossbeam rather than shrinking toward the deck. Added calibrated vertical offsets (`SailVerticalOffset = 1.05`, `KarveSailVerticalOffset = 2.15`) for perfect alignment with Raft and Karve masts.
- **Minimum Sail Propulsion Enforced**: Set a minimum baseline propulsion speed of `10` (`MinSailSpeed`) so vessels with hulls move comfortably even with a single sail.
- **Storm Wind Velocity Capped**: Lowered default `MaxLinearVelocity` from 100 to 50 m/s to prevent uncontrollable runaway speeds and physics destabilization during severe storms.
- **Disabled Glitchy Sails & Masts from Hammer**: Removed custom square sail, triangle sail, and custom masts 1-3 from the build menu to prevent physics glitches while keeping existing placed prefabs fully functional.

### Usability & Quality of Life
- **Automatic Rope Ladder Deployment**: Rope ladders now automatically extend when the vessel is anchored, hovering, or stationary (`speed <= 0.01`), ensuring swimming players can always climb back aboard without requiring manual anchor drops. Ladders retract cleanly during active flight or navigation.
- **Steering Wheel Anchored Alert**: Attempting to move forward (<kbd>W</kbd>) or reverse (<kbd>S</kbd>) while the vessel is anchored now displays a floating yellow `"RAISE ANCHOR FIRST"` alert directly above the steering wheel helm.
- **Font & Localization Improvements**: Registered all active game fonts (AveriaSerifLibre, Norse) and dynamically generated Arial SDF Unicode fallbacks in `TMPProHelpers` to prevent missing glyph errors. Cleaned up outdated foreign translation bundles and added missing localization keys.
- **Robust Mod Folder Detection**: Rewrote `CustomTextureGroup` path resolution to automatically find mod assets across all mod managers (Thunderstore, r2modman, Gale, Vortex, manual installs).

---

## 🛠️ Changelog (v4.3.1 Summary)
- **Valheim 1.0.12 & Unity 6 Runtime**: Fully recompiled and updated for Unity `v6000.0.75` and Jotunn `2.30.0`.
- **Save & Logout Freeze Fixed**: Fixed black screen hang when returning to main menu caused by `DontDestroyOnLoad` on `Game.instance`.
- **Convex Hull & Collision Spam Silenced**: Suppressed 0-point boundary warnings and disabled per-frame collision contact debug logging.
- **Vanilla Esc Menu Restored**: Single-player pause and smooth camera panning restored when pressing **Esc**.
- **Centered Mechanism UI**: Fixed coordinate math for Mechanism Toggle and Swivel UI menus.
- **Tuned Sail Speeds & Flight Landing**: Balanced sail speed factors and added smooth flight-to-water landing transitions.

---

## 🎮 Controls Quick Reference

| Action | Control (Default) |
| :--- | :--- |
| **Take Helm / Steer** | Press **E** at Steering Wheel |
| **Forward / Increase Speed** | **W** (1 = Slow / Rudder, 2 = Half Sail, 3 = Full Sail) |
| **Reverse / Decrease Speed** | **S** |
| **Turn Rudder** | **A** / **D** |
| **Ascend / Fly Up** | **Spacebar** ("Jump") |
| **Descend / Ballast Down** | **Ctrl** or **C** ("Crouch") |
| **Drop / Raise Anchor** | **Left Shift** ("Run") |
| **Interact / Open Mechanism UI** | Press **E** on Mechanism Toggle Switch |
| **Pause Game (Single Player)** | **Esc** |

---

## 📥 Installation

### Option A: Thunderstore / r2modman / Gale (Recommended)
1. Search for **ValheimRAFT** in your mod manager and click **Install with Mod Manager**.
2. Launch the game through your mod manager.

### Option B: NexusMods / Vortex / Manual Installation
1. If using Vortex, install and enable the zip archive directly.
2. If installing manually, extract `plugins/ValheimRAFT` into your `Valheim/BepInEx/plugins/` directory:
   ```
   Valheim/
   └── BepInEx/
       └── plugins/
           └── ValheimRAFT/
               ├── ValheimRAFT.dll
               ├── ValheimVehicles.dll
               ├── Zolantris.Shared.dll
               ├── ZdoWatcher.dll
               ├── DynamicLocations.dll
               ├── Assets/
               └── docs/
   ```

---

## 🌐 Community & Source Code

- **GitHub Repository**: [https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12](https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12)
- **Upstream Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)
- **Support JNDEV0 on Ko-fi**: [https://ko-fi.com/jndev0](https://ko-fi.com/jndev0)
