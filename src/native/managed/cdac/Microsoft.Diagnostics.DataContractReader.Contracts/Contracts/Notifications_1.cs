// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Notifications_1 : INotifications
{
    private enum NotificationType_1 : uint
    {
        ModuleLoad = 1,
        ModuleUnload = 2,
        // Jit = 3 (JIT_NOTIFICATION, legacy - not handled)
        // 4 unused
        Exception = 5,
        Gc = 6,
        ExceptionCatcherEnter = 7,
        Jit2 = 8, // JIT_NOTIFICATION2 - the active JIT notification type
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

    NotificationType INotifications.GetNotificationType(ReadOnlySpan<TargetPointer> exceptionInformation)
    {
        if (exceptionInformation.IsEmpty)
            return NotificationType.Unknown;

        return (NotificationType_1)(uint)exceptionInformation[0].Value switch
        {
            NotificationType_1.ModuleLoad => NotificationType.ModuleLoad,
            NotificationType_1.ModuleUnload => NotificationType.ModuleUnload,
            NotificationType_1.Jit2 => NotificationType.Jit2,
            NotificationType_1.Exception => NotificationType.Exception,
            NotificationType_1.Gc => NotificationType.Gc,
            NotificationType_1.ExceptionCatcherEnter => NotificationType.ExceptionCatcherEnter,
            _ => NotificationType.Unknown,
        };
    }

    void INotifications.ParseModuleLoadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress)
    {
        moduleAddress = exceptionInformation[1];
    }

    void INotifications.ParseModuleUnloadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress)
    {
        moduleAddress = exceptionInformation[1];
    }

    void INotifications.ParseJITNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress)
    {
        methodDescAddress = exceptionInformation[1];
        nativeCodeAddress = exceptionInformation[2];
    }

    void INotifications.ParseExceptionNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer threadAddress)
    {
        threadAddress = exceptionInformation[1];
    }

    bool INotifications.ParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData)
    {
        GcEventType eventType = (GcEventType)(uint)exceptionInformation[1].Value;
        eventData = new GcEventData(eventType, (int)(uint)exceptionInformation[2].Value);
        return eventType == GcEventType.MarkEnd;
    }

    void INotifications.ParseExceptionCatcherEnterNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out uint nativeOffset)
    {
        methodDescAddress = exceptionInformation[1];
        nativeOffset = (uint)exceptionInformation[2].Value;
    }
}
