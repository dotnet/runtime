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

    /// <summary>
    /// Returns the <see cref="NotificationType"/> for the given exception information arguments,
    /// decoupling the public surface from the private DACNotify enumeration values.
    /// </summary>
    NotificationType GetNotificationType(ReadOnlySpan<TargetPointer> exceptionInformation) => throw new NotImplementedException();

    /// <summary>
    /// Parses a module-load notification from the exception information arguments.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments represent a valid module-load notification.</returns>
    bool TryParseModuleLoadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress) => throw new NotImplementedException();

    /// <summary>
    /// Parses a module-unload notification from the exception information arguments.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments represent a valid module-unload notification.</returns>
    bool TryParseModuleUnloadNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer moduleAddress) => throw new NotImplementedException();

    /// <summary>
    /// Parses a JIT compilation notification from the exception information arguments.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments represent a valid JIT notification.</returns>
    bool TryParseJITNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out TargetPointer nativeCodeAddress) => throw new NotImplementedException();

    /// <summary>
    /// Parses an exception notification from the exception information arguments.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments represent a valid exception notification.</returns>
    bool TryParseExceptionNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer threadAddress) => throw new NotImplementedException();

    /// <summary>
    /// Parses a GC notification from the exception information arguments.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments represent a valid GC notification.</returns>
    bool TryParseGCNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out GcEventData eventData) => throw new NotImplementedException();

    /// <summary>
    /// Parses an exception catcher-enter notification from the exception information arguments.
    /// </summary>
    /// <returns><see langword="true"/> if the arguments represent a valid exception-catcher-enter notification.</returns>
    bool TryParseExceptionCatcherEnterNotification(ReadOnlySpan<TargetPointer> exceptionInformation, out TargetPointer methodDescAddress, out uint nativeOffset) => throw new NotImplementedException();
}

public readonly struct Notifications : INotifications
{
    // Everything throws NotImplementedException
}
