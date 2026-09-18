using System;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.Components;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Prefabs.Registry;

public class VehicleRecallHornItemRegistry : RegisterPrefab<VehicleRecallHornItemRegistry>
{
  public static void RegisterVehicleHorn()
  {
    GameObject? hornPrefab = null;

    try
    {
      hornPrefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VesselHorn, "Tankard_Odin");
    }
    catch (Exception e)
    {
      LoggerProvider.LogDebug($"Tankard_Odin clone failed: {e.Message}, trying TankardAnniversary");
    }

    if (!hornPrefab)
    {
      try
      {
        hornPrefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VesselHorn, "TankardAnniversary");
      }
      catch (Exception e)
      {
        LoggerProvider.LogDebug($"TankardAnniversary clone failed: {e.Message}, trying Tankard");
      }
    }

    if (!hornPrefab)
    {
      try
      {
        hornPrefab = PrefabManager.Instance.CreateClonedPrefab(PrefabNames.VesselHorn, "Tankard");
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"All horn clones failed: {e.Message}");
      }
    }

    if (!hornPrefab)
    {
      LoggerProvider.LogError("Failed to create cloned prefab for VesselHorn!");
      return;
    }

    var nv = PrefabRegistryHelpers.AddNetViewWithPersistence(hornPrefab);
    var zSyncTransform = hornPrefab.GetComponent<ZSyncTransform>() ?? hornPrefab.AddComponent<ZSyncTransform>();
    zSyncTransform.m_syncBodyVelocity = false;
    zSyncTransform.m_syncRotation = true;
    zSyncTransform.m_syncPosition = true;

    // Attach our custom hold-action listener
    if (hornPrefab.GetComponent<VesselHornComponent>() == null)
    {
      hornPrefab.AddComponent<VesselHornComponent>();
    }

    var itemDrop = hornPrefab.GetComponent<ItemDrop>();
    if (itemDrop == null)
    {
      itemDrop = hornPrefab.AddComponent<ItemDrop>();
    }
    if (itemDrop.m_nview == null)
    {
      itemDrop.m_nview = nv;
    }

    itemDrop.m_itemData.m_shared.m_name = "$item_vessel_horn";
    itemDrop.m_itemData.m_shared.m_description = "$item_vessel_horn_desc";
    itemDrop.m_itemData.m_shared.m_itemType = ItemDrop.ItemData.ItemType.Tool;
    itemDrop.m_itemData.m_shared.m_animationState = ItemDrop.ItemData.AnimationState.OneHanded;
    itemDrop.m_itemData.m_shared.m_equipDuration = 0;
    itemDrop.m_itemData.m_shared.m_maxStackSize = 1;
    itemDrop.m_itemData.m_shared.m_weight = 1.0f;
    itemDrop.m_itemData.m_shared.m_useDurability = false;
    itemDrop.m_itemData.m_shared.m_maxDurability = 100f;
    itemDrop.m_itemData.m_shared.m_consumeStatusEffect = null;
    itemDrop.m_itemData.m_shared.m_attack = null;
    itemDrop.m_itemData.m_shared.m_secondaryAttack = null;
    itemDrop.m_itemData.m_shared.m_blockPower = 0f;

    var itemConfig = new ItemConfig
    {
      Name = "$item_vessel_horn",
      Description = "$item_vessel_horn_desc",
      CraftingStation = "", // Hand-crafted, no workbench required
      MinStationLevel = 0,
      Requirements =
      [
        new RequirementConfig
        {
          Amount = 2,
          Item = "Wood"
        }
      ]
    };

    var customItem = new CustomItem(hornPrefab, true, itemConfig);
    var success = ItemManager.Instance.AddItem(customItem);
    if (!success)
    {
      LoggerProvider.LogError($"Error occurred while registering {PrefabNames.VesselHorn}");
    }
    else
    {
      LoggerProvider.LogMessage($"Registered custom item {PrefabNames.VesselHorn} (Horn of the Sea)");
    }
  }

  public override void OnRegister()
  {
    RegisterVehicleHorn();
  }
}