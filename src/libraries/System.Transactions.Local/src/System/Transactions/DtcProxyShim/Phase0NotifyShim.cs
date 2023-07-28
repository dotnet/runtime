// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices.Marshalling;
using System.Transactions.DtcProxyShim.DtcInterfaces;
using System.Transactions.Oletx;

namespace System.Transactions.DtcProxyShim;

[GeneratedComClass]
internal sealed partial class Phase0NotifyShim : NotificationShimBase, ITransactionPhase0NotifyAsync
{
    internal Phase0NotifyShim(DtcProxyShimFactory shimFactory, object enlistmentIdentifier)
        : base(shimFactory, enlistmentIdentifier)
    {
    }

    public void Phase0Request(bool fAbortHint)
    {
        AbortingHint = fAbortHint;
        NotificationType = ShimNotificationType.Phase0RequestNotify;
        ShimFactory.NewNotification(this);
    }

    public void EnlistCompleted(int status)
    {
        // We don't care about these. The managed code waited for the enlistment to be completed.
    }
}
