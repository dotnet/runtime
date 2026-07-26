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
    Tenured = 0x1,                      // Set once we know for sure the Module will not be freed until the appdomain itself exits
    JitOptimizationDisabled = 0x2,      // Cached flag: JIT optimizations are disabled
    EditAndContinue = 0x8,              // Edit and Continue is enabled for this module
    ReflectionEmit = 0x40,              // Reflection.Emit was used to create this module
    ProfDisableOptimizations = 0x80,    // Profiler disabled JIT optimizations
    EncCapable = 0x200,                 // Cached flag: module is Edit and Continue capable
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
                                              // See comment at code:Assembly::IsAvailableToProfilers

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
    TargetPointer MethodDefToILCodeVersioningState,
    uint TableDataOffset);

readonly record struct LoaderHeapBlock(TargetPointer Address, TargetNUInt Size);

enum LoaderAllocatorHeapType
{
    Unknown,
    LowFrequencyHeap,
    HighFrequencyHeap,
    StaticsHeap,
    StubHeap,
    ExecutableHeap,
    FixupPrecodeHeap,
    NewStubPrecodeHeap,
    DynamicHelpersStubHeap,
    IndcellHeap,
    CacheEntryHeap,
}
```

``` csharp
ModuleHandle GetModuleHandleFromModulePtr(TargetPointer module);
ModuleHandle GetModuleHandleFromAssemblyPtr(TargetPointer assemblyPointer);
IEnumerable<ModuleHandle> GetModuleHandles(TargetPointer appDomain, AssemblyIterationFlags iterationFlags);
TargetPointer GetRootAssembly();
string GetAppDomainFriendlyName();
TargetPointer GetAppDomain();
TargetPointer GetModule(ModuleHandle handle);
TargetPointer GetAssembly(ModuleHandle handle);
TargetPointer GetPEAssembly(ModuleHandle handle);
bool TryGetLoadedImageContents(ModuleHandle handle, out TargetPointer baseAddress, out uint size, out uint imageFlags);
TargetPointer GetILAddr(TargetPointer peAssemblyPtr, int rva);
TargetPointer GetFieldAddressFromRva(TargetPointer peAssemblyPtr, int rva);
bool TryGetSymbolStream(ModuleHandle handle, out TargetPointer buffer, out uint size);
IEnumerable<TargetPointer> GetAvailableTypeParams(ModuleHandle handle);
IEnumerable<TargetPointer> GetInstantiatedMethods(ModuleHandle handle);

bool IsProbeExtensionResultValid(ModuleHandle handle);
ModuleFlags GetFlags(ModuleHandle handle);
bool IsReadyToRun(ModuleHandle handle);
string GetSimpleName(ModuleHandle handle);
string GetPath(ModuleHandle handle);
string GetFileName(ModuleHandle handle);
bool GetFileHeadersInfo(ModuleHandle handle, out uint timeStamp, out uint imageSize);
TargetPointer GetLoaderAllocator(ModuleHandle handle);
TargetPointer GetILBase(ModuleHandle handle);
TargetPointer GetAssemblyLoadContext(ModuleHandle handle);
ModuleLookupTables GetLookupTables(ModuleHandle handle);
TargetPointer GetModuleLookupMapElement(TargetPointer table, uint token, out TargetNUInt flags);
IEnumerable<(TargetPointer, uint)> EnumerateModuleLookupMap(TargetPointer table);
bool IsCollectible(ModuleHandle handle);
bool IsDynamic(ModuleHandle handle);
bool IsModuleMapped(ModuleHandle handle);
bool IsAssemblyLoaded(ModuleHandle handle);
TargetPointer GetGlobalLoaderAllocator();
TargetPointer GetSystemAssembly();
TargetPointer GetHighFrequencyHeap(TargetPointer loaderAllocatorPointer);
TargetPointer GetLowFrequencyHeap(TargetPointer loaderAllocatorPointer);
TargetPointer GetStubHeap(TargetPointer loaderAllocatorPointer);
TargetPointer GetObjectHandle(TargetPointer loaderAllocatorPointer);
TargetPointer GetILHeader(ModuleHandle handle, uint token);
TargetPointer GetDynamicIL(ModuleHandle handle, uint token);
IEnumerable<LoaderHeapBlock> EnumerateLoaderHeapBlocks(TargetPointer loaderHeap);
IReadOnlyDictionary<LoaderAllocatorHeapType, TargetPointer> GetLoaderAllocatorHeaps(TargetPointer loaderAllocatorPointer);

