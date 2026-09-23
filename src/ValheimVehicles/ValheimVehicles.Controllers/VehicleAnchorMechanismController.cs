#region

  using System;
  using UnityEngine;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.Constants;
  using ValheimVehicles.SharedScripts;

#endregion

  namespace ValheimVehicles.Controllers;

  /// <summary>
  /// An integration level component, meant to work with Valheim specific content / apis
  /// </summary>
  public class VehicleAnchorMechanismController : AnchorMechanismController
  {
    public const float maxAnchorDistance = 40f;

    public static void SyncHudAnchorValues()
    {
      HideAnchorTimer = HudConfig.HudAnchorMessageTimer.Value;
      HasAnchorTextHud = HudConfig.HudAnchorTextAboveAnchors.Value;
      foreach (var anchorMechanismController in Instances)
        anchorMechanismController.anchorTextSize =
          HudConfig.HudAnchorTextSize.Value;
    }

    public override void Awake()
    {
      base.Awake();
      CanUseHotkeys = false;
    }

    public VehicleMovementController? MovementController;

    public override void FixedUpdate()
    {
      base.FixedUpdate();
      if (currentState == AnchorState.Lowering) UpdateDistanceToGround();
    }

    public float GetDistanceToGround()
    {
      if (!this || !transform) return 0f;
      var worldPos = (anchorRopeAttachStartPoint != null && anchorRopeAttachStartPoint)
        ? anchorRopeAttachStartPoint.position
        : transform.position;

      var groundHeight = ZoneSystem.instance != null
        ? ZoneSystem.instance.GetGroundHeight(worldPos)
        : 0f;

      return worldPos.y - groundHeight;
    }

    public void UpdateDistanceToGround()
    {
      var dtg = GetDistanceToGround();
      anchorDropDistance = Mathf.Clamp(dtg, 1f, maxAnchorDistance);
    }

    public static string GetCurrentStateTextStatic(AnchorState anchorState, bool isLandVehicle)
    {
      if (isLandVehicle)
      {
        return anchorState == AnchorState.Anchored ? ModTranslations.AnchorPrefab_breakingText : ModTranslations.AnchorPrefab_idleText;
      }

      return anchorState switch
      {
        AnchorState.Idle => "Idle",
        AnchorState.Lowering => ModTranslations.AnchorPrefab_loweringText,
        AnchorState.Anchored => ModTranslations.AnchorPrefab_anchoredText,
        AnchorState.Reeling => ModTranslations.AnchorPrefab_reelingText,
        AnchorState.Recovered => ModTranslations.AnchorPrefab_RecoveredAnchorText,
        _ => throw new ArgumentOutOfRangeException()
      };
    }

    public bool IsLandVehicle()
    {
      return MovementController != null && MovementController.Manager != null && MovementController.Manager.IsLandVehicle;
    }

    public override string GetCurrentStateText()
    {
      return GetCurrentStateTextStatic(currentState, IsLandVehicle());
    }

    /// <summary>
    /// Catch all if the anchor is not near the ground when it becomes anchored, move it down to the ground.
    /// </summary>
    public void UpdateAnchorPositionIfNotNearGround()
    {
      var deltaGround = GetDistanceToGround();
      if (!(deltaGround > 2)) return;
      var clampedDepth = Mathf.Clamp(deltaGround, 1f, maxAnchorDistance);
      var newPos = GetAnchorStartLocalPosition();
      newPos.y -= clampedDepth;
      if (anchorTransform != null)
      {
        anchorTransform.localPosition = newPos;
      }
      UpdateRopeVisual();
    }

    public override void OnAnchorStateChange(AnchorState newState)
    {
      // No callbacks for anchor when flying
      if (MovementController != null && MovementController.IsFlying())
      {
        return;
      }

      switch (newState)
      {
        case AnchorState.Idle:
          break;
        case AnchorState.Lowering:
          break;
        case AnchorState.Anchored:
          UpdateAnchorPositionIfNotNearGround();
          break;
        case AnchorState.Reeling:
          break;
        case AnchorState.Recovered:
          if (anchorTransform != null)
          {
            anchorTransform.localPosition = GetAnchorStartLocalPosition();
            anchorTransform.localRotation = Quaternion.identity;
          }
          UpdateRopeVisual();
          break;
        default:
          throw new ArgumentOutOfRangeException(nameof(newState), newState, null);
      }

      if (MovementController != null && MovementController.m_nview != null && MovementController.m_nview.IsOwner())
      {
        if (MovementController.vehicleAnchorState != newState)
        {
          MovementController.SendSetAnchor(newState);
        }
      }
    }
  }