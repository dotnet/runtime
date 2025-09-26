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
ModuleHandle GetModuleHandleFromModulePtr(TargetPointer module);
ModuleHandle GetModuleHandleFromAssemblyPtr(TargetPointer assemblyPointer);
IEnumerable<ModuleHandle> GetModuleHandles(TargetPointer appDomain, AssemblyIterationFlags iterationFlags);
TargetPointer GetRootAssembly();
string GetAppDomainFriendlyName();
TargetPointer GetModule(ModuleHandle handle);
TargetPointer GetAssembly(ModuleHandle handle);
TargetPointer GetPEAssembly(ModuleHandle handle);
bool TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags);
TargetPointer ILoader.GetILAddr(TargetPointer peAssemblyPtr, int rva);
bool TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size);
IEnumerable<TargetPointer> GetAvailableTypeParams(ModuleHandle handle);
IEnumerable<TargetPointer> GetInstantiatedMethods(ModuleHandle handle);

bool IsProbeExtensionResultValid(ModuleHandle handle);
ModuleFlags GetFlags(ModuleHandle handle);
string GetPath(ModuleHandle handle);
string GetFileName(ModuleHandle handle);
TargetPointer GetLoaderAllocator(ModuleHandle handle);
TargetPointer GetILBase(ModuleHandle handle);
TargetPointer GetAssemblyLoadContext(ModuleHandle handle);
ModuleLookupTables GetLookupTables(ModuleHandle handle);
TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags);
IEnumerable<(TargetPointer, uint)> EnumerateModuleLookupMap(TargetPointer table);
bool IsCollectible(ModuleHandle handle);
bool IsAssemblyLoaded(ModuleHandle handle);
TargetPointer GetGlobalLoaderAllocator();
TargetPointer GetHighFrequencyHeap(TargetPointer loaderAllocatorPointer);
TargetPointer GetLowFrequencyHeap(TargetPointer loaderAllocatorPointer);
TargetPointer GetStubHeap(TargetPointer loaderAllocatorPointer);
TargetPointer GetObjectHandle(TargetPointer loaderAllocatorPointer);
TargetPointer GetILHeader(ModuleHandle handle, uint token);
TargetPointer GetDynamicIL(ModuleHandle handle, uint token);
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
| `Module` | `Path` | Path of the Module (UTF-16, null-terminated) |
| `Module` | `FileName` | File name of the Module (UTF-16, null-terminated) |
| `Module` | `GrowableSymbolStream` | Pointer to the in memory symbol stream |
| `Module` | `AvailableTypeParams` | Pointer to an EETypeHashTable |
| `Module` | `InstMethodHashTable` | Pointer to an InstMethodHashTable |
| `Module` | `FieldDefToDescMap` | Mapping table |
| `Module` | `ManifestModuleReferencesMap` | Mapping table |
| `Module` | `MemberRefToDescMap` | Mapping table |
| `Module` | `MethodDefToDescMap` | Mapping table |
| `Module` | `TypeDefToMethodTableMap` | Mapping table |
| `Module` | `TypeRefToMethodTableMap` | Mapping table |
| `Module` | `DynamicILBlobTable` | pointer to the table of dynamic IL |
| `ModuleLookupMap` | `TableData` | Start of the mapping table's data |
| `ModuleLookupMap` | `SupportedFlagsMask` | Mask for flag bits on lookup map entries |
| `ModuleLookupMap` | `Count` | Number of TargetPointer sized entries in this section of the map |
| `ModuleLookupMap` | `Next` | Pointer to next ModuleLookupMap segment for this map |
| `Assembly` | `Module` | Pointer to the Assemblies module |
| `Assembly` | `IsCollectible` | Flag indicating if this module may be collected |
| `Assembly` | `IsDynamic` | Flag indicating if this module is dynamic |
| `Assembly` | `Error` | Pointer to exception. No error if nullptr |
| `Assembly` | `NotifyFlags` | Flags relating to the debugger/profiler notification state of the assembly |
| `Assembly` | `Level` | File load level of the assembly |
| `PEAssembly` | `PEImage` | Pointer to the PEAssembly's PEImage |
| `PEAssembly` | `AssemblyBinder` | Pointer to the PEAssembly's binder |
| `AssemblyBinder` | `AssemblyLoadContext` | Pointer to the AssemblyBinder's AssemblyLoadContext |
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
| `AppDomain` | `FriendlyName` | Friendly name of the AppDomain |
| `SystemDomain` | `GlobalLoaderAllocator` | global LoaderAllocator |
| `LoaderAllocator` | `ReferenceCount` | Reference count of LoaderAllocator |
| `LoaderAllocator` | `HighFrequencyHeap` | High-frequency heap of LoaderAllocator |
| `LoaderAllocator` | `LowFrequencyHeap` | Low-frequency heap of LoaderAllocator |
| `LoaderAllocator` | `StubHeap` | Stub heap of LoaderAllocator |
| `LoaderAllocator` | `ObjectHandle` | object handle of LoaderAllocator |
| `ArrayListBase` | `Count` | Total number of elements in the ArrayListBase |
| `ArrayListBase` | `FirstBlock` | First ArrayListBlock |
| `ArrayListBlock` | `Next` | Next ArrayListBlock in chain |
| `ArrayListBlock` | `Size` | Size of data section in block |
| `ArrayListBlock` | `ArrayStart` | Start of data section in block |
| `EETypeHashTable` | `Buckets` | Pointer to hash table buckets |
| `EETypeHashTable` | `Count` | Count of elements in the hash table |
| `EETypeHashTable` | `VolatileEntryValue` | The data stored in the hash table entry |
| `EETypeHashTable` | `VolatileEntryNextEntry` | Next pointer in the hash table entry |
| `InstMethodHashTable` | `Buckets` | Pointer to hash table buckets |
| `InstMethodHashTable` | `Count` | Count of elements in the hash table |
| `InstMethodHashTable` | `VolatileEntryValue` | The data stored in the hash table entry |
| `InstMethodHashTable` | `VolatileEntryNextEntry` | Next pointer in the hash table entry |
| `DynamicILBlobTable` | `Table` | Pointer to IL blob table |
| `DynamicILBlobTable` | `TableSize` | Number of entries in table |
| `DynamicILBlobTable` | `EntrySize` | Size of each table entry |
| `DynamicILBlobTable` | `EntryMethodToken` | Offset of each entry method token from entry address |
| `DynamicILBlobTable` | `EntryIL` | Offset of each entry IL from entry address |



