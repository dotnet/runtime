---
name: build-and-test
description: >
  Build and test dotnet/runtime locally. Covers the mandatory baseline build,
  per-component build and test commands, Windows/PowerShell equivalents, running
  individual tests, and troubleshooting. USE FOR: any build or test invocation in
  this repo, and before making code changes under CCA. DO NOT USE FOR: CI pipeline
  triage (use ci-pipeline-monitor) or benchmarking (use performance-benchmark).
---

# Building & Testing in dotnet/runtime

## Shell Conventions

Commands below are written for bash. On Windows, use the `.cmd` entrypoints and PowerShell equivalents:

| bash | Windows (PowerShell) |
|------|----------------------|
| `./build.sh <args>` | `.\build.cmd <args>` |
| `src/tests/build.sh <args>` | `src\tests\build.cmd <args>` |
| `src/tests/run.sh <args>` | `src\tests\run.cmd <args>` |
| `export FOO=<value>` | `$env:FOO = '<value>'` |
| `$CORE_ROOT/corerun <Test>.dll` | `& "$env:CORE_ROOT\corerun.exe" <Test>.dll` |
| `find <dir> -name '<pattern>'` | `Get-ChildItem <dir> -Recurse -Filter '<pattern>'` |
| `tail -20 <log>` | `Get-Content <log> -Tail 20` |
| `grep '<pattern>' <log>` | `Select-String -Path <log> -Pattern '<pattern>'` |

Some arguments also differ in casing: `-priority1` on bash is `-Priority 1` on Windows.

## Baseline Build

A successful baseline build of the affected component is required for incremental builds and tests. Without it you'll hit "missing testhost" and "shared framework" errors that cost 20+ minutes per occurrence. If a baseline build fails, STOP, report the failure, and do not attempt to work around it.

### When a baseline is required

