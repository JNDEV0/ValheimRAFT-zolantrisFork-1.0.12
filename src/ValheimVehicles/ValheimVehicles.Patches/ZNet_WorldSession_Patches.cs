using HarmonyLib;
using ValheimVehicles.Integrations;
namespace ValheimVehicles.Patches;

public static class ZNet_WorldSession_Patches
{
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Start))]
  [HarmonyPostfix]
  private static void SessionStart()
  {
    if (!ZNet.instance) return;
    try
    {
      if (ZNet.instance.GetWorld() != null)
      {
        var currentWorldId = ZNet.instance.GetWorldUID();
        WorldSessionState.EnsureWorldScope(currentWorldId);
      }
    }
    catch (System.Exception)
    {
      // World is null on multiplayer clients until received from server
    }
  }

  [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
  [HarmonyPostfix]
  private static void SessionTeardown()
  {
    WorldSessionState.OnSessionTeardown();
  }
}