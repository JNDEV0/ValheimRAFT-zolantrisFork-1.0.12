using System;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

public static class VesselHornChanneler
{
  public enum HornAction { None, Teleport, Recall, Attune }

  public static bool IsChanneling { get; private set; }
  public static float ChannelProgress { get; private set; }
  public static string ChannelActionName { get; private set; } = "";
  public static HornAction CurrentAction { get; private set; } = HornAction.None;

  private static float _holdTimer = 0f;
  private const float RequiredHoldDuration = 3.0f;
  private static Vector3 _startPosition;
  private static float _startHealth;

  public static bool IsHoldingVesselHorn(Player? player)
  {
    if (player == null) return false;

    var right = player.GetRightItem();
    if (right != null)
    {
      if (right.m_shared.m_name == "$item_vessel_horn") return true;
      if (right.m_dropPrefab != null && right.m_dropPrefab.name.StartsWith(PrefabNames.VesselHorn)) return true;
    }

    var weapon = player.GetCurrentWeapon();
    if (weapon != null)
    {
      if (weapon.m_shared.m_name == "$item_vessel_horn") return true;
      if (weapon.m_dropPrefab != null && weapon.m_dropPrefab.name.StartsWith(PrefabNames.VesselHorn)) return true;
    }

    return false;
  }

  public static void UpdateLocalPlayer(Player player)
  {
    if (player == null || player.IsDead())
    {
      if (IsChanneling) CancelAction(player);
      return;
    }

    if (!IsHoldingVesselHorn(player))
    {
      if (IsChanneling) CancelAction(player);
      return;
    }

    if ((Chat.instance != null && Chat.instance.HasFocus()) ||
        Console.IsVisible() ||
        InventoryGui.IsVisible())
    {
      if (IsChanneling) CancelAction(player);
      return;
    }

    var leftHeld = ZInput.GetMouseButton(0) || ZInput.GetButton("Attack");
    var rightHeld = ZInput.GetMouseButton(1) || ZInput.GetButton("Block") || ZInput.GetButton("AltPlace");
    var middleHeld = ZInput.GetMouseButton(2);

    if (!IsChanneling)
    {
      if (leftHeld)
      {
        StartAction(HornAction.Teleport, "Teleporting to Boat...", player);
      }
      else if (rightHeld)
      {
        StartAction(HornAction.Recall, "Recalling Boat...", player);
      }
      else if (middleHeld)
      {
        StartAction(HornAction.Attune, "Binding to Boat...", player);
      }
    }
    else
    {
      var stillHeld = (CurrentAction == HornAction.Teleport && leftHeld) ||
                      (CurrentAction == HornAction.Recall && rightHeld) ||
                      (CurrentAction == HornAction.Attune && middleHeld);

      if (!stillHeld)
      {
        CancelAction(player);
        return;
      }

      // Check movement or damage
      if (Vector3.Distance(player.transform.position, _startPosition) > 0.4f)
      {
        CancelAction(player, "Action cancelled by movement!");
        return;
      }

      if (player.GetHealth() < _startHealth - 0.1f)
      {
        CancelAction(player, "Action cancelled by damage!");
        return;
      }

      _holdTimer += Time.deltaTime;
      ChannelProgress = Mathf.Clamp01(_holdTimer / RequiredHoldDuration);

      if (_holdTimer >= RequiredHoldDuration)
      {
        CompleteAction(player);
      }
    }
  }

  private static void StartAction(HornAction action, string actionName, Player player)
  {
    IsChanneling = true;
    CurrentAction = action;
    ChannelActionName = actionName;
    ChannelProgress = 0f;
    _holdTimer = 0f;
    _startPosition = player.transform.position;
    _startHealth = player.GetHealth();

    try
    {
      player.StartEmote("cheer", false);
    }
    catch (Exception)
    {
      // ignored
    }
  }

  public static void CancelAction(Player? player, string? message = null)
  {
    if (IsChanneling)
    {
      IsChanneling = false;
      CurrentAction = HornAction.None;
      ChannelProgress = 0f;
      _holdTimer = 0f;

      if (player != null)
      {
        try { player.StopEmote(); } catch { }
        if (!string.IsNullOrEmpty(message))
        {
          player.Message(MessageHud.MessageType.Center, message);
        }
      }
    }
  }

  private static void CompleteAction(Player player)
  {
    var action = CurrentAction;
    IsChanneling = false;
    CurrentAction = HornAction.None;
    ChannelProgress = 0f;
    _holdTimer = 0f;

    try { player.StopEmote(); } catch { }

    if (action == HornAction.Attune)
    {
      var targetId = VehicleRecallController.DetectAimedOrCurrentVehicle(player);
      if (targetId != 0)
      {
        VehicleRecallController.AttunePlayerToVehicle(player, targetId);
      }
      else
      {
        player.Message(MessageHud.MessageType.Center, "No boat found to bind!");
      }
      return;
    }

    var vehicleId = VehicleRecallController.ResolveTargetVehicle(player);
    if (vehicleId == 0)
    {
      player.Message(MessageHud.MessageType.Center, "No boat found to summon!");
      return;
    }

    switch (action)
    {
      case HornAction.Teleport:
        VehicleRecallController.TeleportPlayerToVehicle(player, vehicleId);
        break;
      case HornAction.Recall:
        VehicleRecallController.RecallVehicleToCrosshair(player, vehicleId);
        break;
    }
  }
}