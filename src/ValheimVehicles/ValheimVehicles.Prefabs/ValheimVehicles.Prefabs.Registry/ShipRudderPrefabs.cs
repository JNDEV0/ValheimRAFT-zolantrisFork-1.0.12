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

    SharedSetup(prefab);

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

  private static void SharedSetup(GameObject prefab)
  {
    PrefabRegistryHelpers.AddNetViewWithPersistence(prefab);
    PrefabRegistryHelpers.AddPieceForPrefab(prefab.name, prefab);
    var rudderComponent = prefab.AddComponent<RudderComponent>();
    rudderComponent.PivotPoint = prefab.transform.FindDeepChild("rudder_rotation");

    PrefabRegistryHelpers.SetWearNTear(prefab);
    PrefabRegistryHelpers.FixCollisionLayers(prefab);
    PrefabRegistryHelpers.HoistSnapPointsToPrefab(prefab);
  }


  private static void RegisterAdvancedRudderVariant(string variantName, GameObject prefabAsset, RequirementConfig[] requirements)
  {
    var prefab =
      PrefabManager.Instance.CreateClonedPrefab(
        variantName, prefabAsset);
    SharedSetup(prefab);

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
      ]);

    // Step 2: Ship Rudder (Advanced)
    RegisterAdvancedRudderVariant(PrefabNames.ShipRudderAdvancedDoubleWood,
      LoadValheimVehicleAssets.ShipRudderAdvancedDoubleWoodAsset,
      [
        new RequirementConfig
        {
          Amount = 40,
          Item = "FineWood",
          Recover = true
        },
        new RequirementConfig
        {
          Amount = 10,
          Item = "Iron",
          Recover = true
        }
      ]);

    // Step 3: Removed ShipRudderAdvancedIron and ShipRudderAdvancedDoubleIron
  }
}