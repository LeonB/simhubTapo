# SimHub Tapo Plugin

SimHub plugin for controlling TP-Link Tapo smart plugs from SimHub actions.

The plugin connects to Tapo devices on your local network using your Tapo account credentials and configured device IP addresses. It currently exposes actions for switching plugs on, switching them off, and toggling the current state.

## Features

- SimHub plugin named `Tapo`
- Settings panel for Tapo username, password, and named smart plug devices
- Per-device SimHub actions registered under each device's configured name:
  - `{Name} On`
  - `{Name} Off`
  - `{Name} Toggle`
- Per-device startup and shutdown actions (on SimHub start/stop)
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

For each Tapo device, fill in a unique `Name`, its local `IP` address, and optionally the actions to run `On Startup` and `On Shutdown`. Click `Add Device` to save. Click a saved device to edit it, or click the trash icon next to it to remove it. For best results, reserve plug IP addresses in your router so they do not change.

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
- `System.Buffers.dll`
- `System.Formats.Asn1.dll`
- `System.IO.Pipelines.dll`
- `System.Memory.dll`
- `System.Numerics.Vectors.dll`
- `System.Runtime.CompilerServices.Unsafe.dll`
- `System.ValueTuple.dll`

Restart SimHub after copying the files.

## Usage In SimHub

After the plugin is installed and configured, create a SimHub control mapping, event, or button action and select one of the per-device actions. For a device named `Monitor`, the available actions are:

- `Monitor On`: turns the plug on
- `Monitor Off`: turns the plug off
- `Monitor Toggle`: reads the current plug state and switches it to the opposite state

Each device gets its own set of actions using the name you configured in the settings panel.

## Troubleshooting

If the log says `Tapo KLAP handshake was rejected with HTTP 403`, the plug is refusing local third-party API access. In the Tapo app, enable third-party compatibility or local access for the device, then verify the configured Tapo account email, password, and plug IP address. For newer firmware such as Tapo P115 `1.4.0`, also make sure the plug has internet access; reports from Home Assistant users indicate that this firmware can reject local control with `403` when the device is offline.

If the legacy protocol reports a response like `<html><body><center>200 OK</center></body></html>`, the device is answering with a generic web page instead of the old JSON API. Modern Tapo firmware usually requires KLAP and will not work through the legacy fallback.

## Current Limitations

- Device discovery is not implemented; plug IP addresses must be added manually.
- Actions are implemented as fire-and-forget async handlers, so connection errors are not surfaced in the UI.

## Vendored Library

This repository includes `libs/tapo-devices`, a .NET library for local-network TP-Link Tapo control. See [libs/tapo-devices/README.md](libs/tapo-devices/README.md) for supported device details and library notes.
