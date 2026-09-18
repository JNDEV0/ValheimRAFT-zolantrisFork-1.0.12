using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using BepInEx;
using Jotunn.Managers;
using UnityEngine;
using ValheimVehicles.BepInExConfig;
using ValheimVehicles.SharedScripts;
using Zolantris.Shared;

namespace ValheimVehicles.Integrations;

/// <summary>
/// Multi-Tier Localization Manager for ValheimRAFT.
/// 
/// 1. Guaranteed English Baseline:
///    - Tier 1: Search disk files in Assets/Translations/English/valheimraft.json
///    - Tier 2: Embedded Assembly Resource (English.valheimraft.json) as absolute safety net.
/// 2. Optional Language Discovery:
///    - Scans Assets/Translations/ for optional community language folders.
/// 3. Fail-Safe Overlay:
///    - English is seeded first into Localization.instance for all 368 tokens.
///    - If an optional language is active, its localized tokens overwrite the English baseline.
///    - If a language file is missing or has missing tokens, it safely defaults to English.
/// </summary>
public static class ValheimRaftLocalization
{
  private static bool _isInitialized;
  private static FieldInfo _translationsField;
  private static MethodInfo _addWordMethod;

  public static string CurrentActiveLanguage { get; private set; } = "English";

  public static readonly List<string> DiscoveredLanguages = new() { "English" };
  public static readonly Dictionary<string, Dictionary<string, string>> AllTranslations =
    new(StringComparer.OrdinalIgnoreCase);

