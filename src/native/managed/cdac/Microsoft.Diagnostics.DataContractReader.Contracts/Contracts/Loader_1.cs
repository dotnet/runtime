// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct Loader_1 : ILoader
{
    private const uint ASSEMBLY_LEVEL_LOADED = 4; // Assembly Level required to be considered Loaded
    private const uint ASSEMBLY_NOTIFYFLAGS_PROFILER_NOTIFIED = 0x1; // Assembly Notify Flag for profiler notification

    private enum ModuleFlags_1 : uint
    {
        Tenured = 0x1,           // Set once we know for sure the Module will not be freed until the appdomain itself exits
        ClassFreed = 0x4,
        EditAndContinue = 0x8, // Edit and Continue is enabled for this module

        ProfilerNotified = 0x10,
        EtwNotified = 0x20,

        ReflectionEmit = 0x40,    // Reflection.Emit was used to create this module
        ProfilerDisableOptimizations = 0x80,
        ProfilerDisableInlining = 0x100,

        DebuggerUserOverridePriv = 0x400,
        DebuggerAllowJitOptsPriv = 0x800,
        DebuggerTrackJitInfoPriv = 0x1000,
        DebuggerEnCEnabledPriv = 0x2000,
        DebuggerPDBsCopied = 0x4000,
        DebuggerIgnorePDbs = 0x8000,

        IJWFixedUp = 0x80000,
        BeingUnloaded = 0x100000,
    }

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

    IEnumerable<ModuleHandle> ILoader.GetModules(TargetPointer appDomain, AssemblyIterationFlags iterationFlags)
    {
        if (appDomain == TargetPointer.Null)
            throw new ArgumentNullException(nameof(appDomain));

        Data.AppDomain domain = _target.ProcessedData.GetOrAdd<Data.AppDomain>(appDomain);
        ArrayListBase arrayList = _target.ProcessedData.GetOrAdd<ArrayListBase>(domain.DomainAssemblyList);

        foreach (TargetPointer domainAssembly in arrayList.Elements)
        {
            TargetPointer pAssembly = _target.ReadPointer(domainAssembly);
            Data.Assembly assembly = _target.ProcessedData.GetOrAdd<Data.Assembly>(pAssembly);

            // following logic is based on AppDomain::AssemblyIterator::Next_Unlocked in appdomain.cpp

            if (assembly.IsError)
            {
                // assembly is in an error state, return if we are supposed to include it
                // otherwise we skip it and continue to the next assembly
                if (iterationFlags.HasFlag(AssemblyIterationFlags.IncludeFailedToLoad))
                {
                    yield return new ModuleHandle(assembly.Module);
                }
                continue;
            }

            if ((assembly.NotifyFlags & ASSEMBLY_NOTIFYFLAGS_PROFILER_NOTIFIED) != 0 && !iterationFlags.HasFlag(AssemblyIterationFlags.IncludeAvailableToProfilers))
            {
                // The assembly has reached the state at which we would notify profilers,
                // and we're supposed to include such assemblies in the enumeration. So
                // don't reject it (i.e., noop here, and don't bother with the rest of
                // the load status checks). Check for this first, since
                // IncludeAvailableToProfilers contains some loaded AND loading
                // assemblies.
            }
            else if (assembly.Level >= ASSEMBLY_LEVEL_LOADED /* IsLoaded */)
            {
                if (!iterationFlags.HasFlag(AssemblyIterationFlags.IncludeLoaded))
                    continue; // skip loaded assemblies
            }
            else
            {
                if (!iterationFlags.HasFlag(AssemblyIterationFlags.IncludeLoading))
                    continue; // skip loading assemblies
            }

            // Next, reject assemblies whose execution status is
            // not to be included in the enumeration

            if (!iterationFlags.HasFlag(AssemblyIterationFlags.IncludeExecution))
                continue; // skip assemblies with execution status

            if (assembly.IsCollectible != 0)
            {
                if (iterationFlags.HasFlag(AssemblyIterationFlags.ExcludeCollectible))
                    continue; // skip collectible assemblies

                Module module = _target.ProcessedData.GetOrAdd<Data.Module>(assembly.Module);
                if (!GetFlags(module).HasFlag(ModuleFlags.Tenured))
                    continue; // skip un-tenured modules

                LoaderAllocator loaderAllocator = _target.ProcessedData.GetOrAdd<Data.LoaderAllocator>(module.LoaderAllocator);
                if (!loaderAllocator.IsAlive && !iterationFlags.HasFlag(AssemblyIterationFlags.IncludeCollected))
                    continue; // skip collected assemblies
            }

            yield return new ModuleHandle(assembly.Module);
        }

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

    private static ModuleFlags GetFlags(Data.Module module)
    {
        // currently these flags are the same, but could diverge in the future
        ModuleFlags_1 runtimeFlags = (ModuleFlags_1)module.Flags;
        ModuleFlags flags = default;
        if (runtimeFlags.HasFlag(ModuleFlags_1.Tenured))
            flags |= ModuleFlags.Tenured;
        if (runtimeFlags.HasFlag(ModuleFlags_1.ClassFreed))
            flags |= ModuleFlags.ClassFreed;
        if (runtimeFlags.HasFlag(ModuleFlags_1.EditAndContinue))
            flags |= ModuleFlags.EditAndContinue;
        if (runtimeFlags.HasFlag(ModuleFlags_1.ProfilerNotified))
            flags |= ModuleFlags.ProfilerNotified;
        if (runtimeFlags.HasFlag(ModuleFlags_1.EtwNotified))
            flags |= ModuleFlags.EtwNotified;
        if (runtimeFlags.HasFlag(ModuleFlags_1.ReflectionEmit))
            flags |= ModuleFlags.ReflectionEmit;
        if (runtimeFlags.HasFlag(ModuleFlags_1.ProfilerDisableOptimizations))
            flags |= ModuleFlags.ProfilerDisableOptimizations;
        if (runtimeFlags.HasFlag(ModuleFlags_1.ProfilerDisableInlining))
            flags |= ModuleFlags.ProfilerDisableInlining;
        if (runtimeFlags.HasFlag(ModuleFlags_1.DebuggerUserOverridePriv))
            flags |= ModuleFlags.DebuggerUserOverridePriv;
        if (runtimeFlags.HasFlag(ModuleFlags_1.DebuggerAllowJitOptsPriv))
            flags |= ModuleFlags.DebuggerAllowJitOptsPriv;
        if (runtimeFlags.HasFlag(ModuleFlags_1.DebuggerTrackJitInfoPriv))
            flags |= ModuleFlags.DebuggerTrackJitInfoPriv;
        if (runtimeFlags.HasFlag(ModuleFlags_1.DebuggerEnCEnabledPriv))
            flags |= ModuleFlags.DebuggerEnCEnabledPriv;
        if (runtimeFlags.HasFlag(ModuleFlags_1.DebuggerPDBsCopied))
            flags |= ModuleFlags.DebuggerPDBsCopied;
        if (runtimeFlags.HasFlag(ModuleFlags_1.DebuggerIgnorePDbs))
            flags |= ModuleFlags.DebuggerIgnorePDbs;
        if (runtimeFlags.HasFlag(ModuleFlags_1.IJWFixedUp))
            flags |= ModuleFlags.IJWFixedUp;
        if (runtimeFlags.HasFlag(ModuleFlags_1.BeingUnloaded))
            flags |= ModuleFlags.BeingUnloaded;

        return flags;
    }

    ModuleFlags ILoader.GetFlags(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        return GetFlags(module);
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

    bool ILoader.IsAssemblyLoaded(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.Assembly assembly = _target.ProcessedData.GetOrAdd<Data.Assembly>(module.Assembly);
        return assembly.Level >= ASSEMBLY_LEVEL_LOADED /* IsLoaded */;
    }
}
