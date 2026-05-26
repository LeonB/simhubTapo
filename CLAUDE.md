# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build

Requires the `SIMHUB_INSTALL_PATH` environment variable pointing to the SimHub install directory. The post-build step copies the output DLLs there automatically.

```powershell
$env:SIMHUB_INSTALL_PATH = "C:\Program Files (x86)\SimHub\"
msbuild .\TapoPlugin.sln /p:Configuration=Release
```

After a successful build, restart SimHub to pick up the new plugin DLL.

**Always verify the project builds successfully after every code change before committing.** Use `/p:PostBuildEvent=""` to skip the XCOPY step when SimHub is not installed in the build environment:

```powershell
$env:SIMHUB_INSTALL_PATH = "C:\Program Files (x86)\SimHub\"; & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "TapoPlugin.sln" /p:Configuration=Debug "/p:PostBuildEvent=" /v:minimal
```

## Language version

The project targets **.NET Framework 4.8** and **C# 7.3**. Do not use C# 8+ syntax (e.g. `is not`, `??=`, switch expressions, default interface members) — the WPF temporary project the build system generates enforces this strictly and will fail with a clear `error CS8370` if violated.

## Architecture

The plugin is a single SimHub plugin class (`Tapoer` in `Tapo.cs`) that implements `IPlugin`, `IDataPlugin`, and `IWPFSettings`.

**Settings** (`TapoSettings.cs`) are serialized to JSON by SimHub via `ReadCommonSettings`/`SaveCommonSettings`. The authoritative device list is `DataPluginDemoSettings.Devices` — a list of `TapoDeviceConfig` objects each holding `Name`, `IP`, `OnStartup`, and `OnShutdown`. The older flat fields (`IP`, `DeviceIPs`, `OnStartup`, `OnShutdown`) are kept for backwards-compatibility and kept in sync via `SyncLegacyFields()`. Migration from the legacy format runs in `NormalizeDeviceSettings()` on both the plugin side (`Tapo.cs`) and the settings UI side (`SettingsControl.xaml.cs`) — both copies must be kept consistent.

**SimHub actions** are registered per device in `Init()` using the device's `Name` as a prefix: `{Name} On`, `{Name} Off`, `{Name}Toggle`. `RegisterDeviceActions` / `UnregisterDeviceActions` (using `PluginManager.ClearActions(Type, prefix)`) allow live re-registration from the settings UI without a SimHub restart. `PluginManager` has no `RemoveAction` method — `ClearActions(Type, string prefix)` is the only way to remove individual actions.

**Device communication** goes through the vendored `libs/tapo-devices` library (`TapoDeviceFactory`, `TapoPlug`). Connection always tries the modern KLAP protocol first; if that fails with anything other than HTTP 403, it retries with the legacy plaintext protocol. A 403 is treated as a configuration error (third-party access not enabled in the Tapo app) and surfaced immediately without fallback.

**Lifecycle actions** (`OnStartup`, `OnShutdown`) are per-device. Startup actions fire-and-forget from `Init()`. Shutdown actions block synchronously (up to 10 s) in `End()` via `ExecuteDeviceLifecycleActionsAndWait`.

**Settings UI** (`SettingsControl.xaml` / `SettingsControl.xaml.cs`) manages the device list directly against `Plugin.Settings.Devices`. Selecting a saved device loads it into the form for editing; the `_editingName` field tracks which device is being edited by its name. Device names must be unique. Adding or updating a device calls `Plugin.RegisterDeviceActions` / `Plugin.UnregisterDeviceActions` immediately.

## Keeping docs up to date

After any change, check whether `CLAUDE.md` and `README.md` need updating:

- **CLAUDE.md** — update if architecture, constraints, or non-obvious conventions changed (e.g. new fields on settings models, new API limitations discovered, renamed identifiers that are called out by name).
- **README.md** — update if user-facing behaviour changed (e.g. new UI controls, renamed actions, changed configuration steps, new limitations).