DebuggerAssemblyControlFlags GetDebuggerInfoBits(ModuleHandle handle);
void SetDebuggerInfoBits(ModuleHandle handle, DebuggerAssemblyControlFlags newBits);
```

The `DebuggerAssemblyControlFlags` enum is defined as:
```csharp
[Flags]
enum DebuggerAssemblyControlFlags : uint
{
    DACF_NONE = 0x00,
    DACF_ALLOW_JIT_OPTS = 0x02,
    DACF_ENC_ENABLED = 0x08,
    DACF_IGNORE_PDBS = 0x20,
    DACF_CONTROL_FLAGS_MASK = 0x2E,
}
```

The `ClrModifiableAssemblies` enum (from `EEConfig::ModifiableAssemblies`) is defined as:
```csharp
enum ClrModifiableAssemblies : uint
{
    Unset = 0,
    None = 1,
    Debug = 2,
}
```

## Version 1

<!-- BEGIN GENERATED: usage contract=Loader version=c1 -->
### Data descriptors used

| Data Descriptor | Field | Type | Meaning |
| --- | --- | --- | --- |
| `AppDomain` | `AssemblyList` | `pointer` | Pointer to the list of assemblies in the application domain |
| `AppDomain` | `FriendlyName` | `pointer` | Pointer to the application domain's friendly name |
| `AppDomain` | `RootAssembly` | `pointer` | Pointer to the root assembly |
| `ArrayListBase` | `Count` | `uint32` | Total number of elements in the array list |
| `ArrayListBase` | `FirstBlock` | `pointer` | Pointer to the first array-list block |
| `ArrayListBlock` | `ArrayStart` | `pointer` | Pointer to the start of the block's element array |
| `ArrayListBlock` | `Next` | `pointer` | Pointer to the next array-list block |
| `ArrayListBlock` | `Size` | `uint32` | Number of elements available in the block |
| `Assembly` | `Error` | `pointer` | Pointer to the load exception, or null when no error occurred |
| `Assembly` | `IsCollectible` | `uint8` | Whether the assembly may be collected |
| `Assembly` | `IsDynamic` | `uint8` | Whether the assembly was created dynamically |
| `Assembly` | `IsLoaded` | `uint8` | Whether the assembly has finished loading |
| `Assembly` | `Module` | `pointer` | Pointer to the assembly's manifest module |
| `Assembly` | `NotifyFlags` | `uint32` | Debugger and profiler notification state for the assembly |
| `AssemblyBinder` | `AssemblyLoadContext` | `ObjectHandle` | Handle to the managed assembly load context |
| `CGrowableSymbolStream` | `Buffer` | `pointer` | Pointer to the in-memory symbol stream buffer |
| `CGrowableSymbolStream` | `Size` | `uint32` | Size of the symbol stream buffer in bytes |
| `DynamicILBlobTable` | *(type size)* | `uint32` | Size in bytes of each table entry |
| `DynamicILBlobTable` | `EntryIL` | `pointer` | Offset of the IL pointer within each dynamic IL table entry |
| `DynamicILBlobTable` | `EntryMethodToken` | `uint32` | Offset of the method token within each dynamic IL table entry |
| `DynamicILBlobTable` | `Table` | `pointer` | Address of the SHash table |
| `DynamicILBlobTable` | `TableSize` | `uint32` | Number of entries in the table |
| `EEConfig` | `ModifiableAssemblies` | `uint32` | Edit and Continue configuration represented by `ClrModifiableAssemblies` |
| `EETypeHashTable` | `Buckets` | `pointer` | Pointer to the hash table buckets |
| `EETypeHashTable` | `Count` | `uint32` | Number of elements in the hash table |
| `EETypeHashTable` | `VolatileEntryNextEntry` | `pointer` | Offset of the next-entry pointer within a hash table entry |
| `EETypeHashTable` | `VolatileEntryValue` | `pointer` | Offset of the value within a hash table entry |
| `InstMethodHashTable` | `Buckets` | `pointer` | Pointer to the hash table buckets |
| `InstMethodHashTable` | `Count` | `uint32` | Number of elements in the hash table |
| `InstMethodHashTable` | `VolatileEntryNextEntry` | `pointer` | Offset of the next-entry pointer within a hash table entry |
| `InstMethodHashTable` | `VolatileEntryValue` | `pointer` | Offset of the value within a hash table entry |
| `LoaderAllocator` | `DynamicHelpersStubHeap` | `pointer` | Dynamic-helper stub heap (optional, present when ReadyToRun dynamic-helper stubs are enabled) |
| `LoaderAllocator` | `ExecutableHeap` | `pointer` | Executable-code heap |
| `LoaderAllocator` | `FixupPrecodeHeap` | `pointer` | Fixup-precode heap (optional, present when fixup precodes are supported) |
| `LoaderAllocator` | `HighFrequencyHeap` | `pointer` | High-frequency allocation heap |
| `LoaderAllocator` | `LowFrequencyHeap` | `pointer` | Low-frequency allocation heap |
| `LoaderAllocator` | `NewStubPrecodeHeap` | `pointer` | New-stub-precode heap (optional, absent with portable entry points) |
| `LoaderAllocator` | `ObjectHandle` | `ObjectHandle` | Handle to the managed loader allocator object |
| `LoaderAllocator` | `ReferenceCount` | `uint32` | Reference count of the loader allocator |
| `LoaderAllocator` | `StaticsHeap` | `pointer` | Heap containing statics-related allocations |
| `LoaderAllocator` | `StubHeap` | `pointer` | Heap containing runtime stubs |
| `LoaderAllocator` | `VirtualCallStubManager` | `pointer` | Pointer to the virtual-call stub manager |
| `LoaderHeap` | `FirstBlock` | `pointer` | Pointer to the first loader-heap block |
| `LoaderHeapBlock` | `Next` | `pointer` | Pointer to the next loader-heap block |
| `LoaderHeapBlock` | `VirtualAddress` | `pointer` | Start address of the reserved virtual memory |
| `LoaderHeapBlock` | `VirtualSize` | `nuint` | Size of the reserved virtual memory region in bytes |
| `Module` | `Assembly` | `pointer` | Pointer to the containing assembly |
| `Module` | `AvailableTypeParams` | `pointer` | Pointer to the available type-parameter hash table |
| `Module` | `Base` | `pointer` | Base address of the module's loaded image |
| `Module` | `DynamicILBlobTable` | `pointer` | Pointer to the table of dynamically supplied IL bodies |
| `Module` | `FieldDefToDescMap` | `pointer` | Pointer to the field-definition-to-field-descriptor lookup map |
| `Module` | `FileName` | `pointer` | Pointer to the null-terminated UTF-16 module file name |
| `Module` | `Flags` | `uint32` | Module state and capability flags |
| `Module` | `GrowableSymbolStream` | `pointer` | Pointer to the in-memory symbol stream |
| `Module` | `InstMethodHashTable` | `pointer` | Pointer to the instantiated-method hash table |
| `Module` | `LoaderAllocator` | `pointer` | Pointer to the module's loader allocator |
| `Module` | `ManifestModuleReferencesMap` | `pointer` | Pointer to the manifest-module-reference lookup map |
| `Module` | `MemberRefToDescMap` | `pointer` | Pointer to the member-reference-to-descriptor lookup map |
| `Module` | `MethodDefToDescMap` | `pointer` | Pointer to the method-definition-to-method-descriptor lookup map |
| `Module` | `MethodDefToILCodeVersioningStateMap` | `pointer` | Pointer to the method-definition-to-IL-code-versioning-state lookup map |
| `Module` | `Path` | `pointer` | Pointer to the null-terminated UTF-16 module path |
| `Module` | `PEAssembly` | `pointer` | Pointer to the module's PE assembly |
| `Module` | `ReadyToRunInfo` | `pointer` | Pointer to the module's ReadyToRun information |
| `Module` | `SimpleName` | `pointer` | Pointer to the null-terminated UTF-8 module name |
| `Module` | `TypeDefToMethodTableMap` | `pointer` | Pointer to the type-definition-to-method-table lookup map |
| `Module` | `TypeRefToMethodTableMap` | `pointer` | Pointer to the type-reference-to-method-table lookup map |
| `ModuleLookupMap` | `Count` | `uint32` | Number of pointer-sized entries in this map segment |
| `ModuleLookupMap` | `Next` | `pointer` | Pointer to the next segment of the lookup map |
| `ModuleLookupMap` | `SupportedFlagsMask` | `nuint` | Mask of flag bits supported on lookup-map entries |
| `ModuleLookupMap` | `TableData` | `pointer` | Pointer to the first lookup-map entry |
| `PEAssembly` | `AssemblyBinder` | `pointer` | Pointer to the assembly binder |
| `PEAssembly` | `PEImage` | `pointer` | Pointer to the PE image |
| `PEImage` | `FlatImageLayout` | `pointer` | Pointer to the PEImage's flat PEImageLayout (used when there is no loaded layout, e.g. webcil images) |
| `PEImage` | `LoadedImageLayout` | `pointer` | Pointer to the loaded image layout |
| `PEImage` | `ProbeExtensionResult` | `ProbeExtensionResult` | Result of probing the image file extension |
| `PEImageLayout` | `Base` | `pointer` | Base address of the image layout |
| `PEImageLayout` | `Flags` | `uint32` | Image layout state flags |
| `PEImageLayout` | `Format` | `uint32` | Image format discriminator (PE or Webcil) |
| `PEImageLayout` | `Size` | `uint32` | Size of the image layout in bytes |
| `ProbeExtensionResult` | `Type` | `int32` | Kind of extension-probe result |
| `SystemDomain` | `GlobalLoaderAllocator` | `pointer` | Pointer to the global loader allocator |
| `SystemDomain` | `SystemAssembly` | `pointer` | Pointer to the system assembly |
| `VirtualCallStubManager` | `CacheEntryHeap` | `pointer` | Cache-entry heap (optional, present with virtual stub dispatch) |
| `VirtualCallStubManager` | `IndcellHeap` | `pointer` | Indirection-cell heap |

### Global variables used

| Global | Type | Meaning |
| --- | --- | --- |
| `AppDomain` | `pointer` | Pointer to the global application domain |
| `EEConfig` | `pointer` | Pointer to the runtime configuration |
| `SystemDomain` | `pointer` | Pointer to the global system domain |

### Contracts used

| Contract Name |
| --- |
| `EcmaMetadata` |
| `SHash` |
<!-- END GENERATED: usage contract=Loader version=c1 -->

### Contract Constants:
| Name | Type | Purpose | Value |
| --- | --- | --- | --- |
| `ASSEMBLY_NOTIFYFLAGS_PROFILER_NOTIFIED` | uint | Flag in Assembly NotifyFlags indicating the Assembly will notify profilers. | `0x1` |
| `DefaultDomainFriendlyName` | string | Friendly name returned when `AppDomain.FriendlyName` is null (matches native `DEFAULT_DOMAIN_FRIENDLY_NAME`) | `"DefaultDomain"` |
| `MaxWebcilSections` | ushort | Maximum number of COFF sections supported in a Webcil image (must stay in sync with native `WEBCIL_MAX_SECTIONS`) | `16` |
| `DebuggerInfoMask` | uint | Mask for the debugger info bits within the Module's transient flags | `0x0000FC00` |
| `DebuggerInfoShift` | int | Bit shift for the debugger info bits within the Module's transient flags | `10` |
| `DEBUGGER_ALLOW_JIT_OPTS_PRIV` | uint | Debugger allows JIT optimizations (shifted in transient flags) | `0x00000800` |

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

// Must stay in sync with native PEImageLayout::ImageFormat values.
private enum ImageFormat : uint
{
    PE = 0,
    Webcil = 1,
}
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
    ArrayListBase arrayList = // read ArrayListBase starting at appDomain + AppDomain::AssemblyList offset

    foreach (TargetPointer pAssembly in arrayList.Elements)
    {
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
        else if (assembly.IsLoaded)
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
                // window during collectible assembly creation.
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
    TargetPointer namePtr = appDomain + /* AppDomain::FriendlyName offset */;
    // Match native AppDomain::GetFriendlyName(): return "DefaultDomain" when pointer is null.
    if (namePtr == TargetPointer.Null)
        return "DefaultDomain";
    char[] name = // Read<char> from target starting at namePtr until null terminator
    return new string(name);
}

TargetPointer GetAppDomain()
{
    TargetPointer appDomainPointer = target.ReadGlobalPointer("AppDomain");
    return target.ReadPointer(appDomainPointer);
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

    // try to get loaded PE image (peImage), if not loaded return false

    TargetPointer peImageLayout = target.ReadPointer(peImage + /* PEImage::LoadedImageLayout offset */);
    if (peImageLayout == TargetPointer.Null)
    {
        // Images that are never mapped/loaded (e.g. a webcil ReadyToRun image on WASM) have no
        // loaded layout; their metadata lives in the flat layout (m_pLayouts[IMAGE_FLAT]).
        peImageLayout = target.ReadPointer(peImage + /* PEImage::FlatImageLayout offset */);
        if (peImageLayout == TargetPointer.Null)
            return false;
    }

    baseAddress = target.ReadPointer(peImageLayout + /* PEImageLayout::Base offset */);
    size = target.Read<uint>(peImageLayout + /* PEImageLayout::Size offset */);
    imageFlags = target.Read<uint>(peImageLayout + /* PEImageLayout::Flags offset */);
    return true;
}

bool IsModuleMapped(ModuleHandle handle)
{
    // try to get loaded PE image, if not loaded return false
    // try to get layout (peImageLayout)

    uint format = target.Read<uint>(peImageLayout + /* PEImageLayout::Format offset */);
    return /* Webcil images are never mapped; for PE images check the FLAG_MAPPED flag */;
}

TargetPointer ILoader.GetILAddr(TargetPointer peAssemblyPtr, int rva)
{
    return GetRvaData(peAssemblyPtr, rva, isNullOk: false);
}

TargetPointer ILoader.GetFieldAddressFromRva(TargetPointer peAssemblyPtr, int rva)
{
    return GetRvaData(peAssemblyPtr, rva, isNullOk: true);
}

private TargetPointer GetRvaData(TargetPointer peAssemblyPtr, int rva, bool isNullOk)
{
    if (rva == 0 && !isNullOk)
        return TargetPointer.Null;
    TargetPointer peImage = target.ReadPointer(peAssemblyPtr + /* PEAssembly::PEImage offset */);
    if(peImage == TargetPointer.Null)
        throw new InvalidOperationException("PEAssembly does not have a PEImage associated with it.");

    TargetPointer peImageLayout = target.ReadPointer(peImage + /* PEImage::LoadedImageLayout offset */);
    if(peImageLayout == TargetPointer.Null)
    {
        // Images that are never mapped/loaded (e.g. a webcil ReadyToRun image on WASM) have no
        // loaded layout; fall back to the flat layout (m_pLayouts[IMAGE_FLAT]).
        peImageLayout = target.ReadPointer(peImage + /* PEImage::FlatImageLayout offset */);
        if(peImageLayout == TargetPointer.Null)
            throw new InvalidOperationException("PEImage does not have a usable image layout associated with it.");
    }

    // Get base address and flags from PEImageLayout
    TargetPointer baseAddress = target.ReadPointer(peImageLayout + /* PEImageLayout::Base offset */);
    uint imageFlags = target.Read<uint>(peImageLayout + /* PEImageLayout::Flags offset */);

    bool isMapped = /* Webcil images are never mapped; for PE images check the FLAG_MAPPED flag */;

    uint offset;
    if (isMapped)
    {
        offset = (uint)rva;
    }
    else
    {
        offset = RvaToOffset(rva, peImageLayout);
    }
    return baseAddress + offset;
}

uint RvaToOffset(int rva, Data.PEImageLayout imageLayout)
{
    uint format = target.Read<uint>(imageLayout + /* PEImageLayout::Format offset */);
    if (format == (uint)ImageFormat.Webcil)
        return WebcilRvaToOffset(rva, imageLayout);

    TargetPointer baseAddress = target.ReadPointer(imageLayout + /* PEImageLayout::Base offset */);

    // find NT headers using DOS header
    uint dosHeaderLfanew = target.Read<uint>(baseAddress + /* ImageDosHeader::LfanewOffset */);
    TargetPointer ntHeadersPtr = baseAddress + dosHeaderLfanew;

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

        if (rva >= virtualAddress && rva < virtualAddress + sizeOfRawData)
        {
            sectionPtr = currentSectionPtr;
        }
    }
    if (sectionPtr == TargetPointer.Null)
    {
        throw new InvalidOperationException("Failed to read from image.");
    }

    // Convert RVA to file offset using section information
    uint sectionVirtualAddress = target.Read<uint>(sectionPtr + /* ImageSectionHeader::VirtualAddressOffset */);
    uint sectionPointerToRawData = target.Read<uint>(sectionPtr + /* ImageSectionHeader::PointerToRawDataOffset */);
    return (rva - sectionVirtualAddress) + sectionPointerToRawData;
}

uint WebcilRvaToOffset(int rva, Data.PEImageLayout imageLayout)
{
    if (rva < 0)
        throw new InvalidOperationException("Negative RVA in Webcil image.");

    TargetPointer headerBase = imageLayout.Base;
    // The webcil specification is found at docs/design/mono/webcil.md
    Data.WebcilHeader webcilHeader = // read WebcilHeader at headerBase
    uint webcilHeaderSize = /* sizeof(WebcilHeader) + 4 if the VersionMajor of the header is 1 or greater */; // Size is defined in webcil spec
    uint webcilSectionSize = /* sizeof(WebcilSectionHeader) */; // Size is defined in webcil spec

    ushort numSections = webcilHeader.CoffSections;
    if (numSections == 0 || numSections > MaxWebcilSections)
        throw new InvalidOperationException("Invalid Webcil section count.");

    TargetPointer sectionTableBase = headerBase + webcilHeaderSize;

    for (int i = 0; i < numSections; i++)
    {
        TargetPointer sectionPtr = sectionTableBase + (uint)(i * (int)webcilSectionSize);
        Data.WebcilSectionHeader section = // read WebcilSectionHeader at sectionPtr

        uint rvaUnsigned = (uint)rva;
        if (rvaUnsigned >= section.VirtualAddress)
        {
            uint offset = rvaUnsigned - section.VirtualAddress;
            if (offset < section.VirtualSize && offset < section.SizeOfRawData)
            {
                return offset + section.PointerToRawData;
            }
        }
    }

    throw new InvalidOperationException("Failed to resolve RVA in Webcil image.");
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
    // try to get loaded PE image, if not loaded return false

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

string GetSimpleName(ModuleHandle handle)
{
    TargetPointer simpleNameStart = target.ReadPointer(handle.Address + /* Module::SimpleName offset */);
    byte[] simpleNameBytes = // Read<byte> from target starting at simpleNameStart until null terminator
    return // convert to string, throw on invalid UTF-8
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

bool GetFileHeadersInfo(ModuleHandle handle, out uint timeStamp, out uint imageSize)
{
    timeStamp = 0;
    imageSize = 0;

    if (!TryGetLoadedImageContents(handle, out TargetPointer baseAddress, out _, out _))
        return false;
    TargetPointer ntHeadersPtr = baseAddress + // offset to NT headers
    timeStamp = // read from NT header
    imageSize = // read from NT header
    return true;
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
    uint tableDataOffset = (uint)/* ModuleLookupMap::TableData offset */;
    return new ModuleLookupTables(
        FieldDefToDescMap: target.ReadPointer(handle.Address + /* Module::FieldDefToDescMap */),
        ManifestModuleReferencesMap: target.ReadPointer(handle.Address + /* Module::ManifestModuleReferencesMap */),
        MemberRefToDescMap: target.ReadPointer(handle.Address + /* Module::MemberRefToDescMap */),
        MethodDefToDescMap: target.ReadPointer(handle.Address + /* Module::MethodDefToDescMap */),
        TypeDefToMethodTableMap: target.ReadPointer(handle.Address + /* Module::TypeDefToMethodTableMap */),
        TypeRefToMethodTableMap: target.ReadPointer(handle.Address + /* Module::TypeRefToMethodTableMap */),
        // Module::MethodDefToILCodeVersioningState is only present when the target was built
        // with code versioning (FEATURE_CODE_VERSIONING). When absent (e.g. on WASM) it is
        // treated as a null (empty) table.
        MethodDefToILCodeVersioningState: HasField(Module::MethodDefToILCodeVersioningState)
            ? target.ReadPointer(handle.Address + /* Module::MethodDefToILCodeVersioningState */)
            : TargetPointer.Null,
        TableDataOffset: tableDataOffset);
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
    bool isLoaded = target.Read<byte>(assembly + /* Assembly::IsLoaded*/) != 0;
    return isLoaded;
}

TargetPointer GetGlobalLoaderAllocator()
{
    TargetPointer systemDomainPointer = target.ReadGlobalPointer("SystemDomain");
    TargetPointer systemDomain = target.ReadPointer(systemDomainPointer);
    return systemDomain + /* SystemDomain::GlobalLoaderAllocator offset */;
}

TargetPointer GetSystemAssembly()
{
    TargetPointer systemDomainPointer = target.ReadGlobalPointer("SystemDomain");
    TargetPointer systemDomain = target.ReadPointer(systemDomainPointer);
    return target.ReadPointer(systemDomain + /* SystemDomain::SystemAssembly offset */);
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

IReadOnlyDictionary<LoaderAllocatorHeapType, TargetPointer> GetLoaderAllocatorHeaps(TargetPointer loaderAllocatorPointer)
{
    // Read LoaderAllocator data
    LoaderAllocator la = // read LoaderAllocator object at loaderAllocatorPointer

    // Always-present heaps
    Dictionary<LoaderAllocatorHeapType, TargetPointer> heaps = {
        [LoaderAllocatorHeapType.LowFrequencyHeap] = la.LowFrequencyHeap,
        [LoaderAllocatorHeapType.HighFrequencyHeap] = la.HighFrequencyHeap,
        [LoaderAllocatorHeapType.StaticsHeap] = la.StaticsHeap,
        [LoaderAllocatorHeapType.StubHeap] = la.StubHeap,
        [LoaderAllocatorHeapType.ExecutableHeap] = la.ExecutableHeap,
    };

    // Feature-conditional heaps: only included when the data descriptor field exists
    if (LoaderAllocator type has "FixupPrecodeHeap" field)
        heaps[LoaderAllocatorHeapType.FixupPrecodeHeap] = la.FixupPrecodeHeap;

    if (LoaderAllocator type has "NewStubPrecodeHeap" field)
        heaps[LoaderAllocatorHeapType.NewStubPrecodeHeap] = la.NewStubPrecodeHeap;

    if (LoaderAllocator type has "DynamicHelpersStubHeap" field)
        heaps[LoaderAllocatorHeapType.DynamicHelpersStubHeap] = la.DynamicHelpersStubHeap;

    // VirtualCallStubManager heaps: only included when VirtualCallStubManager is non-null
    if (la.VirtualCallStubManager != null)
    {
        VirtualCallStubManager vcsMgr = // read VirtualCallStubManager object at la.VirtualCallStubManager

        heaps[LoaderAllocatorHeapType.IndcellHeap] = vcsMgr.IndcellHeap;

        if (VirtualCallStubManager type has "CacheEntryHeap" field)
            heaps[LoaderAllocatorHeapType.CacheEntryHeap] = vcsMgr.CacheEntryHeap;
    }

    return heaps;
}

private sealed class DynamicILBlobTraits : ITraits<uint, DynamicILBlobEntry>
{
    public uint GetKey(DynamicILBlobEntry entry) => entry.EntryMethodToken;
    public bool Equals(uint left, uint right) => left == right;
    public uint Hash(uint key) => key;
    public bool IsNull(DynamicILBlobEntry entry) => entry.EntryMethodToken == 0;
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
    Contracts.ISHash shashContract = target.Contracts.SHash;
    DynamicILBlobTraits traits = new();
    /* To construct an SHash we must pass a DataType enum.
    We must be able to look up this enum in a dictionary of known types and retrieve a Target.TypeInfo struct.
    This struct contains a dictionary of fields with keys corresponding to the names of offsets
    and values corresponding to the offset values. Optionally, it contains a Size field.
    */
    SHash<uint, Data.DynamicILBlobEntry> shash = shashContract.CreateSHash<uint, Data.DynamicILBlobEntry>(target, dynamicBlobTablePtr, DataType.DynamicILBlobTable, traits)
    // LookupSHash returns null when no entry matches the token.
    Data.DynamicILBlobEntry? blobEntry = shashContract.LookupSHash(shash, token);
    return blobEntry?.EntryIL ?? TargetPointer.Null;
}

DebuggerAssemblyControlFlags GetDebuggerInfoBits(ModuleHandle handle)
{
    uint flags = // read Module::Flags at handle.Address + Flags offset
    return (DebuggerAssemblyControlFlags)((flags & DebuggerInfoMask) >> DebuggerInfoShift);
}

void SetDebuggerInfoBits(ModuleHandle handle, DebuggerAssemblyControlFlags newBits)
{
    uint currentFlags = // read Module::Flags at handle.Address + Flags offset
    uint debuggerInfoBitsMask = DebuggerInfoMask >> DebuggerInfoShift;
    uint updated = (currentFlags & ~DebuggerInfoMask) | (((uint)newBits & debuggerInfoBitsMask) << DebuggerInfoShift);

    bool jitOptDisabled = (updated & DEBUGGER_ALLOW_JIT_OPTS_PRIV) == 0 || (updated & PROF_DISABLE_OPTIMIZATIONS) != 0;
    // Set or clear IS_JIT_OPTIMIZATION_DISABLED accordingly.

    if ((updated & IS_ENC_CAPABLE) != 0)
    {
        ClrModifiableAssemblies modifiable = // read EEConfig::ModifiableAssemblies from g_pConfig
        if (modifiable != None)
        {
            bool encRequested = (newBits & DACF_ENC_ENABLED) != 0;
            bool setEnC = encRequested || (modifiable == Debug && jitOptDisabled);
            if (setEnC)
                updated |= IS_EDIT_AND_CONTINUE;
        }
    }

    // Write updated flags back to handle.Address + Flags offset
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

#### EnumerateLoaderHeapBlocks

```csharp
IEnumerable<LoaderHeapBlock> ILoader.EnumerateLoaderHeapBlocks(TargetPointer loaderHeap)
{
    TargetPointer block = target.ReadPointer(loaderHeap + /* LoaderHeap::FirstBlock offset */);
    HashSet<TargetPointer> visited = [];
    while (block != TargetPointer.Null)
    {
        if (!visited.Add(block))
            throw new InvalidOperationException();

        yield return new LoaderHeapBlock(
            target.ReadPointer(block + /* LoaderHeapBlock::VirtualAddress offset */),
            target.ReadNUInt(block + /* LoaderHeapBlock::VirtualSize offset */));
        block = target.ReadPointer(block + /* LoaderHeapBlock::Next offset */);
    }
}
```
