# Flake Desktop roadmap

A feature is complete only after implementation, Windows runtime testing, and documentation.

## Stage 1 — Standalone foundation
- [x] Separate application shell from the former integration project.
- [x] Create independent module projects and shared design resources.
- [x] Add JSON settings persistence.
- [x] Add navigation shell for all product areas.
- [ ] Verify clean Windows build and smoke test.

## Stage 2 — Snow engine and desktop overlay
- [ ] Reusable particle simulation with pooled particle state.
- [ ] Transparent click-through overlay per monitor.
- [ ] Density, speed, wind, gusts, flake variants and quality presets.
- [ ] Low-power idle behavior and frame pacing.

## Stage 3 — Accumulation and window interaction
- [ ] Screen snowbanks and window-edge surface sampling.
- [ ] Window movement, resize, close and monitor topology tracking.
- [ ] Melt, drift and redistribution.

## Stage 4 — HOT/COLD
- [ ] Cursor heat field and melt interaction.
- [ ] Cold field, slowdown and ice visual effects.

## Stage 5 — Characters and tools
- [ ] Penguins and behavior state machines.
- [ ] Snowman builder, accessories and saved designs.
- [ ] Snow cleaner and targeted/full clear operations.

## Stage 6 — PC Manager
- [ ] CPU, memory and disk telemetry.
- [ ] Process and installed-app inventory.
- [ ] Windows-native uninstall handoff.
- [ ] Startup management and confirmed temporary-file cleanup.

## Stage 7 — Settings and validation
- [ ] Theme, startup, multi-monitor and performance settings.
- [ ] Automated tests and Windows 10/11 runtime matrix.
- [ ] Accessibility, DPI and display-change validation.

## Stage 8 — Release
- [x] Windows x64 self-contained single-file CI pipeline.
- [x] EXE existence and size validation in CI.
- [x] Tag-triggered GitHub Release workflow.
- [ ] Successful clean CI run and published release.
