# Browser/WASM CoreCLR Library Tests Plan

## Overview

This document outlines the process for running .NET library tests on the Browser/WASM target with the **CoreCLR virtual machine** (interpreter mode, no JIT).

## Target Platform Characteristics

| Characteristic | Value |
|----------------|-------|
| OS | Browser (WebAssembly) |
| VM | CoreCLR (interpreter only, no JIT) |
| Threading | **Not supported** - no thread creation, no blocking waits |
| Known Issues | C# finalizers don't work, GC memory corruption bugs |
| Test Runner | Xharness (local web server + Chrome browser) |

## Reference Baseline

The same tests already pass on **Mono + Browser**. Results are in:
- [Mono-chrome-workitems.json](Mono-chrome-workitems.json)

Each work item has a `DetailsUrl` that links to Helix logs with `ConsoleOutputUri` showing test summaries.

## Goals

1. Run all library test suites on Browser/WASM + CoreCLR
2. Compare results with Mono baseline
3. Mark failing tests with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
4. Document each failure with full test name and stack trace in `/browser-tests/failures/`

## Environment Setup

```bash
# Set environment variables (required for all build and test commands)
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"
export XunitShowProgress="true"
```

## Build Commands

### Initial Build (one-time)

```bash
./build.sh -os browser -subset clr+libs+host -c Debug
```

**Note:** This build can take 30-40+ minutes.

### Rebuild After Changes

```bash
# Rebuild just libraries after test attribute changes
./build.sh libs -os browser -c Debug
```

## Running Tests

### Single Test Suite

```bash
# Linux/macOS
./dotnet.sh build -bl \
    /p:TargetOS=browser \
    /p:TargetArchitecture=wasm \
    /p:Configuration=Debug \
    /t:Test \
    src/libraries/<LibraryName>/tests/<TestProject>.csproj
```

### Example: System.Runtime.InteropServices.JavaScript.Tests

```bash
./dotnet.sh build -bl \
    /p:TargetOS=browser \
    /p:TargetArchitecture=wasm \
    /p:Configuration=Debug \
    /t:Test \
    src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj
```

**Results location for this test:**
- XML: `artifacts/bin/System.Runtime.InteropServices.JavaScript.Tests/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/testResults.xml`
- Log: `artifacts/bin/System.Runtime.InteropServices.JavaScript.Tests/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/wasm-console.log`

## Test Result Locations

| Artifact | Location |
|----------|----------|
| Build logs | `artifacts/log/Debug/` |
| Test results XML | `artifacts/bin/<TestProject>/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/testResults.xml` |
| Console log | `artifacts/bin/<TestProject>/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/wasm-console.log` |

**Timeout Configuration:** `WasmXHarnessTestsTimeout` in `eng/testing/tests.wasm.targets` (default: 00:30:00)

## Processing Test Failures

### For Each Failing Test:

1. **Identify the failure** from `testResults.xml` or `wasm-console.log`
2. **Extract full information:**
   - Full test name (namespace.class.method)
   - Full stack trace
   - Failure reason/exception type
3. **Create failure record** in `/browser-tests/failures/<TestSuiteName>/<TestName>.md`
4. **Mark test** with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
5. **Rebuild and re-run** the test suite to continue

### Handling Timeouts/Crashes/Aborts

If the test suite hangs, times out, VM crashes, or exits with non-zero code:

1. **Find the last running test** in `wasm-console.log`:
   ```
   [STRT] System.Runtime.InteropServices.JavaScript.Tests.JSImportTest.JsImportSleep
   ```
2. **Mark that test** with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
3. **Re-run the suite** to discover remaining failures
4. **Repeat** until the suite completes (pass or fail, but not hang/crash)

### Failure Documentation Template

```markdown
# Test: <FullTestName>

## Test Suite
<TestSuiteName>

## Failure Type
<crash|timeout|exception|assertion>

## Exception Type
<ExceptionType>

## Stack Trace
```
<full stack trace>
```

## Notes
- Platform: Browser/WASM + CoreCLR
- Category: [threading|gc|finalizer|interpreter|other]
```

## Test Marking Guidelines

### Existing Skip Attributes to Be Aware Of

```csharp
// Skips on Mono - may need review for CoreCLR+Browser
[SkipOnMono("reason")]

// Skips on Browser platform entirely
[SkipOnPlatform(TestPlatforms.Browser, "reason")]

// Our new marker for CoreCLR+Browser specific issues
[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]
```

### When to Use Each

| Scenario | Attribute |
|----------|-----------|
| Test fails only on Browser+CoreCLR | `[ActiveIssue("...123011")]` |
| Test fails on all Browser (Mono+CoreCLR) | `[SkipOnPlatform(TestPlatforms.Browser)]` |
| Test timeout/hang | `[ActiveIssue("...123011")]` with note |

## Automation Scripts

### run-browser-test.sh

Script to run a single test suite and collect results.

**Usage:**
```bash
./browser-tests/run-browser-test.sh <path-to-test-csproj>
```

**Features:**
- Sets required environment variables
- Runs the test with proper parameters
- Captures console output to log file
- Copies XML results to `/browser-tests/results/`

## Progress Tracking

### Test Suites to Process

Starting with: `System.Runtime.InteropServices.JavaScript.Tests`

Full list from Mono baseline: See [Mono-chrome-workitems.json](Mono-chrome-workitems.json)

### Status Legend

- ⬜ Not started
- 🔄 In progress
- ✅ All tests passing
- ⚠️ Tests marked with ActiveIssue
- ❌ Blocked

---

## Decisions Made

| Question | Decision |
|----------|----------|
| GitHub Issue | Use single umbrella issue **#123011** for all Browser+CoreCLR failures |
| Build Configuration | **Debug** - for better stack traces |
| Failure Categories | Decide when all failures collected (threading, gc, finalizer, interpreter, other) |
| Automation | Keep simple, improve as we go |
| Timeouts | Keep current defaults (`WasmXHarnessTestsTimeout` = 00:30:00) |

---

## Current Session

### Step 1: Build the runtime

```bash
export RuntimeFlavor="CoreCLR"
export Scenario="WasmTestOnChrome"
export InstallFirefoxForTests="false"

./build.sh -os browser -subset clr+libs+host -c Debug
```

### Step 2: Run first test suite

```bash
./dotnet.sh build -bl \
    /p:TargetOS=browser \
    /p:TargetArchitecture=wasm \
    /p:Configuration=Debug \
    /t:Test \
    src/libraries/System.Runtime.InteropServices.JavaScript/tests/System.Runtime.InteropServices.JavaScript.UnitTests/System.Runtime.InteropServices.JavaScript.Tests.csproj
```

### Step 3: Analyze results

Check:
- `artifacts/bin/System.Runtime.InteropServices.JavaScript.Tests/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/testResults.xml`
- `artifacts/bin/System.Runtime.InteropServices.JavaScript.Tests/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/wasm-console.log`
