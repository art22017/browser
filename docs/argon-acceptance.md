# Argon Windows acceptance

Run this checklist against the complete Actions package, not a standalone shell build. Record the commit, Windows version and display scaling. No account credentials or authentication tokens belong in test reports.

## Profile separation

1. Create Personal and Work using the bottom workspace control. Confirm different directories and different Gecko parent process IDs.
2. Open the same test origin in both. Set a different cookie, localStorage value, IndexedDB record and service-worker cache value in each. Reload both, then restart Argon. Confirm values remain independent.
3. Visit a unique URL in Personal. Confirm Work's history and address suggestions do not include it.
4. Save a test-site password in Personal. Confirm Work's password manager does not contain it; check that Windows reauthentication protects sensitive actions.
5. Install an AMO-signed extension in Personal. Confirm Work neither lists it nor runs its background process. Configure it differently in the two workspaces and verify independent extension storage.
6. Clear browsing data in Work. Confirm Personal remains authenticated and keeps its history/data.

## One-window hosting

- Switch among three live workspaces repeatedly. Confirm one shell taskbar window, correct active profile, no clicks delivered to a hidden viewport and consistent keyboard focus.
- Check resize, maximize/restore, minimize, Snap layouts and moving between 100%, 150% and 200% displays.
- Check Ctrl+T, Ctrl+L, Ctrl+W, Ctrl+Tab, Alt+F4, Ctrl+N, file pickers, download prompts, permission panels and notification placement. Document explicitly opened secondary windows.
- Check a legitimate OAuth popup and a test WebAuthn/Windows Hello flow. Authentication popups must belong to the correct profile, and shell switching must not redirect their sessions.
- Close a workspace with an unsaved form and close the shell during a download. Confirm normal Gecko shutdown, visible confirmation dialogs and no profile data corruption.
- Test startup failure and a profile lock. Retry must not spawn a second process against the same live profile. Check crash recovery and reopening a workspace after its engine exits.

## Design and routing

- Compare light/dark sidebar, top toolbar, Ctrl+T palette, menus and settings to the supplied references. Verify readable text and actual rendering of grain/blur on the embedded Gecko surface.
- Check opaque fallbacks in High Contrast and reduced transparency; check reduced motion.
- Compare an HTTPS page, HTTP page, subdomain, punycode hostname and nonstandard port. The shortened label must identify the real origin. Focus and clipboard must contain the full URL.
- Test `!gh a&b`, `!w 日本語`, `!wa sqrt(4)` and an unknown bang. Confirm proper encoding and direct navigation. Use a network trace to verify that typing a bang sends no search-suggestion request, even when ordinary suggestions are enabled.
- Verify Argon branding in shell, application icons, About dialog, settings, installer and installed shortcuts.

## Release prerequisites

- Complete Gecko build, native-runtime checks and the tests above.
- Resolve inherited dependency advisories in build tooling and record the exact engine revision.
- Establish the update-signing/code-signing pipeline and a timely Firefox security-update process before a production release.
- Add secure external-link dispatch/default-browser registration for the outer shell before using it as the system default browser.
