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
TargetPointer GetRootAssembly();
TargetPointer GetAssembly(ModuleHandle handle);
TargetPointer GetPEAssembly(ModuleHandle handle);
bool TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags);
bool TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size);
bool IsProbeExtensionResultValid(ModuleHandle handle);
ModuleFlags GetFlags(ModuleHandle handle);
string GetPath(ModuleHandle handle);
string GetFileName(ModuleHandle handle);
TargetPointer GetLoaderAllocator(ModuleHandle handle);
TargetPointer GetThunkHeap(ModuleHandle handle);
TargetPointer GetILBase(ModuleHandle handle);
ModuleLookupTables GetLookupTables(ModuleHandle handle);
TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags);
bool IsCollectible(ModuleHandle handle);
```

## Version 1

Data descriptors used:
| Data Descriptor Name | Field | Meaning |
| --- | --- | --- |
| `Module` | `Assembly` | Assembly of the Module |
| `Module` | `PEAssembly` | PEAssembly of the Module |
| `Module` | `Base` | Pointer to start of PE file in memory |
| `Module` | `Flags` | Assembly of the Module |
| `Module` | `LoaderAllocator` | LoaderAllocator of the Module |
| `Module` | `ThunkHeap` | Pointer to the thunk heap |
| `Module` | `Path` | Path of the Module (UTF-16, null-terminated) |
| `Module` | `FileName` | File name of the Module (UTF-16, null-terminated) |
| `Module` | `GrowableSymbolStream` | Pointer to the in memory symbol stream |
| `Module` | `FieldDefToDescMap` | Mapping table |
| `Module` | `ManifestModuleReferencesMap` | Mapping table |
| `Module` | `MemberRefToDescMap` | Mapping table |
| `Module` | `MethodDefToDescMap` | Mapping table |
| `Module` | `TypeDefToMethodTableMap` | Mapping table |
| `Module` | `TypeRefToMethodTableMap` | Mapping table |
| `ModuleLookupMap` | `TableData` | Start of the mapping table's data |
| `ModuleLookupMap` | `SupportedFlagsMask` | Mask for flag bits on lookup map entries |
| `ModuleLookupMap` | `Count` | Number of TargetPointer sized entries in this section of the map |
| `ModuleLookupMap` | `Next` | Pointer to next ModuleLookupMap segment for this map |
| `Assembly` | `IsCollectible` | Flag indicating if this is module may be collected |
| `PEAssembly` | `PEImage` | Pointer to the PEAssembly's PEImage |
| `PEImage` | `LoadedImageLayout` | Pointer to the PEImage's loaded PEImageLayout |
| `PEImage` | `ProbeExtensionResult` | PEImage's ProbeExtensionResult |
| `ProbeExtensionResult` | `Type` | Type of ProbeExtensionResult |
| `PEImageLayout` | `Base` | Base address of the image layout |
| `PEImageLayout` | `Size` | Size of the image layout |
| `PEImageLayout` | `Flags` | Flags associated with the PEImageLayout |
| `CGrowableSymbolStream` | `Buffer` | Pointer to the raw symbol stream buffer start |
| `CGrowableSymbolStream` | `Size` | Size of the raw symbol stream buffer |
| `AppDomain` | `RootAssembly` | Pointer to the root assembly |

Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `AppDomain` | TargetPointer | Pointer to the global AppDomain |


``` csharp
ModuleHandle GetModuleHandle(TargetPointer modulePointer)
{
    return new ModuleHandle(modulePointer);
}

TargetPointer GetRootAssembly()
{
    TargetPointer appDomainPointer = _target.ReadGlobalPointer(Constants.Globals.AppDomain);
    AppDomain appDomain = // read AppDomain object starting at appDomainPointer
    return appDomain.RootAssembly;
}

TargetPointer GetAssembly(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Assembly offset */);
}

TargetPointer GetPEAssembly(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::PEAssembly offset */);
}

