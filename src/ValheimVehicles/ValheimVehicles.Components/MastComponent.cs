using ValheimVehicles.BepInExConfig;
using System;
using UnityEngine;

namespace ValheimVehicles.Components;

public class MastComponent : MonoBehaviour
{
  public GameObject? m_sailObject;

  public Cloth? m_sailCloth;

  public bool m_allowSailRotation = false;
  public Transform? m_rotationTransform = null;

  public bool m_allowSailShrinking = true;

  public bool m_disableCloth;

  public float m_sailWidthScale = 1.4f;

  public Vector3 m_initialSailLocalPos = Vector3.zero;
  public float m_sailTopLocalY = 0f;
  public bool m_hasInitializedSailPositions = false;

  public void InitSailPositions()
  {
    if (m_hasInitializedSailPositions || m_sailObject == null || m_sailObject == gameObject) return;
    m_initialSailLocalPos = m_sailObject.transform.localPosition;

    var mf = m_sailObject.GetComponentInChildren<MeshFilter>(true);
    var smr = m_sailObject.GetComponentInChildren<SkinnedMeshRenderer>(true);
    var mesh = mf != null ? mf.sharedMesh : (smr != null ? smr.sharedMesh : null);
    var targetTransform = mf != null ? mf.transform : (smr != null ? smr.transform : null);

    if (mesh != null && targetTransform != null)
    {
      var matrix = m_sailObject.transform.worldToLocalMatrix * targetTransform.localToWorldMatrix;
      var p1 = matrix.MultiplyPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.max.y, mesh.bounds.center.z));
      var p2 = matrix.MultiplyPoint(new Vector3(mesh.bounds.center.x, mesh.bounds.min.y, mesh.bounds.center.z));
      m_sailTopLocalY = Mathf.Max(p1.y, p2.y);
    }

    if (m_sailTopLocalY <= 0.1f)
    {
      var renderers = m_sailObject.GetComponentsInChildren<Renderer>(true);
      if (renderers.Length > 0)
      {
        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        var p = m_sailObject.transform.InverseTransformPoint(b.center + Vector3.up * b.extents.y);
        m_sailTopLocalY = Mathf.Max(0.5f, p.y);
      }
      else
      {
        m_sailTopLocalY = 5f;
      }
    }

    m_hasInitializedSailPositions = true;
  }

  public float GetVerticalOffset()
  {
    var name = gameObject.name;
    if (name.IndexOf("karve", StringComparison.OrdinalIgnoreCase) >= 0)
    {
      return PropulsionConfig.KarveSailVerticalOffset != null ? PropulsionConfig.KarveSailVerticalOffset.Value : 0.25f;
    }
    return PropulsionConfig.SailVerticalOffset?.Value ?? 0f;
  }

  public void Start()
  {
    InitSailPositions();
    if (m_hasInitializedSailPositions && m_sailObject != null && m_sailObject != gameObject)
    {
      var verticalOffset = GetVerticalOffset();
      var pos = m_initialSailLocalPos;
      pos.y += verticalOffset;
      m_sailObject.transform.localPosition = pos;
    }
  }

  // for custom masts. Other masts do not support this. We may need to add a selector to make this cleaner.
  public void Awake()
  {
    m_rotationTransform = transform.Find("rotational_yard");
  }
}