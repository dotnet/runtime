# System.Threading.Overlapped.Tests - Browser/WASM CoreCLR Test Results

## Summary

| Metric | Value |
|--------|-------|
| **Status** | ⚠️ PASSED (3 tests disabled) |
| **Tests Run** | 18 |
| **Tests Passed** | 18 |
| **Tests Failed** | 0 |
| **Tests Skipped** | 3 (ActiveIssue) |
| **Mono Baseline** | 21 tests |
| **Date** | 2026-01-30 |

## Comparison with Mono Baseline

- ✅ All Mono tests also ran on CoreCLR
- Test counts match baseline after disabling 3 tests that only run on CoreCLR (SkipOnMono)

## Disabled Tests

| Test | Issue | Reason |
|------|-------|--------|
| `PreAllocatedOverlapped_NullAsCallback_ThrowsArgumentNullException` | #123011 | No ArgumentNullException thrown on Browser/WASM |
| `PreAllocatedOverlapped_NonBlittableTypeAsPinData_Throws` | #123011 | No ArgumentException thrown on Browser/WASM |
| `PreAllocatedOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws` | #123011 | No ArgumentException thrown on Browser/WASM |

## Failure Records

| Test | Type | Link |
|------|------|------|
| `PreAllocatedOverlapped_NullAsCallback_ThrowsArgumentNullException` | exception | [Failure Report](../../failures/System.Threading.Overlapped.Tests/PreAllocatedOverlapped_NullAsCallback_ThrowsArgumentNullException.md) |
| `PreAllocatedOverlapped_NonBlittableTypeAsPinData_Throws` | exception | [Failure Report](../../failures/System.Threading.Overlapped.Tests/PreAllocatedOverlapped_NonBlittableTypeAsPinData_Throws.md) |
| `PreAllocatedOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws` | exception | [Failure Report](../../failures/System.Threading.Overlapped.Tests/PreAllocatedOverlapped_ObjectArrayWithNonBlittableTypeAsPinData_Throws.md) |

## Test Categories

The suite covers:
- Overlapped property tests
- Pack/Unpack tests
- ThreadPoolBoundHandle tests
- PreAllocatedOverlapped tests

## Notes

The disabled tests were originally `[SkipOnMono]` meaning they only run on CoreCLR. On Browser/WASM CoreCLR interpreter mode, the validation for non-blittable types and null callbacks behaves differently - no exceptions are thrown.

## Files

- [Console Log](console_20260130_132513.log)
- [Test Results XML](testResults_20260130_132513.xml)
- [Mono Baseline Console](mono-console.log)
- [Mono Baseline Results](mono-testResults.xml)
- [Test Comparison](test-comparison.txt)
