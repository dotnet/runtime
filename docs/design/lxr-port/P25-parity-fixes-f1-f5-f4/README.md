# P25: parity fixes F1, F5 and F4

Status: three product changes, each in its own commit (`F1:`, `F5:`, `F4:`). Nothing was pushed. No pull request was opened.
Branch: `kkokosa-lxr-parity-fixes-f1-f5-f4`, made from `kkokosa-lxr-slowness-investigation` (tip `c16710a2c1c`).

Labels: **Measured** = I ran it and saw the result. **Code** = I read it in the source (file and function are named).
**Inferred** = I conclude it from other facts, and it can be wrong. **Open** = not known, or not done.

Words used here:

- **the Java LXR** = the LXR collector of MMTk in OpenJDK: mmtk-openjdk `0682434ae725`, mmtk-core `304ce69d43aa`. I read the
  source of exactly these versions (exported with `git archive`).
- **pause** = the time when the program threads are stopped. **RC pause** = a pause that only does reference counting work.
  **InitialMark / FinalMark** = the pauses that start and end a concurrent trace. **Full** = a pause that traces the whole heap.
- **SATB trace** = the concurrent trace of the live heap that finds dead cycles. **Packet** = one unit of work for the worker threads.
- **Black allocation** = an object that is allocated while a trace runs is marked at once. The trace does not need to visit it.
- **Control** = a deliberately broken variant of the code (a build define). The test must fail on it. A test without a control
  can pass because it tests nothing.
- **Nonmoving / nursery** = two modes of our collector: no copy of young objects; copy of young objects at the RC pause.
- **WKS** = .NET Workstation GC. **x WKS** = ratio of the median wall time to WKS in the same table.

## 1. Result in short

All numbers are **Measured** on WSL (16 logical cores), Linux Release, ReadyToRun on, heap 4 times the smallest WKS heap,
3 repetitions, natural collections only. The checksums of all runs are equal.

### Wall time (s, median of 3), with the ratio to WKS of the same session

| program | arm | P23 | P24 baseline (c16710a) | after F1 | after F5 | after F4 (final) | final x WKS | final CPU s | final CPU x WKS |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | wks | 0.32 | 0.34 | 0.35 | 0.38 | 0.33 | 1.0 | 0.25 | 1.0 |
| binarytrees | server |  |  | 0.37 | 0.36 | 0.33 | 1.0 | 0.27 | 1.1 |
| binarytrees | lxr-nonmoving | 0.75 | 0.76 | 0.52 | 0.53 | 0.49 | 1.5 | 0.65 | 2.7 |
| binarytrees | lxr-nursery | 0.86 | 0.82 | 0.64 | 0.63 | 0.55 | 1.6 | 0.87 | 3.6 |
| requests | wks | 1.71 | 1.72 | 1.92 | 1.73 | 1.71 | 1.0 | 1.58 | 1.0 |
| requests | server |  |  | 1.92 | 1.82 | 1.73 | 1.0 | 1.82 | 1.2 |
| requests | lxr-nonmoving | 10.57 | 10.72 | 10.92 | 3.99 | 4.04 | 2.4 | 5.91 | 3.8 |
| requests | lxr-nursery | 4.2 | 4.32 | 4.33 | 4.07 | 4.16 | 2.4 | 8.82 | 5.6 |
| listsuffix | wks | 3.41 | 3.57 | 4.05 | 3.59 | 3.60 | 1.0 | 3.63 | 1.0 |
| listsuffix | server |  |  | 1.85 | 1.64 | 1.53 | 0.4 | 1.91 | 0.5 |
| listsuffix | lxr-nonmoving | 21.62 | 20.39 | 5.80 | 5.57 | 3.68 | 1.0 | 10.71 | 3.0 |
| listsuffix | lxr-nursery | 30.42 | 28.8 | 6.92 | 6.38 | 4.56 | 1.3 | 13.49 | 3.7 |


The WKS and Server rows of each column come from the run of that column, so the ratio is only valid inside one column. P23 and P24 are the numbers of the task text and of P24. WKS in the final run: 0.33, 1.71 and 3.60 s (P23: 0.32, 1.71, 3.41 s). **Compare Java and .NET only through ratios; this document has no Java time.**

### Final build (F4): all columns

| program | arm | wall s | cpu s | gc s | pauses | full | RSS MiB | RefCount pause median ms (traced run) |
|---|---|---|---|---|---|---|---|---|
| binarytrees | wks | 0.33 | 0.25 | 0.08 | 87 | 0 | 54 |  |
| binarytrees | server | 0.33 | 0.27 | 0.05 | 68 | 0 | 67 |  |
| binarytrees | lxr-nonmoving | 0.49 | 0.65 | 0.08 | 17 | 0 | 92 | 5.18 |
| binarytrees | lxr-nursery | 0.55 | 0.87 | 0.13 | 18 | 0 | 92 | 5.64 |
| requests | wks | 1.71 | 1.58 | 0.23 | 277 | 0 | 83 |  |
| requests | server | 1.73 | 1.82 | 0.27 | 274 | 0 | 87 |  |
| requests | lxr-nonmoving | 4.04 | 5.91 | 0.37 | 58 | 0 | 155 | 15.98 |
| requests | lxr-nursery | 4.16 | 8.82 | 0.97 | 68 | 0 | 155 | 18.84 |
| listsuffix | wks | 3.60 | 3.63 | 2.01 | 191 | 0 | 66 |  |
| listsuffix | server | 1.53 | 1.91 | 0.62 | 51 | 0 | 74 |  |
| listsuffix | lxr-nonmoving | 3.68 | 10.71 | 0.91 | 53 | 0 | 107 | 33.5 |
| listsuffix | lxr-nursery | 4.56 | 13.49 | 1.53 | 51 | 0 | 103 | 40.91 |


