# Design for partitioned R2R modules with delayed cold code loading

## Overview

This doc outlines a design for a logical ReadyToRun (R2R) module which is distributed across a primary WebAssembly module and one or more auxiliary WebAssembly modules.

Loading the primary module makes the logical R2R module functional and usable. Methods whose compiled code has not yet arrived remain callable through R2R-to-interpreter thunks. Auxiliary modules can arrive later.

During the first execution of a method whose body is in the auxiliary module (an "aux method"), a new fixup kind, `READYTORUN_FIXUP_Auxiliary_Module` identifies the method as an aux method. The method's `_pActualCode` is set to the interpreter thunk with the correct signature, and `kPrefersInterpreterEntryPoint` is set, as well as a new flag, `kAuxiliaryModulePending`. This flag marks the method as requiring activation once the module has been marked activated. An alternative tracking of methods that require activation which creates a queue of pending methods and drains the queue incrementally once the module is activated is described at the end of this document. The runtime will also mark the module as pending activation.

Once the auxiliary module is downloaded and marked as pending activation, the host calls the exported `ActivateModule` function, which copies over all data required for methods to be activated and executed, fills the global function table (table 0) with funcrefs, and marks itself as activated via a well-known location in the primary module's data. Once the module is activated, its methods can be activated.

The interpreter will check for the `kAuxiliaryModulePending` flag as it checks `kPrefersInterpreterEntryPoint`. If set, it will also check the well-known location in the primary module to see if the auxiliary module has been activated. If the module has been activated, the method will be activated. Note this could cause significant work to be done when running fixups. That issue is what the pending method queue + draining alternative seeks to address.

The runtime must support N `RuntimeFunctions` tables per logical R2R module, with one table per physical Wasm file, spreading the EH and GC info across the auxiliary modules. Alternatively, the primary module must hold all entries. In this case, the Virtual IP field must be present for each RuntimeFunction entry, though GC and EH info could be zero and filled in by the auxiliary module.

This design currently supports single-threaded hosting only. Auxiliary-module installation and method activation run on the runtime's single execution thread. Multithreaded hosting is outside the scope of this design.

## Primary Module

### Data layout

The primary module defines a data segment large enough to hold all relevant data for the logical module, including data supplied by auxiliary modules.

Regions whose contents belong exclusively to auxiliary modules are initially filled with zeros. Zero bytes in a data segment are stored literally, so they cost file size before compression but compress very well.

### Callable methods and thunks

The primary module contains:

- All string-referenced thunks.
- Shared R2R-to-interpreter thunks, one per signature shape needed by auxiliary methods. An unactivated aux method's PEP points at the thunk for its signature.
- An element segment defining the primary module's own callable methods and thunks. The slot range reserved for methods in auxiliary modules is left empty and is filled during activation of the auxiliary module.
- `MethodDefEntryPoints` and `InstanceMethodEntryPoints` for the entire logical module.

### Aux Method Fixups

For an aux method, the first fixup of its fixup list is an auxiliary-module marker `READYTORUN_FIXUP_Auxiliary_Module` identifying the module that holds its compiled code. In the initial implementation, this is assumed to be the only auxiliary module. Future implementations may append an ID to indicate which auxiliary module the method body belongs to.

The method's entry in `MethodDefEntryPoints` (or `InstanceMethodEntryPoints`) is unchanged and points to the method's fixup list at a fixed address in the primary module. Before the auxiliary module loads, the fixup list holds only the marker's bytes, followed by reserved zero-filled space for the rest of the list. The complete list, including the marker, is stored in the auxiliary module's passive data. During auxiliary module activation, the module rewrites the entire fixup list, with identical bytes for the marker. No separate lookup structure or per-module fixup-list address is required.

The compiler must encode the complete fixup list, with the marker as its first fixup, and then emit the marker's bytes and the reserved space. The tail of the list is delta-encoded relative to the marker, so it must not be encoded independently.

### Method activation behavior

