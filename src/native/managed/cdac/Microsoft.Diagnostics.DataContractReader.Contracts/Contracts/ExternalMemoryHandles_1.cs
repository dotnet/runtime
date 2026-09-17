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
        TargetPointer headPointer = _target.ReadGlobalPointer(Constants.Globals.ExternalMemoryHandles);
        TargetPointer current = _target.ReadPointer(headPointer);

        HashSet<TargetPointer> visited = [];
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
            ByRefPointerOffsetsReporter reporter = new(_rts, memory, (uint)_target.PointerSize);
            foreach (ulong slotAddress in reporter.Find(typeHandle, 0))
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
        ulong invalidPointer = _target.PointerSize == 8 ? ulong.MaxValue : uint.MaxValue;
        if (obj == TargetPointer.Null || obj.Value == invalidPointer)
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
}
