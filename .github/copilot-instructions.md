---
excludeAgent: code-review-agent
---

**Any code you commit MUST compile, and new and existing tests related to the change MUST pass.**

You MUST make your best effort to ensure any code changes satisfy those criteria before committing. If for any reason you were unable to build or test code changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

If you make code changes, do not complete without checking the relevant code builds and relevant tests still pass after the last edits you make. Do not simply assume that your changes fix test failures you see, actually build and run those tests again to confirm.

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](/.editorconfig).

In addition to the rules enforced by `.editorconfig`, you SHOULD:

- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- When adding new unit tests, strongly prefer to add them to existing test code files rather than creating new code files.
- When adding new test files, examine the directory structure of sibling tests first. Some test directories use flat files (e.g., `GCEvents.cs` alongside `GCEvents.csproj`) while others use per-test subdirectories. Match the existing convention.
- When working with tests, look for `README.md` files along the directory hierarchy (starting from the test's directory and walking up). These contain build, run, and authoring guidance specific to that test area.
- When adding new unit tests, avoid adding a regression comment citing a GitHub issue or PR number unless explicitly asked to include such information.
- If you add new code files, ensure they are listed in the csproj file (if other files in that folder are listed there) so they build.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
- Do not finish work with any tests commented out or disabled that were not previously commented out or disabled.
- When writing tests, do not emit "Act", "Arrange" or "Assert" comments.
- For markdown (`.md`) files, ensure there is no trailing whitespace at the end of any line.
- When adding XML documentation to APIs, follow the guidelines at [`docs.prompt.md`](/.github/prompts/docs.prompt.md).

---

# Building & Testing in dotnet/runtime

## ⚠️ MANDATORY: Run Baseline Build First

**You MUST complete a baseline build BEFORE making any code changes.** Skipping this causes "missing testhost" and "shared framework" errors that waste time.

### Step 1: Identify Your Component

Based on file paths you will modify:

| Files Changed | Component |
|---------------|-----------|
| `src/coreclr/` | CoreCLR |
| `src/mono/` | Mono |
| `src/libraries/` (no Browser/WASM or WASI targets) | Libraries |
| `src/libraries/` with Browser/WASM or WASI targets in the affected `.csproj` | WASM/WASI Libraries |
| `src/native/corehost/`, `src/installer/` | Host |
| `src/tools` | Tools |
| `src/native/managed` | Tools |
| `src/tasks` | Build Tasks |
| `src/tests` | Runtime Tests |

**WASM/WASI Library Detection:** A change under `src/libraries/` is WASM/WASI-relevant if the library's `.csproj` has explicit Browser/WASM or WASI targets (`TargetFrameworks`, `TARGET_BROWSER`, `TARGET_WASI` constants, or `Condition` attributes referencing `browser`/`wasi`), **and** the changed file is not excluded from those targets via `Condition` on `<ItemGroup>` or `<Compile>`.

### Step 2: Run the Baseline Build (from repo root)

**First, checkout the `main` branch** to establish a known-good baseline, then run the appropriate build command:

| Component | Linux / macOS | Windows |
|-----------|---------------|---------|
| **CoreCLR** | `./build.sh clr+libs+host` | `.\build.cmd clr+libs+host` |
| **Mono** | `./build.sh mono+libs` | `.\build.cmd mono+libs` |
| **Libraries** | `./build.sh clr+libs -rc release` | `.\build.cmd clr+libs -rc release` |
| **WASM Libraries** | `./build.sh mono+libs -os browser` | `.\build.cmd mono+libs -os browser` |
| **Host** | `./build.sh clr+libs+host -rc release -lc release` | `.\build.cmd clr+libs+host -rc release -lc release` |
| **Tools** | `./build.sh clr+libs -rc release` | `.\build.cmd clr+libs -rc release` |
| **Build Tasks** | `./build.sh clr+libs -rc release` | `.\build.cmd clr+libs -rc release` |
| **Runtime Tests** | `./build.sh clr+libs -lc release -rc checked` | `.\build.cmd clr+libs -lc release -rc checked` |

For System.Private.CoreLib changes, use `-rc checked` instead of `-rc release` for asserts.

⏱️ **This build can take up to 40 minutes.** Do not cancel unless no output for 5+ minutes.

### Step 3: Configure Environment

**Linux / macOS:**
```bash
export PATH="$(pwd)/.dotnet:$PATH"
dotnet --version  # Should match sdk.version in global.json
```

**Windows:**
```cmd
set PATH=%CD%\.dotnet;%PATH%
dotnet --version
```

**Only proceed with changes after the baseline build succeeds.** If it fails, report the failure and stop. After the baseline build, switch back to your working branch before making changes.

---

## Component-Specific Workflows

After completing the baseline build above (the baseline build MUST be completed before running tests), use the appropriate workflow for your changes.
All commands must complete with exit code 0, and all tests must pass with zero failures.

### Libraries (Most Common)

**Build and test a specific library:**
```bash
cd src/libraries/<LibraryName>
dotnet build
dotnet build /t:test ./tests/<TestProject>.csproj
```

Test projects are typically at: `tests/<LibraryName>.Tests.csproj` or `tests/<LibraryName>.Tests/<LibraryName>.Tests.csproj`, or under `tests/FunctionalTests/`, `tests/UnitTests/`, etc. Use `find tests -name '*.Tests.csproj'` to discover them.

**Test all libraries:** `./build.sh libs.tests -test -rc release`

**System.Private.CoreLib:** Rebuild with `./build.sh clr.corelib+clr.nativecorelib+libs.pretest -rc checked`

Before completing, ensure ALL tests for affected libraries pass.

### CoreCLR

**Test:** `cd src/tests && ./build.sh && ./run.sh`

### Mono

**Test:**
```bash
./build.sh clr.host
cd src/tests
./build.sh mono debug /p:LibrariesConfiguration=debug
./run.sh
```

### WASM Libraries

**Build:** `./build.sh libs -os browser`

**Test:** `./build.sh libs.tests -test -os browser`

### Host

**Build:** `./build.sh host -rc release -lc release`

**Test:** `./build.sh host.tests -rc release -lc release -test`

### Tools

**Build:** `./build.sh tools+tools.ilasm`

**Test:** `./build.sh tools+tools.ilasm+tools.illinktests+tools.cdactests -test`

### Build Tasks

**Build:** `./build.sh tasks`

### Runtime Tests

Subdirectories under `src/tests/` may contain `README.md` files with
area-specific guidance (e.g., EventPipe test patterns).

**Build all tests:**
```bash
# Linux / macOS
./build.sh clr+libs -lc release -rc checked
./src/tests/build.sh checked
./src/tests/run.sh checked
```
```cmd
:: Windows
.\build.cmd clr+libs -lc release -rc checked
src\tests\build.cmd checked
src\tests\run.cmd checked
```

**Build a single test project** (path is relative to the repo root):
```bash
# Linux / macOS — use -priority1 for tests with <CLRTestPriority>1</CLRTestPriority>,
# otherwise the build silently reports "0 test projects" and builds nothing.
src/tests/build.sh -Test tracing/eventpipe/eventsvalidation/GCEvents.csproj x64 Release -priority1
```
```cmd
:: Windows — use -Priority 1 (the flag takes a numeric argument on Windows)
src\tests\build.cmd -Test tracing\eventpipe\eventsvalidation\GCEvents.csproj x64 Release -Priority 1
```

Other useful flags (run `src/tests/build.sh -h` or `src\tests\build.cmd /?` for the full list):

| Flag | Linux / macOS (`build.sh`) | Windows (`build.cmd`) |
|------|----------------------------|-----------------------|
| Build one project | `-Test <path>` | `-Test <path>` |
| Build all projects in a directory | `-Dir <path>` | `-Dir <path>` |
| Build a subtree recursively | `-Tree <path>` | `-Tree <path>` |
| Include priority 1 tests | `-priority1` | `-Priority 1` |
| Generate Core_Root layout only | `-GenerateLayoutOnly` | `-GenerateLayoutOnly` |

**Generate Core_Root layout** (required before running individual tests):
```bash
# Linux / macOS
src/tests/build.sh -GenerateLayoutOnly x64 Release
```
```cmd
:: Windows
src\tests\build.cmd -GenerateLayoutOnly x64 Release
```

**Run a single test:**
```bash
export CORE_ROOT=$(pwd)/artifacts/tests/coreclr/<os>.x64.Release/Tests/Core_Root
cd artifacts/tests/coreclr/<os>.x64.Release/<test-path>/
$CORE_ROOT/corerun <TestName>.dll
# Exit code 100 = pass, any other value = fail.
```

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
| "testhost" missing / FileNotFoundException | Run baseline build first (Step 2 above) |
| Build timeout | Wait up to 40 min; only fail if no output for 5 min |
| "Target does not exist" | Avoid specifying a target framework; the build will auto-select `$(NetCoreAppCurrent)` |
| "0 test projects" after `build.sh -Test` | The test has `<CLRTestPriority>` > 0; add `-priority1` (`build.sh`) or `-Priority 1` (`build.cmd`) to the build command |

**When reporting failures:** Include logs from `artifacts/log/` and console output for diagnostics.

---

## Reference

- [Build Libraries](/docs/workflow/building/libraries/README.md) · [Test Libraries](/docs/workflow/testing/libraries/testing.md)
- [Build CoreCLR](/docs/workflow/building/coreclr/README.md) · [Test CoreCLR](/docs/workflow/testing/coreclr/testing.md)
- [Build Mono](/docs/workflow/building/mono/README.md) · [Test Mono](/docs/workflow/testing/mono/testing.md)
- [WASM Build](/docs/workflow/building/libraries/webassembly-instructions.md) · [WASM Test](/docs/workflow/testing/libraries/testing-wasm.md)
- [Host Tests](/docs/workflow/testing/host/testing.md)
