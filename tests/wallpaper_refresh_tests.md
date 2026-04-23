# Wallpaper Refresh — Test Plan

Since this is a WPF desktop application without a unit test framework set up,
these tests are structured as manual verification steps and code review checks.

## Test 1: Config Serialization Roundtrip

**Goal:** Verify new config properties serialize/deserialize correctly.

**Steps:**
1. Run Lively → close it → open `SaveData/lively_config.json`
2. Verify `WebWallpaperAutoReload` (bool) and `WebWallpaperAutoReloadIntervalMin` (int) exist
3. Set `WebWallpaperAutoReload: true` and `WebWallpaperAutoReloadIntervalMin: 15` in the JSON
4. Restart Lively → verify Settings tab shows Auto-Refresh ON with 15 minutes selected

**Expected:** Properties persist across restarts. Default values: `false` and `30`.

---

## Test 2: Manual Refresh via System Tray

**Goal:** Verify "Refresh Wallpaper" tray menu item works.

**Steps:**
1. Set a URL wallpaper (e.g., `https://www.shadertoy.com/view/MsKcRh`)
2. Right-click Lively icon in system tray
3. Verify "Refresh Wallpaper" appears between "Customize Wallpaper" and the separator
4. Click "Refresh Wallpaper"
5. Verify the wallpaper page reloads (visual flicker/re-render)

**Expected:** Page reloads without process restart. Log shows `Sent Reload to web wallpaper PID:XXXX`.

---

## Test 3: Manual Refresh with No Web Wallpapers

**Goal:** Verify no crash when refreshing with non-web wallpapers.

**Steps:**
1. Set a GIF or video wallpaper (not web/url)
2. Right-click tray → "Refresh Wallpaper"

**Expected:** Nothing happens. No crash. No error dialog.

---

## Test 4: Auto-Reload Timer

**Goal:** Verify periodic auto-reload works.

**Steps:**
1. Set a URL wallpaper
2. Open Settings → Web Browser section
3. Toggle "Auto-Refresh" ON
4. Set interval to "5 minutes"
5. Wait 5 minutes
6. Verify wallpaper reloads (check logs for `Sent Reload` messages)

**Expected:** Log shows reload every 5 minutes. Timer stops when wallpaper is closed.

---

## Test 5: Auto-Reload Timer Lifecycle

**Goal:** Verify timer starts/stops correctly.

**Steps:**
1. Enable auto-reload with 5 min interval
2. Set a web wallpaper → verify log: "Auto-reload timer started: 5 min"
3. Close all wallpapers → verify log: "Auto-reload timer stopped."
4. Set another web wallpaper → verify timer restarts

**Expected:** Timer lifecycle matches wallpaper lifecycle.

---

## Test 6: Settings UI State Persistence

**Goal:** Verify UI controls restore correctly.

**Steps:**
1. Enable auto-reload, set interval to "6 hours"
2. Close Lively settings tab, reopen it
3. Verify toggle is ON and combo shows "6 hours"
4. Restart Lively
5. Verify settings persist

**Expected:** All UI state matches saved config values.

---

## Test 7: Interval ComboBox Disabled When Toggle Off

**Goal:** Verify combo is disabled when auto-reload is off.

**Steps:**
1. Toggle auto-reload OFF
2. Verify interval combo box becomes disabled (grayed out)
3. Toggle ON → verify combo becomes enabled

**Expected:** ComboBox `IsEnabled` tracks toggle state.

---

## Test 8: PAGE_RELOADED Handler (Phase 2 only)

**Goal:** Verify reload detection from CEF → Lively.

**Prerequisite:** Rebuilt CEF binary with Phase 2 changes.

**Steps:**
1. Set a web wallpaper
2. Trigger a navigation within the page (click a link that changes the URL)
3. Check Lively log for "Web wallpaper page reloaded: <filepath>"

**Expected:** Log message appears for each page navigation/reload.

---

## Test 9: Right-Click Refresh in Browser (Phase 2 only)

**Goal:** Verify CEF right-click context menu.

**Prerequisite:** Rebuilt CEF binary + mouse input forwarding enabled.

**Steps:**
1. Set a web wallpaper
2. Enable mouse input forwarding in Settings
3. Right-click on the desktop wallpaper area
4. Verify "Refresh Wallpaper" appears in context menu
5. Click it → verify page reloads

**Expected:** Custom context menu item works. Note: may conflict with Windows desktop context menu.

---

## Code Review Checks

- [ ] `ReloadWebWallpapers()` guards against `HasExited` and null processes
- [ ] `autoReloadTimer` is disposed properly (no memory leaks)
- [ ] `StartAutoReloadTimer()` is idempotent (calls `StopAutoReloadTimer()` first)
- [ ] ComboBox interval index mapping is correct: [5, 10, 15, 30, 60, 360, 720, 1440]
- [ ] Event handlers are subscribed in `SubcribeUI()` (not XAML) to prevent init-time triggers
- [ ] `CloseAllWallpapers()` stops the auto-reload timer
- [ ] CEF `isFirstLoad` flag prevents duplicate `msg_wploaded` on reloads
