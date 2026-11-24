# Flake Desktop roadmap

This document describes intended work. A feature is not complete until it is implemented and tested.

## Milestone 0 — Foundation
- [x] Create public repository and initial source scaffold.
- [ ] Confirm build on Windows with .NET 10 SDK.
- [ ] Establish CI build and basic automated checks.
- [ ] Select a license for original Flake Desktop code.
- [ ] Document third-party components and notices.

## Milestone 1 — Atoll integration
- [ ] Audit Atoll architecture, dependencies, assets and startup lifecycle.
- [ ] Preserve required MIT attribution and license notices.
- [ ] Integrate the Dynamic Island UI into Flake Desktop.
- [ ] Add shared state/services for Flake controls.
- [ ] Verify original Atoll features still work after integration.

## Milestone 2 — Snow engine
- [ ] Transparent desktop overlay and multi-monitor handling.
- [ ] Snowfall modes, particle variation, wind and gusts.
- [ ] Accumulation, sliding, melting and performance controls.
- [ ] Window-edge tracking and snow caps that follow moved/resized windows.

## Milestone 3 — Interactions
- [ ] HOT/COLD cursor state and triple-click gesture.
- [ ] Penguins: thawing, movement, reactions and inactivity freezing.
- [ ] Cursor-driven snowman construction and accessories.
- [ ] Manual snowplow gesture and clearing behavior.

## Milestone 4 — PC Manager
- [ ] Installed applications inventory.
- [ ] Safe uninstall flow with confirmation and system-app warnings.
- [ ] Storage overview.
- [ ] Usage tracking from the point the feature is enabled.
- [ ] Clearly distinguish observed usage from unavailable historical data.

## Milestone 5 — Release
- [ ] Windows startup integration and settings persistence.
- [ ] Self-contained single-file Windows executable.
- [ ] GitHub Actions build and GitHub Releases workflow.
- [ ] Free project website with download link.
- [ ] Privacy, security and end-user documentation.

## Engineering constraints

- Never claim historical app usage is available when Windows does not expose reliable data for it. Start local usage tracking only after the user enables it.
- Ask before uninstalling applications; protect or clearly warn about system components.
- Keep overlays performant and support multiple displays.
- Keep source code available in the repository for every release.