If activation is unsuccessful because fixup resolution fails, the `kAuxiliaryModulePending` flag is cleared and the `kPrefersInterpreterEntryPoint` flag is left set. Method activation should not be retried after a failure, and the runtime should treat the method as any other interpreter-only method.

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

- `ActivateModule()`, a method which copies the module's data into its allotted range in the primary module's memory, installs funcrefs to all of the module's compiled methods and funclets into its allotted range of table 0, and marks itself as loaded via a well-known flag in the primary module's memory. Note that installing a funcref does not mean the method is activated and ready to be executed.

## Auxiliary-Module Metadata

Metadata for each auxiliary module resides at a well-defined location in the primary module. This metadata enables the runtime to activate the module.

The Auxiliary module metadata table has a header with 2 fields, the `VirtualIPSpan` (the total virtual IP span of the entire logical module, divided by 2), and the `AuxiliaryModuleCount` (the number of auxiliary modules). The runtime reserves a virtual IP range of `VirtualIPSpan * 2` for the module without needing all `RuntimeFunction` sections to be present.

Following the header are `AuxiliaryModuleCount` entries with the following data:

| Field | Meaning |
|---|---|
| Filename RVA | RVA of the string naming the auxiliary module. |
| First method table index | Primary-module-relative table index of the first slot assigned to the auxiliary module. |
| `MethodCount` | Number of runtime-function slots assigned to the auxiliary module, **including funclet slots**. Defines the number of entries in its `RuntimeFunctions` table and the length of its contiguous function-table range. |
| Auxiliary `RuntimeFunctions` table RVA | RVA of that physical module's runtime-function table. The table contains `MethodCount` records. |
| `INITIALIZED_FLAG` | Initialization state, described below. |
| `REQUEST_COUNT` | Initially `0`; incremented when R2R metadata identifies a method in this auxiliary module as required. |

### Module Initialization states

| `INITIALIZED_FLAG` | State |
|---|---|
| `0` | Auxiliary module has not been loaded. |
| `1` | Auxiliary module is activated. Data has been copied and the function table range it is allotted has been filled. |

### Index relationships

Each auxiliary module's allotted range in the function table is contiguous and has a one-to-one correspondence with its local `RUNTIME_FUNCTION` records, including funclets:

```text
auxiliaryRelativeIndex =
    primaryModuleRelativeTableIndex - firstMethodTableIndex

actualFunctionTableIndex =
    primaryModuleTableBase + primaryModuleRelativeTableIndex

auxiliaryRuntimeFunctionIndex = auxiliaryRelativeIndex
```

`auxiliaryRelativeIndex` must be in `[0, MethodCount)`.

### Direct-call dependency sets

Activating method A must also complete the required fixups for any method B that A calls directly, even if B was not independently requested.

A method's activation state is **not activated** (`_pActualCode` not set, or `kAuxiliaryModulePending` set), **activation-in-progress** (in a visited set local to the activation walk or a new flag on the PEP is set), or **activated** (`kAuxiliaryModulePending` clear and `kPrefersInterpreterEntryPoint` not set). Encountering an activation-in-progress dependency records the dependency rather than recursively restarting its activation.

Mutually dependent methods, such as A calling B and B calling A, are completed as an activation set. A and B must be activated at the same time and must both succeed activation, or they both fail.

`READYTORUN_FIXUP_MethodEntry_ReadyToRun` already requests target-method fixup processing. These are implemented as Eager imports which run at module load. This does not work for aux methods whose data and fixups are not present in the primary module. These fixups for aux modules must run either during auxiliary module activation, or be modified to be run as pre-code fixups for methods that have direct calls (which requires handling cycles of these fixups - e.g. M1 direct calls M2 which direct calls M1).

Direct calls within an auxiliary module are permitted subject to this activation requirement. Direct calls from the primary module to an auxiliary module are not supported.

Direct calls from one auxiliary module to another are not supported.

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