bool TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags)
{
    baseAddress = TargetPointer.Null;
    size = 0;
    imageFlags = 0;

    TargetPointer peAssembly = target.ReadPointer(handle.Address + /* Module::PEAssembly offset */);
    if (peAssembly == 0) return false; // no loaded PEAssembly

    TargetPointer peImage = target.ReadPointer(peAssembly + /* PEAssembly::PEImage offset */);
    if(peImage == 0) return false; // no loaded PEImage

    TargetPointer peImageLayout = target.ReadPointer(peImage + /* PEImage::LoadedImageLayout offset */);

    baseAddress = target.ReadPointer(peImageLayout + /* PEImageLayout::Base offset */);
    size = target.Read<uint>(peImageLayout + /* PEImageLayout::Size offset */);
    imageFlags = target.Read<uint>(peImageLayout + /* PEImageLayout::Flags offset */);
    return true;
}

bool TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size)
{
    buffer = TargetPointer.Null;
    size = 0;

    TargetPointer growableSymbolStream = target.ReadPointer(handle.Address + /* Module::GrowableSymbolStream offset */);
    if (growableSymbolStream == 0) return false; // no GrowableSymbolStream found

    buffer = target.ReadPointer(growableSymbolStream + /* CGrowableSymbolStream::Buffer offset */);
    size = target.Read<uint>(growableSymbolStream + /* CGrowableSymbolStream::Size offset */);
    return true;
}

bool IsProbeExtensionResultValid(ModuleHandle handle)
{
    TargetPointer peAssembly = target.ReadPointer(handle.Address + /* Module::PEAssembly offset */);
    if (peAssembly == 0) return false; // no loaded PEAssembly

    TargetPointer peImage = target.ReadPointer(peAssembly + /* PEAssembly::PEImage offset */);
    if(peImage == 0) return false; // no loaded PEImage

    TargetPointer probeExtensionResult = target.ReadPointer(peImage + /* PEImage::ProbeExtensionResult offset */);
    int type = target.Read<int>(probeExtensionResult + /* ProbeExtensionResult::Type offset */);
    return type != 0; // 0 is the invalid type. See assemblyprobeextension.h for details
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

string GetFileName(ModuleHandle handle)
{
    TargetPointer fileNameStart = target.ReadPointer(handle.Address + /* Module::FileName offset */);
    char[] fileName = // Read<char> from target starting at fileNameStart until null terminator
    return new string(fileName);
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

TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags);
{
    uint rid = /* get row id from token*/ (token);
    flags = new TargetNUInt(0);
    if (table == TargetPointer.Null)
        return TargetPointer.Null;
    uint index = rid;
    // have to read lookupMap an extra time upfront because only the first map
    // has valid supportedFlagsMask
    TargetNUInt supportedFlagsMask = _target.ReadNUInt(table + /* ModuleLookupMap::SupportedFlagsMask */);
    do
    {
        if (index < _target.Read<uint>(table + /*ModuleLookupMap::Count*/))
        {
            TargetPointer entryAddress = _target.ReadPointer(lookupMap + /*ModuleLookupMap::TableData*/) + (ulong)(index * _target.PointerSize);
            TargetPointer rawValue = _target.ReadPointer(entryAddress);
            flags = rawValue & supportedFlagsMask;
            return rawValue & ~(supportedFlagsMask.Value);
        }
        else
        {
            table = _target.ReadPointer(lookupMap + /*ModuleLookupMap::Next*/);
            index -= _target.Read<uint>(lookupMap + /*ModuleLookupMap::Count*/);
        }
    } while (table != TargetPointer.Null);
    return TargetPointer.Null;
}
```

```csharp
bool ILoader.IsCollectible(ModuleHandle handle)
{
    TargetPointer assembly = _target.ReadPointer(handle.Address + /*Module::Assembly*/);
    byte isCollectible = _target.Read<byte>(assembly + /* Assembly::IsCollectible*/);
    return isCollectible != 0;
}
```
