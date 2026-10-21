# P15: Why are our pauses long, and why do we run so many full collections?

Status: research only. No product code changed. The accepted port stays at `e8d2d23`.

All numbers come from Linux (WSL2), the P14 matched programs, a heap of 4 times the minimum, and the accepted port in non-moving mode unless the text says otherwise. Windows was not measured. Most traces are single runs. Treat the numbers as a clear direction, not as exact values.

Each claim has a label:

- **Measured**: a number from a run, `perf` or gdb.
- **Code**: read in the source. Not yet confirmed by a run.
- **Inferred**: my conclusion from the two items above. It can be wrong.

The tool results use "share of all CPU samples in that run". The mutator threads are in that total. So a share is not a share of the pause.

## Words used here

- **Pause**: the time when the program threads are stopped for a collection.
- **Young collection** (the Java LXR calls it a "reference-counting pause"): a pause that handles only recent changes.
- **Full collection**: a pause that also walks the whole live object graph.
- **Block**: a 32 KiB piece of the heap. A block has 128 lines.
- **Nursery block**: a block that got new objects since the last pause.
- **Registered start**: our port keeps, for each object, a record of its start address and size (the "extent slot" and the "live-object bit"). The Java LXR build keeps none of these: it finds object starts in the RC table. See [P16](../P16-remove-per-object-bookkeeping/README.md).

## Question 1: why are the pauses so long?

### What we measured

| Program | Kind of pause | Our port | The Java LXR |
|---|---|---|---|
| Binary trees | young | 90 to 110 ms | 1.3 to 5 ms |
| Requests | young | about 100 ms | 1.3 to 5 ms |
| Requests | full | 280 to 400 ms | none happened (0 full collections) |
| List suffix | young | 145 to 150 ms | 1.3 to 5 ms |

Measured: the pause length follows the number of objects allocated since the last pause. Binary trees allocates about 1 million objects between pauses. Average pauses for heaps of 20, 40 and 80 MiB were 41, 109 and 199 ms.

### Finding 1A: `ReclaimNursery` walks every young object. Why?

**The question.** The coalesced reference counting and the write barrier are in our port, and they work. A new object has a count of 0. If nobody increments it in the pause, it is dead. Nobody needs to look at it. So why does our pause visit each young object?

**What the Java LXR does** (code: `block_allocation.rs`, `sweep_nursery_blocks`). At the end of the pause it counts the nursery blocks. It gives them all back with one call: `bulk_release_blocks(number_of_nursery_blocks - blocks_promoted_in_place)`. It does not open a block. It does not look at one dead object. The objects that lived were copied out of the block, or their block is marked "promoted in place" and stays.

**What our port does** (code: `ReclaimNursery`, `Reclaim`, `CompleteAllocation`). It does the same reference counting. But it also keeps the registered start of each object. At allocation (`CompleteAllocation`) it writes three things for each object: the size in the extent slot, one bit in the live-object bitmap, and the added bytes in a shared counter. At the pause, `ReclaimNursery` finds every registered start in each nursery block, reads its count, and for each dead object calls `Reclaim`. `Reclaim` undoes the three writes. The number of calls is the number of dead objects.

**Why the port keeps registered starts** (code, then inferred):

- `FindContainingObject` takes an address that points into the middle of an object. It walks backward over the registered starts until it finds the object. .NET code can hold such interior addresses. The Java LXR does not need this search.
- `WalkObjects` and `FindNextObject` use the registered starts to walk the heap. The .NET runtime needs to walk the heap (diagnostics, finalization).
- Inferred: these two needs are the reason for the record. I did not find a design note that states it. The roadmap says only that the engine uses "exact registered object starts".

**What the roadmap says.** Rule 4 says: "Sweeping never parses objects". The code does not break this rule in the strict sense: it does not read the object's fields. But it does visit each dead object. That is the work that the rule wanted to avoid.

**Cost.** Measured, share of all CPU samples in the run: `ReclaimNursery` is 16.9% in binary trees, 10.3% in requests, and 3.5% in list suffix.

**What a fix would look like.** Inferred. Clear the registered starts of a freed nursery block with one range operation. For blocks that stay, clear the dead starts by lines, not by objects. I did not test this.

### Finding 1B: `Sweep` and heap walks in every pause. Why?

