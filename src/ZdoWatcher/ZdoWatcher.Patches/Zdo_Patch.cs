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
      if (liveZdo != null && !ReferenceEquals(liveZdo, __instance))
      {
        // Live ZDO is still active in ZDOMan.m_objectsByID, and __instance is a separate clone!
        // DO NOT deregister the live ZDO.
        return true;
      }
    }

    ZdoWatchController.Instance.Reset(__instance);
    return true;
  }
}
