using BepInEx.Configuration;
using ValheimVehicles.Components;
using ValheimVehicles.Helpers;
using ValheimVehicles.SharedScripts.Validation;
using Zolantris.Shared;
namespace ValheimVehicles.BepInExConfig;

public class VehicleGlobalConfig : BepInExBaseConfig<VehicleGlobalConfig>
{
  // sounds for VehicleShip Effects
  public static ConfigEntry<bool> EnableShipWakeSounds = null!;
  public static ConfigEntry<bool> EnableShipInWaterSounds = null!;
  public static ConfigEntry<bool> EnableShipSailSounds = null!;

  // updaters
  public static ConfigEntry<float> ServerRaftUpdateZoneInterval = null!;
    public static ConfigEntry<bool> FastMultiplayerSync = null!;
  public static ConfigEntry<bool> ForceShipOwnerUpdatePerFrame { get; set; }

  // localization
  public static ConfigEntry<string> ModLanguage = null!;
  private const string VehicleLocalizationKey = $"{VehicleGlobalBaseKey}:Localization";

  // Horn of Loki
  public static ConfigEntry<float> HornTeleportCooldownSeconds = null!;
  public static ConfigEntry<float> HornChannelDurationSeconds = null!;
  private const string VehicleHornKey = $"{VehicleGlobalBaseKey}:VesselHorn";

  // section keys
  private const string VehicleGlobalBaseKey = "VehicleGlobal";
  private const string VehicleSoundKey = $"{VehicleGlobalBaseKey}:Sound";
  private const string VehicleGlobalUpdateKey = $"{VehicleGlobalBaseKey}:Updates";

  public override void OnBindConfig(ConfigFile config)
  {
    CreateSoundConfig(config);
    CreateVehicleUpdaterConfig(config);
    CreateLocalizationConfig(config);
    CreateHornConfig(config);
  }

  private static void CreateHornConfig(ConfigFile config)
  {
    HornTeleportCooldownSeconds = config.BindUnique(VehicleHornKey,
      "HornTeleportCooldownSeconds", 10.0f,
      ConfigHelpers.CreateConfigDescription(
        "Cooldown in seconds applied after successfully teleporting to a vessel using the Horn of Loki.",
        false, false, new AcceptableValueRange<float>(0f, 60f)));

    HornChannelDurationSeconds = config.BindUnique(VehicleHornKey,
      "HornChannelDurationSeconds", 3.0f,
      ConfigHelpers.CreateConfigDescription(
        "Channel duration in seconds required to activate the Horn of Loki.",
        false, false, new AcceptableValueRange<float>(1.0f, 10f)));
  }

  private static void CreateLocalizationConfig(ConfigFile config)
  {
    ModLanguage = config.BindUnique(VehicleLocalizationKey,
      "Language",
      "Auto",
      ConfigHelpers.CreateConfigDescription(
        "Language used by ValheimRAFT. 'Auto' follows the game's selected language if an optional translation is installed, else defaults to English.",
        false, false));
  }

  private static void CreateVehicleUpdaterConfig(ConfigFile config)
  {
    ForceShipOwnerUpdatePerFrame = config.BindUnique("Rendering",
      "Force Ship Owner Piece Update Per Frame", false,
      ConfigHelpers.CreateConfigDescription(
        "Forces an update during the Update sync of unity meaning it fires every frame for the Ship owner who also owns Physics. This will possibly make updates better for non-boat owners. Noting that the boat owner is determined by the first person on the boat, otherwise the game owns it.",
        true, true));


    FastMultiplayerSync = config.BindUnique(VehicleGlobalUpdateKey,
      "FastMultiplayerSync",
      false,
      ConfigHelpers.CreateConfigDescription(
        "When enabled on multiplayer servers, tightens piece sync interval to 1.0s for reduced sync latency at higher network load. When disabled, uses ServerRaftUpdateZoneInterval (default 3.0s).",
        true, true));

    ServerRaftUpdateZoneInterval = config.BindUnique(VehicleGlobalUpdateKey,
      "ServerRaftUpdateZoneInterval",
      3f,
      ConfigHelpers.CreateConfigDescription(
        "Allows Server Admin control over the update tick for the RAFT location. Larger Rafts will take much longer and lag out players, but making this ticket longer will make the raft turn into a box from a long distance away.",
        true, true, new AcceptableValueRange<float>(1, 30f)));
  }

  private static void CreateSoundConfig(ConfigFile config)
  {
    EnableShipSailSounds = config.BindUnique(VehicleSoundKey, "Ship Sailing Sounds", true,
      "Toggles the ship sail sounds.");
    EnableShipWakeSounds = config.BindUnique(VehicleSoundKey, "Ship Wake Sounds", true,
      "Toggles Ship Wake sounds. Can be pretty loud");
    EnableShipInWaterSounds = config.BindUnique(VehicleSoundKey, "Ship In-Water Sounds",
      true,
      "Toggles ShipInWater Sounds, the sound of the hull hitting water");

    EnableShipSailSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
    EnableShipWakeSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
    EnableShipInWaterSounds.SettingChanged += VehicleManager.UpdateAllShipSounds;
  }
}