**What the Java LXR does** (code: `immixspace.rs`, `rc_work.rs`).

- In the pause: only the bulk release of nursery blocks described above.
- When a decrement kills a mature object, the block of that object goes on a list (`add_to_possibly_dead_mature_blocks`).
- After the pause, in the background, worker packets (`SweepBlocksAfterDecs`) look at only the blocks on that list.

**What our port does** (code: `Sweep(nullptr)` inside `ReleaseEpoch`). It loops over every block in the heap, in every pause. For each block it asks "is the whole block dead?" (`TryReleaseIfRcDead`, a check of the count table of the block). For each block that stays, it scans all 128 lines for free holes (`TryFindNextHole`) and writes the block state. Our engine has a list like the Java one (`m_mature_candidates`, set in `Death`). The lazy sweep uses it. The sweep in the pause does not.

Inferred: the pause sweep rebuilds the state of all blocks from the count table because the port was built to compute block states, and not to track them. I found no written reason.

Measured, share of all CPU samples: `Sweep` is 5.0% in binary trees, 9.9% in requests (of which `SweepCycles`, the full-collection part, is 6.3%), and 0.7% in list suffix.

**Walks of all objects** (code: `ExecuteScan`, kinds 0 and 1). Every pause, the scan thread walks all registered objects up to three times:

- `LeaseFinalizer` resets the pin and copy flags in `m_flags` of each object, and collects the objects that have a finalizer.
- `QueueFinalizer` finds the dead objects that have a finalizer.
- `TracePendingFinalizer` runs only if the second walk found some.

Also `GetHeapStatistics` walks the heap to compute the heap size and fragmentation (inferred: for the memory information that the .NET runtime asks for).

Why (inferred): in our port, "this object has a finalizer" and "this object is pinned" are flags on each object. To find them, the port must visit each object. The Java LXR keeps finalizable objects and weak references in lists that it finds through its reference processing.

Measured, share of all CPU samples: `WalkObjects` is 5.0% (binary trees), 5.8% (requests), 1.2% (list suffix). `GetHeapStatistics` is 4.1% in requests.

### Finding 1C: the increment and decrement code (list suffix)

Earlier I wrote that I could not find the cause. The code is here. This is where it is:

| Work | Function | Source line (`lxr_reference_counting.cpp`) |
|---|---|---|
| Process a batch of increments | `ProcessIncrementWork` | 3744 |
| Process one increment | `ProcessIncrement` | 2916 |
| Scan the fields of a new object | `ScanIncrements` | 3040 |
| Process a batch of decrements | `ProcessDecrements` | 3170 |
| A mature object dies | `Death` | 3122 |
| Remove the registered start | `Reclaim` | 3952 |

Measured on list suffix (57 collections, about 290 thousand increments and 290 thousand decrements per pause): `ProcessIncrementWork` is 38.3% and `ProcessDecrements` is 24.9% of all samples. These two are the real work of reference counting. The Java LXR does the same work. The question is the cost of each step, about 1.2 microseconds per logged increment in our port.

Measured, inside `ProcessIncrementWork` (share of all samples; sums to about 34 of the 38.3):

| Callee | Share |
|---|---|
| `ImmixBlockManager::IsNursery` | 10.2% |
| `ProcessIncrement` itself | 5.3% |
| `ReferenceCountTable::ValidateAddress` | 3.7% |
| `ProcessIncrementWork` itself | 3.6% |
| `SideMetadataManager::Load` | 2.7% |
| `NewWork` | 2.6% |
| `SideMetadataManager::GetLocation` | 2.3% |
| `ResetRangeQuiescentCore` | 2.0% |
| `IncrementSaturating` | 1.3% |

The largest item is `IsNursery`. Code: it is called for every object that gets its first increment. Each call does three things: (1) it starts a `BlockOperationScope`, which adds 1 to one shared atomic counter (`m_active_operations`) and subtracts 1 when it ends; (2) it reads the block state; (3) it reads the epoch of the block. Every worker thread uses the same shared counter. Inferred: the threads fight for the cache line of that counter. The Java LXR checks the same fact with two plain byte loads (`Block::is_nursery`).

