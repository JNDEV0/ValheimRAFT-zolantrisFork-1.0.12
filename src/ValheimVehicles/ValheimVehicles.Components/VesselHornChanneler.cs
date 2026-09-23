using ValheimVehicles.Helpers;
using System;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.Controllers;
using ValheimVehicles.Propulsion.Rudder;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

public static class VesselHornChanneler
{
  public enum HornAction { None, Teleport, Attune }

  public static bool IsChanneling { get; private set; }
  public static float ChannelProgress { get; private set; }
  public static string ChannelActionName { get; private set; } = "";
  public static HornAction CurrentAction { get; private set; } = HornAction.None;

  private static float _holdTimer = 0f;
  private const float RequiredHoldDuration = 3.0f;
  private static Vector3 _startPosition;
  private static float _startHealth;
  private static int _targetVehicleId;
  private static float _teleportCooldownUntil = 0f;
  private static float _attuneCooldownUntil = 0f;
  private static bool _awaitingInputRelease = false;

  public static bool IsHoldingVesselHorn(Player? player)
  {
    if (player == null) return false;

    var right = player.GetRightItem();
    if (right != null)
    {
      if (right.m_shared != null)
      {
        var name = right.m_shared.m_name;
        if (name == "$item_vessel_horn" || name == "Horn of Loki" || name == "Horn of the Sea" || name.Contains("vessel_horn"))
          return true;
      }
      if (right.m_dropPrefab != null && right.m_dropPrefab.name.Contains("vessel_horn"))
        return true;
    }

    var weapon = player.GetCurrentWeapon();
    if (weapon != null)
    {
      if (weapon.m_shared != null)
      {
        var name = weapon.m_shared.m_name;
        if (name == "$item_vessel_horn" || name == "Horn of Loki" || name == "Horn of the Sea" || name.Contains("vessel_horn"))
          return true;
      }
      if (weapon.m_dropPrefab != null && weapon.m_dropPrefab.name.Contains("vessel_horn"))
        return true;
    }

    return false;
  }

  public static SteeringWheelComponent? GetTargetSteeringWheel(Player player, float maxDistance = 3.5f)
  {
    if (player == null) return null;

    // 1. Check player's hover object
    try
    {
      var hover = player.GetHoverObject();
      if (hover != null)
      {
        var wheel = hover.GetComponentInParent<SteeringWheelComponent>() ??
                    hover.GetComponentInChildren<SteeringWheelComponent>();
        if (wheel != null && Vector3.Distance(player.transform.position, wheel.transform.position) <= maxDistance + 1.2f)
        {
          return wheel;
        }
      }
    }
    catch { }

    // 2. Raycast from camera or eye point
    var cam = GameCamera.instance ? GameCamera.instance.transform : null;
    var rayOrigin = cam != null ? cam.position : player.GetEyePoint();
    var rayDir = cam != null ? cam.forward : player.GetLookDir();

    var hits = Physics.RaycastAll(rayOrigin, rayDir, maxDistance + 2.5f);
    Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

    foreach (var hit in hits)
    {
      if (hit.collider == null || hit.collider.isTrigger) continue;
      if (hit.collider.transform.root == player.transform.root) continue;

      if (Vector3.Distance(player.transform.position, hit.point) > maxDistance + 0.8f) continue;

      var wheel = hit.collider.GetComponentInParent<SteeringWheelComponent>() ??
                  hit.collider.GetComponentInChildren<SteeringWheelComponent>();
      if (wheel != null) return wheel;

      var piece = hit.collider.GetComponentInParent<Piece>();
      if (piece != null && (piece.name.Contains("ShipSteeringWheel") || piece.m_name == "$valheim_vehicles_wheel"))
      {
        var pieceWheel = piece.GetComponentInChildren<SteeringWheelComponent>() ??
                         piece.GetComponentInParent<SteeringWheelComponent>();
        if (pieceWheel != null) return pieceWheel;
      }
    }

    return null;
  }

    public static bool HasVesselHornInInventory(Player? player)
  {
    if (player == null || player.m_inventory == null) return false;
    foreach (var item in player.m_inventory.GetAllItems())
    {
      if (item != null && item.m_shared != null)
      {
        var name = item.m_shared.m_name;
        if (name == "$item_vessel_horn" || name == "Horn of Loki" || name == "Horn of the Sea" || name.Contains("vessel_horn"))
          return true;
      }
    }
    return false;
  }

  public static void UpdateLocalPlayer(Player player)
  {
    if (player == null || player.IsDead() || player.IsTeleporting() || player.InIntro())
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
        InventoryGui.IsVisible() ||
        StoreGui.IsVisible() ||
        Menu.IsVisible())
    {
      if (IsChanneling) CancelAction(player);
      return;
    }

    // Never channel or poll horn inputs while sprinting/running
    if (player.m_run || player.IsRunning())
    {
      if (IsChanneling) CancelAction(player);
      return;
    }

    // Strictly check mouse buttons to avoid conflicts with Shift / AltPlace
    var leftHeld = Input.GetMouseButton(0) || ZInput.GetMouseButton(0);
    var leftDown = Input.GetMouseButtonDown(0) || ZInput.GetMouseButtonDown(0);
    var rightHeld = Input.GetMouseButton(1) || ZInput.GetMouseButton(1);
    var rightDown = Input.GetMouseButtonDown(1) || ZInput.GetMouseButtonDown(1);

    // Failsafe: Input release debouncing after an action completes
    if (_awaitingInputRelease)
    {
      if (!leftHeld && !rightHeld)
      {
        _awaitingInputRelease = false;
      }
      else
      {
        return; // Ignore held button from previous channel
      }
    }

