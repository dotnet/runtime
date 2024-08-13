// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Loader_1 : ILoader
{
    private readonly Target _target;

    internal Loader_1(Target target)
    {
        _target = target;
    }

    ModuleHandle ILoader.GetModuleHandle(TargetPointer modulePointer)
    {
        if (modulePointer == TargetPointer.Null)
            throw new ArgumentNullException(nameof(modulePointer));

        return new ModuleHandle(modulePointer);
    }

    TargetPointer ILoader.GetAssembly(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.Assembly;
    }

    ModuleFlags ILoader.GetFlags(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return (ModuleFlags)module.Flags;
    }

    string ILoader.GetPath(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        // TODO: [cdac] Add/use APIs on Target for reading strings in target endianness
        TargetPointer addr = module.Path;
        while (true)
        {
            // Read characters until we find the null terminator
            char nameChar = _target.Read<char>(addr);
            if (nameChar == 0)
                break;

            addr += sizeof(char);
        }

        int length = (int)(addr.Value - module.Path.Value);
        if (length == 0)
            return string.Empty;

        Span<byte> span = stackalloc byte[length];
        _target.ReadBuffer(module.Path, span);
        return new string(MemoryMarshal.Cast<byte, char>(span));
    }

    TargetPointer ILoader.GetLoaderAllocator(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.LoaderAllocator;
    }

    TargetPointer ILoader.GetThunkHeap(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.ThunkHeap;
    }

    TargetPointer ILoader.GetILBase(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.Base;
    }

    ModuleLookupTables ILoader.GetLookupTables(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return new ModuleLookupTables(
            module.FieldDefToDescMap,
            module.ManifestModuleReferencesMap,
            module.MemberRefToDescMap,
            module.MethodDefToDescMap,
            module.TypeDefToMethodTableMap,
            module.TypeRefToMethodTableMap,
            module.MethodDefToILCodeVersioningStateMap);
    }

    TargetPointer ILoader.GetModuleLookupMapElement(TargetPointer table, uint rid, out TargetNUInt flags)
    {
        rid &= 0x00FFFFFF; // FIXME: do we have a util that does this?
        ArgumentOutOfRangeException.ThrowIfZero(rid);
        flags = new TargetNUInt(0);
        if (table == TargetPointer.Null)
            return TargetPointer.Null;
        uint index = rid;
        Data.ModuleLookupMap lookupMap = _target.ProcessedData.GetOrAdd<Data.ModuleLookupMap>(table);
        // have to read lookupMap an extra time upfront because only the first map
        // has valid supportedFlagsMask
        TargetNUInt supportedFlagsMask = lookupMap.SupportedFlagsMask;
        do
        {
            lookupMap = _target.ProcessedData.GetOrAdd<Data.ModuleLookupMap>(table);
            if (index < lookupMap.Count)
            {
                TargetPointer entryAddress = lookupMap.TableData + (ulong)(index * _target.PointerSize);
                TargetPointer rawValue = _target.ReadPointer(entryAddress);
                flags = new TargetNUInt(rawValue & supportedFlagsMask.Value);
                return rawValue & ~(supportedFlagsMask.Value);
            }
            else
            {
                table = lookupMap.Next;
                index -= lookupMap.Count;
            }
        } while (table != TargetPointer.Null);
        return TargetPointer.Null;
    }

    bool ILoader.IsCollectibleLoaderAllocator(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        TargetPointer loaderAllocator = module.LoaderAllocator;
        Data.LoaderAllocator la = _target.ProcessedData.GetOrAdd<Data.LoaderAllocator>(loaderAllocator);
        return la.IsCollectible != 0;
    }
}