In the decrement path, each dead object calls `Reclaim`. Measured: `Reclaim` is 6.4% self time, and it has no loop. Code: it does four atomic read-modify-write operations: one on the extent slot of the object, and three on memory that other threads also use (the bitmap word, `m_live_bytes` and `m_reclaimed_bytes`). Inferred: the same cache-line contention.

I did not run an experiment that removes these atomics. So the contention is a strong guess, not a result.

### Finding 1D: what "checked metadata access" means

This is the code in `side_metadata.cpp`. Every read of a metadata value (reference count, block state, unlog bit, and others) calls `SideMetadataManager::Load`. This is one generic function for all kinds of metadata. It does this each time:

1. `GetLocation`: check that the manager is ready, that the address belongs to the heap, and that this kind of metadata is enabled.
2. Find the description of the kind in a table.
3. `GetLocationUnchecked`: compute the byte address, the shift and the mask from the description. It checks for overflow in each addition and checks that the result is inside the metadata area.
4. Do the atomic load, and apply the mask and the shift.
5. Return a result code. The caller tests it.

The function is in another source file from its callers, so the compiler cannot inline it. The Java LXR does this with one shift, one add of a constant base, and one load. The work of one access is much larger in our port. This was needed in early phases to prove correctness. It is now part of every operation.

Measured, share of all CPU samples: `Load`, `Store` and `GetLocation` together are about 35% in binary trees (`Load` 17.9% and `Store` 13% inclusive), 24% (`Load` only) in requests, and 14% inclusive plus 5.6% `GetLocation` and 3.9% `Store` in list suffix.

### Finding 1E: the allocating thread waits for lazy work

Code: `CollectLocked` calls `Drain`. `Drain` waits for the lazy work inside the collection lock. The thread that triggers the collection counts this wait as part of the pause. The Java LXR runs lazy decrements and lazy sweep after the pause, at the same time as the program.

### What we did not measure

I did not put timers around each phase inside one pause. The split above uses CPU-sample shares, not wall-clock time per phase.

## Question 2: why do we run so many full collections?

### What we measured

Requests, 4 times heap: 51 of 57 collections in our port are full. The Java LXR has 0 full collections out of 113.

All collections in our port start for the same reason as in the Java LXR: the heap is full. The difference is in the later decision: young or full.

### Correction to the first version of this page

The first version said that our port counts mature pages differently from the Java LXR, and that this is the cause. That was wrong. I read the code again (`global.rs` `decide_next_gc_may_perform_cycle_collection`).

- Java LXR: mature pages = pages in use at the end of the pause, minus the blocks that the lazy sweep released after the pause. A block with a few live objects counts as a full block.
- Our port (`GetMaturePages`): every block that is not empty, when the lazy sweep has finished. A block with a few live objects also counts as a full block.

These are the same measure. The rule is the same in both. The difference is in the data: **which blocks stay non-empty**. The next findings show why.

### Finding 2A: after collection 10, every collection is full

Same rule in both: the next collection is full when mature pages are above 85% of the admitted pages.

Measured with gdb on requests (84 MiB = 21504 pages, threshold 18278 pages):

- Mature pages: 9689, 13593, and so on. At the 10th update, 18689 (87%). They settle at 19009 (88%, 74.3 MiB).
- 2363 of 2368 blocks are in state `Reusable`.
- The live data is only about 15.3 MiB (about 18%).

So almost every block holds a few live objects. No block is empty. The sweep can release only empty blocks.

Measured in the Java LXR on the same program: mature pages peak at 70% (9283 of 13312). The rule does not fire.

### Finding 2B: why the blocks are never empty. The copy has no room.

**What the Java LXR does.** In a young pause, the objects that live are copied out of the nursery blocks into new, dense blocks. Then all nursery blocks are released. The live objects of the program end up packed together.

**What our port does.** It tries to copy, too. Measured, in copying mode (`LXREvacuateNursery=1`, `LXREvacuateMature=1`), default headroom, 58 collections:

- Objects copied: 0, for young and mature.
- Copy failures: 11 in the first pause, then 14, 17, 20, 25, 33, 35 (it stays at 35).
- Mature blocks selected for copying: 0 in every collection.

