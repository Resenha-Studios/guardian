<p align="center">
  <h1 align="center">🛡️ Guardian</h1>
  <p align="center">
    <strong>Find and fix errors, broken references, and performance issues in your Unity project fast and effortlessly!</strong>
  </p>
  <p align="center">
    <a href="https://github.com/Resenha-Studios/guardian/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License MIT"></a>
    <img src="https://img.shields.io/badge/Unity-6000.0%2B-black.svg?logo=unity" alt="Unity Version"></a>
    <img src="https://img.shields.io/badge/UI-UI%20Toolkit-informational.svg" alt="UI Toolkit"></a>
    <img src="https://img.shields.io/badge/PRs-Welcome-brightgreen.svg" alt="PRs Welcome"></a>
    <img src="https://img.shields.io/badge/Localization-5%20Languages-orange.svg" alt="Localization"></a>
  </p>
</p>

---

## ⚡ What is Guardian?

> ### 🎯 In a Nutshell
> **Guardian is an in-editor tool used to find and fix errors, missing scripts, and performance bottlenecks in your Unity code quickly and easily!**
> 
> With a single click (or shortcut **`Ctrl + G`** / **`Cmd + G`**), Guardian audits your entire project in milliseconds directly inside the Unity Editor and detects:
> - ❌ **Missing Scripts & Broken References**: Finds broken prefabs and deleted scripts before they crash your build.
> - ⚠️ **Code Anti-Patterns & Lag**: Spots expensive calls (like `FindObjectOfType`, `Camera.main`) and memory allocations (`new List`) hidden inside `Update()` loops.
> - 🌐 **Hardcoded Strings**: Ensures your UI is properly localized.
> - 📋 **One-Click Quick Fix**: Cleans corrupted components automatically and provides direct links to the exact line in your code editor.

---

## 🚀 How to Open

Open Guardian directly from the top menu:  
👉 **`Tools > Resenha > Guardian`** (or press **`Ctrl + G`** / **`Cmd + G`**).

---

## 📦 Key Features

- **Integrity Scanner**:
  - Automatically identifies GameObjects with corrupted or deleted `Missing MonoScript` components in Prefabs and open scenes.
  - One-click **Quick Fix** action to safely clean up orphan components using native undo records.
  - Detects broken `Missing Reference` fields across serialized properties.
- **Code Anti-Patterns Scanner**:
  - Scans project C# scripts for expensive native calls (`FindObjectOfType`, `GameObject.Find`, `Camera.main`, `GetComponent`) placed inside high-frequency frame loops (`Update`, `FixedUpdate`, `LateUpdate`, `OnGUI`).
  - Detects collection allocations (`new List`, `new Dictionary`, `new HashSet`) that cause avoidable Garbage Collection spikes.
  - Direct **Open Line** button to jump straight to the source line in your IDE.
- **Localization Compliance Scanner**:
  - Validates that runtime UI elements leverage `LocalizedString` rather than hardcoded string literals.
- **One-Click Markdown Export**:
  - Built-in **Copy Report** button generates a clean, structured Markdown breakdown ready to paste into issue trackers, pull requests, or team chats (Slack, Discord).
- **Clean UI Toolkit Dashboard**:
  - Weighted **Health Score** calculation with letter grades (`A+` to `F`).
  - Native multilingual interface supporting **English**, **Português**, **Español**, **日本語**, and **简体中文**.

---

## 🚀 Installation

### Via Unity Package Manager (Git URL)

1. Open your Unity project.
2. Go to **Window > Package Manager**.
3. Click the **`+`** icon in the top-left and select **Add package from git URL...**.
4. Paste the repository URL:
   ```text
   https://github.com/Resenha-Studios/guardian.git
   ```
5. Click **Add**.

Alternatively, add it directly to your `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.resenha.guardian": "https://github.com/Resenha-Studios/guardian.git"
  }
}
```

---

## 🛠️ Usage

1. Open Guardian from the top menu: `Tools > Resenha > Guardian` (Shortcut: `Ctrl + G` / `Cmd + G`).
2. Click **Scan Project** to run an instant audit.
3. Use the severity chips (`Critical`, `Warnings`, `Suggestions`) to filter results.
4. Click **Select Asset** to highlight the file in the Project window, or **Open Line** to jump directly to the relevant line in your IDE.
5. Click **Copy Report** to copy the full Markdown audit to your clipboard.

---

## 🧩 Creating Custom Scanners

Guardian is designed to be easily extensible. To create a new audit pass, implement `IGuardianScanner`:

```csharp
using System;
using System.Collections.Generic;
using Resenha.Guardian.Editor.Core;

public class CustomAudioScanner : IGuardianScanner
{
    public string Name => "Audio Quality Scanner";
    public string Description => "Ensures background music assets use streaming load type.";
    public IssueCategory Category => IssueCategory.Project;
    public bool IsEnabled { get; set; } = true;

    public IEnumerable<GuardianIssue> Scan(Action<float, string> onProgress)
    {
        var issues = new List<GuardianIssue>();
        // Add your custom validation logic here...
        return issues;
    }
}
```

Register your scanner in `GuardianRunner`:
```csharp
runner.RegisterScanner(new CustomAudioScanner());
```

---

## 🤝 Contributing

Contributions, bug reports, and scanner proposals are warmly welcome!
1. Fork the Project.
2. Create your Feature Branch (`git checkout -b feat/amazing-scanner`).
3. Commit your Changes (`git commit -m "feat: add audio import scanner"`).
4. Push to the Branch (`git push origin feat/amazing-scanner`).
5. Open a Pull Request.

---

## 📄 License

Distributed under the **MIT License**. See [`LICENSE`](./LICENSE) for more details.

Developed with ❤️ by **[Resenha Studios](https://github.com/Resenha-Studios)**.
