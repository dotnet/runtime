// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public readonly struct ModuleHandle
{
    public ModuleHandle(TargetPointer address)
    {
        Address = address;
    }

    public TargetPointer Address { get; }
}

[Flags]
public enum ModuleFlags
{
    Tenured = 0x1,                      // Set once we know for sure the Module will not be freed until the appdomain itself exits
    JitOptimizationDisabled = 0x2,      // Cached flag: JIT optimizations are disabled
    EditAndContinue = 0x8,              // Edit and Continue is enabled for this module
    ReflectionEmit = 0x40,              // Reflection.Emit was used to create this module
    ProfDisableOptimizations = 0x80,    // Profiler disabled JIT optimizations
    EncCapable = 0x200,                 // Cached flag: module is Edit and Continue capable
}

[Flags]
public enum DebuggerAssemblyControlFlags : uint
{
    DACF_NONE = 0x00,
    DACF_ALLOW_JIT_OPTS = 0x02,
    DACF_ENC_ENABLED = 0x08,
    DACF_IGNORE_PDBS = 0x20,
    DACF_CONTROL_FLAGS_MASK = 0x2E,
}

public enum ClrModifiableAssemblies : uint
{
    Unset = 0,
    None = 1,
    Debug = 2,
}

[Flags]
public enum AssemblyIterationFlags
{
    // load status flags
    IncludeLoaded = 0x00000001, // include assemblies that are already loaded
                                // (m_level >= code:FILE_LOAD_DELIVER_EVENTS)
    IncludeLoading = 0x00000002, // include assemblies that are still in the process of loading
                                 // (all m_level values)
    IncludeAvailableToProfilers = 0x00000020, // include assemblies available to profilers
                                              // See comment at code:DomainAssembly::IsAvailableToProfilers

    // Execution / introspection flags
    IncludeExecution = 0x00000004, // include assemblies that are loaded for execution only

    IncludeFailedToLoad = 0x00000010, // include assemblies that failed to load

    // Collectible assemblies flags
    ExcludeCollectible = 0x00000040, // Exclude all collectible assemblies
    IncludeCollected = 0x00000080, // Include all collectible assemblies that have been collected
}

public record struct ModuleLookupTables(
    TargetPointer FieldDefToDesc,
    TargetPointer ManifestModuleReferences,
    TargetPointer MemberRefToDesc,
    TargetPointer MethodDefToDesc,
    TargetPointer TypeDefToMethodTable,
    TargetPointer TypeRefToMethodTable,
    TargetPointer MethodDefToILCodeVersioningState);

public readonly struct LoaderHeapBlockData
{
    public TargetPointer Address { get; init; }
    public TargetNUInt Size { get; init; }
    public TargetPointer NextBlock { get; init; }
}

public interface ILoader : IContract
{
    static string IContract.Name => nameof(Loader);

    ModuleHandle GetModuleHandleFromModulePtr(TargetPointer modulePointer) => throw new NotImplementedException();
    ModuleHandle GetModuleHandleFromAssemblyPtr(TargetPointer assemblyPointer) => throw new NotImplementedException();
    IEnumerable<ModuleHandle> GetModuleHandles(TargetPointer appDomain, AssemblyIterationFlags iterationFlags) => throw new NotImplementedException();
    TargetPointer GetRootAssembly() => throw new NotImplementedException();
    string GetAppDomainFriendlyName() => throw new NotImplementedException();
    TargetPointer GetModule(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetAssembly(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetPEAssembly(ModuleHandle handle) => throw new NotImplementedException();
    bool TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags) => throw new NotImplementedException();
    TargetPointer GetILAddr(TargetPointer peAssemblyPtr, int rva) => throw new NotImplementedException();
    TargetPointer GetFieldAddressFromRva(TargetPointer peAssemblyPtr, int rva) => throw new NotImplementedException();
    bool TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size) => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetAvailableTypeParams(ModuleHandle handle) => throw new NotImplementedException();
    IEnumerable<TargetPointer> GetInstantiatedMethods(ModuleHandle handle) => throw new NotImplementedException();

    bool IsProbeExtensionResultValid(ModuleHandle handle) => throw new NotImplementedException();
    ModuleFlags GetFlags(ModuleHandle handle) => throw new NotImplementedException();
    bool IsReadyToRun(ModuleHandle handle) => throw new NotImplementedException();
    bool TryGetSimpleName(ModuleHandle handle, out string simpleName) => throw new NotImplementedException();
    string GetPath(ModuleHandle handle) => throw new NotImplementedException();
    string GetFileName(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetLoaderAllocator(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetILBase(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetAssemblyLoadContext(ModuleHandle handle) => throw new NotImplementedException();
    ModuleLookupTables GetLookupTables(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags) => throw new NotImplementedException();
    IEnumerable<(TargetPointer, uint)> EnumerateModuleLookupMap(TargetPointer table) => throw new NotImplementedException();
    bool IsCollectible(ModuleHandle handle) => throw new NotImplementedException();
    bool IsDynamic(ModuleHandle handle) => throw new NotImplementedException();
    bool IsModuleMapped(ModuleHandle handle) => throw new NotImplementedException();
    bool IsAssemblyLoaded(ModuleHandle handle) => throw new NotImplementedException();

    TargetPointer GetGlobalLoaderAllocator() => throw new NotImplementedException();
    TargetPointer GetSystemAssembly() => throw new NotImplementedException();
    TargetPointer GetHighFrequencyHeap(TargetPointer loaderAllocatorPointer) => throw new NotImplementedException();
    TargetPointer GetLowFrequencyHeap(TargetPointer loaderAllocatorPointer) => throw new NotImplementedException();
    TargetPointer GetStubHeap(TargetPointer loaderAllocatorPointer) => throw new NotImplementedException();
    TargetPointer GetILHeader(ModuleHandle handle, uint token) => throw new NotImplementedException();
    TargetPointer GetObjectHandle(TargetPointer loaderAllocatorPointer) => throw new NotImplementedException();
    TargetPointer GetDynamicIL(ModuleHandle handle, uint token) => throw new NotImplementedException();

    // Returns the first block of the loader heap linked list, or TargetPointer.Null if the heap has no blocks.
    TargetPointer GetFirstLoaderHeapBlock(TargetPointer loaderHeap) => throw new NotImplementedException();
    // Returns the data for the given loader heap block (address, size, and next block pointer).
    LoaderHeapBlockData GetLoaderHeapBlockData(TargetPointer block) => throw new NotImplementedException();
    IReadOnlyDictionary<string, TargetPointer> GetLoaderAllocatorHeaps(TargetPointer loaderAllocatorPointer) => throw new NotImplementedException();

    DebuggerAssemblyControlFlags GetDebuggerInfoBits(ModuleHandle handle) => throw new NotImplementedException();
    void SetDebuggerInfoBits(ModuleHandle handle, DebuggerAssemblyControlFlags newBits) => throw new NotImplementedException();
}

public readonly struct Loader : ILoader
{
    // Everything throws NotImplementedException
}
