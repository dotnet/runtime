// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// The type of a CLR DAC notification event.
/// </summary>
public enum NotificationType
{
    Unknown = 0,
    ModuleLoad,
    ModuleUnload,
    Jit,
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

public interface INotifications : IContract
{
    static string IContract.Name { get; } = nameof(Notifications);

    void SetGcNotification(int condemnedGeneration) => throw new NotImplementedException();

    NotificationType GetNotificationType(ReadOnlySpan<TargetPointer> exceptionInformation) => throw new NotImplementedException();

    void ParseModuleLoadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress) => throw new NotImplementedException();

    void ParseModuleUnloadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress) => throw new NotImplementedException();

    void ParseJITNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress) => throw new NotImplementedException();

    void ParseExceptionNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer threadAddress) => throw new NotImplementedException();

    void ParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData) => throw new NotImplementedException();

    void ParseExceptionCatcherEnterNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out uint nativeOffset) => throw new NotImplementedException();
}

public readonly struct Notifications : INotifications
{
    // Everything throws NotImplementedException
}
