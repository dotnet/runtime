# Design for partitioned R2R modules with delayed cold code loading

## Overview

This doc outlines a design for a logical ReadyToRun (R2R) module which is distributed across a primary WebAssembly module and one or more auxiliary WebAssembly modules.

Loading the primary module makes the logical R2R module functional and usable. Methods whose compiled code has not yet arrived remain callable through R2R-to-interpreter thunks. Auxiliary modules can arrive later.

Auxiliary modules will hold the wasm method bodies for their aux methods, as well as their fixup data, GC info, and EH info.

Auxiliary modules export a function, `ActivateModule()`, which copies all its data into the shared memory, and fills in its allotted range in the global function table. Once the module is activated, the runtime can run fixups and enable executing R2R code for the methods in the auxiliary module.

The mapping for which methods have code in the auxiliary modules is in a separate module called the Auxiliary EntryPoints Module. This contains a second set of `MethodDefEntryPoints` and `InstanceMethodEntryPoints` sections. Until that module is downloaded, it's not possible to know which methods may have R2R code and which will always be interpreted.

Metadata and activation info about auxiliary modules is stored in the AuxiliaryModuleMetadata section in the primary module.

This design currently supports single-threaded hosting only. Auxiliary-module installation and method activation run on the runtime's single execution thread. Multithreaded hosting is outside the scope of this design.

## Flow

When a new method is discovered to be executed, the runtime will attempt to resolve R2R code for the method, first in the primary module, then in the auxiliary modules, if the Auxiliary EntryPoints Module information is available. If the method is determined to be in an auxiliary module, the runtime will check if the module has been activated, and activate the method if it is. Otherwise, flags on the PEP will track what is holding activation of the method: the Auxiliary EntryPoints Module, or the auxiliary module containing the code. Subsequent calls to the interpreted method will check activation status and attempt activation of the R2R code.

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

    if (!IsActivated(primary.AuxiliaryModuleMetadataHeader.EntryPointsModuleActivationStatus))
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
        metadata.MethodDefEntryPointsRVA, metadata.InstanceMethodEntryPointsRVA, pep.MethodDesc);
    if (entry is null)
    {
        EnsureInterpreterFallback(pep);
        return InterpOrR2R.Interp;
    }

    AuxiliaryModuleMetadataEntry auxModuleMetadata = FindAuxiliaryModuleMetadataForRuntimeIndex(entry.RuntimeFunctionIndex);
    if (!IsActivated(auxModuleMetadata.ActivationStatus))
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

### MethodDefEntryPoints and InstanceMethodEntryPoints

Two `MethodDefEntryPoints` and `InstanceMethodEntryPoints` exist, one in the primary module and the Auxiliary EntryPoints Module. Fixup lists for the entries in the Auxiliary EntryPoints Module exist in the auxiliary modules. The runtime must check both sections when the Auxiliary EntryPoints Module is downloaded. The primary module will allocate space for each section at the RVA designated in the Auxiliary Module Metadata. The entries for each method and their RuntimeFunctions index are present in the Auxiliary EntryPoints Module, but the fixup lists are placed in the auxiliary modules and copied during activation.

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

If activation is unsuccessful because fixup resolution fails, the `kPrefersInterpreterEntryPoint` flag is left set. Method activation should not be retried after a failure, and the runtime should treat the method as any other interpreter-only method.

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

The auxiliary entrypoints module contains the `MethodDefEntryPoints` and `InstanceMethodEntryPoints` for the aux methods, excluding the fixup cell info. These sections tell which index in the `RuntimeFunctions` table to find information for the method, as well as which fixups to resolve. A single logical `RuntimeFunctions` is spread across the primary and auxiliary modules. Using the length of the primary module's `RuntimeFunctions` range, and the `MethodCount` field in each Auxiliary module metadata entry (see below), the owning auxiliary module can be determined.

The EntryPoints sections will leave zeroed space for the fixup lists that will be filled in by the auxiliary module during activation. Fixups for methods from the auxiliary module cannot be run until the module is activated and these indices are filled in. This enables the EntryPoints Module to be smaller, and allow methods without R2R code to be identified as interpreter-only sooner.

