# SimHub Tapo Plugin

SimHub plugin for controlling TP-Link Tapo smart plugs from SimHub actions.

The plugin connects to a Tapo device on your local network using your Tapo account credentials and a configured device IP address. It currently exposes actions for switching the plug on, switching it off, and toggling the current state.

## Features

- SimHub plugin named `Tapo`
- Settings panel for Tapo username, password, and smart plug IP address
- SimHub actions:
  - `TapoOn`
  - `TapoOff`
  - `TapoToggle`
- Local network control through the included `tapo-devices` library
- Post-build copy step for installing the plugin into the SimHub folder

## Requirements

- Windows
- SimHub installed
- Visual Studio 2019 or newer, or MSBuild with .NET Framework targeting packs
- .NET Framework 4.8 developer pack
- A TP-Link Tapo smart plug already registered in the Tapo app
- The local IP address of the Tapo device

## Project Structure

```text
.
|-- Tapo.cs                         # SimHub plugin entry point and actions
|-- TapoSettings.cs                 # Serialized plugin settings
|-- SettingsControl.xaml            # SimHub settings UI
|-- SettingsControl.xaml.cs         # Settings UI event handlers
|-- TapoPlugin.csproj               # .NET Framework 4.8 plugin project
|-- TapoPlugin.sln                  # Visual Studio solution
`-- libs/tapo-devices/              # Vendored Tapo device library
```

## Configuration

In the SimHub plugin settings, enter:

- `User`: your Tapo account email or username
- `Password`: your Tapo account password
- `IP`: the local IP address of the Tapo smart plug

For best results, reserve the plug IP address in your router so it does not change.

## Building

The project expects SimHub assemblies through the `SIMHUB_INSTALL_PATH` environment variable.

Example:

```powershell
$env:SIMHUB_INSTALL_PATH = "C:\Program Files (x86)\SimHub\"
msbuild .\TapoPlugin.sln /p:Configuration=Release
```

The post-build step copies the plugin output and required JSON dependencies into `%SIMHUB_INSTALL_PATH%`.

## Manual Installation

If you do not use the post-build step, copy the built files from `bin\Debug` or `bin\Release` into your SimHub install directory:

- `Tapo.dll`
- `TapoDevices.dll`
- `System.Text.Json.dll`
- `System.Text.Encodings.Web.dll`
- `Microsoft.Bcl.AsyncInterfaces.dll`
- `System.IO.Pipelines.dll`

Restart SimHub after copying the files.

## Usage In SimHub

After the plugin is installed and configured, create a SimHub control mapping, event, or button action and select one of the plugin actions:

- `TapoOn`: turns the configured plug on
- `TapoOff`: turns the configured plug off
- `TapoToggle`: reads the current plug state, then switches it to the opposite state

## Current Limitations

- Only one plug IP address is stored in the settings.
- The `Add Device`, `On Startup`, and `On Shutdown` controls are present in the UI but are not wired to behavior yet.
- Actions are implemented as fire-and-forget async handlers, so connection errors are not surfaced in the UI.
- The plugin currently logs the configured username, password, and IP address when running some actions. Avoid sharing logs that contain credentials.

## Vendored Library

This repository includes `libs/tapo-devices`, a .NET library for local-network TP-Link Tapo control. See [libs/tapo-devices/README.md](libs/tapo-devices/README.md) for supported device details and library notes.
