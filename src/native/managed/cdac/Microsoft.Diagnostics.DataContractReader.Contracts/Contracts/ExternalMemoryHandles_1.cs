// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class ExternalMemoryHandles_1 : IExternalMemoryHandles
{
    private readonly Target _target;
    private readonly IGC _gc;
    private readonly IRuntimeTypeSystem _rts;
    private readonly GCInteriorPointerResolver _interiorPointerResolver;

    internal ExternalMemoryHandles_1(Target target)
    {
        _target = target;
        _gc = target.Contracts.GC;
        _rts = target.Contracts.RuntimeTypeSystem;
        _interiorPointerResolver = new GCInteriorPointerResolver(target, _gc, _rts);
    }

    IReadOnlyList<ExternalMemoryHandleRootData> IExternalMemoryHandles.GetRoots(bool resolveInteriorPointers)
    {
        List<ExternalMemoryHandleRootData> roots = [];
        TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
        TargetPointer appDomain = _target.ReadPointer(appDomainPointer);
        if (appDomain == TargetPointer.Null)
            return roots;

        Data.AppDomain domain = _target.ProcessedData.GetOrAdd<Data.AppDomain>(appDomain);

        HashSet<TargetPointer> visited = [];
        TargetPointer current = domain.ExternalMemoryHandles;
        while (current != TargetPointer.Null)
        {
            if (!visited.Add(current))
                throw new InvalidOperationException("ExternalMemoryHandle list is cyclic.");

            Data.ExternalMemoryHandle handle = _target.ProcessedData.GetOrAdd<Data.ExternalMemoryHandle>(current);
            ITypeHandle typeHandle = _rts.GetTypeHandle(handle.MethodTable);

            if (_rts.IsValueType(typeHandle))
            {
                AddValueTypeRoots(roots, typeHandle, handle.Memory, resolveInteriorPointers);
            }
            else if (handle.GCFlags != 0)
            {
                AddInteriorRoot(roots, handle.Memory, resolveInteriorPointers);
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
        ITypeHandle typeHandle,
        TargetPointer memory,
        bool resolveInteriorPointers)
    {
        ulong pointerSize = (ulong)_target.PointerSize;

        if (_rts.IsByRefLike(typeHandle))
        {
            foreach (ulong slotAddress in EnumerateByRefLikeInteriorSlots(_rts, typeHandle, memory.Value, (uint)_target.PointerSize))
                AddInteriorRoot(roots, new TargetPointer(slotAddress), resolveInteriorPointers);
        }

        if (_rts.ContainsGCPointers(typeHandle))
        {
            foreach ((uint seriesOffset, uint seriesSize) in _rts.GetGCDescSeries(typeHandle))
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
        bool resolveInteriorPointers)
    {
        TargetPointer obj = _target.ReadPointer(slotAddress.Value);
        if (obj == TargetPointer.Null || obj.Value == ulong.MaxValue)
            return;

        if (resolveInteriorPointers)
        {
            obj = _interiorPointerResolver.Resolve(obj);
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
