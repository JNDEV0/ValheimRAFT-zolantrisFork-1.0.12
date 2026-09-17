using ValheimVehicles.Controllers;
using ValheimVehicles.Shared.Constants;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Components;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using HarmonyLib;
using Jotunn;
using UnityEngine;
using Logger = UnityEngine.Logger;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public class Teleport_Patch
{
  public static Dictionary<Player, ZDOID> m_teleportTarget = new();

  public static void TeleportToObject(Player __instance, Vector3 pos,
    Quaternion rot,
    ZDOID objectId)
  {
    if (__instance.TeleportTo(pos, rot, true))
      m_teleportTarget[__instance] = objectId;
  }

  public static void SyncVehiclePiecesBeforeTeleport(int parentId, Vector3 vehiclePos, Quaternion vehicleRot)
  {
    var pieces = VehiclePiecesController.EnsurePiecesForVehicle(parentId);
    int raftCount = 0;
    int vanillaCount = 0;
    int modCount = 0;

    foreach (var zdo in pieces)
    {
      if (zdo == null || !zdo.IsValid()) continue;
      var prefab = zdo.GetPrefab();
      var prefabGo = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab(prefab) : null;
      var prefabName = prefabGo != null ? prefabGo.name : "";

      if (prefabName.StartsWith("MB_") || prefabName.StartsWith("MBRaft") ||
          prefabName.StartsWith("ShipHull") || prefabName.StartsWith("Sail") ||
          prefabName.StartsWith("Mast") || prefabName.StartsWith("Rudder") ||
          prefabName.StartsWith("Rope") || prefabName.StartsWith("Swivel") ||
          prefabName.StartsWith("VehiclePiece") || prefabName.StartsWith("animated_") ||
          prefabName.StartsWith("WaterVehicle"))
      {
        raftCount++;
      }
      else if (prefabName.StartsWith("OA_") || prefabName.StartsWith("Odin") ||
               prefabName.StartsWith("VF_") || prefabName.StartsWith("Clutter_"))
      {
        modCount++;
      }
      else
      {
        vanillaCount++;
      }

      // Synchronize ZDO position and sector so ZNetScene loads them in the target sector
      var nv = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(zdo) : null;
      if (nv == null)
      {
        var localPos = zdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        var localRot = Quaternion.Euler(zdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));
        var pieceWorldPos = vehiclePos + vehicleRot * localPos;
        var pieceWorldRot = vehicleRot * localRot;

        zdo.SetPosition(pieceWorldPos);
        zdo.SetRotation(pieceWorldRot);

        if (ZDOMan.instance != null)
        {
          VehiclePiecesController.MigratePortalSectorInZdoMan(zdo, pieceWorldPos);
        }
      }
    }

    var total = pieces.Count;
    var msg = $"[BoatPortal] Destination Vehicle ID {parentId} at {vehiclePos:F1}: Found {total} pieces ({raftCount} ValheimRAFT, {vanillaCount} Vanilla, {modCount} OdinArchitect/Modded).";
    Jotunn.Logger.LogInfo(msg);
    if (MessageHud.instance != null)
    {
      MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, msg);
    }
  }

  public static void TeleportToActivePosition(TeleportWorld __instance,
    ZDOID playerId)
  {
    var connId = __instance.m_nview.m_zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType.Portal);
    if (connId.IsNone()) return;

    var zDO = ZDOMan.instance.GetZDO(connId);
    if (zDO == null)
    {
      ZDOMan.instance.RequestZDO(connId);
      return;
    }

    var parentId = zDO.GetInt(VehicleZdoVars.MBParentId, 0);
    var nv = ZNetScene.instance.FindInstance(zDO);
    var position = nv ? nv.transform.position : zDO.GetPosition();
    var rotation = nv ? nv.transform.rotation : zDO.GetRotation();

    // If destination portal is on a vehicle, check debug force anchor failsafe
    if (parentId != 0)
    {
      var vehicleZdo = ZDOMan.instance.GetZDO(new ZDOID(zDO.m_uid.UserID, (uint)parentId))
                    ?? ZDOMan.instance.GetZDO(new ZDOID(1, (uint)parentId));
      if (vehicleZdo != null)
      {
        var vNv = ZNetScene.instance ? ZNetScene.instance.FindInstance(vehicleZdo) : null;
        var vmc = vNv ? vNv.GetComponentInChildren<VehicleMovementController>() : null;
        var shouldForceAnchor = vehicleZdo.GetBool(VehicleZdoVars.ForceAnchorOnPortalTeleport, false);
        if (!shouldForceAnchor && vmc != null)
        {
          shouldForceAnchor = vmc.ShouldForceAnchorOnPortalTeleport();
        }

        if (shouldForceAnchor)
        {
          if (vmc != null)
          {
            vmc.TriggerForceAnchorTeleportAlert();
          }
          else
          {
            vehicleZdo.Set(VehicleZdoVars.VehicleAnchorState, (int)AnchorState.Anchored);
            vehicleZdo.Set(ZDOVars.s_forward, 0);
          }
        }

        if (nv == null)
        {
          var vPos = vehicleZdo.GetPosition();
          var vRot = vehicleZdo.GetRotation();
          var localPos = zDO.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
          var localRot = Quaternion.Euler(zDO.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));
          position = vPos + vRot * localPos;
          rotation = vRot * localRot;
        }

        SyncVehiclePiecesBeforeTeleport(parentId, vehicleZdo.GetPosition(), vehicleZdo.GetRotation());
      }
      else
      {
        SyncVehiclePiecesBeforeTeleport(parentId, position, rotation);
      }
    }

    var vector = rotation * Vector3.forward;
    var exitDistance = Mathf.Max(__instance.m_exitDistance, 1.6f);
    var pos = position + vector * exitDistance + Vector3.up * 0.2f;
    var playerGo = ZNetScene.instance.FindInstance(playerId);
    if (!(bool)playerGo) return;

    var player = playerGo.GetComponent<Player>();
    if (!(bool)player) return;

    // very important, without this the player gets deactivated
    if (player.transform.parent != null) player.transform.SetParent(null);

    TeleportToObject(player, pos, rotation, zDO.m_uid);
  }

  [HarmonyPatch(typeof(TeleportWorld), nameof(TeleportWorld.Teleport))]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> TeleportWorld_Teleport(
    IEnumerable<CodeInstruction> instructions)
  {
    var found = false;
    List<CodeInstruction> list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
      if (list[i].Calls(AccessTools.Method(typeof(Character), "TeleportTo")))
      {
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Teleport_Patch),
            nameof(Player_TeleportTo)));
        list.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
        found = true;
        break;
      }

    if (!found) Jotunn.Logger.LogWarning("TeleportWorld patch failed.");

    return list;
  }

  public static bool Player_TeleportTo(Player player, Vector3 pos,
    Quaternion rot,
    bool distantTeleport, TeleportWorld __instance)
  {
    TeleportToActivePosition(__instance,
      ((Character)player).m_nview.m_zdo.m_uid);
    return true;
  }

  [HarmonyPatch(typeof(Player), "UpdateTeleport")]
  [HarmonyPrefix]
  public static bool Player_UpdateTeleport_Prefix(Player __instance, float dt)
  {
    if (!__instance.m_teleporting)
    {
      return true;
    }

    if (!m_teleportTarget.TryGetValue(__instance, out var targetZdoid))
    {
      return true;
    }

    var targetZdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(targetZdoid) : null;
    var parentId = targetZdo != null ? targetZdo.GetInt(VehicleZdoVars.MBParentId, 0) : 0;
    if (parentId == 0)
    {
      return true;
    }

    // Specific handler for vehicle portals to prevent infinite hang on FindFloor or distant zone loading
    __instance.m_teleportCooldown = 0f;
    __instance.m_teleportTimer += dt;
    if (__instance.m_teleportTimer <= 2f)
    {
      return false;
    }

    var targetPos = GetTeleportTargetPos(__instance);
    var targetRot = __instance.m_teleportTargetRot;
    var nv = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(targetZdoid) : null;
    if (nv != null)
    {
      targetRot = nv.transform.rotation;
    }
    else if (targetZdo != null)
    {
      var vehicleZdo = ZDOMan.instance.GetZDO(new ZDOID(targetZdo.m_uid.UserID, (uint)parentId))
                    ?? ZDOMan.instance.GetZDO(new ZDOID(1, (uint)parentId));
      if (vehicleZdo != null)
      {
        var vRot = vehicleZdo.GetRotation();
        var localRot = Quaternion.Euler(targetZdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));
        targetRot = vRot * localRot;

        var vNv = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(vehicleZdo) : null;
        var vmc = vNv ? vNv.GetComponentInChildren<VehicleMovementController>() : null;
        var shouldForceAnchor = vehicleZdo.GetBool(VehicleZdoVars.ForceAnchorOnPortalTeleport, false);
        if (!shouldForceAnchor && vmc != null)
        {
          shouldForceAnchor = vmc.ShouldForceAnchorOnPortalTeleport();
        }

        if (shouldForceAnchor && vmc != null && !vmc.isAnchored)
        {
          vmc.TriggerForceAnchorTeleportAlert();
        }
      }
    }

    var dir = targetRot * Vector3.forward;
    __instance.transform.position = targetPos;
    __instance.transform.rotation = targetRot;
    if (__instance.m_body != null)
    {
      __instance.m_body.linearVelocity = Vector3.zero;
    }
    __instance.m_maxAirAltitude = targetPos.y;
    if (EnvMan.instance != null)
    {
      EnvMan.instance.ForceInstantEnvironmentSwitch();
    }
    __instance.SetLookDir(dir);

    var zone = ZoneSystem.GetZone(targetPos);
    if (ZoneSystem.instance != null && !ZoneSystem.instance.IsZoneLoaded(zone))
    {
      ZoneSystem.instance.PokeLocalZone(zone);
      return false;
    }

    VehicleManager? vm = null;
    if (VehicleManager.VehicleInstances != null)
    {
      VehicleManager.VehicleInstances.TryGetValue(parentId, out vm);
    }

    var isAreaReady = ZNetScene.instance != null && ZNetScene.instance.IsAreaReady(targetPos);
    var piecesController = vm != null && vm.Instance != null ? vm.Instance.PiecesController : null;
    var isVehicleReady = piecesController != null && piecesController.isActiveAndEnabled;
    var isPortalReady = nv != null;

    var targetPieces = VehiclePiecesController.EnsurePiecesForVehicle(parentId);
    if (ZNetScene.instance != null)
    {
      foreach (var pZdo in targetPieces)
      {
        if (pZdo != null && pZdo.IsValid() && ZNetScene.instance.FindInstance(pZdo) == null)
        {
          try
          {
            ZNetScene.instance.CreateObject(pZdo);
          }
          catch (System.Exception ex)
          {
            Jotunn.Logger.LogWarning($"[BoatPortal] Failed to create object for piece {pZdo.m_uid}: {ex.Message}");
          }
        }
      }
    }

    int loadedPieceCount = piecesController != null ? piecesController.m_pieces.Count : 0;
    int targetPieceCount = targetPieces.Count;
    bool piecesReady = (targetPieceCount == 0) || (loadedPieceCount >= targetPieceCount) || (__instance.m_teleportTimer > 6f);

    // Do not abort prematurely at 4 seconds. Wait for area, vehicle/portal, and pieces, with 10s fallback timeout.
    var canComplete = (isAreaReady && (isVehicleReady || isPortalReady) && piecesReady) || __instance.m_teleportTimer > 10f;
    if (!canComplete)
    {
      return false;
    }

    var completionMsg = $"[BoatPortal] Completed teleport to Vehicle {parentId}. Loaded pieces: {loadedPieceCount}/{targetPieceCount}";
    Jotunn.Logger.LogInfo(completionMsg);
    if (MessageHud.instance != null)
    {
      MessageHud.instance.ShowMessage(MessageHud.MessageType.TopLeft, completionMsg);
    }

    // Floor placement: check FindFloor, fallback to targetPos on deck if missed
    if (ZoneSystem.instance != null && ZoneSystem.instance.FindFloor(targetPos, out var floorHeight))
    {
      __instance.transform.position = new Vector3(targetPos.x, Mathf.Max(targetPos.y, floorHeight), targetPos.z);
    }
    else
    {
      __instance.transform.position = targetPos;
    }

    __instance.m_teleportTimer = 0f;
    __instance.m_teleporting = false;
    __instance.ResetCloth();
    m_teleportTarget.Remove(__instance);

    // Guaranteed Onboarding: add player to local ship
    if (vm != null && vm.Instance != null && vm.Instance.OnboardController != null)
    {
      vm.Instance.OnboardController.TryAddPlayerIfMissing(__instance);
    }
    else if (nv != null)
    {
      var controller = nv.GetComponentInParent<VehiclePiecesController>();
      if (controller != null && controller.Manager != null && controller.Manager.OnboardController != null)
      {
        controller.Manager.OnboardController.TryAddPlayerIfMissing(__instance);
      }
    }
    else if (VehicleManager.VehicleInstances != null && VehicleManager.VehicleInstances.TryGetValue(parentId, out var fallbackVm) && fallbackVm.Instance != null && fallbackVm.Instance.OnboardController != null)
    {
      fallbackVm.Instance.OnboardController.TryAddPlayerIfMissing(__instance);
    }

    return false;
  }

  [HarmonyPatch(typeof(Player), "UpdateTeleport")]
  [HarmonyTranspiler]
  public static IEnumerable<CodeInstruction> Player_UpdateTeleport(
    IEnumerable<CodeInstruction> instructions)
  {
    var list = instructions.ToList();
    for (var i = 0; i < list.Count; i++)
    {
      if (list[i]
          .LoadsField(AccessTools.Field(typeof(Player), "m_teleportTargetPos")))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Teleport_Patch),
            nameof(GetTeleportTargetPos)));

      if (list[i]
          .StoresField(AccessTools.Field(typeof(Player), "m_teleporting")))
        list[i] = new CodeInstruction(OpCodes.Call,
          AccessTools.Method(typeof(Teleport_Patch), nameof(SetIsTeleporting)));
    }

    return list;
  }

  private static Vector3 GetTeleportTargetPos(Player __instance)
  {
    if (!m_teleportTarget.TryGetValue(__instance, out var zdoid))
      return __instance.m_teleportTargetPos;

    var go = ZNetScene.instance != null ? ZNetScene.instance.FindInstance(zdoid) : null;
    if (go != null) return GetTeleportPosition(go.gameObject);

    var targetZdo = ZDOMan.instance != null ? ZDOMan.instance.GetZDO(zdoid) : null;
    if (targetZdo != null)
    {
      var parentId = targetZdo.GetInt(VehicleZdoVars.MBParentId, 0);
      if (parentId != 0)
      {
        VehicleManager? vm = null;
        if (VehicleManager.VehicleInstances != null)
        {
          VehicleManager.VehicleInstances.TryGetValue(parentId, out vm);
        }

        var vehicleZdo = ZDOMan.instance.GetZDO(new ZDOID(targetZdo.m_uid.UserID, (uint)parentId))
                      ?? ZDOMan.instance.GetZDO(new ZDOID(1, (uint)parentId));

        if (vm != null && vm.Instance != null && vm.Instance.PiecesController != null)
        {
          var vPos = vm.transform.position;
          var vRot = vm.transform.rotation;
          var localPos = targetZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
          var localRot = Quaternion.Euler(targetZdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));
          var portalPos = vPos + vRot * localPos;
          var portalRot = vRot * localRot;
          return portalPos + portalRot * Vector3.forward * 1.6f + Vector3.up * 0.2f;
        }
        else if (vehicleZdo != null)
        {
          var vPos = vehicleZdo.GetPosition();
          var vRot = vehicleZdo.GetRotation();
          var localPos = targetZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
          var localRot = Quaternion.Euler(targetZdo.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));
          var portalPos = vPos + vRot * localPos;
          var portalRot = vRot * localRot;
          return portalPos + portalRot * Vector3.forward * 1.6f + Vector3.up * 0.2f;
        }
      }
    }

    return __instance.m_teleportTargetPos;
  }

  private static Vector3 GetTeleportPosition(GameObject go)
  {
    var tp = go.GetComponent<TeleportWorld>();

    if ((bool)tp)
      return tp.transform.position + tp.transform.forward * 1.6f + Vector3.up * 0.2f;

    return go.transform.position + go.transform.forward * 1.6f + Vector3.up * 0.2f;
  }

  private static IEnumerator DebouncedTeleportCoordinateUpdater(
    Player __instance,
    bool isTeleporting, ZDOID zdoid)
  {
    var zdo = ZDOMan.instance.GetZDO(zdoid);
    if (zdo == null)
    {
      __instance.m_teleporting = false;
      m_teleportTarget.Remove(__instance);
      yield break;
    }

    // If destination portal is on land (no vehicle parent), vanilla Valheim has already placed
    // the player properly at the portal once floor was found. We must not run a 10s wait
    // or poke zones or fall back to stale coordinates.
    var parentId = zdo.GetInt(VehicleZdoVars.MBParentId, 0);
    if (parentId == 0)
    {
      __instance.m_teleporting = false;
      m_teleportTarget.Remove(__instance);
      yield break;
    }

    ZNetView? go = null;
    var zoneId = ZoneSystem.GetZone(zdo.m_position);
    var startTime = Time.unscaledTime;
    const float timeout = 10f;

    while (go == null)
    {
      if (Time.unscaledTime - startTime > timeout)
      {
        Jotunn.Logger.LogWarning($"DebouncedTeleportCoordinateUpdater: Timed out waiting for portal instance {zdoid}.");
        break;
      }
      go = ZNetScene.instance.FindInstance(zdo);
      if (go) break;
      if (ZNetScene.instance != null)
      {
        try
        {
          var created = ZNetScene.instance.CreateObject(zdo);
          if (created != null)
          {
            go = created.GetComponent<ZNetView>();
            if (go) break;
          }
        }
        catch (System.Exception ex)
        {
          Jotunn.Logger.LogWarning($"DebouncedTeleportCoordinateUpdater: Could not create portal instance {zdoid}: {ex.Message}");
        }
      }
      zoneId = ZoneSystem.GetZone(zdo.m_position);
      if (ZoneSystem.instance != null)
      {
        ZoneSystem.instance.PokeLocalZone(zoneId);
      }
      yield return null;
    }

    if (go != null)
    {
      zoneId = ZoneSystem.GetZone(zdo.m_position);
      if (ZoneSystem.instance != null)
      {
        ZoneSystem.instance.PokeLocalZone(zoneId);
        var zoneWaitStart = Time.unscaledTime;
        while (!ZoneSystem.instance.IsZoneLoaded(zoneId) && Time.unscaledTime - zoneWaitStart < 5f)
        {
          yield return null;
        }
      }

      var teleportPosition = GetTeleportPosition(go.gameObject);
      __instance.transform.position = teleportPosition;

      var controller = go.GetComponentInParent<VehiclePiecesController>();
      if (controller != null && controller.Manager != null && controller.Manager.OnboardController != null)
      {
        controller.Manager.OnboardController.TryAddPlayerIfMissing(__instance);
      }
      else if (VehicleManager.VehicleInstances != null && VehicleManager.VehicleInstances.TryGetValue(parentId, out var vm) && vm.Instance != null && vm.Instance.OnboardController != null)
      {
        vm.Instance.OnboardController.TryAddPlayerIfMissing(__instance);
      }
    }

    __instance.m_teleporting = false;
    m_teleportTarget.Remove(__instance);
  }

  private static void SetIsTeleporting(Player __instance, bool isTeleporting)
  {
    __instance.m_teleporting = isTeleporting;
    if (isTeleporting ||
        !m_teleportTarget.TryGetValue(__instance, out var zdoid))
      return;

    __instance.StopCoroutine(nameof(DebouncedTeleportCoordinateUpdater));
    __instance.StartCoroutine(
      DebouncedTeleportCoordinateUpdater(__instance, isTeleporting, zdoid));
  }

  [HarmonyPatch(typeof(Game), nameof(Game.SpawnPlayer))]
  [HarmonyPostfix]
  public static void Game_SpawnPlayer_Postfix(Game __instance, Player __result)
  {
    if (__result == null || Game.instance == null) return;
    var profile = Game.instance.GetPlayerProfile();
    if (profile == null) return;
    var customSpawn = profile.GetCustomSpawnPoint();
    if (customSpawn == Vector3.zero) return;

    if (VehicleManager.VehicleInstances != null)
    {
      foreach (var vm in VehicleManager.VehicleInstances.Values)
      {
        if (vm != null && vm.Instance != null && vm.Instance.MovementController != null && vm.Instance.PiecesController != null)
        {
          if (vm.Instance.MovementController.ShouldForceAnchorOnBedTeleport())
          {
            if (vm.Instance.PiecesController.m_bedPieces != null)
            {
              foreach (var b in vm.Instance.PiecesController.m_bedPieces)
              {
                if (b != null && Vector3.Distance(b.GetSpawnPoint(), customSpawn) < 5f)
                {
                  vm.Instance.MovementController.TriggerForceAnchorTeleportAlert();
                  return;
                }
              }
            }
          }
        }
      }
    }
  }

  [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.AddPeer))]
  public static class ZDOMan_AddPeer_Patch
  {
    private static void Postfix(ZDOMan __instance, ZNetPeer netPeer)
    {
      if (ZNet.instance == null || !ZNet.instance.IsServer() || netPeer == null) return;
      if (VehicleManager.VehicleInstances == null) return;

      foreach (var vm in VehicleManager.VehicleInstances.Values)
      {
        if (vm == null || vm.Instance == null) continue;
        var rootZdo = vm.m_zdo ?? (vm.m_nview != null && vm.m_nview.IsValid() ? vm.m_nview.GetZDO() : null);
        if (rootZdo != null)
        {
          __instance.ForceSendZDO(netPeer.m_uid, rootZdo.m_uid);
        }

        if (vm.Instance.PiecesController != null && vm.Instance.PiecesController.m_portals != null)
        {
          foreach (var p in vm.Instance.PiecesController.m_portals)
          {
            if (p != null && p.IsValid())
            {
              __instance.ForceSendZDO(netPeer.m_uid, p.GetZDO().m_uid);
            }
          }
        }
      }
    }
  }
}
