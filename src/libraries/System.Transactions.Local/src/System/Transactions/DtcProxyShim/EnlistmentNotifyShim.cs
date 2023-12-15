// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.Marshalling;
using System.Threading;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

[GeneratedComClass]
internal sealed partial class EnlistmentNotifyShim : NotificationShimBase, ITransactionResourceAsync
{
    internal ITransactionEnlistmentAsync? EnlistmentAsync;

    // MSDTCPRX behaves unpredictably in that if the TM is down when we vote
    // no it will send an AbortRequest.  However if the TM does not go down
    // the enlistment is not go down the AbortRequest is not sent.  This
    // makes reliable cleanup a problem.  To work around this the enlisment
    // shim will eat the AbortRequest if it knows that it has voted No.

    // On Win2k this same problem applies to responding Committed to a
    // single phase commit request.
    private bool _ignoreSpuriousProxyNotifications;

    internal EnlistmentNotifyShim(DtcProxyShimFactory shimFactory, OletxEnlistment enlistmentIdentifier)
        : base(shimFactory, enlistmentIdentifier)
    {
        _ignoreSpuriousProxyNotifications = false;
    }

    internal void SetIgnoreSpuriousProxyNotifications()
        => _ignoreSpuriousProxyNotifications = true;

    public void PrepareRequest(bool fRetaining, OletxXactRm grfRM, bool fWantMoniker, bool fSinglePhase)
    {
        ITransactionEnlistmentAsync? pEnlistmentAsync = Interlocked.Exchange(ref EnlistmentAsync, null);

        if (pEnlistmentAsync is null)
        {
            throw new InvalidOperationException("Unexpected null in pEnlistmentAsync");
        }

        var pPrepareInfo = (IPrepareInfo)pEnlistmentAsync;
        pPrepareInfo.GetPrepareInfoSize(out uint prepareInfoLength);
        var prepareInfoBuffer = new byte[prepareInfoLength];
        pPrepareInfo.GetPrepareInfo(prepareInfoBuffer);

        PrepareInfo = prepareInfoBuffer;
        IsSinglePhase = fSinglePhase;
        NotificationType = ShimNotificationType.PrepareRequestNotify;
        ShimFactory.NewNotification(this);
    }

    public void CommitRequest(OletxXactRm grfRM, IntPtr pNewUOW)
    {
        NotificationType = ShimNotificationType.CommitRequestNotify;
        ShimFactory.NewNotification(this);
    }

    public void AbortRequest(IntPtr pboidReason, bool fRetaining, IntPtr pNewUOW)
    {
        if (!_ignoreSpuriousProxyNotifications)
        {
            // Only create the notification if we have not already voted.
            NotificationType = ShimNotificationType.AbortRequestNotify;
            ShimFactory.NewNotification(this);
        }
    }

    public void TMDown()
    {
        NotificationType = ShimNotificationType.ResourceManagerTmDownNotify;
        ShimFactory.NewNotification(this);
    }
}
