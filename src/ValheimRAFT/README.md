# ValheimRAFT v4.3.1 (Valheim 1.0 Compatibility Update)

**ValheimRAFT** allows you to build custom, fully functional movable rafts, ships, and vehicles in Valheim. Expand your boat with standard building pieces, craft custom sails and steering wheels, drop anchors, navigate open seas, and even take flight!

This release is an **unofficial community update** restoring full compatibility with **Valheim 1.0.12** (Unity 6 runtime) and **Jotunn 2.30.0+**.

---

## ⛵ Attribution & Heritage

- **Original Mod Creator**: **Sarcen** (created the legendary ValheimRAFT mod and graciously open-sourced it in 2023 under GPLv3).
- **Vehicle Rewrite & Modern Architecture**: **Zolantris** ([ValheimMods GitHub](https://github.com/zolantris/ValheimMods)), who completely redesigned the vehicle physics, modular systems, and monorepo.
- **Valheim 1.0 Compatibility & Maintenance**: Maintained and updated for the Valheim 1.0 community to resolve engine breakage, crash bugs, physics hangs, and console spam.

*This project is licensed under the [GNU General Public License v3.0 (GPLv3)](LICENSE).*

---

## 🛠️ What's New in v4.3.1

### Engine & Framework Upgrades
- **Valheim 1.0.12 & Unity 6**: Recompiled against the new Unity 6 engine runtime (`v6000.0.75`).
- **Jotunn 2.30.0+**: Migrated from outdated Jotunn references (2.27 / 2.20) to Jotunn 2.30.0 for seamless piece table and prefab registration.

### Bug Fixes & Stability
- **Save & Logout Black Screen / Freeze Fixed**: Resolved an issue where `SingletonBehaviour` marked Valheim's core `Game.instance` GameObject as `DontDestroyOnLoad`. This caused duplicate popups and `ArgumentException` crashes when returning to the main menu, requiring Alt+F4. Scene unloading now executes smoothly.
- **Steering Wheel Attachment Fixed**: Fixed the Unity console error `Can't remove Rigidbody because FixedJoint depends on it` triggered whenever players grabbed the helm or reloaded the world.
- **Silenced Convex Hull Warnings**: Normal ships without custom boundary markers no longer spam `Not enough boundary points to generate boundary mesh: 0`.
- **Collision Debug Console Spam Silenced**: Fixed per-frame contact logging during `OnCollisionStay`. Added an `EnableCollisionDebugLogging` config toggle (disabled by default) under `[Vehicle Physics: Floatation]` and corrected debug log levels so they don't flood the console as Info messages.
- **Restored Vanilla Esc Menu Pause**: Disabled intrusive background pause suppression patches. In single player, pressing **Esc** now pauses the game simulation and performs the vanilla camera panning as intended.
- **Centered Mechanism UI**: Fixed coordinate math that previously trapped the Mechanism Toggle action selector and Swivel UI menus in the bottom-left corner of the screen.

### Physics & Controls Polish
- **Tuned Sail Speeds**: Scaled down excessive tailwind propulsion:
  - **Speed 2 (Half Sail)**: Scaled to **25%** force.
  - **Speed 3 (Full Sail)**: Scaled to **50%** force.
  - Both multipliers are configurable in `zolantris.ValheimRAFT.cfg` (`SpeedHalfSailFactor` and `SpeedFullSailFactor`).
- **Smooth Flight-to-Water Landing**: When descending from flight (holding Ctrl/C), physical contact with the water surface is automatically detected. The vehicle immediately exits flight mode, resets elevation targets, unlocks rotation constraints, and transitions seamlessly back to natural water buoyancy and wave floating.

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

## 📦 Requirements

1. **[BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)** (Version 5.4.2300 or newer)
2. **[Jotunn - the Valheim Mod Tool](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/)** (Version 2.30.0 or newer)

---

## 📥 Installation

### Option A: Thunderstore / r2modman / Gale (Recommended)
1. Install via your mod manager by searching for **ValheimRAFT** or clicking **Install with Mod Manager**.
2. If installing manually from the Thunderstore zip, extract all contents into your Valheim folder or profile `BepInEx/plugins/ValheimRAFT/`.

### Option B: NexusMods / Vortex / Manual Installation
1. Download the release archive.
2. If using Vortex, drag and drop the `.zip` directly into Vortex.
3. If installing manually, extract the `plugins/ValheimRAFT` folder into your game's `Valheim/BepInEx/plugins/` directory:
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

Configuration is located at `BepInEx/config/zolantris.ValheimRAFT.cfg` after launching the game once with the mod installed. You can adjust:
- **`AllowFlight`**: Enable or disable flight controls.
- **`SpeedHalfSailFactor`**: Multiplier for Speed 2 sail force (Default: `0.25`).
- **`SpeedFullSailFactor`**: Multiplier for Speed 3 sail force (Default: `0.50`).
- **`EnableCollisionDebugLogging`**: Toggle verbose console logging for collision points (Default: `false`).
- **`Vehicles Prevent Pausing`**: Keep `false` to preserve vanilla single-player Esc pause.

---

## 📜 Credits & Links

- **Sarcen**: Original author of ValheimRAFT.
- **Zolantris**: Author of the ValheimVehicles rewrite ([GitHub Repository](https://github.com/zolantris/ValheimMods)).
- **Jotunn Team**: Valheim Modding Library.