### Global variables used:
| Global Name | Type | Purpose |
| --- | --- | --- |
| `AppDomain` | TargetPointer | Pointer to the global AppDomain |
| `SystemDomain` | TargetPointer | Pointer to the global SystemDomain |


### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `ASSEMBLY_LEVEL_LOADED` | uint | The value of Assembly Level required for an Assembly to be considered loaded. In the runtime, this is `FILE_LOAD_DELIVER_EVENTS` | `0x4` |
| `ASSEMBLY_NOTIFYFLAGS_PROFILER_NOTIFIED` | uint | Flag in Assembly NotifyFlags indicating the Assembly will notify profilers. | `0x1` |

Contracts used:
| Contract Name |
| --- |
| EcmaMetadata |
| SHash |

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

private enum PEImageFlags : uint
{
    FLAG_MAPPED             = 0x01, // the file is mapped/hydrated (vs. the raw disk layout)
};
```

### Method Implementations
``` csharp
ModuleHandle GetModuleHandleFromModulePtr(TargetPointer modulePointer)
{
    return new ModuleHandle(modulePointer);
}

ModuleHandle ILoader.GetModuleHandleFromAssemblyPtr(TargetPointer assemblyPointer)
{
    Data.Assembly assembly = // read Assembly object at assemblyPointer
    return new ModuleHandle(assembly.Module);
}

