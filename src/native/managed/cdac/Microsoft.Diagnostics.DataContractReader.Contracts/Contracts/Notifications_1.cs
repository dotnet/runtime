// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Notifications_1 : INotifications
{
    private readonly Target _target;

    private const int BIT_SHIFT_TYPE = 15; // Assembly Level required to be considered Loaded
    internal Notifications_1(Target target)
    {
        _target = target;
    }

    void INotifications.SetGcNotification(uint type, int condemnedGeneration)
    {
        TargetPointer pGcNotificationFlags = _target.ReadGlobalPointer(Constants.Globals.GcNotificationFlags);
        ushort currentFlags = _target.Read<ushort>(pGcNotificationFlags);
        if (type == 0)
            _target.Write<ushort>(pGcNotificationFlags, 0);
        else
        {
            _target.Write<ushort>(pGcNotificationFlags, (ushort)(currentFlags | condemnedGeneration | (1 << BIT_SHIFT_TYPE)));
        }
    }
}
