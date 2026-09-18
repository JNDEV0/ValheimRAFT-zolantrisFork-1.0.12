using System;
using System.Collections.Generic;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.SharedScripts.Enums;
using ValheimVehicles.Shared.Constants;
using ZdoWatcher;
using Zolantris.Shared;
using Object = UnityEngine.Object;

namespace ValheimVehicles.Controllers;

public static class VehicleRecallController
{
  public const string AttunedVesselZdoKey = "VR_AttunedVesselId";

  public static int ResolveTargetVehicle(Player player)
  {
    if (player == null) return 0;

    // 1. Check if player has explicitly attuned a vehicle ID
    var playerZdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      var attunedId = playerZdo.GetInt(AttunedVesselZdoKey, 0);
      if (attunedId != 0 && DoesVehicleExist(attunedId))
      {
        return attunedId;
      }
    }

    // 2. Check if player is currently standing on a vehicle
    var vpcOnPlayer = player.GetComponentInParent<VehiclePiecesController>() ??
                      player.transform.root.GetComponentInChildren<VehiclePiecesController>();
    if (vpcOnPlayer != null && vpcOnPlayer.PersistentZdoId != 0)
    {
      return vpcOnPlayer.PersistentZdoId;
    }

    var playerId = player.GetPlayerID();

    // 3. Check loaded vehicles created by this player
    foreach (var kvp in VehicleManager.VehicleInstances)
    {
      var vm = kvp.Value;
      if (vm != null && vm.m_nview != null && vm.m_nview.GetZDO() != null)
      {
        var zdo = vm.m_nview.GetZDO();
        if (zdo.GetLong(ZDOVars.s_creator, 0) == playerId)
        {
          return kvp.Key;
        }
      }
    }