### Number of Full pauses and median RefCount pause (ms), per fix

| program | arm | Full F1 | Full F5 | Full F4 | RefCount ms F1 | RefCount ms F5 | RefCount ms F4 |
|---|---|---|---|---|---|---|---|
| binarytrees | lxr-nonmoving | 0 | 0 | 0 | 5.72 | 6.25 | 5.18 |
| binarytrees | lxr-nursery | 0 | 0 | 0 | 6.4 | 7.49 | 5.64 |
| requests | lxr-nonmoving | 52 | 0 | 0 | 10.43 | 17.06 | 15.98 |
| requests | lxr-nursery | 0 | 0 | 0 | 19.74 | 19.54 | 18.84 |
| listsuffix | lxr-nonmoving | 0 | 0 | 0 | 69.53 | 69.92 | 33.5 |
| listsuffix | lxr-nursery | 0 | 0 | 0 | 85.01 | 84.63 | 40.91 |


## 2. F1: no trace packet for an object that needs none

### 2.1 The basics

Reference counting (RC) counts the references to an object. A new (young) object has count 0. At an RC pause the engine
adds one to the count of every young object that the program can still reach. This is the "increment". The first increment
promotes the object: it becomes a mature object.

The concurrent SATB trace finds dead cycles. A cycle of objects keeps its counts above zero, so RC alone cannot free it. The
trace marks every object that was reachable when the trace started (the snapshot). At the end, the sweep frees each object that
has a count above zero and no mark.

Two rules keep this correct and cheap:

1. An object that is allocated after the snapshot is **black**: it is marked when it is allocated. The trace does not need to
   visit it.
2. The trace starts from the **roots**. It follows the fields of the objects. It does not need one packet for each object.

### 2.2 What the Java LXR does

- **Code** (`mmtk-core 304ce69d43aa`, `src/plan/lxr/rc.rs`, `ProcessIncs::promote`): when an object is counted for the first
  time during a trace, the Java LXR asserts that it is marked (`debug_assert!(self.lxr.is_marked(o))`). It queues no packet for it.
- **Code** (`rc.rs`, `ProcessIncs::do_work`): only in an InitialMark pause the roots go to `LXRConcurrentTraceObjects`. In a FinalMark
  or Full pause the roots go to the closure of the pause. In an RC pause the roots are not traced.
- **Code** (`src/policy/immix/block_allocation.rs: initialize_new_clean_block`, `immixspace.rs: rc_get_next_available_lines`):
  new blocks and new holes are marked at allocation while a trace runs (black allocation).
- **Code** (`src/plan/lxr/cm.rs: LXRConcurrentTraceObjects::trace_object`): an object that is already marked is skipped.

### 2.3 What we did before

**Code** (`LxrReferenceCounting::ProcessIncrement`, before F1): `if (trace && GetTracePhase() != Inactive) QueueTrace(&target, 1)`.
Every counted object got its own trace packet, in every pause that ran while a trace was active. The packet found the mark bit
set and did nothing (`ProcessTraceObject`: `mark != 0`).

**Measured** (P24, list suffix, nursery mode): about 260 000 packets per RC pause. The trace backlog never went down
(326 520 packets after 14 s). The pause time was 390 ms. A diagnostic variant that skipped marked objects gave 8 s instead of 27 s.

### 2.4 What F1 does

1. `ProcessIncrement` queues a packet only if all three are true: the flag `trace` is set, the trace phase is `Concurrent`
   or `FinalMark` (the window after the snapshot), and the mark bit of the object is 0 (`NeedsIncrementTrace`).
   - In an InitialMark or a Full pause no packet is queued for a counted object. `PrepareTrace` queues the roots, as the
     Java LXR does.
   - A black object has its mark bit set, so it gets no packet.
2. The children of a black object get the `trace` flag, as before. A child gets a packet only if its own mark bit is 0.
   **Why we differ from the Java LXR here:** a .NET program can read the target of a weak handle and store it in a new object.
   The trace did not mark a weak target. The Java LXR has a keep-alive barrier on the referent of a `Reference`; .NET has no such
   barrier. The test `ValidateBlackNewbornClosure` (P4) has checked this since P17. With the mark test the extra packets exist
   only for objects that are really unmarked. **Inferred:** this is the only place where the port needs more than the Java LXR.
3. `PublishTraceWork` (called in `Resume`) visits the list of the pending trace packets (`m_trace_todo`). Before it visited
   every packet of the epoch. This is the part of F2 that was needed: the cost of the walk now follows the number of pending
   packets, not the number of all packets.
