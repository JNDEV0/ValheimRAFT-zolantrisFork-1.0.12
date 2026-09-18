using System.Collections.Generic;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts;
using HarmonyLib;
using UnityEngine;
using ValheimVehicles.Components;
using Zolantris.Shared;

namespace ValheimVehicles.Patches;

[HarmonyPatch]
public static class VesselHorn_Patches
{
  [HarmonyPatch(typeof(Player), nameof(Player.Update))]
  [HarmonyPostfix]
  public static void Player_Update_Postfix(Player __instance)
  {
    if (__instance == Player.m_localPlayer)
    {
      VesselHornChanneler.UpdateLocalPlayer(__instance);
    }
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
  [HarmonyPrefix]
  public static bool Humanoid_StartAttack_Prefix(Humanoid __instance, ref bool __result)
  {
    if (__instance is Player player && VesselHornChanneler.IsHoldingVesselHorn(player))
    {
      __result = false;
      // LoggerProvider.LogDebug("[VesselHorn] Suppressed punch/attack while holding horn.");
      return false; // Prevent weapon swing / punch while holding horn
    }
    return true;
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
  [HarmonyPrefix]
  public static bool Humanoid_BlockAttack_Prefix(Humanoid __instance, ref bool __result)
  {
    if (__instance is Player player && VesselHornChanneler.IsHoldingVesselHorn(player))
    {
      __result = false;
      // LoggerProvider.LogDebug("[VesselHorn] Suppressed block while holding horn.");
      return false; // Prevent block while holding horn
    }
    return true;
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.IsBlocking))]
  [HarmonyPrefix]
  public static bool Humanoid_IsBlocking_Prefix(Humanoid __instance, ref bool __result)
  {
    if (__instance is Player player && VesselHornChanneler.IsHoldingVesselHorn(player))
    {
      __result = false;
      return false;
    }
    return true;
  }

  [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.UpdateBlock))]
  [HarmonyPrefix]
  public static bool Humanoid_UpdateBlock_Prefix(Humanoid __instance)
  {
    if (__instance is Player player && VesselHornChanneler.IsHoldingVesselHorn(player))
    {
      return false; // Suppress block animation while holding horn
    }
    return true;
  }

  [HarmonyPatch(typeof(Hud), nameof(Hud.UpdateActionProgress))]
  [HarmonyPrefix]
  public static bool Hud_UpdateActionProgress_Prefix(Hud __instance, Player player)
  {
    if (VesselHornChanneler.IsChanneling)
    {
      if (__instance.m_actionBarRoot != null)
      {
        __instance.m_actionBarRoot.SetActive(true);

        if (__instance.m_actionProgress != null)
        {
          __instance.m_actionProgress.SetValue(VesselHornChanneler.ChannelProgress);
        }

        if (__instance.m_actionName != null)
        {
          __instance.m_actionName.text = VesselHornChanneler.ChannelActionName;
        }
      }
      return false; // Skip vanilla logic while channeling
    }
    return true;
  }
  private static bool _retainedHornOnDeath = false;
  private static ItemDrop.ItemData? _retainedHornData = null;

  [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
  [HarmonyPrefix]
  public static void Player_CreateTombStone_Prefix(Player __instance)
  {
    _retainedHornOnDeath = false;
    _retainedHornData = null;
    if (__instance == null || __instance.m_inventory == null) return;

    var items = __instance.m_inventory.GetAllItems();
    for (int i = items.Count - 1; i >= 0; i--)
    {
      var item = items[i];
      if (item != null && item.m_shared != null)
      {
        var name = item.m_shared.m_name;
        if (name == "$item_vessel_horn" || name == "Horn of Loki" || name == "Horn of the Sea" || name.Contains("vessel_horn"))
        {
          _retainedHornOnDeath = true;
          _retainedHornData = item;
          __instance.m_inventory.RemoveItem(item);
          if (LoopTracker.Enabled)
          {
            LoggerProvider.LogInfo("[VesselHorn] Preserved Horn of Loki from tombstone drop.");
          }
          break;
        }
      }
    }
  }

  [HarmonyPatch(typeof(Player), nameof(Player.CreateTombStone))]
  [HarmonyPostfix]
  public static void Player_CreateTombStone_Postfix(Player __instance)
  {
    if (__instance == null || __instance.m_inventory == null || _retainedHornData == null) return;
    __instance.m_inventory.AddItem(_retainedHornData);
    if (LoopTracker.Enabled)
    {
      LoggerProvider.LogInfo("[VesselHorn] Restored Horn of Loki to player inventory after tombstone creation.");
    }
  }

  [HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
  [HarmonyPostfix]
  public static void Player_OnSpawned_Postfix(Player __instance)
  {
    if (__instance == Player.m_localPlayer && _retainedHornOnDeath)
    {
      if (!VesselHornChanneler.HasVesselHornInInventory(__instance))
      {
        if (_retainedHornData != null)
      {
        __instance.m_inventory.AddItem(_retainedHornData);
      }
      else if (ObjectDB.instance != null)
      {
        var prefab = ObjectDB.instance.GetItemPrefab(PrefabNames.VesselHorn);
        var drop = prefab ? prefab.GetComponent<ItemDrop>() : null;
        if (drop != null)
        {
          __instance.m_inventory.AddItem(drop.m_itemData.Clone());
        }
      }
        if (LoopTracker.Enabled)
        {
          LoggerProvider.LogInfo("[VesselHorn] Granted Horn of Loki to respawned player.");
        }
      }
      _retainedHornOnDeath = false;
      _retainedHornData = null;
    }
  }
}