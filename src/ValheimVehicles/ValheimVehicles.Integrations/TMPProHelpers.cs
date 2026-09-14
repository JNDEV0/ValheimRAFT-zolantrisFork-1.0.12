using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using ValheimVehicles.Prefabs;

namespace ValheimVehicles.ValheimVehicles.Integrations;

public class TMPProHelpers
{
  public static void Init()
  {
    var liberation = LoadValheimVehicleAssets.LiberationSansFontAsset;
    if (liberation)
    {
      TMP_Settings.defaultFontAsset = liberation;

      if (TMP_Settings.fallbackFontAssets == null)
        TMP_Settings.fallbackFontAssets = new List<TMP_FontAsset>();

      if (!TMP_Settings.fallbackFontAssets.Contains(liberation))
        TMP_Settings.fallbackFontAssets.Add(liberation);

      if (liberation.fallbackFontAssetTable == null)
        liberation.fallbackFontAssetTable = new List<TMP_FontAsset>();

      // Register all existing TMP_FontAssets in the game (e.g. AveriaSerifLibre, Norse, etc.) as fallbacks
      try
      {
        var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        foreach (var font in allFonts)
        {
          if (font != null && font != liberation)
          {
            if (font.name == "LegacyRuntime" || font.name.StartsWith("Legacy"))
              continue;

            if (!liberation.fallbackFontAssetTable.Contains(font))
              liberation.fallbackFontAssetTable.Add(font);

            if (!TMP_Settings.fallbackFontAssets.Contains(font))
              TMP_Settings.fallbackFontAssets.Add(font);
          }
        }
      }
      catch (Exception e)
      {
        Debug.LogWarning($"[TMP] Could not gather game font assets: {e.Message}");
      }

      // Generate dynamic Arial fallback for extended unicode coverage
      try
      {
        var arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (arial)
        {
          var arialSdf = TMP_FontAsset.CreateFontAsset(arial);
          if (arialSdf)
          {
            if (!liberation.fallbackFontAssetTable.Contains(arialSdf))
              liberation.fallbackFontAssetTable.Add(arialSdf);

            if (!TMP_Settings.fallbackFontAssets.Contains(arialSdf))
              TMP_Settings.fallbackFontAssets.Add(arialSdf);
          }
        }
      }
      catch { }

      Debug.Log("[TMP] Default font set to LiberationSans with system and game fallbacks");
    }
  }
}
