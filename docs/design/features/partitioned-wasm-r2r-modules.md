# Design for partitioned R2R modules with delayed cold code loading

## Overview

This doc outlines a design for a logical ReadyToRun (R2R) module which is distributed across a primary WebAssembly module and one or more auxiliary WebAssembly modules.

Loading the primary module makes the logical R2R module functional and usable. Methods whose compiled code has not yet arrived remain callable through R2R-to-interpreter thunks. Auxiliary modules can arrive later.

Auxiliary modules will hold the wasm method bodies for their aux methods, as well as their fixup data, GC info, and EH info.

Auxiliary modules export a function, `ActivateModule()`, which copies all its data into the shared memory, and fills in its allotted range in the global function table. Once the module is activated, the runtime can run fixups and enable executing R2R code for the methods in the auxiliary module.

The mapping for which methods have code in the auxiliary modules is in a separate module called the Auxiliary EntryPoints Module. This contains a second set of `MethodDefEntryPoints` and `InstanceMethodEntryPoints` sections. Until that module is downloaded, it's not possible to know which methods may have R2R code and which will always be interpreted.

Metadata and activation info about auxiliary modules is stored in the AuxiliaryModuleMetadata section in the primary module.

## Flow

When a new method is discovered to be executed, the runtime will attempt to resolve R2R code for the method, first in the primary module, then in the auxiliary modules, if the Auxiliary EntryPoints Module information is available. If the method is determined to be in an auxiliary module, the runtime will check if the module has been activated, and activate the method if it is. Otherwise, flags on the PEP will track what is holding activation of the method: the Auxiliary EntryPoints Module, or the auxiliary module containing the code. Subsequent calls to the interpreted method will check activation status and attempt activation of the R2R code. Checking for activation of the EntryPoints module and the auxiliary module should be as cheap as possible. In this design, it is a simple flag check in the WASM memory.

Auxiliary modules are downloaded eagerly unless it is determined better to wait and download on demand.

To reduce cold download size, the mapping of method to auxiliary module resides in a separate module, the Auxiliary EntryPoints Module, that is downloaded eagerly.

Once an auxiliary module is downloaded (we may also require the module to be "requested"), the host calls the exported `ActivateModule()` function, which copies over all data required for methods to be activated and executed, fills the global function table (table 0) with funcrefs, and marks itself as activated via a well-known location in the primary module's data. Once the module is activated, its methods can be activated.

### Pseudocode

The following code describes the interpreter flow for activation of aux methods. 2 new PEP flags and a field in the interpreter data are introduced to track activation status and cache lookups: `kPendingAuxiliaryEntryPoints`, `kPendingAuxiliaryModule`, and `InterpreterData.pAuxActivationFlag`.

The helper names below are illustrative only. Entry-point lookup only reads the method's entry-point record; it does not run fixups or publish a callable target. Status addresses refer to cells in shared memory. `EnsureInterpreterFallback` ensures interpreter bytecode and data exist and leaves `kPrefersInterpreterEntryPoint` set.

```csharp
InterpOrR2R DiscoverMethod(PEP pep)
{
    var primary = pep.GetModule();
    EntryPoint entry = LookupEntryPoint(
        primary.MethodDefEntryPoints, primary.InstanceMethodEntryPoints, pep.MethodDesc);
    if (entry is not null)
    {
        return ActivateOrFallBackToInterpreter(pep, entry);
    }

    if (primary.AuxiliaryModuleMetadataHeader.EntryPointsModuleActivationStatus == 0)
    {
        EnsureInterpreterFallback(pep);
        pep.InterpreterData.pAuxActivationFlag = &primary.AuxiliaryModuleMetadataHeader.EntryPointsModuleActivationStatus;
        pep.SetFlag(kPendingAuxiliaryEntryPoints);
        return InterpOrR2R.Interp;
    }

    return ResolveAuxiliaryMethod(pep);
}

InterpOrR2R ResolveAuxiliaryMethod(PEP pep)
{
    // Only called after the auxiliary entry-point tables are activated.
    pep.ClearFlag(kPendingAuxiliaryEntryPoints);
    pep.ClearFlag(kPendingAuxiliaryModule);

    AuxiliaryModuleMetadataHeader metadata = pep.GetModule().AuxiliaryModuleMetadataHeader;
    EntryPoint entry = LookupEntryPoint(
        metadata.MethodDefEntryPointsInfoRVA, metadata.InstanceMethodEntryPointsRVA, pep.MethodDesc);
    if (entry is null)
    {
        EnsureInterpreterFallback(pep);
        return InterpOrR2R.Interp;
    }

    AuxiliaryModuleMetadataEntry auxModuleMetadata = FindAuxiliaryModuleMetadataForRuntimeIndex(entry.RuntimeFunctionIndex);
    if (auxModuleMetadata.ActivationStatus == 0)
    {
        EnsureInterpreterFallback(pep);
        pep.InterpreterData.pAuxActivationFlag = &auxModuleMetadata.ActivationStatus;
        pep.SetFlag(kPendingAuxiliaryModule);
        return InterpOrR2R.Interp;
    }

    return ActivateOrFallBackToInterpreter(pep, entry);
}
```

