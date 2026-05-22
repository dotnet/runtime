// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

/// <summary>
/// Describes a single register or stack slot of an argument at a call site,
/// relative to the start of the transition block. A simple argument has one
/// slot; a split SystemV struct (e.g. <c>struct { object o; double d; }</c>) has
/// multiple slots — one per eightbyte's register or stack location.
/// </summary>
/// <param name="Offset">Byte offset of the slot from the start of the transition block.</param>
/// <param name="ElementType">
/// The <see cref="CorElementType"/> that describes the slot's contents (e.g.
/// <see cref="CorElementType.Class"/> for a GC ref slot, <see cref="CorElementType.R8"/>
/// for a floating-point slot). Callers (e.g. the GC scanner) classify the slot
/// from this.
/// </param>
public readonly record struct ArgSlot(
    int Offset,
    CorElementType ElementType);

/// <summary>
/// Describes the layout of a single argument at a call site, as imposed by the
/// target's managed calling convention. A simple argument has one
/// <see cref="ArgSlot"/>; a split struct has multiple.
/// </summary>
/// <param name="IsPassedByRef">
/// True if the argument is passed by implicit reference (e.g. a value type larger
/// than the ABI's enregister limit). When true, <see cref="Slots"/> contains a
/// single slot holding an interior pointer to the value.
/// </param>
/// <param name="Slots">
/// One or more register/stack slots that together carry the argument's value.
/// Always non-empty.
/// </param>
/// <param name="ValueTypeHandle">
/// Identity of the value type whose storage occupies <see cref="Slots"/> as an
/// <em>opaque, contiguous, undecomposed buffer</em>, or <see langword="null"/>
/// when the slots do not describe such a buffer.
/// <para>
/// This is layout information about what the per-arch iterator chose to do, not
/// GC information per se: the iterator surfaces the type only when it left the
/// argument's storage in a single contiguous run that the iterator did <em>not</em>
/// crack open into individually-typed <see cref="ArgSlot"/>s. Consumers that want
/// to inspect the buffer's internals (e.g. the GC scanner walking its GC
/// descriptor, a future debugger arg-formatter rendering the buffer's fields)
/// can resolve the layout via <see cref="IRuntimeTypeSystem"/>.
/// </para>
/// <para>
/// Populated when all of the following hold:
/// <list type="bullet">
///   <item><description>The argument is a value type passed <em>by value</em>
///     (not <see cref="IsPassedByRef"/>).</description></item>
///   <item><description>The per-arch iterator left the storage opaque — i.e.
///     every entry in <see cref="Slots"/> has
///     <see cref="ArgSlot.ElementType"/> == <see cref="CorElementType.ValueType"/>,
///     and together they cover one contiguous byte range starting at
///     <c>Slots[0].Offset</c>. Examples: Windows-AMD64 enregistered struct,
///     ARM64 non-HFA struct in consecutive GPRs, any stack-passed struct.</description></item>
/// </list>
/// </para>
/// <para>
/// Both ordinary value types and ByRefLike types (ref-struct / Span-style) are
/// surfaced here. Consumers select the appropriate field-enumeration strategy
/// by querying <see cref="IRuntimeTypeSystem.IsByRefLike"/>: ordinary value types
/// walk the CGCDesc series; ByRefLike types require a field-by-field walk to
/// pick up managed byref fields that the CGCDesc series does not encode.
/// </para>
/// <para>
/// <see langword="null"/> otherwise — i.e. for primitives, references, byrefs,
/// arguments passed by implicit reference (<see cref="IsPassedByRef"/>), HFAs
/// and other aggregates the iterator already decomposed into individually-typed
/// slots (e.g. SystemV split struct: one slot per eightbyte with each slot's
/// <see cref="ArgSlot.ElementType"/> reflecting that eightbyte's classification).
/// </para>
/// </param>
public readonly record struct ArgLayout(
    bool IsPassedByRef,
    IReadOnlyList<ArgSlot> Slots,
    TypeHandle? ValueTypeHandle = null);

/// <summary>
/// Describes the layout of all arguments at a call site, as imposed by the
/// target's managed calling convention. Offsets are byte offsets from the
/// start of the transition block.
/// </summary>
/// <param name="ThisOffset">
/// Byte offset of the <c>this</c> pointer slot if the method is an instance method;
/// <see langword="null"/> for static methods.
/// </param>
/// <param name="IsValueTypeThis">
/// True if <c>this</c> points at a value-type instance (i.e. the slot contains a
/// managed interior pointer). False for reference-type instance methods.
/// </param>
/// <param name="AsyncContinuationOffset">
/// Byte offset of the implicit async-continuation argument slot for async methods;
/// <see langword="null"/> if the method has no async-continuation argument.
/// </param>
/// <param name="VarArgCookieOffset">
/// Byte offset of the vararg-cookie slot for vararg methods; <see langword="null"/>
/// for non-vararg methods.
/// </param>
/// <param name="Arguments">
/// Layout of each fixed argument in declaration order. Empty when the call site
/// cannot be described (e.g. missing signature, decode failure).
/// </param>
public readonly record struct CallSiteLayout(
    int? ThisOffset,
    bool IsValueTypeThis,
    int? AsyncContinuationOffset,
    int? VarArgCookieOffset,
    IReadOnlyList<ArgLayout> Arguments);

/// <summary>
/// Computes call-site argument layouts according to the target runtime's
/// managed calling convention.
/// </summary>
public interface ICallingConvention : IContract
{
    static string IContract.Name { get; } = nameof(CallingConvention);

    /// <summary>
    /// Computes the layout of arguments at a call site for the given method.
    /// </summary>
    /// <param name="method">The method whose call site should be described.</param>
    /// <returns>
    /// The call-site layout. Returns a layout with an empty <see cref="CallSiteLayout.Arguments"/>
    /// list and null offsets if the method's call site cannot be described
    /// (missing signature, decode failure, etc.).
    /// </returns>
    CallSiteLayout ComputeCallSiteLayout(MethodDescHandle method)
        => throw new NotImplementedException();
}

public readonly struct CallingConvention : ICallingConvention
{
    // Everything throws NotImplementedException
}
