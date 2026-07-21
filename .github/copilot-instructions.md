When reviewing pull requests, use the `code-review` skill unless the user has stated they will review the changes themselves.

**Any code you commit MUST compile, and new and existing tests related to the change MUST pass.**

You MUST make your best effort to ensure any code changes satisfy those criteria before committing. If for any reason you were unable to build or test code changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

If you make code changes, do not complete without checking the relevant code builds and relevant tests still pass after the last edits you make. Do not simply assume that your changes fix test failures you see, actually build and run those tests again to confirm.

When running under CCA and before completing, use the `code-review` skill to review your code changes. Any issues flagged as errors or warnings should be addressed before the task is considered complete.

When NOT running under CCA, skip the `code-review` skill if the user has stated they will review the changes themselves.

Before making changes to a directory, search for `README.md` files in that directory and its parent directories up to the repository root. Read any you find — they contain conventions, patterns, and architectural context relevant to your work.

If the changes are intended to improve performance, or if they could negatively impact performance, use the `performance-benchmark` skill to validate the impact before completing.

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
- When writing tests, prefer using `[Theory]` with multiple data sources (like `[InlineData]` or `[MemberData]`) over multiple duplicative `[Fact]` methods. Fewer test methods that validate more inputs are better than many similar test methods.
- If you add new code files, ensure they are listed in the csproj file (if other files in that folder are listed there) so they build.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
- Do not finish work with any tests commented out or disabled that were not previously commented out or disabled.
- When writing tests, do not emit "Act", "Arrange" or "Assert" comments.
- For markdown (`.md`) files, ensure there is no trailing whitespace at the end of any line.
- When adding XML documentation to APIs, follow the guidelines at [`docs.prompt.md`](/.github/prompts/docs.prompt.md).

When NOT running under CCA, guidance for creating commits and pushing changes:

- Never squash and force push unless explicitly instructed. Always push incremental commits on top of previous PR changes.
- Never push to an active PR without being explicitly asked, even in autopilot/yolo mode. Always wait for explicit instruction to push.
- Never chain commit and push in the same command. Always commit first, report what was committed, then wait for an explicit push instruction. This creates a mandatory decision point.
- Prefer creating a new commit rather than amending an existing one. Exceptions: (1) explicitly asked to amend, or (2) the existing commit is obviously broken with something minor (e.g., typo or comment fix) and hasn't been pushed yet.
- **Before posting to GitHub (PRs, issues, comments):** Include the AI-generated content disclosure (see below).

## AI-Generated Content Disclosure

When posting any content to GitHub under a user's credentials — opening PRs, creating issues, commenting on PRs or issues, posting review comments, or any other public-facing action — and the account is **not** a dedicated "copilot" or "bot" account/app (e.g., `github-actions[bot]`, `copilot`), you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) at the bottom of the content indicating the content was AI/Copilot-generated.

This applies to all GitHub interactions: PR descriptions, issue bodies, comments, review comments, etc. Exceptions:
- The account is a recognized bot or Copilot app account (e.g., `github-actions[bot]`, `copilot`), where the AI origin is already apparent from the account identity.
- The user explicitly asks you to omit the disclosure.

---

# Building & Testing in dotnet/runtime

## Baseline Build

A successful baseline build of the affected component is required for incremental builds and tests. Without it you'll hit "missing testhost" and "shared framework" errors that cost 20+ minutes per occurrence.

### When running under CCA — MANDATORY

⚠️ **You MUST complete the baseline build BEFORE making any code changes.** No exceptions. The CCA environment is fresh; there are no pre-existing artifacts, and incremental builds will fail in ways that waste significant compute. Skipping this step IS a task failure — do not proceed with code changes until the baseline build succeeds.

If the baseline build fails, STOP, report the failure, and do not attempt to work around it.

### When running under CLI (interactive) — flexible

A baseline may already exist in the user's workspace from prior work. Don't re-run a 40-minute baseline if the existing one is usable. Apply this rule (works for human users and for local agents driving the CLI):

1. **Check the sentinel artifact** for the work you're about to do. Each component-specific workflow below lists a sentinel path under `artifacts/`. If it's missing, run the baseline before proceeding.
2. **Otherwise, attempt the incremental work.** If a build or test step then fails with a baseline-missing signature from [Troubleshooting](#troubleshooting) (e.g., "shared framework must be built", "testhost" missing, `FileNotFoundException` on shared framework paths), run the baseline once and retry. Do not loop.
3. **Honor explicit user signals when offered.** If the user (or a driving agent) volunteered "just built" / "skip baseline", trust it and skip step 1's check. If they said "fresh checkout" / "no baseline", run the baseline up front without probing.

If you're uncertain which mode you're in, follow the CCA rule.

The remaining steps below apply in both modes whenever a baseline build is actually being performed.

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

From the repo root, run the appropriate build command on the branch you intend to modify. The baseline reflects whatever is in your working tree at that moment, so:

- If you're baselining up front (CCA, or CLI with a fresh checkout), ensure HEAD is clean — no uncommitted changes.
- If you're baselining after a probe failure and already have work-in-progress changes, either stash them first or accept that the baseline incorporates those changes.

| Component | Command |
|-----------|---------|
| **CoreCLR** | `./build.sh clr+libs+host` |
| **Mono** | `./build.sh mono+libs` |
| **Libraries** | `./build.sh clr+libs -rc release` |
| **WASM Libraries** | `./build.sh mono+libs -os browser` |
| **Host** | `./build.sh clr+libs+host -rc release -lc release` |
| **Tools** | `./build.sh clr+libs -rc release` |
| **Build Tasks** | `./build.sh clr+libs -rc release` |
| **Runtime Tests** | `./build.sh clr+libs -lc release -rc checked` |

For System.Private.CoreLib changes, use `-rc checked` instead of `-rc release` for asserts.

⏱️ **This build can take up to 40 minutes.** Do not cancel unless no output for 5+ minutes.

### Step 3: Configure Environment

```bash
export PATH="$(pwd)/.dotnet:$PATH"
dotnet --version  # Should match sdk.version in global.json
```

**If the baseline build fails, report the failure and stop** before proceeding with changes that depend on it.

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

Before completing, ensure ALL tests for affected libraries pass.

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
| "0 test projects" after `build.sh -Test` | The test has `<CLRTestPriority>` > 0; add `-priority1` to the build command |

**When reporting failures:** Include logs from `artifacts/log/` and console output for diagnostics.

**Windows:** Use `build.cmd` instead of `build.sh`.

---

## Reference

- [Build Libraries](/docs/workflow/building/libraries/README.md) · [Test Libraries](/docs/workflow/testing/libraries/testing.md)
- [Build CoreCLR](/docs/workflow/building/coreclr/README.md) · [Test CoreCLR](/docs/workflow/testing/coreclr/testing.md)
- [Build Mono](/docs/workflow/building/mono/README.md) · [Test Mono](/docs/workflow/testing/mono/testing.md)
- [WASM Build](/docs/workflow/building/libraries/webassembly-instructions.md) · [WASM Test](/docs/workflow/testing/libraries/testing-wasm.md)
- [Host Tests](/docs/workflow/testing/host/testing.md)