- **Under CCA — always, before changing any of the product code in the table below.** ⚠️ The environment is fresh, so there are no pre-existing artifacts and incremental builds fail in ways that waste significant compute. Skipping this step IS a task failure. Changes outside those paths — docs, markdown, workflow and instruction files — don't need one. **Exception:** if you are on a feature branch with commits upstream of main and the baseline build fails, make whatever code changes are needed to fix the build, then resume requiring a baseline.
- **Under CLI (interactive) — only when needed.** A usable baseline may already exist from prior work; don't re-run a 40-minute build unnecessarily. Check the component's [baseline sentinel](#baseline-sentinels) and build if it is missing. Otherwise attempt the work, and if it fails with a baseline-missing signature from [Troubleshooting](#troubleshooting), run the baseline once and retry — do not loop. Trust volunteered user signals ("just built", "fresh checkout") over probing.
- **Unsure which mode you're in?** Follow the CCA rule.

### Step 1: Build the Baseline (from repo root)

Pick the row matching the files you will modify:

| Files Changed | Component | Baseline Build |
|---------------|-----------|----------------|
| `src/coreclr/` | CoreCLR | `./build.sh clr+libs+host` |
| `src/mono/` | Mono | `./build.sh mono+libs` |
| `src/libraries/` (no Browser/WASM or WASI targets) | Libraries | `./build.sh clr+libs -rc release` |
| `src/libraries/` with Browser/WASM or WASI targets in the affected `.csproj` | WASM/WASI Libraries | `./build.sh mono+libs -os browser` |
| `src/native/corehost/`, `src/installer/` | Host | `./build.sh clr+libs+host -rc release -lc release` |
| `src/tools`, `src/native/managed` | Tools | `./build.sh clr+libs -rc release` |
| `src/tasks` | Build Tasks | None — `./build.sh tasks` is self-contained |
| `src/tests` | Runtime Tests | `./build.sh clr+libs -lc release -rc checked` |

**WASM/WASI Library Detection:** A change under `src/libraries/` is WASM/WASI-relevant if the library's `.csproj` has explicit Browser/WASM or WASI targets (`TargetFrameworks`, `TARGET_BROWSER`, `TARGET_WASI` constants, or `Condition` attributes referencing `browser`/`wasi`), **and** the changed file is not excluded from those targets via `Condition` on `<ItemGroup>` or `<Compile>`.

For System.Private.CoreLib changes, use `-rc checked` instead of `-rc release` for asserts.

Build on the branch you intend to modify — the baseline reflects your working tree at that moment. Baselining up front requires a clean HEAD; baselining after a probe failure means either stashing work-in-progress changes first or accepting that the baseline incorporates them.

⏱️ **This build can take up to 40 minutes.** Do not cancel unless no output for 5+ minutes.

Redirect it to a log and poll a bounded view rather than watching the console:

```bash
./build.sh clr+libs -rc release > artifacts/build.log 2>&1; echo "exit=$?" > artifacts/build.status
```

Then check `artifacts/build.status`, `tail -20 artifacts/build.log`, or grep the log for `: error`.
`artifacts/log/` holds the per-project binlogs for reporting failures.

### Baseline sentinels

A path under `artifacts/` whose absence means the baseline is missing.

| Component | Sentinel |
|-----------|----------|
| Libraries | `artifacts/bin/testhost/` and `artifacts/bin/microsoft.netcore.app.runtime.<RID>/<config>/`. Building a single library usually works without one; running its tests does not. |
| CoreCLR | `artifacts/bin/coreclr/<OS>.<arch>.<config>/`, plus `artifacts/tests/coreclr/<OS>.<arch>.<config>/Tests/Core_Root/` to run tests |
| Mono | `artifacts/bin/mono/<OS>.<arch>.<config>/`, plus `artifacts/tests/coreclr/<OS>.<arch>.<config>/Tests/Core_Root/` to run tests (Mono reuses the Core_Root layout) |
| WASM Libraries | `artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/<config>/` |
| Host | `artifacts/bin/coreclr/<OS>.<arch>.<config>/` and `artifacts/bin/testhost/` |
| Tools | `artifacts/bin/coreclr/<OS>.<arch>.<config>/` and `artifacts/bin/testhost/` |
| Build Tasks | None — `./build.sh tasks` is self-contained |
| Runtime Tests | `artifacts/tests/coreclr/<OS>.<arch>.<config>/Tests/Core_Root/`, produced by the baseline build plus `src/tests/build.sh -GenerateLayoutOnly` |

### Step 2: Configure Environment

```bash
export PATH="$(pwd)/.dotnet:$PATH"
dotnet --version  # Should match sdk.version in global.json
```

On Windows: `$env:PATH = "$PWD\.dotnet;$env:PATH"`

---

## Component Workflows

Read only the file for the component you are working on. All commands must complete with exit
code 0, and all tests must pass with zero failures.

| Component | Workflow |
|-----------|----------|
| Libraries, WASM/WASI Libraries | [`components/libraries.md`](components/libraries.md) |
| CoreCLR, Mono | [`components/runtime.md`](components/runtime.md) |
| Runtime Tests (`src/tests`) | [`components/runtime-tests.md`](components/runtime-tests.md) |
| Host, Tools, Build Tasks | [`components/host-and-tools.md`](components/host-and-tools.md) |

---

## Adding new tests

When creating a regression test for a bug fix:

1. **Verify the test FAILS without the fix** — build and run against the unfixed code.
2. **Verify the test PASSES with the fix** — apply the fix, rebuild, and run again.
3. If the fix is not yet merged locally, manually apply the minimal changes from the PR/commit to verify.

Do not mark a regression test task as complete until both conditions are confirmed.

## Troubleshooting

| Error | Solution |
|-------|----------|
| "shared framework must be built" | Run baseline build: `./build.sh clr+libs -rc release` |
| "testhost" missing / FileNotFoundException | Run baseline build first (Step 1 above) |
| Build timeout | Wait up to 40 min; only fail if no output for 5 min |
| "Target does not exist" | Avoid specifying a target framework; the build will auto-select `$(NetCoreAppCurrent)` |
| "0 test projects" after `build.sh -Test` | The test has `<CLRTestPriority>` > 0; add `-priority1` to the build command |

**When reporting failures:** Include logs from `artifacts/log/` and console output for diagnostics.
