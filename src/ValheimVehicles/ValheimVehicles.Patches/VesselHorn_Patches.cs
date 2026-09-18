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
      LoggerProvider.LogInfo("[VesselHorn] Suppressed punch/attack while holding horn.");
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
      LoggerProvider.LogInfo("[VesselHorn] Suppressed block while holding horn.");
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
}