// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct ExternalMemoryHandles_1 : IExternalMemoryHandles
{
    private readonly Target _target;

    internal ExternalMemoryHandles_1(Target target)
    {
        _target = target;
    }

    IReadOnlyList<ExternalMemoryHandleRootData> IExternalMemoryHandles.GetRoots(bool resolveInteriorPointers)
    {
        List<ExternalMemoryHandleRootData> roots = [];
        TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
        TargetPointer appDomain = _target.ReadPointer(appDomainPointer);
        if (appDomain == TargetPointer.Null)
            return roots;

        Data.AppDomain domain = _target.ProcessedData.GetOrAdd<Data.AppDomain>(appDomain);
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        GCInteriorPointerResolver? interiorPointerResolver = null;

        HashSet<TargetPointer> visited = [];
        TargetPointer current = domain.ExternalMemoryHandles;
        while (current != TargetPointer.Null)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException("ExternalMemoryHandle list is cyclic.");

            Data.ExternalMemoryHandle handle = _target.ProcessedData.GetOrAdd<Data.ExternalMemoryHandle>(current);
            ITypeHandle typeHandle = rts.GetTypeHandle(handle.MethodTable);

            if (rts.IsValueType(typeHandle))
            {
                AddValueTypeRoots(roots, rts, typeHandle, handle.Memory, resolveInteriorPointers, ref interiorPointerResolver);
            }
            else if (handle.GCFlags != 0)
            {
                AddInteriorRoot(roots, handle.Memory, resolveInteriorPointers, ref interiorPointerResolver);
            }
            else
            {
                roots.Add(new ExternalMemoryHandleRootData { Address = handle.Memory });
            }

            current = handle.Next;
        }

        return roots;
    }

    private void AddValueTypeRoots(
        List<ExternalMemoryHandleRootData> roots,
        IRuntimeTypeSystem rts,
        ITypeHandle typeHandle,
        TargetPointer memory,
        bool resolveInteriorPointers,
        ref GCInteriorPointerResolver? interiorPointerResolver)
    {
        ulong pointerSize = (ulong)_target.PointerSize;

        if (rts.IsByRefLike(typeHandle))
        {
            foreach (ulong slotAddress in EnumerateByRefLikeInteriorSlots(rts, typeHandle, memory.Value, (uint)_target.PointerSize))
                AddInteriorRoot(roots, new TargetPointer(slotAddress), resolveInteriorPointers, ref interiorPointerResolver);
        }

        if (rts.ContainsGCPointers(typeHandle))
        {
            foreach ((uint seriesOffset, uint seriesSize) in rts.GetGCDescSeries(typeHandle))
            {
                // GCDesc series offsets include the boxed object's MethodTable pointer.
                ulong fieldStart = memory.Value + seriesOffset - pointerSize;
                for (ulong suboffset = 0; suboffset < seriesSize; suboffset += pointerSize)
                    roots.Add(new ExternalMemoryHandleRootData { Address = new TargetPointer(fieldStart + suboffset) });
            }
        }
    }

    private void AddInteriorRoot(
        List<ExternalMemoryHandleRootData> roots,
        TargetPointer slotAddress,
        bool resolveInteriorPointers,
        ref GCInteriorPointerResolver? interiorPointerResolver)
    {
        TargetPointer obj = _target.ReadPointer(slotAddress.Value);
        if (obj == TargetPointer.Null || obj.Value == ulong.MaxValue)
            return;

        if (resolveInteriorPointers)
        {
            interiorPointerResolver ??= new GCInteriorPointerResolver(_target);
            obj = interiorPointerResolver.Resolve(obj);
            if (obj == TargetPointer.Null)
                return;
        }

        roots.Add(new ExternalMemoryHandleRootData
        {
            IsInteriorPointer = true,
            Address = slotAddress,
            Object = obj,
        });
    }

    private static IEnumerable<ulong> EnumerateByRefLikeInteriorSlots(
        IRuntimeTypeSystem rts,
        ITypeHandle typeHandle,
        ulong baseAddress,
        uint pointerSize)
    {
        bool isInlineArray = rts.IsInlineArray(typeHandle);

        foreach (TargetPointer fieldDesc in rts.GetFieldDescList(typeHandle))
        {
            if (rts.IsFieldDescStatic(fieldDesc))
                continue;

            CorElementType fieldType = rts.GetFieldDescType(fieldDesc);
            ITypeHandle? fieldTypeHandle = fieldType == CorElementType.ValueType
                ? rts.GetFieldDescApproxTypeHandle(fieldDesc)
                : null;

            if (isInlineArray)
            {
                uint elementSize = fieldType == CorElementType.Byref
                    ? pointerSize
                    : fieldTypeHandle is not null ? rts.GetNumInstanceFieldBytes(fieldTypeHandle) : 0;
                if (elementSize == 0)
                    continue;

                uint totalSize = rts.GetNumInstanceFieldBytes(typeHandle);
                for (uint elementOffset = 0; elementOffset < totalSize; elementOffset += elementSize)
                {
                    foreach (ulong slot in EnumerateByRefLikeFieldSlots(rts, fieldType, fieldTypeHandle, baseAddress + elementOffset, pointerSize))
                        yield return slot;
                }
            }
            else
            {
                uint fieldOffset = rts.GetFieldDescOffset(fieldDesc, fieldDef: null);
                foreach (ulong slot in EnumerateByRefLikeFieldSlots(rts, fieldType, fieldTypeHandle, baseAddress + fieldOffset, pointerSize))
                    yield return slot;
            }
        }
    }

    private static IEnumerable<ulong> EnumerateByRefLikeFieldSlots(
        IRuntimeTypeSystem rts,
        CorElementType fieldType,
        ITypeHandle? fieldTypeHandle,
        ulong fieldAddress,
        uint pointerSize)
    {
        if (fieldType == CorElementType.Byref)
        {
            yield return fieldAddress;
        }
        else if (fieldType == CorElementType.ValueType && fieldTypeHandle is not null && rts.IsByRefLike(fieldTypeHandle))
        {
            foreach (ulong slot in EnumerateByRefLikeInteriorSlots(rts, fieldTypeHandle, fieldAddress, pointerSize))
                yield return slot;
        }
    }
}
