# System.Reflection.DispatchProxy.Tests - Browser/WASM CoreCLR Test Results

## Summary

| Metric | Value |
|--------|-------|
| **Status** | ⚠️ PASSED (1 test disabled) |
| **Tests Run** | 88 |
| **Tests Passed** | 88 |
| **Tests Failed** | 0 |
| **Tests Skipped** | 1 (ActiveIssue) |
| **Mono Baseline** | 89 tests |
| **Date** | 2026-01-30 |

## Comparison with Mono Baseline

- ✅ All Mono tests also ran on CoreCLR (except 1 disabled)
- CoreCLR has 2 extra tests not in Mono baseline (Test_Unloadability)
- 1 test disabled with ActiveIssue for Browser/WASM

## Disabled Tests

| Test | Issue | Reason |
|------|-------|--------|
| `Create_Using_PrivateProxyAndInternalServiceWithExternalGenericArgument` | #123011 | TypeLoadException: 'Type from assembly is attempting to implement an inaccessible interface' |

## Failure Records

| Test | Type | Link |
|------|------|------|
| `Create_Using_PrivateProxyAndInternalServiceWithExternalGenericArgument` | exception | [Failure Report](../../failures/System.Reflection.DispatchProxy.Tests/DispatchProxyTests.Create_Using_PrivateProxyAndInternalServiceWithExternalGenericArgument.md) |

## Test Categories

The suite covers:
- DispatchProxy creation with various configurations
- Proxy method invocation and argument passing
- Interface property/event/indexer proxying
- Exception handling through proxies
- Assembly load context tests
- Static virtual method handling

## Files

- [Console Log](console_20260130_125054.log)
- [Test Results XML](testResults_20260130_125054.xml)
- [Mono Baseline Console](mono-console.log)
- [Mono Baseline Results](mono-testResults.xml)
- [Test Comparison](test-comparison.txt)
