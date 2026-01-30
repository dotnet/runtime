# PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls

## Test Location
- **File:** src/libraries/System.Transactions.Local/tests/NonMsdtcPromoterTests.cs
- **Method:** PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls
- **Test Suite:** System.Transactions.Local.Tests

## Failure Description
Test fails intermittently with NullReferenceException on Browser+CoreCLR.

## Error Message
```
System.NullReferenceException : Object reference not set to an instance of an object.
```

## Stack Trace
```
at System.Transactions.Tests.NonMsdtcPromoterTests.TestCase_FailPromotableSinglePhaseNotificationCalls()
at System.Transactions.Tests.NonMsdtcPromoterTests.PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls()
at System.RuntimeMethodHandle.InvokeMethod(...)
at System.Reflection.MethodBaseInvoker.InterpretedInvoke_Method(Object obj, IntPtr* args)
```

## Analysis
This test is related to non-MSDTC promotable transactions. The failure appears intermittent/flaky, passing on some runs and failing on others. This suggests:
1. A race condition or timing issue in the interpreter
2. Or state leaking from other skipped tests that were previously sharing setup

## Resolution
Marked with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011", typeof(PlatformDetection), nameof(PlatformDetection.IsBrowser), nameof(PlatformDetection.IsCoreCLR))]`

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011
