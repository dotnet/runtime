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
        TargetPointer addr = module.Path;
        List<char> name = [];
        while (true)
        {
            // Read characters until we find the null terminator
            char nameChar = _target.Read<char>(addr);
            if (nameChar == 0)
                break;

            name.Add(nameChar);
            addr += sizeof(char);
        }

        return new string(CollectionsMarshal.AsSpan(name));
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

    TargetPointer ILoader.GetMetadataAddress(ModuleHandle handle, out ulong size)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.GetLoadedMetadata(out size);
    }

    AvailableMetadataType ILoader.GetAvailableMetadataType(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        AvailableMetadataType flags = AvailableMetadataType.None;

        if (module.DynamicMetadata != TargetPointer.Null)
            flags |= AvailableMetadataType.ReadWriteSavedCopy;
        else
            flags |= AvailableMetadataType.ReadOnly;

        // TODO(cdac) implement direct reading of unsaved ReadWrite metadata
        return flags;
    }

    TargetPointer ILoader.GetReadWriteSavedMetadataAddress(ModuleHandle handle, out ulong size)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.DynamicMetadata dynamicMetadata = _target.ProcessedData.GetOrAdd<Data.DynamicMetadata>(module.DynamicMetadata);
        TargetPointer result = dynamicMetadata.Data;
        size = dynamicMetadata.Size;
        return result;
    }

    TargetEcmaMetadata ILoader.GetReadWriteMetadata(ModuleHandle handle) => throw new NotImplementedException();


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
}
