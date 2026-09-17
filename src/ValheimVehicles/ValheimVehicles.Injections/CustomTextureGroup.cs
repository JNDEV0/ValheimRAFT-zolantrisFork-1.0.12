using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jotunn.Utils;
using ValheimVehicles.Compat;
using ValheimVehicles.BepInExConfig;
using Paths = BepInEx.Paths;
using Logger = Jotunn.Logger;

namespace ValheimVehicles.Injections;

public class CustomTextureGroup
{
  private static readonly string[] m_validExtensions =
    new string[3] { ".png", ".jpg", ".jpeg" };

  private static Dictionary<string, CustomTextureGroup> m_groups = new();

  private Dictionary<string, CustomTexture> m_textureLookUp = new();

  private Dictionary<int, CustomTexture> m_textureHashLookUp = new();

  private List<CustomTexture> m_textures = new();

  public List<CustomTexture> Textures => m_textures;

  public CustomTexture GetTextureByHash(int hash)
  {
    CustomTexture texture;
    return m_textureHashLookUp.TryGetValue(hash, out texture) ? texture : null;
  }

  public CustomTexture GetTextureByName(string name)
  {
    CustomTexture texture;
    return m_textureLookUp.TryGetValue(name, out texture) ? texture : null;
  }

  public static CustomTextureGroup? Get(string groupName)
  {
    if (m_groups.TryGetValue(groupName, out var group)) return group;

    return null;
  }

  public static string[] GetFiles(string groupName, string modFolderName)
  {
    var folderPath = Path.IsPathRooted(modFolderName)
      ? modFolderName
      : Path.Combine(Paths.PluginPath, modFolderName);

    if (!Directory.Exists(folderPath))
      return Array.Empty<string>();

    var assetDirectory = Path.Combine(folderPath, "Assets");
    if (!Directory.Exists(assetDirectory))
      return Array.Empty<string>();

    var targetDir = Path.Combine(assetDirectory, groupName);
    if (!Directory.Exists(targetDir))
      return Array.Empty<string>();

    return Directory.GetFiles(targetDir);
  }

  public static CustomTextureGroup Load(string groupName)
  {
    if (m_groups.TryGetValue(groupName, out var group)) return group;

    group = new CustomTextureGroup();
    m_groups.Add(groupName, group);

    var files = Array.Empty<string>();

    // 1. Check directory where the mod's DLL is located (works for all mod managers: Gale, r2modman, Thunderstore, manual)
    try
    {
      var asmDir = Path.GetDirectoryName(typeof(CustomTextureGroup).Assembly.Location);
      if (!string.IsNullOrEmpty(asmDir))
      {
        files = GetFiles(groupName, asmDir);
        if (files.Length == 0 && Directory.Exists(Path.Combine(asmDir, "ValheimRAFT")))
        {
          files = GetFiles(groupName, Path.Combine(asmDir, "ValheimRAFT"));
        }
      }
    }
    catch { }

    // 2. Check user-configured PluginFolderName
    if (files.Length == 0)
    {
      var modFolderName = ModSupportConfig.PluginFolderName.Value;
      if (!string.IsNullOrEmpty(modFolderName))
      {
        files = GetFiles(groupName, modFolderName);
      }
    }

    // 3. Check known possible folder names under Paths.PluginPath
    if (files.Length == 0)
    {
      foreach (var possibleModFolderName in ValheimRAFT_API.possibleModFolderNames)
      {
        files = GetFiles(groupName, possibleModFolderName);
        if (files.Length > 0) break;
      }
    }

    // 4. Recursive fallback: find groupName inside any Assets directory under Paths.PluginPath
    if (files.Length == 0 && Directory.Exists(Paths.PluginPath))
    {
      try
      {
        var matchingDirs = Directory.GetDirectories(Paths.PluginPath, groupName, SearchOption.AllDirectories);
        foreach (var dir in matchingDirs)
        {
          var parent = Path.GetDirectoryName(dir);
          if (parent != null && Path.GetFileName(parent).Equals("Assets", StringComparison.OrdinalIgnoreCase))
          {
            files = Directory.GetFiles(dir);
            if (files.Length > 0) break;
          }
        }
      }
      catch { }
    }

    if (files.Length == 0)
    {
      Logger.LogDebug(
        $"ValheimRAFT: No custom textures found in Assets/{groupName}.");
    }

    foreach (var file in files)
    {
      var ext = Path.GetExtension(file);
      if (m_validExtensions.Contains(ext,
            StringComparer.InvariantCultureIgnoreCase) && !Path
            .GetFileNameWithoutExtension(file)
            .EndsWith("_normal", StringComparison.InvariantCultureIgnoreCase))
        group.AddTexture(file);
    }

    return group;
  }

  public void AddTexture(string file)
  {
    var name = Path.GetFileNameWithoutExtension(file);
    var ext = Path.GetExtension(file);
    var path = Path.GetDirectoryName(file);
    var texture = new CustomTexture();
    var normal = Path.Combine(path, name + "_normal" + ext);
    if (File.Exists(normal))
      texture.Normal = AssetUtils.LoadTexture(normal, false);

    texture.Texture = AssetUtils.LoadTexture(file, false);
    texture.Texture.name = name;
    AddTexture(texture);
  }

  public void AddTexture(CustomTexture texture)
  {
    var name = texture.Texture.name;
    if (!m_textureLookUp.ContainsKey(name))
    {
      m_textureLookUp.Add(name, texture);
      m_textureHashLookUp.Add(name.GetStableHashCode(), texture);
      texture.Index = m_textures.Count;
      m_textures.Add(texture);
    }
  }
}