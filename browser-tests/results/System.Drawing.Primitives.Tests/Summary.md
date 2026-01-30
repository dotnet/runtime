# System.Drawing.Primitives.Tests - Browser WASM Test Summary

## Test Configuration
- **Runtime:** CoreCLR (interpreter, no JIT)
- **Test Environment:** browser-wasm, Chrome via xharness
- **Build Configuration:** Release
- **Date:** 2026-01-30

## Results

| Metric | Value |
|--------|-------|
| Total Tests | 2439 |
| Passed | 1863 |
| Failed | 574 |
| Skipped | 2 |

## Comparison with Mono Baseline
- Mono tests: 2444
- CoreCLR tests: 2444
- Missing in CoreCLR: 0
- Extra in CoreCLR: 0

✅ All Mono tests also ran on CoreCLR (but some failed).

## Failed Tests Analysis

The failures appear to be in two main categories:

### 1. ColorTests.GetHashCodeTest failures
All `GetHashCodeTest` tests fail with hash code mismatches. The actual value is consistently `1896580171` instead of the expected values. This suggests a potential issue with the `Color.GetHashCode()` implementation on CoreCLR WASM.

Example:
```
Assert.Equal() Failure: Values differ
Expected: -679176371
Actual:   1896580171
```

### 2. ColorTranslatorTests.FromHtml_String_ReturnsExpected failures
Color parsing from HTML strings fails for named colors:
- `Blue` returns `Color [Empty]` instead of `Color [Blue]`
- `"Blue"` (quoted) returns `Color [Empty]` instead of `Color [Blue]`
- `255,0,0` returns `Color [A=255, R=255, G=0, B=0]` instead of `Color [Red]`

This suggests issues with the color name lookup table or parsing logic on CoreCLR WASM.

## Conclusion
**FAILED** - 574 test failures, primarily in Color hash code and HTML color parsing functionality. Needs ActiveIssue tracking.
