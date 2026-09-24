using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Extensions;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Prefabs.Registry;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;

namespace ValheimVehicles.Prefabs;

public class ShipRudderPrefabs : RegisterPrefab<ShipRudderPrefabs>
{
  public override void OnRegister()
  {
    RegisterShipRudderBasic();
    RegisterShipRudderAdvanced();
  }

  private static void RegisterShipRudderBasic()
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        PrefabNames.ShipRudderBasic, LoadValheimVehicleAssets.ShipRudderBasicAsset);

    // Adjust pivot point on basic rudder so it hinges at the top attachment tip (hull contact point)
    // rather than the center of the paddle mesh, preventing visual detachment from the boat when turning.
    var rudderRotation = prefab.transform.FindDeepChild("rudder_rotation");
    var rudderMeshTransform = rudderRotation != null ? rudderRotation.Find("rudder") : null;
    if (rudderRotation != null && rudderMeshTransform != null)
    {
      var pivotOffset = new Vector3(-0.0833f, 0.8158f, 0.4648f);
      rudderRotation.localPosition = pivotOffset;
      rudderMeshTransform.localPosition = -pivotOffset;
    }

    SharedSetup(prefab, RudderTier.Basic);

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
      Enabled = true,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 20,
          Item = "Wood",
          Recover = true
        }
      ]
    }));
  }

  private static void SharedSetup(GameObject prefab, RudderTier tier = RudderTier.Basic)
  {
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(prefab.name, prefab);
    var rudderComponent = prefab.AddComponent<RudderComponent>();
    rudderComponent.tier = tier;
    rudderComponent.PivotPoint = prefab.transform.FindDeepChild("rudder_rotation");

    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
  }


  private static void RegisterAdvancedRudderVariant(string variantName, GameObject prefabAsset, RequirementConfig[] requirements, RudderTier tier = RudderTier.Standard)
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        variantName, prefabAsset);
    SharedSetup(prefab, tier);

    PrefabRegistryController.AddPiece(new CustomPiece(prefab, false, new PieceConfig
    {
      PieceTable = PrefabRegistryController.GetPieceTableName(),
      Category = PrefabRegistryController.SetCategoryName(VehicleHammerTableCategories.Propulsion),
      Enabled = true,
      Requirements = requirements
    }));
  }

  /**
 * Ship rudders: Basic, Standard, and Advanced.
 * - Rudder controls the ship direction.
 */
  private static void RegisterShipRudderAdvanced()
  {
    // Step 4: Ship Rudder (Standard)
    RegisterAdvancedRudderVariant(PrefabNames.ShipRudderAdvancedWood,
      LoadValheimVehicleAssets.ShipRudderAdvancedSingleWoodAsset,
      [
        new RequirementConfig
        {
          Amount = 30,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 5,
          Item = "Bronze",
          Recover = true
        }
      ],
      RudderTier.Standard);

    // Removed Ship Rudder (Advanced) from build menu (kept Basic and Standard)

    // Step 3: Removed ShipRudderAdvancedIron and ShipRudderAdvancedDoubleIron
  }
}