// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

[GeneratedComClass]
internal sealed partial class VoterNotifyShim : NotificationShimBase, ITransactionVoterNotifyAsync2
{
    internal VoterNotifyShim(DtcProxyShimFactory shimFactory, object enlistmentIdentifier)
        : base(shimFactory, enlistmentIdentifier)
    {
    }

    public void VoteRequest()
    {
        NotificationType = Interop.Xolehlp.ShimNotificationType.VoteRequestNotify;
        ShimFactory.NewNotification(this);
    }

    public void Committed([MarshalAs(UnmanagedType.Bool)] bool fRetaining, IntPtr pNewUOW, uint hresult)
    {
        NotificationType = Interop.Xolehlp.ShimNotificationType.CommittedNotify;
        ShimFactory.NewNotification(this);
    }

    public void Aborted(IntPtr pboidReason, [MarshalAs(UnmanagedType.Bool)] bool fRetaining, IntPtr pNewUOW, uint hresult)
    {
        NotificationType = Interop.Xolehlp.ShimNotificationType.AbortedNotify;
        ShimFactory.NewNotification(this);
    }

    public void HeuristicDecision([MarshalAs(UnmanagedType.U4)] Interop.Xolehlp.OletxTransactionHeuristic dwDecision, IntPtr pboidReason, uint hresult)
    {
        NotificationType = dwDecision switch {
            Interop.Xolehlp.OletxTransactionHeuristic.XACTHEURISTIC_ABORT => Interop.Xolehlp.ShimNotificationType.AbortedNotify,
            Interop.Xolehlp.OletxTransactionHeuristic.XACTHEURISTIC_COMMIT => Interop.Xolehlp.ShimNotificationType.CommittedNotify,
            _ => Interop.Xolehlp.ShimNotificationType.InDoubtNotify
        };

        ShimFactory.NewNotification(this);
    }

    public void Indoubt()
    {
        NotificationType = Interop.Xolehlp.ShimNotificationType.InDoubtNotify;
        ShimFactory.NewNotification(this);
    }
}
