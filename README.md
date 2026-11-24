# Flake Desktop

**A winter-themed Windows desktop experience built around Atoll.**

Flake Desktop is a free Windows 10/11 application. The Atoll Dynamic Island codebase is connected to this repository as a pinned Git submodule at `vendor/Atoll`. The build pipeline publishes the Atoll-based Windows app under the Flake Desktop executable name.

> Development status: the first Flake snowfall prototype is being built and validated. It currently draws falling snow on each detected display and supports basic density, speed and wind configuration. It is not yet a complete snow simulation: accumulation, window-edge tracking, melting and interactive objects are still in development.

## Principles

- Free to download and use; no paid feature tiers or subscriptions.
- Keep Flake source and the pinned Atoll source reference public.
- Publish Windows builds through GitHub Actions and GitHub Releases.
- Donations are voluntary and do not unlock features.
- Contributions are welcome through issues and pull requests. Official releases remain under maintainer review.

## Planned Flake features

- Desktop snowfall with intensity, wind, accumulation, melting and multi-monitor support.
- Snow accumulation on window edges that follows window movement and resizing.
- HOT/COLD cursor interaction.
- Interactive penguins and a cursor-driven snowman builder.
- Manual snowplow action.
- PC Manager: installed apps, uninstall flow, storage overview and usage statistics.

## Snow prototype configuration

On first launch, the prototype creates:

`%AppData%\Flake\snow-settings.json`

Example:

```json
{
  "Intensity": 2,
  "Speed": 1.0,
  "Wind": 0.35
}
```

- `Intensity`: 0 = Off, 1 = Light, 2 = Steady, 3 = Heavy, 4 = Variable.
- `Speed`: multiplier from 0.1 to 3.0.
- `Wind`: direction and strength from -2.0 to 2.0.

These are prototype configuration values, not yet controls in the Atoll settings interface. Restart the application after editing the file.

## Atoll source

Atoll is included as a pinned submodule:

- Upstream: https://github.com/Fuwni/dynamic-island-windows
- Local path: `vendor/Atoll`
- Pinned revision: `012560b3b2138b3d2dfb605e864063204cbe8e4e`

Clone with source included:

```powershell
git clone --recurse-submodules https://github.com/arietjxbdbd-bot/Atoll-and-flake.git
```

Atoll remains under its upstream MIT license. The full notice is preserved in [licenses/Atoll-MIT.txt](licenses/Atoll-MIT.txt) and attribution details are in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Build

On Windows with the .NET 10 SDK:

```powershell
dotnet restore vendor/Atoll/DynamicIsland.csproj
dotnet publish vendor/Atoll/DynamicIsland.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:AssemblyName=Flake.Desktop -o artifacts/publish
```

GitHub Actions runs this publish command automatically. The workflow uploads `Flake.Desktop.exe` as a downloadable artifact. A tagged release (`v*`) publishes the same executable to GitHub Releases.

## Current status

Atoll source is connected. The first snowfall overlay, multi-display placement and file-based prototype settings are in the feature branch and require CI and real Windows runtime testing. The Flake-specific features listed above are not all implemented.

See [ROADMAP.md](docs/ROADMAP.md) for milestones and [CONTRIBUTING.md](CONTRIBUTING.md) for contribution rules. The license for original Flake code will be selected before the first public release.