On subsequent invocations (but not continuation resumption), the interpreter checks the pending flags and their cached status flag address before executing the method body:

```csharp
InterpOrR2R ReenterDiscoveredMethod(PEP pep)
{
    if (!pep.HasFlag(kPrefersInterpreterEntryPoint))
    {
        return InterpOrR2R.R2R;
    }

    if (!pep.HasFlag(kPendingAuxiliaryEntryPoints) && !pep.HasFlag(kPendingAuxiliaryModule))
    {
        return InterpOrR2R.Interp;
    }

    if (!IsActivated(pep.InterpreterData.pAuxActivationFlag))
    {
        return InterpOrR2R.Interp;
    }

    return ResolveAuxiliaryMethod(pep);
}
```

## Partitioned Data

### RuntimeFunctions

A single logical `RuntimeFunctions` section exists, spread across the primary module and auxiliary modules. The primary module contains contiguous entries for the methods it contains, as well as the last entry for the last function in the last aux module (and the unwind data it points to), and zeroed memory for auxiliary modules to fill in during activation. The last entry in the section allows the runtime to determine the total VirtualIP allocation required for the virtual module. When a module is activated, it fills in its allotted range in the RuntimeFunctions table for activation of its methods.

VirtualIP lookup must use a range-aware binary search that only searches the ranges of activated modules, using the AuxiliaryModuleMetadata activation info to determine active ranges.

```text
AuxiliaryModuleMetadata                         RuntimeFunctions table
                                                Index  Owning module
                                               ┌─────┬───────────────┐
Header:                                      ┌─┤  0  │ Primary       │
  PrimaryModuleMethodCount = 3 ─────────────►┤ │  1  │ Primary       │
  AuxiliaryModuleCount = 2                   └─┤  2  │ Primary       │
                                               ├─────┼───────────────┤
Entries:                                     ┌─┤  3  │ Aux A         │
  Aux A                                      │ │  4  │ Aux A         │
    MethodCount = 4 ────────────────────────►┤ │  5  │ Aux A         │
    RelativeTableBase = 3                    └─┤  6  │ Aux A         │
                                               ├─────┼───────────────┤
  Aux B                                      ┌─┤  7  │ Aux B         │
    MethodCount = 3 ────────────────────────►┤ │  8  │ Aux B         │
    RelativeTableBase = 7                    └─┤  9  │ Aux B         │
                                               └─────┴───────────────┘
```

The entire table is allocated space in the primary module. Slots 0-2 are populated, slots 3-8 zeroed, and slot 9 (plus its unwind data) is populated for VirtualIP sizing. The method in slot 9 is still "owned" by Auxiliary module B, and is included in its MethodCount.

### MethodDefEntryPoints and InstanceMethodEntryPoints

Each `MethodDefEntryPoints` section (one for each assembly in the R2R image), as well as the image-wide `InstanceMethodEntryPoints` section will have a partial entry in each  (shared by all assemblies in the R2R image), one in the primary module and the Auxiliary EntryPoints Module. Fixup lists for the entries in the Auxiliary EntryPoints Module exist in the auxiliary modules. The runtime must check both sections when the Auxiliary EntryPoints Module is downloaded. The primary module will allocate space for each section at the RVA designated in the Auxiliary Module Metadata. The entries for each method and their RuntimeFunctions index are present in the Auxiliary EntryPoints Module, but the fixup lists are placed in the auxiliary modules and copied during activation.

### ImportSection

