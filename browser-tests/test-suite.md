# Test Suite Execution Guide

This document describes the process for running individual Browser/WASM CoreCLR library test suites.

**Prerequisites:** Complete the [before-testing.md](before-testing.md) setup first.


## Downloading Mono Baseline

Each test suite has a corresponding Mono baseline from Helix. Download it before running tests:

```bash
./browser-tests/download-mono-baseline.sh <TestProjectName>
```

Example:
```bash
./browser-tests/download-mono-baseline.sh System.Runtime.InteropServices.JavaScript.Tests
```

This downloads to `browser-tests/results/<TestProject>/mono-console.log` and displays the Mono test summary.

## Running Tests

### Using the Script

```bash
./browser-tests/run-browser-test.sh <path-to-test-csproj>
./browser-tests/run-browser-test.sh -c Release <path-to-test-csproj>
```

### Manual Command

```bash
./dotnet.sh build -bl \
    /p:TargetOS=browser \
    /p:TargetArchitecture=wasm \
    /p:Configuration=Debug \
    /t:Test \
    src/libraries/<LibraryName>/tests/<TestProject>.csproj
```

## Test Result Locations

| Artifact | Location |
|----------|----------|
| Build logs | `artifacts/log/Debug/` |
| Test results XML | `artifacts/bin/<TestProject>/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/testResults.xml` |
| Console log | `artifacts/bin/<TestProject>/Debug/net11.0-browser/browser-wasm/wwwroot/xharness-output/wasm-console.log` |
| Collected results | `browser-tests/results/<TestProject>/` |

**Timeout Configuration:** `WasmXHarnessTestsTimeout` in `eng/testing/tests.wasm.targets` (default: 00:30:00)

## Processing Test Failures

### For Each Failing Test:

1. **Identify the failure** from `testResults.xml` or `wasm-console.log`
2. **Extract full information:**
   - Full test name (namespace.class.method)
   - Full stack trace
   - Failure reason/exception type
3. **Create failure record** for each individual test/method/Fact/Theory in `/browser-tests/failures/<TestSuiteName>/<ClassName.MethodName>.md` (e.g., `JSImportTest.JsImportSleep.md`)
4. **Mark test** with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
5. **Compare test counts**: `Tests run: X Passed: Y Failed: Z Skipped: N` with the Mono baseline at: `browser-tests/results/<TestProject>/mono-console.log`
6. **Compare test sets** with the Mono baseline at: `browser-tests/results/<TestProject>/mono-testResults.xml`. Which tests are missing and which are extra ?
7. **Create or update** `browser-tests/results/<TestProject>/Summary.md` with the outcome
8. **Stop and ask for feedback before proceeding.**
9. **Rebuild and re-run** the test suite to continue until all enabled tests pass.

### Handling Timeouts/Crashes/Aborts

If the test suite hangs, times out, VM crashes, or exits with non-zero code:

1. **Find the last running test** in `wasm-console.log`:
   ```
   [STRT] System.Runtime.InteropServices.JavaScript.Tests.JSImportTest.JsImportSleep
   ```
2. **Mark that test** with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
3. **Re-run the suite** to discover remaining failures
4. **Repeat** until the suite completes (pass or fail, but not hang/crash)

### Method Failure Documentation Template

```markdown
# Test: <FullTestName>

## Test Suite
<TestSuiteName>

## Failure Type
<crash|timeout|exception|assertion>

## Exception Type
<ExceptionType>

## Stack Trace
\```
<full stack trace>
\```

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

## Comparing Test Sets

Use the comparison script to identify differences between CoreCLR and Mono test sets:

```bash
./browser-tests/compare-test-results.sh <TestProjectName>
```

This uses `sort` and `comm` to compare sorted test name lists and reports:
- **Extra in CoreCLR**: Tests that ran on CoreCLR but not Mono (usually `[SkipOnMono]` tests)
- **Missing in CoreCLR**: Tests that ran on Mono but not CoreCLR (potential issues)

Common benign differences:
- DateTime/timestamp values in test names (e.g., `2026-01-28T02:18:05` vs `2026-01-29T16:09:02`)
- Random string suffixes (e.g., `Ahoj1082296292` vs `Ahoj487988353`)
- Renamed tests between versions

## Summary.md Template

Create or update `browser-tests/results/<TestProject>/Summary.md` after each test run:

```markdown
# <TestProject> Summary

## Latest Run
- **Date:** YYYY-MM-DD
- **CoreCLR:** Tests run: X, Passed: Y, Failed: Z, Skipped: N
- **Mono Baseline:** Tests run: X, Passed: Y, Failed: Z, Skipped: N
- **Status:** ✅ All pass | ⚠️ Tests disabled | ❌ Failures

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh <TestProject>`

### Extra in CoreCLR (X tests)

_Tests that run on CoreCLR but were skipped on Mono._

| Test Name | Reason |
|-----------|--------|
| ClassName.MethodName | [SkipOnMono] or new test |

### Missing in CoreCLR (X tests)

_Tests that ran on Mono but not CoreCLR. Investigate if unexpected._

| Test Name | Reason |
|-----------|--------|
| ClassName.MethodName | renamed / timestamp diff / actual issue |

## Disabled Tests (ActiveIssue #123011)

| Test Name | Failure Type | Category |
|-----------|--------------|----------|
| ClassName.MethodName | timeout | threading |

## Notes

_Any observations or patterns._
```
