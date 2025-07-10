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
    Tenured = 0x00000001, // Set once we know for sure the Module will not be freed until the appdomain itself exits
    EditAndContinue = 0x00000008,   // Edit and Continue is enabled for this module
    ReflectionEmit = 0x00000040,    // Reflection.Emit was used to create this module
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
IEnumerable<ModuleHandle> GetModules(TargetPointer appDomain, AssemblyIterationFlags iterationFlags);
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
bool IsAssemblyLoaded(ModuleHandle handle);
```

## Version 1

### Data descriptors used:
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
| `Assembly` | `Module` | Pointer to the Assemblies module |
| `Assembly` | `IsCollectible` | Flag indicating if this is module may be collected |
| `Assembly` | `Error` | Pointer to exception. No error if nullptr |
| `Assembly` | `NotifyFlags` | Flags relating to the debugger/profiler notification state of the assembly |
| `Assembly` | `Level` | File load level of the assembly |
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
| `AppDomain` | `DomainAssemblyList` | ArrayListBase of assemblies in the AppDomain |
| `LoaderAllocator` | `ReferenceCount` | Reference count of LoaderAllocator |
| `ArrayListBase` | `Count` | Total number of elements in the ArrayListBase |
| `ArrayListBase` | `FirstBlock` | First ArrayListBlock |
| `ArrayListBlock` | `Next` | Next ArrayListBlock in chain |
| `ArrayListBlock` | `Size` | Size of data section in block |
| `ArrayListBlock` | `ArrayStart` | Start of data section in block |


### Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `AppDomain` | TargetPointer | Pointer to the global AppDomain |


### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `ASSEMBLY_LEVEL_LOADED` | uint | The value of Assembly Level required for an Assembly to be considered loaded. In the runtime, this is `FILE_LOAD_DELIVER_EVENTS` | `0x4` |
| `ASSEMBLY_NOTIFYFLAGS_PROFILER_NOTIFIED` | uint | Flag in Assembly NotifyFlags indicating the Assembly will notify profilers. | `0x1` |

### Data Structures
```csharp
// The runtime representation of Module's flag field.
// For contract version 1, these are identical to ModuleFlags on the contract interface, but could diverge in the future.
private enum ModuleFlags_1 : uint
{
    Tenured = 0x00000001,           // Set once we know for sure the Module will not be freed until the appdomain itself exits
    EditAndContinue = 0x00000008,   // Edit and Continue is enabled for this module
    ReflectionEmit = 0x00000040,    // Reflection.Emit was used to create this module
}
```

### Method Implementations
``` csharp
ModuleHandle GetModuleHandle(TargetPointer modulePointer)
{
    return new ModuleHandle(modulePointer);
}

