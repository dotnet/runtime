# System.Runtime.ReflectionInvokeInterpreted.Tests - Browser/WASM CoreCLR Test Results

## Summary

| Metric | Value |
|--------|-------|
| **Status** | ✅ PASSED |
| **Tests Run** | 209 |
| **Tests Passed** | 209 |
| **Tests Failed** | 0 |
| **Tests Skipped** | 0 |
| **Mono Baseline** | 207 tests |
| **Date** | 2026-01-30 |

## Comparison with Mono Baseline

- ✅ All Mono tests also ran on CoreCLR
- CoreCLR has 5 extra tests not in Mono baseline (PointerPropertyGetValue tests)

### Extra in CoreCLR (5 tests)

| Test Name | Reason |
|-----------|--------|
| `PointerTests.PointerPropertyGetValue(value: -1)` | [SkipOnMono] or new test |
| `PointerTests.PointerPropertyGetValue(value: -2147483648)` | [SkipOnMono] or new test |
| `PointerTests.PointerPropertyGetValue(value: 0)` | [SkipOnMono] or new test |
| `PointerTests.PointerPropertyGetValue(value: 1)` | [SkipOnMono] or new test |
| `PointerTests.PointerPropertyGetValue(value: 2147483647)` | [SkipOnMono] or new test |

## Test Categories

The suite covers:
- Ref return method invocation
- Pointer method parameters and returns
- Ref-like argument handling
- BindingFlags.DoNotWrap tests

## Files

- [Console Log](console_20260130_132231.log)
- [Test Results XML](testResults_20260130_132231.xml)
- [Mono Baseline Console](mono-console.log)
- [Mono Baseline Results](mono-testResults.xml)
- [Test Comparison](test-comparison.txt)
