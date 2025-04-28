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

public interface ILoader : IContract
{
    static string IContract.Name => nameof(Loader);

    ModuleHandle GetModuleHandle(TargetPointer modulePointer) => throw new NotImplementedException();

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
}

public readonly struct Loader : ILoader
{
    // Everything throws NotImplementedException
}
