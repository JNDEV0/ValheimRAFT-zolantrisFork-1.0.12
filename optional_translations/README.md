# ValheimRAFT - Optional Community Translations 🌐

This directory contains optional community localizations for the **ValheimRAFT** mod. By default, release distributions include only English to keep the mod package clean, lightweight, and clutter-free. 

If you play Valheim in another language, you can easily install your preferred translation from this folder.

---

## 📦 Available Languages

| Language | Folder | In-Game Name |
| :--- | :--- | :--- |
| **Chinese (Simplified)** | `Chinese/` | 中文 (简体) |
| **Russian** | `Russian/` | Русский |
| **German** | `German/` | Deutsch |
| **Spanish** | `Spanish/` | Español |
| **French** | `French/` | Français |
| **Portuguese (Brazilian)** | `Portuguese_Brazilian/` | Português (Brasil) |
| **Polish** | `Polish/` | Polski |
| **Japanese** | `Japanese/` | 日本語 |
| **Korean** | `Korean/` | 한국어 |

---

## 🛠️ How to Install

1. Download or copy the folder of your language (e.g. `Russian/` or `Chinese/`) from this directory.
2. Navigate to your Valheim mod installation directory:
   ```text
   <Valheim Directory>/BepInEx/plugins/ValheimRAFT/Assets/Translations/
   ```
3. Paste the language folder inside `Assets/Translations/`. For example:
   ```text
   BepInEx/plugins/ValheimRAFT/Assets/Translations/Russian/valheimraft.json
   ```
4. Start Valheim!

---

## ⚙️ How It Works

- **Automatic Detection**: If your Valheim game language matches the installed translation, ValheimRAFT will automatically apply it on startup.
- **In-Game Language Selector**: You can also manually switch languages at any time directly in the game:
  1. Approach any **Mechanism Switch** on your ship and press **[E]** to open the configuration panel.
  2. Locate the **Mod Language** dropdown.
  3. Select **Auto (Follow Game)**, **English**, or your installed language. The interface and vehicle tooltips will update immediately without restarting the game.
- **Fail-Safe English Fallback**: If an optional translation is not installed, or if any community translation file is ever missing a token, the mod automatically and cleanly defaults to English so you will never see broken `[variable_names]`.
