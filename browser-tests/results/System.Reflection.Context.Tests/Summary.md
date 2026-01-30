# System.Reflection.Context.Tests - Browser/WASM CoreCLR Test Results

## Summary

| Metric | Value |
|--------|-------|
| **Status** | ✅ PASSED |
| **Tests Run** | 110 |
| **Tests Passed** | 109 |
| **Tests Failed** | 0 |
| **Tests Skipped** | 1 |
| **Mono Baseline** | 118 tests |
| **CoreCLR Total** | 121 test cases |
| **Date** | 2026-01-30 |

## Comparison with Mono Baseline

- ✅ All Mono tests also ran on CoreCLR
- CoreCLR has 3 extra tests not in Mono baseline:
  - `CustomReflectionContextTests.MapType_Interface_Throws`
  - `CustomReflectionContextTests.MapType_MemberAttributes_Success`
  - `CustomReflectionContextTests.MapType_ParameterAttributes_Success`

## Test Categories

The suite covers:
- Virtual property info tests (getters, setters)
- Custom assembly projection tests
- Projecting module tests
- Inherited property/method info tests
- Custom reflection context tests

## Files

- [Console Log](console_20260130_124956.log)
- [Test Results XML](testResults_20260130_124956.xml)
- [Mono Baseline Console](mono-console.log)
- [Mono Baseline Results](mono-testResults.xml)
- [Test Comparison](test-comparison.txt)
