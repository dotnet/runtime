// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Diagnostics.DataContractReader.Data;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

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

    private enum PEImageFlags : uint
    {
        FLAG_MAPPED = 0x01, // the file is mapped/hydrated (vs. the raw disk layout)
    };
    private readonly Target _target;

    internal Loader_1(Target target)
    {
        _target = target;
    }

    ModuleHandle ILoader.GetModuleHandleFromModulePtr(TargetPointer modulePointer)
    {
        if (modulePointer == TargetPointer.Null)
            throw new ArgumentNullException(nameof(modulePointer));

        return new ModuleHandle(modulePointer);
    }
    ModuleHandle ILoader.GetModuleHandleFromAssemblyPtr(TargetPointer assemblyPointer)
    {
        if (assemblyPointer == TargetPointer.Null)
            throw new ArgumentNullException(nameof(assemblyPointer));

        Data.Assembly assembly = _target.ProcessedData.GetOrAdd<Data.Assembly>(assemblyPointer);
        if (assembly.Module == TargetPointer.Null)
            throw new InvalidOperationException("Assembly does not have a module associated with it.");

        return new ModuleHandle(assembly.Module);
    }

    IEnumerable<ModuleHandle> ILoader.GetModuleHandles(TargetPointer appDomain, AssemblyIterationFlags iterationFlags)
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

    string ILoader.GetAppDomainFriendlyName()
    {
        TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
        Data.AppDomain appDomain = _target.ProcessedData.GetOrAdd<Data.AppDomain>(_target.ReadPointer(appDomainPointer));
        return appDomain.FriendlyName != TargetPointer.Null
            ? _target.ReadUtf16String(appDomain.FriendlyName)
            : string.Empty;
    }

    TargetPointer ILoader.GetModule(ModuleHandle handle)
    {
        return handle.Address;
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

    private static bool IsMapped(Data.PEImageLayout peImageLayout)
    {
        return (peImageLayout.Flags & (uint)PEImageFlags.FLAG_MAPPED) != 0;
    }

    private TargetPointer FindNTHeaders(Data.PEImageLayout imageLayout)
    {
        Data.ImageDosHeader dosHeader = _target.ProcessedData.GetOrAdd<Data.ImageDosHeader>(imageLayout.Base);
        return imageLayout.Base + (uint)dosHeader.Lfanew;
    }

    private TargetPointer RvaToSection(int rva, Data.PEImageLayout imageLayout)
    {
        TargetPointer ntHeadersPtr = FindNTHeaders(imageLayout);
        Data.ImageNTHeaders ntHeaders = _target.ProcessedData.GetOrAdd<Data.ImageNTHeaders>(ntHeadersPtr);
        int offset = Data.ImageNTHeaders.OptionalHeaderOffset;
        TargetPointer section = ntHeadersPtr + (uint)offset + ntHeaders.FileHeader.SizeOfOptionalHeader;
        TargetPointer sectionEnd = section + Data.ImageSectionHeader.Size * ntHeaders.FileHeader.NumberOfSections;
        while (section < sectionEnd)
        {
            Data.ImageSectionHeader sectionHeader = _target.ProcessedData.GetOrAdd<Data.ImageSectionHeader>(section);
            if (rva >= sectionHeader.VirtualAddress && rva < sectionHeader.VirtualAddress + sectionHeader.SizeOfRawData)
            {
                return section;
            }
            section += Data.ImageSectionHeader.Size;
        }
        return TargetPointer.Null;
    }

    private uint RvaToOffset(int rva, Data.PEImageLayout imageLayout)
    {
        TargetPointer section = RvaToSection(rva, imageLayout);
        if (section == TargetPointer.Null)
            throw new InvalidOperationException("Failed to read from image.");

        Data.ImageSectionHeader sectionHeader = _target.ProcessedData.GetOrAdd<Data.ImageSectionHeader>(section);
        uint offset = (uint)(rva - sectionHeader.VirtualAddress) + sectionHeader.PointerToRawData;
        return offset;
    }

    TargetPointer ILoader.GetILAddr(TargetPointer peAssemblyPtr, int rva)
    {
        Data.PEAssembly assembly = _target.ProcessedData.GetOrAdd<Data.PEAssembly>(peAssemblyPtr);
        if (assembly.PEImage == TargetPointer.Null)
            throw new InvalidOperationException("PEAssembly does not have a PEImage associated with it.");
        Data.PEImage peImage = _target.ProcessedData.GetOrAdd<Data.PEImage>(assembly.PEImage);
        if (peImage.LoadedImageLayout == TargetPointer.Null)
            throw new InvalidOperationException("PEImage does not have a LoadedImageLayout associated with it.");
        Data.PEImageLayout peImageLayout = _target.ProcessedData.GetOrAdd<Data.PEImageLayout>(peImage.LoadedImageLayout);
        uint offset;
        if (IsMapped(peImageLayout))
            offset = (uint)rva;
        else
            offset = RvaToOffset(rva, peImageLayout);
        return peImageLayout.Base + offset;
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

    IEnumerable<TargetPointer> ILoader.GetAvailableTypeParams(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        if (module.AvailableTypeParams == TargetPointer.Null)
            return [];

        EETypeHashTable typeHashTable = _target.ProcessedData.GetOrAdd<EETypeHashTable>(module.AvailableTypeParams);
        return typeHashTable.Entries.Select(entry => entry.TypeHandle);
    }

    IEnumerable<TargetPointer> ILoader.GetInstantiatedMethods(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);

        if (module.InstMethodHashTable == TargetPointer.Null)
            return [];

        InstMethodHashTable methodHashTable = _target.ProcessedData.GetOrAdd<InstMethodHashTable>(module.InstMethodHashTable);

        return methodHashTable.Entries.Select(entry => entry.MethodDesc);
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

    TargetPointer ILoader.GetAssemblyLoadContext(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.PEAssembly peAssembly = _target.ProcessedData.GetOrAdd<Data.PEAssembly>(module.PEAssembly);
        Data.AssemblyBinder binder = _target.ProcessedData.GetOrAdd<Data.AssemblyBinder>(peAssembly.AssemblyBinder);
        Data.ObjectHandle objectHandle = _target.ProcessedData.GetOrAdd<Data.ObjectHandle>(binder.AssemblyLoadContext);
        return objectHandle.Object;
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

    private static (bool Done, uint NextIndex) IterateLookupMap(uint index) => (false, index + 1);
    private static (bool Done, uint NextIndex) SearchLookupMap(uint index) => (true, index);
    private delegate (bool Done, uint NextIndex) Delegate(uint index);
    private IEnumerable<(TargetPointer, uint)> IterateModuleLookupMap(TargetPointer table, uint index, Delegate iterator)
    {
        bool doneIterating;
        do
        {
            Data.ModuleLookupMap lookupMap = _target.ProcessedData.GetOrAdd<Data.ModuleLookupMap>(table);
            if (index < lookupMap.Count)
            {
                TargetPointer entryAddress = lookupMap.TableData + (ulong)(index * _target.PointerSize);
                TargetPointer rawValue = _target.ReadPointer(entryAddress);
                yield return (rawValue, index);
                (doneIterating, index) = iterator(index);
                if (doneIterating)
                    yield break;
            }
            else
            {
                table = lookupMap.Next;
                index -= lookupMap.Count;
            }
        } while (table != TargetPointer.Null);
    }

    TargetPointer ILoader.GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags)
    {
        if (table == TargetPointer.Null)
        {
            flags = new TargetNUInt(0);
            return TargetPointer.Null;
        }

        Data.ModuleLookupMap lookupMap = _target.ProcessedData.GetOrAdd<Data.ModuleLookupMap>(table);
        ulong supportedFlagsMask = lookupMap.SupportedFlagsMask.Value;

        uint rid = EcmaMetadataUtils.GetRowId(token);
        ArgumentOutOfRangeException.ThrowIfZero(rid);
        (TargetPointer rval, uint _) = IterateModuleLookupMap(table, rid, SearchLookupMap).FirstOrDefault();
        flags = new TargetNUInt(rval & supportedFlagsMask);
        return rval & ~supportedFlagsMask;
    }

    IEnumerable<(TargetPointer, uint)> ILoader.EnumerateModuleLookupMap(TargetPointer table)
    {
        if (table == TargetPointer.Null)
            yield break;
        Data.ModuleLookupMap lookupMap = _target.ProcessedData.GetOrAdd<Data.ModuleLookupMap>(table);
        ulong supportedFlagsMask = lookupMap.SupportedFlagsMask.Value;
        TargetNUInt flags = new TargetNUInt(0);
        uint index = 1; // zero is invalid
        foreach ((TargetPointer targetPointer, uint idx) in IterateModuleLookupMap(table, index, IterateLookupMap))
        {
            TargetPointer rval = targetPointer & ~supportedFlagsMask;
            if (rval != TargetPointer.Null)
                yield return (rval, idx);
        }
    }

    bool ILoader.IsCollectible(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.Assembly la = _target.ProcessedData.GetOrAdd<Data.Assembly>(module.Assembly);
        return la.IsCollectible != 0;
    }

    bool ILoader.IsDynamic(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.Assembly assembly = _target.ProcessedData.GetOrAdd<Data.Assembly>(module.Assembly);
        return assembly.IsDynamic;
    }

    bool ILoader.IsAssemblyLoaded(ModuleHandle handle)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        Data.Assembly assembly = _target.ProcessedData.GetOrAdd<Data.Assembly>(module.Assembly);
        return assembly.Level >= ASSEMBLY_LEVEL_LOADED /* IsLoaded */;
    }

    TargetPointer ILoader.GetGlobalLoaderAllocator()
    {
        TargetPointer systemDomainPointer = _target.ReadGlobalPointer(Constants.Globals.SystemDomain);
        Data.SystemDomain systemDomain = _target.ProcessedData.GetOrAdd<Data.SystemDomain>(_target.ReadPointer(systemDomainPointer));
        return systemDomain.GlobalLoaderAllocator;
    }

    TargetPointer ILoader.GetSystemAssembly()
    {
        TargetPointer systemDomainPointer = _target.ReadGlobalPointer(Constants.Globals.SystemDomain);
        Data.SystemDomain systemDomain = _target.ProcessedData.GetOrAdd<Data.SystemDomain>(_target.ReadPointer(systemDomainPointer));
        return systemDomain.SystemAssembly;
    }

    TargetPointer ILoader.GetHighFrequencyHeap(TargetPointer loaderAllocatorPointer)
    {
        Data.LoaderAllocator loaderAllocator = _target.ProcessedData.GetOrAdd<Data.LoaderAllocator>(loaderAllocatorPointer);
        return loaderAllocator.HighFrequencyHeap;
    }

    TargetPointer ILoader.GetLowFrequencyHeap(TargetPointer loaderAllocatorPointer)
    {
        Data.LoaderAllocator loaderAllocator = _target.ProcessedData.GetOrAdd<Data.LoaderAllocator>(loaderAllocatorPointer);
        return loaderAllocator.LowFrequencyHeap;
    }

    TargetPointer ILoader.GetStubHeap(TargetPointer loaderAllocatorPointer)
    {
        Data.LoaderAllocator loaderAllocator = _target.ProcessedData.GetOrAdd<Data.LoaderAllocator>(loaderAllocatorPointer);
        return loaderAllocator.StubHeap;
    }

    TargetPointer ILoader.GetObjectHandle(TargetPointer loaderAllocatorPointer)
    {
        Data.LoaderAllocator loaderAllocator = _target.ProcessedData.GetOrAdd<Data.LoaderAllocator>(loaderAllocatorPointer);
        return loaderAllocator.ObjectHandle.Handle;
    }

    private int GetRVAFromMetadata(ModuleHandle handle, int token)
    {
        IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
        MetadataReader mdReader = ecmaMetadataContract.GetMetadata(handle)!;
        MethodDefinition methodDef = mdReader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(token));
        return methodDef.RelativeVirtualAddress;
    }

    TargetPointer ILoader.GetILHeader(ModuleHandle handle, uint token)
    {
        // we need module
        ILoader loader = this;
        TargetPointer peAssembly = loader.GetPEAssembly(handle);
        TargetPointer headerPtr = GetDynamicIL(handle, token);
        if (headerPtr == TargetPointer.Null)
        {
            int rva = GetRVAFromMetadata(handle, (int)token);
            headerPtr = loader.GetILAddr(peAssembly, rva);
        }
        return headerPtr;
    }

    private sealed class DynamicILBlobTraits : ITraits<uint, DynamicILBlobEntry>
    {
        public uint GetKey(DynamicILBlobEntry entry) => entry.EntryMethodToken;
        public bool Equals(uint left, uint right) => left == right;
        public uint Hash(uint key) => key;
        public bool IsNull(DynamicILBlobEntry entry) => entry.EntryMethodToken == 0;
        public DynamicILBlobEntry Null() => new DynamicILBlobEntry(0, TargetPointer.Null);
        public bool IsDeleted(DynamicILBlobEntry entry) => false;
    }

    private sealed class DynamicILBlobTable : IData<DynamicILBlobTable>
    {
        static DynamicILBlobTable IData<DynamicILBlobTable>.Create(Target target, TargetPointer address)
            => new DynamicILBlobTable(target, address);

        public DynamicILBlobTable(Target target, TargetPointer address)
        {
            ISHash sHashContract = target.Contracts.SHash;
            Target.TypeInfo type = target.GetTypeInfo(DataType.DynamicILBlobTable);
            HashTable = sHashContract.CreateSHash(target, address, type, new DynamicILBlobTraits());
        }
        public ISHash<uint, DynamicILBlobEntry> HashTable { get; init; }
    }

    private TargetPointer GetDynamicIL(ModuleHandle handle, uint token)
    {
        Data.Module module = _target.ProcessedData.GetOrAdd<Data.Module>(handle.Address);
        if (module.DynamicILBlobTable == TargetPointer.Null)
        {
            return TargetPointer.Null;
        }
        DynamicILBlobTable dynamicILBlobTable = _target.ProcessedData.GetOrAdd<DynamicILBlobTable>(module.DynamicILBlobTable);
        ISHash shashContract = _target.Contracts.SHash;
        return shashContract.LookupSHash(dynamicILBlobTable.HashTable, token).EntryIL;
    }
}
