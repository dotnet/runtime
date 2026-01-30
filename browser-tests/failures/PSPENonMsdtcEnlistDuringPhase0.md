# PSPENonMsdtcEnlistDuringPhase0

## Test Location
- **File:** src/libraries/System.Transactions.Local/tests/NonMsdtcPromoterTests.cs
- **Method:** PSPENonMsdtcEnlistDuringPhase0
- **Test Suite:** System.Transactions.Local.Tests

## Failure Description
Test fails on Browser+CoreCLR with transaction-related assertion failures. Multiple Theory cases fail intermittently.

## Error Message
```
Assert.Null() Failure: Value is not null
Expected: null
Actual:   System.ApplicationException: Exception System.Transactions.TransactionPromotionException: There is a promotable enlistment for the transaction which has a PromoterType value that is not recognized by System.Transactions.
```

## Stack Trace
```
at System.Transactions.Tests.NonMsdtcPromoterTests.TestCase_FailPromotableSinglePhaseNotificationCalls()
at System.Transactions.Tests.NonMsdtcPromoterTests.TryProhibitedOperations(Transaction tx, Guid expectedPromoterType)
at System.Transactions.Tests.NonMsdtcPromoterTests.CreatePSPEEnlistment(...)
at System.Transactions.Tests.NonMsdtcPromoterTests.TestCase_EnlistDuringPrepare(...)
at System.Transactions.Tests.NonMsdtcPromoterTests.PSPENonMsdtcEnlistDuringPhase0(...)
```

## Analysis
This test exercises PSPE (Promotable Single Phase Enlistment) non-MSDTC scenarios with enlistment during Phase 0 of two-phase commit. The failure appears related to:
1. Transaction promotion behavior differences on Browser+CoreCLR
2. Interpreter reflection issues with Guid handling (PromoterType is a Guid)
3. Possible state leaking from other skipped/disabled tests in the same fixture

## Resolution
Marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]`

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011
