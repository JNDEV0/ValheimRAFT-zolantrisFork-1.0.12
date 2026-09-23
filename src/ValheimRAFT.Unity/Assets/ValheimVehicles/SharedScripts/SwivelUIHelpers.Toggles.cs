#region

  using System;
  using System.Linq;
  using TMPro;
  using UnityEngine;
  using UnityEngine.Events;
  using UnityEngine.UI;

#endregion

// ReSharper disable ArrangeNamespaceBody
// ReSharper disable NamespaceStyle
  namespace ValheimVehicles.SharedScripts.UI
  {
    public static partial class SwivelUIHelpers
    {
      public static GameObject AddMultiToggleRow(Transform parent, SwivelUISharedStyles viewStyles, string label, string[] toggleLabels, bool[] initialStates, Action<bool[]> onChanged)
      {
        var row = CreateRow(parent, viewStyles, label, out _, false);

        var toggleStates = new bool[toggleLabels.Length];
        var toggles = new Toggle[toggleLabels.Length];

        for (var i = 0; i < toggleLabels.Length; i++)
        {
          var toggleWrapper = new GameObject($"{toggleLabels[i]}Wrapper", typeof(RectTransform), typeof(VerticalLayoutGroup));
          toggleWrapper.transform.SetParent(row.transform, false);

          var wrapperLayout = toggleWrapper.GetComponent<VerticalLayoutGroup>();
          wrapperLayout.childAlignment = TextAnchor.MiddleCenter;
          wrapperLayout.childControlHeight = true;
          wrapperLayout.childControlWidth = true;
          wrapperLayout.childForceExpandHeight = false;
          wrapperLayout.childForceExpandWidth = false;
          wrapperLayout.spacing = 2f;

          var container = new GameObject($"{toggleLabels[i]}ToggleContainer", typeof(RectTransform), typeof(LayoutElement));
          container.transform.SetParent(toggleWrapper.transform, false);
          var layoutElement = container.GetComponent<LayoutElement>();
          layoutElement.minWidth = 48;
          layoutElement.minHeight = 48;
          layoutElement.preferredWidth = 48;
          layoutElement.preferredHeight = 48;
          layoutElement.flexibleWidth = 0;

          var toggleGO = new GameObject($"{toggleLabels[i]}Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
          toggleGO.transform.SetParent(container.transform, false);
          var toggleImage = toggleGO.GetComponent<Image>();
          toggleImage.color = initialStates[i] ? SwivelUIColors.greenBg : SwivelUIColors.grayBg;
          toggleImage.raycastTarget = true;

          var toggle = toggleGO.GetComponent<Toggle>();
          toggle.interactable = CanNavigatorInteractWithPanel;
          toggle.isOn = initialStates[i];
          toggle.targetGraphic = toggleImage;

          var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
          checkmarkGO.transform.SetParent(toggleGO.transform, false);
          var checkmarkImage = checkmarkGO.GetComponent<Image>();
          checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
          checkmarkImage.raycastTarget = false;

          var checkmarkRT = checkmarkGO.GetComponent<RectTransform>();
          checkmarkRT.anchorMin = new Vector2(0.2f, 0.2f);
          checkmarkRT.anchorMax = new Vector2(0.8f, 0.8f);
          checkmarkRT.offsetMin = Vector2.zero;
          checkmarkRT.offsetMax = Vector2.zero;

          toggle.graphic = checkmarkImage;
          var index = i;
          toggle.onValueChanged.AddListener(value =>
          {
            toggleStates[index] = value;
            toggleImage.color = value ? SwivelUIColors.greenBg : SwivelUIColors.grayBg;
            onChanged(toggleStates);
          });

          var toggleRT = toggleGO.GetComponent<RectTransform>();
          toggleRT.anchorMin = Vector2.zero;
          toggleRT.anchorMax = Vector2.one;
          toggleRT.offsetMin = Vector2.zero;
          toggleRT.offsetMax = Vector2.zero;

          toggles[i] = toggle;

          // Add label below toggle
          var labelGO = new GameObject($"{toggleLabels[i]}Label", typeof(TextMeshProUGUI));
          labelGO.transform.SetParent(toggleWrapper.transform, false);
          var labelText = labelGO.GetComponent<TextMeshProUGUI>();
          labelText.text = toggleLabels[i];
          labelText.fontSize = viewStyles.FontSizeRowLabel;
          labelText.color = viewStyles.LabelColor;
          labelText.alignment = TextAlignmentOptions.Center;
          labelText.enableAutoSizing = false;
        }

        return row;
      }

      public static GameObject AddToggleRow(Transform parent, SwivelUISharedStyles viewStyles, string label, bool initial, UnityAction<bool> onChanged)
      {
        return AddToggleRow(parent, viewStyles, label, initial, onChanged, out _);
      }
      public static GameObject AddToggleRow(Transform parent, SwivelUISharedStyles viewStyles, string label, bool initial, UnityAction<bool> onChanged, out Toggle toggle)
      {
        var row = CreateRow(parent, viewStyles, label, out _, false);

        // === Toggle Container (fix width/height to enforce square) ===
        var toggleContainer = new GameObject("ToggleContainer", typeof(RectTransform), typeof(LayoutElement));
        toggleContainer.transform.SetParent(row.transform, false);

        var toggleContainerRT = toggleContainer.GetComponent<RectTransform>();
        toggleContainerRT.sizeDelta = new Vector2(48, 48);

        var toggleContainerLE = toggleContainer.GetComponent<LayoutElement>();
        toggleContainerLE.minWidth = 48;
        toggleContainerLE.minHeight = 48;
        toggleContainerLE.preferredWidth = 48;
        toggleContainerLE.preferredHeight = 48;
        toggleContainerLE.flexibleWidth = 0f;

        // === Toggle ===
        var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
        toggleGO.transform.SetParent(toggleContainer.transform, false);
        var toggleRT = toggleGO.GetComponent<RectTransform>();
        toggleRT.anchorMin = Vector2.zero;
        toggleRT.anchorMax = Vector2.one;
        toggleRT.offsetMin = Vector2.zero;
        toggleRT.offsetMax = Vector2.zero;

        toggle = toggleGO.GetComponent<Toggle>();
        toggle.interactable = CanNavigatorInteractWithPanel;
        var toggleImage = toggleGO.GetComponent<Image>();
        toggleImage.color = SwivelUIColors.grayBg; // outer box
        toggle.targetGraphic = toggleImage;
        toggle.isOn = initial;

        // === Checkmark ===
        var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmarkGO.transform.SetParent(toggleGO.transform, false);
        var checkmarkImage = checkmarkGO.GetComponent<Image>();
        checkmarkImage.color = SwivelUIColors.greenBg; // filled green check
        checkmarkImage.raycastTarget = false;

        var checkmarkRT = checkmarkGO.GetComponent<RectTransform>();
        checkmarkRT.anchorMin = new Vector2(0.2f, 0.2f);
        checkmarkRT.anchorMax = new Vector2(0.8f, 0.8f);
        checkmarkRT.offsetMin = Vector2.zero;
        checkmarkRT.offsetMax = Vector2.zero;

        toggle.graphic = checkmarkImage;
        toggle.onValueChanged.AddListener(onChanged);

        return row;
      }

      public static GameObject AddRadioToggleRow(
        Transform parent,
        SwivelUISharedStyles viewStyles,
        string label,
        string[] optionLabels,
        int defaultIndex,
        Action<int> onChanged)
      {
        var row = CreateRow(parent, viewStyles, label, out _, false);
        var toggles = new Toggle[optionLabels.Length];
        var toggleImages = new Image[optionLabels.Length];

        for (var i = 0; i < optionLabels.Length; i++)
        {
          var toggleWrapper = new GameObject($"{optionLabels[i]}Wrapper", typeof(RectTransform), typeof(VerticalLayoutGroup));
          toggleWrapper.transform.SetParent(row.transform, false);

          var wrapperLayout = toggleWrapper.GetComponent<VerticalLayoutGroup>();
          wrapperLayout.childAlignment = TextAnchor.MiddleCenter;
          wrapperLayout.childControlHeight = true;
          wrapperLayout.childControlWidth = true;
          wrapperLayout.childForceExpandHeight = false;
          wrapperLayout.childForceExpandWidth = false;
          wrapperLayout.spacing = 2f;

          var container = new GameObject($"{optionLabels[i]}ToggleContainer", typeof(RectTransform), typeof(LayoutElement));
          container.transform.SetParent(toggleWrapper.transform, false);
          var layoutElement = container.GetComponent<LayoutElement>();
          layoutElement.minWidth = 48;
          layoutElement.minHeight = 48;
          layoutElement.preferredWidth = 48;
          layoutElement.preferredHeight = 48;
          layoutElement.flexibleWidth = 0;

          var toggleGO = new GameObject($"{optionLabels[i]}Toggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
          toggleGO.transform.SetParent(container.transform, false);
          var toggleImage = toggleGO.GetComponent<Image>();
          var isDefault = (i == defaultIndex);
          toggleImage.color = isDefault ? SwivelUIColors.greenBg : SwivelUIColors.grayBg;
          toggleImage.raycastTarget = true;
          toggleImages[i] = toggleImage;

          var toggle = toggleGO.GetComponent<Toggle>();
          toggle.interactable = CanNavigatorInteractWithPanel;
          toggle.isOn = isDefault;
          toggle.targetGraphic = toggleImage;

          var checkmarkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
          checkmarkGO.transform.SetParent(toggleGO.transform, false);
          var checkmarkImage = checkmarkGO.GetComponent<Image>();
          checkmarkImage.color = new Color(0.2f, 0.8f, 0.2f, 1f);
          checkmarkImage.raycastTarget = false;

          var checkmarkRT = checkmarkGO.GetComponent<RectTransform>();
          checkmarkRT.anchorMin = new Vector2(0.2f, 0.2f);
          checkmarkRT.anchorMax = new Vector2(0.8f, 0.8f);
          checkmarkRT.offsetMin = Vector2.zero;
          checkmarkRT.offsetMax = Vector2.zero;
          toggle.graphic = checkmarkImage;

          var toggleRT = toggleGO.GetComponent<RectTransform>();
          toggleRT.anchorMin = Vector2.zero;
          toggleRT.anchorMax = Vector2.one;
          toggleRT.offsetMin = Vector2.zero;
          toggleRT.offsetMax = Vector2.zero;

          toggles[i] = toggle;

          var index = i;
          toggle.onValueChanged.AddListener(value =>
          {
            if (!value)
            {
              // Radio button must stay checked if clicked again
              if (!toggles.Any(t => t != null && t.isOn))
              {
                toggle.SetIsOnWithoutNotify(true);
              }
              return;
            }

            for (var j = 0; j < toggles.Length; j++)
            {
              if (j == index) continue;
              if (toggles[j] != null)
              {
                toggles[j].SetIsOnWithoutNotify(false);
                if (toggleImages[j] != null) toggleImages[j].color = SwivelUIColors.grayBg;
              }
            }

            toggleImage.color = SwivelUIColors.greenBg;
            onChanged?.Invoke(index);
          });

          var labelGO = new GameObject($"{optionLabels[i]}Label", typeof(TextMeshProUGUI));
          labelGO.transform.SetParent(toggleWrapper.transform, false);
          var labelText = labelGO.GetComponent<TextMeshProUGUI>();
          labelText.text = optionLabels[i];
          labelText.fontSize = viewStyles.FontSizeRowLabel;
          labelText.color = viewStyles.LabelColor;
          labelText.alignment = TextAlignmentOptions.Center;
          labelText.enableAutoSizing = false;
        }

        return row;
      }
    }
  }