Important: **Ensure the code compiles and the tests pass.** Use the "Building & Testing in dotnet/runtime" instructions below.

Additionally,

- Follow all code-formatting and naming conventions defined in `./.editorconfig`.
- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- When writing tests, do not emit "Act", "Arrange" or "Assert" comments.

---

# Building & Testing in dotnet/runtime

## 1. Prerequisites

These steps need to be done **before** applying any changes.

### 1.1. Determine Affected Components

Identify which components will be impacted by the changes. If in doubt, analyze the paths of the files to be updated:

- **CoreCLR (CLR):** Changes in `src/coreclr/` or `src/tests/`
- **Mono Runtime:** Changes in `src/mono/`
- **Libraries:** Changes in `src/libraries/`
- **WASM/WASI Libraries:** Changes in `src/libraries/` *and* the affected library targets WASM or WASI *and* the changes are included for the target (see below for details).
- If none above apply, it is most possibly an infra-only or a docs-only change. Skip build and test steps.

**WASM/WASI Library Change Detection**

A change is considered WASM/WASI-relevant if:

- The relevant `.csproj` contains explicit Browser/WASM or WASI targets (look for `<TargetFrameworks>`, `$(TargetPlatformIdentifier)`, or `Condition` attributes referencing `browser` or `wasi`, as well as `TARGET_BROWSER` or `TARGET_WASI` constants), **and**
- The changed file is not excluded from the build for that platform in any way with a `Condition` attribute on `<ItemGroup>` or `<Compile>`.

---

### 1.2. Baseline Setup

Ensure you have a full successful build of the needed runtime+libraries as a baseline.

1. Checkout `main` branch

2. From the repository root, run the build depending on the affected component. If multiple components are affected, subsequently run and verify the builds for all of them.
    - **CoreCLR (CLR):** `./build.sh clr+libs+host`
    - **Mono Runtime:** `./build.sh mono+libs`
    - **Libraries:** `./build.sh clr+libs -rc release`
    - **WASM/WASI Libraries:** `./build.sh mono+libs -os browser`

3. Verify the build completed without error.
    - _If the build failed, report the failure and don't proceed with the changes._

4. From the repository root:
    - Configure PATH: `export PATH="$(pwd)/.dotnet:$PATH"`
    - Verify SDK Version: `dotnet --version` should match `sdk.version` in `global.json`.

5. Switch back to the working branch.

---

## 2. Iterative Build and Test Strategy

1. Apply the intended changes

2. **Attempt Build.** If the build fails, attempt to fix and retry the step (up to 5 attempts).

3. **Attempt Test.**
    - If a test _build_ fails, attempt to fix and retry the step (up to 5 attempts).
    - If a test _run_ fails,
        - Determine if the problem is in the test or in the source
        - If the problem is in the test, attempt to fix and retry the step (up to 5 attempts).
        - If the problem is in the source, reconsider the full changeset, attempt to fix and repeat the workflow.

4. **Workflow Iteration:**
    - Repeat build and test up to 5 cycles.
    - If issues persist after 5 workflow cycles, report failure.
    - If the same error persists after each fix attempt, do not repeat the same fix. Instead, escalate or report with full logs.

When retrying, attempt different fixes and adjust based on the build/test results.

### 2.1. Success Criteria

- **Build:**
    - Completes without errors.
    - Any non-zero exit code from build commands is considered a failure.

- **Tests:**
    - All tests must pass (zero failures).
    - Any non-zero exit code from test commands is considered a failure.

- **Workflow:**
    - On success: Report completion
    - Otherwise: Report error(s) with logs for diagnostics.
        - Collect logs from `artifacts/log/` and the console output for both build and test steps.
        - Attach relevant log files or error snippets when reporting failures.

---

## 3. CoreCLR (CLR) Workflow

From the repository root:

- Build:
  `./build.sh -subset clr`

- Run tests:
  `cd src/tests && ./build.sh && ./run.sh`

- More info:
    - [Building CoreCLR Guide](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/coreclr/README.md)
    - [Building and Running CoreCLR Tests](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/coreclr/testing.md)

---

## 4. Mono Runtime Workflow

From the repository root:

