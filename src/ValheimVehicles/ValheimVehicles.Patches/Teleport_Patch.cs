using ValheimVehicles.Shared.Constants;
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

  public static void TeleportToActivePosition(TeleportWorld __instance,
    ZDOID playerId)
  {
    var zDO = ZDOMan.instance.GetZDO(
      __instance.m_nview.m_zdo.GetConnectionZDOID(ZDOExtraData.ConnectionType
        .Portal));
    if (zDO == null) return;

    var parentId = zDO.GetInt(VehicleZdoVars.MBParentId, 0);
    var nv = ZNetScene.instance.FindInstance(zDO);
    var position = nv ? nv.transform.position : zDO.GetPosition();
    var rotation = nv ? nv.transform.rotation : zDO.GetRotation();

    // If destination portal is on a vehicle and not loaded locally yet, compute current vehicle world coords
    if (parentId != 0 && nv == null)
    {
      var vehicleZdo = ZDOMan.instance.GetZDO(new ZDOID(zDO.m_uid.UserID, (uint)parentId))
                    ?? ZDOMan.instance.GetZDO(new ZDOID(1, (uint)parentId));
      if (vehicleZdo != null)
      {
        var vPos = vehicleZdo.GetPosition();
        var vRot = vehicleZdo.GetRotation();
        var localPos = zDO.GetVec3(VehicleZdoVars.MBPositionHash, Vector3.zero);
        var localRot = Quaternion.Euler(zDO.GetVec3(VehicleZdoVars.MBRotationVecHash, Vector3.zero));
        position = vPos + vRot * localPos;
        rotation = vRot * localRot;
      }
    }

    var vector = rotation * Vector3.forward;
    var pos = position + vector * __instance.m_exitDistance + Vector3.up;
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

    var go = ZNetScene.instance.FindInstance(zdoid);
    if ((bool)go) return GetTeleportPosition(go);

    return __instance.m_teleportTargetPos;
  }

  private static Vector3 GetTeleportPosition(GameObject go)
  {
    var tp = go.GetComponent<TeleportWorld>();

    if ((bool)tp)
      return tp.transform.position + tp.transform.forward * tp.m_exitDistance +
             Vector3.up;

    return go.transform.position;
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
    var startTime = Time.time;
    const float timeout = 10f;

    while (go == null)
    {
      if (Time.time - startTime > timeout)
      {
        Jotunn.Logger.LogWarning($"DebouncedTeleportCoordinateUpdater: Timed out waiting for portal instance {zdoid}.");
        break;
      }
      go = ZNetScene.instance.FindInstance(zdo);
      if (go) break;
      zoneId = ZoneSystem.GetZone(zdo.m_position);
      ZoneSystem.instance.PokeLocalZone(zoneId);
      yield return new WaitForFixedUpdate();
    }

    if (go != null)
    {
      zoneId = ZoneSystem.GetZone(zdo.m_position);
      ZoneSystem.instance.PokeLocalZone(zoneId);
      var zoneWaitStart = Time.time;
      while (!ZoneSystem.instance.IsZoneLoaded(zoneId) && Time.time - zoneWaitStart < 5f)
      {
        yield return new WaitForFixedUpdate();
      }

      var teleportPosition = GetTeleportPosition(go.gameObject);
      __instance.transform.position = teleportPosition;
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
}