- Split the `RuntimeFunctions` table into per-physical-module tables.
- Arrange function-table indices so that the primary module's R2R methods are contiguous and each auxiliary module's R2R methods occupy a contiguous range.
- Include funclet slots in each auxiliary module's `MethodCount` and preserve the one-to-one correspondence between that range and its local `RUNTIME_FUNCTION` records.
- Generate R2R-to-interpreter thunks for every signature shape needed by auxiliary methods. The primary module holds one thunk per shape, and an unactivated auxiliary method's PEP names the thunk for its signature. The primary module's element segment does not reserve a thunk per auxiliary slot.
- Emit in each auxiliary module an element segment that fills its slot range in the imported table 0 with that module's methods and funclets, so `ActivateModule` can install every funcref.

### Partition data and emit auxiliary metadata

> [!NOTE]
> Data partitioning is not strictly required for correctness. Fully populating the primary module's data would work, and data partitioning may be postponed to later implementation phases.

The compiler must identify memory regions belonging to auxiliary modules and communicate that ownership to the Wasm module writers. Memory owned by the auxiliary modules must not include any target locations of any fixups that may run before the auxiliary module is loaded. The auxiliary module should be able to use `memory.init` instructions to fill data, and must not overwrite any fixup cells that have been resolved.

The compiler must also:

- Emit each auxiliary method's fixup list as a marker followed by reserved zero-filled space in the primary module, with the complete list in the auxiliary module's passive data. Lay the auxiliary methods' lists out contiguously so a single copy initializes them. The marker's bytes must be identical in both copies.
- Generate auxiliary-module metadata at a well-known location discoverable by the runtime.
- Adjust code-generation rules so auxiliary code can be loaded safely.
- Retain IL for any method assigned to an auxiliary module.

## Runtime Changes

### Multiple runtime-function tables

A logical R2R module can have multiple `RuntimeFunctions` sections. An auxiliary module's table becomes eligible for lookup when its `INITIALIZED_FLAG` is non-zero.

`MethodCount`, including funclet slots, defines the length of the auxiliary table.

### Deferred method activation

The runtime recognizes an auxiliary-module marker in the first fixup of a method. Whether the auxiliary module is activated or not, the runtime sets `_pActualCode` to point to the interpreter thunk slot in the global function table, sets interpreter preference, and sets `kAuxiliaryModulePending` on the PEP. If the module has been activated, it then activates the method synchronously. If not, the method is interpreted. On subsequent calls, the `kAuxiliaryModulePending` flag tells the interpreter to check the auxiliary module's activation state to see if the method can be activated.

Resolving which auxiliary module the method belongs to can be done in a few ways.
- Search MethodDefEntryPoints/InstanceMethodEntryPoints, determine what auxiliary module owns the range the entry resides in through the AuxiliaryModuleMetadata.
- Add slots to the range allotted to the auxiliary module for interpreter thunks for each signature in the auxiliary module. `_pActualCode` then already points somewhere in the range of the auxiliary module, so we avoid the MethodDefEntryPoints/InstanceMethodEntryPoints lookup.
- Limit the auxiliary module count to 1. This will be done in the initial implementation.

Once activated, `_pActualCode` is set to the method's global table index and `kAuxiliaryModulePending` and `kPrefersInterpreterEntryPoint` are cleared, and `_pInterpreterData` is cleared.

### Host notification

When an auxiliary module's `REQUEST_COUNT` changes from **`0` to `1`**, a hosting hook notifies the host that a method from that module has been requested.

CoreCLR calls
```
void OnAuxiliaryModuleRequest(const char *auxModuleName, int moduleBase, int functionTableBase);
```

The hook supplies the primary module's base table index and base memory offset which should be enough to identify the primary module and enable instantiation of the auxiliary module. Future support for direct calls between modules may require a more complex import/export scheme.

## JavaScript Hosting Changes

The JavaScript host must:

1. Trigger auxiliary-module downloads, either in response to `OnAuxiliaryModuleRequest` or through a more proactive download policy.
2. Instantiate each auxiliary module with the required imports.
3. Call that module's `ActivateModule` export.

Methods are then activated lazily on their next call; the host does not drive method activation.

### Gaps

