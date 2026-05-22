// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal static class GcRefEnumeration
{
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