4. In a Debug build the engine checks the black-allocation rule as the Java LXR does: an object that is counted for the first
   time inside the window must be marked, else the engine fails with `InvariantViolation` (`ProcessIncrement`, `_DEBUG`).
   **Measured:** it never fired in the native Debug selectors, the random-graph test and the 90 managed cells with the Debug collector.
5. `TraceBatch` is no longer used for increments (it is still used by the trace scan). `ProcessFullIncrementWork` is removed.

### 2.5 Why this is correct

- **Code.** Every path that creates a young object during a trace marks it: the mutator refill (`PrepareFreshBlock`,
  `PrepareRecycledHole`, marking context `ConcurrentOrFinalMark`), the large-object allocation (`Allocate`, `m_marking`), the pinned
  allocator (same refill) and the copy allocator (`AllocateCopy`: `Concurrent` or `FinalMark`). `FlushMutators` resets every mutator
  allocator at each pause, so no buffer from before the snapshot survives into the window.
- **Code.** A young object that is counted in the InitialMark pause is not marked. The trace from the roots reaches it, because the
  increment ran before the trace and the trace follows the fields. An unreachable one is dead and the sweep may free it.
- **Inferred.** An object that is reachable at the snapshot is marked by the trace or its deletion is in the SATB buffers. An object
  that a program stores into an already scanned object is caught by the packet of the slot increment (mark bit 0), not by the first trace.

### 2.6 Tests and controls

Selector `--f1-trace` of `lxr-rc-validation` (new, 79 477 checks; part of the Linux and Windows native lists):

| test | what it shows | control that must fail |
|---|---|---|
| `ValidateF1BlackAllocation` (nonmoving and nursery) | 300 black newborns are counted by an RC pause inside the window with at most 1 packet (the root). All are marked, none is swept when the trace ends | `LXR_F1_QUEUE_ALL_CONTROL` (the old behaviour) and `LXR_D2_NO_MARK_CONTROL` (no black marking) |
| `ValidateF1RootsOnly` (both modes) | InitialMark queues 0 packets for counted objects. The trace from the roots still marks them. The sweep frees exactly the dead cycle (2 objects) | `LXR_F1_QUEUE_ALL_CONTROL` |
| `ValidateF1SnapshotRoot` | an object that only a root holds at InitialMark is marked by the first trace. Then the program stores it into a scanned object. It survives the sweep | `LXR_F1_NO_ROOT_TRACE_CONTROL` (roots are not queued) |
| `ValidateBlackNewbornClosure` (old test, now in the selector) | a black newborn cannot hide an unmarked child | `LXR_F1_SKIP_ALL_CONTROL` (no packet even for an unmarked object) |
| `ValidateF1Stress` (8 seeds, 60 rounds each) | random graph, random stores and deletions, interleaved RefCount, InitialMark, FinalMark and Full pauses, with the trace running on the workers. After every pause every reachable object is counted, owned and linked as in the model. 8 seeds give 100 to 160 trace cycles | `LXR_F1_NO_ROOT_TRACE_CONTROL`, `LXR_D2_NO_MARK_CONTROL` |

**Measured** (`results/controls-f1-Release.txt`, `controls-f1-Debug.txt`): the baseline passes in both builds. All four controls fail
in both builds. The two old selectors `--root-batching` and `--trace-batching` counted the copies and appends of the old
increment trace. Their expected numbers were changed (3 copies per root became 2; 16 appends per parent became 8).

Other tests (F1 build, **Measured**; `results/f1-*`): all 35 native selectors pass in Debug and in Release; the legacy P2.1 to P3.4
suites pass (P3.4 has a flaky check, see section 7); the 90 managed cells with the Debug collector pass in the three modes.

### 2.7 Before and after (Measured)

| cell | before F1 (P24 matrix) | after F1 | note |
|---|---|---|---|
| list suffix nonmoving, wall s | 20.39 | 5.80 | CPU 55.3 s to 13.0 s |
| list suffix nursery, wall s | 28.80 | 6.92 | CPU 69.1 s to 15.8 s |
| binary trees nonmoving, wall s | 0.76 | 0.52 | |
| binary trees nursery, wall s | 0.82 | 0.64 | |
| requests nonmoving, wall s | 10.72 | 10.92 | F5 is the cause here |
| requests nursery, wall s | 4.32 | 4.33 | |
| list suffix nursery, RC pause median | 389.5 ms (P24 trace) | 85.0 ms (traced run) | publish_trace 204 ms and prepare_trace 79 ms are gone |
| trace packets queued for counted objects, per RC pause | about 260 000 (P24: Code and Measured) | 0 or 1 (the root of the pause; test `--f1-trace`) | the packet count of the increment work itself (1 024 slots per packet) was 515 per pause in the F1 build, see section 4.6 |
| binary trees, InitialMark pause | 142 ms (nonmoving), 159 ms (nursery) | 15.0 ms, 40.8 ms | traced runs |

Raw: esults/f1-*. The list suffix cell is now close to the trace-off result of P24 (5.7 s), as the P24 analysis predicted.