- **Calls from an auxiliary module into another auxiliary module** remain unsupported.
- **First-call latency.** Fixup work and closure resolution happen on the calling thread at an arbitrary point after the auxiliary module is downloaded. The queue alternative below spreads this work out.

## Alternative: deferred-activation queue drained incrementally

Instead of activating on first call, the runtime keeps a list of methods that need activating and drains it in batches. This trades more runtime and host machinery for control over when activation cost is paid.

### Changes relative to on-demand activation

- **Method loading when the module is not loaded.** Instead of setting `kAuxiliaryModulePending`, add the `MethodDesc` to a deferred-activation list. The `_pActualCode` thunk is left in place, interpreter preference is still set, and `REQUEST_COUNT` and `OnAuxiliaryModuleRequest` behave as before. Methods discovered after module arrival activate synchronously and are never queued.
- **Initialization states.** `INITIALIZED_FLAG` gains a third value.

  | `INITIALIZED_FLAG` | State |
  |---|---|
  | `0` | Auxiliary module has not been loaded. |
  | `1` | Auxiliary module is loaded; deferred fixup processing remains. |
  | `2` | The module is loaded and its deferred-activation queue is drained. This does not mean that every method in the module has been activated. |

  Methods in other modules that have not yet loaded do not prevent the transition to `2`. Methods discovered in state `2` activate synchronously, so discovery does not reopen the queue.
- **Activation procedure.** Unchanged, except that the runtime drains the queue instead of waiting for first calls. Methods completed early, including dependencies completed during synchronous activation or by a first call, are removed from the queue. The helper's first-call path can still be kept as a fast path for hot methods.
- **Per-method state.** Not activated, activation-in-progress, and activated are tracked explicitly rather than by a visited set or PEP flag.
- **Activation failure.** A queued method whose fixups fail is rejected for R2R and stays interpreted. It is removed from the queue and does not count as remaining work, so the host's drain loop terminates.

### Incremental activation entrypoint

CoreCLR exposes:

```c
int DoRuntimeDeferredModuleActivationWork(int methodsToProcess);
```

The argument is a heuristic budget of root methods to process, not a bound on time. Activating one method can also activate its direct-call dependency closure, so a batch can exceed the budget.

The return value counts remaining methods pending activation for loaded auxiliary modules.

Deferred methods belonging to modules that have not loaded are excluded from this count. A return value of `0` means there is no remaining activation work for currently loaded modules; it does not mean that all auxiliary modules have arrived or that every method in them has been activated.

### JavaScript host changes

After calling `ActivateModule`, the host calls `DoRuntimeDeferredModuleActivationWork` in batches, yielding to normal execution between batches, until it returns `0` for the currently loaded modules. A JavaScript-side timer is one possible driver. When another auxiliary module arrives, repeat the sequence for the newly available work.

### Trade-offs

| | On-demand (default) | Queue and batching (alternative) |
|---|---|---|
| Runtime state | One loaded state, `kAuxiliaryModulePending` flag | Queue, request counting, three module states |
| Host involvement | Host only loads and installs modules | Host drives the batch loop |
| Activation cost | On the first call to each method | Spread across batches by the host |
| Warm-up control | None without an added prefetch pass | Eager, host-scheduled |
| Compiler and R2R format | No new helper or thunk. One new PEP flag and a check in the existing helper. | Existing helper |


## Appendix: Method activation PEP state table

The table covers every combination of four values:

- **`pep._pActualCode`:** what the PEP dispatches to: `0` (never prepared), `Thunk` (R2R-to-interpreter thunk), or `R2R` (an index naming valid R2R code; for an auxiliary method, its own slot in table 0).
- **`pep.kPreferInterp`:** `kPrefersInterpreterEntryPoint`.
- **`pep.kAuxPending`:** `kAuxiliaryModulePending`.
- **`AuxModuleMetadata.INITIALIZED_FLAG`:** the owning auxiliary module's `INITIALIZED_FLAG` is `1`, meaning `ActivateModule` has run and table 0 holds valid function references to the auxiliary module's bodies. It is meaningless for methods that are not in an auxiliary module.

