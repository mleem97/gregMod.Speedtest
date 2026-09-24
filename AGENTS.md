# AGENTS.md — Notes for AI agents (gregMod.Speedtest)

Repo: gregMod.Speedtest · License: see `LICENSE` if present, else Apache-2.0 · Version: see `VERSION` (0.1.0).

MelonMod for Data Center (`SpeedtestMod`). In-game network/port speed
measurement with results panel. Declares `gregCore` in `manifest.json`
dependencies — still keep the soft-dependency pattern (probe + JIT split)
so the mod degrades gracefully without gregCore.

## Duties

1. **Read first:** `README.md`, `manifest.json`, `src/` — only then make changes.
2. **Do not commit secrets** (keys, tokens, `.env`). Use keys only via environment variables.
3. **Preserve history:** no `push --force`, no history rewrite without instruction.
4. **Verify changes:** before reporting done, build the mod (`dotnet build gregMod.Speedtest.csproj -c Release` or `./build.sh Speedtest` from `ModRepositories/`).
5. **Keep docs in sync:** for new features update `README.md` + `CHANGELOG.md` (Unreleased) and bump `manifest.json` version in lockstep with `VERSION`.
6. **Conventions:** Conventional Commits (`feat:`, `fix:`, `docs:`, `chore:` …), one logical change per commit.
7. **When unsure:** stop and ask instead of guessing — especially for deletes, migrations, CI.

## Build and references

- Target: `net6.0`, x64. Game: Data Center (`MelonGame("Waseku", "Data Center")`).
- `references/` holds absolute symlinks into the Steam Data Center install.
  Never commit `references/*.dll`, `bin/`, or `obj/`.
- After a fresh clone, run `../tools/sync-melon-assemblies.sh`.
- Deploy only with `./build.sh Speedtest --deploy`.

## Hard rules

- Measurements never mutate game state — observe and report only.
- Long measurements run off the main thread; marshal UI updates back to the
  main thread. Never block the game loop.
- Defensive `try/catch` in every per-frame path; no per-frame reflection.

## Layout

- `src/SpeedtestMod.cs` — MelonMod entry, prefs, toggle.
- `src/SpeedtestEngine.cs` — measurement engine (threading lives here).
- `src/SpeedtestPanel.cs` — results UI.
