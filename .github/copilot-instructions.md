# dotnet/runtime — AI Coding Agent Instructions

## Essential Architecture

The .NET runtime repository builds the complete .NET runtime, libraries, and host installers for all supported platforms. Understanding the major components and their interactions is critical for effective development:

- **CoreCLR**: Main .NET runtime implementation (`src/coreclr/`, `src/tests/`)
- **Mono**: Alternative runtime with WASM/mobile support (`src/mono/`)
- **Libraries**: Base Class Library and framework libraries (`src/libraries/`)
- **Host/Installers**: Native host (`dotnet`) and installer components (`src/native/corehost/`, `src/installer/`)

**Cross-component dependencies**: Changes affecting multiple components require building and testing all affected areas. Use component mapping in [section 1.1](#11-determine-affected-components) to identify impact scope.

## Mandatory Verification Requirements

**All code you commit MUST compile successfully, and all new and existing tests related to the change MUST pass.**

You are REQUIRED to ensure your changes satisfy these criteria before completing any task. You MUST NOT complete a task or claim success unless:
1. All relevant builds complete successfully with zero errors
2. All relevant tests pass with zero failures
3. You have verified both build and test success after your final code changes

If you cannot achieve successful builds and passing tests, you MUST continue working to fix the issues. Only report inability to complete if you have exhausted all reasonable approaches and the fundamental requirements cannot be met.

CRITICAL: You MUST verify that builds succeed and tests pass after making your final edits. Never assume your changes work - always verify through actual build and test execution.

Additionally, you MUST provide evidence—show the successful build and test output in your completion message.

FAILURE TO MEET THESE REQUIREMENTS MEANS THE TASK IS INCOMPLETE—regardless of how much code was written or how close you came to a solution.
---

You MUST refer to the [Building & Testing in dotnet/runtime](#building--testing-in-dotnetruntime) instructions and use the commands and approaches specified there before attempting your own suggestions.

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
- If you add new code files, ensure they are listed in the csproj file (if other files in that folder are listed there) so they build.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
- Do not finish work with any tests commented out or disabled that were not previously commented out or disabled.
- When writing tests, do not emit "Act", "Arrange" or "Assert" comments.

## Project-Specific Conventions

**Component Integration**:
- External dependencies are managed via `NuGet.config` and individual project files
- Cross-component communication uses public APIs and shared contracts; avoid direct internal calls across major component boundaries (CoreCLR ↔ Mono ↔ Libraries ↔ Host)
- WASM/WASI builds require special workflows; see [WebAssembly workflow](#6-webassembly-wasm-libraries-workflow)

**Build System Patterns**:
- Always use provided build scripts (`build.cmd`/`build.sh`) from repository root
- Component detection by file path determines required build/test scope
- Cross-platform builds may require different target platforms than build platforms

**Testing Workflows**:
- Establish clean baseline on default branch before making changes
- Test filtering during development is acceptable, but final verification requires ALL relevant tests to pass
- Component-specific test commands vary; see workflow sections below

---

# Building & Testing in dotnet/runtime

- [1. Prerequisites](#1-prerequisites)
    - [1.1. Determine Affected Components](#11-determine-affected-components)
    - [1.2. Baseline Setup](#12-baseline-setup)
- [2. Iterative Build and Test Strategy](#2-iterative-build-and-test-strategy)
    - [2.1. Success Criteria](#21-success-criteria)
- [3. CoreCLR (CLR) Workflow](#3-coreclr-clr-workflow)
- [4. Mono Runtime Workflow](#4-mono-runtime-workflow)
- [5. Libraries Workflow](#5-libraries-workflow)
    - [5.1. How To: Identify Affected Libraries](#51-how-to-identify-affected-libraries)
    - [5.2. How To: Build and Test Specific Library](#52-how-to-build-and-test-specific-library)
- [6. WebAssembly (WASM) Libraries Workflow](#6-webassembly-wasm-libraries-workflow)
- [7. Host Workflow](#7-host-workflow)
- [8. Additional Notes](#8-additional-notes)
    - [8.1. Troubleshooting](#81-troubleshooting)
    - [8.2. Windows Command Equivalents](#82-windows-command-equivalents)
    - [8.3. References](#83-references)

## 1. Prerequisites

These steps need to be done **before** applying any changes.

### 1.1. Determine Affected Components

Identify which components will be impacted by the changes. If in doubt, analyze the paths of the files to be updated:

- **CoreCLR (CLR):** Changes in `src/coreclr/` or `src/tests/`
- **Mono Runtime:** Changes in `src/mono/`
- **Libraries:** Changes in `src/libraries/`
- **WASM/WASI Libraries:** Changes in `src/libraries/` *and* the affected library targets WASM or WASI *and* the changes are included for the target (see below for details).
- **Host:** Changes in `src/native/corehost/`, `src/installer/managed/`, or `src/installer/tests/`
- If none above apply, it is most possibly an infra-only or a docs-only change. Skip build and test steps.

**WASM/WASI Library Change Detection**

A change is considered WASM/WASI-relevant if:

- The relevant `.csproj` contains explicit Browser/WASM or WASI targets (look for `<TargetFrameworks>`, `$(TargetPlatformIdentifier)`, or `Condition` attributes referencing `browser` or `wasi`, as well as `TARGET_BROWSER` or `TARGET_WASI` constants), **and**
- The changed file is not excluded from the build for that platform in any way with a `Condition` attribute on `<ItemGroup>` or `<Compile>`.

---

### 1.2. Baseline Setup

**CRITICAL:** You MUST establish a working baseline before making any changes. This ensures you can distinguish between pre-existing issues and problems caused by your changes.

1. **Checkout `main` branch**

2. **Execute Baseline Builds:** From the repository root, run the build for affected component(s). If multiple components are affected, build and verify ALL of them:
    - **CoreCLR (CLR):** `./build.sh clr+libs+host`
    - **Mono Runtime:** `./build.sh mono+libs`
    - **Libraries:** `./build.sh clr+libs -rc release`
    - **WASM/WASI Libraries:** `./build.sh mono+libs -os browser`
    - **Host:** `./build.sh clr+libs+host -rc release -lc release`

3. **Verify Baseline Success:**
    - Build MUST complete with exit code 0
    - NO build errors are acceptable in baseline
    - If baseline build fails, you MUST report this issue and not proceed with changes

4. **Environment Setup:** From the repository root:
    - Configure PATH: `export PATH="$(pwd)/.dotnet:$PATH"`
    - Verify SDK Version: `dotnet --version` should match `sdk.version` in `global.json`

5. **Execute Baseline Tests:** Run the appropriate tests for your component to verify they pass in main branch:
    - If baseline tests fail, document these failures and ensure your changes don't introduce additional failures

6. **Switch to working branch** only after confirming baseline functionality

**Remember:** A failing baseline means you cannot reliably assess whether your changes introduce problems. Always establish a clean baseline first.

---

## 2. Iterative Build and Test Strategy

**MANDATORY WORKFLOW:** You MUST follow this exact sequence for every change:

1. **Apply Changes:** Make your intended code modifications

2. **Build Verification:**
   - Execute the appropriate build command for your component
   - Build MUST complete with zero errors and zero warnings where possible
   - Any non-zero exit code is a build failure
   - If build fails, you MUST fix the issues and rebuild until successful

3. **Test Verification:**
   - Execute the appropriate test command for your component
   - ALL tests MUST pass (zero failures)
   - Any non-zero exit code from test commands is a test failure
   - If tests fail, you MUST analyze whether the issue is in your code or the test itself
   - Fix issues and re-run tests until all pass

4. **Final Verification:**
   - After your last code edit, you MUST re-run both build and test commands
   - Confirm both succeed before claiming task completion
   - NEVER assume previous success means current success

**Failure Resolution:**
- Maximum 3 attempts to fix build failures before escalating
- Maximum 3 attempts to fix test failures before escalating
- If the same error persists after 2 fix attempts, you MUST try a different approach
- If you cannot achieve passing builds and tests after reasonable effort, explain the blocking issues with full diagnostic information

**No Shortcuts:** You cannot skip verification steps or claim success without demonstrated passing builds and tests.

### 2.1. Success Criteria

**ABSOLUTE REQUIREMENTS for task completion:**

- **Build Success:**
    - ALL builds MUST complete with zero errors
    - Exit code MUST be 0 for all build commands
    - Any build warnings should be addressed when possible
    - Build success MUST be verified after final code changes

- **Test Success:**
    - ALL tests MUST pass (zero failures, zero errors)
    - Exit code MUST be 0 for all test commands
    - Test success MUST be verified after final code changes
    - Test count verification: Ensure expected tests actually ran

- **Completion Requirements:**
    - You MUST demonstrate both build and test success before claiming completion
    - Provide clear evidence of successful build and test execution
    - Any deviation from these requirements renders the task incomplete

- **Failure Reporting:**
    - If unable to meet requirements: Provide detailed error analysis with full logs
    - Include logs from `artifacts/log/` and console output
    - Explain specific blocking issues and attempted solutions
    - Do not claim partial success or "best effort" completion

---

## 3. CoreCLR (CLR) Workflow

From the repository root:

- Build:
  `./build.sh clr`

- Run tests:
  `cd src/tests && ./build.sh && ./run.sh`

- More info can be found in the dedicated workflow docs:
    - [Building CoreCLR Guide](/docs/workflow/building/coreclr/README.md)
    - [Building and Running CoreCLR Tests](/docs/workflow/testing/coreclr/testing.md)

---

## 4. Mono Runtime Workflow

From the repository root:

- Build:
  `./build.sh mono+libs`

- Run tests:

  ```bash
  ./build.sh clr.host
  cd src/tests
  ./build.sh mono debug /p:LibrariesConfiguration=debug
  ./run.sh
  ```

- More info can be found in the dedicated workflow docs:
    - [Building Mono](/docs/workflow/building/mono/README.md)
    - [Running test suites using Mono](/docs/workflow/testing/mono/testing.md)

---

## 5. Libraries Workflow

From the repository root:

- Build all libraries:
  `./build.sh libs -rc release`

- Run all tests for libraries:
  `./build.sh libs.tests -test -rc release`

- Build a specific library:
    - Refer to the section [5.2. How To: Build and Test Specific Library](#52-how-to-build-and-test-specific-library) below.

- Test a specific library:
    - Refer to the sections [5.1. How To: Identify Affected Libraries](#51-how-to-identify-affected-libraries) and [5.2. How To: Build and Test Specific Library](#52-how-to-build-and-test-specific-library) below.

- More info can be found in the dedicated workflow docs:
    - [Build Libraries](/docs/workflow/building/libraries/README.md)
    - [Testing Libraries](/docs/workflow/testing/libraries/testing.md)

When working on changes limited to a specific library, you MUST complete ALL the following before claiming task completion:

1. **Build the affected library successfully** - zero build errors required
2. **Run ALL tests for that library** - zero test failures required
3. **Verify success after final edits** - re-run tests to confirm they still pass

For example, if you are working within "System.Text.RegularExpressions" then you MUST ensure after your final edits that ALL tests under `src\libraries\System.Text.RegularExpressions\tests` pass.

**No exceptions:** You may filter to specific tests during development, but final verification requires ALL library tests to pass.

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

- More info can be found in the dedicated workflow docs:
    - [Build libraries for WebAssembly](/docs/workflow/building/libraries/webassembly-instructions.md)
    - [Testing Libraries on WebAssembly](/docs/workflow/testing/libraries/testing-wasm.md)

---

## 7. Host Workflow

From the repository root:

- Build:
  `./build.sh host -rc release -lc release`

- Run all tests:
  `./build.sh host.tests -rc release -lc release -test`

- More info can be found in the dedicated workflow docs:
    - [Building and running host tests](/docs/workflow/testing/host/testing.md)

---

## 8. Additional Notes

### 8.1. Troubleshooting

- **Shared Framework Missing**

    - If the build fails with an error "The shared framework must be built before the local targeting pack can be consumed.", build both the runtime (clr or mono) and the libs.
      E.g., from the repo root, run `./build.sh clr+libs -rc release` if working on Libraries on CoreCLR. To find the applicable command, refer to the section [1.2. Baseline Setup](#12-baseline-setup).

- **Testhost Is Missing**

    - If a test run fails with errors indicating a missing testhost, such as:
        - "Failed to launch testhost with error: System.IO.FileNotFoundException", or
        - "artifacts/bin/testhost/... No such file or directory",
      that means some of the prerequisites were not built.

    - To resolve, build both the appropriate runtime (clr or mono) and the libs as a single command before running tests.
      E.g., from the repo root, run `./build.sh clr+libs -rc release` before testing Libraries on CoreCLR. To find the applicable command, refer to the section [1.2. Baseline Setup](#12-baseline-setup).

- **Build Timeout**

    - Do not fail or cancel initial `./build.sh` builds due to timeout unless at least 40 minutes have elapsed.
      A full `clr+libs` build from scratch can take up to 32 minutes or more on some systems.

    - Only wait for long-running `./build.sh` commands if they continue to produce output.
      If there is no output for 5 minutes, assume the build is stuck and fail early.

- **Target Does Not Exist**

    - Avoid specifying a target framework when building unless explicitly asked.
      Build should identify and select the appropriate `$(NetCoreAppCurrent)` automatically.

---

### 8.2. Windows Command Equivalents

- Use `build.cmd` instead of `build.sh` on Windows.
- Set PATH: `set PATH=%CD%\.dotnet;%PATH%`
- All other commands are similar unless otherwise noted.

---

### 8.3. References

- [`.editorconfig`](/.editorconfig)
- [Building CoreCLR Guide](/docs/workflow/building/coreclr/README.md)
- [Building and Running CoreCLR Tests](/docs/workflow/testing/coreclr/testing.md)
- [Building Mono](/docs/workflow/building/mono/README.md)
- [Running test suites using Mono](/docs/workflow/testing/mono/testing.md)
- [Build Libraries](/docs/workflow/building/libraries/README.md)
- [Testing Libraries](/docs/workflow/testing/libraries/testing.md)
- [Build libraries for WebAssembly](/docs/workflow/building/libraries/webassembly-instructions.md)
- [Testing Libraries on WebAssembly](/docs/workflow/testing/libraries/testing-wasm.md)
- [Building and running host tests](/docs/workflow/testing/host/testing.md)
