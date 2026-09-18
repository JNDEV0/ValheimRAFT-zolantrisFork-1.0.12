using System;
using UnityEngine;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

public class VesselHornComponent : MonoBehaviour
{
  private enum HornAction { None, Teleport, Recall, Attune }

  private HornAction _currentAction = HornAction.None;
  private float _holdTimer = 0f;
  private const float RequiredHoldDuration = 3.0f;
  private Vector3 _startPosition;
  private float _startHealth;

  private void Update()
  {
    var player = Player.m_localPlayer;
    if (player == null || player.IsDead())
    {
      if (_currentAction != HornAction.None) CancelAction();
      return;
    }

    if (!IsHoldingHorn(player))
    {
      if (_currentAction != HornAction.None) CancelAction();
      return;
    }

    if ((Chat.instance != null && Chat.instance.HasFocus()) ||
        Console.IsVisible() ||
        InventoryGui.IsVisible())
    {
      if (_currentAction != HornAction.None) CancelAction();
      return;
    }

    var leftHeld = ZInput.GetMouseButton(0);
    var rightHeld = ZInput.GetMouseButton(1);
    var middleHeld = ZInput.GetMouseButton(2);

    if (_currentAction == HornAction.None)
    {
      if (leftHeld)
      {
        StartAction(HornAction.Teleport, player);
      }
      else if (rightHeld)
      {
        StartAction(HornAction.Recall, player);
      }
      else if (middleHeld)
      {
        StartAction(HornAction.Attune, player);
      }
    }
    else
    {
      var stillHeld = (_currentAction == HornAction.Teleport && leftHeld) ||
                       (_currentAction == HornAction.Recall && rightHeld) ||
                       (_currentAction == HornAction.Attune && middleHeld);

      if (!stillHeld)
      {
        CancelAction();
        return;
      }

      // Interruption: Player moved or took damage
      if (Vector3.Distance(player.transform.position, _startPosition) > 0.3f ||
          player.GetHealth() < _startHealth - 0.1f)
      {
        CancelAction("Action interrupted by movement or damage!");
        return;
      }

      _holdTimer += Time.deltaTime;
      var progress = Mathf.Clamp01(_holdTimer / RequiredHoldDuration);
      UpdateProgressBar(progress);

      if (_holdTimer >= RequiredHoldDuration)
      {
        CompleteAction(player);
      }
    }
  }

  private static bool IsHoldingHorn(Player player)
  {
    var weapon = player.GetCurrentWeapon();
    if (weapon != null)
    {
      if (weapon.m_shared.m_name == "$item_vessel_horn") return true;
      if (weapon.m_dropPrefab != null && weapon.m_dropPrefab.name.StartsWith(PrefabNames.VesselHorn)) return true;
    }

    var right = player.GetRightItem();
    if (right != null)
    {
      if (right.m_shared.m_name == "$item_vessel_horn") return true;
      if (right.m_dropPrefab != null && right.m_dropPrefab.name.StartsWith(PrefabNames.VesselHorn)) return true;
    }

    return false;
  }

  private void StartAction(HornAction action, Player player)
  {
    _currentAction = action;
    _holdTimer = 0f;
    _startPosition = player.transform.position;
    _startHealth = player.GetHealth();

    UpdateProgressBar(0f);
  }

  private void CancelAction(string? message = null)
  {
    if (_currentAction != HornAction.None)
    {
      HideProgressBar();
      if (!string.IsNullOrEmpty(message) && Player.m_localPlayer != null)
      {
        Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);
      }
      _currentAction = HornAction.None;
      _holdTimer = 0f;
    }
  }

  private void CompleteAction(Player player)
  {
    HideProgressBar();
    var action = _currentAction;
    _currentAction = HornAction.None;
    _holdTimer = 0f;

    if (action == HornAction.Attune)
    {
      var targetId = VehicleRecallController.DetectAimedOrCurrentVehicle(player);
      if (targetId != 0)
      {
        VehicleRecallController.AttunePlayerToVehicle(player, targetId);
      }
      else
      {
        player.Message(MessageHud.MessageType.Center, "No vessel found to attune!");
      }
      return;
    }

    var vehicleId = VehicleRecallController.ResolveTargetVehicle(player);
    if (vehicleId == 0)
    {
      player.Message(MessageHud.MessageType.Center, "No vessel found to summon!");
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

  private void UpdateProgressBar(float progress)
  {
    if (Hud.instance == null || Hud.instance.m_actionBarRoot == null) return;

    Hud.instance.m_actionBarRoot.SetActive(true);

    if (Hud.instance.m_actionProgress != null)
    {
      Hud.instance.m_actionProgress.SetValue(progress);
    }

    if (Hud.instance.m_actionName != null)
    {
      var title = _currentAction switch
      {
        HornAction.Teleport => "Teleporting to Vessel...",
        HornAction.Recall => "Recalling Vessel to Aim...",
        HornAction.Attune => "Attuning to Vessel...",
        _ => ""
      };
      Hud.instance.m_actionName.text = title;
    }
  }

  private void HideProgressBar()
  {
    if (Hud.instance != null && Hud.instance.m_actionBarRoot != null)
    {
      Hud.instance.m_actionBarRoot.SetActive(false);
    }
  }

  private void OnDisable()
  {
    CancelAction();
  }

  private void OnDestroy()
  {
    CancelAction();
  }
}