# Test Failure Report

## Test Information
- **Test Suite**: System.Transactions.Local.Tests
- **Test Class**: System.Transactions.Tests.NonMsdtcPromoterTests
- **Test Method**: PSPENonMsdtcSetDistributedTransactionIdentifierCallWithWrongNotificationObject
- **Test File**: src/libraries/System.Transactions.Local/tests/NonMsdtcPromoterTests.cs

## Failure Details
- **Platform**: Browser/WASM + CoreCLR (interpreter)
- **Passes on Mono**: Yes
- **GitHub Issue**: https://github.com/dotnet/runtime/issues/123011

## Error Message
```
Assert.Null() Failure: Value is not null
Expected: null
Actual: System.ApplicationException: Exception System.Transactions.TransactionPromotionException: 
There is a promotable enlistment for the transaction which has a PromoterType value that is 
not recognized by System.Transactions. d9a34fdf-d02a-4eed-98c3-5ad092355e17
```

## Stack Trace
```
at System.Transactions.InternalTransaction.ThrowIfPromoterTypeIsNotMSDTC()
at System.Transactions.Transaction.Promote()
at System.Transactions.TransactionInterop.ConvertToOletxTransaction(Transaction transaction)
at System.Transactions.TransactionInterop.GetDtcTransaction(Transaction transaction)
at System.Transactions.Tests.NonMsdtcPromoterTests.TryProhibitedOperations(Transaction tx, Guid expectedPromoterType)
at System.Transactions.Tests.NonMsdtcPromoterTests.CreatePSPEEnlistment(...)
at System.Transactions.Tests.NonMsdtcPromoterTests.TestCase_SetDistributedIdWithWrongNotificationObject()
at System.Transactions.Tests.NonMsdtcPromoterTests.PSPENonMsdtcSetDistributedTransactionIdentifierCallWithWrongNotificationObject()
```

## Root Cause
The test expects `TryProhibitedOperations` to return `null` (meaning all prohibited operations correctly threw exceptions). On CoreCLR+Browser, the transaction promotion path behaves differently than on Mono, causing the test to fail.

## Fix Applied
Added `[ActiveIssue]` attribute to skip this test on Browser+CoreCLR until the underlying issue is resolved.
