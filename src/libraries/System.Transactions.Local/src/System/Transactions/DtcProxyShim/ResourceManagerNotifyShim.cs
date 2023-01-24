// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Transactions.Oletx;
using System.Transactions.DtcProxyShim.DtcInterfaces;

namespace System.Transactions.DtcProxyShim;

internal sealed class ResourceManagerNotifyShim : NotificationShimBase, IResourceManagerSink
{
    internal ResourceManagerNotifyShim(
        DtcProxyShimFactory shimFactory,
        object enlistmentIdentifier)
        : base(shimFactory, enlistmentIdentifier)
    {
    }

    public void TMDown()
    {
        NotificationType = ShimNotificationType.ResourceManagerTmDownNotify;
        ShimFactory.NewNotification(this);
    }
}
