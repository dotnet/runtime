# System.Net.WebSockets.Tests Summary

## Latest Run
- **Date:** 2026-01-29
- **Configuration:** Debug
- **CoreCLR:** Tests run: 268, Passed: 266, Failed: 0, Skipped: 2
- **Mono Baseline:** Tests run: 268, Passed: 266, Failed: 0, Skipped: 2
- **Status:** ✅ All pass - identical to Mono baseline

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Net.WebSockets.Tests`

### Summary

| Metric | Count |
|--------|-------|
| CoreCLR tests | 276 |
| Mono tests | 276 |
| Extra in CoreCLR | 0 |
| Missing in CoreCLR | 0 |

**Test sets are identical.**

## Disabled Tests (ActiveIssue #123011)

_None - all tests pass._

## Failures and Asserts

| Issue | Type | Link |
|-------|------|------|
| GetImplMethodDesc | assertion (non-fatal) | [GetImplMethodDesc.Assert.md](../../failures/System.Net.WebSockets.Tests/GetImplMethodDesc.Assert.md) |

## Notes

The CoreCLR interpreter emitted ASSERT FAILED messages after all tests completed successfully. This did not affect test results - exit code was 0 and all 268 tests passed. The assert is likely related to the known "finalizers don't work" limitation of CoreCLR WASM.