The `ImportSection` (fixups) will be split among primary and auxiliary modules. Each module will have a range of `READYTORUN_IMPORT_SECTION` descriptors for the methods contained within each module. The primary module will allocate space in the WebCil `ImportSection` for auxiliary modules to copy their ImportSection entries, as well as the fixup signatures, non-zero import cells, and auxiliary data to their respective RVAs in the data section. Methods in the auxiliary module may reference fixups in the primary module, but auxiliary modules may not reference fixups owned by other auxiliary modules.

## Primary Module

### Data layout

The primary module defines a data segment large enough to hold all relevant data for the logical module, including data supplied by auxiliary modules.

Regions whose contents belong exclusively to auxiliary modules are initially filled with zeros. Zero bytes in a data segment are stored literally, so they cost file size before compression but compress very well.

### Callable methods and thunks

The primary module contains:

- All string-referenced thunks.
- Shared R2R-to-interpreter thunks, one per signature shape needed by auxiliary methods. An unactivated aux method's PEP points at the thunk for its signature.
- An element segment defining the primary module's own callable methods and thunks. The slot range reserved for methods in auxiliary modules is left empty and is filled during activation of the auxiliary module.
- `MethodDefEntryPoints` and `InstanceMethodEntryPoints` for the methods in the primary module.

### Method activation behavior

If activation is unsuccessful because fixup resolution fails, the `kPrefersInterpreterEntryPoint` flag is left set. Method activation should not be retried after a failure. After failed activation, the runtime should treat the method as any other interpreter-only method.

## Auxiliary Modules

An auxiliary module contains:

- The function bodies of the contained methods.
- The RuntimeFunctions table for the contained methods.
- The fixup data for the contained methods.
- An Element segment of funcrefs to its contained methods to fill its allotted range in table 0 during activation.
- Any other data required by the module but able to be removed from the primary module.

It imports:

- The primary module's memory and function table.
- The primary module's base memory address and base table index.
- (future, optional) functions from the primary module, such as R2R helpers or commonly called managed methods. This enables direct calls to these methods in the primary module.

It exports:

- `ActivateModule()`, a method which copies the module's data into its allotted range in the primary module's memory, installs funcrefs to all of the module's compiled methods and funclets into its allotted range of table 0, and marks itself as activated via a well-known flag in the primary module's memory. Note that installing a funcref does not mean the method is activated and ready to be executed.

## Auxiliary EntryPoints Module

The auxiliary entrypoints holds information about what methods have code in any auxiliary module. There is one AuxiliaryEntryPointsModule per logical R2R module. It contains one or more `MethodDefEntryPoints` sections (one for each assembly in the R2R image), and one `InstanceMethodEntryPoints` (for the entire image). Together, these hold a mappings from method to `RuntimeFunctions` index. Each section for the aux methods, excluding the fixup cell info. These sections tell which index in the `RuntimeFunctions` table to find information for the method, as well as which fixups to resolve. A single logical `RuntimeFunctions` is spread across the primary and auxiliary modules. Using the length of the primary module's `RuntimeFunctions` range, and the `MethodCount` field in each Auxiliary module metadata entry (see below), the owning auxiliary module can be determined.

The EntryPoints sections in the Auxiliary EntryPoints Module will leave space for the fixup lists that will be filled in by the auxiliary module during activation. To enable auxiliary modules to activate before the Auxiliary EntryPoints Module, this space should not be written overwritten during activation of the Auxiliary EntryPoints Module. Fixups for methods from the auxiliary module cannot be run until the module is activated and these indices are filled in. This enables the EntryPoints Module to be smaller, and allow methods without R2R code to be identified as interpreter-only sooner.

### Auxiliary MethodDefEntryPoints format

The auxiliary `MethodDefEntryPoints` section for each assembly may have code spread across multiple auxiliary modules. The AuxiliaryEntryPointsModule will hold the sparse array metadata (header and block-index array), as well as the `RuntimeFunctions` index and flags for each entry, and backwards-distance-encoded offset to the method's fixup list. The actual fixup list will be owned by the Auxiliary Module that holds the code for that method. Each auxiliary module will be allocated contiguous space for all its encoded fixup lists in a blob before the array. This enables the auxiliary module's `ActivateModule()` function to simply copy a data element to a known location.

