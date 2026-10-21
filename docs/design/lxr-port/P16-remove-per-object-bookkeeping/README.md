# P16: Removing per-object bookkeeping — design for the sweep, finalization and pinning

Status: design only. Nothing is built. No product code changed. The accepted port stays at `e8d2d23`.

This page answers four follow-up questions about [P15](../P15-pauses-and-full-gcs/README.md). It then gives a design for each fix. Labels: **Measured** (a run), **Code** (read in the source, not run), **Inferred** (my conclusion; can be wrong), **Open** (not yet known; must be checked before we build).

## Short answers

1. **Why do we keep a record for every object, and can we remove it?** The Java LXR, as built, keeps none: it finds object starts in the RC table. Our port has the RC table and three more records. Two of them mean the same thing, and the third (a size table) exists because of our own rule 4. We can remove two of the three extra records. We must keep one bit per slot for young objects (section 1).
2. **Why is `Sweep` not like the Java LXR's?** I found no design reason. The pieces we need already exist in the port (the list of allocated blocks, the list of mature candidates, the lazy sweep). The pause code does not use them. Finalization and pinning need new designs; sections 2 to 4 give them. **Decision taken (by you):** finalizable objects go in a registry list, and, as in the Java LXR, weak and finalizable objects are kept alive in RC pauses and handled only in tracing pauses (section 3).
3. **What is the shared counter, and where is the coalesced RC?** The counter is not part of the RC algorithm. It is a lock-like gate around block queries. My earlier claim that the Java LXR flips its epoch only in the pause, and we do not, was wrong: both flip only inside the pause (section 5).
4. **Why does no block become empty, and why do we differ from the Java LXR?** The to-space for the copy is too small at the start of the pause. One failed copy stops all copying in that pause, so all survivors stay where they are. We differ because the port admits heap in explicit ranges, and each admitted range needs its per-object tables committed (section 7).

## 1. The per-object records

### What exists

Code: our port keeps these tables, indexed by every 8-byte slot of the Immix heap (`GetObjectStorageIndex`, `LxrVirtualArray`):

| Table | Bytes per 8 bytes of heap | Who uses it |
|---|---|---|
| `m_extents` (size of each object) | 8 | `GetObjectExtent`, `Reclaim`, `FindContainingObject`, `SweepCycles`, `WalkObjects`, `RetireForwardedSources`, `GetHeapStatistics`, `ProcessTraceObject`, `TryCopy*` |
| `m_trace_scans` (a pointer per slot) | 8 | tracing of large objects |
| `m_flags` in `lxr_gc.cpp` (finalizer and pin flags) | 4 | `QueueFinalizer`, `LeaseFinalizer`, `RegisterForFinalization`, `IsPinned`, `AddRoot`, `DidCopy`, and others |
| `m_trace_locks` | 1 | tracing |
| `m_live_objects` (one bit per slot) | 0.125 | `ReclaimNursery`, `FindNextOccupiedSlot` and everything that walks the heap |
| `ValidObject` side-metadata plane (one bit per slot) | 0.125 | `FindContainingObject` |

The first five add up to about 21 bytes of table for every 8 bytes of heap, or 2.6 bytes per byte. The tables are committed with the admitted heap. For 74 MiB of Immix space this is about 195 MiB (arithmetic, not a measurement).

Measured: peak resident memory of the requests program with an 84 MiB heap is 370 MiB for our port (non-moving), 79 MiB for the .NET Workstation GC, and 101 MiB for the Java G1 collector with a 52 MiB heap. I did not separate the tables from other costs.

### The three overlapping records

**What the Java LXR uses.** Nothing beyond the RC table. Measured: the Java LXR library in WSL was built with features `default` and `ix_always_defrag`. The `vo_bit` feature is off (it is off by default, and the OpenJDK binding turns it on only with `MMTK_VO_BIT=1`). Code (`rc_work.rs`, `SweepDeadCycles::process_block`): the Java LXR finds objects by walking the RC table at 8-byte steps. A non-zero count means "an object starts here". A count of 1 on a line start with a "straddle" mark means "the continuation of an object from the previous line". That is the whole record. Young objects have count 0, and nobody needs to find them: they are found only by following pointers.

**What our port keeps in addition to the RC table** (code, `reference_count.cpp`, `immix_allocation.cpp`, `immix_block.cpp`):

