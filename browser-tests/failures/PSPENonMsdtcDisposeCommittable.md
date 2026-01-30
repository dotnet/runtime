# PSPENonMsdtcDisposeCommittable

## Test Location
- **File:** src/libraries/System.Transactions.Local/tests/NonMsdtcPromoterTests.cs
- **Method:** PSPENonMsdtcDisposeCommittable
- **Test Suite:** System.Transactions.Local.Tests

## Failure Description
Test fails on Browser+CoreCLR with unexpected exception type.

## Error Message
```
Assert.IsType() Failure: Value is not the exact type
Expected: typeof(System.ObjectDisposedException)
Actual:   typeof(System.ApplicationException)
```

## Stack Trace
```
at System.Transactions.Tests.NonMsdtcPromoterTests.TestCase_DisposeCommittableTransaction(Boolean promote)
at System.Transactions.Tests.NonMsdtcPromoterTests.PSPENonMsdtcDisposeCommittable(Boolean promote)
```

## Analysis
The test expects `ObjectDisposedException` when disposing a committable transaction early, but receives `ApplicationException` on Browser+CoreCLR. This indicates the interpreter is not properly handling the exception type hierarchy or exception generation in transaction disposal scenarios.

## Resolution
Marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]`

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011