### 2.8 The "cycle never ends" problem (P24 Open 2)

- **Measured.** After F1, every traced run ends its cycles: list suffix has 1 or 2 InitialMark and 1 to 3 FinalMark in all
  6 runs of the matrix; before F1 the cycle did not end in 5 of 6 runs.
- **Inferred (mechanism).** It was a feedback loop, not a lost wakeup. Each RC pause added one packet per promoted object (about
  260 000). The workers drained fewer packets in the 30 ms between two pauses than the pause added. So `m_trace_pending` never
  reached 0 and `IsTraceExhausted` was never true. Each pause then also had to walk the backlog (`PublishTraceWork`,
  `RetireCompletedWork`, `FixForwardingReaders`). That made the pause longer and the window shorter. The run in which the cycle ended
  early was the run in which the backlog was drained once before it grew.
- **Open.** I did not find a true race. I did read the places where a packet can stay unsubmitted for one pause: `QueueTrace` resets
  `traceSubmitted` when the scheduler answers `Busy`, `StageDisabled` or `InvalidState`; the next `Resume` submits it. This delays
  the end of a cycle by one pause. It cannot keep a cycle open for ever. I have no run where it matters after F1.

### 2.9 What is still open for F1 (the rest of F2)

- `RetireCompletedWork` still walks every packet of the epoch at each pause. After F1 that list is short. **Open** for a trace with
  a big real backlog.
- `FixForwardingReaders` still rewrites the references of all unprocessed packets when a pause copies objects. The Java LXR resolves
  the forwarding when it uses a pointer. After F1 the packets are few. **Open.**
- `ProcessTraceWork` does not close over the children in the packet (the Java trace packet loops until its queue is empty). F4 does
  this for increments and decrements; the trace packet still makes one packet per 1 024 children. **Open.**

## 3. F5: the emergency rule counts the pages that hold data

### 3.1 The basics

After each pause the policy decides if the next pause is `Full`. This is the **emergency** rule. The Java LXR says: the next
pause is Full if `total_pages < min(total_pages * 15 / 100, 1 GiB) + mature_space_pages`. In words: less than 15 percent of the
heap is available.

Our heap is in blocks of 32 KiB. A block has 128 lines of 256 bytes. A block is **free** only when no line holds a live object.
If the pause does not copy young survivors (nonmoving mode), the survivors stay where they were allocated. They are spread over
all blocks. Every block then holds a few live lines, and no block is free.

### 3.2 What the Java LXR does

- **Code** (`src/plan/lxr/global.rs: next_gc_is_emergency_gc`, `decide_next_gc_may_perform_cycle_collection`): the rule above.
  `mature_space_pages` is `HEAP_AFTER_GC` minus the clean blocks that the lazy sweep released minus the released large-object pages.
  `HEAP_AFTER_GC` is `get_reserved_pages()` at the end of the pause (`gc_pause_end`). That is the page resource: whole blocks.
  `rc_stop_percent` is 15 (`src/args.rs`).
- **Code** (`src/policy/immix/rc_work.rs: SweepBlocksAfterDecs`, `block.rs: rc_sweep_mature`): a block with some free lines is
  marked reusable and stays reserved. It is counted as a whole block.
- **Code and Measured (P24).** So the Java LXR counts blocks, as we did. It does not reach the full state, because the RC pause
  copies the young survivors into dense blocks (`process_inc_and_evacuate`) and releases all other nursery blocks with one call.
  Its heap was 56 percent full after an RC pause, and it had 0 Full pauses in 112 pauses (requests).
- **Code.** In a Full pause the Java LXR also defragments (`select_mature_evacuation_candidates`). So a Full pause is useful there.

So the formula is the same. The data are not. Our nonmoving mode does not exist in the Java LXR.

### 3.3 What we did before (Measured, P24)

Requests, nonmoving: the Immix space is 72 MiB, the live data are about 15 MiB. After pause 6 every block was non-empty. The rule
was true after every pause. 52 of 58 pauses were Full. A Full took 121 to 152 ms and freed no block (`blocks_released` 0). The wall
time was 10.7 s. With `LXRRCStopPercent=10` the same cell had 0 Full and needed 4.2 s.

### 3.4 What F5 does

`LxrReferenceCounting::GetOccupiedPages` = pages of the non-free blocks, minus the free lines of the reusable blocks, plus the
mature large-object pages. `LxrGcHeap::FinishLazy` gives this number to the policy as `maturePages`. The policy code (`LxrCollectionPolicy`)
is not changed. The same number feeds the cycle decision and the prediction of live mature pages, as `mature_space_pages` does in
the Java LXR.

- **Why this is parity and not a knob.** The Java rule means "pages that hold mature data". A dense block is full of live lines, so
  the block count and the line count are the same number. Our nonmoving mode has sparse blocks, so only the line count keeps the
  meaning of the rule. For dense blocks `GetOccupiedPages` equals `GetMaturePages` (the old figure; it stays in the code and in
  the test).