- The `ValidObject` plane: one bit per slot. It is set at allocation (`SetValidObject(object, 1)`). It is cleared for a whole range when the allocator takes a hole or a block (`InitializeFieldLogRange`). It is cleared for a whole block when the block is released (`TryReleaseIfRcDead`). **This is the bulk-clear design, and it already works.** It is the "VO bit" of MMTk, which the Java LXR build does not use.
- `m_live_objects`: a second bit per slot. It means the same thing as `ValidObject`. The two are doubles of each other.
- `m_extents`: the size of each object. The code can already read the size from the object: `GetObjectSize` calls `object->GetSize()` (used in `ProcessIncrement`, `Death`, `Sweep`, `AddRoot`).

The per-dead-object work of `ReclaimNursery` (P15, finding 1A) exists only to keep `m_live_objects`, `m_extents` and two byte counters in step. The `ValidObject` plane needs none of it.

### Why each record exists

I read the commit history. The roadmap and the commit messages are the only record; no design note explains these choices.

| Record | Introduced | Reason found | Is the reason valid? |
|---|---|---|---|
| `m_extents` | `ac1da112dd8` (P5.5, "metadata-only cycle sweep") | Our own **operating rule 4** in the roadmap: "Sweeping never parses objects." P5.5 reclaims dead cycles "using extents and metadata without parsing dead layouts" | Rule 4 is stricter than the Java LXR. Java `process_dead_object` calls `unmark_straddle_object(o)`, which reads the size from the dead object. The Java walk at 8-byte steps matches rule 4. The "never parses" part does not match the Java code. We can relax the rule for the background cycle sweep. |
| `m_live_objects` | `e7e6594f89c` (nonmoving speed and scale) | Inferred. The commit message says only "speed and scale". The bit makes `FindNextOccupiedSlot` skip empty slots 64 at a time. It is the index that `ReclaimNursery`, the heap walks and `FindNextObject` use | It duplicates `ValidObject`. `ValidObject` can serve as the index. |
| `m_live_bytes`, `m_reclaimed_bytes` | `ac1da112dd8` | The P5.5 study "verifies real allocated/reclaimed bytes and identity", so they were a test oracle for the cycle sweep. They also feed `GetTotalBytesInUse`, `GetCurrentObjSize` and the diagnostic metrics 3 and 4 | As a test oracle: keep them only in a test build. For the .NET memory APIs: an estimate from block counts is enough. The standard .NET GC also returns an estimate that includes dead objects not yet collected. |
| `ValidObject` | P2.1 side-metadata inventory | "valid-object/allocation state", used by `FindContainingObject` for interior pointers | **Keep.** See below. |

**Why `ValidObject` must stay.** An *interior pointer* is a pointer to the inside of an object, not to its start. In C# you create one with `ref int x = ref array[5]`, with `fixed`, or with a `Span<T>` over an array. The JIT may keep only that address in a register or on the stack, and no pointer to the array itself. When a pause scans the stack, the runtime reports such a slot as "interior" (`GC_CALL_INTERIOR`). The collector must keep the whole array alive. So it must turn the address into the start of the object that contains it. Only then can it increment that object's RC. `FindContainingObject` does this.

How does the lookup work? For an object that was already promoted, the RC table can answer: go backward from the address to the nearest slot with a non-zero count that is not a continuation line. For a **young** object the count is still 0 (the pause is just about to increment it), so the RC table shows nothing. And a root can point into a young object: a method that just allocated an array and takes a `Span` over it is a normal case. So for young objects we need another record of where objects start. That record is the `ValidObject` bit: one bit per 8 bytes, 1.5% of the heap.

