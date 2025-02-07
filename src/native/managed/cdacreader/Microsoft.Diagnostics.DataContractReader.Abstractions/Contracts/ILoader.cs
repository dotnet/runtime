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

    public virtual ModuleHandle GetModuleHandle(TargetPointer modulePointer) => throw new NotImplementedException();

    public virtual TargetPointer GetAssembly(ModuleHandle handle) => throw new NotImplementedException();
    public virtual ModuleFlags GetFlags(ModuleHandle handle) => throw new NotImplementedException();
    public virtual string GetPath(ModuleHandle handle) => throw new NotImplementedException();
    public virtual string GetFileName(ModuleHandle handle) => throw new NotImplementedException();

    public virtual TargetPointer GetLoaderAllocator(ModuleHandle handle) => throw new NotImplementedException();
    public virtual TargetPointer GetThunkHeap(ModuleHandle handle) => throw new NotImplementedException();
    public virtual TargetPointer GetILBase(ModuleHandle handle) => throw new NotImplementedException();
    public virtual ModuleLookupTables GetLookupTables(ModuleHandle handle) => throw new NotImplementedException();

    public virtual TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags) => throw new NotImplementedException();
    public virtual bool IsCollectible(ModuleHandle handle) => throw new NotImplementedException();
}

public readonly struct Loader : ILoader
{
    // Everything throws NotImplementedException
}