- **No knob was changed.** `LXRRCStopPercent` is still 15 and the split between Immix and the large-object space is the same.
- **Code.** The figure is read after the lazy sweep, when the free lines of the promoted blocks are counted
  (`LxrGcHeap::FinishLazy`). The counters are the ones that `BlockCountersMatchWalk` checks against a walk.
- **Inferred.** A heap that is really full of live data still gets its emergency pause: the allocation path calls
  `CollectForPressure(emergency)` when it finds no hole, and the dense test below shows that the rule fires for a dense heap.

### 3.5 Tests and controls

Selector `--f5-policy` (new, 16 272 checks): one engine with 4 blocks where only 1 object in 100 survives (sparse), and one where
all survive (dense). **Measured** page figures: sparse 32 block pages and 6 occupied pages; dense 32 block pages and 31 occupied pages.

- sparse: the block figure calls the heap full, the occupied figure does not;
- dense: both figures call the heap full.

Control `LXR_F5_BLOCK_PAGES_CONTROL` (`GetOccupiedPages` returns the block pages): the sparse checks fail, in Debug and in Release
(`results/controls-f5.txt`).

Other tests (F5 build, **Measured**; `results/f5-*`): 36 native selectors in Debug and in Release pass; the legacy suites pass
(P3.4 flaky, section 7); the 90 managed cells with the Debug collector pass.

### 3.6 Before and after (Measured)

| cell | before F5 (F1 build) | after F5 |
|---|---|---|
| requests nonmoving, wall s | 10.92 | 3.99 |
| requests nonmoving, Full pauses (median of 3 runs) | 52 | 0 |
| requests nonmoving, pause time (gc s) | 6.20 | 0.40 |
| requests nonmoving, CPU s | 11.66 | 4.65 |
| requests nonmoving, RefCount pause median | 10.4 ms (only 3 RC pauses) | 17.1 ms (55 RC pauses) |
| requests with mature evacuation on (lxr-defrag), wall s | 16.6 (P24) | 4.22 (one run) |
| all other cells | | equal within the noise of the runs (section 1) |

LXRRCStopPercent=10 gave 4.15 s in P24; the F5 result is 3.99 s with the default 15. So the rule now behaves as the knob did, without the knob.

### 3.7 `LXREvacuateMature=0` in compare.sh (asked in the task)

`compare.sh` sets `LXREvacuateMature=0` for all LXR arms. The collector default is on. **Measured** with the F5 build, one run each
(`results/f5-mature-arms.txt`): requests defrag 4.22 s (16.6 s before F5), requests mature 4.28 s, binary trees mature 0.64 s,
list suffix mature 7.36 s. Compare with the arms that have it off in section 1. The values are in the same range. **Inferred:** the
setting is not wrong for these programs, but it hides the default. I did not change the default. I would remove the setting from the
next comparison script so that the arms run as the collector runs by default. **Open:** one run each is not a proof.

### 3.8 Still open for F5

- A Full pause in nonmoving mode still cannot free blocks. If a program really fills the heap with sparse blocks, the allocation
  fallback (`CollectForPressure(emergency)`) makes a Full pause. The Java LXR would defragment in that pause. This is F6 in P24 (to-space
  from a shared pool) and is not done.
- The Full pause traces on one thread (F7). Not done.

## 4. F4: the scheduler and the worker model

### 4.1 The basics

The collector runs its work in **packets** on **worker threads**. Before F4 we had 4 workers, one global mutex, one condition
variable, and `notify_all` for every packet that was submitted and for every packet that completed. Every packet from a
chain of young objects was one packet for each level of the chain.

### 4.2 What the Java LXR does

- **Code** (`mmtk-openjdk 0682434ae725`, `openjdk/thirdPartyHeapArguments.cpp: ThirdPartyHeapArguments::initialize`): the number of
  workers is `ParallelGCThreads` (the HotSpot rule: one worker per processor up to 8, then 5 for every 8 more; 16 processors give 13,
  **Measured** in P24). `ConcGCThreads = max((ParallelGCThreads + 2) / 4, 1)` (`scale_concurrent_worker_threads`): 3 of 13.
  The rule for `ParallelGCThreads` is from OpenJDK `vm_version.cpp` (`nof_parallel_worker_threads`), which is not in the pinned source:
  **Inferred**, confirmed by the 13 workers that P24 measured.
- **Code** (`mmtk-core src/scheduler/`): work buckets with lock-free `crossbeam` deques and injectors. A mutex and a condition variable
  only park idle workers; a new packet wakes one worker.
- **Code** (`src/plan/lxr/rc.rs: ProcessIncs::do_work`, `ProcessDecs::do_work`): a packet loops `while !new_incs.is_empty()`. Only a full
  buffer (1 024 slots) becomes a new packet. A chain of 63 nodes costs one packet. The trace packet (`cm.rs: do_work`) loops in the same way.

### 4.3 What F4 does

1. **Worker count** (`lxr_worker_policy.h`, `LxrGcHeap::Initialize`): `ParallelThreads(processors)` = the rule above, at least 1 and at most 64
   (our limit: each worker owns copy buffers). The processor count is `GCToEEInterface::GetCurrentProcessCpuCount()`: it knows the affinity
   and the container limit. Settings `LXRGCThreads` and `LXRConcGCThreads` override it (0 = the rule). 16 processors: 13 workers.
   1 to 8 processors: one worker for each processor.
