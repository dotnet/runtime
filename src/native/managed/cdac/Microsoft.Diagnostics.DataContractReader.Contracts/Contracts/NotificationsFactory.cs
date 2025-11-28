// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class NotificationsFactory : IContractFactory<INotifications>
{
    INotifications IContractFactory<INotifications>.CreateContract(Target target, int version)
    {
        return version switch
        {
            1 => new Notifications_1(target),
            _ => default(Notifications),
        };
    }
}
