// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

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

    TargetPointer ILoader.GetRootAssembly()
    {
        TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
        Data.AppDomain appDomain = _target.ProcessedData.GetOrAdd<Data.AppDomain>(_target.ReadPointer(appDomainPointer));
        return appDomain.RootAssembly;
    }

    TargetPointer ILoader.GetAssembly(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.Assembly;
    }

    TargetPointer ILoader.GetPEAssembly(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.PEAssembly;
    }

    bool ILoader.TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags)
    {
        baseAddress = TargetPointer.Null;
        size = 0;
        imageFlags = 0;
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        if (module.PEAssembly == TargetPointer.Null)
            return false; // no loaded PEAssembly

        Data.PEAssembly peAssembly = _target.ProcessedData.GetOrAdd<Data.PEAssembly>(module.PEAssembly);

        if (peAssembly.PEImage == TargetPointer.Null)
            return false; // no loaded PEImage

        Data.PEImage peImage = _target.ProcessedData.GetOrAdd<Data.PEImage>(peAssembly.PEImage);

        if (peImage.LoadedImageLayout == TargetPointer.Null)
            return false; // no loaded image layout

        Data.PEImageLayout peImageLayout = _target.ProcessedData.GetOrAdd<Data.PEImageLayout>(peImage.LoadedImageLayout);

        baseAddress = peImageLayout.Base;
        size = peImageLayout.Size;
        imageFlags = peImageLayout.Flags;

        return true;
    }

    bool ILoader.TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size)
    {
        buffer = TargetPointer.Null;
        size = 0;

        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        if (module.GrowableSymbolStream == TargetPointer.Null)
            return false;

        Data.CGrowableSymbolStream growableSymbolStream = _target.ProcessedData.GetOrAdd<CGrowableSymbolStream>(module.GrowableSymbolStream);

        buffer = growableSymbolStream.Buffer;
        size = growableSymbolStream.Size;

        return true;
    }

    bool ILoader.IsProbeExtensionResultValid(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        if (module.PEAssembly == TargetPointer.Null)
            return false; // no loaded PEAssembly

        Data.PEAssembly peAssembly = _target.ProcessedData.GetOrAdd<Data.PEAssembly>(module.PEAssembly);

        if (peAssembly.PEImage == TargetPointer.Null)
            return false; // no loaded PEImage

        Data.PEImage peImage = _target.ProcessedData.GetOrAdd<Data.PEImage>(peAssembly.PEImage);

        // 0 is the invalid type. See assemblyprobeextension.h for details
        return peImage.ProbeExtensionResult.Type != 0;
    }

    ModuleFlags ILoader.GetFlags(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return (ModuleFlags)module.Flags;
    }

    string ILoader.GetPath(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.Path != TargetPointer.Null
            ? _target.ReadUtf16String(module.Path)
            : string.Empty;
    }

    string ILoader.GetFileName(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.FileName != TargetPointer.Null
            ? _target.ReadUtf16String(module.FileName)
            : string.Empty;
    }

    TargetPointer ILoader.GetLoaderAllocator(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return module.LoaderAllocator;
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

    TargetPointer ILoader.GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags)
    {
        uint rid = EcmaMetadataUtils.GetRowId(token);
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

    bool ILoader.IsCollectible(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        TargetPointer assembly = module.Assembly;
        Data.Assembly la = _target.ProcessedData.GetOrAdd<Data.Assembly>(assembly);
        return la.IsCollectible != 0;
    }
}
