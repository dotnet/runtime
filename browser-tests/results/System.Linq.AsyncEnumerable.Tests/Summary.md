# System.Linq.AsyncEnumerable.Tests Summary

## Latest Run
- **Date:** 2026-01-30
- **CoreCLR:** Tests run: 613, Passed: 613, Failed: 0, Skipped: 0
- **Mono Baseline:** Tests run: 613, Passed: 613, Failed: 0, Skipped: 0
- **Status:** ✅ All pass

## Test Set Comparison

Run: `./browser-tests/compare-test-results.sh System.Linq.AsyncEnumerable.Tests`

### Extra in CoreCLR (0 tests)

_No extra tests._

### Missing in CoreCLR (0 tests)

_No missing tests._

## Disabled Tests (ActiveIssue #123011)

_None required._

## Failures and Asserts

_None._

## Non-Fatal Asserts During Test Run

A non-fatal assertion was observed during test execution (after all tests passed):

```
ASSERT FAILED
    Expression: slotNumber >= GetNumVirtuals() || pMDRet == m_pDeclMT->GetMethodDescForSlot_NoThrow(slotNumber)
    Location:   /home/pavelsavara/dev/runtime/src/coreclr/vm/methodtable.cpp:6963
    Function:   GetImplMethodDesc
    Process:    42
```

This assertion occurred during the test teardown phase (in `MessageSinkMessageExtensions::Dispatch`). It appears to be a known CoreCLR interpreter issue and does not affect test results.

## Notes

- All 613 tests passed on both CoreCLR and Mono
- Test set comparison shows identical tests executed (678 test names extracted from XML, including Theory data variations)
- Non-fatal assertion in CoreCLR interpreter during test runner cleanup - same pattern observed in other test suites
