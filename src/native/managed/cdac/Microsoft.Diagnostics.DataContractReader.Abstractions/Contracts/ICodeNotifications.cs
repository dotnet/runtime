// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Kinds of JIT code notifications that can be requested for a given method.
/// The contract layer only exchanges this typed enum — COM wrappers translate
/// to/from the raw uint at the boundary.
/// </summary>
[Flags]
public enum CodeNotificationKind : uint
{
    None = 0,
    Generated = 1,
    Discarded = 2,
}

/// <summary>
/// Contract for reading and writing the JIT code notification table in the target process.
/// The table is an allowlist of (module, method token) pairs that causes the runtime to
/// raise <c>DEBUG_CODE_NOTIFICATION</c> events when the specified methods are JIT-compiled
/// or discarded.
/// </summary>
public interface ICodeNotifications : IContract
{
    static string IContract.Name { get; } = nameof(CodeNotifications);

    /// <summary>
    /// Set the notification flags for a single (module, methodToken) pair.
    /// If the in-target table has not been allocated yet, lazily allocates it when
    /// <paramref name="flags"/> is non-zero.
    /// </summary>
    void SetCodeNotification(TargetPointer module, uint methodToken, CodeNotificationKind flags) => throw new NotImplementedException();

    /// <summary>
    /// Get the notification flags for a single (module, methodToken) pair.
    /// </summary>
    CodeNotificationKind GetCodeNotification(TargetPointer module, uint methodToken) => throw new NotImplementedException();

    /// <summary>
    /// Set notification flags for all methods in a module, or all methods if module is null.
    /// </summary>
    void SetAllCodeNotifications(TargetPointer module, CodeNotificationKind flags) => throw new NotImplementedException();
}

public readonly struct CodeNotifications : ICodeNotifications
{
    // Everything throws NotImplementedException
}
