# Flake Desktop

A standalone Windows desktop application built from original Flake source code with C# and WPF on .NET 10.

**No Atoll source, runtime, packages, or project dependency is used by the standalone application or its build pipeline.**

## Architecture

- `Flake.App` — WPF application shell and navigation.
- `Flake.Core` — shared contracts and domain types.
- `Flake.SnowEngine` — snowfall simulation.
- `Flake.DesktopOverlay` — transparent Win32/WPF desktop surfaces.
- `Flake.Characters` — interactive desktop characters.
- `Flake.Snowman` — snowman composition and persistence.
- `Flake.PCManager` — safe Windows system information and utilities.
- `Flake.Settings` — JSON settings persistence.
- `Flake.UI` — shared WPF design system.

## Build

On Windows with the .NET 10 SDK:

```powershell
dotnet restore src/Flake.App/Flake.App.csproj
dotnet build src/Flake.App/Flake.App.csproj -c Release
dotnet publish src/Flake.App/Flake.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/publish
```

The published executable is `Flake.Desktop.exe`. GitHub Actions builds and verifies it on Windows. A tag matching `v*` creates a GitHub Release with the EXE attached.

## Development status

The standalone foundation and navigation shell are the first milestone. Feature modules are deliberately separated; a module is not considered complete until its behavior is implemented and tested on Windows 10/11. The dashboard is a navigation shell, not a claim that all planned features are already functional.

## Safety

PC Manager operations must use documented Windows mechanisms, require user confirmation for destructive actions, and never remove system files or disable Windows security features.
