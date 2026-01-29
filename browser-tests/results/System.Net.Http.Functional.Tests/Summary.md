# System.Net.Http.Functional.Tests Summary

## Latest Run
- **Date:** 2026-01-29
- **Configuration:** Release (Debug skipped by `[SkipOnCoreClr]`)
- **CoreCLR:** Tests run: 901, Passed: 781, Failed: 0, Skipped: 120
- **Mono Baseline:** Tests run: 901, Passed: 781, Failed: 0, Skipped: 120
- **Status:** ✅ All pass - identical to Mono baseline

## Important Notes

### Configuration Requirement

This test suite has an assembly-level skip for CoreCLR in Debug mode:

```csharp
[assembly: SkipOnCoreClr("System.Net.Tests are flaky and/or long running: https://github.com/dotnet/runtime/issues/131", ~RuntimeConfiguration.Release)]
```

**You must run with `-c Release` to execute tests on CoreCLR!**

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Net.Http.Functional.Tests`

### Summary

| Metric | Count |
|--------|-------|
| CoreCLR tests | 899 |
| Mono tests | 899 |
| Extra in CoreCLR | 183 (all port differences) |
| Missing in CoreCLR | 183 (all port differences) |

### Analysis

All 366 differences are **benign port number differences** - the same tests use different local server ports in test parameters:

- CoreCLR: `127.0.0.1:42257` / `127.0.0.1:44881`
- Mono: `127.0.0.1:49170` / `127.0.0.1:49171`

This is expected behavior - test servers run on ephemeral ports assigned by the OS.

**No actual test differences between CoreCLR and Mono.**

## Disabled Tests (ActiveIssue #123011)

_None - all tests pass._

## Notes

- Results are **identical** to Mono baseline
- 120 tests skipped (same as Mono) - these are skipped via test traits/platform detection
- Test comparison shows port differences only, not actual test differences