The `MethodDefEntryPoints` sparse array will use backwards-distance encoding to point to each entry's fixup list. Each auxiliary module will be allocated space for all its encoded fixup lists in a blob before the array. This enables the auxiliary module's `ActivateModule()` function to simply copy a data element to a known location.

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

Similarly, the `InstanceMethodEntryPoints` hashtable will allocate contiguous memory to hold the EntryPoints records for all instance methods in an auxiliary module. The auxiliary module can then copy a data element to the correct offset to fill in the list of fixups.

```
Hash index                         Method-record storage

Bucket 0 ─── entry for A2 ──────────────┐
         └── entry for B1 ────────────────────────┐
                                        │         │
Bucket 1 ─── entry for A1 ──────────┐   │         │
                                    │   │         │
                                    ▼   │         │
                              ┌────────────────┐  │
                              │ A1 record      │  │
                              │ A2 record ◄────┼──┘
                              │ A3 record      │  │
                              └────────────────┘  │
                              Aux A's records     │
                                                  │
                              ┌────────────────┐  │
                              │ B1 record ◄────┼──┘
                              │ B2 record      │
                              └────────────────┘
                              Aux B's records
```

## Auxiliary Module Metadata

Metadata for each auxiliary module resides at a well-defined location in the primary module. This metadata enables the runtime to activate the module.

Header:

| Field | Meaning |
|---|---|
| `InstanceMethodEntryPointsRVA` | RVA of the auxiliary modules' InstanceMethodEntryPoints section |
| `MethodDefEntryPointsRVA` | RVA of the auxiliary modules' MethodDefEntryPoints section |
| `EntryPointsModuleActivationStatus` | Flag indicating whether the Auxiliary EntryPoints Module has completed activation and the auxiliary `InstanceMethodEntryPoints` and `MethodDefEntryPoints` tables are valid for reading. This also could be a flag on the ModuleDesc. |
| `PrimaryModuleMethodCount` | The count of functions (including funclets) that have code and a `RuntimeFunctions` entry in the primary module. This doesn't include the final `RuntimeFunctions` entry used for VirtualIP calculations. |
| `AuxiliaryModuleCount` | Count of auxiliary modules |

The Auxiliary Module Metadata table is a count-prefixed array of entries with the following fields:

| Field | Meaning |
|---|---|
| `FilenameRVA` | RVA of the string naming the auxiliary module. |
| `RelativeTableBase` | Primary-module-relative table index of the first slot assigned to the auxiliary module. |
| `MethodCount` | Number of method slots assigned to the auxiliary module, including funclets. Defines the number of entries in its `RuntimeFunctions` table and the length of its contiguous function-table range. |
| `ActivationStatus` | Flag indicating whether the auxiliary module has completed activation and its methods can be activated and the R2R code executed. This also could be a flag on the ModuleDesc. |

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

A logical R2R module can have two `MethodDefEntryPoints` and `InstanceMethodEntryPoints`. The runtime must attempt to resolve R2R code using the Auxiliary EntryPoints tables when present.

A module's `RuntimeFunctions` table may be only partially filled. VirtualIP lookup must use a range-aware binary search that only searches the ranges of activated modules, using the AuxiliaryModuleMetadata activation info to determine active ranges.

### Interpreter changes

The interpreter must implement the tracking and activation flow defined above.

## JavaScript Hosting Changes

The JavaScript host must:

1. Trigger auxiliary-module downloads, either eagerly or in response to method activation requests.
2. Instantiate each auxiliary module with the required imports.
3. Call that module's `ActivateModule` export.

## Implementation phases

### 1. Support delayed resolution of R2R code within the same module.

Using either call-counting in the interpreter or a timer in the host JavaScript code, force methods to be interpreted (even if R2R code exists for them in the primary module) before installing R2R code. This doesn't require any changes to crossgen2 or the webcil format.

### 2. Split some wasm function bodies into the cold module.

Leave all other R2R metadata (fixups, EH and GC info, etc) in the primary module, but move some cold wasm method bodies to a cold module. `ActivateModule` only has to update the global function table. Requires changes from crossgen2, the host, and the runtime.

### 3. Move metadata for aux methods into the auxiliary module.

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
