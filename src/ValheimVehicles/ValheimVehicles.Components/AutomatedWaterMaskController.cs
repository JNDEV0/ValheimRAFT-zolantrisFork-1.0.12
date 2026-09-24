using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using ValheimVehicles.Controllers;
using ValheimVehicles.Helpers;
using ValheimVehicles.Prefabs;
using ValheimVehicles.Shared.Constants;
using Zolantris.Shared;

namespace ValheimVehicles.Components;

public class AutomatedWaterMaskController : MonoBehaviour
{
  public static readonly List<AutomatedWaterMaskController> ActiveInstances = [];
  private readonly HashSet<Character> _charactersInside = [];

  private VehicleManager? _manager;
  private GameObject? _maskContainer;
  private Material? _maskMaterial;

  public bool IsActive => _maskContainer != null && _maskContainer.activeSelf;

  public void Awake()
  {
    _manager = GetComponent<VehicleManager>() ?? GetComponentInParent<VehicleManager>();
  }

  public void OnEnable()
  {
    if (!ActiveInstances.Contains(this)) ActiveInstances.Add(this);
  }

  public void OnDisable()
  {
    ActiveInstances.Remove(this);
    _charactersInside.Clear();
  }

  public void OnDestroy()
  {
    ActiveInstances.Remove(this);
    DestroyWaterMask();
  }

  public Material GetWaterMaskMaterial()
  {
    if (_maskMaterial != null) return _maskMaterial;

    if (LoadValheimAssets.waterMask != null)
    {
      var renderer = LoadValheimAssets.waterMask.GetComponent<Renderer>();
      if (renderer != null && renderer.sharedMaterial != null)
      {
        _maskMaterial = new Material(renderer.sharedMaterial)
        {
          name = "AutomatedWaterMask_Material"
        };
        return _maskMaterial;
      }
    }

    if (LoadValheimVehicleAssets.TransparentDepthMaskMaterial != null)
    {
      _maskMaterial = new Material(LoadValheimVehicleAssets.TransparentDepthMaskMaterial);
      return _maskMaterial;
    }

    var shader = LoadValheimAssets.waterMaskShader ?? Shader.Find("Custom/InvertWaterMask") ?? Shader.Find("Standard");
    _maskMaterial = new Material(shader);
    return _maskMaterial;
  }

  public void BuildWaterMask()
  {
    DestroyWaterMask();

    if (_manager == null || _manager.PiecesController == null) return;

    var convexHull = _manager.PiecesController.convexHullComponent;
    if (convexHull == null) return;

    // Ensure hull geometry is current
    if (convexHull.convexHullMeshes.Count == 0)
    {
      _manager.PiecesController.ForceRebuildBounds();
    }

    if (convexHull.convexHullMeshes.Count == 0) return;

    _maskContainer = new GameObject("AutomatedWaterMask")
    {
      layer = LayerHelpers.IgnoreRaycastLayer
    };
    _maskContainer.transform.SetParent(_manager.PiecesController.transform, false);
    _maskContainer.transform.localPosition = Vector3.zero;
    _maskContainer.transform.localRotation = Quaternion.identity;
    _maskContainer.transform.localScale = Vector3.one;

    var mat = GetWaterMaskMaterial();

    for (var i = 0; i < convexHull.convexHullMeshes.Count; i++)
    {
      var hullGo = convexHull.convexHullMeshes[i];
      if (hullGo == null) continue;

      var meshCollider = hullGo.GetComponent<MeshCollider>();
      var meshFilter = hullGo.GetComponent<MeshFilter>();
      var sourceMesh = meshCollider != null && meshCollider.sharedMesh != null
        ? meshCollider.sharedMesh
        : (meshFilter != null ? meshFilter.sharedMesh : null);

      if (sourceMesh == null) continue;

      var origVerts = sourceMesh.vertices;
      var origNormals = sourceMesh.normals;
      if (origNormals == null || origNormals.Length != origVerts.Length)
      {
        sourceMesh.RecalculateNormals();
        origNormals = sourceMesh.normals;
      }

      var centroid = sourceMesh.bounds.center;
      var insetVerts = new Vector3[origVerts.Length];

      for (var v = 0; v < origVerts.Length; v++)
      {
        var vert = origVerts[v];
        var normal = origNormals[v];

        // Safeguard narrow sections from crossing over centroid
        var maxInset = Vector3.Distance(vert, centroid) * 0.45f;
        var insetDist = Mathf.Min(0.25f, maxInset);

        insetVerts[v] = vert - normal * insetDist;
      }

      var maskMesh = new Mesh
      {
        name = $"AutomatedWaterMask_Mesh_{i}",
        vertices = insetVerts,
        triangles = sourceMesh.triangles
      };
      maskMesh.RecalculateNormals();
      maskMesh.RecalculateBounds();

      var subObj = new GameObject($"MaskSub_{i}")
      {
        layer = LayerHelpers.IgnoreRaycastLayer
      };
      subObj.transform.SetParent(_maskContainer.transform, false);
      subObj.transform.localPosition = hullGo.transform.localPosition;
      subObj.transform.localRotation = hullGo.transform.localRotation;
      subObj.transform.localScale = hullGo.transform.localScale;

      var filter = subObj.AddComponent<MeshFilter>();
      filter.sharedMesh = maskMesh;

      var renderer = subObj.AddComponent<MeshRenderer>();
      renderer.sharedMaterial = mat;
      renderer.lightProbeUsage = LightProbeUsage.Off;
      renderer.receiveShadows = false;
      renderer.shadowCastingMode = ShadowCastingMode.Off;
      renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

      // Convex trigger for accurate underwater character detection
      var triggerCollider = subObj.AddComponent<MeshCollider>();
      triggerCollider.convex = true;
      triggerCollider.isTrigger = true;
      triggerCollider.sharedMesh = maskMesh;

      var triggerBridge = subObj.AddComponent<WaterMaskTriggerBridge>();
      triggerBridge.ParentController = this;
    }
  }

  public void DestroyWaterMask()
  {
    if (_maskContainer != null)
    {
      Destroy(_maskContainer);
      _maskContainer = null;
    }
    _charactersInside.Clear();
  }

  public void OnCharacterEnter(Character c)
  {
    if (c != null && !_charactersInside.Contains(c))
      _charactersInside.Add(c);
  }

  public void OnCharacterExit(Character c)
  {
    if (c != null)
      _charactersInside.Remove(c);
  }

  public bool ContainsCharacter(Character c)
  {
    return _charactersInside.Contains(c);
  }

  public static bool IsCharacterInAnyWaterMask(Character character)
  {
    if (character == null) return false;

    for (var i = 0; i < ActiveInstances.Count; i++)
    {
      var inst = ActiveInstances[i];
      if (inst == null || !inst.IsActive) continue;

      if (inst.ContainsCharacter(character))
        return true;

      // Fallback: character standing inside the hull of this active watermasked vehicle
      if (inst._manager != null && WaterZoneUtils.IsOnboard(character))
      {
        if (WaterZoneUtils.IsWithinHull(character))
          return true;
      }
    }

    return false;
  }
}

public class WaterMaskTriggerBridge : MonoBehaviour
{
  public AutomatedWaterMaskController? ParentController;

  public void OnTriggerEnter(Collider other)
  {
    var character = other.GetComponent<Character>();
    if (character != null && ParentController != null)
      ParentController.OnCharacterEnter(character);
  }

  public void OnTriggerExit(Collider other)
  {
    var character = other.GetComponent<Character>();
    if (character != null && ParentController != null)
      ParentController.OnCharacterExit(character);
  }
}
