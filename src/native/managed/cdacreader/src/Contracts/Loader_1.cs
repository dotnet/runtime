// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

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

    bool ILoader.IsReflectionEmit(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.PEAssembly.PEImage == null;
    }

    TargetPointer ILoader.GetILBase(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        if (module.PEAssembly.PEImage == null)
            return TargetPointer.Null;

        return module.PEAssembly.PEImage.Base;
    }

    TargetPointer ILoader.GetMetadataAddress(ModuleHandle handle, out ulong size)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        if (module.PEAssembly.PEImage == null)
        {
            size = 0;
            return TargetPointer.Null;
        }

        // TODO: [cdac]
        size = 0;
        return TargetPointer.Null;
    }

    IDictionary<ModuleLookupTable, TargetPointer> ILoader.GetLookupTables(ModuleHandle handle)
    {
        Dictionary<ModuleLookupTable, TargetPointer> tables = [];
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        tables[ModuleLookupTable.FieldDefToDesc] = module.FieldDefToDescMap;
        tables[ModuleLookupTable.ManifestModuleReferences] = module.ManifestModuleReferencesMap;
        tables[ModuleLookupTable.MemberRefToDesc] = module.MemberRefToDescMap;
        tables[ModuleLookupTable.MethodDefToDesc] = module.MethodDefToDescMap;
        tables[ModuleLookupTable.TypeDefToMethodTable] = module.TypeDefToMethodTableMap;
        tables[ModuleLookupTable.TypeRefToMethodTable] = module.TypeRefToMethodTableMap;
        return tables;
    }
}
