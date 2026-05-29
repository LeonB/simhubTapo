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
- Per-device reachability indicator in the settings UI (colored dot, checked on startup and on demand)
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

For each Tapo device, fill in a unique `Name`, its local `IP` address, and optionally the actions to run `On Startup` and `On Shutdown`. Click `Test` next to the IP field to check whether the device is reachable before saving. Click `Add Device` to save. Each saved device shows a colored dot (green = reachable, red = unreachable, gray = unchecked) and a refresh icon button to re-run the check at any time. Click a saved device to edit it, or click the trash icon next to it to remove it. For best results, reserve plug IP addresses in your router so they do not change.

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

## Future Improvements

- **Per-session actions** — "On Game Start" and "On Game End" hooks that fire when a SimHub game session begins or ends, rather than only on SimHub process startup/shutdown. More useful for race setups where you want devices to respond to individual sessions.
- **Action error feedback** — Actions are currently fire-and-forget with errors silently logged. Showing a last-action status (timestamp + success/fail) per device in the settings list would make failures visible without blocking anything.
- **SimHub data properties** — Expose per-device on/off state and wattage as SimHub properties so they can drive dashboard overlays or other plugin mappings. `GetEnergyUsageAsync()` already exists on `TapoPlug`.
- **Delayed on/off actions** — Register additional SimHub actions such as `{Name} On in 30s` using `TurnOnWithDelayAsync` / `TurnOffWithDelayAsync`, which are already implemented in the device library.
- **Energy monitoring** — Surface per-session power consumption data as SimHub properties, allowing overlays or logging of wattage during a race session.
- **Bulb support** — `TapoBulb` is already present in the vendored library with brightness and colour control. Adding a device-type selector in the settings UI would unlock these devices without any new protocol work.
- **Device groups** — A named group that maps to multiple devices and registers combined SimHub actions (e.g. `Race Setup On` turns on monitor, fan, and LEDs at once).
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
