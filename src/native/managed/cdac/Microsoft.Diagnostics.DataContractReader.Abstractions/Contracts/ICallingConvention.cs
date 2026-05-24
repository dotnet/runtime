// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Describes the location of an argument on a caller's transition frame,
/// produced by walking the callee's method signature with ArgIterator.
/// </summary>
public readonly struct ArgumentLocation
{
    /// <summary>Byte offset from the start of the transition block.</summary>
    public int Offset { get; init; }

    /// <summary>The CorElementType of this argument (Class, ValueType, Byref, I4, etc.).</summary>
    public CorElementType ElementType { get; init; }

    /// <summary>The TypeHandle for this argument's type (needed for struct GC walking).</summary>
    public TypeHandle TypeHandle { get; init; }

    /// <summary>True if this is the "this" pointer slot.</summary>
    public bool IsThis { get; init; }

    /// <summary>True if this is a value type "this" (passed as interior pointer).</summary>
    public bool IsValueTypeThis { get; init; }

    /// <summary>True if this slot holds a generic instantiation parameter (MethodTable* or MethodDesc*).</summary>
    public bool IsParamType { get; init; }

    /// <summary>True if this argument is a struct passed by reference (e.g., large struct on AMD64).</summary>
    public bool IsPassedByRef { get; init; }
}

public interface ICallingConvention : IContract
{
    static string IContract.Name => nameof(CallingConvention);

    /// <summary>
    /// Enumerate argument locations on the caller's transition frame for the given method.
    /// This uses the shared ArgIterator to walk the method signature and determine
    /// where each argument resides (stack offset, element type, type handle).
    /// The caller is responsible for interpreting these locations for GC or other purposes.
    /// </summary>
    IEnumerable<ArgumentLocation> EnumerateArguments(MethodDescHandle methodDesc) => throw new System.NotImplementedException();
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
