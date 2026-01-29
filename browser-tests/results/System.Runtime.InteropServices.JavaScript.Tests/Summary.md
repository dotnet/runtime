# System.Runtime.InteropServices.JavaScript.Tests Summary

## Latest Run
- **Date:** 2026-01-29
- **CoreCLR:** Tests run: 457, Passed: 455, Failed: 0, Skipped: 2
- **Mono Baseline:** Tests run: 454, Passed: 452, Failed: 0, Skipped: 2
- **Status:** ✅ All pass

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Runtime.InteropServices.JavaScript.Tests`

### Summary

| Metric | Count |
|--------|-------|
| CoreCLR tests | 457 |
| Mono tests | 454 |
| Extra in CoreCLR | 43 (raw) |
| Missing in CoreCLR | 40 (raw) |

### Analysis

Most differences are **benign** due to:

1. **DateTime timestamps in test names** - Tests use `DateTime.Now` which differs between runs
2. **Random string suffixes** - Tests like `JsExportString(value: "Ahoj<random>")` 
3. **Compiler-generated lambda names** - `b__24_*` vs `b__25_*` differ between builds
4. **Renamed tests** - `TaskOfLong` → `TaskOfBigLong` (API rename)

### Actual New Tests in CoreCLR (not in Mono)

These tests are new or were previously `[SkipOnMono]`:

| Test Name | Notes |
|-----------|-------|
| DateTimeMinValueBoundaryCondition | New boundary test |
| DateTimeMaxValueBoundaryCondition | New boundary test |
| DateTimeMarshallingLosesMicrosecondComponentPrecisionLoss | New precision test |
| Int32ArrayWithOutOfRangeValues | New validation test |
| TaskOfByteOutOfRange_ThrowsAssertionInTaskContinuation | New overflow test |
| TaskOfShortOutOfRange_ThrowsAssertionInTaskContinuation | New overflow test |
| TaskOfDateTimeOutOfRange_ThrowsAssertionInTaskContinuation | New overflow test |
| JsExportTaskOfShortOutOfRange_ThrowsAssertionInTaskContinuation | New overflow test |
| JsExportTaskOfStringTypeAssertion_ThrowsAssertionInTaskContinuation | New assertion test |
| JsExportDateTime_ReturnValue_OverflowNETDateTime | New overflow test |
| JsExportTaskOfDateTime_TaskReturnValue_OverflowNETDateTime | New overflow test |
| JsExportFuncOfDateTime_Argument_OverflowNETDateTime | New overflow test |
| JsExportTaskOfLong_TaskReturnValue_OverflowInt52 | New overflow test |
| JsExportCallback_FunctionLongLong_OverflowInt52_JSSide | New overflow test |
| JsExportCallback_FunctionLongLong_OverflowInt52_NETSide | New overflow test |
| JsExportFunctionDateTimeDateTime | New function test |

### Tests Renamed (Mono → CoreCLR)

| Mono Name | CoreCLR Name |
|-----------|--------------|
| JsExportTaskOfLong | JsExportTaskOfBigLong |
| JsExportCompletedTaskOfLong | JsExportCompletedTaskOfBigLong |

### Tests Removed/Changed

Single/Float array tests appear removed or refactored:

| Mono Test | Status |
|-----------|--------|
| JsExportSpanOfDouble (3 variants) | Removed |
| JsExportSpanOfSingle (3 variants) | Removed |
| JsExportArraySegmentOfDouble | Removed |
| JsExportArraySegmentOfSingle | Removed |
| JsImportSingleArray (3 variants) | Removed |
| JsImportSpanOfSingle | Removed |
| JsImportArraySegmentOfSingle | Removed |

## Disabled Tests (ActiveIssue #123011)

_None - all tests pass._

## Notes

- CoreCLR runs **3 more tests** than Mono baseline (457 vs 454)
- All enabled tests pass on both runtimes
- Test differences are due to API changes/renames, not failures
- The 2 skipped tests are the same on both runtimes
