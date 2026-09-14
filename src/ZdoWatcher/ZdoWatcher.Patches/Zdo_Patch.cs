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
  private static void ZDO_Reset(ZDO __instance)
  {
    ZdoWatchController.Instance.Reset(__instance);
  }
}