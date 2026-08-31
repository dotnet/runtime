// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class TransactionNotifyShim : NotificationShimBase, ITransactionOutcomeEvents
{
    internal TransactionNotifyShim(DtcProxyShimFactory shimFactory, object? enlistmentIdentifier)
        : base(shimFactory, enlistmentIdentifier)
    {
    }

    public void Committed(bool fRetaining, IntPtr pNewUOW, int hresult)
    {
        NotificationType = ShimNotificationType.CommittedNotify;
        ShimFactory.NewNotification(this);
    }

    public void Aborted(IntPtr pboidReason, bool fRetaining, IntPtr pNewUOW, int hresult)
    {
        NotificationType = ShimNotificationType.AbortedNotify;
        ShimFactory.NewNotification(this);
    }

    public void HeuristicDecision(OletxTransactionHeuristic dwDecision, IntPtr pboidReason, int hresult)
    {
        NotificationType = dwDecision switch
        {
            OletxTransactionHeuristic.XACTHEURISTIC_ABORT => ShimNotificationType.AbortedNotify,
            OletxTransactionHeuristic.XACTHEURISTIC_COMMIT => ShimNotificationType.CommittedNotify,
            _ => ShimNotificationType.InDoubtNotify
        };

        ShimFactory.NewNotification(this);
    }

    public void Indoubt()
    {
        NotificationType = ShimNotificationType.InDoubtNotify;
        ShimFactory.NewNotification(this);
    }
}