IEnumerable<ModuleHandle> GetModules(TargetPointer appDomain, AssemblyIterationFlags iterationFlags)
{
    if (appDomain == TargetPointer.Null) throw new ArgumentException("appDomain must not be null");

    // ArrayListBase encapsulates the data structure defined in arraylist.h
    // It handles reading each contained pointer and exposing them as a C# List
    ArrayListBase arrayList = // read ArrayListBase starting at appDomain + AppDomain::DomainAssemblyList offset

    foreach (TargetPointer domainAssembly in arrayList.Elements)
    {
        // We have a list of DomainAssemblies, this class contains a single pointer to an Assembly.
        // Therefore we can read a pointer at the DomainAssembly to access the actual Assembly.
        TargetPointer pAssembly = target.ReadPointer(domainAssembly);
        Assembly assembly = // read Assembly object at pAssembly

        // The Assemblies map 1:1 to Modules, however we must filter them based on the iterationFlags before returning.
        // The following filtering logic is based on AppDomain::AssemblyIterator::Next_Unlocked in appdomain.cpp

        if (assembly.IsError)
        {
            // assembly is in an error state, return if we are supposed to include it
            // in either case, we continue to the next assembly
            if (iterationFlags.HasFlag(AssemblyIterationFlags.IncludeFailedToLoad))
            {
                yield return new ModuleHandle(assembly.Module);
            }
            continue;
        }

        if ((assembly.NotifyFlags & ASSEMBLY_NOTIFYFLAGS_PROFILER_NOTIFIED) != 0 &&
            !iterationFlags.HasFlag(AssemblyIterationFlags.IncludeAvailableToProfilers))
        {
            // The assembly has reached the state at which we would notify profilers,
            // and we're supposed to include such assemblies in the enumeration. So
            // don't reject it (i.e., noop here, and don't bother with the rest of
            // the load status checks). Check for this first, since
            // IncludeAvailableToProfilers contains some loaded AND loading
            // assemblies.
        }
        else if (assembly.Level >= ASSEMBLY_LEVEL_LOADED)
        {
            if (!iterationFlags.HasFlag(AssemblyIterationFlags.IncludeLoaded))
            {
                // the assembly is loaded, but we aren't including loaded assemblies, skip
                continue;
            }
        }
        else
        {
            // assembly must be in the process of loading as it is not currently loaded

            if (!iterationFlags.HasFlag(AssemblyIterationFlags.IncludeLoading))
            {
                // the assembly is loading, but we aren't including loading assemblies, skip
                continue;
            }
        }

        // Next, reject assemblies whose execution status is
        // not to be included in the enumeration

        if (!iterationFlags.HasFlag(AssemblyIterationFlags.IncludeExecution))
        {
            // the assembly is executing, but we aren't including executing assemblies, skip
            continue;
        }

        if (assembly.IsCollectible != 0)
        {
            if (iterationFlags.HasFlag(AssemblyIterationFlags.ExcludeCollectible))
            {
                // the assembly is collectible, but we are excluding collectible assemblies, skip
                continue;
            }

            Module module = // read Module at assembly.Module
            if (((ModuleFlags)module.Flags).HasFlag(ModuleFlags.Tenured))
            {
                // Un-tenured collectible assemblies should not be returned. (This can only happen in a brief
                // window during collectible assembly creation. No thread should need to have a pointer
                // to the just allocated DomainAssembly at this stage.)
                // the assemblies Module is not Tenured, skip
                continue;
            }

            LoaderAllocator loaderAllocator = // read LoaderAllocator at module.LoaderAllocator
            if (!loaderAllocator.IsAlive && !iterationFlags.HasFlag(AssemblyIterationFlags.IncludeCollected))
            {
                // if the assembly is not alive anymore and we aren't including Collected assemblies, skip
                continue;
            }
        }

        yield return new ModuleHandle(assembly.Module);
    }
}

TargetPointer GetRootAssembly()
{
    TargetPointer appDomainPointer = target.ReadGlobalPointer(Constants.Globals.AppDomain);
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

private static ModuleFlags GetFlags(uint flags)
{
    ModuleFlags_1 runtimeFlags = (ModuleFlags_1)flags;
    ModuleFlags flags = default;
    if (runtimeFlags.HasFlag(ModuleFlags_1.Tenured))
        flags |= ModuleFlags.Tenured;
    if (runtimeFlags.HasFlag(ModuleFlags_1.EditAndContinue))
        flags |= ModuleFlags.EditAndContinue;
    if (runtimeFlags.HasFlag(ModuleFlags_1.ReflectionEmit))
        flags |= ModuleFlags.ReflectionEmit;
    return flags;
}

ModuleFlags GetFlags(ModuleHandle handle)
{
    return GetFlags(target.Read<uint>(handle.Address + /* Module::Flags offset */));
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

bool ILoader.IsAssemblyLoaded(ModuleHandle handle)
{
    TargetPointer assembly = _target.ReadPointer(handle.Address + /*Module::Assembly*/);
    uint loadLevel = _target.Read<uint>(assembly + /* Assembly::Level*/);
    return assembly.Level >= ASSEMBLY_LEVEL_LOADED;
}
```
