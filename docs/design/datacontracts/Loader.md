# Contract Loader

This contract is for getting information about loaded modules and assemblies

## APIs of contract

``` csharp
readonly struct ModuleHandle
{
    // Opaque handle - no public members

    internal TargetPointer Address;
}

[Flags]
enum ModuleFlags
{
    EditAndContinue = 0x00000008, // Edit and Continue is enabled for this module
}

enum ModuleLookupTable
{
    FieldDefToDesc,
    ManifestModuleReferences,
    MemberRefToDesc,
    MethodDefToDesc,
    TypeDefToMethodTable,
    TypeRefToMethodTable,
}
```

``` csharp
ModuleHandle GetModuleHandle(TargetPointer);
TargetPointer GetAssembly(ModuleHandle handle);
ModuleFlags GetFlags(ModuleHandle handle);
TargetPointer GetLoaderAllocator(ModuleHandle handle);
TargetPointer GetThunkHeap(ModuleHandle handle);
bool IsReflectionEmit(ModuleHandle handle);
TargetPointer GetILBase(ModuleHandle handle);
TargetPointer GetMetadataAddress(ModuleHandle handle, out ulong size);
IDictionary<ModuleLookupTable, TargetPointer> GetLookupTables(ModuleHandle handle);
```

## Version 1

Data descriptors used:
- `Module`
- `PEAssembly`

``` csharp
ModuleHandle GetModuleHandle(TargetPointer modulePointer)
{
    return new ModuleHandle(modulePointer);
}

TargetPointer GetAssembly(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Assrembly offset */);
}

ModuleFlags GetFlags(ModuleHandle handle)
{
    return target.Read<uint>(handle.Address + /* Module::Flags offset */);
}

TargetPointer GetLoaderAllocator(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::LoaderAllocator offset */);
}

TargetPointer GetThunkHeap(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::ThunkHeap offset */);
}

bool IsReflectionEmit(ModuleHandle handle)
{
    TargetPointer peAssembly = target.ReadPointer(handle.Address + /* Module::PEAssembly offset */);
    TargetPointer peImage = target.ReadPointer(peAssembly + /* PEAssembly::PEImage offset */);
    return peImage == TargetPointer.Null;
}

TargetPointer GetILBase(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Base offset */);
}

TargetPointer GetMetadataAddress(ModuleHandle handle, out ulong size)
{
    TargetPointer baseAddress = GetILBase(handle);
    if (baseAddress == TargetPointer.Null)
    {
        size = 0;
        return TargetPointer.Null;
    }

    // Read CLR header per https://learn.microsoft.com/windows/win32/debug/pe-format
    ulong clrHeaderRVA = ...

    // Read Metadata per ECMA-335 II.25.3.3 CLI Header
    ulong metadataDirectoryAddress = baseAddress + clrHeaderRva + /* offset to Metadata */
    int rva = target.Read<int>(metadataDirectoryAddress);
    size = target.Read<int>(metadataDirectoryAddress + sizeof(int));
    return baseAddress + rva;
}

IDictionary<ModuleLookupTable, TargetPointer> GetLookupTables(ModuleHandle handle)
{
    Dictionary<ModuleLookupTable, TargetPointer> tables = [];
    tables[ModuleLookupTable.FieldDefToDesc] = target.ReadPointer(handle.Address + /* Module::FieldDefToDescMap */);
    tables[ModuleLookupTable.ManifestModuleReferences] = target.ReadPointer(handle.Address + /* Module::ManifestModuleReferencesMap */);
    tables[ModuleLookupTable.MemberRefToDesc] = target.ReadPointer(handle.Address + /* Module::MemberRefToDescMap */);
    tables[ModuleLookupTable.MethodDefToDesc] = target.ReadPointer(handle.Address + /* Module::MethodDefToDescMap */);
    tables[ModuleLookupTable.TypeDefToMethodTable] = target.ReadPointer(handle.Address + /* Module::TypeDefToMethodTableMap */);
    tables[ModuleLookupTable.TypeRefToMethodTable] = target.ReadPointer(handle.Address + /* Module::TypeRefToMethodTableMap */);
    return tables;
}
```