IEnumerable<ModuleHandle> GetModuleHandles(TargetPointer appDomain, AssemblyIterationFlags iterationFlags)
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
    TargetPointer appDomainPointer = target.ReadGlobalPointer("AppDomain");
    AppDomain appDomain = // read AppDomain object starting at appDomainPointer
    return appDomain.RootAssembly;
}

string ILoader.GetAppDomainFriendlyName()
{
    TargetPointer appDomainPointer = target.ReadGlobalPointer("AppDomain");
    TargetPointer appDomain = target.ReadPointer(appDomainPointer)
    TargetPointer pathStart = appDomain + /* AppDomain::FriendlyName offset */;
    char[] name = // Read<char> from target starting at pathStart until null terminator
    return new string(name);
}

TargetPointer ILoader.GetModule(ModuleHandle handle)
{
    return handle.Address;
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

TargetPointer ILoader.GetILAddr(TargetPointer peAssemblyPtr, int rva)
{
    TargetPointer peImage = target.ReadPointer(peAssemblyPtr + /* PEAssembly::PEImage offset */);
    if(peImage == TargetPointer.Null)
        throw new InvalidOperationException("PEAssembly does not have a PEImage associated with it.");

    TargetPointer peImageLayout = target.ReadPointer(peImage + /* PEImage::LoadedImageLayout offset */);
    if(peImageLayout == TargetPointer.Null)
        throw new InvalidOperationException("PEImage does not have a LoadedImageLayout associated with it.");

    // Get base address and flags from PEImageLayout
    TargetPointer baseAddress = target.ReadPointer(peImageLayout + /* PEImageLayout::Base offset */);
    uint imageFlags = target.Read<uint>(peImageLayout + /* PEImageLayout::Flags offset */);

    bool isMapped = (imageFlags & (uint)PEImageFlags.FLAG_MAPPED) != 0;

    uint offset;
    if (isMapped)
    {
        offset = (uint)rva;
    }
    else
    {
        // find NT headers using DOS header
        uint dosHeaderLfanew = target.Read<uint>(baseAddress + /* ImageDosHeader::LfanewOffset */);
        TargetPointer ntHeadersPtr = baseAddress + dosHeaderLfanew;

        TargetPointer optionalHeaderPtr = ntHeadersPtr + /* ImageNTHeaders::OptionalHeaderOffset */;

        // Get number of sections from file header
        TargetPointer fileHeaderPtr = ntHeadersPtr + /* ImageNTHeaders::FileHeaderOffset */;
        uint numberOfSections = target.Read<uint>(fileHeaderPtr + /* ImageFileHeader::NumberOfSectionsOffset */);

        // Calculate first section address (after NT headers and optional header)
        uint imageFileHeaderSize = target.Read<ushort>(fileHeaderPtr + /* ImageFileHeader::SizeOfOptionalHeaderOffset */);
        TargetPointer firstSectionPtr = ntHeadersPtr + /* ImageNTHeaders::OptionalHeaderOffset */ + imageFileHeaderSize;

        // Find the section containing this RVA
        TargetPointer sectionPtr = TargetPointer.Null;
        uint sectionHeaderSize = /* sizeof(ImageSectionHeader native struct) */;

        for (uint i = 0; i < numberOfSections; i++)
        {
            TargetPointer currentSectionPtr = firstSectionPtr + (i * sectionHeaderSize);
            uint virtualAddress = target.Read<uint>(currentSectionPtr + /* ImageSectionHeader::VirtualAddressOffset */);
            uint sizeOfRawData = target.Read<uint>(currentSectionPtr + /* ImageSectionHeader::SizeOfRawDataOffset */);

            if (rva >= VirtualAddress && rva < VirtualAddress + SizeOfRawData)
            {
                sectionPtr = currentSectionPtr;
            }
        }
        if (sectionPtr == TargetPointer.Null)
        {
            throw new InvalidOperationException("Failed to read from image.");
        }
        else
        {
            // Convert RVA to file offset using section information
            uint sectionVirtualAddress = target.Read<uint>(sectionPtr + /* ImageSectionHeader::VirtualAddressOffset */);
            uint sectionPointerToRawData = target.Read<uint>(sectionPtr + /* ImageSectionHeader::PointerToRawDataOffset */);
            offset = ((rva - sectionVirtualAddress) + sectionPointerToRawData);
        }
    }
    return baseAddress + offset;
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

IEnumerable<TargetPointer> GetAvailableTypeParams(ModuleHandle handle)
{
    TargetPointer availableTypeParams = target.ReadPointer(handle.Address + /* Module::AvailableTypeParams offset */);

    if (availableTypeParams == TargetPointer.Null) return [];

    // EETypeHashTable is read as a DacEnumerableHash table.
    // For more information on how this is read, see section below.
    EETypeHashTable typeHashTable = // read EETypeHashTable at availableTypeParams
    return typeHashTable.Entries.Select(entry => entry.TypeHandle);
}

IEnumerable<TargetPointer> GetInstantiatedMethods(ModuleHandle handle)
{
    TargetPointer instMethodHashTable = target.ReadPointer(handle.Address + /* Module::InstMethodHashTable offset */);

    if (instMethodHashTable == TargetPointer.Null) return [];

    // InstMethodHashTable is read as a DacEnumerableHash table.
    // For more information on how this is read, see section below.
    InstMethodHashTable methodHashTable = // read InstMethodHashTable at instMethodHashTable
    return methodHashTable.Entries.Select(entry => entry.MethodDesc);
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

TargetPointer GetILBase(ModuleHandle handle)
{
    return target.ReadPointer(handle.Address + /* Module::Base offset */);
}

TargetPointer ILoader.GetAssemblyLoadContext(ModuleHandle handle)
{
    PEAssembly peAssembly = target.ReadPointer(handle.Address + /* Module::PEAssembly offset */);
    AssemblyBinder binder = target.ReadPointer(peAssembly + /* PEAssembly::AssemblyBinder offset */);
    ObjectHandle objectHandle = new ObjectHandle(binder);
    return objectHandle.Object;
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
    TargetNUInt supportedFlagsMask = target.ReadNUInt(table + /* ModuleLookupMap::SupportedFlagsMask */);
    do
    {
        if (index < target.Read<uint>(table + /*ModuleLookupMap::Count*/))
        {
            TargetPointer entryAddress = target.ReadPointer(lookupMap + /*ModuleLookupMap::TableData*/) + (ulong)(index * target.PointerSize);
            TargetPointer rawValue = target.ReadPointer(entryAddress);
            flags = rawValue & supportedFlagsMask;
            return rawValue & ~(supportedFlagsMask.Value);
        }
        else
        {
            table = target.ReadPointer(lookupMap + /*ModuleLookupMap::Next*/);
            index -= target.Read<uint>(lookupMap + /*ModuleLookupMap::Count*/);
        }
    } while (table != TargetPointer.Null);
    return TargetPointer.Null;
}

IEnumerable<(TargetPointer, uint)> EnumerateModuleLookupMap(TargetPointer table)
{
    Data.ModuleLookupMap lookupMap = new Data.ModuleLookupMap(table);
    // have to read lookupMap an extra time upfront because only the first map
    // has valid supportedFlagsMask
    TargetNUInt supportedFlagsMask = target.ReadNUInt(table + /* ModuleLookupMap::SupportedFlagsMask */);
    uint index = 1; // zero is invalid
    do
    {
        uint count = target.Read<uint>(table + /*ModuleLookupMap::Count*/);
        if (index < count)
        {
            TargetPointer entryAddress = target.ReadPointer(table + /*ModuleLookupMap::TableData*/) + (ulong)(index * target.PointerSize);
            TargetPointer rawValue = target.ReadPointer(entryAddress);
            ulong maskedValue = rawValue & ~(supportedFlagsMask.Value);
            if (maskedValue != 0)
                yield return (new TargetPointer(maskedValue), index);
            index++;
        }
        else
        {
            table = target.ReadPointer(table + /*ModuleLookupMap::Next*/);
            index -= count;
        }
    } while (table != TargetPointer.Null);
}

bool IsCollectible(ModuleHandle handle)
{
    TargetPointer assembly = target.ReadPointer(handle.Address + /*Module::Assembly*/);
    byte isCollectible = target.Read<byte>(assembly + /* Assembly::IsCollectible*/);
    return isCollectible != 0;
}

bool IsDynamic(ModuleHandle handle)
{
    TargetPointer assembly = target.ReadPointer(handle.Address + /*Module::Assembly*/);
    byte isDynamic = target.Read<byte>(assembly + /* Assembly::IsDynamic*/);
    return isDynamic != 0;
}

bool IsAssemblyLoaded(ModuleHandle handle)
{
    TargetPointer assembly = target.ReadPointer(handle.Address + /*Module::Assembly*/);
    uint loadLevel = target.Read<uint>(assembly + /* Assembly::Level*/);
    return assembly.Level >= ASSEMBLY_LEVEL_LOADED;
}

TargetPointer GetGlobalLoaderAllocator()
{
    TargetPointer systemDomainPointer = target.ReadGlobalPointer("SystemDomain");
    TargetPointer systemDomain = target.ReadPointer(systemDomainPointer);
    return target.ReadPointer(systemDomain + /* SystemDomain::GlobalLoaderAllocator offset */);
}

TargetPointer GetHighFrequencyHeap(TargetPointer loaderAllocatorPointer)
{
    return target.ReadPointer(loaderAllocatorPointer + /* LoaderAllocator::HighFrequencyHeap offset */);
}

TargetPointer GetLowFrequencyHeap(TargetPointer loaderAllocatorPointer)
{
    return target.ReadPointer(loaderAllocatorPointer + /* LoaderAllocator::LowFrequencyHeap offset */);
}

TargetPointer GetStubHeap(TargetPointer loaderAllocatorPointer)
{
    return target.ReadPointer(loaderAllocatorPointer + /* LoaderAllocator::StubHeap offset */);
}

TargetPointer GetObjectHandle(TargetPointer loaderAllocatorPointer)
{
    return target.ReadPointer(loaderAllocatorPointer + /* LoaderAllocator::ObjectHandle offset */);
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

TargetPointer GetILHeader(ModuleHandle handle, uint token)
{
    if (GetDynamicIL(handle, token) == TargetPointer.Null)
    {
        TargetPointer peAssembly = loader.GetPEAssembly(handle);
        IEcmaMetadata ecmaMetadataContract = _target.Contracts.EcmaMetadata;
        MetadataReader? mdReader = ecmaMetadataContract.GetMetadata(handle);
        if (mdReader == null)
            throw new NotImplementedException();
        MethodDefinition methodDef = mdReader.GetMethodDefinition(MetadataTokens.MethodDefinitionHandle(token));
        int rva = methodDef.RelativeVirtualAddress;
        headerPtr = loader.GetILAddr(peAssembly, rva);
    }
    return headerPtr;
}

TargetPointer GetDynamicIL(ModuleHandle handle, uint token)
{
    TargetPointer dynamicBlobTablePtr = target.ReadPointer(handle.Address + /* Module::DynamicILBlobTable offset */);
    Contracts.IThread shashContract = target.Contracts.SHash;
    DynamicILBlobTraits traits = new();
    /* To construct an SHash we must pass a DataType enum.
    We must be able to look up this enum in a dictionary of known types and retrieve a Target.TypeInfo struct.
    This struct contains a dictionary of fields with keys corresponding to the names of offsets
    and values corresponding to the offset values. Optionally, it contains a Size field.
    */
    SHash<uint, Data.DynamicILBlobEntry> shash = shashContract.CreateSHash<uint, Data.DynamicILBlobEntry>(target, dynamicBlobTablePtr, DataType.DynamicILBlobTable, traits)
    Data.DynamicILBlobEntry blobEntry = shashContract.LookupSHash(shash, token);
    return /* blob entry IL address */
}
```

### DacEnumerableHash (EETypeHashTable and InstMethodHashTable)

Both `EETypeHashTable` and `InstMethodHashTable` are based on the templated `DacEnumerableHash`. Because the base class is templated on the derived type, offsets may be different in derived types.

The base implementation of `DacEnumerableHash` uses four datadescriptors:
| Datadescriptor | Purpose |
| --- | --- |
| `Buckets` | Pointer to the bucket array |
| `Count` | Number of elements in the hash table |
| `VolatileEntryValue` | The data held by an entry, defined by the derived class |
| `VolatileEntryNextEntry` | The next pointer on an hash table entry |

The hash table is laid out as an array of `VolatileEntry` pointers's (buckets), each possibly forming a chain for values that hash into that bucket. The first three buckets are special and reserved for metadata. Instead of containing a `VolatileEntry`, these pointers are read as values with the following meanings.

| Reserved Bucket offset | Purpose |
| --- | --- |
| `0` | Length of the Bucket array, this value does not include the first 3 slots which are special |
| `1` | Pointer to the next bucket array, not currently used in the cDAC |
| `2` | End sentinel for the current bucket array, not currently used in the cDAC |

The current cDAC implementation does not use the 'hash' part of the table at all. Instead it iterates all elements in the table. Following the existing iteration logic in the runtime (and DAC), resizing the table while iterating is not supported. Given this constraint, the pointer to the next bucket array (resized data table) and the current end sentinel are not required to iterate all entries.

To read all entries in the hash table:
1. Read the length bucket to find the number of chains `n`.
2. Initialize a list of elements `entries = []`.
3. For each chain, (buckets with offsets `3..n + 3`):
    1. Read the pointer in the bucket as `volatileEntryPtr`.
    2. If `volatileEntryPtr & 0x1 == 0x1`, this is an end sentinel and we stop reading this chain.
    3. Otherwise, add `volatileEntryPtr + /* VolatileEntryValue offset */` to entries. This points to the derived class defined data type.
    4. Set `volatileEntryPtr` to the value of the pointer located at `volatileEntryPtr + /* VolatileEntryNextEntry offset */` and go to step 3.2.
4. Return `entries` to be further parsed by derived classes.

While both EETypeHashTable and InstMethodHashTable store pointer sized data types, they both use the LSBs as special flags.

#### EETypeHashTable
EETypeHashTable uses the LSB to indicate if the TypeHandle is a hot entry. The cDAC implementation separates each value `value` in the table into two parts. The actual TypeHandle pointer and the associated flags.

```csharp
class EETypeHashTable
{
    private const ulong FLAG_MASK = 0x1ul;

    public IReadOnlyList<Entry> Entires { get; }

    public readonly struct Entry(TargetPointer value)
    {
        public TargetPointer TypeHandle { get; } = value & ~FLAG_MASK;
        public uint Flags { get; } = (uint)(value.Value & FLAG_MASK);
    }
}
```

#### InstMethodHashTable
InstMethodHashTable uses the 2 LSBs as flags for the MethodDesc. The cDAC implementation separates each value `value` in the table into two parts. The actual MethodDesc pointer and the associated flags.

```csharp
class InstMethodHashTable
{
    private const ulong FLAG_MASK = 0x3ul;

    public IReadOnlyList<Entry> Entires { get; }

    public readonly struct Entry(TargetPointer value)
    {
        public TargetPointer MethodDesc { get; } = value & ~FLAG_MASK;
        public uint Flags { get; } = (uint)(value.Value & FLAG_MASK);
    }
}
```