2. **In-packet recursion** (`ProcessChildIncrements`, `ProcessDecrements`): the increments of the children of a counted object run in the
   same packet, one level after the other. The same for the decrements of the children of a dead object, with the SATB trace packet of the
   children kept (as `ReferenceBatch::Flush` did). A buffer that fills up (1 024 slots) still becomes a new packet so that other workers can
   take it. This is the F3 of P24.
3. **Wake only as many workers as there is work** (`lxr_work_scheduler.cpp`): idle workers wait on their own condition variable.
   A submitted packet wakes one worker, and only if the workers that are already on their way do not cover the waiting packets.
   A completed packet wakes only the threads that wait for the scheduler, and only if there are any. A stage that opens wakes as many
   workers as it has packets. Pause start, stage changes and shutdown still wake all.
4. **Limit of concurrent trace workers** (`ConfigureConcurrentTraceWorkers`): at most N workers run concurrent trace packets while the program
   runs. **The default is all workers, not the Java value of 3.** See 4.5.

### 4.4 Why this is correct

- **Code.** The scheduler state machine did not change: stages, buckets, completion counters, the all-workers-parked check. Only the
  wake-ups changed. A woken worker always polls the queues; the worker that finishes a packet polls by itself; the last worker that parks
  opens the next stage. A submit and the park of a worker both hold the mutex, so a wake-up cannot be lost.
- **Measured.** The P3.1 to P3.4 scheduler suites pass. One check of P3.1 (`workers repark after closed-stage submissions`) asserted that *all four*
  workers wake for two submissions. It encoded the broadcast. I changed it to "at least one worker wakes and reparks". P3.4 has a flaky check
  (section 7).

### 4.5 Tests and controls

Selector `--f4-scheduler` (new, 3 547 checks):

| test | what it shows | control that must fail |
|---|---|---|
| `ValidateF4WorkerPolicy` | table of 11 processor counts against the Java rule (16 gives 13 and 3) | `LXR_F4_WORKERS_CONTROL` (constant 4) |
| `ValidateF4Wakeups` | 2 000 packets on 8 workers: `signals` <= packets + 64. **Measured** 393 (the old code: 2 per packet) | `LXR_F4_NOTIFY_ALL_CONTROL` |
| `ValidateF4ConcurrentLimit` | limit 2 of 6: peak of concurrent trace packets is 2; other packets reach 6 | `LXR_F4_NO_CONC_LIMIT_CONTROL` |
| `ValidateF4Recursion` | a chain of 500 young objects costs 3 packets in the increment pause (old: 500 or more); the dead chain 5 | `LXR_F4_NO_RECURSION_CONTROL` |

**Measured** (`results/controls-f4.txt`): baseline passes, all four controls fail, in Debug and in Release.
Other tests (F4 build): 37 native selectors in Debug and Release; the 90 managed cells with the Debug collector; legacy suites.

### 4.6 Before and after (Measured)

| cell | after F5 | after F4 | note |
|---|---|---|---|
| list suffix nonmoving, wall s | 5.57 | 3.68 | 1.0 x WKS |
| list suffix nursery, wall s | 6.38 | 4.56 | 1.3 x WKS |
| binary trees nonmoving / nursery, wall s | 0.53 / 0.63 | 0.49 / 0.55 | |
| requests nonmoving / nursery, wall s | 3.99 / 4.07 | 4.04 / 4.16 | no gain |
| requests nonmoving / nursery, CPU s | 4.65 / 5.87 | 5.91 / 8.82 | **worse**, see 4.7 |
| list suffix, RefCount pause median (traced run) | 69.9 / 84.6 ms | 33.5 / 40.9 ms | nonmoving / nursery |
| list suffix, packets per RC pause (median) | 515 increment, 514 decrement | 10 increment, 9 decrement | esults/phases-f1-f4.txt |
| requests nursery, packets per RC pause | 27 increment, 26 decrement | 7 increment, 6 decrement | |

### 4.7 Worker counts and the trace limit (Measured, one run per cell, esults/f4-worker-sweep.txt, 4-conc-limit.txt, 4-thread-cpu.txt)

| workers (trace limit = workers) | requests nursery wall / CPU s | list suffix nonmoving wall / CPU s | list suffix nursery wall / CPU s |
|---|---|---|---|
| 4 | 3.93 / 5.53 | 4.38 / 8.89 | 5.38 / 11.64 |
| 8 | 3.93 / 6.74 | 3.64 / 9.64 | 4.66 / 13.28 |
| 13 (the Java rule for 16 processors) | 4.25 / 8.84 | 3.77 / 10.61 | 4.85 / 14.06 |

| list suffix nursery, 13 workers | wall s | CPU s |
|---|---|---|
| trace limit 13 (default) | 4.67 and 4.53 | 14.05 and 13.35 |
| trace limit 3 (the Java value) | 10.23 | 29.08 |
| 4 workers, trace limit 1 | 16.00 | 26.79 (the trace did not end in the traced run: esults/f4-trace-listsuffix-nursery-conc3-4workers-stuck.txt) |

