// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

internal sealed class VoterNotifyShim : NotificationShimBase, ITransactionVoterNotifyAsync2
{
    internal VoterNotifyShim(DtcProxyShimFactory shimFactory, object enlistmentIdentifier)
        : base(shimFactory, enlistmentIdentifier)
    {
    }

    public void VoteRequest()
    {
        NotificationType = ShimNotificationType.VoteRequestNotify;
        ShimFactory.NewNotification(this);
    }

    public void Committed([MarshalAs(UnmanagedType.Bool)] bool fRetaining, IntPtr pNewUOW, uint hresult)
    {
        NotificationType = ShimNotificationType.CommittedNotify;
        ShimFactory.NewNotification(this);
    }

    public void Aborted(IntPtr pboidReason, [MarshalAs(UnmanagedType.Bool)] bool fRetaining, IntPtr pNewUOW, uint hresult)
    {
        NotificationType = ShimNotificationType.AbortedNotify;
        ShimFactory.NewNotification(this);
    }

    public void HeuristicDecision([MarshalAs(UnmanagedType.U4)] OletxTransactionHeuristic dwDecision, IntPtr pboidReason, uint hresult)
    {
        NotificationType = dwDecision switch {
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
