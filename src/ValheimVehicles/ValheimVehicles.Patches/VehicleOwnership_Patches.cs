using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.Controllers;
using ValheimVehicles.Shared.Constants;
using ZdoWatcher;

namespace ValheimVehicles.Patches;

/// <summary>
/// Scraps/bypasses ownership and creator restrictions on vehicle pieces and containers.
/// Allows any player to add, open chests, repair, or hammer/dismantle parts on the ship.
/// </summary>
[HarmonyPatch]
public static class VehicleOwnership_Patches
{
  public static bool IsVehiclePiece(GameObject? go)
  {
    if (go == null) return false;

    var netView = go.GetComponentInParent<ZNetView>();
    if (netView != null && netView.IsValid())
    {
      var zdo = netView.GetZDO();
      if (zdo != null)
      {
        if (zdo.GetInt(VehicleZdoVars.MBParentId, 0) != 0) return true;
        if (zdo.GetInt(VehicleZdoVars.TempPieceParentId, 0) != 0) return true;
        if (ZdoWatchController.GetPersistentID(zdo, out var pId) &&
            (VehicleManager.VehicleInstances.ContainsKey(pId) || VehiclePiecesController.ActiveInstances.ContainsKey(pId)))
          return true;
        if (VehiclePiecesController.VehicleParentIdCache.ContainsKey(zdo)) return true;
      }
    }

    if (go.GetComponentInParent<VehicleManager>() != null ||
        go.GetComponentInParent<VehiclePiecesController>() != null)
      return true;

    // Check if the object is physically within any active vehicle's deck bounds
    foreach (var controller in VehiclePiecesController.ActiveInstances.Values)
    {
      if (controller != null && controller.isActiveAndEnabled && controller.OnboardCollider != null)
      {
        if (controller.OnboardCollider.bounds.Contains(go.transform.position))
          return true;
      }
    }

    return false;
  }

  [HarmonyPatch(typeof(Container), "CheckAccess", typeof(long))]
  [HarmonyPrefix]
  public static bool Container_CheckAccess(Container __instance, long playerID, ref bool __result)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Container), nameof(Container.Interact))]
  [HarmonyPrefix]
  public static void Container_Interact(Container __instance)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.m_nview != null && __instance.m_nview.IsValid())
      {
        if (!__instance.m_nview.IsOwner())
        {
          __instance.m_nview.ClaimOwnership();
        }
      }
    }
  }

  [HarmonyPatch(typeof(Container), "RPC_RequestOpen")]
  [HarmonyPrefix]
  public static bool Container_RPC_RequestOpen(Container __instance, long uid, long playerID)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.IsInUse() && uid != ZNet.GetUID())
      {
        __instance.m_nview.InvokeRPC(uid, "RPC_OpenResponse", false);
        return false;
      }
      ZDOMan.instance.ForceSendZDO(uid, __instance.m_nview.GetZDO().m_uid);
      __instance.m_nview.GetZDO().SetOwner(uid);
      __instance.m_nview.InvokeRPC(uid, "RPC_OpenResponse", true);
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Container), nameof(Container.CanBeRemoved))]
  [HarmonyPrefix]
  public static bool Container_CanBeRemoved(Container __instance, ref bool __result)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Piece), nameof(Piece.CanBeRemoved))]
  [HarmonyPrefix]
  public static bool Piece_CanBeRemoved(Piece __instance, ref bool __result)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      __instance.m_canBeRemoved = true;
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Player), "CheckCanRemovePiece")]
  [HarmonyPrefix]
  public static bool Player_CheckCanRemovePiece(Piece piece, ref bool __result)
  {
    if (piece != null && IsVehiclePiece(piece.gameObject))
    {
      __result = true;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Player), "RemovePiece")]
  [HarmonyPrefix]
  public static void Player_RemovePiece(Player __instance)
  {
    var hoveringPiece = __instance.GetHoveringPiece();
    if (hoveringPiece != null && IsVehiclePiece(hoveringPiece.gameObject))
    {
      hoveringPiece.m_canBeRemoved = true;
      var nv = hoveringPiece.GetComponent<ZNetView>();
      if (nv != null && nv.IsValid() && !nv.IsOwner())
      {
        nv.ClaimOwnership();
      }
    }
  }

  [HarmonyPatch(typeof(WearNTear), nameof(WearNTear.Remove))]
  [HarmonyPrefix]
  public static void WearNTear_Remove(WearNTear __instance)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.m_nview != null && __instance.m_nview.IsValid() && !__instance.m_nview.IsOwner())
      {
        __instance.m_nview.ClaimOwnership();
      }
    }
  }

  [HarmonyPatch(typeof(WearNTear), "RPC_Remove")]
  [HarmonyPrefix]
  public static bool WearNTear_RPC_Remove(WearNTear __instance, long sender, bool blockDrop)
  {
    if (IsVehiclePiece(__instance.gameObject))
    {
      if (__instance.m_nview != null && __instance.m_nview.IsValid())
      {
        if (!__instance.m_nview.IsOwner())
        {
          __instance.m_nview.ClaimOwnership();
        }
        __instance.Destroy(null, blockDrop);
        return false;
      }
    }
    return true;
  }

  [HarmonyPatch(typeof(PrivateArea), nameof(PrivateArea.CheckAccess))]
  [HarmonyPrefix]
  public static bool PrivateArea_CheckAccess(Vector3 point, ref bool __result)
  {
    foreach (var controller in VehiclePiecesController.ActiveInstances.Values)
    {
      if (controller != null && controller.isActiveAndEnabled && controller.OnboardCollider != null)
      {
        if (controller.OnboardCollider.bounds.Contains(point))
        {
          __result = true;
          return false;
        }
      }
    }
    return true;
  }
}
