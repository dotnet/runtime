# System.Collections.Immutable.Tests - Browser/WASM CoreCLR Test Results

## Test Run Summary

| Metric | CoreCLR | Mono Baseline | Delta |
|--------|---------|---------------|-------|
| Tests Run | 22420 | 22497 | -77 |
| Passed | 22279 | 22441 | -162 |
| Failed | 85 | 0 | +85 |
| Skipped | 56 | 56 | 0 |

## Status: ❌ FAILING

The test suite is NOT passing due to runtime bugs in the CoreCLR interpreter.

## Known Issues Found

### 1. EnumComparer<T>.Compare Bug (85 failures)
**Issue:** `System.TypeLoadException: Could not load type 'Invalid_Token.0x02000000'`

All 85 failures are related to the `EnumComparer<T>.Compare` method crashing when comparing enum values with the CoreCLR interpreter. This affects:
- `FrozenDictionary_Generic_Tests_ContiguousFromZeroEnum_byte.LookupItems_AllItemsFoundAsExpected` (all variants with size >= 2)

**Stack trace pattern:**
```
System.InvalidOperationException : Failed to compare two elements in the array.
---- System.TypeLoadException : Could not load type 'Invalid_Token.0x02000000' from assembly 'System.Private.CoreLib'
   at System.Collections.Generic.EnumComparer`1.Compare(T x, T y)
   at System.Collections.Generic.ArraySortHelper`2.SwapIfGreaterWithValues(...)
   at System.Collections.Frozen.SmallValueTypeComparableFrozenDictionary`2..ctor(...)
```

**Root cause:** The CoreCLR interpreter has a bug with generic enum type resolution when instantiating `EnumComparer<T>` for comparison operations.

**Related GitHub Issue:** This should be filed as a new CoreCLR interpreter bug.

### 2. Tests Previously Marked as ActiveIssue

The following tests were marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]` due to earlier crashes:
- `ImmutableArrayTest.DebuggerAttributesValid_AdditionalCases`
- `ImmutableArrayTest.Add` (line 1127)
- `ImmutableDictionaryTestBase.DebuggerAttributesValid` (line 104)

## Tests Missing in CoreCLR (16 tests)

These tests ran in Mono but not CoreCLR:
- `FrozenFromKnownValuesTests.FrozenDictionary_Int32String` (6 variants)
- `FrozenFromKnownValuesTests.FrozenSet_Int32String` (7 variants)
- `ImmutableArrayTest.ContainsNull<T>` (3 variants)

## Recommendations

1. **File EnumComparer bug:** Create a GitHub issue for the `EnumComparer<T>.Compare` `TypeLoadException` with `Invalid_Token.0x02000000`

2. **Do NOT mark 85 tests as ActiveIssue:** These failures are all caused by the same underlying bug. Once the EnumComparer bug is fixed, all 85 tests should pass.

3. **Investigate missing tests:** The 16 tests that ran in Mono but not CoreCLR may indicate test discovery issues or conditional compilation differences.

## Files

- Test results: `testResults_20260130_012044.xml`
- Console log: `wasm-console_20260130_012044.log`
- Comparison: `test-comparison.txt`

## Date

2026-01-30
