**Any code you commit MUST compile, and new and existing tests related to the change MUST pass.**

You MUST make your best effort to ensure any code changes satisfy those criteria before committing. Build and run the relevant tests after your last edit — do not assume a change fixed a failure you saw, actually run them again to confirm. If for any reason you were unable to build or test code changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

Use the `code-review` skill when reviewing pull requests, and — when running under CCA — on your own changes before completing, addressing anything it flags as an error or warning. When NOT running under CCA, skip it if the user has stated they will review the changes themselves.

Before making changes to a directory, search for `README.md` files in that directory and its parent directories up to the repository root. Read any you find — they contain conventions, patterns, and architectural context relevant to your work.

If the changes are intended to improve performance, or if they could negatively impact performance, use the `performance-benchmark` skill to validate the impact before completing.

When writing or reviewing SIMD / hardware-intrinsics code (anything using `Vector128`/`Vector256`/`Vector512`, `Vector<T>`, or the platform intrinsics in `System.Runtime.Intrinsics.*`), use the `vectorization` skill.

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](/.editorconfig).

In addition to the rules enforced by `.editorconfig`, when writing C# you SHOULD:

- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- If you add new code files, ensure they are listed in the csproj file (if other files in that folder are listed there) so they build.
- When adding XML documentation to APIs, follow the guidelines at [`docs.prompt.md`](/.github/prompts/docs.prompt.md).

When writing or modifying tests, you SHOULD:

- Strongly prefer to add new unit tests to existing test code files rather than creating new code files.
- When adding new test files, examine the directory structure of sibling tests first. Some test directories use flat files (e.g., `GCEvents.cs` alongside `GCEvents.csproj`) while others use per-test subdirectories. Match the existing convention.
- Avoid adding a regression comment citing a GitHub issue or PR number unless explicitly asked to include such information.
- Prefer using `[Theory]` with multiple data sources (like `[InlineData]` or `[MemberData]`) over multiple duplicative `[Fact]` methods. Fewer test methods that validate more inputs are better than many similar test methods.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
- Do not finish work with any tests commented out or disabled that were not previously commented out or disabled.
- Do not emit "Act", "Arrange" or "Assert" comments.

For markdown (`.md`) files, ensure there is no trailing whitespace at the end of any line.

When NOT running under CCA, guidance for creating commits and pushing changes:

- Never squash and force push unless explicitly instructed. Always push incremental commits on top of previous PR changes.
- Never push to an active PR without being explicitly asked, even in autopilot/yolo mode. Always wait for explicit instruction to push.
- Never chain commit and push in the same command. Always commit first, report what was committed, then wait for an explicit push instruction. This creates a mandatory decision point.
- Prefer creating a new commit rather than amending an existing one. Exceptions: (1) explicitly asked to amend, or (2) the existing commit is obviously broken with something minor (e.g., typo or comment fix) and hasn't been pushed yet.
- **Before posting to GitHub (PRs, issues, comments):** Include the AI-generated content disclosure (see below).

## AI-Generated Content Disclosure

When posting to GitHub under a user's credentials — PR descriptions, issue bodies, comments, review comments, or any other public-facing action — you **MUST** add a concise, visible note (e.g. a `> [!NOTE]` alert) at the bottom of the content indicating it was AI/Copilot-generated. Skip it only when posting from a recognized bot or Copilot app account (e.g. `github-actions[bot]`, `copilot`), where the AI origin is already apparent from the account identity, or when the user explicitly asks you to omit it.

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
| `$CORE_ROOT/corerun <Test>.dll` | `$env:CORE_ROOT\corerun.exe <Test>.dll` |
| `find <dir> -name '<pattern>'` | `Get-ChildItem <dir> -Recurse -Filter '<pattern>'` |

Some arguments also differ in casing: `-priority1` on bash is `-Priority 1` on Windows.

## Baseline Build

A successful baseline build of the affected component is required for incremental builds and tests. Without it you'll hit "missing testhost" and "shared framework" errors that cost 20+ minutes per occurrence. If a baseline build fails, STOP, report the failure, and do not attempt to work around it.