- **Measured.** More workers shorten the big pauses (list suffix), and they cost CPU: 13 workers use 6.7 s of user time and 2.5 s of system
  time in requests nursery; 4 workers use 5.2 s and 0.74 s. Each of the 13 workers uses about 300 ms user and 180 ms system time.
- **Inferred.** The system time is the single global mutex (each worker takes it at every packet and every stage) and the worker start of
  each pause. I did not isolate it with perf. **Open.** Lock-free deques with local buffers (the rest of the Java scheduler) are not done.
- **Measured, with one process on 1, 2 and 4 processors** (	askset, esults/f4-small-machine.txt): all cells finish with the
  same checksums, and requests needs 3.7 to 4.3 s of CPU on one processor. The worker count follows the affinity (1 processor: 1 worker).
- **Why the trace limit is not the Java value.** **Measured:** with 13 workers and 3 trace workers list suffix nursery needs 10.2 s with 68 pauses
  (default: 4.5 s, 51 pauses). In the traced run with 4 workers and 1 trace worker the trace never ended (1 InitialMark, no FinalMark in 84 pauses) and
  FixForwardingReaders took 177 ms per pause. **Inferred:** the trace cannot finish in the 30 ms between pauses with few workers, so the backlog grows,
  and our pause cost still follows the backlog (F2 is open). The Java LXR makes this walk cheap (pause_concurrent_marking_work_packets_during_gc).
  The mechanism is in the code (ConfigureConcurrentTraceWorkers, tested). The default is all workers. This is the one place where F4 is **not** at parity.

## 5. What I ran, and what I did not run

All **Measured**. "Passed" means the command printed it. Logs and outputs are in `results/`.