Code (`TryCopyNursery`, `AllocateCopy`): the copy needs a free block for the new place. If there is none (`AllocationFailure`), the engine keeps the object in place, sets a "copy exhausted" flag, and every later copy in that pause is skipped. A comment in the mature selection code says that, unlike the shared page pool of the Java LXR, only blocks that are already admitted can be used as the new place. So in the Java LXR the new place can come from a shared pool of pages, also when the heap is full (I did not read the Java page-pool code to confirm this). In our port the new place must come from admitted blocks. The trigger fires when the heap is full, so there are almost none.

The port has a reserve to prevent this (`CollectionReservedPages` in the trigger: twice the predicted survival plus the headroom). The default headroom is 5% of the blocks in use, the same default as the Java LXR. It is not enough here.

**Experiment (configuration only, no code change).** Same program (requests, 84 MiB), copying mode, but a larger headroom with the existing setting `LXRDefragHeadroomPercent`:

| Headroom | Collections | Full collections | Objects copied (young, by the end) | Typical pause |
|---|---|---|---|---|
| 5% (default) | 58 | 52 | 0 | about 480 ms (full) |
| 30% | 77 | 0 | 873 thousand | about 110 ms (some 200 to 350 ms at the start) |
| 50% | 98 | 0 | 982 thousand | about 90 to 100 ms |

Measured, one run each. With enough room, copying starts, young survivors are packed, empty blocks appear (`blocks_released` grows each pause), the mature count stays below the threshold, and **no full collection happens**. A pause then costs about 100 ms, as in the young pauses of Question 1. A large headroom takes memory from the program, so this is a test, not a setting to ship.

Inferred: the root cause of the many full collections is that the copy has no room when the pause starts. It is not the formula for mature pages.

In the non-moving mode no copy is attempted by design. So the blocks never become dense, and the result is the same: many full collections.

### Finding 2C: the decision sits on a cliff

Measured: for list suffix on a 40 MiB heap, a large-object space of 8 MiB and an Immix space of 32 MiB gives 0 full collections. A large-object space of 5 MiB and an Immix space of 35 MiB gives 40 full collections of 42. A small change in the heap split changes the behavior completely.

### Finding 2D: a full collection costs much more in our port

Measured: when the Java LXR runs full collections on list suffix, each costs about 32 ms (75 of them). In our port with the 35/5 split, each costs about 600 ms (see the P13 gaps).

## How the pieces connect

1. The copy has no room (2B). Survivors stay where they are. Blocks hold a few live objects each.
2. No block is empty, so the mature count passes 85% (2A). Every later collection is full.
3. Each full collection costs 300 to 600 ms (2D). These are the longest stalls (143 ms to 1.9 s).
4. Even without full collections, each young pause costs about 100 ms (Question 1): the registered-start clean-up per dead object (1A), the sweep of all blocks and the object walks (1B), the slow metadata path (1C, 1D), and the lazy wait (1E).

## Side finding: a deterministic abort

`requests 32768 250000` at an 84 MiB heap stops with "LXR allocation-complete metadata publication failed" (`LxrGcHeap::AllocationComplete`, an object of 40 bytes with references). It also happens at 150000 operations. 600000 operations works. This was found with gdb (break on `GCToEEInterface::LogErrorToHost`). It is a separate defect. It is not analyzed further here.

## Candidate fixes, in order (not done; research only)

These are suggestions. We did not test any of them, except the headroom experiment above.

1. Give the copy a to-space that exists at pause time (2B). This is the largest effect: no full collections in the test.
2. Make young reclaim cost follow survivors, not allocations (1A). Clear registered starts of a freed block in one operation.
3. Sweep only the blocks that were allocated into or listed as possibly dead (1B).
4. Replace the finalizer and pin flags walks by lists (1B).
5. Add a fast path for the metadata access: inline address arithmetic with constants (1D). Replace the shared counters in `IsNursery` and `Reclaim` by per-thread counters (1C).
6. Move lazy work out of the pause (1E).
7. Add per-phase timers inside a pause before step 2, so we can check each gain.

## Where the data is

- Scripts in this folder: `GcTrace.cs` (tracer), `trace.sh`, `perf.sh`, `inclusive.sh`, `inclusive-many.sh`, `gdb.sh`, `gdb-policy.py`, `copymode.sh` (third argument: headroom percent), `headroom.sh`, and the analysis scripts.
- Raw results are in `/root/gcbench/results/p15-*` inside WSL. They are not committed.
- Source snapshots: `artifacts\P13-full-reference-sources` (not committed).
