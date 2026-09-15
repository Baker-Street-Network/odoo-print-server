# Odoo Print Server
This is the server that runs on the Windows computer to do the printing.

It is a .NET 8 WinForms app packaged with [Velopack](https://velopack.io). Installed copies
check GitHub Releases for updates 30 seconds after startup and then every 4 hours, and
update and restart themselves automatically.

## Releasing
Releases are built by GitHub Actions ([.github/workflows/build.yml](.github/workflows/build.yml)).
The workflow runs when a tag matching `v*.*.*` is pushed. It:

1. Publishes a self-contained, single-file `win-x64` build, stamped with the tag's version.
2. Packages it with Velopack (`vpk pack`).
3. Creates a GitHub Release named `Release <version>` with the installer and update feed
   files attached, and auto-generated release notes.

### Steps
1. Commit and push your changes to `main`.
2. Pick the next version. Check the latest one with `git tag --sort=-v:refname` or on the
   [Releases page](https://github.com/Baker-Street-Network/odoo-print-server/releases).
   The tag **must** start with `v` (e.g. `v1.0.7`), or the workflow won't run.
3. Create and push the tag:
   ```powershell
   git tag v1.0.7
   git push origin v1.0.7
   ```
4. Watch the build under the repo's **Actions** tab (or `gh run watch`). It takes about 2–3 minutes.
5. When it finishes, the release shows up on the Releases page. Installed clients pick it up
   on their next update check.

The version must be higher than the last release, or Velopack clients won't treat it as an update.
You don't need to change `<Version>` in `OdooPrintServer.csproj`, because the workflow overrides it.

### Manual run
You can also start the workflow from the **Actions** tab ("Build and Release" → **Run workflow**)
and enter a version such as `1.0.7`. The workflow creates the `v1.0.7` tag on the release for you.

### First-time install
Download `OdooPrintServer-win-Setup.exe` from the latest release and run it on the print
computer. The app registers itself to start with Windows.

## Building locally
To build an installer on your own machine without publishing a release:

```powershell
dotnet tool install -g vpk
.\build-release.ps1 -Version 1.0.7
```

The output goes to `.\releases`.