The Java VM does not have this problem. Its stack slots hold exact object pointers (from the compiler's maps), never a pointer into the middle of an object. So it never asks "which object contains this address?". That is why the Java LXR needs no start record for young objects, and we do. Option: a hint of the first object start per line (1 byte per 256 bytes of heap, 0.4%), then walk forward by object size. It uses less memory but needs parsing of objects at the root scan. I recommend keeping the bit: it already works and has no parsing.

**What else the extent served** (code), and its replacement:

| Need | Where | Replacement |
|---|---|---|
| "Is this address the start of a live object?" | `GetObjectExtent(object) != 0` in `IsMarked`, `ResolveForwarded`, `TryCopy*` | `ValidObject` bit, plus the RC count where a pause needs "live" |
| Size of an object whose body was overwritten | `RetireForwardedSources`: a forwarded source has its first field overwritten by the forwarding pointer, so array length and string length are lost | Read the size from the copy (same size), or save it in the retire list when the copy is made |
| Size of a dead object in a cycle | `SweepCycles` | Read the size from the object, as the Java LXR does. Open: for objects of collectible types, check that the type data is still valid at that time. `ScanDecrements` keeps the loader allocator alive through a count, which suggests it is, but this needs a test |
| Walk the heap for profilers and ETW | `DiagWalkHeap` and related | Walk `ValidObject` bits and skip objects with RC 0. Stale bits are filtered by the RC count (see below) |
| Totals for `GetHeapStatistics`, `GetMarkedBytes` | walk all slots | Keep counters at block state changes. Compute the rest on demand, outside the pause |

### Stale `ValidObject` bits

If we stop clearing bits per dead object, a bit stays set until its range is reused or its block is freed. Is that safe?

- **Interior pointer lookup** (`FindContainingObject`): the search goes backward from the address to the nearest set bit. A live object O had its range cleared when the hole was taken, and only its own start bit was set afterward. So no stale bit can lie between the start of O and an address inside O. The search finds the start of O. Inferred; this needs a test with random interior addresses.
- **Heap walk:** a stale bit can point to a dead object whose lines are partly reused. A walker must not read it. Rule: at a pause, after increments are done, a walker takes only bits whose RC count is above 0. A dead young object has count 0. A dead mature object has count 0 after its `Death`. Open: check that every reuse path also clears the RC bytes of the reused range.
- **Mature death:** `Death` already runs per dead mature object (the Java LXR also does per-object work there), and it already clears one `ValidObject` bit (`reference_count.cpp`, `SetValidObject(object, 0)`). This is work per mature death, not per young allocation.

### The result

After this change, young allocation costs: set one `ValidObject` bit (already done), no extent store, no `live` bit, no shared counters. The pause does not visit dead young objects. This is what the Java LXR does.

## 2. The sweep: background, and only the blocks that need it

### The Java LXR

- Pause: nursery blocks are released as a count (`bulk_release_blocks`). Blocks that got a surviving object are marked "promoted in place" and stay.
- After the pause (in the background): only blocks on the "possibly dead mature blocks" list are swept (`SweepBlocksAfterDecs`). Free lines are found by the allocator when it takes a block (`TryFindNextHole`).

### What we have

Code: the port already has:

- `m_allocated_block_storage`: one flag per block, set when a block is given to a mutator. `ReclaimNursery` uses it.
- `m_mature_candidates`: one flag per block, set in `Death`.
- A lazy sweep (`Sweep(guard)` with a guard, started by `DecrementsDrained` and `SubmitMatureSweep`) that already skips blocks without the candidate flag.
- Hole search in the allocator (`ImmixAllocationManager::TryFindNextHole`).

### The design

1. **In the pause:** loop over the allocated-block list only (not all blocks). A block that had no promoted object goes back to the free pool in one operation. The RC range of such a block is zero by definition (no object in it was incremented). The block's `ValidObject` range is cleared in bulk. A block that had a promoted object keeps its state. Do not scan its lines. Do not call `SetReusable` with a line count.
2. **After the pause (background):** sweep the blocks with `m_mature_candidates` set. This is the existing lazy path.
3. **Remove from the pause:** `Sweep(nullptr)` over all blocks; the 128-line scan; `GetHeapStatistics`.
4. **Policy input:** `GetMaturePages` loops over all blocks. Replace it with a counter that block transitions update (blocks in use), as the Java LXR does with `HEAP_AFTER_GC` and `num_clean_blocks_released_lazy`.

Checked (C2 in section 9): the allocator takes holes only from blocks in state `Reusable` with enough free lines. So the work of `SetReusable` must move to the background sweep or to the allocator's first touch of a block. It must not be dropped.

Measured, share of all CPU samples: `Sweep` 5.0% (binary trees), 9.9% (requests, of which `SweepCycles` is 6.3%), 0.7% (list suffix). `GetHeapStatistics` 4.1% (requests). The gain is bounded by these numbers plus the wait in `Drain` for lazy work.

## 3. Finalization: a list, not a heap walk

### What we do now

Code: each pause, `ExecuteScan` walks every registered object up to three times (`LeaseFinalizer`, `QueueFinalizer`, `TracePendingFinalizer`). The state "has a finalizer", "pending" and "suppressed" is a flag in `m_flags`, one flag word for every 8 bytes of heap.

### What the Java LXR does

Code (`global.rs`): in an RC pause the weak, final and phantom reference closures are switched off (`disable_unnecessary_buckets`). Weak references and their referents are kept alive by an extra increment (`discover_reference`). They are processed only in the pauses that trace (final mark or full).

### Design (decided: a registry list, with the Java LXR timing)

- **Registration:** `RegisterForFinalization` appends the object to a list and gives the object one RC increment. So the registry keeps it alive. Whether finalization is suppressed is a bit in the object header (CoreCLR has `BIT_SBLK_FINALIZER_RUN`). No table is needed.
- **Weak handles in RC pauses:** the handle table is scanned as strong roots, as the Java LXR does (`discover_reference`, `requires_weak_root_scanning`). A target of a weak handle lives until a tracing pause decides.
- **RC pauses do no finalization or weak-handle work.** The three heap walks are gone.
- **Tracing pauses (final mark, full):** walk the registry list, not the heap. An entry whose object is unmarked is moved to the ready queue, traced (so that its children survive), and kept alive until its finalizer ends. An entry whose object is marked stays. Then the short weak handles whose target is unmarked are cleared, and after finalization the long weak handles. The dead-cycle sweep must skip registered objects.
- **What is removed:** `LeaseFinalizer`, `QueueFinalizer`, `TracePendingFinalizer` as heap walks, and the registered, pending and suppressed flags in `m_flags`.

Consequences of the decision (see section 9, D3): an object with a finalizer, or with only a weak handle to it, is handled only after the next tracing pause. The delay is the time between two SATB cycles. An explicit `GC.Collect()` is a full pause, so it still works at once.

Open: `GcScanHandles` and the weak scans already work over the handle table, not the heap. Check that they need no per-object flags.

## 4. Pinning: a short list, not a flag for every object

This is new to LXR; the Java LXR switches its pinning buckets off. So there is no model to copy. Here is a design that avoids a walk of all objects.

### Where pins come from

Code: (1) stack roots and handle roots reported with `GC_CALL_PINNED` (`AddRoot`); (2) pinned handles; (3) objects of the pinned object heap.

### What we do now

Code: `AddRoot` sets an `ObservedPin` flag in `m_flags`. `LeaseFinalizer` clears the pin and copy flags of every object, every pause. `RetainPinnedMatureBlocks` walks every registered object to find pinned ones.

### Design

- **Pin list per pause.** While roots are scanned, each pinned root is appended to a small list (a vector per worker, merged at the end). Number of entries = number of pinned roots. This is usually small.
- **One byte per block: "this block has a pinned object in this pause."** It is set when a pin is added. The copy decision does this: read the block byte. If it is 0, copy as usual. If it is 1, look the object up in the pin list (sorted, or a small hash set). Only blocks with pins pay for a lookup.
- **Mature blocks:** for each entry in the pin list, mark its block as "retained" (do not evacuate). This replaces `RetainPinnedMatureBlocks`.
- **End of pause:** clear the block bytes by walking the pin list. The cost follows the pin count.
- **Pinned object heap:** allocate these objects in dedicated blocks. A block attribute says "never copy, never defrag". A POH object needs no per-object flag.
- **Copy log:** the `CopiedThisPause` flag exists for the survivor callbacks (`DiagWalkSurvivorsWithType`). Replace it with a list of copied objects, produced only when a profiler asks for it.

Checked (D4 in section 9): `IsPinned` is an engine callback that is used only inside pauses, so a pin list is enough.

## 5. The shared counter in `IsNursery` — where is the coalesced RC?

**It is not in the RC algorithm.** The coalesced RC is the logging write barrier, plus the pause code that reads the logged slots: increment the current target, decrement the old target (`ProcessIncrementWork`, `ProcessDecrements`). The block bookkeeping is a different step.

When a young object gets its first increment, the pause must do this: "is this object's block a nursery block? If yes, mark the block as 'promoted in place'". The Java LXR does this with two plain byte loads and an early return if the block is already marked (`Block::is_nursery`, `set_as_in_place_promoted`). It uses a global "phase epoch" and a per-block epoch byte.

**When does the epoch change?** Code, in both systems, only inside the pause, twice per pause:

| | At the start of the pause | At the end of the pause |
|---|---|---|
| Java LXR (`global.rs`) | `gc_pause_start` calls `Block::update_global_phase_epoch` | after `release_rc`, calls it again |
| Our port | `Suspend` calls `StartGcPause`, after the runtime has suspended the mutators | `ReleaseEpoch` calls `ReleaseGcPause` |

So my earlier statement ("the Java LXR flips only in the pause, so it needs no gate") implied that our flip is outside the pause. That was wrong. Both flip inside the pause.

Our port does the same block check in `ImmixBlockManager::IsNursery`, but wraps it in a `BlockOperationScope`.

Code (`immix_block.cpp`, `BeginBlockOperation`, `BeginPhaseTransition`):

- `BlockOperationScope` adds 1 to `m_active_operations` with sequentially-consistent ordering, and subtracts 1 at the end. Many threads use the same counter. This is a reader gate.
- The epoch flip (`BeginPhaseTransition`) sets a flag and waits until the counter is 0. So the gate keeps the flip from running while a query is in progress.

**What could the gate protect?** Code (`RunPause`): before a pause starts, the engine calls `Drain` when no trace is active. `Drain` waits until all lazy work is idle. When a concurrent trace is active, it calls only `DrainEpoch`, so concurrent trace workers may still run when the pause starts. Inferred: the gate is meant for those workers, or it is a defensive design from the unit-test phase (P2.x), where block queries were tested alone. The Java LXR has the same concurrent workers and has no gate. Open: find out if any worker queries a block during the flip. A debug assertion at the flip ("no operation in flight") would answer this.

Measured: `IsNursery` is 10.2% of all CPU samples in the list-suffix run (self time), the largest single item in the increment work.

### Design

1. Remove the gate from read-only queries (`IsNursery`, `GetState`, `IsNurseryOrReusing`). Replace it with a debug assertion at each flip. If the assertion never fires in the test suites and the P14 matrix, the gate was not needed. If it fires, the workers that cause it must be stopped at the packet boundary before the flip, as the Java LXR does.
2. Read the block state and epoch with plain loads on a hot path that has no checks (section 6).
3. The same applies to the other per-object shared counters in the pause loops. Code: `m_metrics[...]` counters (`IncrementEntries`, `DecrementEntries`, `Promotions`, `ObjectScans`, `ObjectSizeQueries`, `Deaths`) and `m_live_bytes`, `m_reclaimed_bytes`. Each worker keeps its own counters and the totals are summed when someone reads them.

Inferred: the gate and the shared counters make the threads fight for cache lines. I have not run an experiment that removes them. The first experiment should do this (section 8).

## 6. The "checked metadata" fast path

Code: `SideMetadataManager::Load` and `Store` check the manager, the address, the kind, look up a description, and check for overflow on each step. They are in `side_metadata.cpp`, so callers cannot inline them. The Java LXR uses a shift, an add and a load.

Design: for the hot kinds (RC count, block state, phase epoch, unlog bit, `ValidObject`, mark bit), add inline functions in a header. They use constants for the base address and the shift. The checked functions stay for the slow path and for tests. Debug builds can assert that both give the same result.

Measured: `Load`, `Store` and `GetLocation` together are about 35% of CPU samples in the cheap-pause profile of binary trees, and about 24% inclusive for `Load` in requests.

## 7. Why does no block become empty? (follow-up to P15, finding 2B)

The question: "the real cause is that no block ever becomes empty, because the young copy has no room — why?"

The chain, with evidence:

1. **A young pause copies survivors out of the nursery blocks into new blocks.** If a pause copies all survivors, every nursery block is empty and is released. This is how the Java LXR keeps blocks empty.
2. **How much room does the copy need?** Measured (requests, 84 MiB heap, headroom 50% run, first pause): 20.9 MiB copied, 390 thousand objects. The survival ratio is about 22%.
3. **How much room does our port have at that moment?** Code: the trigger fires when `usedPages + collectionReservedPages` reaches the admitted pages. `CollectionReservedPages` is `2 × survival + headroom`. The headroom is 5% of the blocks in use: about 3.7 MiB for 74 MiB of Immix space. The survival term is computed in MiB and added as pages (the code says "the MiB-valued survival term is added as pages"). So it is small by a factor of 256. The room is about 3.7 MiB; the need is about 21 MiB.
4. **What happens when the room ends?** Code: `AllocateCopy` returns `AllocationFailure`. The engine sets `m_copy_exhausted`. From then on, every other copy in that pause is skipped. All remaining survivors are promoted in place. Measured: 11 copy failures in the first pause, 0 objects copied in the 58 collections of the default run.
5. **Why can it not take more blocks? Why do we differ from the Java LXR?**
   - Java LXR (code, `space.rs`): `Space::acquire` calls the trigger only for mutator threads (`should_poll = is_mutator(tls)`). A GC thread is never stopped by the heap budget, so the copy can take new pages even when the heap is "full". Code reading only, not run.
   - Our port (code): a comment in the mature selection says that, unlike the shared page resource of the Java LXR, only blocks that are already admitted can be used as to-space. The roadmap records this as a deliberate scope choice: P6.3 "over actual admitted Immix storage ... rather than treating reserved LOS pages as to-space", and P7.4 "occupied-capacity/headroom integration" with "the shipped reservation-unit mismatch is explicit" and "standard total-GC hard limits ... remain unsupported".
   - Why admission is hard in our port (code, `AdmitRange`, `CanAdmitRange`, `Grow`): the port reserves one large arena and admits it in ranges. **Admitting a range commits the per-object tables of that range** (the 21 bytes per 8 bytes of section 1) and checks them against `m_native_plane_limit`. Capacity only grows: I found no code that gives admitted capacity back (`m_immix_capacity` is only increased). Growth in a pause exists (`Suspend` calls `GrowSuspended` when `m_grow_bytes` is set), but only a failed mutator allocation sets it. So taking blocks for the copy would mean permanent growth plus committing the tables.
   - Inferred: the Java LXR gets "free" overshoot because its side metadata is small and mapped by the operating system on demand, and because MMTk's page resource can grow within the virtual range. In our port the per-object tables turned admission into an expensive, permanent step, so the design avoided it. This is my reading. I found no document that states this reason.
6. **Result:** survivors stay scattered in all nursery blocks. Measured (gdb): 2363 of 2368 blocks are `Reusable` while only 18% of the heap is live. No block is empty, so the sweep can release none. Mature pages go over 85% of the heap, and every later collection is full.
7. **Test.** With `LXRDefragHeadroomPercent` at 30 or 50, the copy has room, `blocks_released` grows each pause, and there are 0 full collections (P15, table in finding 2B). One run per setting.

The unit mistake of step 3 is in the Java LXR too (`get_collection_reserved_pages` returns MiB as pages). It does no harm there because of step 5.

### Design

- Size the reserve from the measured survival: bytes of young allocation since the last pause, times the survival ratio, times a safety factor. Use bytes, not MiB-as-pages. This works inside the admitted heap and needs no new admission. It costs memory: more of the heap is free when the pause starts, and pauses start earlier (77 collections instead of 58 in the 30% test).
- Remove the per-object tables first (section 1). Then admission is cheap, because it commits only the RC, `ValidObject` and mark planes. After that, a pause can admit blocks for the copy, as the Java LXR does. Open: the port has no way to give admitted capacity back. We would add a decommit of blocks that the pause released.
- The .NET hard limit (`GCHeapHardLimit`) is a strict cap. We must decide how much overshoot is allowed and document it. The port does not support the standard hard-limit settings today.
- Do not let one failed copy stop all copying. Retry when a block is released during the pause. Open: check the cost.
- Keep a test: "after a pause with survivors, the number of blocks with any live object must be at most survivors divided by block size, plus the number of pinned blocks".

## 8. Proposed order of work

Each step is small, measurable and can be reverted. Every step runs the P14 matrix (checksums must match) and the existing correctness suites before any claim.

| Step | Change | Expected effect (to be measured) | Risk |
|---|---|---|---|
| S0 | Per-phase timers inside a pause (no behavior change) | Gives the wall-clock split we lack | Low |
| S1 | Remove the gate and the shared counters from the pause loops | `IsNursery` 10%, `Reclaim` 6% self time | Low; Open item in section 5 |
| S2 | Inline fast path for hot metadata (section 6) | up to a third of pause CPU | Medium: many call sites |
| S3 | Remove `m_live_objects`, `m_extents` and the byte counters; walk `ValidObject` bits; relax rule 4 for the cycle sweep (section 1) | `ReclaimNursery` 3.5% to 17%; memory | Medium: see the table of needs |
| S4 | Pause sweep over the allocated-block list; background sweep of candidates (section 2) | `Sweep` 1% to 10%; `GetHeapStatistics` 4% | Medium |
| S5a | Size the to-space reserve in bytes from measured survival (inside the admitted heap) | Full collections 52 to 0 in the headroom test; the largest effect on stalls | Low to medium: more memory kept free |
| S5b | After S3: admit blocks for the copy in the pause, and give them back | Same effect without keeping memory free | Medium: memory-limit semantics, decommit |
| S6 | Finalizer registry list; weak and finalizable objects handled only in tracing pauses (section 3; decided) | `WalkObjects` 1% to 6% | Medium: behavior of finalization (D3) |
| S7 | Pin list and block bytes (section 4); then remove `m_flags` and `m_trace_*` tables | Memory; `WalkObjects` | Higher: new design (D4) |

I recommend S5a first, because it removes the full collections, which are the longest stalls. S0 should come with it, so that we can see what is left.

## 9. Decisions and open items

**Decided by you:**

- Relax roadmap rule 4 for the background cycle sweep: it may read the size from the dead object (as the Java LXR does).
- `m_live_bytes` and `m_reclaimed_bytes` stay in test builds only. The .NET memory APIs use a block-count estimate.
- Finalizable objects go in a registry list. Weak and finalizable objects stay alive in RC pauses and are handled only in tracing pauses (as in the Java LXR).
- The plan of section 8 is accepted. Implementation starts with S0, S5a and S1 (a child session).
- **D1: option 1.** The cycle sweep reads the size from the dead object. Consequence: S3 must include a guard that the runtime cannot free the type data (loader allocator) of collectible types before the lazy cycle sweep has finished. Open for S3: find where the runtime frees loader allocators after a pause, and defer that step until `SweepCycles` ends (or run the sweep for collectible-type objects inside the pause). A test with an unloadable `AssemblyLoadContext` is required.
- **D2: option 2 (S5b).** Overshoot for the copy only when no hard limit is set. With a hard limit, keep the S5a reserve. Consequence: S5b needs the decommit (give-back) code, and it comes after S3 (admission is cheaper without the per-object tables).
- **D3: option 1.** No bound on the delay of finalization and weak handles. Measure with a finalizer-heavy test in S6. Add a trigger only if needed.
- **D4: option 1.** Pinned-object-heap objects go in dedicated Immix blocks with a "never copy, never defrag" attribute. The allocator gets a new block class for them (S7).

Order from here: layer 1 (S0, S5a, S1, running), then S2, then S3 (with the D1 guard), then S4, then S5b, then S6, then S7. Each layer is its own child session, based on the previous layer's branch.

**Checks.** Items C1 to C4 are checks that are done as part of the work.

### D1. Where does the cycle sweep read the size of a dead object? (affects S3)

*Background.* In a tracing pause, objects that were never marked but still have RC above 0 are garbage in cycles. The cycle sweep must clear their RC and "continuation line" marks. To clear the continuation marks, it needs to know how many lines the object covers. Today it reads that from the size table we want to remove.

*The problem.* The size can be read from the object itself (`object->GetSize()`). That needs the object's type data (`MethodTable`). For objects of **collectible types** (types in an unloadable `AssemblyLoadContext`), the type data belongs to a loader allocator, and the runtime frees it when the loader allocator is found dead.

*What I found in the code (this session).* `SweepCycles` does not run inside the tracing pause. It runs from `Sweep(guard)`, the lazy mature sweep, which starts after the decrements are drained (`DecrementsDrained`, `SubmitMatureSweep`). So it runs after the pause, while the program runs. The weak-handle scans of the pause (`GcWeakPtrScan`, `SyncBlockCacheWeakPtrScan`) are where the runtime learns that a loader allocator is dead. Inferred: the runtime can then free the type data before or while the lazy sweep reads it. Dead-cycle objects keep their count on the loader allocator, because the sweep does not decrement the edges of dead cycles. That may delay the unload, but I did not prove it does. So option 1 below ("sweep before unload") is hard: the sweep is lazy.

*Options.*
1. Read the size from the object, and make sure the unload cannot happen before the lazy sweep ends. Hard, because the sweep is lazy.
2. Do not read the size at all. Use the RC table. Code check (this session, `reference_count.cpp`): a continuation line is marked by the straddle bit and a count of 1 on the first slot of the line, and `ValidObject` is 0 there. **A plain "count the next lines that have the straddle bit" is not exact.** If object X ends in the middle of a line and a second promoted object Y starts later in that same line and runs into the next line, that next line carries Y's mark, and the plain walk would clear it for X. The exact walk goes slot by slot from the start of X. It stops at the first slot that has a count above 0 and is not a continuation mark (that slot starts another object), or at the first line start without the straddle bit. It counts the continuation lines it passes before stopping. This needs no type data, so it is safe for collectible types, and it keeps rule 4. The cost is one RC read per 8 bytes of the dead object, the same order as the work to clear the marks. Young objects have no straddle marks (they get them at promotion), so they cannot confuse the walk. It still needs a test with objects that share lines, objects that end at a line end, and objects larger than a block.
3. Keep the size table only for collectible types. Not recommended: it keeps the table and the per-object cost.

*Recommendation:* option 2 (now stronger, because of the lazy timing). If the exactness check fails, use option 3 for collectible types only.

### D2. To-space beyond the admitted heap, and the hard limit (affects S5b)

*Background.* "Admitted" heap is the part of the reserved arena that the collector may use now. Today a pause can copy only into blocks that are already admitted. When the heap is full there are almost none, so S5a keeps a bigger free reserve instead. The Java LXR lets GC threads take pages beyond the budget for the time of the pause.

*The problem.* To do the same we must (a) admit more range during the pause (the code for this exists, `GrowSuspended`, but only a failed mutator allocation uses it), (b) pay for committing the tables of that range (this cost shrinks a lot after S3), and (c) give the range back after the pause. I found no code that gives admitted capacity back. And (d) .NET has `GCHeapHardLimit`, a strict cap. In containers .NET sets it by default to 75% of the container memory. An overshoot in the pause would break that cap.

*Options.*
1. S5a only: never overshoot. Reserve enough free bytes, so the trigger fires earlier. Costs memory (the reserve is free but not usable by the program) and gives more frequent pauses. No new risk.
2. S5b with a rule: overshoot only when no hard limit is set. With a hard limit, use option 1. Needs the give-back code (decommit of the tail of the arena, which works only if the tail blocks are free, and they are right after the pause).
3. S5b always, and count the overshoot against the hard limit by lowering the trigger. This is option 1 again.

*Recommendation:* start with option 1 (S5a, now). Decide on option 2 after we measure what the reserve costs on the P14 matrix.

### D3. What the delayed finalization and weak handles mean for programs (affects S6)

*Background.* With your decision, a young object with a finalizer, or reachable only through a weak handle, is not handled by RC pauses. It is handled in the next tracing pause (final mark or full).

*Consequences.*
- A tracing pause happens when mature garbage passes a threshold (`trace_threshold`) or when the heap is in emergency state. A program that creates many short-lived finalizable objects (for example `FileStream` or `SafeHandle` without `Dispose`) keeps them, and their native resources, until that pause. The registry also keeps each one alive, so the objects are promoted and count as mature.
- `WeakReference` and `ConditionalWeakTable` entries are cleared later than with the standard GC. Code that relies on the target being gone after the next gen0 collection will see the target for longer.
- `GC.Collect()` is a full pause, so tests that call it still work.
- Some mature garbage exists only because the registry holds it. That helps to trigger a tracing pause, but only after the threshold.

*Options.*
1. Exactly as the Java LXR: no bound.
2. Add a trigger: start a tracing cycle when the registry (finalizable count or bytes) passes a limit, for example 10% of the heap. Bounds the delay and the memory. Small code, and it needs one new policy knob.

*Recommendation:* option 2, but as a separate step after S6, so that we can measure whether option 1 is enough.

### D4. Pinned object heap and the meaning of `IsPinned` (affects S7)

*Background.* Pinning has three sources (section 4). For the pinned object heap (POH: `GC.AllocateArray(pinned: true)`) the design allocates these objects in dedicated blocks that are never copied. Today POH objects get a `Pinned` flag in the per-object table.

*Open, now answered (code).* `IsPinned` is not a runtime API. `IGCHeap` has no such method. It is only the engine callback `m_object_model.isPinned`, and the engine calls it only inside a pause: the nursery and mature copy decisions, `RetainPinnedMatureBlocks`, and one metric in `ProcessIncrement`. So a pin list that exists only during a pause can answer every caller. The `Pinned` flag is set at allocation for pinned-object-heap objects (`GC_ALLOC_PINNED_OBJECT_HEAP`, in `Alloc` and `AllocationComplete`). The `ObservedPin` flag is set by `AddRoot` for pinned roots and is cleared by `LeaseFinalizer`, the heap walk that S6 removes.

*Options.*
1. Block attribute for pinned-object-heap objects, pin list for roots in the pause, and the callback answers from the block attribute plus the list. This is now safe, because all callers are inside pauses.
2. Keep a small hash set of pinned objects that is valid at all times. More code. Not needed.

*Recommendation:* option 1. It needs only your confirmation. Open: how small pinned-object-heap objects are placed (Immix block or large-object space). A dedicated block class needs the allocator to support it.

### Checks that I will do (no decision needed)

- **C1. The epoch gate.** Add a debug assertion at each epoch flip: "no block operation in flight". Run it on the test suites and the P14 matrix. If it never fires, remove the gate (S1). If it fires, the cause is a worker that must be stopped at a packet boundary before the flip.
- **C2. `SetReusable(free_lines)`. Checked (code).** The allocator reads it. To find a block with holes (`immix_allocation.cpp`), it takes only blocks in state `Reusable`, and skips blocks whose free-line count is below `m_minimum_reuse_lines`. The pause sweep is where a promoted nursery block becomes `Reusable`. So S4 must **move** this work to the background sweep (or to the first touch by the allocator). It must not drop it. Otherwise promoted nursery blocks are never reused.
- **C3. Reuse paths and RC bytes.** Check that every path that reuses a range also clears the RC bytes of that range. The heap walk relies on "RC 0 means dead" for stale `ValidObject` bits (S3).
- **C4. Weak handle scans.** Check that `GcScanHandles` and the weak scans need no per-object flag (S6).
