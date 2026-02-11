# ⏱️ Rolaco Schedule

![.NET
Version](https://img.shields.io/badge/.NET-6.0-512BD4?style=flat-square&logo=dotnet)
![WPF](https://img.shields.io/badge/GUI-WPF-5C2D91?style=flat-square&logo=windows)
![License](https://img.shields.io/badge/License-MIT-green?style=flat-square)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D6?style=flat-square&logo=windows)

**Rolaco Schedule** is a modern desktop application built with **WPF and
.NET 6**, designed to help you manage your daily tasks and stay focused
using a clean, dark-themed timer interface.

------------------------------------------------------------------------

## 📸 App Screenshot

![Rolaco Schedule Main Interface](AppScreenshot/screenshot.png)

> *Dark modern UI with task management and focus timer*

------------------------------------------------------------------------

## ✨ Features

  -----------------------------------------------------------------------
  Feature                     Description
  --------------------------- -------------------------------------------
  ✅ **Task Management**      Add, edit, delete, and track tasks with
                              custom duration

  ⏳ **Focus Timer**          Start a timer linked to a specific task
                              with visual feedback

  ☕ **Break Mode**           Take a 15-minute break and resume your
                              session automatically

  🔔 **Smart Notifications**  Popup window + tray notifications with
                              taskbar flashing

  🧠 **Pomodoro Inspired**    Stay productive with timed work sessions

  🎨 **Dark Modern UI**       Clean, gold-accented interface with custom
                              WPF styles

  💾 **Persistent Storage**   Tasks saved locally in `tasks.json` using
                              JSON serialization

  🖥️ **System Tray**          Minimize to tray with quick controls and
                              notifications

  🚀 **Single Executable**    Publish as standalone .exe with .NET 6
  -----------------------------------------------------------------------

------------------------------------------------------------------------

## 🛠️ Built With

-   **[C# / .NET 6](https://dotnet.microsoft.com/)** -- Core framework
-   **[Windows Presentation Foundation
    (WPF)](https://docs.microsoft.com/en-us/dotnet/desktop/wpf/)** -- UI
    framework
-   **[Windows
    Forms](https://docs.microsoft.com/en-us/dotnet/desktop/winforms/)**
    -- NotifyIcon integration
-   **JSON** -- Local task storage
-   **XAML** -- Custom styles, templates, and converters

------------------------------------------------------------------------

## 🚀 Getting Started

### Prerequisites

-   [.NET 6 SDK](https://dotnet.microsoft.com/download/dotnet/6.0)
-   Windows 7 or later
-   Visual Studio 2022 (recommended)

### Installation

1.  **Clone the repository:**

    ``` bash
    git clone https://github.com/Rolaco0/rolaco-schedule.git
    ```

2.  **Open the project:**

    -   Open `RolacoSchedule.sln` in Visual Studio 2022\
    -   Or open the folder directly

3.  **Build and run:**

    -   Press `F5` to build and run\
    -   Or use .NET CLI:

    ``` bash
    dotnet run
    ```

------------------------------------------------------------------------

## 📦 Publish as Single Executable

To create a standalone `.exe` file:

``` bash
dotnet publish -c Release -r win-x64 --self-contained true
```

📁 Output:\
`bin\Release\net6.0-windows\win-x64\publish\RolacoSchedule.exe`

------------------------------------------------------------------------

## 📬 Contact

Discord: 6a.b

GitHub: @Rolaco0
