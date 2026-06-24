// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ArgumentLocation
{
    public int Offset { get; init; }
    public CorElementType ElementType { get; init; }
    public ITypeHandle TypeHandle { get; init; }
    public bool IsThis { get; init; }
    public bool IsValueTypeThis { get; init; }
    public bool IsParamType { get; init; }

    // Implicit VASigCookie pointer for a vararg (__arglist) method. When set,
    // the encoder emits a VASigCookie token here and stops reporting fixed
    // arguments (the variadic tail is reported through the cookie at GC time).
    public bool IsVASigCookie { get; init; }

    // Struct passed by reference (e.g. large struct on AMD64).
    public bool IsPassedByRef { get; init; }

    // By-value ByRefLike struct (Span<T>, ReadOnlySpan<T>, ...). The encoder
    // walks instance fields for these to emit INTERIOR tokens at each managed
    // pointer slot.
    public bool IsByRefLikeStruct { get; init; }

    // For generic-instantiation parameters with an uncached closed ITypeHandle,
    // the open generic MethodTable (e.g. Span<T> for a Span<int> arg) so
    // encoders can inspect type structure as a fallback.
    public ITypeHandle OpenGenericType { get; init; }
}
