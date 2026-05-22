// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Helpers for enumerating embedded managed references inside an unboxed value-type
/// instance. Mirrors native <c>ReportPointersFromValueType</c>
/// (<c>src/coreclr/vm/siginfo.cpp</c>): a CGCDesc series walk with the boxed-to-unboxed
/// offset adjustment subtracted out.
/// </summary>
/// <remarks>
/// Callers are responsible for ensuring <paramref name="valueType"/> is an ordinary
/// (non-ByRefLike) value type when calling <see cref="EnumerateValueTypeRefs"/>.
/// ByRefLike types (<c>Span&lt;T&gt;</c>, ref structs) can carry byref-typed fields at
/// arbitrary offsets which the CGCDesc series does not describe; use
/// <see cref="EnumerateByRefLikeRoots"/> for those.
/// </remarks>
internal static class GcRefEnumeration
{
    /// <summary>
    /// Yields the address of every managed reference embedded inside an unboxed
    /// value-type instance located at <paramref name="baseAddress"/>.
    /// </summary>
    /// <param name="rts">Runtime type system contract, used to read the CGCDesc series.</param>
    /// <param name="valueType">The value type whose layout describes the unboxed instance.</param>
    /// <param name="baseAddress">Address of the start of the unboxed instance (the field area).</param>
    /// <param name="pointerSize">Target pointer size in bytes (4 or 8).</param>
    /// <remarks>
    /// <para>
    /// <see cref="IRuntimeTypeSystem.GetGCDescSeries"/> returns offsets measured from the start
    /// of a <em>boxed</em> object (i.e. including the <c>MethodTable*</c> prefix). For an
    /// unboxed instance the same field sits <paramref name="pointerSize"/> bytes earlier, so
    /// we subtract <paramref name="pointerSize"/> from each series offset. This matches the
    /// native adjustment in <c>ReportPointersFromValueType</c>.
    /// </para>
    /// <para>
    /// References are emitted in series order (i.e. the order the runtime stored them in
    /// the GCDesc); no deduplication or sorting is performed. <c>numComponents</c> is fixed
    /// at 0 because value-type arguments are never arrays.
    /// </para>
    /// </remarks>
    public static IEnumerable<TargetPointer> EnumerateValueTypeRefs(
        IRuntimeTypeSystem rts,
        TypeHandle valueType,
        TargetPointer baseAddress,
        int pointerSize)
    {
        foreach ((uint seriesOffset, uint seriesSize) in rts.GetGCDescSeries(valueType, numComponents: 0))
        {
            // Convert the boxed offset (relative to the MethodTable* slot) to an unboxed
            // offset (relative to the field area). Equivalent to
            //   cur->GetSeriesOffset() - TARGET_POINTER_SIZE
            // in native ReportPointersFromValueType.
            ulong unboxedOffset = seriesOffset - (ulong)pointerSize;
            ulong refCount = seriesSize / (ulong)pointerSize;
            for (ulong i = 0; i < refCount; i++)
            {
                yield return new TargetPointer(baseAddress.Value + unboxedOffset + i * (ulong)pointerSize);
            }
        }
    }

    /// <summary>
    /// Yields the GC roots embedded inside an unboxed ByRefLike value-type instance
    /// (a <c>Span&lt;T&gt;</c>, <c>ReadOnlySpan&lt;T&gt;</c>, or other ref struct)
    /// located at <paramref name="baseAddress"/>.
    /// </summary>
    /// <param name="rts">Runtime type system contract, used to walk the type's fields.</param>
    /// <param name="byRefLikeType">The ByRefLike value type whose layout describes the instance.</param>
    /// <param name="baseAddress">Address of the start of the unboxed instance (the field area).</param>
    /// <param name="pointerSize">Target pointer size in bytes (4 or 8). Used for nested non-ByRefLike value-type recursion.</param>
    /// <remarks>
    /// <para>
    /// Mirrors native <c>MetaSig::ReportPointersFromValueTypeArg</c> /
    /// <c>ByRefPointerOffsetsReporter</c> in <c>siginfo.cpp</c>: ByRefLike types have
    /// no usable CGCDesc series (interior byrefs are not encoded there), so we walk the
    /// declared instance fields and emit one root per ref/byref field. Object refs are
    /// emitted with <see cref="GcScanFlags.None"/>; managed byrefs are emitted with
    /// <see cref="GcScanFlags.GC_CALL_INTERIOR"/>.
    /// </para>
    /// <para>
    /// Nested aggregate fields are handled compositionally: a nested ByRefLike field
    /// recurses through this method; a nested non-ByRefLike value-type field delegates
    /// to <see cref="EnumerateValueTypeRefs"/> for the standard CGCDesc walk.
    /// </para>
    /// <para>
    /// Primitives, raw pointers (<see cref="CorElementType.Ptr"/>), and function pointers
    /// (<see cref="CorElementType.FnPtr"/>) are not GC-relevant and yield nothing. If a
    /// nested value-type field's <see cref="TypeHandle"/> can't be resolved (e.g. the
    /// enclosing module's metadata is unavailable), that field is skipped — matching the
    /// native DAC's conservative behavior.
    /// </para>
    /// </remarks>
    public static IEnumerable<(TargetPointer Address, GcScanFlags Flags)> EnumerateByRefLikeRoots(
        IRuntimeTypeSystem rts,
        TypeHandle byRefLikeType,
        TargetPointer baseAddress,
        int pointerSize)
    {
        foreach (TargetPointer fd in rts.EnumerateInstanceFieldDescs(byRefLikeType))
        {
            // For instance fields the BigRVA sentinel path is never taken, so passing
            // a default FieldDefinition is safe.
            uint offset = rts.GetFieldDescOffset(fd, default);
            TargetPointer fieldAddr = new(baseAddress.Value + offset);
            CorElementType et = rts.GetFieldDescType(fd);

            switch (et)
            {
                case CorElementType.Class:
                case CorElementType.String:
                case CorElementType.Object:
                case CorElementType.SzArray:
                case CorElementType.Array:
                    yield return (fieldAddr, GcScanFlags.None);
                    break;

                case CorElementType.Byref:
                    yield return (fieldAddr, GcScanFlags.GC_CALL_INTERIOR);
                    break;

                case CorElementType.ValueType:
                {
                    TypeHandle inner = rts.LookupApproxFieldTypeHandle(fd);
                    if (!inner.IsMethodTable())
                        break;

                    if (rts.IsByRefLike(inner))
                    {
                        foreach ((TargetPointer addr, GcScanFlags flags) in
                                 EnumerateByRefLikeRoots(rts, inner, fieldAddr, pointerSize))
                        {
                            yield return (addr, flags);
                        }
                    }
                    else
                    {
                        foreach (TargetPointer refAddr in
                                 EnumerateValueTypeRefs(rts, inner, fieldAddr, pointerSize))
                        {
                            yield return (refAddr, GcScanFlags.None);
                        }
                    }
                    break;
                }

                // Primitives, raw pointers, function pointers, etc. carry no GC refs.
                default:
                    break;
            }
        }
    }
}