```
Lower addresses
┌───────────────────────────────────┐
│ Aux A fixup lists                 │ ◄── installed by Aux A
├───────────────────────────────────┤
│ Aux B fixup lists                 │ ◄── installed by Aux B
├───────────────────────────────────┤
│ Sparse-array header               │ ◄── R2R directory points HERE
│ Block-index array                 │
│ Tree nodes and entrypoint records │
│   [method index + flags][distance]│
└───────────────────────────┬───────┘
                            │
             backward reference to the relevant list
Higher addresses
```

### Auxiliary InstanceMethodEntryPoints format

The AuxiliaryEntryPointsModule contains the `InstanceMethodEntryPoints` hash table metadata and the method records it points to. Each record contains the method signature, the encoded `RuntimeFunctions` index and flags, and, when fixups are required, a backward distance to its fixup list. Each auxiliary module has a contiguous reserved region before its method records for its encoded fixup lists. The auxiliary module copies only those lists into the reserved region during activation; the signatures, indices, flags, and distances are supplied by the AuxiliaryEntryPointsModule.

```
Hash index (supplied by AuxiliaryEntryPointsModule)

Bucket 0 ─── [low hash][relative offset] ────┐
         └── [low hash][relative offset] ────┼───────────────────────────────────────────────┐
Bucket 1 ─── [low hash][relative offset] ─┐  │                                               │
                                          │  │  Aux A storage                                │   Aux B storage
                                          │  │  Lower addresses                              │   Lower addresses
                                          │  │  ┌───────────────────────────────────┐        │   ┌───────────────────────────────────┐
                                          │  │  │ Reserved fixup-list space         │◄─┐     │   │ Reserved fixup-list space         │◄─┐
                                          │  │  │ (filled by Aux A)                 │  │     │   │ (filled by Aux B)                 │  │
                                          │  │  ├───────────────────────────────────┤  │     │   ├───────────────────────────────────┤  │
                                          └──┼─►│ A1 signature                      │  │     └──►│ B1 signature                      │  │
                                             │  │ RuntimeFunctions index + flags    │  │         │ RuntimeFunctions index + flags    │  │
                                             │  │ backward distance                 │──┤         │ backward distance                 │──┤
                                             │  ├───────────────────────────────────┤  │         ├───────────────────────────────────┤  │
                                             └─►│ A2 signature                      │  │         │ B2 signature                      │  │
                                                │ RuntimeFunctions index + flags    │  │         │ RuntimeFunctions index + flags    │  │
                                                │ backward distance                 │──┘         │ backward distance                 │──┘
                                                └───────────────────────────────────┘            └───────────────────────────────────┘
                                                Higher addresses                                 Higher addresses

Method records are supplied by AuxiliaryEntryPointsModule.
Each backward distance identifies that record's list within the reserved space.
The illustrated records have fixups; their index-and-flags field is encoded as
(RuntimeFunctions index << 2) | 3. Records without fixups omit the distance.
```

## Auxiliary Module Metadata

Metadata for each auxiliary module resides at a well-defined location in the primary module, probably in a new section. This metadata enables the runtime to activate the module.

Header:

| Field | Meaning |
|---|---|
| `InstanceMethodEntryPointsRVA` | RVA of the auxiliary modules' InstanceMethodEntryPoints section |
| `MethodDefEntryPointsInfoRVA` | RVA of an array of (IMAGE_DATA_DIRECTORY `CorHeader`, IMAGE_DATA_DIRECTORY `MethodDefEntryPointsRVA`) tuples that allow the runtime to resolve an assembly's auxiliary MethodDefEntryPoints section |
| `EntryPointsModuleActivationStatus` | Flag indicating whether the Auxiliary EntryPoints Module has completed activation and the auxiliary `InstanceMethodEntryPoints` and `MethodDefEntryPoints` tables are valid for reading. This also could be a flag on the ModuleDesc. |
| `PrimaryModuleMethodCount` | The count of functions (including funclets) that have code and a `RuntimeFunctions` entry in the primary module. This doesn't include the final `RuntimeFunctions` entry used for VirtualIP calculations. |
| `AuxiliaryModuleCount` | Count of auxiliary modules |

Following the header is the Auxiliary Module Metadata table. The table is an array of entries with the following fields:

| Field | Meaning |
|---|---|
| `FilenameRVA` | RVA of the string naming the auxiliary module. |
| `RelativeTableBase` | Primary-module-relative table index of the first slot assigned to the auxiliary module. |
| `MethodCount` | Number of method slots assigned to the auxiliary module, including funclets. Defines the number of entries in its `RuntimeFunctions` table and the length of its contiguous function-table range. |
| `ActivationStatus` | Flag indicating whether the auxiliary module has completed activation and its methods can be activated and the R2R code executed. This also could be a flag on the ModuleDesc. |

