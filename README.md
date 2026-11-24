# Flake Desktop

**A winter-themed Windows desktop experience.**

Flake Desktop is planned as a free Windows 10/11 application combining a Dynamic Island-style interface based on [Atoll](https://github.com/Fuwni/dynamic-island-windows) with interactive snowfall, desktop characters, snowman building and PC management tools.

> The project is in early development. Features described in the roadmap are planned and are not yet implemented.

## Principles

- Free to download and use; no paid feature tiers or subscriptions.
- Keep the complete source code in this public repository alongside release builds.
- Publish Windows builds through GitHub Releases; a simple project website may link to the latest release.
- Donations through Telegram Stars are voluntary and do not unlock features.
- Contributions are welcome through issues and pull requests. Official releases remain under maintainer review.

## Planned features

- Atoll-inspired Dynamic Island interface, integrated into one application.
- Desktop snowfall with intensity, wind, accumulation, melting and multi-monitor support.
- Snow accumulation on window edges that follows window movement and resizing.
- HOT/COLD cursor interaction.
- Interactive penguins and a cursor-driven snowman builder.
- Manual snowplow action.
- PC Manager: installed apps, uninstall flow, storage overview and usage statistics.
- Windows startup integration and a self-contained Windows executable.

See [ROADMAP.md](docs/ROADMAP.md) for milestones and [CONTRIBUTING.md](CONTRIBUTING.md) for contribution rules.

## Development status

The repository currently contains the initial project scaffold. Do not treat roadmap items as completed functionality.

## Atoll attribution

The project plans to reuse and adapt parts of Atoll, a Windows Dynamic Island application by [Fuwni](https://github.com/Fuwni/dynamic-island-windows). Atoll is distributed under the MIT License. Its copyright and license notice must be preserved for any reused or adapted portions. See [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Building

The desktop application targets Windows and uses WPF on .NET 10. A Windows machine with the .NET 10 SDK is required.

```powershell
dotnet build src/Flake.Desktop/Flake.Desktop.csproj -c Release
```

## License

The license for original Flake Desktop code will be selected before the first public release. Third-party components retain their own licenses.
