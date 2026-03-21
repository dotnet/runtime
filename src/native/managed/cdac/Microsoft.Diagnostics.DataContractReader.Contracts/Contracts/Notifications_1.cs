// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Notifications_1 : INotifications
{
    // Internal representation of DACNotify notification types from src/coreclr/vm/util.hpp.
    // These values must not be exposed publicly to decouple the public surface from the
    // private runtime implementation.
    private enum DacNotificationType : ulong
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

        return (DacNotificationType)exceptionInformation[0].Value switch
        {
            DacNotificationType.ModuleLoad => NotificationType.ModuleLoad,
            DacNotificationType.ModuleUnload => NotificationType.ModuleUnload,
            DacNotificationType.Jit2 => NotificationType.Jit,
            DacNotificationType.Exception => NotificationType.Exception,
            DacNotificationType.Gc => NotificationType.Gc,
            DacNotificationType.ExceptionCatcherEnter => NotificationType.ExceptionCatcherEnter,
            _ => NotificationType.Unknown,
        };
    }

    bool INotifications.TryParseModuleLoadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress)
    {
        moduleAddress = TargetPointer.Null;
        if (exceptionInformation.Length < 2 || (DacNotificationType)exceptionInformation[0].Value != DacNotificationType.ModuleLoad)
            return false;

        moduleAddress = exceptionInformation[1];
        return true;
    }

    bool INotifications.TryParseModuleUnloadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress)
    {
        moduleAddress = TargetPointer.Null;
        if (exceptionInformation.Length < 2 || (DacNotificationType)exceptionInformation[0].Value != DacNotificationType.ModuleUnload)
            return false;

        moduleAddress = exceptionInformation[1];
        return true;
    }

    bool INotifications.TryParseJITNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress)
    {
        methodDescAddress = TargetPointer.Null;
        nativeCodeAddress = TargetPointer.Null;
        if (exceptionInformation.Length < 3 || (DacNotificationType)exceptionInformation[0].Value != DacNotificationType.Jit2)
            return false;

        methodDescAddress = exceptionInformation[1];
        nativeCodeAddress = exceptionInformation[2];
        return true;
    }

    bool INotifications.TryParseExceptionNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer threadAddress)
    {
        threadAddress = TargetPointer.Null;
        if (exceptionInformation.Length < 2 || (DacNotificationType)exceptionInformation[0].Value != DacNotificationType.Exception)
            return false;

        threadAddress = exceptionInformation[1];
        return true;
    }

    bool INotifications.TryParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData)
    {
        eventData = default;
        if (exceptionInformation.Length < 3 || (DacNotificationType)exceptionInformation[0].Value != DacNotificationType.Gc)
            return false;

        GcEventType eventType = (GcEventType)(uint)exceptionInformation[1].Value;
        switch (eventType)
        {
            case GcEventType.MarkEnd:
                eventData = new GcEventData(eventType, (int)(uint)exceptionInformation[2].Value);
                return true;
            default:
                return false;
        }
    }

    bool INotifications.TryParseExceptionCatcherEnterNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out uint nativeOffset)
    {
        methodDescAddress = TargetPointer.Null;
        nativeOffset = 0;
        if (exceptionInformation.Length < 3 || (DacNotificationType)exceptionInformation[0].Value != DacNotificationType.ExceptionCatcherEnter)
            return false;

        methodDescAddress = exceptionInformation[1];
        nativeOffset = (uint)exceptionInformation[2].Value;
        return true;
    }
}