### When a baseline is required

- **Under CCA — always, before making any code changes.** ⚠️ The environment is fresh, so there are no pre-existing artifacts and incremental builds fail in ways that waste significant compute. Skipping this step IS a task failure. **Exception:** if you are on a feature branch with commits upstream of main and the baseline build fails, make whatever code changes are needed to fix the build, then resume requiring a baseline.
- **Under CLI (interactive) — only when needed.** A usable baseline may already exist from prior work; don't re-run a 40-minute build unnecessarily. Check the component's **Baseline sentinel** path under `artifacts/` (listed per component below) and build if it is missing. Otherwise attempt the work, and if it fails with a baseline-missing signature from [Troubleshooting](#troubleshooting), run the baseline once and retry — do not loop. Trust volunteered user signals ("just built", "fresh checkout") over probing.
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
| `src/tasks` | Build Tasks | `./build.sh clr+libs -rc release` |
| `src/tests` | Runtime Tests | `./build.sh clr+libs -lc release -rc checked` |

**WASM/WASI Library Detection:** A change under `src/libraries/` is WASM/WASI-relevant if the library's `.csproj` has explicit Browser/WASM or WASI targets (`TargetFrameworks`, `TARGET_BROWSER`, `TARGET_WASI` constants, or `Condition` attributes referencing `browser`/`wasi`), **and** the changed file is not excluded from those targets via `Condition` on `<ItemGroup>` or `<Compile>`.

For System.Private.CoreLib changes, use `-rc checked` instead of `-rc release` for asserts.

Build on the branch you intend to modify — the baseline reflects your working tree at that moment. Baselining up front requires a clean HEAD; baselining after a probe failure means either stashing work-in-progress changes first or accepting that the baseline incorporates them.

⏱️ **This build can take up to 40 minutes.** Do not cancel unless no output for 5+ minutes.

### Step 2: Configure Environment

```bash
export PATH="$(pwd)/.dotnet:$PATH"
dotnet --version  # Should match sdk.version in global.json
```

On Windows: `$env:PATH = "$PWD\.dotnet;$env:PATH"`

---

## Component-Specific Workflows

These workflows assume a usable baseline build exists for the component (either freshly produced per the section above, or already present in the user's workspace under CLI use). Each workflow lists a **Baseline sentinel** — a path under `artifacts/` whose absence indicates the baseline is missing and must be run before proceeding. All commands must complete with exit code 0, and all tests must pass with zero failures.

### Libraries (Most Common)

**Baseline sentinel (for tests):** `artifacts/bin/testhost/` and `artifacts/bin/microsoft.netcore.app.runtime.<RID>/<config>/`. (Building a single library typically works without a baseline; running its tests does not.)

**Build and test a specific library:**
```bash
cd src/libraries/<LibraryName>
dotnet build
dotnet build /t:test ./tests/<TestProject>.csproj
```

Test projects are typically at: `tests/<LibraryName>.Tests.csproj` or `tests/<LibraryName>.Tests/<LibraryName>.Tests.csproj`, or under `tests/FunctionalTests/`, `tests/UnitTests/`, etc. Use `find tests -name '*.Tests.csproj'` to discover them.

**Test all libraries:** `./build.sh libs.tests -test -rc release`

**System.Private.CoreLib:** Rebuild with `./build.sh clr.corelib+clr.nativecorelib+libs.pretest -rc checked`

### CoreCLR

**Baseline sentinel:** `artifacts/bin/coreclr/<OS>.<arch>.<config>/` for incremental runtime builds; `artifacts/tests/coreclr/<OS>.<arch>.<config>/Tests/Core_Root/` for running tests.

**Test:** `cd src/tests && ./build.sh && ./run.sh`

### Mono

**Baseline sentinel:** `artifacts/bin/mono/<OS>.<arch>.<config>/` for incremental runtime builds; `artifacts/tests/coreclr/<OS>.<arch>.<config>/Tests/Core_Root/` for running tests (Mono tests reuse the Core_Root layout).

**Test:**
```bash
./build.sh clr.host
cd src/tests
./build.sh mono debug /p:LibrariesConfiguration=debug
./run.sh
```

### WASM Libraries

**Baseline sentinel:** `artifacts/bin/microsoft.netcore.app.runtime.browser-wasm/<config>/`.

**Build:** `./build.sh libs -os browser`

**Test:** `./build.sh libs.tests -test -os browser`

### Host

**Baseline sentinel:** `artifacts/bin/coreclr/<OS>.<arch>.<config>/` and `artifacts/bin/testhost/` (host build/tests need both clr and libs in place).

**Build:** `./build.sh host -rc release -lc release`

**Test:** `./build.sh host.tests -rc release -lc release -test`

### Tools

**Baseline sentinel:** `artifacts/bin/coreclr/<OS>.<arch>.<config>/` and `artifacts/bin/testhost/`.

**Build:** `./build.sh tools+tools.ilasm`

**Test:** `./build.sh tools+tools.ilasm+tools.illinktests+tools.cdactests -test`

### Build Tasks

**Baseline sentinel:** none required for `./build.sh tasks` — it's self-contained. If you go on to consume the tasks from a workflow that does need a baseline (e.g., libraries tests), apply that workflow's sentinel instead.

**Build:** `./build.sh tasks`

### Runtime Tests

**Baseline sentinel:** `artifacts/tests/coreclr/<OS>.<arch>.<config>/Tests/Core_Root/` (required to run individual tests; produced by the baseline build plus `src/tests/build.sh -GenerateLayoutOnly`).

Subdirectories under `src/tests/` may contain `README.md` files with
area-specific guidance (e.g., EventPipe test patterns).

**Build all tests:**
```bash
./build.sh clr+libs -lc release -rc checked
./src/tests/build.sh checked
./src/tests/run.sh checked
```

**Build a single test project** (path is relative to the repo root):
```bash
# Use -priority1 ("-Priority 1" on Windows) for tests with <CLRTestPriority>1</CLRTestPriority>,
# otherwise the build silently reports "0 test projects" and builds nothing.
src/tests/build.sh -Test tracing/eventpipe/eventsvalidation/GCEvents.csproj x64 Release -priority1
```

Other useful flags (run `src/tests/build.sh -h` for the full list):

| Flag | Description |
|------|-------------|
| `-Test <path>` | Build one project |
| `-Dir <path>` | Build all projects in a directory |
| `-Tree <path>` | Build a subtree recursively |
| `-priority1` (`-Priority 1` on Windows) | Include priority 1 tests |
| `-GenerateLayoutOnly` | Generate Core_Root layout only |

**Generate Core_Root layout** (required before running individual tests):
```bash
src/tests/build.sh -GenerateLayoutOnly x64 Release
```

**Run a single test:**
```bash
export CORE_ROOT=$(pwd)/artifacts/tests/coreclr/<os>.x64.Release/Tests/Core_Root
cd artifacts/tests/coreclr/<os>.x64.Release/<test-path>/
$CORE_ROOT/corerun <TestName>.dll
# Exit code 100 = pass, any other value = fail.
```

On Windows: `$env:CORE_ROOT = "$PWD\artifacts\tests\coreclr\windows.x64.Release\Tests\Core_Root"`, then
run `$env:CORE_ROOT\corerun.exe <TestName>.dll` from the test's output directory.

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

---

## Reference

- [Build Libraries](/docs/workflow/building/libraries/README.md) · [Test Libraries](/docs/workflow/testing/libraries/testing.md)
- [Build CoreCLR](/docs/workflow/building/coreclr/README.md) · [Test CoreCLR](/docs/workflow/testing/coreclr/testing.md)
- [Build Mono](/docs/workflow/building/mono/README.md) · [Test Mono](/docs/workflow/testing/mono/testing.md)
- [WASM Build](/docs/workflow/building/libraries/webassembly-instructions.md) · [WASM Test](/docs/workflow/testing/libraries/testing-wasm.md)
- [Host Tests](/docs/workflow/testing/host/testing.md)