  public static void Initialize()
  {
    if (_isInitialized) return;
    _isInitialized = true;

    try
    {
      LoadEnglishBaseline();
      DiscoverOptionalLanguages();

      // Subscribe to game language change event
      try
      {
        Localization.OnLanguageChange += OnGameLanguageChanged;
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning($"[ValheimRAFT] Could not bind to Localization.OnLanguageChange: {e.Message}");
      }

      if (VehicleGlobalConfig.ModLanguage != null)
      {
        VehicleGlobalConfig.ModLanguage.SettingChanged += (sender, args) => ApplyActiveLanguage();
      }

      ApplyActiveLanguage();
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"[ValheimRAFT] Localization initialization error: {e}");
    }
  }

  private static void OnGameLanguageChanged()
  {
    ApplyActiveLanguage();
  }

  private static void LoadEnglishBaseline()
  {
    string englishJson = null;

    // Tier 1: Look on disk
    var diskPath = FindTranslationFileOnDisk("English");
    if (!string.IsNullOrEmpty(diskPath) && File.Exists(diskPath))
    {
      try
      {
        englishJson = File.ReadAllText(diskPath);
        LoggerProvider.LogInfo($"[ValheimRAFT] Loaded English localization from disk: {diskPath}");
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning($"[ValheimRAFT] Failed reading English disk file {diskPath}: {e.Message}");
      }
    }

    // Tier 2: Embedded resource in assembly
    if (string.IsNullOrEmpty(englishJson))
    {
      englishJson = ReadEmbeddedResource("English.valheimraft.json");
      if (!string.IsNullOrEmpty(englishJson))
      {
        LoggerProvider.LogInfo("[ValheimRAFT] Loaded English localization from embedded assembly resource.");
      }
    }

    if (!string.IsNullOrEmpty(englishJson))
    {
      var parsed = ParseJsonDictionary(englishJson);
      AllTranslations["English"] = parsed;

      try
      {
        LocalizationManager.Instance.AddJson("English", englishJson);
      }
      catch (Exception e)
      {
        LoggerProvider.LogWarning($"[ValheimRAFT] Jotunn AddJson('English') warning: {e.Message}");
      }
    }
    else
    {
      LoggerProvider.LogError("[ValheimRAFT] CRITICAL: English localization could not be loaded from disk or assembly!");
    }
  }

  private static void DiscoverOptionalLanguages()
  {
    var translationsRoot = FindTranslationsRootDirectory();
    if (string.IsNullOrEmpty(translationsRoot) || !Directory.Exists(translationsRoot))
    {
      return;
    }

    try
    {
      var subDirs = Directory.GetDirectories(translationsRoot);
      foreach (var dir in subDirs)
      {
        var langName = Path.GetFileName(dir);
        if (langName.Equals("English", StringComparison.OrdinalIgnoreCase)) continue;

        var jsonFile = Path.Combine(dir, "valheimraft.json");
        if (File.Exists(jsonFile))
        {
          try
          {
            var content = File.ReadAllText(jsonFile);
            var parsed = ParseJsonDictionary(content);
            if (parsed.Count > 0)
            {
              AllTranslations[langName] = parsed;
              if (!DiscoveredLanguages.Contains(langName))
              {
                DiscoveredLanguages.Add(langName);
              }

              try
              {
                LocalizationManager.Instance.AddJson(langName, content);
              }
              catch { }

              LoggerProvider.LogInfo($"[ValheimRAFT] Discovered optional localization for '{langName}' ({parsed.Count} tokens)");
            }
          }
          catch (Exception e)
          {
            LoggerProvider.LogWarning($"[ValheimRAFT] Failed loading optional translation '{langName}': {e.Message}");
          }
        }
      }
    }
    catch (Exception e)
    {
      LoggerProvider.LogWarning($"[ValheimRAFT] Error scanning optional translations directory: {e.Message}");
    }
  }

  public static void ApplyActiveLanguage()
  {
    var configured = VehicleGlobalConfig.ModLanguage?.Value ?? "Auto";
    string targetLang = "English";

    if (configured.Equals("Auto", StringComparison.OrdinalIgnoreCase))
    {
      var gameLang = Localization.instance?.GetSelectedLanguage() ?? PlayerPrefs.GetString("language", "English");
      if (gameLang.Equals("Portuguese_European", StringComparison.OrdinalIgnoreCase) && AllTranslations.ContainsKey("Portuguese_Brazilian"))
      {
        gameLang = "Portuguese_Brazilian";
      }

      if (AllTranslations.ContainsKey(gameLang))
      {
        targetLang = gameLang;
      }
      else
      {
        targetLang = "English";
      }
    }
    else if (AllTranslations.ContainsKey(configured))
    {
      targetLang = configured;
    }

    CurrentActiveLanguage = targetLang;

    if (Localization.instance != null)
    {
      try
      {
        // 1. Unconditionally seed master English baseline
        if (AllTranslations.TryGetValue("English", out var enMap))
        {
          foreach (var kvp in enMap)
          {
            InjectWord(kvp.Key, kvp.Value);
          }
        }

        // 2. If target language is non-English, overlay localized tokens
        if (!targetLang.Equals("English", StringComparison.OrdinalIgnoreCase) &&
            AllTranslations.TryGetValue(targetLang, out var langMap))
        {
          foreach (var kvp in langMap)
          {
            if (!string.IsNullOrEmpty(kvp.Value))
            {
              InjectWord(kvp.Key, kvp.Value);
            }
          }
        }

        // 3. Refresh static translation cache
        ModTranslations.ForceUpdateTranslations();
        LoggerProvider.LogInfo($"[ValheimRAFT] Applied active language '{targetLang}' (Mode: '{configured}')");
      }
      catch (Exception e)
      {
        LoggerProvider.LogError($"[ValheimRAFT] Error applying active language: {e}");
      }
    }
  }

  private static void InjectWord(string key, string value)
  {
    if (string.IsNullOrEmpty(key) || value == null) return;

    // Direct injection into Localization.instance's internal translation dictionary
    try
    {
      if (_translationsField == null && Localization.instance != null)
      {
        _translationsField = typeof(Localization).GetField("m_translations", BindingFlags.Instance | BindingFlags.NonPublic);
      }

      if (_translationsField != null && Localization.instance != null)
      {
        var dict = _translationsField.GetValue(Localization.instance) as Dictionary<string, string>;
        if (dict != null)
        {
          dict[key] = value;
          dict["$" + key] = value;
          return;
        }
      }
    }
    catch { }

    // Fallback: invoke AddWord via reflection
    try
    {
      if (_addWordMethod == null && Localization.instance != null)
      {
        _addWordMethod = typeof(Localization).GetMethod("AddWord", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
      }

      if (_addWordMethod != null && Localization.instance != null)
      {
        _addWordMethod.Invoke(Localization.instance, new object[] { key, value });
        _addWordMethod.Invoke(Localization.instance, new object[] { "$" + key, value });
      }
    }
    catch { }
  }

  public static string GetEffectiveLanguage()
  {
    return CurrentActiveLanguage;
  }

  private static string FindTranslationFileOnDisk(string language)
  {
    var root = FindTranslationsRootDirectory();
    if (!string.IsNullOrEmpty(root))
    {
      var path = Path.Combine(root, language, "valheimraft.json");
      if (File.Exists(path)) return path;
    }
    return null;
  }

  private static string FindTranslationsRootDirectory()
  {
    var candidates = new List<string>();

    // 1. Assembly location Assets/Translations
    try
    {
      var asmDir = Path.GetDirectoryName(typeof(ValheimRaftLocalization).Assembly.Location);
      if (!string.IsNullOrEmpty(asmDir))
      {
        candidates.Add(Path.Combine(asmDir, "Assets", "Translations"));
        candidates.Add(Path.Combine(asmDir, "..", "Assets", "Translations"));
      }
    }
    catch { }

    // 2. BepInEx plugin paths
    candidates.Add(Path.Combine(Paths.PluginPath, "ValheimRAFT", "Assets", "Translations"));
    candidates.Add(Path.Combine(Paths.PluginPath, "Assets", "Translations"));

    foreach (var path in candidates)
    {
      try
      {
        if (Directory.Exists(path)) return path;
      }
      catch { }
    }

    return null;
  }

  private static string ReadEmbeddedResource(string resourceName)
  {
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    foreach (var asm in assemblies)
    {
      try
      {
        var names = asm.GetManifestResourceNames();
        if (names == null) continue;

        foreach (var name in names)
        {
          if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
          {
            using var stream = asm.GetManifestResourceStream(name);
            if (stream != null)
            {
              using var reader = new StreamReader(stream);
              return reader.ReadToEnd();
            }
          }
        }
      }
      catch { }
    }
    return null;
  }

  private static Dictionary<string, string> ParseJsonDictionary(string json)
  {
    var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    if (string.IsNullOrEmpty(json)) return dict;

    try
    {
      int i = 0;
      int len = json.Length;
      while (i < len)
      {
        int keyStart = json.IndexOf('"', i);
        if (keyStart == -1) break;
        int keyEnd = json.IndexOf('"', keyStart + 1);
        if (keyEnd == -1) break;

        string key = json.Substring(keyStart + 1, keyEnd - keyStart - 1).TrimStart('$');

        int colon = json.IndexOf(':', keyEnd + 1);
        if (colon == -1) break;

        int valStart = json.IndexOf('"', colon + 1);
        if (valStart == -1) break;

        int valEnd = valStart + 1;
        while (valEnd < len)
        {
          if (json[valEnd] == '"')
          {
            int bsCount = 0;
            int checkIdx = valEnd - 1;
            while (checkIdx >= valStart && json[checkIdx] == '\\')
            {
              bsCount++;
              checkIdx--;
            }
            if (bsCount % 2 == 0) break;
          }
          valEnd++;
        }
        if (valEnd >= len) break;

        string rawVal = json.Substring(valStart + 1, valEnd - valStart - 1);
        string val = rawVal.Replace(@"\\", "\u0001")
                           .Replace(@"\""", "\"")
                           .Replace(@"\n", "\n")
                           .Replace(@"\r", "\r")
                           .Replace(@"\t", "\t")
                           .Replace("\u0001", @"\");

        dict[key] = val;
        i = valEnd + 1;
      }
    }
    catch (Exception e)
    {
      LoggerProvider.LogError($"[ValheimRAFT] ParseJsonDictionary error: {e.Message}");
    }
    return dict;
  }
}