    if (!IsChanneling)
    {
      if (leftHeld)
      {
        // Teleport cooldown check
        if (Time.time < _teleportCooldownUntil)
        {
          if (leftDown)
          {
            var remaining = Mathf.CeilToInt(_teleportCooldownUntil - Time.time);
            player.Message(MessageHud.MessageType.Center, $"Horn on cooldown ({remaining}s)");
          }
          return;
        }

        var targetId = VehicleRecallController.ResolveTargetVehicle(player);
        if (targetId == 0)
        {
          if (leftDown)
          {
            player.Message(MessageHud.MessageType.Center, "Bind to a boat first");
          }
          return;
        }

        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselHorn] Left click detected -> starting Teleport to Boat #{targetId} channel");
        StartAction(HornAction.Teleport, "Teleporting to Boat...", player, targetId);
      }
      else if (rightHeld)
      {
        if (Time.time < _attuneCooldownUntil)
        {
          return;
        }

        var interactDist = player.m_maxInteractDistance > 0 ? player.m_maxInteractDistance : 3.5f;
        var wheel = GetTargetSteeringWheel(player, interactDist);
        if (wheel == null)
        {
          if (rightDown)
          {
            var wheelName = Localization.instance != null
              ? Localization.instance.Localize("$valheim_vehicles_wheel")
              : "Vehicle Wheel";
            player.Message(MessageHud.MessageType.Center, $"Must bind at {wheelName}");
          }
          return;
        }

        var vehicleId = VehicleRecallController.GetVehicleIdFromWheel(wheel);
        if (vehicleId == 0)
        {
          if (rightDown)
          {
            player.Message(MessageHud.MessageType.Center, "Wheel is not attached to a boat!");
          }
          return;
        }

        if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselHorn] Right click at wheel detected -> starting Bind Boat #{vehicleId} channel");
        StartAction(HornAction.Attune, "Binding to Boat...", player, vehicleId);
      }
    }
    else
    {
      var stillHeld = (CurrentAction == HornAction.Teleport && leftHeld) ||
                      (CurrentAction == HornAction.Attune && rightHeld);

      if (!stillHeld)
      {
        CancelAction(player);
        return;
      }

      // Check movement or damage
      if (Vector3.Distance(player.transform.position, _startPosition) > 0.4f)
      {
        CancelAction(player);
        return;
      }

      if (player.GetHealth() < _startHealth - 0.1f)
      {
        CancelAction(player);
        return;
      }

      var requiredDuration = VehicleGlobalConfig.HornChannelDurationSeconds?.Value ?? RequiredHoldDuration;
      _holdTimer += Time.deltaTime;
      ChannelProgress = Mathf.Clamp01(_holdTimer / requiredDuration);

      if (_holdTimer >= requiredDuration)
      {
        CompleteAction(player);
      }
    }
  }

  private static void StartAction(HornAction action, string actionName, Player player, int targetVehicleId)
  {
    IsChanneling = true;
    CurrentAction = action;
    ChannelActionName = actionName;
    ChannelProgress = 0f;
    _holdTimer = 0f;
    _startPosition = player.transform.position;
    _startHealth = player.GetHealth();
    _targetVehicleId = targetVehicleId;

    if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselHorn] Channeling {action} ({actionName}) started for vessel #{targetVehicleId} at {_startPosition}");

    try
    {
      player.StartEmote("toast", true);
    }
    catch (Exception)
    {
      try { player.StartEmote("cheer", false); } catch { }
    }
  }

  public static void CancelAction(Player? player, string? message = null)
  {
    if (IsChanneling)
    {
      if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselHorn] Channel {CurrentAction} cancelled silently ({message ?? "released"}).");

      IsChanneling = false;
      CurrentAction = HornAction.None;
      ChannelProgress = 0f;
      _holdTimer = 0f;

      if (Hud.instance != null && Hud.instance.m_actionBarRoot != null)
      {
        Hud.instance.m_actionBarRoot.SetActive(false);
      }

      if (player != null)
      {
        try { player.StopEmote(); } catch { }
      }
    }
  }

  private static void CompleteAction(Player player)
  {
    var action = CurrentAction;
    var targetVehicleId = _targetVehicleId;

    if (LoopTracker.Enabled) LoggerProvider.LogInfo($"[VesselHorn] Channel 3.0s complete for {action} (Target: #{targetVehicleId})! Executing action now.");

    IsChanneling = false;
    CurrentAction = HornAction.None;
    ChannelProgress = 0f;
    _holdTimer = 0f;

    if (Hud.instance != null && Hud.instance.m_actionBarRoot != null)
    {
      Hud.instance.m_actionBarRoot.SetActive(false);
    }

    try { player.StopEmote(); } catch { }

    _awaitingInputRelease = true;

    if (action == HornAction.Attune)
    {
      _attuneCooldownUntil = Time.time + 2.0f;
      if (targetVehicleId != 0)
      {
        VehicleRecallController.AttunePlayerToVehicle(player, targetVehicleId);
      }
      else
      {
        LoggerProvider.LogWarning("[VesselHorn] Bind action failed: No boat found to bind!");
        player.Message(MessageHud.MessageType.Center, "No boat found to bind!");
      }
      return;
    }

    if (action == HornAction.Teleport)
    {
      var cooldown = VehicleGlobalConfig.HornTeleportCooldownSeconds?.Value ?? 10.0f;
      _teleportCooldownUntil = Time.time + cooldown;
      if (targetVehicleId != 0)
      {
        VehicleRecallController.TeleportPlayerToVehicle(player, targetVehicleId);
      }
      else
      {
        LoggerProvider.LogWarning("[VesselHorn] Teleport action failed: No boat found in world!");
        player.Message(MessageHud.MessageType.Center, "Bind to a boat first");
      }
    }
  }
}