    // 4. Scan all ZDOs in ZDOMan for vehicles created by this player
    if (ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
    {
      var waterShipPrefab = PrefabNames.WaterVehicleShip.GetStableHashCode();
      var landShipPrefab = PrefabNames.LandVehicle.GetStableHashCode();

      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        var zdo = kvp.Value;
        if (zdo == null || !zdo.IsValid()) continue;
        var prefab = zdo.GetPrefab();
        if (prefab == waterShipPrefab || prefab == landShipPrefab)
        {
          if (zdo.GetLong(ZDOVars.s_creator, 0) == playerId)
          {
            var pId = zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
            if (pId != 0) return pId;
          }
        }
      }

      // 5. Fallback: If no vehicle created by this player, pick the first ship in the world
      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        var zdo = kvp.Value;
        if (zdo == null || !zdo.IsValid()) continue;
        var prefab = zdo.GetPrefab();
        if (prefab == waterShipPrefab || prefab == landShipPrefab)
        {
          var pId = zdo.GetInt(ZdoVarController.PersistentUidHash, 0);
          if (pId != 0) return pId;
        }
      }
    }

    // 6. Fallback: Check tracked piece buckets in VehiclePiecesController
    foreach (var key in VehiclePiecesController.m_allPieces.Keys)
    {
      if (key != 0) return key;
    }

    return 0;
  }

  public static bool DoesVehicleExist(int vehicleId)
  {
    if (vehicleId == 0) return false;
    if (VehicleManager.VehicleInstances.ContainsKey(vehicleId)) return true;
    if (ZdoWatchController.Instance != null && ZdoWatchController.Instance.GetZdo(vehicleId) != null) return true;

    if (ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
    {
      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        if (kvp.Value != null && kvp.Value.GetInt(ZdoVarController.PersistentUidHash, 0) == vehicleId)
        {
          return true;
        }
      }
    }
    return false;
  }

  public static bool GetVehicleLocation(int vehicleId, out Vector3 position, out Quaternion rotation, out bool isLoaded, out VehicleManager? vehicleManager)
  {
    position = Vector3.zero;
    rotation = Quaternion.identity;
    isLoaded = false;
    vehicleManager = null;

    if (vehicleId == 0) return false;

    if (VehicleManager.VehicleInstances.TryGetValue(vehicleId, out vehicleManager) && vehicleManager != null)
    {
      position = vehicleManager.transform.position;
      rotation = vehicleManager.transform.rotation;
      isLoaded = true;
      return true;
    }

    ZDO? zdo = null;
    if (ZdoWatchController.Instance != null)
    {
      zdo = ZdoWatchController.Instance.GetZdo(vehicleId);
    }

    if (zdo == null && ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
    {
      foreach (var kvp in ZDOMan.instance.m_objectsByID)
      {
        if (kvp.Value != null && kvp.Value.GetInt(ZdoVarController.PersistentUidHash, 0) == vehicleId)
        {
          zdo = kvp.Value;
          break;
        }
      }
    }

    if (zdo != null && zdo.IsValid())
    {
      position = zdo.GetPosition();
      rotation = zdo.GetRotation();
      isLoaded = false;
      return true;
    }

    return false;
  }

  public static void TeleportPlayerToVehicle(Player player, int vehicleId)
  {
    if (player == null) return;

    if (!GetVehicleLocation(vehicleId, out var targetPos, out var targetRot, out var isLoaded, out var vm))
    {
      player.Message(MessageHud.MessageType.Center, "Could not locate vessel position!");
      return;
    }

    Vector3 landingPos;
    Quaternion landingRot = targetRot;

    if (isLoaded && vm != null)
    {
      var wheel = vm.GetComponentInChildren<SteeringWheelComponent>();
      if (wheel != null)
      {
        landingPos = wheel.transform.position - wheel.transform.forward * 0.8f + Vector3.up * 0.1f;
        landingRot = wheel.transform.rotation;
      }
      else if (vm.RudderObject != null)
      {
        landingPos = vm.RudderObject.transform.position + Vector3.up * 0.5f;
      }
      else
      {
        landingPos = targetPos + targetRot * Vector3.up * 1.5f;
      }
    }
    else
    {
      // Unloaded vessel: try to find steering wheel ZDO offset
      var pieces = VehiclePiecesController.EnsurePiecesForVehicle(vehicleId);
      var wheelHash = PrefabNames.ShipSteeringWheel.GetStableHashCode();
      ZDO? wheelZdo = null;
      foreach (var pz in pieces)
      {
        if (pz != null && pz.IsValid() && pz.GetPrefab() == wheelHash)
        {
          wheelZdo = pz;
          break;
        }
      }

      if (wheelZdo != null)
      {
        var wheelOffset = wheelZdo.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        landingPos = targetPos + targetRot * wheelOffset + Vector3.up * 0.5f;
      }
      else
      {
        landingPos = targetPos + Vector3.up * 2.0f;
      }
    }

    PlaySfxAt("sfx_portal_activate", player.transform.position);

    // Distant teleport handles sector streaming if far away
    player.TeleportTo(landingPos, landingRot, distantTeleport: !isLoaded);
    player.Message(MessageHud.MessageType.Center, $"Teleported to Vessel #{vehicleId} wheel!");
  }

  public static void RecallVehicleToCrosshair(Player player, int vehicleId)
  {
    if (player == null) return;

    if (!GetVehicleLocation(vehicleId, out _, out _, out var isLoaded, out var vm))
    {
      player.Message(MessageHud.MessageType.Center, "Could not locate vessel to recall!");
      return;
    }

    // Determine target location via camera raycast
    var cam = GameCamera.instance ? GameCamera.instance.transform : null;
    var rayOrigin = cam != null ? cam.position : player.GetEyePoint();
    var rayDir = cam != null ? cam.forward : player.GetLookDir();

    Vector3 hitPoint;
    var mask = LayerMask.GetMask("Default", "static_solid", "Default_small", "piece", "terrain", "water");
    if (Physics.Raycast(rayOrigin, rayDir, out var hit, 120f, mask))
    {
      hitPoint = hit.point;
    }
    else
    {
      hitPoint = player.transform.position + rayDir * 25f;
    }

    var waterLevel = ZoneSystem.instance != null ? ZoneSystem.instance.m_waterLevel : 30f;
    var isLand = hitPoint.y > waterLevel + 0.5f;

    Vector3 targetPos;
    float targetHeightAboveWater;

    if (isLand)
    {
      // Land recall: flight mode safely 6 meters above the terrain
      targetPos = new Vector3(hitPoint.x, hitPoint.y + 6.0f, hitPoint.z);
      targetHeightAboveWater = targetPos.y - waterLevel;
    }
    else
    {
      // Water recall: float mode at water level
      targetPos = new Vector3(hitPoint.x, waterLevel, hitPoint.z);
      targetHeightAboveWater = 0f;
    }

    var flatForward = Vector3.ProjectOnPlane(rayDir, Vector3.up).normalized;
    if (flatForward.sqrMagnitude < 0.01f) flatForward = player.transform.forward;
    var targetRot = Quaternion.LookRotation(flatForward, Vector3.up);

    ZDO? vehicleZdo = null;
    if (isLoaded && vm != null)
    {
      vm.transform.position = targetPos;
      vm.transform.rotation = targetRot;

      var rb = vm.MovementControllerRigidbody ?? vm.GetComponentInChildren<Rigidbody>();
      if (rb != null)
      {
        rb.position = targetPos;
        rb.rotation = targetRot;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
      }

      if (vm.MovementController != null)
      {
        vm.MovementController.SetTargetHeight(targetHeightAboveWater);
        vm.MovementController.SetAnchor(AnchorState.Anchored);
      }

      if (vm.m_nview != null && vm.m_nview.GetZDO() != null)
      {
        vehicleZdo = vm.m_nview.GetZDO();
        vehicleZdo.Set(VehicleZdoVars.VehicleTargetHeight, targetHeightAboveWater);
        var oldSec = vehicleZdo.GetSectorIndex();
        vehicleZdo.SetPosition(targetPos);
        vehicleZdo.SetRotation(targetRot);
        var newSec = vehicleZdo.GetSectorIndex();
        if (oldSec != newSec && ZDOMan.instance != null)
        {
          ZDOMan.instance.RemoveFromSector(vehicleZdo, oldSec);
          ZDOMan.instance.AddToSector(vehicleZdo, newSec);
        }
      }

      vm.PiecesController?.ForceUpdateAllPiecePositions();
      VehiclePiecesController.SyncAllPrefabsToVehiclePosition(vehicleId);
    }
    else
    {
      // Distant/unloaded vessel ZDO migration
      if (ZdoWatchController.Instance != null)
      {
        vehicleZdo = ZdoWatchController.Instance.GetZdo(vehicleId);
      }
      if (vehicleZdo == null && ZDOMan.instance != null && ZDOMan.instance.m_objectsByID != null)
      {
        foreach (var kvp in ZDOMan.instance.m_objectsByID)
        {
          if (kvp.Value != null && kvp.Value.GetInt(ZdoVarController.PersistentUidHash, 0) == vehicleId)
          {
            vehicleZdo = kvp.Value;
            break;
          }
        }
      }

      if (vehicleZdo != null && vehicleZdo.IsValid())
      {
        vehicleZdo.Set(VehicleZdoVars.VehicleTargetHeight, targetHeightAboveWater);
        var oldSec = vehicleZdo.GetSectorIndex();
        vehicleZdo.SetPosition(targetPos);
        vehicleZdo.SetRotation(targetRot);
        var newSec = vehicleZdo.GetSectorIndex();
        if (oldSec != newSec && ZDOMan.instance != null)
        {
          ZDOMan.instance.RemoveFromSector(vehicleZdo, oldSec);
          ZDOMan.instance.AddToSector(vehicleZdo, newSec);
        }

        var pieceZdos = VehiclePiecesController.EnsurePiecesForVehicle(vehicleId);
        foreach (var pz in pieceZdos)
        {
          if (pz == null || !pz.IsValid()) continue;
          var localOffset = pz.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
          var pieceWorldPos = targetPos + targetRot * localOffset;
          var pOldSec = pz.GetSectorIndex();
          pz.SetPosition(pieceWorldPos);
          var pNewSec = pz.GetSectorIndex();
          if (pOldSec != pNewSec && ZDOMan.instance != null)
          {
            ZDOMan.instance.RemoveFromSector(pz, pOldSec);
            ZDOMan.instance.AddToSector(pz, pNewSec);
          }
        }

        if (ZNetScene.instance != null)
        {
          ZNetScene.instance.CreateObject(vehicleZdo);
          foreach (var pz in pieceZdos)
          {
            ZNetScene.instance.CreateObject(pz);
          }
        }
      }
    }

    if (isLand)
    {
      PlaySfxAt("sfx_portal_activate", targetPos);
      player.Message(MessageHud.MessageType.Center, $"Vessel #{vehicleId} recalled in Flight Mode!");
    }
    else
    {
      PlaySfxAt("sfx_water_splash", targetPos);
      player.Message(MessageHud.MessageType.Center, $"Vessel #{vehicleId} recalled in Float Mode!");
    }
  }

  public static void AttunePlayerToVehicle(Player player, int vehicleId)
  {
    if (player == null || vehicleId == 0) return;

    var playerZdo = player.m_nview != null ? player.m_nview.GetZDO() : null;
    if (playerZdo != null)
    {
      playerZdo.Set(AttunedVesselZdoKey, vehicleId);
    }

    PlaySfxAt("sfx_cheers", player.transform.position);
    player.Message(MessageHud.MessageType.Center, $"Horn bound to Vessel #{vehicleId}!");
  }

  public static int DetectAimedOrCurrentVehicle(Player player)
  {
    if (player == null) return 0;

    // Check if standing on vehicle
    var vpc = player.GetComponentInParent<VehiclePiecesController>() ??
              player.transform.root.GetComponentInChildren<VehiclePiecesController>();
    if (vpc != null && vpc.PersistentZdoId != 0)
    {
      return vpc.PersistentZdoId;
    }

    // Check if aiming directly at a vehicle piece
    var cam = GameCamera.instance ? GameCamera.instance.transform : null;
    var rayOrigin = cam != null ? cam.position : player.GetEyePoint();
    var rayDir = cam != null ? cam.forward : player.GetLookDir();

    if (Physics.Raycast(rayOrigin, rayDir, out var hit, 50f))
    {
      var hitVpc = hit.collider.GetComponentInParent<VehiclePiecesController>();
      if (hitVpc != null && hitVpc.PersistentZdoId != 0)
      {
        return hitVpc.PersistentZdoId;
      }
      var nv = hit.collider.GetComponentInParent<ZNetView>();
      if (nv != null && nv.GetZDO() != null)
      {
        var parentId = nv.GetZDO().GetInt(VehicleZdoVars.MBParentId, 0);
        if (parentId != 0) return parentId;
        var pId = nv.GetZDO().GetInt(ZdoVarController.PersistentUidHash, 0);
        if (pId != 0 && DoesVehicleExist(pId)) return pId;
      }
    }

    return ResolveTargetVehicle(player);
  }

  private static void PlaySfxAt(string prefabName, Vector3 position)
  {
    if (ZNetScene.instance == null) return;
    var sfx = ZNetScene.instance.GetPrefab(prefabName);
    if (sfx != null)
    {
      Object.Instantiate(sfx, position, Quaternion.identity);
    }
  }
}