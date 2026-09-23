using System.Collections.Generic;
using ValheimVehicles.Components;
using ValheimVehicles.ConsoleCommands;
using ValheimVehicles.Controllers;
using ValheimVehicles.SharedScripts;
using ValheimVehicles.Structs;
namespace ValheimVehicles.UI;

/// <summary>
/// Nit-pick: rename GUI to different value. It makes names messy
/// </summary>
public static class VehicleGUIItems
{
  public static readonly List<GenericInputAction> configSections =
  [
    new()
    {
      title = "Treads Max Width"
      // description = "Set the max width of treads",
      // saveAction = (string val) =>
      // {
      //   CurrentSelectedVehicle?.VehicleCustomConfig.TreadDistance = val;
      // },
      // resetAction = () => {}
      // onSubmit: () => 
      // onChange: () => 
    }
  ];


  // todo translate all of this.
  public static readonly List<GenericInputAction> commandButtonActions =
  [
    new()
    {
      title = ModTranslations.VehicleCommand_WatermaskDebugger ?? "Watermask Debugger",
      OnButtonPress = VehicleCommands.ToggleColliderEditMode
    },
    new()
    {
      title = ModTranslations.WaterMaskOnOff ?? "Water Mask On/Off",
      OnButtonPress = VehicleCommands.ToggleAutomatedWaterMask
    },
    new()
    {
      title = ModTranslations.VehicleCommand_HullDebugger ?? "Hull Debugger",
      OnButtonPress = VehicleGui.ToggleConvexHullDebugger
    },
    new()
    {
      title = ModTranslations.VehicleCommand_PhysicsDebugger ?? "Physics Debugger",
      OnButtonPress = VehicleGui.ToggleColliderDebugger
    }
  ];
}