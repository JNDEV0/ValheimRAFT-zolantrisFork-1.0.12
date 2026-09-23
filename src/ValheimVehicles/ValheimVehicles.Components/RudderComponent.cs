using UnityEngine;

namespace ValheimVehicles.Components;

public enum RudderTier
{
  Basic = 1,
  Standard = 2,
  Advanced = 3
}

public class RudderComponent : MonoBehaviour
{
  public Transform PivotPoint;
  public RudderTier tier = RudderTier.Basic;
  public float maxRotation = 45;
  public float minRotation = -45;
  public float initialRotation = 0;

  // Tier-derived properties
  public float MaxTurnAngle => tier switch
  {
    RudderTier.Basic => 30f,
    RudderTier.Standard => 60f,
    RudderTier.Advanced => 89f,
    _ => 30f
  };

  public float TurnSpeed => tier switch
  {
    RudderTier.Basic => 0.33f,
    RudderTier.Standard => 0.66f,
    RudderTier.Advanced => 1.0f,
    _ => 0.33f
  };

  public float RowSpeed => tier switch
  {
    RudderTier.Basic => 5f,
    RudderTier.Standard => 10f,
    RudderTier.Advanced => 15f,
    _ => 5f
  };
}