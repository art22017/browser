# Argon

Argon is an experimental Windows browser built from [Zen](https://github.com/zen-browser/desktop) and Firefox. It combines vertical tabs and a frosted, textured interface with independent browsing profiles inside one Windows shell.

## Workspaces

Each **Argon workspace** starts its own Gecko parent process using its own profile under `%LOCALAPPDATA%\Argon\Workspaces\<UUID>\profile`. Cookies, cache, history, saved passwords, extensions, browser preferences and restored tab sessions therefore use separate Firefox profile storage. The shell shares only workspace names and identifiers. It does not clone existing profiles or expose a debugging port.

The native Windows shell embeds the active profile's browser HWND as a child window. Switching workspaces shows another already-running child window in the same shell. Inactive profiles keep running; memory use increases with the number of open workspaces. Gecko's renderer sandbox and extension signature checks remain enabled.

In hosted mode, Argon's shell is the workspace creation/switching entry point; the native Zen tab-space creation controls are hidden to avoid confusing tab organization with independent profiles. Authentication dialogs and browser windows explicitly opened by a site/user can remain separate native windows belonging to that workspace's process.

## Interface

- Vertical tabs with a neutral glass surface and an original tiled grain texture.
- Address bar above the page, permanent shell window controls and a rounded command palette on Ctrl+T.
- Unfocused HTTP(S) addresses display the complete ASCII hostname and port. Focusing or copying retains the full URL. HTTP is visibly identified; security indicators remain native.
- Platform sans-serif typography, original Argon artwork and matching preferences styling. These are independently authored designs inspired by the supplied Arc/Helium references.
- Local `!bangs`, including `!w`, `!wa`, `!k`, `!gh`, `!yt`, `!cgt`, `!mdn` and others. There are 25 bundled templates, not Helium's entire catalogue. Search suggestions do not receive bang input.
- Reduced-motion, reduced-transparency and high-contrast fallbacks in browser styling.

## Passwords and passkeys

Argon uses Firefox's password manager, profile encryption and Windows OS reauthentication. It preserves the native WebAuthn/Windows Hello implementation rather than adding a custom credential database. Device-backed passkeys belong to the authenticator/Windows account and relying party; they are not duplicated into each workspace profile. Signing into the same cloud sync account or installing an extension that synchronizes data can intentionally share data across profiles.

## Windows builds

Pushing the implementation branch starts the **Argon Windows** workflow. Once the workflow is on the default branch, it can also be run manually from GitHub Actions. It runs storage, native embedding and bang tests; cross-compiles the Gecko engine on a standard Ubuntu runner; publishes a self-contained .NET Windows shell; and assembles a portable ZIP and per-user installer. No Zen deployment key, private runner or update-signing secret is required. Build artifacts are named **Argon-Windows-x64** and include SHA-256 hashes.

Extract the full portable package and start `Argon.exe`. Its `engine` directory must stay beside it. The engine-only executable is not the workspace shell.

For shell development:

```powershell
dotnet build host/Argon.Host/Argon.Host.csproj
dotnet run --project host/Argon.Host.Tests/Argon.Host.Tests.csproj
dotnet run --project host/Argon.Native.Tests/Argon.Native.Tests.csproj
npm ci --ignore-scripts
npm run test:argon
```

## Verification status

The shell compiles and the storage/native fixture tests have passed locally. The fixture tests verify cross-process parenting, switching and graceful shutdown; they do not prove compatibility with Gecko, passkey dialogs, real websites, every DPI setup or extension behavior. Full-engine build and interactive browser acceptance are required before treating this as a finished browser.

There is no Argon automatic-update service or Windows code-signing certificate yet. Upstream Zen updates are deliberately not installed into Argon. Install fresh Argon builds to receive engine updates; published artifacts must not be described as production-ready or fully security-audited.

See [the acceptance checklist](docs/argon-acceptance.md) for the remaining runtime verification.

## License and credits

The source retains the Mozilla Public License 2.0. Firefox, Gecko and Zen retain their respective notices and authorship. Argon's shell, artwork and new modules are original additions. Helium and Arc are design references; their branding assets and Chromium code are not redistributed here.
