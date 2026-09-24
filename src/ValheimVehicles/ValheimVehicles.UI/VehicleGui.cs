#region

  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Diagnostics.CodeAnalysis;
  using System.Globalization;
  using DynamicLocations.Constants;
  using DynamicLocations.Controllers;
  using Jotunn.Managers;
  using TMPro;
  using UnityEngine;
  using UnityEngine.UI;
  using ValheimVehicles.Components;
  using ValheimVehicles.BepInExConfig;
  using ValheimVehicles.ConsoleCommands;
  using ValheimVehicles.Constants;
  using ValheimVehicles.Controllers;
  using ValheimVehicles.Shared.Constants;
  using ValheimVehicles.SharedScripts;
  using ValheimVehicles.SharedScripts.Helpers;
  using ValheimVehicles.SharedScripts.UI;
  using ValheimVehicles.Structs;
  using Zolantris.Shared;
  using Logger = Jotunn.Logger;

#endregion

  namespace ValheimVehicles.UI;

  public class VehicleGui : SingletonBehaviour<VehicleGui>
  {
    private GUIStyle? myButtonStyle;
    private string ShipMovementOffsetText;
    private Vector3 _shipMovementOffset;
    public static TMP_Dropdown VehicleSelectDropdown;

    public static VehicleGui Gui;
    public static GameObject GuiObj;

    public static GameObject GetVehicleGui()
    {
      if (GuiObj == null)
      {
        GuiObj = new GameObject("ValheimVehicles_VehicleGui")
        {
          transform = { parent = GUIManager.CustomGUIFront.transform },
          layer = LayerHelpers.UILayer
        };
      }
      return GuiObj;
    }

    public static void AddRemoveVehicleGui()
    {
      if (ZNet.instance == null) return;
      if (Gui)
      {
        Gui.RemoveGui();
        Gui = null;
      }
      if (GuiObj)
      {
        Destroy(GuiObj);
        GuiObj = null;
      }

      GuiObj = GetVehicleGui();

      if (Gui == null)
      {
        Gui = GuiObj.gameObject.GetOrAddComponent<VehicleGui>();
      }


      if (Instance != null)
      {
        Instance.InitPanel();
        SetCommandsPanelState(VehicleGuiMenuConfig.VehicleDebugMenuEnabled.Value);
      }
    }

    private Vector3 GetShipMovementOffset()
    {
      var shipMovementVectors = ShipMovementOffsetText.Split(',');
      if (shipMovementVectors.Length != 3) return new Vector3(0, 0, 0);
      var x = float.Parse(shipMovementVectors[0]);
      var y = float.Parse(shipMovementVectors[1]);
      var z = float.Parse(shipMovementVectors[2]);
      return new Vector3(x, y, z);
    }


    public static string vehicleCommandsHide => $"{ModTranslations.GuiCommandsMenuTitle} ({ModTranslations.GuiHide})";
    public static string vehicleCommandsShow => $"{ModTranslations.GuiCommandsMenuTitle} ({ModTranslations.GuiShow})";

    // todo translate this
    public const string vehicleConfigHide = "Vehicle Config (Hide)";
    public const string vehicleConfigShow = "Vehicle Config (Show)";

    public static bool vehicleDebugPhysicsSync = true;

    private int buttonFontSize = 16;

    private int titleFontSize = 18;

    private GUIStyle buttonStyle;

    private GUIStyle labelStyle;

    // private GameObject devCommandsWindow;
    public static bool hasCommandsWindowOpened = false;
    public static bool hasConfigPanelOpened = false;

    // overlay buttons that control toggling the panel
    public static bool isCommandsToggleButtonVisible = false;
    public static bool isConfigPanelToggleButtonVisible = false;


    private GameObject? configWindow;
    private GameObject? commandsWindow;

    private GameObject? commandsToggleButtonWindow;
    private GameObject? configToggleButtonWindow;

    private List<GameObject> commandsPanelToggleObjects = [];
    // private List<GameObject> devCommandsPanelToggleObjects = [];
    private List<GameObject> configPanelToggleObjects = [];

    private bool hasInitialized = false;

    private const int panelHeight = 500;
    private const float buttonHeight = 60f;
    private const float buttonWidth = 350f;
    private static float panelWidth = buttonWidth * 1.1f;

    private static readonly Vector2 anchorMin = new(0f, 0.5f);
    private static readonly Vector2 anchorMax = new(0.5f, 0.5f);
    private static readonly Vector2 panelPosition = Vector2.zero;
    private static readonly Vector3 buttonHeightVector3 = Vector3.up * buttonHeight;
    private static readonly Vector3 panelHeightVector3 = Vector3.up * panelHeight / 2f;

    private void Start()
    {
      buttonFontSize = VehicleGuiMenuConfig.ButtonFontSize.Value;
      titleFontSize = VehicleGuiMenuConfig.TitleFontSize.Value;
      hasInitialized = false;

      GUIManager.OnCustomGUIAvailable += InitPanel;
    }

    private void OnEnable()
    {
      hasInitialized = false;
      InitPanel();
    }

    private void OnDisable()
    {
      RemoveGui();
    }

    private void Update()
    {
      if (Player.m_localPlayer == null || ZNet.instance == null || Game.instance == null)
      {
        RemoveGui();
      }
    }

    public void RemoveGui()
    {
      commandsPanelToggleObjects.Clear();
      configPanelToggleObjects.Clear();

      if (commandsToggleButtonWindow) Destroy(commandsToggleButtonWindow);
      if (configToggleButtonWindow) Destroy(configToggleButtonWindow);
      if (commandsWindow) Destroy(commandsWindow);
      if (configWindow) Destroy(configWindow);

      commandsToggleButtonWindow = null;
      configToggleButtonWindow = null;
      commandsWindow = null;
      configWindow = null;

      if (GuiObj) Destroy(GuiObj);
      GuiObj = null;
      Gui = null;
    }

    public bool lastPanelState = false;

    public static void ToggleConfigPanelState(bool shouldHideShowButton = false)
    {
      hasConfigPanelOpened = !hasConfigPanelOpened;
      HideOrShowVehicleConfigPanel(hasConfigPanelOpened, shouldHideShowButton);
    }

    public static void SetConfigPanelState(bool val)
    {
      hasConfigPanelOpened = val;
      HideOrShowVehicleConfigPanel(val, true);
    }

    public void HideOrShowPanel(bool isVisible, bool shouldDeactivateToggleButton, ref GameObject toggleWindow, ref GameObject panelWindow, ref List<GameObject> toggleObjects)
    {
      if (Instance == null) return;
      if (toggleWindow == null || panelWindow == null)
      {
        InitPanel();
      }

      if (toggleWindow != null && shouldDeactivateToggleButton)
      {
        toggleWindow.SetActive(isVisible);
      }

      if (panelWindow != null)
      {
        panelWindow.SetActive(isVisible);
        toggleObjects.ForEach((x) =>
        {
          if (x == null) return;
          x.SetActive(isVisible);
        });
      }
    }

    public static void HideOrShowCommandPanel(bool isVisible, bool canUpdateTogglePanel)
    {
      if (Instance == null)
      {
        AddRemoveVehicleGui();
        return;
      }
      Instance.HideOrShowPanel(isVisible, canUpdateTogglePanel, ref Instance.commandsToggleButtonWindow, ref Instance.commandsWindow, ref Instance.commandsPanelToggleObjects);
    }

    public static void HideOrShowVehicleConfigPanel(bool isVisible, bool canUpdateTogglePanel)
    {
      if (Instance == null)
      {
        AddRemoveVehicleGui();
        return;
      }
      Instance.HideOrShowPanel(isVisible, canUpdateTogglePanel, ref Instance.configToggleButtonWindow, ref Instance.configWindow, ref Instance.configPanelToggleObjects);
    }

    public static void SetCommandsPanelState(bool val)
    {
      hasCommandsWindowOpened = val;
      HideOrShowVehicleConfigPanel(val, false);
    }

    public static void ToggleCommandsPanelState(bool canUpdateTogglePanel)
    {
      hasCommandsWindowOpened = !hasCommandsWindowOpened;
      // this should not hide the actual commands panel button.
      HideOrShowCommandPanel(hasCommandsWindowOpened, canUpdateTogglePanel);

      // also hide config panel. But do not show it.
      if (!hasCommandsWindowOpened)
      {
        HideOrShowVehicleConfigPanel(hasCommandsWindowOpened, true);
      }
    }

    public void InitPanel()
    {
      if (GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
      {
        return;
      }

      if (GuiObj == null)
      {
        GuiObj = GetVehicleGui();
      }

      CreateCommandsShortcutPanel();
      CreateVehicleConfigShortcutPanel();

      HideOrShowCommandPanel(hasCommandsWindowOpened, true);
      HideOrShowVehicleConfigPanel(hasConfigPanelOpened, true);

      hasInitialized = true;
    }

    public VehicleManager? targetInstance;

    private GenericInputAction _getCurrentVehicleGenericInputAction = new()
    {
      title = "Update current vehicle",
      OnButtonPress = () =>
      {
        if (Instance == null) return;
        var vehicleManager = VehicleCommands.GetNearestVehicleManager();
        if (vehicleManager == null)
        {
          Instance.targetInstance = null;
          return;
        }
        Instance.targetInstance = vehicleManager;
      }
    };


    public static void VehicleSelectOnDropdownChanged(int index)
    {
      var vehicles = VehicleStorageController.GetAllVehicles();

      // index 0 is a [None].
      if (index == 0)
      {
        VehicleStorageController.SelectedVehicle = "";
        return;
      }

      if (index > 0 && index <= vehicles.Count)
      {
        VehicleStorageController.SelectedVehicle = vehicles[index - 1].VehicleName;
        LoggerProvider.LogInfo($"Selected Vehicle: {VehicleStorageController.SelectedVehicle}");
      }
      else
      {
        LoggerProvider.LogWarning("No vehicles detected cannot select any vehicle.");
        VehicleStorageController.SelectedVehicle = "";
      }
    }

    public static GameObject AddDropdownWithAction(GenericInputAction genericInputAction, int index, float StartHeight, Transform parent)
    {
      var tmpDropdown = TMPDropdownFactory.CreateTMPDropDown(
        parent,
        new Vector2(0f, StartHeight - index * buttonHeight),
        new Vector2(buttonWidth, buttonHeight * 1.5f)
      );

      // it should never be null here.
      if (genericInputAction.OnDropdownChanged != null)
      {
        tmpDropdown.onValueChanged.AddListener(genericInputAction.OnDropdownChanged);
      }
      else
      {
        LoggerProvider.LogError("OnDropdownChanged not provided for a AddDropdownAction. This is an error with Valheim Vehicles. Please Report");
      }

      var refreshHandler = tmpDropdown.gameObject.AddComponent<DropdownRefreshOnHover>();
      if (genericInputAction.OnPointerEnterAction != null)
      {
        refreshHandler.OnPointerEnterAction = genericInputAction.OnPointerEnterAction;
      }

      if (genericInputAction.OnCreateDropdown != null)
      {
        genericInputAction.OnCreateDropdown(tmpDropdown);
      }

      return tmpDropdown.gameObject;
    }


    public GameObject AddInputWithAction(GenericInputAction genericInputAction, int index, float StartHeight, Transform windowTransform)
    {

      var buttonObj = GUIManager.Instance.CreateInputField(
        windowTransform,
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0, StartHeight - index * buttonHeight),
        InputField.ContentType.IntegerNumber, "8"
      );
      buttonObj.SetActive(true);
      // Add a listener to the button to close the panel again
      var inputField = buttonObj.GetComponent<InputField>();
      // var text = buttonObj.GetComponent<Text>();?
      // if (inputField != null && inputField.placeholder)
      // {
      //   inputField.placeholder.textfontSize = buttonFontSize;
      // }
      inputField.onSubmit.AddListener((x) =>
      {
        var intString = float.TryParse(x, out var value);
        if (intString)
        {
          Logger.LogDebug($"Converted string to float {value}");
        }
        else
        {
          Logger.LogDebug($"Not a string {x}");
        }
      });

      // button.OnSubmit(() =>
      // {
      //   return inputAction.saveAction;
      // });
      return buttonObj;
    }


    public GameObject AddButtonWithAction(GenericInputAction genericInputAction, int index, float StartHeight, Transform windowTransform)
    {

      var buttonObj = GUIManager.Instance.CreateButton(
        genericInputAction.title,
        windowTransform,
        new Vector2(0.5f, 0.5f),
        new Vector2(0.5f, 0.5f),
        new Vector2(0, StartHeight - index * buttonHeight),
        buttonWidth, buttonHeight
      );
      buttonObj.SetActive(true);
      // Add a listener to the button to close the panel again
      var button = buttonObj.GetComponent<Button>();
      var text = buttonObj.GetComponent<Text>();
      if (text != null)
      {
        text.fontSize = buttonFontSize;
      }

      button.onClick.AddListener(() => genericInputAction.OnButtonPress());
      return buttonObj;
    }

    private GameObject CreateConfigTogglePanel()
    {
      var panelStyles = new Unity2dViewStyles
      {
        anchorMin = anchorMin,
        anchorMax = anchorMax
      };

      var buttonStyles = new Unity2dViewStyles
      {
        anchorMin = new Vector2(0.5f, 0f),
        anchorMax = new Vector2(0.5f, 1f),
        position = Vector2.zero,
        height = buttonHeight,
        width = buttonWidth
      };

      var parentTransform = GuiObj != null ? GuiObj.transform : (GUIManager.CustomGUIFront != null ? GUIManager.CustomGUIFront.transform : null);
      if (parentTransform == null) return null!;

      var panel = PanelUtil.CreateDraggableHideShowPanel(ConfigPanelWindowName, parentTransform, panelStyles, buttonStyles, vehicleConfigHide, vehicleConfigShow, GuiConfig.VehicleCommandsPanelLocation, OnConfigCommandsPanelToggle);
      return panel;
    }

    private void OnConfigCommandsPanelToggle(Text buttonText)
    {
      var nextState = !configWindow.activeSelf;
      HideOrShowVehicleConfigPanel(nextState, false);
    }

    private void OnWindowCommandsPanelToggle(Text buttonText)
    {
      hasCommandsWindowOpened = false;
      HideOrShowCommandPanel(false, true);
    }

    private const string CommandsPanelWindowName = "ValheimVehicles_commandsWindow";
    private const string ConfigPanelWindowName = "ValheimVehicles_configWindow";

    /// <summary>
    /// Todo replace with PanelUtils.CreatePanel
    /// </summary>
    /// <returns></returns>
    private GameObject CreateCommandsTogglePanel()
    {
      var panelStyles = new Unity2dViewStyles
      {
        anchorMin = anchorMin,
        anchorMax = anchorMax
      };

      var buttonStyles = new Unity2dViewStyles
      {
        anchorMin = new Vector2(0.5f, 0f),
        anchorMax = new Vector2(0.5f, 1f),
        position = Vector2.zero,
        height = buttonHeight,
        width = buttonWidth
      };

      var parentTransform = GuiObj != null ? GuiObj.transform : (GUIManager.CustomGUIFront != null ? GUIManager.CustomGUIFront.transform : null);
      if (parentTransform == null) return null!;

      var closeText = ModTranslations.GuiCloseMenu ?? "Close Menu";
      var panel = PanelUtil.CreateDraggableHideShowPanel(CommandsPanelWindowName, parentTransform, panelStyles, buttonStyles, closeText, closeText, GuiConfig.VehicleCommandsPanelLocation, OnWindowCommandsPanelToggle);

      return panel;
    }

    public static GameObject? ConfigScrollView;

    public static Slider? LandVehicleTreadDistance_Slider;
    public static Slider? LandVehicleTreadScale_Slider;
    public static TMP_InputField? LandVehicleTreadDistance_Input;
    public static TMP_InputField? LandVehicleTreadScale_Input;

    public static TMP_InputField? VehicleName_Input;

    public static Slider? WaterFloatation_Slider;
    public static Toggle? WaterFloatation_Toggle;
    public static GameObject? WaterFloatationSliderRow;
    public static TMP_InputField? WaterFloatation_Input;

    public static VehicleManager? CurrentSelectedVehicle;
    public static MechanismSwitch? CurrentSwitch;


    public static bool IsEditing = false;
    private static TextMeshProUGUI _saveStatus;
    private static Button _resetButton;

    private static string FormatConfigFloat(float value)
    {
      return value.ToString(CultureInfo.CurrentCulture);
    }

    private static bool TryParseConfigFloat(string value, out float parsedValue)
    {
      return float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out parsedValue) ||
             float.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out parsedValue);
    }

    private static void SyncConfigInputValue(TMP_InputField? inputField, float value)
    {
      if (inputField != null) inputField.SetTextWithoutNotify(FormatConfigFloat(value));
    }

    public virtual void SetSavedState()
    {
      IsEditing = false;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Saved;
    }

    public static VehicleCustomConfig _tempVehicleConfig = new();

    public static void OnConfigSave(bool isReset)
    {
      if (!TryUpdateNearestVehicle(out var manager)) return;
      if (isReset)
      {
        _tempVehicleConfig = new VehicleCustomConfig();
      }
      manager.VehicleConfigSync.Config.ApplyFrom(_tempVehicleConfig);
      manager.VehicleConfigSync.Save(manager.m_nview.GetZDO(), [VehicleCustomConfig.Key_TreadDistance, VehicleCustomConfig.Key_CustomFloatationHeight, VehicleCustomConfig.Key_VehicleName]);

      if (manager.LandMovementController)
      {
        manager.UpdateLandMovementControllerProperties();
      }

      if (manager.PiecesController)
      {
        manager.PiecesController.ForceRebuildBounds();
      }
    }
    public static void UnsetSavedState()
    {
      IsEditing = true;
      if (_saveStatus) _saveStatus.text = SwivelUIPanelStrings.Save;
    }

    public static bool TryUpdateNearestVehicle([NotNullWhen(true)] out VehicleManager? manager)
    {
      var nearestVehicle = VehicleCommands.GetNearestVehicleManager();

      if (nearestVehicle != null)
      {
        CurrentSelectedVehicle = nearestVehicle;
      }

      manager = CurrentSelectedVehicle;
      return CurrentSelectedVehicle != null;
    }

    private void CreateVehicleConfigShortcutPanel()
    {
      // We destroy every time so we can refresh this panel to be accurate.
      if (configToggleButtonWindow != null)
      {
        Destroy(configToggleButtonWindow);
      }
      if (configWindow)
      {
        Destroy(configWindow);
      }
      if (configToggleButtonWindow != null && configWindow != null) return;

      // syncs the config to our local copy so we never mutate the original.
      _tempVehicleConfig = new VehicleCustomConfig();
      if (TryUpdateNearestVehicle(out var manager))
      {
        manager.Config.ApplyTo(_tempVehicleConfig);
      }

      configToggleButtonWindow = CreateConfigTogglePanel();


      if (!configToggleButtonWindow) return;

      // var dynamicPanelHeight = VehicleGUIItems.configSections.Count * buttonHeight + VehicleGUIItems.configSections.Count * 5;
      var height = Mathf.Min(1000f, Screen.height * 0.8f);
      var width = Mathf.Min(700f, Screen.width * 0.8f);

      // insets the whole scrollpanel and center aligns it
      configWindow = GUIManager.Instance.CreateWoodpanel(
        configToggleButtonWindow.transform,
        new Vector2(0.5f, 0f),
        new Vector2(0.5f, 1f),
        new Vector2(0, 0f),
        width,
        height,
        true);
      configWindow.SetActive(hasConfigPanelOpened);

      var windowVerticalGroup = configWindow.AddComponent<VerticalLayoutGroup>();
      windowVerticalGroup.padding = new RectOffset(16, 16, 16, 16);
      windowVerticalGroup.childForceExpandHeight = false;
      windowVerticalGroup.childForceExpandWidth = true;
      windowVerticalGroup.childControlWidth = true;
      windowVerticalGroup.childControlWidth = true;

      var scrollWidth = 500f;
      ConfigScrollView = GUIManager.Instance.CreateScrollView(configWindow.transform, false, true, 20, 10f, GUIManager.Instance.ValheimToggleColorBlock, new Color(0, 0, 0, 1), scrollWidth, height);

      //allow togglepanel to let children expand
      var viewport = ConfigScrollView.transform.Find("Scroll View/Viewport/Content");
      var viewportVerticalLayout = viewport.GetComponent<VerticalLayoutGroup>();
      windowVerticalGroup.padding = new RectOffset(16, 16, 16, 16);
      viewportVerticalLayout.childForceExpandHeight = true;
      viewportVerticalLayout.childForceExpandWidth = true;
      viewportVerticalLayout.childControlWidth = true;
      viewportVerticalLayout.spacing = 16;

      // ensures the scrollview is able to work within a VerticalLayoutGroup.
      var scrollViewLayoutElement = ConfigScrollView.AddComponent<LayoutElement>();
      scrollViewLayoutElement.flexibleHeight = 600f;
      scrollViewLayoutElement.minHeight = 200f;
      scrollViewLayoutElement.minWidth = scrollWidth;

      var scrollViewVerticalLayout = ConfigScrollView.GetComponentInChildren<VerticalLayoutGroup>();

      if (!scrollViewVerticalLayout) return;

      // var startHeight = height / 2f - buttonHeight / 2;
      var MinTargetOffset = -5;
      var MaxTargetOffset = 20;

      var viewStyles = new SwivelUISharedStyles();
      var svParent = scrollViewVerticalLayout.transform;

      if (manager == null)
      {
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleCommand_Message_VehicleNotFound);
        return;
      }

      var currentManager = manager;
      var sliderWidth = scrollWidth * 0.8f;

      // Vehicle name — shown for all vehicle types
      SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_VehicleName);
      SwivelUIHelpers.AddTextInputRow(
        svParent,
        viewStyles,
        ModTranslations.VehicleConfig_VehicleName,
        _tempVehicleConfig.VehicleName,
        value =>
        {
          _tempVehicleConfig.VehicleName = value;
          UnsetSavedState();
        },
        out VehicleName_Input,
        ModTranslations.VehicleConfig_VehicleName,
        TMP_InputField.ContentType.Standard,
        _ => VehicleName_Input?.SetTextWithoutNotify(_tempVehicleConfig.VehicleName),
        sliderWidth);

      if (!currentManager.IsLandVehicle)
      {
        // water vehicles

        // water floatation height
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_WaterVehicle_Section);
        SwivelUIHelpers.AddToggleRow(svParent, viewStyles, ModTranslations.VehicleConfig_CustomFloatationHeight, _tempVehicleConfig.HasCustomFloatationHeight, v =>
        {
          _tempVehicleConfig.HasCustomFloatationHeight = v;
          if (WaterFloatationSliderRow != null) WaterFloatationSliderRow.gameObject.SetActive(v);
          UnsetSavedState();
        }, out WaterFloatation_Toggle);


        WaterFloatationSliderRow = SwivelUIHelpers.AddSliderRow(svParent, viewStyles, ModTranslations.VehicleConfig_CustomFloatationHeight, -25f, 25f, _tempVehicleConfig.CustomFloatationHeight, v =>
        {
          _tempVehicleConfig.HasCustomFloatationHeight = true;
          _tempVehicleConfig.CustomFloatationHeight = v;
          SyncConfigInputValue(WaterFloatation_Input, _tempVehicleConfig.CustomFloatationHeight);
          UnsetSavedState();
        }, out WaterFloatation_Slider, sliderWidth);
      }

      if (currentManager.IsLandVehicle)
      {
        // land vehicles
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_LandVehicle_Section);

        // distance
        SwivelUIHelpers.AddSliderRow(svParent, viewStyles, ModTranslations.VehicleConfig_TreadsDistance, MinTargetOffset, MaxTargetOffset, _tempVehicleConfig.TreadDistance, v =>
        {
          _tempVehicleConfig.TreadDistance = v;
          SyncConfigInputValue(LandVehicleTreadDistance_Input, _tempVehicleConfig.TreadDistance);
          UnsetSavedState();
        }, out LandVehicleTreadDistance_Slider, sliderWidth);

        // scale
        SwivelUIHelpers.AddSectionLabel(svParent, viewStyles, ModTranslations.VehicleConfig_TreadsScale);
        SwivelUIHelpers.AddSliderRow(svParent, viewStyles, ModTranslations.VehicleConfig_TreadsScale, MinTargetOffset, MaxTargetOffset, _tempVehicleConfig.TreadScaleX, v =>
        {
          _tempVehicleConfig.TreadScaleX = v;
          SyncConfigInputValue(LandVehicleTreadScale_Input, _tempVehicleConfig.TreadScaleX);
          UnsetSavedState();
        }, out LandVehicleTreadScale_Slider, sliderWidth);
      }

      // action buttons
      var actionButtonRow = SwivelUIHelpers.AddRowWithButton(configWindow.transform, viewStyles, null, SwivelUIPanelStrings.Save, 96f, 48f, out _saveStatus, () =>
      {
        OnConfigSave(false);
        SetSavedState();
      });

      var buttonGO = SwivelUIHelpers.AddButton(actionButtonRow.transform, viewStyles, ModTranslations.SharedKeys_Reset, 96f, 48f, out _resetButton, out _, () =>
      {
        _tempVehicleConfig = new VehicleCustomConfig();

        VehicleName_Input?.SetTextWithoutNotify(_tempVehicleConfig.VehicleName);

        if (currentManager.IsLandVehicle && LandVehicleTreadDistance_Slider && LandVehicleTreadScale_Slider)
        {
          LandVehicleTreadDistance_Slider.SetValueWithoutNotify(_tempVehicleConfig.TreadDistance);
          LandVehicleTreadScale_Slider.SetValueWithoutNotify(_tempVehicleConfig.TreadScaleX);
          SyncConfigInputValue(LandVehicleTreadDistance_Input, _tempVehicleConfig.TreadDistance);
          SyncConfigInputValue(LandVehicleTreadScale_Input, _tempVehicleConfig.TreadScaleX);
        }

        if (WaterFloatation_Slider != null) WaterFloatation_Slider.SetValueWithoutNotify(_tempVehicleConfig.CustomFloatationHeight);
        if (WaterFloatation_Toggle != null) WaterFloatation_Toggle.SetIsOnWithoutNotify(_tempVehicleConfig.HasCustomFloatationHeight);
        if (WaterFloatationSliderRow != null) WaterFloatationSliderRow.SetActive(_tempVehicleConfig.HasCustomFloatationHeight);
        SyncConfigInputValue(WaterFloatation_Input, _tempVehicleConfig.CustomFloatationHeight);
        UnsetSavedState();
      });
    }

    private static bool CanAddAdminCommand()
    {
      if (ZNet.instance == null) return false;
      if (ZNet.instance.LocalPlayerIsAdminOrHost() || VehicleGuiMenuConfig.AllowDebugCommandsForNonAdmins.Value) return true;
      return false;
    }

    private void CreateCommandsShortcutPanel()
    {
      if (commandsToggleButtonWindow != null) return;

      commandsToggleButtonWindow = CreateCommandsTogglePanel();

      var viewStyles = new SwivelUISharedStyles();
      var panelWidth = 420f;
      var panelHeight = 440f;

      commandsWindow = GUIManager.Instance.CreateWoodpanel(
        commandsToggleButtonWindow.transform,
        new Vector2(0.5f, 0f),
        new Vector2(0.5f, 1f),
        new Vector2(0, 0f),
        panelWidth,
        panelHeight,
        false);
      commandsWindow.SetActive(hasCommandsWindowOpened);

      var windowVerticalGroup = commandsWindow.AddComponent<VerticalLayoutGroup>();
      windowVerticalGroup.padding = new RectOffset(16, 16, 16, 16);
      windowVerticalGroup.spacing = 8f;
      windowVerticalGroup.childForceExpandHeight = false;
      windowVerticalGroup.childForceExpandWidth = true;
      windowVerticalGroup.childControlWidth = true;
      windowVerticalGroup.childControlHeight = false;

      // 1. Action Buttons (Watermask Debugger, Water Mask On/Off, Hull Debugger, Physics Debugger)
      for (var index = 0; index < VehicleGUIItems.commandButtonActions.Count; index++)
      {
        var action = VehicleGUIItems.commandButtonActions[index];
        var btnObj = SwivelUIHelpers.AddButton(
          commandsWindow.transform,
          viewStyles,
          action.title,
          panelWidth - 32f,
          40f,
          out _,
          out _,
          () => action.OnButtonPress?.Invoke());
        if (btnObj != null) commandsPanelToggleObjects.Add(btnObj);
      }

      // 2. Teleport Drops Anchor (Portals, Beds)
      var vehicle = CurrentSelectedVehicle ?? VehicleCommands.GetNearestVehicleManager();
      var vZdo = vehicle?.m_nview?.GetZDO();
      var initialPortal = vZdo?.GetBool(VehicleZdoVars.ForceAnchorOnPortalTeleport, false)
                          ?? CurrentSwitch?.ForceAnchorOnPortalTeleport
                          ?? false;
      var initialBed = vZdo?.GetBool(VehicleZdoVars.ForceAnchorOnBedTeleport, false)
                       ?? CurrentSwitch?.ForceAnchorOnBedTeleport
                       ?? false;

      var anchorRow = SwivelUIHelpers.AddMultiToggleRow(
        commandsWindow.transform,
        viewStyles,
        ModTranslations.TeleportDropsAnchor ?? "Teleport Drops Anchor",
        new[] { "Portals", "Beds" },
        new[] { initialPortal, initialBed },
        states =>
        {
          if (states == null || states.Length < 2) return;
          var portalVal = states[0];
          var bedVal = states[1];

          // Auto-save to vehicle ZDO
          var v = CurrentSelectedVehicle ?? VehicleCommands.GetNearestVehicleManager();
          if (v?.m_nview?.GetZDO() != null)
          {
            v.m_nview.GetZDO().Set(VehicleZdoVars.ForceAnchorOnPortalTeleport, portalVal);
            v.m_nview.GetZDO().Set(VehicleZdoVars.ForceAnchorOnBedTeleport, bedVal);
          }

          // Auto-save to calling switch
          if (CurrentSwitch != null && CurrentSwitch.m_nview != null && CurrentSwitch.m_nview.GetZDO() != null)
          {
            CurrentSwitch.ForceAnchorOnPortalTeleport = portalVal;
            CurrentSwitch.ForceAnchorOnBedTeleport = bedVal;
            var cfg = new MechanismSwitchCustomConfig();
            cfg.ApplyFrom(CurrentSwitch.Config);
            cfg.ForceAnchorOnPortalTeleport = portalVal;
            cfg.ForceAnchorOnBedTeleport = bedVal;
            CurrentSwitch.prefabConfigSync.Request_CommitConfigChange(cfg);
          }
        });
      if (anchorRow != null) commandsPanelToggleObjects.Add(anchorRow);

      // 3. Console Debug Logs (Loop Log)
      var logRow = SwivelUIHelpers.AddToggleRow(
        commandsWindow.transform,
        viewStyles,
        ModTranslations.ConsoleDebugLogs ?? "Console Debug Logs",
        VehicleGuiMenuConfig.EnableLoopLogging?.Value ?? false,
        val =>
        {
          if (VehicleGuiMenuConfig.EnableLoopLogging != null)
          {
            VehicleGuiMenuConfig.EnableLoopLogging.Value = val;
          }
        });
      if (logRow != null) commandsPanelToggleObjects.Add(logRow);

      // 4. Adjust MP Sync (1s, 3s, 5s - 3s default)
      var curInterval = VehicleGlobalConfig.ServerRaftUpdateZoneInterval?.Value ?? 3.0f;
      var defaultSyncIndex = 1; // 3s
      if (Mathf.Approximately(curInterval, 1.0f) || (VehicleGlobalConfig.FastMultiplayerSync?.Value ?? false))
      {
        defaultSyncIndex = 0; // 1s
      }
      else if (Mathf.Approximately(curInterval, 5.0f))
      {
        defaultSyncIndex = 2; // 5s
      }

      var syncRow = SwivelUIHelpers.AddRadioToggleRow(
        commandsWindow.transform,
        viewStyles,
        ModTranslations.AdjustMpSync ?? "Adjust MP Sync",
        new[] { "1s", "3s", "5s" },
        defaultSyncIndex,
        selectedIndex =>
        {
          var interval = selectedIndex switch
          {
            0 => 1.0f,
            2 => 5.0f,
            _ => 3.0f
          };
          var fastSync = (selectedIndex == 0);

          if (VehicleGlobalConfig.ServerRaftUpdateZoneInterval != null)
          {
            VehicleGlobalConfig.ServerRaftUpdateZoneInterval.Value = interval;
          }
          if (VehicleGlobalConfig.FastMultiplayerSync != null)
          {
            VehicleGlobalConfig.FastMultiplayerSync.Value = fastSync;
          }
        });
      if (syncRow != null) commandsPanelToggleObjects.Add(syncRow);
    }

    public static void ToggleConvexHullDebugger()
    {
      Logger.LogMessage(
        "Toggling convex hull debugger on the ship. This will show/hide the current convex hulls.");
      var currentInstance = VehicleCommands.GetNearestVehicleManager();

      if (currentInstance == null || currentInstance.PiecesController == null) return;

      var convexHullComponent = currentInstance
        .PiecesController.convexHullComponent;

      convexHullComponent.PreviewMode =
        convexHullComponent.PreviewMode switch
        {
          ConvexHullAPI.PreviewModes.None => ConvexHullAPI.PreviewModes.Bubble,
          ConvexHullAPI.PreviewModes.Bubble => ConvexHullAPI.PreviewModes.Debug,
          _ => ConvexHullAPI.PreviewModes.Bubble
        };

      currentInstance.PiecesController.convexHullComponent
        .CreatePreviewConvexHullMeshes();
    }


    public static void ToggleColliderDebugger()
    {
      Logger.LogMessage(
        "Collider debugger called, \nblue = BlockingCollider for collisions and keeping boat on surface, \ngreen is float collider for pushing the boat upwards, typically it needs to be below or at same level as BlockingCollider to prevent issues, \nYellow is onboardtrigger for calculating if player is onboard");
      var currentShip = VehicleCommands.GetNearestVehicleManager();
      if (currentShip == null) return;
      currentShip.Instance.HasVehicleDebugger = !currentShip.Instance.HasVehicleDebugger;
      var currentInstance = VehicleDebugHelpers.GetOnboardVehicleDebugHelper();
      if (currentInstance == null) return;
      currentInstance.StartRenderAllCollidersLoop();
    }

    /// <summary>
    /// For Developers, Modders, and normal users that need to frequently use ValheimRAFT commands.
    /// </summary>
    private void DrawVehicleDebugCommandsMenu()
    {
      GUILayout.BeginArea(new Rect(500, Screen.height - 510, 200, 500),
        myButtonStyle);

      if (GUILayout.Button("ConvexHull debugger"))
      {
        ToggleConvexHullDebugger();
      }

      if (GUILayout.Button("collider debugger"))
      {
        ToggleColliderDebugger();
      }

      if (GUILayout.Button("raftcreative"))
        VehicleCommands.ToggleCreativeMode();

      if (GUILayout.Button("activatePendingPieces"))
      {
        var nearest = VehicleCommands.GetNearestVehicleManager();
        if (nearest != null)
        {
          nearest.PiecesController?.StartActivatePendingPieces();
        }
      }

      if (GUILayout.Button("Zero Ship RotationXZ"))
        VehicleDebugHelpers.GetOnboardVehicleDebugHelper()?.FlipShip();

      if (GUILayout.Button("Toggle Ocean Sway"))
        VehicleCommands.VehicleToggleOceanSway();

      if (GUILayout.Button(ModTranslations.WaterMaskOnOff ?? "Water Mask On/Off"))
        VehicleCommands.ToggleAutomatedWaterMask();

      GUILayout.EndArea();
    }

    /// <summary>
    /// Meant for developers. Should never be enabled for players. These commands can be very destructive to the entire game world.
    /// </summary>
    [Conditional("DEBUG")]
    private void DrawDeveloperDebugCommandsWindow()
    {
#if DEBUG
      GUILayout.BeginArea(new Rect(250, Screen.height - 510, 200, 200),
        myButtonStyle);
      if (GUILayout.Button("Delete ShipZDO"))
      {
        var currentVehicle = VehicleCommands.GetNearestVehicleManager();
        if (currentVehicle != null && currentVehicle.m_nview != null && ZNetScene.instance != null)
          ZNetScene.instance.Destroy(currentVehicle.m_nview
            .gameObject);
      }

      if (GUILayout.Button("Set logoutpoint"))
      {
        var zdo = Player.m_localPlayer
          .GetComponentInParent<VehiclePiecesController>().m_nview
          .GetZDO();
        if (zdo != null) PlayerSpawnController.Instance?.SyncLogoutPoint(zdo);
      }

      if (GUILayout.Button("Move to current spawn"))
        PlayerSpawnController.Instance?.DEBUG_MoveTo(LocationVariation
          .Spawn);

      if (GUILayout.Button("Move to current logout"))
        PlayerSpawnController.Instance?.DEBUG_MoveTo(LocationVariation
          .Logout);

      if (GUILayout.Button("DebugFind PlayerSpawnController"))
      {
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
          if (obj.name.Contains($"{PrefabNames.PlayerSpawnControllerObj}(Clone)"))
            Logger.LogDebug("found playerSpawn controller");
      }

      if (GUILayout.Button("Force Move Vehicle"))
      {
        var currentVehicle = VehicleCommands.GetNearestVehicleManager();
        if (currentVehicle != null)
        {
          var shipBody = currentVehicle?.MovementController?.m_body;
          if (shipBody == null) return;
          shipBody.MovePosition(shipBody.position + Vector3.forward);
        }
      }

      if (GUILayout.Button("Delete All Vehicles"))
      {
        var allObjects = Resources.FindObjectsOfTypeAll<ZNetView>();
        foreach (var obj in allObjects)
          if (obj.name.Contains($"{PrefabNames.WaterVehicleShip}(Clone)") || obj.name.Contains($"{PrefabNames.LandVehicle}(Clone)"))
          {
            Logger.LogInfo($"Destroying {obj.name}");
            ZNetScene.instance.Destroy(obj.gameObject);
          }
      }

      GUILayout.EndArea();
#endif
    }
  }