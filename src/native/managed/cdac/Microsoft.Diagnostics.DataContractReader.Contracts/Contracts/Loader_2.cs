// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Loader_2 : ILoader
{
    private readonly Target _target;
    private readonly Loader_1 _v1;

    internal Loader_2(Target target)
    {
        _target = target;
        _v1 = new Loader_1(target);
    }

    IReadOnlyList<ExternalMemoryHandleRootData> ILoader.GetExternalMemoryHandleRoots(bool resolveInteriorPointers)
    {
        List<ExternalMemoryHandleRootData> roots = [];
        TargetPointer appDomain = ((ILoader)_v1).GetAppDomain();
        if (appDomain == TargetPointer.Null)
            return roots;

        Data.AppDomain domain = _target.ProcessedData.GetOrAdd<Data.AppDomain>(appDomain);
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        GCInteriorPointerResolver? interiorPointerResolver = null;

        HashSet<TargetPointer> visited = new();
        TargetPointer current = domain.ExternalMemoryHandles;
        while (current != TargetPointer.Null)
        {
            if (!visited.Add(current))
            {
                // Defend against a corrupted/cyclic list rather than looping forever.
                throw new InvalidOperationException("ExternalMemoryHandle list is cyclic.");
            }

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

    ModuleHandle ILoader.GetModuleHandleFromModulePtr(TargetPointer modulePointer) => ((ILoader)_v1).GetModuleHandleFromModulePtr(modulePointer);
    ModuleHandle ILoader.GetModuleHandleFromAssemblyPtr(TargetPointer assemblyPointer) => ((ILoader)_v1).GetModuleHandleFromAssemblyPtr(assemblyPointer);
    IEnumerable<ModuleHandle> ILoader.GetModuleHandles(TargetPointer appDomain, AssemblyIterationFlags iterationFlags) => ((ILoader)_v1).GetModuleHandles(appDomain, iterationFlags);
    TargetPointer ILoader.GetRootAssembly() => ((ILoader)_v1).GetRootAssembly();
    string ILoader.GetAppDomainFriendlyName() => ((ILoader)_v1).GetAppDomainFriendlyName();
    TargetPointer ILoader.GetAppDomain() => ((ILoader)_v1).GetAppDomain();
    TargetPointer ILoader.GetModule(ModuleHandle handle) => ((ILoader)_v1).GetModule(handle);
    TargetPointer ILoader.GetAssembly(ModuleHandle handle) => ((ILoader)_v1).GetAssembly(handle);
    TargetPointer ILoader.GetPEAssembly(ModuleHandle handle) => ((ILoader)_v1).GetPEAssembly(handle);
    bool ILoader.TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags) => ((ILoader)_v1).TryGetLoadedImageContents(handle, out baseAddress, out size, out imageFlags);
    TargetPointer ILoader.GetILAddr(TargetPointer peAssemblyPtr, int rva) => ((ILoader)_v1).GetILAddr(peAssemblyPtr, rva);
    TargetPointer ILoader.GetFieldAddressFromRva(TargetPointer peAssemblyPtr, int rva) => ((ILoader)_v1).GetFieldAddressFromRva(peAssemblyPtr, rva);
    bool ILoader.TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size) => ((ILoader)_v1).TryGetSymbolStream(handle, out buffer, out size);
    IEnumerable<TargetPointer> ILoader.GetAvailableTypeParams(ModuleHandle handle) => ((ILoader)_v1).GetAvailableTypeParams(handle);
    IEnumerable<TargetPointer> ILoader.GetInstantiatedMethods(ModuleHandle handle) => ((ILoader)_v1).GetInstantiatedMethods(handle);
    bool ILoader.IsProbeExtensionResultValid(ModuleHandle handle) => ((ILoader)_v1).IsProbeExtensionResultValid(handle);
    ModuleFlags ILoader.GetFlags(ModuleHandle handle) => ((ILoader)_v1).GetFlags(handle);
    bool ILoader.IsReadyToRun(ModuleHandle handle) => ((ILoader)_v1).IsReadyToRun(handle);
    string ILoader.GetSimpleName(ModuleHandle handle) => ((ILoader)_v1).GetSimpleName(handle);
    string ILoader.GetPath(ModuleHandle handle) => ((ILoader)_v1).GetPath(handle);
    string ILoader.GetFileName(ModuleHandle handle) => ((ILoader)_v1).GetFileName(handle);
    bool ILoader.GetFileHeadersInfo(ModuleHandle handle, out uint timeStamp, out uint imageSize) => ((ILoader)_v1).GetFileHeadersInfo(handle, out timeStamp, out imageSize);
    TargetPointer ILoader.GetLoaderAllocator(ModuleHandle handle) => ((ILoader)_v1).GetLoaderAllocator(handle);
    TargetPointer ILoader.GetILBase(ModuleHandle handle) => ((ILoader)_v1).GetILBase(handle);
    TargetPointer ILoader.GetAssemblyLoadContext(ModuleHandle handle) => ((ILoader)_v1).GetAssemblyLoadContext(handle);
    TargetPointer ILoader.GetModuleLookupMapBase(ModuleHandle module, ModuleLookupMapKind kind) => ((ILoader)_v1).GetModuleLookupMapBase(module, kind);
    TargetPointer ILoader.GetModuleLookupMapElement(ModuleHandle module, ModuleLookupMapKind kind, uint token, out TargetNUInt flags) => ((ILoader)_v1).GetModuleLookupMapElement(module, kind, token, out flags);
    TargetPointer ILoader.LookupMemberRefAsMethod(ModuleHandle handle, uint token) => ((ILoader)_v1).LookupMemberRefAsMethod(handle, token);
    IEnumerable<(TargetPointer Value, uint Token)> ILoader.EnumerateModuleLookupMap(ModuleHandle module, ModuleLookupMapKind kind) => ((ILoader)_v1).EnumerateModuleLookupMap(module, kind);
    bool ILoader.IsCollectible(ModuleHandle handle) => ((ILoader)_v1).IsCollectible(handle);
    bool ILoader.IsDynamic(ModuleHandle handle) => ((ILoader)_v1).IsDynamic(handle);
    bool ILoader.IsModuleMapped(ModuleHandle handle) => ((ILoader)_v1).IsModuleMapped(handle);
    bool ILoader.IsAssemblyLoaded(ModuleHandle handle) => ((ILoader)_v1).IsAssemblyLoaded(handle);
    TargetPointer ILoader.GetGlobalLoaderAllocator() => ((ILoader)_v1).GetGlobalLoaderAllocator();
    TargetPointer ILoader.GetSystemAssembly() => ((ILoader)_v1).GetSystemAssembly();
    TargetPointer ILoader.GetHighFrequencyHeap(TargetPointer loaderAllocatorPointer) => ((ILoader)_v1).GetHighFrequencyHeap(loaderAllocatorPointer);
    TargetPointer ILoader.GetLowFrequencyHeap(TargetPointer loaderAllocatorPointer) => ((ILoader)_v1).GetLowFrequencyHeap(loaderAllocatorPointer);
    TargetPointer ILoader.GetILHeader(ModuleHandle handle, uint token) => ((ILoader)_v1).GetILHeader(handle, token);
    TargetPointer ILoader.GetObjectHandle(TargetPointer loaderAllocatorPointer) => ((ILoader)_v1).GetObjectHandle(loaderAllocatorPointer);
    TargetPointer ILoader.GetDynamicIL(ModuleHandle handle, uint token) => ((ILoader)_v1).GetDynamicIL(handle, token);
    IEnumerable<LoaderHeapBlock> ILoader.EnumerateLoaderHeapBlocks(TargetPointer loaderHeap) => ((ILoader)_v1).EnumerateLoaderHeapBlocks(loaderHeap);
    IReadOnlyDictionary<LoaderAllocatorHeapType, TargetPointer> ILoader.GetLoaderAllocatorHeaps(TargetPointer loaderAllocatorPointer) => ((ILoader)_v1).GetLoaderAllocatorHeaps(loaderAllocatorPointer);
    DebuggerAssemblyControlFlags ILoader.GetDebuggerInfoBits(ModuleHandle handle) => ((ILoader)_v1).GetDebuggerInfoBits(handle);
    void ILoader.SetDebuggerInfoBits(ModuleHandle handle, DebuggerAssemblyControlFlags newBits) => ((ILoader)_v1).SetDebuggerInfoBits(handle, newBits);
}
