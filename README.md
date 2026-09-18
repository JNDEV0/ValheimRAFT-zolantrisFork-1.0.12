# ValheimRAFT v4.3.5 (Valheim 1.0.12 Compatibility & Polish Update)

**ValheimRAFT** build custom movable rafts ships and movable bases in Valheim. Expand your vessels with standard building pieces, drop anchors, navigate the open seas, and take flight!

> Update ValheimRAFT for **Valheim 1.0.12**, **[ValheimModding-Jotunn-2.30.0+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/v/2.30.0/)**, **[denikson-BepInExPack_Valheim-5.4.2350+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)**, and **[ValheimModding-JsonDotNET-13.0.4+](https://thunderstore.io/c/valheim/p/ValheimModding/JsonDotNET/)**.

---

## ☕ Support

enjoy, and you can donate at Ko-fi:

☕ **[Support JNDEV0 on Ko-fi (https://ko-fi.com/jndev0)](https://ko-fi.com/jndev0)**

---

## ⚠️ Important "As-Is" Disclaimer

- This fork was created to make ValheimRAFT playable again following Valheim's 1.0 release (Unity 6 engine upgrade).
- Core features have been tested mainly for single-player: vehicle piece building, floating/sailing, sail propulsion, anchor toggling, flying, ballasting, smooth water landing transitions, steering wheel doodad control, and world saving/loading.
- ValheimRAFT is a massive and complex codebase containing advanced mechanics. Not all extended features or edge-case interactions have been exhaustively tested. 
- **Offered "As-Is"**: Provided freely and without warranty. Always back up your character and world saves before testing! Players and modders are warmly invited to report issues, bugs, submit suggestions or Pull Requests, and help maintain this mod at the GitHub repository: 
  👉 **[https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12](https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12)**

---

## 📜 Attribution & Open Source History

- **Original Creator**: **Sarcen** created the original ValheimRAFT mod that defined ship building in Valheim, and generously released it as open source in 2023 under the GPLv3 license.
- **Modern Rewrite & Architecture**: **Zolantris** ([zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)) completely overhauled the mod's architecture, adding modular vehicle systems, convex hull boundary physics, and expansive features.

---

## 📥 Installation

### Option A: Thunderstore / r2modman / Gale (Recommended)
1. Install via your mod manager of choice.
2. Dependencies are automatically resolved and installed.

### Option B: NexusMods / Vortex / Manual Installation
1. If using Vortex, install and enable the zip archive directly.
2. If installing manually, extract `ValheimRAFT` into your `Valheim/BepInEx/plugins/` directory:
   ```
   Valheim/
   └── BepInEx/
       └── plugins/
           ├── ValheimRAFT/
           │   ├── ValheimRAFT.dll
           │   ├── ValheimVehicles.dll
           │   ├── Zolantris.Shared.dll
           │   ├── ZdoWatcher.dll
           │   ├── DynamicLocations.dll
           │   ├── ServerSync.dll
           │   └── Assets/
           │       └── Translations/
           │           └── English/
           │               └── valheimraft.json
           └── Newtonsoft.Json.dll (from ValheimModding-JsonDotNET)
   ```

---

## 📦 Requirements & Dependencies

To use this mod, ensure you have the following required dependencies installed:
1. **[denikson-BepInExPack_Valheim-5.4.2350+](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/)**
2. **[ValheimModding-Jotunn-2.30.0+](https://thunderstore.io/c/valheim/p/ValheimModding/Jotunn/v/2.30.0/)**
3. **[ValheimModding-JsonDotNET-13.0.4+](https://thunderstore.io/c/valheim/p/ValheimModding/JsonDotNET/)**

---

## 📜 Changelog (v4.3.5)

### 📯 Horn of Loki (Boat Recall, Helm Attunement & Emergency Teleport)
- **Craftable Emergency Tool**: Added the **Horn of Loki** (`$item_vessel_horn`), hand-crafted for 2 Wood directly from inventory without requiring a workbench (named after Loki, the mythical Norse ship builder of Naglfar).
- **Kept on Death**: If you have the Horn of Loki in your inventory when you die, you will retain it upon respawn, allowing you to immediately channel and teleport back to your ship to recover or grab backup gear!
- **Steering Wheel Attunement (`[Right-Click]`)**: Aim directly at your boat's Vehicle Wheel within interaction range (≤ 3.5 m) and hold right-click for 3 seconds to attune the horn to your vessel. Displays an on-screen alert (`"Must bind at Vehicle Wheel"`) if aiming anywhere else.
- **Direct Deck Teleport (`[Left-Click]`)**: Hold left-click for 3 seconds to channel an emergency teleport directly to the steering wheel on deck from anywhere in the world (even while carrying metals and regardless of carry weight). Displays `"Bind to a boat first"` if no ship is owned or bound.
- **Blowhorn Emote**: The character raises the horn and plays the `"blowhorn"` emote during the 3-second channel, cleanly and instantly cancelable if you move or release the mouse button.
- **Silent Cancellation & Sprint Fix**: Channeling cancels cleanly and silently when releasing early or moving, without on-screen message spam. Resolved the sprinting input conflict (Shift+W) where `AltPlace` triggered the action bar while running.
- **Diagnostic Logging Toggle**: Horn and teleport console messages are now routed through the mechanism's **"Enable Loop Logging"** toggle, keeping the console completely clean by default.

### ⚓ Physics & Flight Stability
- **Anchored Kinematic Lock**:
  - When a vehicle is anchored (or auto-anchored when left unattended with no players aboard), the vehicle Rigidbody is now locked to kinematic (`m_body.isKinematic = true`) and all velocities are zeroed.
  - Completely prevents airborne vehicles from drifting, sinking, or plummeting into the void (`y < -5000m`) when players step off or travel beyond render distance.
  - Dynamically restores full physics simulation seamlessly upon raising anchor.
- **Relaxed Flight Water-Exit Threshold**:
  - Relaxed the automatic flight-to-water transition trigger from `waterLvl + 0.1f` to `waterLvl - 0.5f` (requiring ~0.5m hull penetration into water).
  - Prevents ocean wave crests and swells from prematurely kicking airborne vehicles out of flight mode while skimming low over the ocean.
- **Fixed Flight Hold Force**:
  - Fixed an issue where stepping off an airborne vehicle erroneously bypassed the vertical altitude-holding force.

### ☸️ Controls & Steering Wheel
- **Steering Wheel Auto-Binding**:
  - Added self-healing vehicle resolution to `SteeringWheelComponent`: automatically detects and links to the vehicle manager through hierarchy, root objects, ZDO parent IDs, or proximity (<15m), binds `InitializeControls()`, and registers the wheel with the vehicle.
- **NullReferenceException & Interaction Fix**:
  - Resolved the `NullReferenceException` spam in `SteeringWheelComponent.GetHoverText()` and `Hud.UpdateCrosshair`.
  - Fixed an issue where newly placed or loaded steering wheels had no hover text and could not be grabbed or steered.

### ⛵ Sails & Construction
- **Contracted Sail Scale 0 Fix (Infinite Material Duplication Bug)**:
  - Fixed an issue where contracted sails at speed 0 / Stop / Slow / Back scaled to `0f`, collapsing `BoxCollider` to zero volume and triggering `BoxCollider does not support negative scale or size` warnings, `Missing prefab hash: -1` errors, and failed deconstruction.
  - Enforced a minimum Y-scale of `0.01f` across all sail prefabs at rest.
  - Deconstructing sails at speed 0 now drops materials once and cleanly destroys the piece without errors or item duplication.
- **Configurable Sail Propulsion Multiplier**:
  - Added `PropulsionConfig.SailPropulsionMultiplier` (default `1.0f`, range `0.05` to `2.0`) to allow server admins or players to scale sailing speed without affecting rowing or reverse.

### 🌊 Immersion & Flight/Ballast Tuning
- **Wave Surface Hull Tilt (Opt-In)**:
  - Added natural wave tilting (`PhysicsConfig.waterSurfaceTiltFactor`, default `0.0f` = disabled, range `0.0`–`1.0`, max tilt up to `45°`).
  - When enabled, smoothly aligns the hull's pitch and roll to the ocean wave surface sampled during buoyancy, eliminating the rigid "tabletop" look at sea.
  - Zero additional raycasts (uses existing floatation points) and automatically disabled during flight mode.
- **Ballast / Submarine Ascent & Descent Speed**:
  - Tuned default `BallastClimbingOffset` to `0.4f` for smooth, realistic underwater diving and surfacing.
  - Maintained `FlightClimbingOffset` at `2.0f` for responsive air maneuvering.

### 🚀 Performance, Sync & Multiplayer
- **Boat Loop Rate-Limiting**:
  - Client-side piece sync (`AllClientsSync()`) is now throttled to run at most once every 2.0 seconds rather than running continuously every tick.
  - Isolated vehicle pieces from unrelated land base structures: `EnsurePiecesForVehicle()` strictly targets pieces with valid boat offsets (`MBPositionHash`) or active raft pieces, preventing massive lag spikes near large coastal settlements.
- **Server Piece-Sync Wait**:
  - Tuned default `ServerRaftUpdateZoneInterval` to `3.0s` (down from `5.0s`), providing a balanced default for multiplayer servers without a hardcoded floor.
- **Segmented Frame Slice Budget (≤ 2 ms)**:
  - Reduced piece synchronization slice times from 10ms down to 2ms per frame in `Server_SyncAllVehiclePiecesToVehiclePosition`, ensuring the background sync yields before it can ever cause a visible frame drop or micro-stutter.
- **Movement Threshold Guard**:
  - Throttled piece ZDO updates when the vessel is anchored, docked, or moving less than 2.5 meters. Idle and stationary vessels consume near-zero background CPU.
- **Diagnostic Loop Logging & Mechanism Switch Panel**:
  - Added lightweight `LoopTracker` performance telemetry.
  - Added a **"Diagnostics & Sync"** section to the Mechanism Switch UI:
    * **"Loop Log" toggle**: Toggles `[LoopPerf]` diagnostic reporting in the console on/off.
    * **"Fast MP Sync" toggle**: Allows server admins and ship owners to toggle tight 1.0s multiplayer piece syncing on the fly (off by default).
- **Teleport Clean-Up**:
  - Removed top-left HUD message popups during portal teleportation to vehicle, keeping piece breakdown details in the BepInEx log.

---

## 🚀 Changelog (v4.3.4)

### Steering Wheel & Helm Controls
- **Refined Hover Interaction Text**:
  - Overhauled steering wheel tooltip to clearly state:
    - `[Shift] Toggle Anchor`
    - `[Jump] and [crouch] adjust elevation/depth`
    - `[jump]+[crouch] toggle float/flight`
  - Removed outdated divider lines, obsolete tutorial hints, mass displays, and debug prompts for a clean, immersive interface.
- **Treadmill Animation & Weapon Holstering Fix**:
  - Fixed the character running in place on a "treadmill" while mounting the steering wheel if entering it while moving.
  - Automatically holsters equipped weapons and shields upon taking the helm so the player stands/crouches cleanly with both hands on the wheel.

### Rope Ladder Ergonomics
- **Instant Auto-Climb by Default**:
  - Defaulted rope ladder movement to automatic fast climb without requiring manual toggling.
  - Cleaned up hover interaction text by removing unnecessary mode toggle prompts.

### Collision Damping & Shoreline Physics
- **Rock Impact Damping & Anti-Stutter**:
  - Fixed the boat acting like an unstoppable power-drill bulldozing shoreline rocks and freezing the game with collision log flooding.
  - Added dynamic collision impulse damping: forward velocity is smoothly diminished exponentially upon hitting rocks and a gentle reverse rebound velocity is applied.

### Save-Cleanup Protection & Teleport Piece Sync
- **Save-Cleanup Poisoning Shielded**:
  - Shielded `ZDO.Reset` and `RemoveZDO` hooks from being wiped out by Valheim Ashlands+ save cleanup cycles (`SaveCleanup`).
  - Active world ZDOs are preserved in memory during world saves, preventing all ship pieces from disappearing from `m_allPieces` and `_zdoGuidLookup`.
- **Vanilla Piece Loading on Boat Teleport Fixed**:
  - Fixed an issue where beds, chests, crafting tables, walls, and vehicle portals remained invisible when teleporting to a ship in float mode.
  - Added universal sector migration for all piece types in `ZDOMan.instance.m_objectsBySector` and portal objects in `m_portalObjects`.
  - Fixed bed map pin desyncing to stale chunk locations by updating bed world positions and sectors even when the bed GameObject is unloaded.
  - Added self-healing piece scanner (`EnsurePiecesForVehicle`) to guarantee piece recovery from `ZDOMan` under any condition.
- **Pre-Teleport Piece Sync & Piece Breakdown Logging**:
  - Synchronizes all piece coordinates and sectors prior to portal arrival.
  - Categorizes pieces and logs counts to Unity console and on-screen HUD (top left):
    `[BoatPortal] Destination Vehicle ID {id} at {pos}: Found {total} pieces ({raftCount} ValheimRAFT, {vanillaCount} Vanilla, {modCount} OdinArchitect/Modded).`
  - Ensures all child GameObjects are fully instantiated before the teleport loading screen completes (`[BoatPortal] Completed teleport to Vehicle {id}. Loaded pieces: {loaded}/{total}`).

### Clean Packaging & Mod Hygiene
- **Decoupled Newtonsoft.Json**:
  - Removed bundled `Newtonsoft.Json.dll` from the mod distribution in compliance with modding guidelines; added official dependency on `ValheimModding-JsonDotNET-13.0.4`.
- **Trimmed Obsolete Assets & Docs**:
  - Removed legacy `docs/` folder and disabled custom sail texture directories (`Assets/Logos`, `Assets/Patterns`, `Assets/Sails`).
- **Cleaned Issue Templates**:
  - Removed obsolete references to `YggdrasilTerrain` from GitHub issue templates.

---

## 🚀 Changelog (v4.3.3)

### Portal & Fast Travel Overhaul
- **Boat-to-Land Portal Routing Fixed**: Resolved a critical issue where walking through a portal on a boat would teleport the player to the center of the boat instead of the connected land portal.
  - Fixed piece placement logic in `Player_Patch` that was accidentally parenting shore/land portals to nearby vehicles (`MBParentId`).
  - Added terrain and placement distance validation to ensure ground and land structures never attach to vehicles.
  - Cleared raycast piece cache (`PlayerLastRayPiece`) on placement to prevent stale vehicle references from leaking into subsequent builds.
  - Fixed `VehiclePiecesController.ForceUpdateAllPiecePositions` from overwriting boat and land portal coordinates to the vehicle root.
  - Optimized `Teleport_Patch.DebouncedTeleportCoordinateUpdater` to immediately complete for land destination portals without unnecessary 10-second timeout delays or fallback drag-backs.
  - Added an automatic healer in `VehiclePiecesController.ActivatePiece` that detects existing misplaced land portals and unbinds them from vehicles back to true world coordinates.

### Bed Spawn Point & Ship Ownership
- **Respawn on Moving Ships**: Fixed bed spawn points on ships so players respawn cleanly on their bed on the vehicle instead of falling into the ocean or defaulting to the world spawn point.
- **Vehicle Ownership Persistence**: Resolved vehicle ownership issues during world load and player login so vehicles remain interactive and responsive across sessions.

### Flight Modes & Propulsion
- **Dynamic Flight Modes**: Added distinct flight dynamics:
  - **Airship / Hover Mode**: Controlled vertical lift with neutral buoyancy.
  - **Airplane Mode**: Aerodynamic pitch, roll, and forward momentum.
  - **Float / Water Mode**: Standard aquatic hull buoyancy.
- **Universal Anchor Toggle (Shift Key)**: Shift toggles the vehicle anchor in all movement and flight modes, regardless of whether a physical anchor piece is attached.

### Map & Navigation
- **Dynamic Minimap Icon**: Vehicle map icon now switches dynamically between ship, airplane, and vehicle indicators based on the current active flight/water mode.
- **Vehicle Renaming Support**: Added default naming and renaming support to prevent vehicles from being stuck as `V:Unnamed`.

---

## 🚀 Changelog (v4.3.2)

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

## 🚀 Changelog (v4.3.1 Summary)
- **Valheim 1.0.12 & Unity 6 Runtime**: Fully recompiled and updated for Unity `v6000.0.75` and Jotunn `2.30.0`.
- **Save & Logout Freeze Fixed**: Fixed black screen hang when returning to main menu caused by `DontDestroyOnLoad` on `Game.instance`.
- **Convex Hull & Collision Spam Silenced**: Suppressed 0-point boundary warnings and disabled per-frame collision contact debug logging.
- **Vanilla Esc Menu Restored**: Single-player pause and smooth camera panning restored when pressing **Esc**.
- **Centered Mechanism UI**: Fixed coordinate math for Mechanism Toggle and Swivel UI menus.
- **Tuned Sail Speeds & Flight Landing**: Balanced sail speed factors and added smooth flight-to-water landing transitions.

---

## 💬 Community & Source Code

- **GitHub Repository**: [https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12](https://github.com/JNDEV0/ValheimRAFT-zolantrisFork-1.0.12)
- **Upstream Repository**: [https://github.com/zolantris/ValheimMods](https://github.com/zolantris/ValheimMods)
- **Original Mod**: [ValheimRAFT by Sarcen](https://www.nexusmods.com/valheim/mods/1136)
- **Support JNDEV0 on Ko-fi**: [https://ko-fi.com/jndev0](https://ko-fi.com/jndev0)
