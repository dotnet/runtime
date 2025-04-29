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
    Tenured = 0x00000001,           // Set once we know for sure the Module will not be freed until the appdomain itself exits
    EditAndContinue = 0x00000008,   // Edit and Continue is enabled for this module
    ReflectionEmit = 0x00000040,    // Reflection.Emit was used to create this module
}

public record struct ModuleLookupTables(
    TargetPointer FieldDefToDesc,
    TargetPointer ManifestModuleReferences,
    TargetPointer MemberRefToDesc,
    TargetPointer MethodDefToDesc,
    TargetPointer TypeDefToMethodTable,
    TargetPointer TypeRefToMethodTable,
    TargetPointer MethodDefToILCodeVersioningState);

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

public interface ILoader : IContract
{
    static string IContract.Name => nameof(Loader);

    ModuleHandle GetModuleHandle(TargetPointer modulePointer) => throw new NotImplementedException();

    IEnumerable<ModuleHandle> GetModules(TargetPointer appDomain, AssemblyIterationFlags iterationFlags) => throw new NotImplementedException();
    TargetPointer GetRootAssembly() => throw new NotImplementedException();
    TargetPointer GetAssembly(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetPEAssembly(ModuleHandle handle) => throw new NotImplementedException();
    bool TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags) => throw new NotImplementedException();
    bool TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size) => throw new NotImplementedException();
    bool IsProbeExtensionResultValid(ModuleHandle handle) => throw new NotImplementedException();
    ModuleFlags GetFlags(ModuleHandle handle) => throw new NotImplementedException();
    string GetPath(ModuleHandle handle) => throw new NotImplementedException();
    string GetFileName(ModuleHandle handle) => throw new NotImplementedException();

    TargetPointer GetLoaderAllocator(ModuleHandle handle) => throw new NotImplementedException();
    TargetPointer GetILBase(ModuleHandle handle) => throw new NotImplementedException();
    ModuleLookupTables GetLookupTables(ModuleHandle handle) => throw new NotImplementedException();

    TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags) => throw new NotImplementedException();
    bool IsCollectible(ModuleHandle handle) => throw new NotImplementedException();
    bool IsAssemblyLoaded(ModuleHandle handle) => throw new NotImplementedException();
}

public readonly struct Loader : ILoader
{
    // Everything throws NotImplementedException
}
