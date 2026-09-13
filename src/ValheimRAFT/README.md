# ValheimRAFT v4.3.1 (Valheim 1.0.12 Compatibility Update)

**ValheimRAFT** allows you to build custom, fully functional movable rafts, ships, and vehicles in Valheim. Expand your vessels with standard building pieces, craft custom sails and steering wheels, drop anchors, navigate open seas, and take flight!

This release is an **unofficial community update** restoring full compatibility with **Valheim 1.0.12** (Unity 6 runtime) and **Jotunn 2.30.0+**.

---

## ☕ Support the Project

If you enjoy this update and want to support continued development and maintenance of ValheimRAFT for the community, you can support on Ko-fi:

[![Support on Ko-fi](https://az743702.vo.msecnd.net/cdn/kofi3.png?v=0)](https://ko-fi.com/jndev0)  
👉 **[Support JNDEV on Ko-fi (https://ko-fi.com/jndev0)](https://ko-fi.com/jndev0)**

---

## ⚠️ Important Community Notice & "As-Is" Disclaimer

- **Community Maintained**: This fork was created to make ValheimRAFT playable again following Valheim's 1.0 release (Unity 6 engine upgrade).
- **Tested Functionality**: Core features have been thoroughly tested and verified: vehicle piece building, floating/sailing, sail propulsion, anchor toggling, flying, ballasting, smooth water landing transitions, steering wheel doodad control, and world saving/loading.
- **Untested & Experimental Features**: ValheimRAFT is a massive and complex codebase containing advanced mechanics (such as complex mechanical swivel contraptions, land vehicle nesting, and advanced toggle mechanism options). Not all extended features or edge-case interactions have been exhaustively tested.
- **Offered "As-Is"**: This release is provided freely and without warranty. Always back up your character and world saves before testing modded structures!
- **Community Contributions Welcome**: Community developers and modders are warmly invited to report issues, submit Pull Requests, and help maintain this mod at our GitHub repository:  
  👉 **[https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12](https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12)**

---

## 📜 Attribution & Open Source History

- **Original Creator**: **Sarcen** created the original ValheimRAFT mod that defined ship building in Valheim, and generously released it as open source in 2023 under the GPLv3 license.
- **Modern Rewrite & Architecture**: **Zolantris** ([zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)) completely overhauled the mod's architecture, adding modular vehicle systems, convex hull boundary physics, and expansive features.
- **1.0 Compatibility Update**: Because Zolantris has been inactive for several months while Valheim 1.0 broke existing builds, this fork was created by **JNDEV0** to address engine breakages introduced by the Valheim 1.0 / Unity 6 update, ensuring the mod remains accessible and functional for the community.
- **Telemetry Removed**: Unused external telemetry wrappers (such as Sentry tracking) and leftover debug spam hooks have been audited and removed/disabled, ensuring clean, local, offline-friendly execution.

*Licensed under the [GNU General Public License v3.0 (GPLv3)](LICENSE).*

---

## 📦 Requirements

To use this mod, ensure you have the following installed:
1. **[BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)** (Package: `denikson-BepInExPack_Valheim-5.4.2350` or newer)
2. **[Jotunn - the Valheim Mod Tool](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)** (Package: `ValheimModding-Jotunn-2.30.0` or newer)

---

## 🛠️ Changelog (v4.3.1)

### Engine & Platform Upgrades
- **Valheim 1.0.12 & Unity 6 Runtime**: Fully recompiled and updated for Unity `v6000.0.75` and Jotunn `2.30.0`.
- **Cleaned Telemetry**: Audited codebase to remove unused external telemetry and tracking wrappers.

### Stability & Bug Fixes
- **Save & Logout Freeze Fixed**: Fixed a critical hang where `SingletonBehaviour` marked Valheim's core `Game.instance` GameObject as `DontDestroyOnLoad`. Returning to the main menu no longer hangs on a black screen or crashes with `UnifiedPopup` / `ArgumentException` errors.
- **Steering Wheel FixedJoint Error Fixed**: Fixed the Unity error `Can't remove Rigidbody because FixedJoint depends on it` when grabbing helm controls or reloading vehicles.
- **Silenced Convex Hull Boundary Warnings**: Eliminated the recurring `Not enough boundary points to generate boundary mesh: 0` warning for normal ships.
- **Collision Debug Console Spam Silenced**: Disabled the intensive per-frame contact logging loop during `OnCollisionStay`. Added a dedicated `EnableCollisionDebugLogging` configuration toggle (default `false`) under `[Vehicle Physics: Floatation]` and corrected log levels so debug logs never spam the console as Info messages.
- **Restored Vanilla Esc Menu Pause & Camera Pan**: Disabled background pause suppression patches. Single-player games pause normally and the camera pans smoothly when pressing **Esc**.
- **Centered Mechanism UI**: Fixed coordinate math that clamped the Mechanism Toggle action selector and Swivel UI menus to the bottom-left corner of the screen.

### Controls & Physics Polish
- **Tuned Sail Propulsion Speeds**: Scaled down excessive tailwind speeds for controllable navigation:
  - **Speed 2 (Half Sail)**: Scaled to **25%** force (configurable via `SpeedHalfSailFactor`).
  - **Speed 3 (Full Sail)**: Scaled to **50%** force (configurable via `SpeedFullSailFactor`).
- **Smooth Flight-to-Water Landing**: Descending from flight (holding Ctrl/C) now automatically detects water contact, immediately exits flight mode, resets height offsets, frees rotation constraints, and transitions smoothly into natural water floating and wave bobbing.

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

## ⚙️ Configuration

Configuration is located at `BepInEx/config/zolantris.ValheimRAFT.cfg` after launching the game once:
- **`AllowFlight`**: Toggle flight capability (`true`/`false`).
- **`SpeedHalfSailFactor`**: Speed 2 sail multiplier (Default: `0.25`).
- **`SpeedFullSailFactor`**: Speed 3 sail multiplier (Default: `0.50`).
- **`EnableCollisionDebugLogging`**: Toggle verbose contact debug logs (Default: `false`).
- **`Vehicles Prevent Pausing`**: Set `false` to allow single-player pause on Esc.

---

## 🤝 Community & Source Code

- **GitHub Repository**: [https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12](https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12)
- **Upstream Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)
- **Support JNDEV on Ko-fi**: [https://ko-fi.com/jndev0](https://ko-fi.com/jndev0)
