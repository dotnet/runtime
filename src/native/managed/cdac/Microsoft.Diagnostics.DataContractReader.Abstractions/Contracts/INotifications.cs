// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// The type of a CLR DAC notification event.
/// </summary>
public enum NotificationType
{
    Unknown = 0,
    ModuleLoad,
    ModuleUnload,
    Jit2,
    Exception,
    Gc,
    ExceptionCatcherEnter,
}

/// <summary>
/// The type of a GC event.
/// </summary>
public enum GcEventType
{
    MarkEnd = 1,
}

/// <summary>
/// Data associated with a GC notification event.
/// </summary>
public record struct GcEventData(GcEventType EventType, int CondemnedGeneration);

/// <summary>
/// Base type for parsed notification data. Pattern match on derived types to access notification-specific fields.
/// </summary>
public abstract record NotificationData(NotificationType Type);

public record ModuleLoadNotificationData(TargetPointer ModuleAddress)
    : NotificationData(NotificationType.ModuleLoad);

public record ModuleUnloadNotificationData(TargetPointer ModuleAddress)
    : NotificationData(NotificationType.ModuleUnload);

public record JitNotificationData(TargetPointer MethodDescAddress, TargetPointer NativeCodeAddress)
    : NotificationData(NotificationType.Jit2);

public record ExceptionNotificationData(TargetPointer ThreadAddress)
    : NotificationData(NotificationType.Exception);

public record GcNotificationData(GcEventData EventData, bool IsSupportedEvent)
    : NotificationData(NotificationType.Gc);

public record ExceptionCatcherEnterNotificationData(TargetPointer MethodDescAddress, uint NativeOffset)
    : NotificationData(NotificationType.ExceptionCatcherEnter);

public interface INotifications : IContract
{
    static string IContract.Name { get; } = nameof(Notifications);

    void SetGcNotification(int condemnedGeneration) => throw new NotImplementedException();

    bool TryParseNotification(ReadOnlySpan<TargetPointer> exceptionInformation, [NotNullWhen(true)] out NotificationData? notification) => throw new NotImplementedException();
}

public readonly struct Notifications : INotifications
{
    // Everything throws NotImplementedException
}
