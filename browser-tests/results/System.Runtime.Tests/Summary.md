# System.Runtime.Tests Summary

## Latest Run
- **Date:** 2026-01-30
- **CoreCLR:** Tests run: 64,163 (partial - run was interrupted), Passed: 64,049, Failed: 3, Skipped: 111
- **Mono Baseline:** Tests run: 67,836, Passed: 67,724, Failed: 0, Skipped: 112
- **Status:** ⚠️ Run incomplete (interrupted) + 3 failures found

## Test Results
The test run was interrupted before completion (64,163 of 67,836 tests ran).

From the partial results:
- Pass rate: 99.8% (of completed tests)
- 3 test failures found before interruption

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Runtime.Tests`

**Note:** Comparison pending - run was incomplete.

## Disabled Tests (ActiveIssue #123011)

| Test Name | Failure Type | Category |
|-----------|--------------|----------|
| (none yet) | | |

## Failures Found

| Test | Type | Category | Link |
|------|------|----------|------|
| Int128Tests.CompareTo_Other_ReturnsExpected | assertion | interpreter | [Int128Tests.CompareTo_Other_ReturnsExpected.md](../../failures/System.Runtime.Tests/Int128Tests.CompareTo_Other_ReturnsExpected.md) |
| UInt128Tests.CompareTo_Other_ReturnsExpected | assertion | interpreter | [UInt128Tests.CompareTo_Other_ReturnsExpected.md](../../failures/System.Runtime.Tests/UInt128Tests.CompareTo_Other_ReturnsExpected.md) |
| StringTests.Contains_Rune_StringComparison | exception | interpreter | [StringTests.Contains_Rune_StringComparison.md](../../failures/System.Runtime.Tests/StringTests.Contains_Rune_StringComparison.md) |

## Failure Analysis

### Int128Tests.CompareTo_Other_ReturnsExpected / UInt128Tests.CompareTo_Other_ReturnsExpected
- **Issue:** When comparing Int128/UInt128(234) to itself (as boxed Object), CompareTo returns 1 instead of 0
- **Root cause:** Likely an issue with boxed value type comparison in the interpreter
- **Both Int128 and UInt128 have the same issue**

### StringTests.Contains_Rune_StringComparison
- **Issue:** `ArgumentOutOfRangeException` thrown in Rune constructor
- **Root cause:** Rune explicit cast from Char fails with certain character values

## Notes

- Test run was interrupted before completion
- Need to re-run to get complete results
- The 3 failures appear to be interpreter-related issues with value type comparisons and Rune handling
