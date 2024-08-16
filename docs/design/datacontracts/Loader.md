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
    EditAndContinue = 0x00000008,   // Edit and Continue is enabled for this module
    ReflectionEmit = 0x00000040,    // Reflection.Emit was used to create this module
}

record struct ModuleLookupTables(
    TargetPointer FieldDefToDesc,
    TargetPointer ManifestModuleReferences,
    TargetPointer MemberRefToDesc,
    TargetPointer MethodDefToDesc,
    TargetPointer TypeDefToMethodTable,
    TargetPointer TypeRefToMethodTable,
    TargetPointer MethodDefToILCodeVersioningState);
```

``` csharp
ModuleHandle GetModuleHandle(TargetPointer module);
TargetPointer GetAssembly(ModuleHandle handle);
ModuleFlags GetFlags(ModuleHandle handle);
string GetPath(ModuleHandle handle);
TargetPointer GetLoaderAllocator(ModuleHandle handle);
TargetPointer GetThunkHeap(ModuleHandle handle);
TargetPointer GetILBase(ModuleHandle handle);
ModuleLookupTables GetLookupTables(ModuleHandle handle);
TargetPointer GetModuleLookupMapElement(TargetPointer table, uint rid, out TargetNUInt flags);
bool IsCollectibleLoaderAllocator(ModuleHandle handle);

```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Module` | `Assembly` | Assembly of the Module |
| `Module` | `Base` | Pointer to start of PE file in memory |
| `Module` | `Flags` | Assembly of the Module |
| `Module` | `LoaderAllocator` | LoaderAllocator of the Module |
| `Module` | `ThunkHeap` | Pointer to the thunk heap |
| `Module` | `Path` | Path of the Module (UTF-16, null-terminated) |
| `Module` | `FieldDefToDescMap` | Mapping table |
| `Module` | `ManifestModuleReferencesMap` | Mapping table |
| `Module` | `MemberRefToDescMap` | Mapping table |
| `Module` | `MethodDefToDescMap` | Mapping table |
| `Module` | `TypeDefToMethodTableMap` | Mapping table |
| `Module` | `TypeRefToMethodTableMap` | Mapping table |
| `ModuleLookupMap` | `TableData` | Start of the mapping table's data |
| `ModuleLookupMap` | `SupportedFlagsMask` | Mask for flag bits on lookup map entries |
| `ModuleLookupMap` | `Count` | Number of TargetPointer sized entries in this section of the map |
| `ModuleLookupMap` | `Next` | Pointer to next ModuleLookupMap segment for this map
| `LoaderAllocator` | `IsCollectible` | Flag indicating if this is loader allocator may be collected

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

string GetPath(ModuleHandle handle)
{
    TargetPointer pathStart = target.ReadPointer(handle.Address + /* Module::Path offset */);
    char[] path = // Read<char> from target starting at pathStart until null terminator
    return new string(path);
}

TargetPointer GetLoaderAllocator(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::LoaderAllocator offset */);
}

TargetPointer GetThunkHeap(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::ThunkHeap offset */);
}

TargetPointer GetILBase(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Base offset */);
}

ModuleLookupTables GetLookupTables(ModuleHandle handle)
{
    return new ModuleLookupTables(
        FieldDefToDescMap: target.ReadPointer(handle.Address + /* Module::FieldDefToDescMap */),
        ManifestModuleReferencesMap: target.ReadPointer(handle.Address + /* Module::ManifestModuleReferencesMap */),
        MemberRefToDescMap: target.ReadPointer(handle.Address + /* Module::MemberRefToDescMap */),
        MethodDefToDescMap: target.ReadPointer(handle.Address + /* Module::MethodDefToDescMap */),
        TypeDefToMethodTableMap: target.ReadPointer(handle.Address + /* Module::TypeDefToMethodTableMap */),
        TypeRefToMethodTableMap: target.ReadPointer(handle.Address + /* Module::TypeRefToMethodTableMap */),
        MethodDefToILCodeVersioningState: target.ReadPointer(handle.Address + /*
        Module::MethodDefToILCodeVersioningState */));
}
```

**TODO* pseudocode for IsCollectibleLoaderAllocator  and LookupTableMap element lookup
