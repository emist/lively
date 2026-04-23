# Wallpaper Refresh Directive

## Goal
Add manual and auto-refresh capabilities for web-based wallpapers in Lively.

## Architecture
- **Lively Host** communicates with **LivelyCefSharp.exe** via stdin/stdout
- Sending `"Reload"` via stdin triggers a page reload in the CEF browser
- The CEF binary currently uses the **legacy plain-text protocol** (not JSON IPC)

## Manual Refresh
- System tray "Refresh Wallpaper" menu item calls `SetupDesktop.ReloadWebWallpapers()`
- Method iterates `webProcesses` list and sends `"Reload"` to each running CEF process

## Auto-Refresh
- Config properties: `WebWallpaperAutoReload` (bool) + `WebWallpaperAutoReloadIntervalMin` (int, minutes)
- `System.Threading.Timer` in SetupDesktop.cs ticks at the configured interval
- Timer starts when web wallpaper is added, stops when all are closed
- Settings UI: ToggleSwitch + NumericUpDown in the Settings tab

## CEF Modifications (Phase 2)
- `Form1.cs`: Detect page reloads via `LoadingStateChanged`, emit `PAGE_RELOADED` on stdout
- `CefMenuHandler.cs`: Add right-click "Refresh Wallpaper" context menu item

## Key Files
- `src/livelywpf/livelywpf/wp_lib/SetupDesktop.cs` — process management, reload logic
- `src/livelywpf/livelywpf/MainWindow.xaml.cs` — system tray menu, settings wiring
- `src/livelywpf/livelywpf/MainWindow.xaml` — settings UI controls
- `src/livelywpf/livelywpf/save/SaveData.cs` — config properties

## Edge Cases
- Guard against disposed/exited processes in `ReloadWebWallpapers()`
- Timer should not run if no web wallpapers are active
- Right-click in CEF only works when mouse input forwarding is active (RawInputDX)
