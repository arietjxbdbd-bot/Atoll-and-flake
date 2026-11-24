# Flake Desktop

**A winter-themed Windows desktop experience built around Atoll.**

Flake Desktop is a free Windows 10/11 application. The Atoll Dynamic Island codebase is now connected to this repository as a pinned Git submodule at `vendor/Atoll`. The build pipeline publishes the Atoll-based Windows app under the Flake Desktop executable name.

> Important: Atoll source integration and the build pipeline are in progress. Snowfall, penguins, HOT/COLD cursor, snowman building and PC Manager are planned Flake features and are not implemented yet.

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

Atoll source is connected; the first integrated build is being validated. The Flake-specific snow features still require implementation and testing.

See [ROADMAP.md](docs/ROADMAP.md) for milestones and [CONTRIBUTING.md](CONTRIBUTING.md) for contribution rules. The license for original Flake code will be selected before the first public release.
