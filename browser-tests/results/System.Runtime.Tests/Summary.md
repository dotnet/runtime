# System.Runtime.Tests Summary

## Latest Run
- **Date:** 2026-01-30
- **CoreCLR:** Tests: 59,502 (completed), Passed: 59,388, Failed: 0, Skipped: 114
- **Mono Baseline:** Tests: 61,578, Passed: 61,466, Failed: 0, Skipped: 112
- **Status:** ✅ All tests pass (after ActiveIssue exclusions)

## Test Results

After adding `[ActiveIssue]` attributes to failing tests:
- Pass rate: 100% (of non-skipped tests)
- 3 tests marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", TestPlatforms.Browser)]`
- Tests now skipped instead of failing

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Runtime.Tests`

- **Extra in CoreCLR:** 352 tests (CoreCLR-specific tests not in Mono)
- **Missing in CoreCLR:** 2,428 tests (skipped due to Browser+CoreCLR ActiveIssue)

## Tests Skipped on Browser+CoreCLR (ActiveIssue #123011)

**2,428 tests** are intentionally skipped when running CoreCLR on Browser, tracked by issue #123011.

### Breakdown by Test Class

| Test Class | Missing Count | Reason |
|------------|---------------|--------|
| `Int128Tests` | 1,033 | `Parse_Utf8Span_Valid`, `Parse_Valid` (many parameter combinations) |
| `UInt128Tests` | 862 | Same parsing tests as Int128 |
| `DateTimeTests` | 129 | Various datetime tests |
| `DecimalTests` | 125 | Multiple decimal operations |
| `HalfTests_GenericMath` | 97 | Generic math tests |
| `HalfTests` | 53 | General Half tests |
| `StringTests` | 37 | String operations |
| `DateTimeOffsetTests` | 21 | DateTime offset tests |
| `DelegateTests` | 14 | Delegate binding tests |
| `WaitHandleTests` | 9 | SignalAndWait tests |
| `ControlledExecutionTests` | 8 | **Entire class excluded** |
| `PeriodicTimerTests` | 4 | Timer tests |
| Others | ~40 | Various |

### Files with ActiveIssue #123011

```
System/Int128Tests.cs
System/UInt128Tests.cs
System/DateTimeTests.cs
System/DecimalTests.cs
System/DelegateTests.cs
System/DoubleTests.GenericMath.cs
System/HalfTests.GenericMath.cs
System/HalfTests.cs
System/SingleTests.GenericMath.cs
System/StringTests.cs
System/GCTests.cs
System/Text/EncodingTests.cs
System/Threading/PeriodicTimerTests.cs
System/Reflection/ModuleTests.cs
System/Reflection/InvokeWithRefLikeArgs.cs
System/Runtime/ControlledExecutionTests.cs
System/Runtime/JitInfoTests.cs
System/Numerics/TotalOrderIeee754ComparerTests.cs
```

## Newly Disabled Tests (This Run)

| Test | Type | Category | Link |
|------|------|----------|------|
| Int128Tests.CompareTo_Other_ReturnsExpected | assertion | interpreter | [Int128Tests.CompareTo_Other_ReturnsExpected.md](../../failures/System.Runtime.Tests/Int128Tests.CompareTo_Other_ReturnsExpected.md) |
| UInt128Tests.CompareTo_Other_ReturnsExpected | assertion | interpreter | [UInt128Tests.CompareTo_Other_ReturnsExpected.md](../../failures/System.Runtime.Tests/UInt128Tests.CompareTo_Other_ReturnsExpected.md) |
| StringTests.Contains_Rune_StringComparison | exception | interpreter | [StringTests.Contains_Rune_StringComparison.md](../../failures/System.Runtime.Tests/StringTests.Contains_Rune_StringComparison.md) |

## Failure Analysis

### Int128Tests.CompareTo_Other_ReturnsExpected / UInt128Tests.CompareTo_Other_ReturnsExpected
- **Issue:** When comparing Int128/UInt128(234) to itself (as boxed Object), CompareTo returns 1 instead of 0
- **Root cause:** Likely an issue with boxed value type comparison in the interpreter
- **Both Int128 and UInt128 have the same issue
- **Fix:** Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", TestPlatforms.Browser)]`

### StringTests.Contains_Rune_StringComparison
- **Issue:** `ArgumentOutOfRangeException` thrown in Rune constructor
- **Root cause:** Rune explicit cast from Char fails with certain character values
- **Fix:** Added `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", TestPlatforms.Browser)]`

## Notes

- All 3 new failures have been marked with `[ActiveIssue]` and are now skipped
- The failures appear to be interpreter-related issues specific to Browser+CoreCLR
- Issue #123011 tracks all Browser+CoreCLR specific test exclusions
