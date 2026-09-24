# Changelog

English (US) release notes, mirroring CHANGELOG.md (Brazilian Portuguese). Before publishing a version, add a `## [VERSION]` section to **both** files: the workflow publishes the section matching the tag from each one and fails if either is missing.

## [1.5.1]
### What's new
- Local control API: while the app is running, other tools on the PC (remote-control agents running as a service, scripts, a Stream Deck) can drive console mode through the `\\.\pipe\ConsoleMode.Control` named pipe with the JSON commands `status`, `start`, `stop` and `show`. Only the signed-in user and LocalSystem can connect; nothing is exposed to the network.

## [1.5.0]
### What's new
- Full navigation with a PlayStation (DualSense / DualShock 4) or Xbox controller, on the home screen and in Settings: D-pad or left stick moves focus, ✕ / A selects, ○ / B closes lists and backs out of Settings, Options / Start opens Settings. In the first-run tour, ✕ / A goes on and ○ / B skips it.
- `ConsoleMode.exe --stop` leaves console mode and restores the desktop from the command line (handy for remote tools and scripts), the same as the tray's "Restore setup".

### Fixes
- The tray menu items (Show window, Enter console mode, Restore setup, Exit) work again.
- When entering console mode, the app waits for the TV to actually turn on (with retries) before turning the other screens off. If it doesn't, it says so and puts everything back instead of leaving you without a picture.
- Closing Big Picture restores the desktop again even when the regular Steam window was open.
- The home screen summary chips wrap to a new line when they don't fit instead of being cut off.
- The one-click shortcut is now called "Console Mode 1 Click" and is created next to the regular app shortcut instead of replacing it.

## [1.5.0-beta.4]
### Fixes
- The one-click shortcut is now called "Console Mode 1 Click" and is created next to the regular app shortcut instead of replacing it.

## [1.5.0-beta.3]
### Fixes
- The summary chips on the home screen (mode, audio, HDR, VRR…) wrap to a new line when they don't fit instead of being cut off. Very long names end in an ellipsis and show in full on hover.

## [1.5.0-beta.2]
### Fixes
- The tray menu items (Show window, Enter console mode, Restore setup, Exit) work again.
- When entering console mode, the app waits for the TV to actually turn on (with retries) before turning the other screens off. If it doesn't, it says so and puts everything back instead of leaving you without a picture.
- Closing Big Picture restores the desktop again even when the regular Steam window was open.
- Controller navigation in Settings also reaches options scrolled out of view.

## [1.5.0-beta.1]
### What's new
- Full navigation with a PlayStation (DualSense / DualShock 4) or Xbox controller: D-pad or left stick moves focus, ✕ / A selects, ○ / B closes lists and backs out of Settings, Options / Start opens Settings. In the first-run tour, ✕ / A goes on and ○ / B skips it.

## [1.4.0]
### What's new
- English (United States) interface, in addition to Brazilian Portuguese. Pick the language in Settings; it switches right away, without restarting the app.

### Fixes
- Resolution names ("Don't change", "(cached)", "(estimated)") and the On/Off buttons now follow the chosen language.
- Cached resolutions no longer lose their "(cached)" label when listed alongside estimated ones.
- The new-version notice shows the first item of the release notes instead of the "What's new" heading.

### How to update
- If you use 1.3.0, you get the notice inside the app: just click **Update now**. The installed version updates itself, and the portable one replaces its own file and reopens.
- Your settings are kept. The language stays Portuguese until you pick **English** in Settings.

## [1.3.0]
### What's new
- Native interface to choose which display to play on and what to do with the others.
- Displays are arranged by their actual position on the desktop.
- Controller navigation, a first-run tutorial, and a confirmation step to help avoid ending up with no picture.
- Settings for resolution, refresh rate, audio, HDR, VRR, and frame rate limit.
- Per-user installer that doesn't require administrator rights, plus a portable version.
- New-version notices from GitHub, with updates for both the installed and portable versions.
