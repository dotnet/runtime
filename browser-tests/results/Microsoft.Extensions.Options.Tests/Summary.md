# Microsoft.Extensions.Options.Tests Summary

## Latest Run
- **Date:** 2026-01-30
- **CoreCLR:** Tests run: 105, Passed: 105, Failed: 0, Skipped: 0
- **Mono Baseline:** Tests run: 107, Passed: 107, Failed: 0, Skipped: 0
- **Status:** ⚠️ Tests disabled

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh Microsoft.Extensions.Options.Tests`

### Extra in CoreCLR (1 test)

_Tests that run on CoreCLR but were skipped on Mono._

| Test Name | Reason |
|-----------|--------|
| OptionsMonitorTest.TestCurrentValueDoesNotAllocateOnceValueIsCached | [SkipOnMono] |

### Missing in CoreCLR (3 tests)

_Tests that ran on Mono but not CoreCLR due to ActiveIssue._

| Test Name | Reason |
|-----------|--------|
| OptionsTest.Configure_GetsNullableOptionsFromConfiguration (3 Theory cases) | [ActiveIssue #123011] - nullable binding issue |

## Disabled Tests (ActiveIssue #123011)

| Test Name | Failure Type | Category |
|-----------|--------------|----------|
| OptionsTest.Configure_GetsNullableOptionsFromConfiguration | assertion | configuration-binding |

## Failures and Asserts

| Issue | Type | Link |
|-------|------|------|
| Configure_GetsNullableOptionsFromConfiguration | assertion | [OptionsTest.Configure_GetsNullableOptionsFromConfiguration.md](../../failures/Microsoft.Extensions.Options.Tests/OptionsTest.Configure_GetsNullableOptionsFromConfiguration.md) |

## Notes

- The nullable binding issue affects configuration binding when non-null values are bound to Nullable<T> properties
- When values are null, the test passes
- This appears to be a CoreCLR/Browser specific issue with reflection-based configuration binding
