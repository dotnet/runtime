# PSPENonMsdtcAbortingCloneNotCompleted

## Test Location
- **File:** src/libraries/System.Transactions.Local/tests/NonMsdtcPromoterTests.cs
- **Method:** PSPENonMsdtcAbortingCloneNotCompleted
- **Test Suite:** System.Transactions.Local.Tests

## Failure Description
Test fails on Browser+CoreCLR with unexpected exception type when aborting an incomplete dependent clone.

## Error Message
```
Assert.IsType() Failure: Value is not the exact type
Expected: typeof(System.Transactions.TransactionAbortedException)
Actual:   typeof(System.ApplicationException)
```

## Stack Trace
```
at System.Transactions.Tests.NonMsdtcPromoterTests.TestCase_AbortingCloneNotCompleted(Boolean promote)
at System.Transactions.Tests.NonMsdtcPromoterTests.PSPENonMsdtcAbortingCloneNotCompleted(Boolean promote)
```

## Analysis
The test expects `TransactionAbortedException` when aborting a dependent clone that is not completed. On Browser+CoreCLR, the interpreter is returning `ApplicationException` instead. This pattern is similar to other NonMsdtcPromoterTests failures where exception types are not properly resolved through the interpreter reflection path.

## Resolution
Marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]`

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011
