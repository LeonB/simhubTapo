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

- Actions are implemented as fire-and-forget async handlers, so connection errors are not surfaced in the UI.

## Planned Improvements

- **Test button** — No way to verify a device responds from within the settings UI. A Test button in the device form that briefly toggles the plug would confirm the connection before binding it to SimHub events.
- **Credential check before any device is saved** — The automatic credential check requires at least one device to be configured; if you are setting up credentials for the first time there is no feedback. The check should also be usable against the IP currently typed in the form.
- **Keep discovery results visible after selection** — Clicking a discovered device fills the form but dismisses the list. If you want to add multiple devices from one scan you have to re-scan each time.
- **Device list readability** — Each saved device is rendered as a single long line. A two-line layout (name and IP on the first line, MAC and lifecycle settings on a smaller second line) would be easier to scan with several devices configured.
- **Per-device reachability indicator** — The device list shows no indication of whether a device is currently reachable. A small status indicator (populated during the network scan or on plugin startup) would make misconfigured or offline devices immediately visible.
- **Cloud API device discovery** — In addition to local UDP broadcast, support fetching the device list from the Tapo cloud API using the configured credentials. The cloud API returns each device's alias, MAC address, model, and type — but **not** its local IP address. The intended integration is to correlate cloud results with local scan results by MAC address: the UDP scan finds IPs, the cloud provides aliases. This means cloud discovery adds value when used alongside the existing scan, not as a standalone replacement.

  **Implementation notes (for when this is built):**
  - Endpoint: `POST https://wap.tplinkcloud.com` for all calls (single path, method in JSON body). Regional variants exist (`eu-wap`, `use1-wap`, `aps1-wap`) but the global endpoint works for device listing.
  - Step 1 — login: `{ "method": "login", "params": { "appType": "Kasa_Android", "cloudUserName": "...", "cloudPassword": "...", "terminalUUID": "<any-uuid-v4>" } }` → returns `result.token`.
  - Step 2 — list devices: `{ "method": "getDeviceList", "params": { "token": "..." } }` → returns `result.deviceList[]` with fields: `alias`, `deviceMac`, `deviceModel`, `deviceType`, `deviceId`, `appServerUrl`, `status`.
  - Filter to plugs by `deviceType == "SMART.TAPOPLUG"`.
  - No new dependencies needed — plain `HttpClient` + `System.Text.Json`, same as the rest of the library. New class `TapoCloudClient` with `LoginAsync` and `GetDeviceListAsync`.
  - **Caveats:** unofficial/undocumented API (TP-Link can change it without notice); rate-limited (error `-20004` if called too often — fine for one-shot settings UI use, not for polling); requires internet access.

## Vendored Library

This repository includes `libs/tapo-devices`, a .NET library for local-network TP-Link Tapo control. See [libs/tapo-devices/README.md](libs/tapo-devices/README.md) for supported device details and library notes.