| # | `_pActualCode` | `kPreferInterp` | `kAuxPending` | `ModInit` | Status | Meaning |
|---|---|---|---|---|---|---|
| 1 | `0` | 0 | 0 | 0 | Valid | Method not yet prepared. The owning module is not loaded, or the method is not in an auxiliary module. |
| 2 | `0` | 0 | 0 | 1 | Same as 1 | Not yet prepared. `ModInit` is irrelevant until the marker fixup is seen. |
| 3 | `0` | 0 | 1 | 0 | Invalid | Marker processing sets the thunk and the flags together. |
| 4 | `0` | 0 | 1 | 1 | Invalid | As row 3. |
| 5 | `0` | 1 | 0 | 0 | Invalid | Interpreter preference is set when the method is prepared, which also sets `_pActualCode`. |
| 6 | `0` | 1 | 0 | 1 | Invalid | As row 5. |
| 7 | `0` | 1 | 1 | 0 | Invalid | As row 3. |
| 8 | `0` | 1 | 1 | 1 | Invalid | As row 3. |
| 9 | `Interpreter thunk` | 0 | 0 | 0 | Invalid | A thunk means the method is interpreted, so interpreter preference must be set. |
| 10 | `Interpreter thunk` | 0 | 0 | 1 | Invalid | As row 9. |
| 11 | `Interpreter thunk` | 0 | 1 | 0 | Invalid | Pending implies interpreter preference. |
| 12 | `Interpreter thunk` | 0 | 1 | 1 | Invalid | As row 11. |
| 13 | `Interpreter thunk` | 1 | 0 | 0 | Valid | Interpreter-only method: no R2R code exists, or it was rejected. An auxiliary method should never be in this state. |
| 14 | `Interpreter thunk` | 1 | 0 | 1 | Valid | For aux methods, this means the method activation failed and the method will always be interpreted. |
| 15 | `Interpreter thunk` | 1 | 1 | 0 | Valid | Auxiliary method whose module has not loaded. Runs in the interpreter and waits for `ActivateModule`. |
| 16 | `Interpreter thunk` | 1 | 1 | 1 | Valid | Auxiliary method whose module is activated. Either no call has happened since module activation, or activation is currently running. |
| 17 | `R2R` | 0 | 0 | 0 | Valid | R2R code in the primary module, or a method outside an auxiliary module. |
| 18 | `R2R` | 0 | 0 | 1 | Same as 17 | Also the state of an activated auxiliary method, where `ModInit` must be `1`. |
| 19 | `R2R` | 0 | 1 | 0 | Invalid | Pending means not activated, so `_pActualCode` cannot name R2R code. |
| 20 | `R2R` | 0 | 1 | 1 | Invalid | As row 19. |
| 21 | `R2R` | 1 | 0 | 0 | Invalid | Interpreter preference is cleared when R2R code is valid and activated. |
| 22 | `R2R` | 1 | 0 | 1 | Invalid | As row 21. |
| 23 | `R2R` | 1 | 1 | 0 | Invalid | `_pActualCode` cannot name an auxiliary slot before the module loads, because the slot is still empty. |
| 24 | `R2R` | 1 | 1 | 1 | Transient | Inside activation, after `_pActualCode` is repointed and before the flags are cleared. Single-threaded hosting makes it unobservable. |

Valid, distinct states and how an auxiliary method moves between them:

```text
1 (unprepared)
  → 13   no R2R code, or R2R code rejected (not an auxiliary method)
  → 17   R2R code found in the primary module
  → 15   marker seen, module not loaded
  → 16 → 18   marker seen, module loaded: activated synchronously
15 → 16   ActivateModule runs (funcrefs installed, ModInit becomes 1)
16 → 18   first call or direct-call closure activation succeeds
16 → 14   activation fails: the method is always interpreted on future calls
```


> [!NOTE]
> This document was formatted and edited from the supplied design text by GitHub Copilot.
