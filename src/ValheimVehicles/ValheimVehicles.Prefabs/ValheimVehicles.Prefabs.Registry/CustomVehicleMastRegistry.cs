using UnityEngine;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using ValheimVehicles.Components;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.SharedScripts;
namespace ValheimVehicles.Prefabs.ValheimVehicles.Prefabs.Registry;

public class CustomVehicleMastRegistry : RegisterPrefab<CustomVehicleMastRegistry>
{
  private static void RegisterMast(string mastTier)
  {
    var mastAsset = LoadValheimVehicleAssets.GetMastVariant(mastTier);
    var prefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.GetMastByLevelName(mastTier), mastAsset);

    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(PrefabNames.GetMastByLevelName(mastTier), prefab);
    PrefabRegistryHelpers.SetWearNTear(prefab);

    foreach (var mc in prefab.GetComponentsInChildren<MeshCollider>(true))
    {
      try
      {
        if (mc.convex && mc.sharedMesh != null && mc.sharedMesh.vertexCount > 255)
        {
          mc.convex = false;
        }
      }
      catch { }
    }

    var mastComponent = prefab.AddComponent<MastComponent>();
    mastComponent.m_sailCloth = null;
    mastComponent.m_sailCloth = null;
    mastComponent.m_allowSailRotation = PrefabConfig.AllowTieredMastToRotate.Value;
    mastComponent.m_allowSailShrinking = false;
    mastComponent.m_rotationTransform = prefab.transform.Find("rotational_yard");

    // Disable custom masts level 1, 2, 3 from hammer build menu
    if (prefab.TryGetComponent<Piece>(out var piece))
    {
      piece.m_enabled = false;
    }
    PrefabManager.Instance.AddPrefab(prefab);
  }

  public override void OnRegister()
  {
    RegisterMast(PrefabNames.MastLevels.ONE);
    RegisterMast(PrefabNames.MastLevels.TWO);
    RegisterMast(PrefabNames.MastLevels.THREE);
  }
}