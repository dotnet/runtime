// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Describes a GC reference slot on a caller's transition frame,
/// produced by walking the callee's method signature with ArgIterator.
/// </summary>
public readonly struct CallerStackGCRef
{
    /// <summary>Byte offset from the start of the transition block.</summary>
    public int Offset { get; init; }

    /// <summary>True if this is an interior pointer (byref); false for a normal object reference.</summary>
    public bool IsInterior { get; init; }

    /// <summary>True if this is the "this" pointer slot.</summary>
    public bool IsThis { get; init; }

    /// <summary>True if this slot holds a generic instantiation parameter (MethodTable* or MethodDesc*).</summary>
    public bool IsParamType { get; init; }

    /// <summary>True if this is a pinned reference.</summary>
    public bool IsPinned { get; init; }
}

public interface ICallingConvention : IContract
{
    static string IContract.Name => nameof(CallingConvention);

    /// <summary>
    /// Enumerate GC reference slots on the caller's transition frame for the given method.
    /// This uses the shared ArgIterator to walk the method signature and determine
    /// which stack/register slots hold GC references.
    /// </summary>
    IEnumerable<CallerStackGCRef> EnumerateCallerStackRefs(MethodDescHandle methodDesc) => throw new System.NotImplementedException();
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
