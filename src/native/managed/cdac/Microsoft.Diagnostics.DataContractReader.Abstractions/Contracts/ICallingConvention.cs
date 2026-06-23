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

    /// <summary>
    /// True if this argument is a by-value ByRefLike struct (Span&lt;T&gt;,
    /// ReadOnlySpan&lt;T&gt;, etc.). The runtime's
    /// <c>ReportPointersFromValueType</c> walks a <c>ByRefPointerOffsetsReporter</c>
    /// for these to emit INTERIOR tokens at each managed-pointer slot inside the
    /// struct, separate from the GCDesc-driven REF emission.
    /// </summary>
    public bool IsByRefLikeStruct { get; init; }

    /// <summary>
    /// For generic-instantiation arguments whose closed
    /// <see cref="TypeHandle"/> is null (uncached), this carries the open
    /// generic <c>MethodTable</c> (e.g. <c>Span&lt;T&gt;</c> for a
    /// <c>Span&lt;int&gt;</c> arg). Encoders that need to inspect the type's
    /// structure (e.g. walk its instance fields to find <c>byref</c> fields
    /// for ByRefLike-struct INTERIOR emission) can fall back to this when
    /// <see cref="TypeHandle"/> isn't resolvable.
    /// </summary>
    public TypeHandle OpenGenericType { get; init; }
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

    /// <summary>
    /// Compute the argument GCRefMap blob for the given method in the same wire
    /// format as the runtime's <c>ComputeCallRefMap</c> (frames.cpp). Returns
    /// <c>null</c> for any method this contract cannot yet encode (e.g. x86 layout,
    /// by-value structs containing GC pointers); the caller treats <c>null</c> as
    /// <c>E_NOTIMPL</c> for the cdacstress ArgIterator sub-check.
    /// </summary>
    byte[]? TryComputeArgGCRefMapBlob(MethodDescHandle methodDesc) => null;

    /// <summary>
    /// Return the number of bytes the callee pops off the stack on return,
    /// for use as the x86 GCRefMap WriteStackPop prefix. Returns 0 on
    /// non-x86 architectures (or VarArgs methods). Used by the cdacstress
    /// ArgIterator sub-check.
    /// </summary>
    uint GetCbStackPop(MethodDescHandle methodDesc) => 0;
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