- Build:
  `./build.sh -subset mono+libs`

- Run tests:

  ```bash
  ./build.sh clr.host
  cd src/tests
  ./build.sh mono debug /p:LibrariesConfiguration=debug
  ./run.sh
  ```

- More info:
    - [Building Mono](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/mono/README.md)
    - [Running test suites using Mono](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/mono/testing.md)

---

## 5. Libraries Workflow

From the repository root:

- Build:
  `./build.sh -subset libs -rc release`

- Run tests:
  `./build.sh -subset libs.tests -test -rc release`

- More info:
    - [Build Libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/README.md)
    - [Testing Libraries](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing.md)

### 5.1. How To: Identify Affected Libraries

For each changed file under `src/libraries/`, find the matching library and its test project(s).
Most libraries use:

- Source: `src/libraries/<LibraryName>/src/<LibraryName>.csproj`

- Tests (single):
    - `src/libraries/<LibraryName>/tests/<LibraryName>.Tests.csproj`
    - OR `src/libraries/<LibraryName>/tests/<LibraryName>.Tests/<LibraryName>.Tests.csproj`

- Tests (multiple types):
    - `src/libraries/<LibraryName>/tests/FunctionalTests/<LibraryName>.Functional.Tests.csproj`
    - `src/libraries/<LibraryName>/tests/UnitTests/<LibraryName>.Unit.Tests.csproj`
    - Or similar.

---

### 5.2. How To: Build and Test Specific Library

If only one library is affected:

1. **Navigate to the library directory:**
   `cd src/libraries/<LibraryName>`

2. **Build the library:**
   `dotnet build`

3. **Build and run all test projects:**

    - For each discovered `*.Tests.csproj` in the `tests` subdirectory:
      `dotnet build /t:test ./tests/<TestProject>.csproj`

        - *Adjust path as needed. If in doubt, search with `find tests -name '*.csproj'`.*

    - `dotnet build /t:test` is generally preferred over `dotnet test`

---

## 6. WebAssembly (WASM) Libraries Workflow

From the repository root:

- Build:
  `./build.sh libs -os browser`

- Run tests:
  `./build.sh libs.tests -test -os browser`

- More info:
    - [Build libraries for WebAssembly](https://github.com/dotnet/runtime/blob/main/docs/workflow/building/libraries/webassembly-instructions.md)
    - [Testing Libraries on WebAssembly](https://github.com/dotnet/runtime/blob/main/docs/workflow/testing/libraries/testing-wasm.md)

---

## 7. Additional Notes

### 7.1. Troubleshooting

- **Shared Framework Missing**

    - If the build fails with an error "The shared framework must be built before the local targeting pack can be consumed.", build both the runtime (clr or mono) and the libs.
      E.g., from the repo root, run `./build.sh clr+libs -rc release` if working on Libraries on CoreCLR. Refer to the section `1.2. Baseline Setup`.

- **Testhost Is Missing**

    - If a test run fails with errors indicating a missing testhost, such as:
        - "Failed to launch testhost with error: System.IO.FileNotFoundException", or
        - "artifacts/bin/testhost/... No such file or directory",
      that means some of the prerequisites were not built.

    - To resolve, build both the appropriate runtime (clr or mono) and the libs as a single command before running tests.
      E.g., from the repo root, run `./build.sh clr+libs -rc release` before testing Libraries on CoreCLR. Refer to the section `1.2. Baseline Setup`.

- **Build Timeout**

    - Do not fail or cancel initial `./build.sh` builds due to timeout unless at least 40 minutes have elapsed.
      A full `clr+libs` build from scratch can take up to 32 minutes or more on some systems.

    - Only wait for long-running `./build.sh` commands if they continue to produce output.
      If there is no output for 5 minutes, assume the build is stuck and fail early.

- **Target Does Not Exist**

    - Avoid specifying a target framework when building unless explicitly asked.
      Build should identify and select the appropriate `$(NetCoreAppCurrent)` automatically.

---

### 7.2. Windows Command Equivalents

- Use `build.cmd` instead of `build.sh` on Windows.
- Set PATH: `set PATH=%CD%\.dotnet;%PATH%`
- All other commands are similar unless otherwise noted.