| item | where / how | result |
|---|---|---|
| Native selectors, Linux Debug and Release, after each fix | `scripts/chain-all.sh`, P17 `native-linux.sh` (selectors added: `--f1-trace`, `--f5-policy`, `--f4-scheduler`) | F1: 35 of 35, F5: 36 of 36, F4: 37 of 37 pass in both builds |
| Native selectors, Windows Release (MSVC, `subst W:`), final tree | `native-windows.ps1 -Config Release -Repo W:\` | 37 of 37 pass (`results/windows-nat-Release.txt`) |
| Windows Release collector build | `build-windows.ps1 -Config Release` (needed the four ignored version headers copied from an older worktree) | built; `clrlxrgc.dll` SHA256 `06DBDF40...B2E1` (`results/windows-build-release.txt`). The first MSVC build failed on `std::min` (Windows `min` macro). Fixed in its own commit |
| Legacy suites P2.1 to P3.4, Linux | P17 `legacy-linux.sh` | all pass except one flaky check of P3.4, see section 7 |
| Managed cells with the Debug collector, 3 modes (nonmoving, nursery, mature), ReadyToRun on | `scripts/run-managed.sh`, P17 `run-managed.py`, D2 fixtures | 90 of 90 cells ok after F1, F5 and F4 |
| Controls | `controls-f1.sh`, `controls-sel.sh`, `results/controls-*.txt` | F1: 4 of 4 fire, F5: 1 of 1, F4: 4 of 4; each in Debug and in Release; baselines pass |
| Stock-GC helper hash | `scripts/hash-helpers.sh` on the `libcoreclr.so` of the frames | `46cee8bd87fd6252f5265267148d0a34`, equal to the baseline. **Code:** the three commits change only files of `libclrlxrgc` (`lxr_*`, its `CMakeLists.txt` list). `libcoreclr.so` was not rebuilt, so the hash could not change |
| Benchmark (P20/P23 method) | `scripts/bench.sh`: `matrix.sh` of P24 with 3 repetitions, arms wks, server, lxr-nonmoving, lxr-nursery, then one traced run per LXR cell | after each fix; all checksums equal; no failed run |

**Not run:**

- The full Linux test chain **after** the Windows build fix (a one-line change from `std::min` to a conditional). Only the rebuild, three selectors and one run of the cells were repeated (`results/final-sanity.txt`).
- Linux **Debug runtime** (`d2dbgrt` frame) and the Linux **Debug-collector build of the full runtime**. I used the Release runtime with the Debug collector (as P22 did for the managed cells).
- Windows **Debug** and the Windows managed cells. Only the Windows Release build and the Windows Release native selectors ran, once, at the end.
- ARM64, NativeAOT, profiler heap walks, GCStress runs.
- `perf` profiles of the final build (the CPU analysis in 4.7 is from thread CPU times only).
- The Java LXR was not run again. The comparison with it is by source reading and by the P24 measurements.

## 6. What is still different from the Java LXR

| # | item | state |
|---|---|---|
| 1 | Work queues | **Open.** One global mutex and one queue per stage with a local cache of 16 packets. The Java LXR has lock-free deques with stealing. CPU is 3.0 to 5.6 times WKS (requests nursery 5.6) |
| 2 | Concurrent trace workers | **Open.** The Java default (3 of 13) does not work until the pause cost is independent of the trace backlog (rest of F2). Default here: all workers |
| 3 | Rest of F2 | **Open.** `RetireCompletedWork` and `FixForwardingReaders` still walk packets and logs. Forwarding is not resolved at use |
| 4 | Trace packet recursion | **Open.** The SATB trace packet still makes a packet per 1 024 children; the Java trace packet loops |
| 5 | Full pause | **Open (F7, F6).** One thread, no defragmentation when no block is free |
| 6 | Lazy decrements wait (F8) | **Open.** `post_drain` is still on the allocating thread: 13 to 14 ms per pause in list suffix |
| 7 | Per-object RC cost, refill cost, prefetch (F9, F10) | **Open** |
| 8 | Emergency rule | **Different on purpose (F5).** Counts occupied pages, not whole blocks. Equal for dense blocks |
| 9 | Increment trace of unmarked objects in a trace window | **Different on purpose (F1).** A weak target stored into a black newborn is traced. The Java LXR relies on a keep-alive barrier of `Reference` |
| 10 | Workers per processor | Same rule. Cap of 64 workers is ours |

## 7. Notes, open questions, and things I noticed

1. **A flaky check in P3.4** (`child registration blocks decrement zero and release`, `lxr-completion-counter-validation.cpp`). It fails in 5 of 30 runs
   with the scheduler of the tip before F4 and in 6 of 30 runs with the F4 scheduler (`results/f4-p34-flake-rate.txt`), and 3 of 8 and 1 of 10 runs in the F5 chain.
   So the rate did not change. **Inferred:** the check reads a snapshot while a child packet is in flight. I did not fix it. It should be fixed or
   quarantined on its own.
2. The P3.1 check `workers repark after closed-stage submissions` asserted the broadcast wake-up. I changed it (section 4.4).
3. The two checks `--root-batching` and `--trace-batching` counted the copies of the old increment trace. I changed their numbers (section 2.6).
4. **List suffix nursery after F4 still has a RefCount pause of 41 ms median and a maximum of 200 ms** (one FinalMark of 193 ms in the traced run). I did not
   find the cause of that FinalMark.
5. **Question for the coordinator:** should the default of the concurrent trace limit follow the Java value (3) once the rest of F2 is done? I have kept
   all workers as the default and the Java value one setting away (`LXRConcGCThreads=3`).
6. **Question:** `compare.sh` sets `LXREvacuateMature=0`. I advise to drop it in the next comparison (section 3.7).
7. The benchmark machine was about 5 to 18 percent slower in this session than in P23 (WKS 1.71 to 4.05 s in different columns). Use the ratios.
8. `binary trees` ends before its first FinalMark in every run (1 InitialMark, 0 FinalMark). This is the same as in P24.
9. The `Debug` check of black allocation (`ProcessIncrement`, `_DEBUG`) is a new assertion in Debug builds. It is not in Release.

## 8. Commits and files

Commits on `kkokosa-lxr-parity-fixes-f1-f5-f4` (local only):

| commit | title |
|---|---|
| `85d1955b23f` | F1: no trace packet for an object that needs none (Java LXR parity) |
| `179b9c2eeb3` | F5: emergency rule counts the pages that hold data, not whole blocks |
| `efcb21cfc67` | F4: Java LXR worker count, in-packet recursion, targeted wake-ups |
| `98a3b54484c` | F4: use a conditional instead of std::min (the Windows min macro broke the MSVC build) |
| the commit that adds this document | P25: document, tables, P19 update |

Product files: `lxr_reference_counting.cpp/.h`, `lxr_gc.cpp`, `lxr_work_scheduler.cpp/.h`, `lxr_worker_policy.h` (new), `CMakeLists.txt` (one header listed).
Tests: `P4/lxr-rc-validation.cpp` (selectors `--f1-trace`, `--f5-policy`, `--f4-scheduler`), `P3.1/lxr-work-packet-validation.cpp` (one check), the selector lists in `P17-pause-cost-and-reserve/native-linux.sh` and `native-windows.ps1`.

`scripts/`: `build.sh` (build the collector in WSL), `sync.sh`, `sync-src.sh`, `one.sh` (one native selector, `EXTRA=-D<control>`), `controls-f1.sh`, `controls-sel.sh`,
`tests-native.sh`, `chain-native.sh`, `chain-all.sh`, `run-managed.sh`, `bench.sh`, `trace1.sh`, `pausemed.py`, `quick.sh`, `sweep-threads.sh`, `limit-test.sh`, `limit-test2.sh`,
`evidence-f4.sh`, `small.sh`, `threadcpu.sh`, `repeat-p34.sh`, `repeat-legacy.sh`, `baseline-p34.sh`, `hash-helpers.sh`, `phases-final.sh`, `mk-tables.py`.
`results/`: `f1-*`, `f5-*`, `f4-*` (matrix, tables, pause medians, traces, native and legacy output), `managed-*`, `controls-*`, `windows-*`, `helper-hash.txt`, `tables.md`.

To repeat: `scripts/sync.sh`, then `scripts/build.sh <worktree> Release`, then `scripts/bench.sh <frame> <tag>`. Paths in the scripts are those of this machine (`/root/p25`, the D2 frames and fixtures).
