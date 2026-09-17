using HarmonyLib;

namespace ZdoWatcher.Patches;

[HarmonyPatch]
public class ZdoPatch
{
  [HarmonyPatch(typeof(ZDO), "Deserialize")]
  [HarmonyPostfix]
  private static void ZDO_Deserialize(ZDO __instance, ZPackage pkg)
  {
    ZdoWatchController.Instance.Deserialize(__instance);
  }

  [HarmonyPatch(typeof(ZDO), "Load", typeof(ZPackage), typeof(global::Version.World))]
  [HarmonyPostfix]
  private static void ZDO_Load(ZDO __instance)
  {
    ZdoWatchController.Instance.Load(__instance);
  }

  [HarmonyPatch(typeof(ZDO), "Reset")]
  [HarmonyPrefix]
  private static bool ZDO_Reset(ZDO __instance)
  {
    if (ZDOMan.instance != null)
    {
      var liveZdo = ZDOMan.instance.GetZDO(__instance.m_uid);
      if (liveZdo != null)
      {
        // Live ZDO is still active in ZDOMan.m_objectsByID!
        // This is a temporary save-data clone recycled during SaveCleanup, or an active world object.
        // DO NOT deregister or wipe from lookups.
        return true;
      }
    }

    ZdoWatchController.Instance.Reset(__instance);
    return true;
  }
}
