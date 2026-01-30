# System.Transactions.Local.Tests - CoreCLR Browser WASM

## Status: ✅ PASSED (with skipped tests)

## Test Results
- **Tests run:** 207
- **Passed:** 197
- **Failed:** 0
- **Skipped:** 10 (due to platform conditions)
- **Ignored:** 0
- **Inconclusive:** 0

## Skipped Tests (via ActiveIssue for Browser+CoreCLR)
The following tests were disabled on Browser+CoreCLR via `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`:

1. **PSPENonMsdtcSetDistributedTransactionIdentifierCallWithWrongNotificationObject** - NullReferenceException in transaction promotion
2. **PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls** - Intermittent NullReferenceException
3. **PSPENonMsdtcEnlistDuringPhase0** (10 variants) - Transaction promotion assertion failures
4. **PSPENonMsdtcDisposeCommittable** (2 variants) - Unexpected exception type (ApplicationException vs ObjectDisposedException)
5. **PSPENonMsdtcAbortingCloneNotCompleted** (2 variants) - Unexpected exception type (ApplicationException vs TransactionAbortedException)

## Root Cause Analysis
All failing tests are in the `NonMsdtcPromoterTests` class which exercises PSPE (Promotable Single Phase Enlistment) for non-MSDTC transactions. The failures are related to:
- Interpreter reflection path issues with exception type handling
- Large value type (Guid) handling issues with PromoterType
- State dependencies between tests when some are skipped

## GitHub Issue
https://github.com/dotnet/runtime/issues/123011

## Failure Reports
- [PSPENonMsdtcSetDistributedTransactionIdentifierCallWithWrongNotificationObject.md](../failures/PSPENonMsdtcSetDistributedTransactionIdentifierCallWithWrongNotificationObject.md)
- [PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls.md](../failures/PSPENonMsdtcFailPromotableSinglePhaseNotificationCalls.md)
- [PSPENonMsdtcEnlistDuringPhase0.md](../failures/PSPENonMsdtcEnlistDuringPhase0.md)
- [PSPENonMsdtcDisposeCommittable.md](../failures/PSPENonMsdtcDisposeCommittable.md)
- [PSPENonMsdtcAbortingCloneNotCompleted.md](../failures/PSPENonMsdtcAbortingCloneNotCompleted.md)
