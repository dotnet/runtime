// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Notifications_1 : INotifications
{
    private enum NotificationType_1 : uint
    {
        ModuleLoad = 1,
        ModuleUnload = 2,
        Exception = 5,
        Gc = 6,
        ExceptionCatcherEnter = 7,
        Jit2 = 8,
    }

    private readonly Target _target;

    internal Notifications_1(Target target)
    {
        _target = target;
    }

    void INotifications.SetGcNotification(int condemnedGeneration)
    {
        TargetPointer pGcNotificationFlags = _target.ReadGlobalPointer(Constants.Globals.GcNotificationFlags);
        uint currentFlags = _target.Read<uint>(pGcNotificationFlags);
        if (condemnedGeneration == 0)
            _target.Write<uint>(pGcNotificationFlags, 0);
        else
        {
            _target.Write<uint>(pGcNotificationFlags, currentFlags | (uint)condemnedGeneration);
        }
    }

    bool INotifications.TryParseNotification(ReadOnlySpan<TargetPointer> exceptionInformation, [NotNullWhen(true)] out NotificationData? notification)
    {
        notification = null;

        if (exceptionInformation.IsEmpty)
            return false;

        notification = (NotificationType_1)(uint)exceptionInformation[0].Value switch
        {
            NotificationType_1.ModuleLoad => new ModuleLoadNotificationData(exceptionInformation[1]),
            NotificationType_1.ModuleUnload => new ModuleUnloadNotificationData(exceptionInformation[1]),
            NotificationType_1.Jit2 => new JitNotificationData(exceptionInformation[1], exceptionInformation[2]),
            NotificationType_1.Exception => new ExceptionNotificationData(exceptionInformation[1]),
            NotificationType_1.Gc => ParseGcNotification(exceptionInformation),
            NotificationType_1.ExceptionCatcherEnter => new ExceptionCatcherEnterNotificationData(exceptionInformation[1], (uint)exceptionInformation[2].Value),
            _ => null,
        };

        return notification is not null;
    }

    private static GcNotificationData ParseGcNotification(ReadOnlySpan<TargetPointer> exceptionInformation)
    {
        GcEventType eventType = (GcEventType)(uint)exceptionInformation[1].Value;
        GcEventData eventData = new(eventType, (int)(uint)exceptionInformation[2].Value);
        return new GcNotificationData(eventData, IsSupportedEvent: eventType == GcEventType.MarkEnd);
    }
}