The `MethodDefEntryPointsInfoRVA` points to data identical to the format of the `ComponentAssemblies` section. Once filled in by the Auxiliary EntryPoints Module, the `CorHeader` will point to the same location in the assembly as the entry in the `ComponentAssemblies` section (right now crossgen2 emits these as zeros and relies on correlated order with the `ManifestMetadata` assembly names - this ordering must be preserved). The `MethodDefEntryPointsRVA` will point to the auxiliary `MethodDefEntryPoints` section for that assembly. All this data would reside within the Auxiliary Module Metadata section.

### Alternative data format

Instead of putting bookkeeping for the Auxiliary EntryPoints Module inside the Auxiliary Module Metadata section, new sections could be created for the image-wide `AuxiliaryInstanceMethodEntryPoints` section and per-assembly auxiliary `AuxiliaryMethodDefEntryPoints` sections. The AuxiliaryModuleMetadata header could remove the `InstanceMethodEntryPointsRVA` and `MethodDefEntryPointsInfoRVA` fields.

### Index relationships

Each auxiliary module's allotted range in the function table is contiguous and has a one-to-one correspondence with its local `RuntimeFunctions` entries, including funclets.

A method's logical `RuntimeFunctions` index is equal to the sum of `PrimaryModuleMethodCount` and preceding auxiliary modules' `MethodCount`, plus its local index within that range.

A method's index in the global function table (table 0) is the `__table_base` of the primary module plus the method's logical-module-relative `RuntimeFunctions` index. That is, `<index in table 0> = __table_base + <resolved RuntimeFunctions index>` where the resolved RuntimeFunctions index is the index resolved in the MethodDefEntryPoints or InstanceMethodEntryPoints table.

The count of entries in the auxiliary module's `RuntimeFunctions` table must be equal to the `AuxiliaryModuleMetadata`'s `MethodCount` field for that module.

This relationship between `RuntimeFunctions` index and function table index (relative to __table_base) must also hold for the primary module.

## Compiler Changes

### Assign methods to physical modules

The compiler must decide which methods belong in the primary module and which belong in auxiliary modules.

The initial implementation will support a **hot/cold split**:

- A PGO- or heuristic-selected hot set goes into the primary module.
- Remaining methods go into a single auxiliary module.

For demonstration purposes, the initial heuristic can classify **helpers and public non-generic methods as hot**, with everything else classified as cold. This is intended as a simple implementation for the proof of concept, not as a long-term solution.

The design also permits future arrangements such as:

- Hot/warm/cold modules.
- A hot primary module plus per-assembly auxiliary modules.

Auxiliary modules use indexed filenames, for example:

```text
app.r2r.1.wasm
app.r2r.2.wasm
```

### Partition tables and generate fallback thunks

The compiler must:

- Split the `RuntimeFunctions` entries and GC and EH info across auxiliary modules, keeping data for each auxiliary module contiguous, and emitting the last entry of the table to the primary module.
- Arrange function-table indices so that the primary module's R2R methods are contiguous and each auxiliary module's R2R methods occupy a contiguous range.
- Include funclet slots in each AuxiliaryModuleMetadata's `MethodCount` and preserve the one-to-one correspondence between that range and its local `RUNTIME_FUNCTION` records.
- Generate R2R-to-interpreter thunks for every signature shape needed by auxiliary methods. The primary module holds one thunk per shape, and an unactivated auxiliary method's PEP names the thunk for its signature. The primary module's element segment does not reserve a thunk per auxiliary slot.
- Emit in each auxiliary module an element segment that fills its slot range in the imported table 0 with that module's methods and funclets, so `ActivateModule` can install every funcref.

### Partition data and emit auxiliary metadata

The compiler must identify memory regions belonging to auxiliary modules and communicate that ownership to the Wasm module writers. Memory owned by the auxiliary modules must not include any target locations of any fixups that may run before the auxiliary module is loaded. The auxiliary module should be able to use `memory.init` instructions to fill data, and must not overwrite any fixup cells that have been resolved.

The compiler must also:

- Generate auxiliary-module metadata at a well-known location discoverable by the runtime.
- Generate `MethodDefEntryPoints` and `InstanceMethodEntryPoints` sections for the primary module and Auxiliary EntryPoints Module.
- Adjust code-generation rules so auxiliary code can be loaded safely.
- Retain IL for any method assigned to an auxiliary module.

## Runtime Changes

### Multiple runtime-function tables

A logical R2R module can have two `MethodDefEntryPoints` per assembly and two `InstanceMethodEntryPoints` per image. The runtime must attempt to resolve R2R code using the Auxiliary EntryPoints Module data once it has been activated.

A module's `RuntimeFunctions` table may be only partially filled. VirtualIP lookup must use a range-aware binary search that only searches the ranges of activated modules, using the AuxiliaryModuleMetadata activation info to determine active ranges.

### Interpreter changes

The interpreter must implement the tracking and activation flow defined above.

## JavaScript Hosting Changes

The JavaScript host must:

1. Trigger auxiliary-module downloads (including the Auxiliary EntryPoints Module), either eagerly or in response to method activation requests.
2. Instantiate each auxiliary module with the required imports.
3. Call that module's `ActivateModule` export.

## Implementation phases

V1 will focus on single-threaded, composite mode support. The designs shouldn't make multithreaded WASM or non-composite mode incompatible, but it will disabled for those configurations in the initial implementation.

### 1. Support delayed resolution of R2R code within the same module.

Using either call-counting in the interpreter or a timer in the host JavaScript code, force methods to be interpreted (even if R2R code exists for them in the primary module) before installing R2R code. This doesn't require any changes to crossgen2 or the webcil format.

### 2. Split InstanceMethodEntryPoints and MethodDefEntryPoints sections out

Move entries of the `InstanceMethodEntryPoints` and `MethodDefEntryPoints` sections into the Auxiliary EntryPoints Module during object emission in crossgen2. Implement activation of the Auxiliary EntryPoints Module in the host. Update the runtime to check both once the Auxiliary EntryPoints Module is activated. All other data is left in the primary module and activation of the Auxiliary EntryPoints Module will also mark auxiliary modules as activated so that activation of those methods can complete.

### 3. Split some wasm function bodies into the cold module.

Leave all other R2R metadata (fixups, EH and GC info, etc) in the primary module, but move some cold wasm method bodies to a cold module. `ActivateModule` only has to update the global function table. Requires changes from crossgen2, the host, and the runtime.

### 4. Move metadata for aux methods into the auxiliary module.

Move the `RuntimeFunctions` table entries and fixups to the auxiliary module, and have method activation resolve this info using the Auxiliary Module Metadata entries.

## Appendix

### Direct-call dependency sets

Direct calls within an auxiliary module will not be implemented in V1, and this section is irrelevant until they are implemented. It is left for completeness.

Activating method A must also complete the required fixups for any method B that A calls directly, even if B was not independently requested. All the methods that are direct-called by A or direct called by any of those methods, recursively, are considered the direct-call dependency set for root method A. Activation of all methods in a dependency set must succeed for the root method to be successfully activated. Subsets of the dependency set of A may succeed even if other subsets fail - they are not necessary all-or-none.

An aux method may be **unvisited** (it has no interpreter bytecode or R2R code), **interpreted** (`kPrefersInterpreterEntryPoint` is set), **activation-in-progress** (currently being activated, and present in a visited set during recursive activation of direct-called methods), or **activated** (`kPrefersInterpreterEntryPoint` not set). Encountering an activation-in-progress dependency must record the dependency rather than infinitely recursing and restarting its activation.

`READYTORUN_FIXUP_MethodEntry_ReadyToRun` already requests target-method fixup processing. These are implemented as Eager imports which run at module load. This does not work for aux methods whose data and fixups are not present in the primary module. These fixups for aux modules must run either during auxiliary module activation, or be modified to be run as pre-code fixups for methods that have direct calls (which requires handling cycles of these fixups - e.g. M1 direct calls M2 which direct calls M1).

Direct calls within an auxiliary module are not supported in V1. They can be implemented later, subject to this activation requirement.

Direct calls from the primary module to an auxiliary module are not supported.

Direct calls from one auxiliary module to another are not supported.

Direct calls from an auxiliary module to the primary module are possible through complex collaboration between the host, exported methods in the primary module, and required imports in the auxiliary module. The required complexity of an implementation is not justified for V1, and is not supported.

> [!NOTE]
> This document was written and edited with assistance from GitHub Copilot.
