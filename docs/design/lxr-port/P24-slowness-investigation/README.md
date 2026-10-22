# P24: Why is LXR slower than Workstation GC? (investigation only)

Status: investigation only. **No product code was changed.** Nothing under `src/` is different from the tip `287e1ed0164`
(branch `kkokosa-lxr-d2-bump-allocation`). This branch holds only this document, scripts and raw results. Nothing was pushed.

Labels: **Measured** = I ran it and saw the result. **Code** = I read it in the source (file and function are named).
**Inferred** = I conclude it from other facts, and it can be wrong. **Open** = not known, or not done.

Words used here:

- **the Java LXR** = the LXR collector of MMTk in OpenJDK: mmtk-openjdk `0682434ae725`, mmtk-core `304ce69d43aa`, OpenJDK
  `7caf8f7d19b`. I read the source of exactly these versions (exported with `git archive`, not the newer `head/mmtk-core`).
- **pause** = the time when the program threads are stopped. **RC pause** = a pause that only does reference counting work
  (the Java LXR calls it `RefCount`). **InitialMark / FinalMark** = the pauses that start and end a concurrent trace.
  **Full** = a pause that traces the whole heap.
- **SATB trace** = the concurrent trace of the live heap that finds dead cycles. **Packet** = one unit of work for the worker threads.
- **Black allocation** = an object that is allocated while a trace runs is marked at once. The trace does not need to visit it.
- **Nonmoving / nursery / defrag / mature** = the four modes of our collector: no copy; copy young objects at the RC pause;
  no young copy but copy mature objects; both. (`DOTNET_LXREvacuateNursery`, `DOTNET_LXREvacuateMature`.)
- **WKS** = .NET Workstation GC. **x WKS** = ratio of the median wall time to WKS in the same table.

## 1. Short answers

**Question 1: why is a list suffix nursery pause about 450 ms?**

1. **Measured.** The pause is not slow because of the copy. About 80 percent of its time is work that grows with a *backlog of
   trace packets*. In the typical run a concurrent trace (SATB) is active from the start and **never ends**. A gdb snapshot
   after 14 s shows 326 520 trace packets that are not done (423 666 work objects in the list). The marked-object counter
   does not move for 27 s.
2. **Code.** Every promoted object gets its own trace packet during an RC pause (`LxrReferenceCounting::ProcessIncrement`,
   the call `QueueTrace(&target, 1)`), although the object was allocated black and the trace does not need it. This is
   about 260 000 packets per pause. The Java LXR queues the roots only.
3. **Measured.** In the pause, the backlog costs four serial or lock-bound steps: `PublishTraceWork` 204 ms, `FixForwardingReaders` 79 ms,
   the suspend step 32 ms (mainly `RetireCompletedWork`, **Inferred**), and slow increments 67 ms. The same program, in a state without a
   trace, has an RC pause of 51 ms with the same 262 000 increments.
4. **Measured.** If the concurrent trace is off (latency mode Batch) the cell takes 5.7 s instead of 26.8 s (nursery) and 5.0 s
   instead of 19.9 s (non-moving). A one-line diagnostic change (skip the trace packet of an object that is already marked) gives
   8.0 s and 5.6 s with the trace still on.
5. **Measured.** The CPU time of 66 s for a wall time of 27 s is mostly not spin. It is 4 workers that sleep and wake all
   the time on one global mutex: kernel code is 37 percent of all samples, mostly futex wake and wait.
6. **Inferred.** The state is a feedback loop: slow pause, short mutator window (about 30 ms), no time for the trace to finish,
   more backlog, slower pause. This also explains why this cell was 11 s in P20 and is 15 to 31 s in other runs: it depends on
   whether the first trace cycle ends early.

**Question 2: why are 52 of 58 requests non-moving pauses Full?**

1. **Measured.** The cause is the emergency rule, not memory pressure. After pause 6, `m_emergency` is true after every pause, and
   `LxrCollectionPolicy::Select` then returns `Full`. The rule is `totalPages < 15 percent of totalPages + maturePages`.
2. **Measured.** `maturePages` is 19 009 of 21 504 pages (88.4 percent). It counts blocks that are not empty. All 2 368 Immix blocks
   are non-empty (state `Reusable`), but the live data is about 15 MiB of 74 MiB.
3. **Code and Measured.** The Immix space alone (2 368 blocks, 18 944 pages) is larger than 85 percent of the heap (18 278 pages),
   so the rule is true as soon as the blocks are all non-empty. Nonmoving mode never empties blocks, so no Full can change the answer.
4. **Measured.** The Full is a real stop-the-world trace of about 307 000 objects. It takes 121 ms (median), and 110 ms of that is one
   thread (`prepare_trace` wall time equals busy time). It reclaims no block (`blocks_released` is 0).
5. **Measured.** With `LXRRCStopPercent=10` or `5` the cell has 0 Full and needs 4.2 s (not 10.7 s), like the nursery mode.
   With the Immix and LOS split 64/20 MiB it has 0 Full too. The decision is on a cliff.
6. **Code and Measured.** The Java LXR has the same rule, but it never reaches this state: it copies young objects into dense blocks
   at every RC pause and its heap stays at about 56 percent. It had 0 Full in 112 pauses. When our mature evacuation is
   switched on (`lxr-defrag`), it selects 0 blocks, because it may use only free blocks as to-space and there are none. The cell becomes
   slower (16.6 s).

**Top 5 ranked causes** (wall time that each explains, from the experiments in sections 5 to 8):

| # | Cause | Evidence (Measured) |
|---|---|---|
| 1 | Trace backlog: one packet per promoted object, and pause work that is proportional to the backlog | list suffix: 22.3 s of the 25.2 s gap (nursery), 14.6 s of 16.8 s (non-moving); binary trees 0.22 s of 0.42 s |
| 2 | Emergency rule fires on block count; no copy to make blocks empty | requests non-moving: 6.6 s of the 9.0 s gap |
| 3 | Worker model: 4 threads, one global mutex, `notify_all` per packet, one packet per level of a chain | kernel code (mostly futex) is 17 to 37 percent of all CPU samples; CPU is 3.5 to 15 times WKS; more workers help only to 8 |
| 4 | Per-object cost of the RC work (about 330 ns CPU per increment when clean, 1.7 us on requests) and of allocation refills | requests nursery: pause 12.9 ms against 1.6 ms in the Java LXR; `LxrGcHeap::Alloc` is 17.8 percent of samples |
| 5 | The allocating thread waits for the lazy decrements after every pause (`post_drain`) | list suffix with trace off: 1.5 s of 5.8 s wall |

Start-up and ReadyToRun: **Measured**, 14 ms for LXR (32 ms with ReadyToRun off): at most 3 percent of any gap.

**Biggest differences from the Java LXR** (section 7 has the full table): per-object trace packets and no black-allocation skip;
packet per level (no in-packet recursion); 4 workers with a global-mutex scheduler (Java: 13 workers, 3 concurrent, lock-free deques);
emergency rule on block count with a static Immix/LOS split and no way to get to-space when the heap is full; Full trace on one thread;
`FixForwardingReaders` rewrites every queue at every pause (Java resolves forwarding when it uses the pointer).

## 2. How the benchmark and the machine were used

- **Measured.** WSL Ubuntu 22.04, 16 logical cores, nothing else running. One run at a time. Natural collections only, no
  `GC.Collect`. Frame `p24-Release-linux` = the `d2-Release-linux` frame of P23 with only `libclrlxrgc.so` replaced. I built the
  new library from the sources of this worktree (`scripts/build-diag.sh`; unstripped, so `perf` and `gdb` see the symbols). The
  library has the same size as the P23 library but not the same bytes (build paths). Check: the list suffix nursery cell took
  26.4 s with the new library and 29.9 s with the P23 library, in the same session.
- **Method of P23.** `scripts/run1.sh` copies the settings of `compare.sh`: ReadyToRun on, `TieredCompilation=0`, heap 4 times the
  smallest Workstation heap (binary trees 40 MiB, requests 84 MiB, list suffix 40 MiB), Immix space = heap minus heap/8,
  LOS = heap/8, `LXRLegacyCapacity=1`, `LXRManagedHeapHardLimitMB` = heap.
  **Important and Measured in the script:** `compare.sh` sets `LXREvacuateMature=0` for all LXR arms. The default of the
  collector is both copy modes on. So the six cells of P23 never ran mature evacuation. The arm `lxr-defrag` (no young copy,
  mature copy) and `lxr-mature` (both) are in section 5 and section 6.
- **Programs.** The P14 programs. Binary trees: allocate trees of depth 4 to 16, keep one. Requests: a table of 32 768 entries, each
  request builds a list of 24 items (128-byte arrays), reads 8 table entries, and replaces 1 entry in 8. List suffix: 4 096 lists of 64
  nodes; each operation walks to a random node and rebuilds the rest of the list (31.5 new nodes of 32 bytes on average).
- **Java.** `scripts/java-lxr.sh` runs the Java LXR on the same programs with `MMTK_VERBOSE=3`. The Java heap is 4 times the Java Serial
  minimum (7, 13, 7 MiB; so 28, 52, 28 MiB). I compare Java and .NET only by ratios and shapes, never by absolute times.
- All checksums are equal in all runs (three values only: 14985972, 317308156512, 301262627042).

### 2.1 The six cells again (Measured; `results/reports/matrix-table.txt`, 3 runs each, median)

| program | arm | wall s | cpu s | gc s | pauses | full | RSS MiB | wall x WKS | cpu x WKS |
|---|---|---|---|---|---|---|---|---|---|
| binary trees | wks | 0.34 | 0.25 | 0.08 | 87 | | 54 | 1.0 | 1.0 |
| binary trees | lxr-nonmoving | 0.76 | 1.35 | 0.35 | 17 | 0 | 145 | 2.3 | 5.5 |
| binary trees | lxr-nursery | 0.82 | 1.45 | 0.41 | 19 | 0 | 131 | 2.4 | 5.9 |
| requests | wks | 1.72 | 1.58 | 0.23 | 277 | | 83 | 1.0 | 1.0 |
| requests | lxr-nonmoving | 10.72 | 11.58 | 6.03 | 58 | 52 | 205 | 6.2 | 7.3 |
| requests | lxr-nursery | 4.32 | 6.86 | 1.18 | 69 | 0 | 154 | 2.5 | 4.3 |
| requests | lxr-defrag | 16.56 | 17.45 | 11.79 | 58 | 52 | 205 | 9.6 | 11.1 |
| requests | lxr-mature | 4.41 | 6.98 | 1.44 | 66 | 0 | 176 | 2.6 | 4.4 |
| list suffix | wks | 3.57 | 3.59 | 2.00 | 192 | | 68 | 1.0 | 1.0 |
| list suffix | server | 1.57 | 1.96 | 0.64 | 50 | | 74 | 0.4 | 0.5 |
| list suffix | lxr-nonmoving | 20.39 | 55.30 | 17.29 | 62 | 0 | 220 | 5.7 | 15.4 |
| list suffix | lxr-nursery | 28.80 | 69.06 | 25.57 | 60 | 0 | 219 | 8.1 | 19.3 |
| list suffix | lxr-defrag | 30.03 | 70.72 | 26.51 | 63 | 0 | 224 | 8.4 | 19.7 |
| list suffix | lxr-mature | 29.95 | 72.16 | 26.66 | 64 | 0 | 219 | 8.4 | 20.1 |

(The Server GC rows of binary trees and requests are 1.0 and 1.1 x WKS and are in the result file.) These numbers reproduce P23 within about 10
percent. The ratio of list suffix nursery is 8.1 here and 8.9 in P23, because WKS needs 3.57 s here and 3.41 s there.
Mature evacuation changes nothing for list suffix and binary trees. For requests it helps nursery by nothing and hurts non-moving
(section 6.5).

### 2.2 Where the wall time goes (Measured; `results/reports/split-table.txt`, seconds)

`wall` = process wall time. `other` = wall minus the measured part of the program (start-up, warm-up run, shutdown).
`mutator` = measured part minus the pause time that the runtime reports. `gc` = reported pause time.

| program | arm | wall | other | mutator | gc |
|---|---|---|---|---|---|
| binary trees | wks | 0.34 | 0.10 | 0.16 | 0.08 |
| binary trees | lxr-nonmoving | 0.76 | 0.12 | 0.29 | 0.35 |
| binary trees | lxr-nonmoving, trace off | 0.54 | 0.13 | 0.30 | 0.11 |
| requests | wks | 1.72 | 0.19 | 1.30 | 0.23 |
| requests | lxr-nonmoving | 10.72 | 0.45 | 4.24 | 6.03 |
| requests | lxr-nursery | 4.32 | 0.33 | 2.81 | 1.18 |
| requests | lxr-nursery, trace off | 4.08 | 0.37 | 2.74 | 0.96 |
| list suffix | wks | 3.57 | 0.22 | 1.35 | 2.00 |
| list suffix | lxr-nonmoving | 20.39 | 0.83 | 2.27 | 17.29 |
| list suffix | lxr-nonmoving, trace off | 5.80 | 0.44 | 3.16 | 2.19 |
| list suffix | lxr-nursery | 28.80 | 0.93 | 2.30 | 25.57 |
| list suffix | lxr-nursery, trace off | 6.46 | 0.67 | 3.03 | 2.75 |

Note: for list suffix the "mutator" of the default arm is small because the pause time is large; with the trace off, "mutator"
grows because it contains the wait of the allocating thread for the lazy decrements (`post_drain`, section 5.6).

## 3. Basics: what the Java LXR does (from the source)

**Code** (mmtk-core `304ce69d43aa`; file names are in `src/plan/lxr/` and `src/policy/immix/`).

1. **Allocation** is a bump pointer in Immix blocks (32 KiB, 128 lines of 256 bytes). A young object has a count of 0.
2. **Write barrier** (`barrier.rs`): on the first store into a field of an object (the "unlog" bit is 1), the barrier clears
   the bit, pushes the **old value** to a decrement buffer and the **slot** to an increment buffer. Buffers have 1 024 entries
   and become work packets while the program runs.
3. **RC pause** (`global.rs: schedule_rc_collection`, `rc.rs: ProcessIncs`): stop the threads, scan roots, and apply the increments.
   An object with count 0 is young. **At this first increment it is copied** (`process_inc_and_evacuate`, when
   `RC_NURSERY_EVACUATION` is on, the default) into a to-space block, or, if it is in a recycled line, it stays and its block
   is marked "promoted in place". The increments of the children are done **in the same packet**: `ProcessIncs::do_work`
   loops `while !new_incs.is_empty()`, and only a buffer of 1 024 slots becomes a new packet.
4. **Release** (`block_allocation.rs: sweep_nursery_blocks`): all nursery blocks that did not keep an object are returned with one
   call (`bulk_release_blocks`). No dead young object is visited.
5. **Lazy decrements** (`rc.rs: ProcessDecs`): the old values run after the pause, in parallel with the program. A dead mature object
   decrements its children in the same packet (loop `while !new_decs.is_empty()`). The block of a dead object goes on a list
   and `SweepBlocksAfterDecs` frees it later (`rc_work.rs`).
6. **Cycle collection**: after an RC pause, `decide_next_gc_may_perform_cycle_collection` computes `mature_space_pages` and decides:
   - emergency (`next_gc_is_emergency_gc`): `total_pages < min(total * rc_stop_percent / 100, 1 GiB) + mature_space_pages`;
     `rc_stop_percent` = 15. The next pause is then `Full` (or `FinalMark` if a trace runs).
   - cycle (`next_gc_is_cycle_gc`): `garbage * 100 >= trace_threshold * total_pages`, with `garbage` = mature pages minus the predicted
     live pages, `trace_threshold` = 20. The next pause is then `InitialMark`.
7. **SATB trace** (`cm.rs: LXRConcurrentTraceObjects`): concurrent workers trace from the roots of InitialMark. The packets close over
   their children in a loop. Objects that are allocated while the trace runs are **marked at allocation** (`block_allocation.rs:
   initialize_new_clean_block`, `immixspace.rs: rc_get_next_available_lines`). The decrement buffers are also the SATB buffers
   (`barrier.rs: flush_decs_and_satb`). In `ProcessIncs::promote` there is no trace packet for a promoted object; during InitialMark
   only the roots are queued.
8. **Mature evacuation** (`rc_work.rs: select_mature_evacuation_candidates`, `mature_evac.rs`): at InitialMark and Full, blocks are
   selected (in a Full: all blocks that are fragmented, with a budget `max(total - used, headroom)` where `headroom` = 5 percent of the
   used pages). The copy happens at FinalMark and Full.
9. **Triggers** (`global.rs: collection_required`): heap full including the reserve, trace drained, predicted survival >= 128 MiB,
   increment limit, nursery block limit, young limit. By default only the first three can happen.
10. **Threads**: `ParallelGCThreads` workers (13 on 16 CPUs), `ConcGCThreads` of them for concurrent work (3). Queues are `crossbeam`
    deques and injectors (lock-free). A mutex and a condition variable only park idle workers; adding one packet wakes one worker
    (`notify_one`).

**Measured** (Java log, `results/reports/java-summary.txt`, one run per program, the whole run):

| program | pauses | RefCount median / max (ms) | InitialMark / FinalMark | Full | median young allocation per RC pause |
|---|---|---|---|---|---|
| binary trees | 20 | 0.93 / 7.6 | 2 / 2 | 0 | 19 MiB |
| requests | 112 | 1.58 / 4.0 | 3 / 3 | 0 | 20 MiB |
| list suffix | 155 | 4.60 / 8.7 | 2 / 2 | 75 (median 32.9 ms, max 39.7) | 5 MiB |

The Java LXR is itself slow on list suffix: 57 percent of its time is in pauses (this is the same share as Java Serial, 60 percent, in P14),
and its heap sits at 93 to 96 percent after each RC pause (for example `27M->26M(28M)`), so every second pause is a Full. Its pause times are still short (a median of 4.6 ms).

## 4. How our collector is built (Code)

- `LxrGcHeap::Alloc` (`lxr_gc.cpp`) gives each thread a buffer = one Immix hole. The stock helper bumps inside it (P22). A refill does
  bulk metadata work (`ImmixAllocationManager::PrepareFreshBlock`, `PrepareRecycledHole` in `immix_allocation.cpp`). If a trace runs, the
  buffer is marked at once: **black allocation exists** (`InitializeObjectMarkRange(..., 1)` when `markingContext == ConcurrentOrFinalMark`).
- A collection is `CollectLocked` (`lxr_gc.cpp`): `DrainEpoch` (wait for the lazy work of the last pause), pause (`RunPause`), then `PostDrain`
  (wait for the lazy work again, **on the thread that triggered the pause**, outside the stop-the-world time).
- The policy (`lxr_collection_policy.h`) is a copy of the Java formulas: `Trigger`, `FinishLazySweep` (emergency and cycle hint),
  `Select`. Differences: a reserve in bytes for the copy (P17 S5a); a static split of the capacity into Immix and LOS (`LXRLegacyCapacity`).
- The engine is `lxr_reference_counting.cpp`. Packets are `Work` objects. `NewWork` allocates a `Work` (`new`) and an array (`new`) and links
  it into the list `m_work`. `ReferenceBatch::Flush` makes one new packet from the children **at the end of a packet**. The workers are
  `LxrWorkScheduler::Impl::WorkerLoop` (`lxr_work_scheduler.cpp`): **4 workers** (`LxrGcHeap::Initialize`, the constant `4`), one `std::mutex`, one
  `std::condition_variable`; `Submit` and every packet completion call `notify_all`.
- At each pause: `Suspend` calls `RetireCompletedWork` and `FlushMutators`, `PrepareWork` makes packets from the log streams, the packet
  `PrepareTrace` runs `FixForwardingReaders` (twice) and, in a Full, the whole trace (`DrainTraceInsidePause`), `ReleaseEpoch` frees
  nursery blocks, and `Resume` calls `PublishTraceWork` when a trace is active.

## 5. Question 1: list suffix, nursery

### 5.1 What one pause has to do (Measured)

Trace `results/trace-listsuffix-nursery-r1.txt`, 66 pauses, phases of the S0 timers (`results/reports/phases-trace-listsuffix-nursery-r1.txt`).

| per RC pause (median) | value |
|---|---|
| pause start to pause start | about 420 ms |
| stop-the-world time | 389.5 ms |
| young allocation since the last pause | 12.8 MB |
| increments / decrements processed | 262 000 / 262 000 |
| objects copied | 0 (copying stops after pause 11) |

Why so many increments? One operation rebuilds on average 31.5 nodes. In one pause each list is changed about 3 times, and every node after the
smallest cut point is young. So 45 to 65 percent of the young bytes survive, and almost the whole live set (262 144 nodes) is counted
again. **Inferred** from the program; the counts are **Measured**. WKS does the same amount of survivor work (a gen0 GC copies the same survivors).

Where the 389.5 ms go:

| step | ms (median) | share | what it is |
|---|---|---|---|
| `publish_trace` | 203.9 | 52 % | `PublishTraceWork` in `Resume`: for every unprocessed trace packet, `Submit` under the global mutex |
| `prepare_trace` | 79.3 | 20 % | `FixForwardingReaders`: one thread walks roots, both decrement logs and every `Work` object |
| `inc_wall` | 67.4 | 17 % | the 262 000 increments (busy time 256 ms: 4 workers) |
| `suspend_prepare` | 31.7 | 8 % | `RetireCompletedWork` (a walk over every `Work` object; `perf`: 1.9 % of samples, about 20 ms per pause) and the rest of the suspend step (**Inferred** split) |
| the rest | 7 | 2 % | |

The same program in a state **without an active trace** (`results/trace2-listsuffix-nursery-r1.txt`, the 10.7 s run, pauses after FinalMark): RC pause
median **51 ms**, `publish_trace` 0, `prepare_trace` 1.4 ms, `suspend_prepare` 0.3 ms, `inc_wall` 47 ms, with 305 000 increments. A clean state with trace off
(`results/trace3-listsuffix-nonmoving-batch.txt`): RC pause 40 ms, `inc_wall` 38 ms, 290 000 increments.
So the pause work itself is about 40 to 50 ms. The other 340 ms are caused by the trace state.

### 5.2 The trace never ends (Measured)

- Six traced runs (`results/rep-ls-nur/`, `scripts/repeat-trace.sh`): wall time 27.6 to 31.5 s. In 5 of 6 runs the trace phase was
  already `Concurrent` at the first event (the trace started in the warm-up run) and no FinalMark came. In the sixth run there was one
  InitialMark and one FinalMark, but 64 of its 69 RC pauses had the trace active.
- In `results/rep-ls-nur/run1.txt` the counter of traced objects is 115 141 at the start and **115 141 at the end**: the trace marks nothing new in 27 s.
- gdb snapshot (`scripts/gdb-trace.sh`, `results/gdb-trace-state.txt`), 14 s after the start:
  `m_trace_pending = 326 520`, `m_trace_phase = Concurrent`, and the list `m_work` has **423 666** work objects: 185 108 trace packets that were never
  submitted, 141 411 that are submitted and not processed, 96 624 processed and not yet freed.
- The same program had one run in 10.7 s (`results/trace2-listsuffix-nursery-r1.txt`). There InitialMark was at 0.67 s and FinalMark at 5.6 s; the 18 RC
  pauses in between took 130 to 527 ms (`publish_trace` grew to 379 ms), and after FinalMark the pauses took 50 ms.
  So the state is **bistable**: when the backlog is drained before it grows, the cycle ends and everything is fast. Otherwise it never ends.
  This also fits the earlier observation (P22, P23) that the cell is "bimodal" and that P20 had 11 s: **Inferred**, one fast run seen in about 12.

### 5.3 Why there is a backlog (Code, then Measured)

1. `LxrReferenceCounting::ProcessIncrement`: after the increment, `if (trace && GetTracePhase() != Inactive)` the object is queued with
   `QueueTrace(&target, 1)` when `targets == nullptr` (every RC pause). `QueueTrace` calls `NewWork`: one `Work` object, one `new[]`, two list pushes and
   one increment of `m_trace_pending`. **This is one packet per promoted object.** The object does not need a visit: during a trace
   the allocator marks every buffer it hands out (`PrepareFreshBlock`, `PrepareRecycledHole`), and the trace is SATB. The visit finds the mark bit set and returns
   (`ProcessTraceObject`: `mark != 0`). **Measured:** `ProcessTraceObject` is the top user function in `perf` (11.7 % self, 17 % inclusive), and
   the counter of marked objects does not move.
2. The decrement side does the same: `ReferenceBatch::Flush` for decrements calls `QueueTrace(references, count)` when a trace is active.
3. The workers can drain only a part. The mutator window between two pauses is about 30 ms (13 MB at about 400 MB/s). The trace packets and the lazy decrements
   share the same 4 workers.
4. In the pause every piece of work that is proportional to the backlog runs: `RetireCompletedWork` (walk of `m_work`), `FixForwardingReaders` (walk of `m_work`
   and of both decrement logs, one `ResolveForwarded` per entry: a cache miss each), `PublishTraceWork` (walk of `m_work`, `Submit` for each trace packet that
   is not yet submitted).
5. Each `Submit` locks the global mutex and calls `notify_all` (`LxrWorkScheduler::Submit`), and each completed packet locks it again and calls `notify_all`
   (`WorkerLoop`). **Measured** with `perf` (cpu-clock, user and kernel, `results/reports/perf-ls-nursery-k.txt`): 35 721 samples (71 s of CPU, equal to `cpu_ms`);
   the 4 workers have 85 % of the samples (21 % each), the allocating thread 15 %; kernel code is 37 % of all samples: `futex_wake` (via `__lll_lock_wake`,
   17.5 %) and `futex_wait` (via `__lll_lock_wait`, 10.3 %).
   User-only `perf` (`results/reports/perf-ls-nursery.txt`): workers 76.6 % inclusive; `ProcessIncrementWork` 29 %; `ProcessTraceWork` 19.5 %;
   `FixForwardingReaders` 11 %; `ProcessDecrements` 8 %; `NewWork` 8 %; `Resume` (with `PublishTraceWork` inlined) 5 %.

So the CPU gap (66 s CPU, 27 s wall) is mainly: 4 workers that wake each other through one mutex, and trace work that has no effect.

### 5.4 Experiments (Measured; all with natural collections; scripts `e2.sh` to `e6.sh`)

| experiment | non-moving elapsed (s) | nursery elapsed (s) | notes |
|---|---|---|---|
| default | 19.9 / 19.6 | 26.8 / 26.5 | GC 17.4 to 17.7 s / 24.3 to 24.6 s; RSS 220 MiB |
| trace off (`GCSettings.LatencyMode = Batch`), 2 runs each | 5.0 / 5.1 | 5.7 / 5.6 | GC 2.1 s / 2.75 s; RSS 97 MiB; 51 / 49 pauses |
| diagnostic variant `P24_SKIP_BLACK_TRACE=1` (trace on), 2 runs each | 5.6 / 5.5 | 8.0 / 8.0 | InitialMark 1, FinalMark 2: the cycles end; GC 2.7 s / 5.1 to 5.4 s |
| WKS (matrix) | 3.57 wall | | |

The diagnostic variant is a **copy** of the sources under `/root/p24/main`, not the repository. It differs by two small hunks (`scripts/diag-variant.patch`):
the condition `m_mark_metadata.Load(start) == 0` before `QueueTrace` in `ProcessIncrement` when `P24_SKIP_BLACK_TRACE` is set, and the worker count from
`P24_WORKERS`. Without the variables it is the p24 collector.

Number of workers (`e4.sh`, diagnostic variant; `results/cells/e4-*`):

| workers | list suffix non-moving, trace off: wall / GC (s) | requests nursery: wall / GC (s) | binary trees nursery, trace off: wall (s) |
|---|---|---|---|
| 1 | 10.48 / 5.97 | 4.33 / 1.27 | 0.59 |
| 2 | 8.09 / 3.55 | 4.14 / 1.06 | 0.56 |
| 4 (default) | 5.66 / 2.14 | 4.38 / 1.18 | 0.67 |
| 8 | 4.65 / 1.50 | 4.81 / 1.43 | 0.61 |
| 12 | 5.75 / 1.95 | 5.18 / 1.71 | 0.63 |

The CPU time of requests nursery grows from 4.5 s (1 worker) to 12.6 s (12 workers) for the same wall time. **Measured conclusion:** with big pauses more workers help up to 8, with small
pauses more workers only add futex work. This is a sign of a scheduler whose cost per packet is too high, not a sign of too few threads.

Does the work scale with allocation or with survivors? (`e6.sh`, trace off, `LXRYoungLimitMB` = young allocation between pauses):

| young limit (MiB) | pauses | GC s (non-moving) | GC per pause (ms) | GC s (nursery) |
|---|---|---|---|---|
| 4 | 194 | 4.21 | 21.7 | 5.23 |
| 8 | 97 | 2.80 | 28.9 | 3.61 |
| 16 | 52 | 2.09 | 40.2 | 2.87 |
| 32 (default, the heap is the limit) | 51 | 2.10 | 41.2 | 2.72 |

The cost of a pause grows with the survivors, which saturate at the live set (8 MiB). It does not grow with the allocation. A bigger nursery
means fewer pauses and less total time, until the live set is the limit. **Measured.**

### 5.5 What remains after the trace problem is gone (Measured)

With the trace off the cell takes 5.8 s (non-moving) and 6.5 s (nursery) against 3.57 s for WKS (1.6 and 1.8 times). The clean pause costs 40 to 51 ms for
about 300 000 increments: about 130 to 170 ns of wall time per object. With `perf` (`results/reports/perf-ls-nursery-batch-k.txt`; 8 067 samples = 16 s CPU): collector user code is 71 %,
kernel 19 % (futex wake is still 14.5 %), and the program 7 %. The hot functions are `TryCopyNursery` (16 %), `ResetRangeQuiescentCore` (8.6 %),
`DecrementWithDeathCore` (6 %), `IsNurseryFast` (6 %), `ProcessIncrement` (5.4 %), `MarkPromotedStart` (3.3 %). About 250 to 330 ns of CPU per increment
(`ProcessIncrementWork` is 23 to 31 % of 16 s, over 15.5 million increments; the report lists two symbol groups). Plus `post_drain` (section 5.6). The Java LXR pauses for this program are a median of 4.6 ms for
about 5 MiB of young data: **Inferred** at about 5 times less wall time per processed object, while it has 3.25 times more workers (13 against 4).

### 5.6 The allocating thread waits for the lazy decrements (Measured)

After each pause `CollectLocked` calls `Drain` (`PostDrain`) on the thread that triggered the pause. For list suffix with the trace off the median is 29.4 ms per pause (sum 1.5 s of 5.9 s,
`results/trace3-listsuffix-nonmoving-batch.txt`, `dec_wall` 25.6 ms). A program with one mutator thread stops for this time. In the Java LXR the lazy decrements run
on the concurrent workers while the program allocates (`global.rs: schedule_collection` only logs a warning if they are not finished).

### 5.7 What the Java LXR does with deep lists (Code)

- It does not bound the work of a pause by a time limit (`max_pause_millis` is `None`). It bounds the **young data** (the heap is small, the survival predictor
  limits at 128 MiB) and it makes the work cheap per object.
- The increments of a chain are done in one packet, one level after the other, with two plain vectors (`ProcessIncs::do_work`). A chain of 63 nodes costs no
  scheduling. Packets are only split when 1 024 slots are pending, and then they go to the shared bucket for stealing.
- A young object is copied at the first increment (`process_inc_and_evacuate`), into a per-worker to-space buffer. The copy stops when `used - reserve > total`
  (`over_space`) and then the objects stay (promoted in place, `set_as_in_place_promoted`).
- With a trace active, young objects are black and cost no trace packet.
- Stealing is lock-free (`crossbeam::deque`), the park lock is touched only by idle workers.

### 5.8 Answer to question 1

The list suffix nursery pause is long because of a trace that stays active. The copy is not the problem (the program copies nothing after pause 11). The work that grows with the backlog is 340 of the
390 ms. The backlog exists because each promoted object gets its own trace packet, and the lock-bound scheduler cannot drain it in the 30 ms between pauses. The work of a clean pause (40 to 50 ms) is
proportional to the survivors (about 300 000 objects, almost the whole live set), not to the allocation.

## 6. Question 2: requests, non-moving

### 6.1 Every Full (Measured)

gdb at the end of every lazy sweep (`scripts/gdb-policy.sh`, `scripts/policy-states.py`; output `results/policy-requests-nonmoving.json`, table `results/reports/policy-requests-nonmoving.txt`,
62 records, the first ones are from the warm-up run) and the trace `results/trace-requests-nonmoving-r1.txt` (58 pauses: 6 before the first Full, then Fulls).

| item | value |
|---|---|
| admitted pages (Immix 72 MiB + LOS 12 MiB) | 21 504 |
| Immix blocks | 2 368 (the same count in every record) |
| mature pages at the end of each pause, record 0 to 12 | 9 729, 13 553, 12 465, 15 041, 15 825, 7 641, 14 265, 17 121, 18 089, 18 577, 18 793, 18 913, 19 001 |
| mature pages from record 12 on | 19 001 to 19 009 (88.4 %); no change in 50 records |
| block states at record 12 and later | `Unallocated` 0 to 1, `Reusable` 2 367 to 2 368 |
| emergency threshold | `total < 15 % * total + mature` is true when mature > 18 279 pages |
| collection cause | `HeapFull` for every Full (the allocator found no hole) |
| free pages at the start of every Full | 0; reserve 947 pages |
| young allocation between two Fulls | 56 MB for the first Full, falling to 48 MB |
| blocks released by a Full | 0 (median); 42 in all 52 Fulls |
| objects traced by a Full | about 306 000 (the live set is about 15 MiB: P15) |
| emergency hint | true after every pause from pause 6 on (the first Full is pause 7) |

Pause 7 is the first Full: pauses 1 to 6 are RC, InitialMark and FinalMark, mature pages rise (the first record to pass 18 278 is record 9).

### 6.2 The trigger in the code

1. `LxrGcHeap::FinishLazy` calls `LxrCollectionPolicy::FinishLazySweep(pause, admittedPages, maturePages, ...)` after every lazy sweep.
   It sets `m_emergency = (totalPages < minimum + maturePages)`, with `minimum = min(totalPages * 15 / 100, 2^18)`.
2. `maturePages` is `LxrReferenceCounting::GetMaturePages`: mature LOS bytes plus **all blocks that are not free** (`GetNonFreeBlocks`) times 32 KiB.
   A block with one live object counts as a full block. This is the same definition as the Java `mature_space_pages` (pages in use after the pause minus the blocks that the lazy sweep freed).
3. `LxrCollectionPolicy::Select`: `if (emergency || user || EmergencyHint()) return marking ? FinalMark : Full`.
4. The pause itself starts because the allocator finds no hole: `LxrGcHeap::Alloc` calls `CollectForPressure(..., heapFull = true)`, cause `HeapFull`.

The rule needs `mature > 85 %` of the **whole** admitted capacity. The Immix part alone is 2 368 blocks = 18 944 pages = 88 %. So the rule becomes true when nearly all
Immix blocks hold at least one live object. In non-moving mode this is the normal state of a heap that has been full once: the young survivors stay where they were allocated,
so they are spread over all blocks (live data 15 MiB, blocks 74 MiB), and a block becomes free only when all its objects are dead.
Experiments that change only the rule or the split (`e5.sh`; `results/cells/e5-*`):

| setting | Full | wall s | GC s |
|---|---|---|---|
| default (`LXRRCStopPercent=15`, Immix/LOS 72/12) | 52 | 11.02 | 6.29 |
| `LXRRCStopPercent=12` | 50 | 10.74 | 6.04 |
| `LXRRCStopPercent=10` | **0** | **4.15** | **0.51** |
| `LXRRCStopPercent=5` | **0** | **4.34** | **0.54** |
| Immix/LOS = 76/8 | 50 | 10.59 | 6.01 |
| Immix/LOS = 72/12 (default) | 53 | 11.26 | 6.44 |
| Immix/LOS = 64/20 | **0** | **4.30** | **0.43** |

(At 15 percent the limit is 3 225 free pages and the heap has 2 495. At 12 percent the limit is 2 580, still more than 2 495, so the Full stays. At 10 percent the limit is 2 150, less than 2 495, so there is no Full.)
The nursery mode of the same program takes 4.3 s. So **a Full collection is not needed to reclaim memory in this program**: RC pauses with line reuse give the same result.
The result is not a memory result: RSS is 204 MiB in the default and 136 MiB (split 64/20) to 204 MiB in the others.

### 6.3 Is our Full the same as the Java LXR trace? (Code, Measured)

It is a stop-the-world trace of the whole heap, as in the Java LXR (`schedule_emergency_full_heap_collection`: roots, `Closure` bucket, then sweep of dead cycles).
The differences are in how it runs:

- Our trace runs on **one thread**: the packet `PrepareTrace` calls `DrainTraceInsidePause`, which processes the trace packets in a loop (`ProcessTraceWork`).
  **Measured:** in a Full `prepare_trace_wall_us` equals `prepare_trace_busy_us` (110.3 ms median). The Java LXR runs the closure on all workers (13).
- Each field read is a `lock cmpxchg` (`Interlocked::CompareExchangePointer(slot, nullptr, nullptr)` in `ScanTraceLocked`), and each object takes a spin lock
  (`EnterTraceLock`) and uses sequentially consistent loads and stores. **Code.**
- After the Full, a lazy sweep of dead cycles runs (`SweepCycles`): `mature_sweep` 29.4 ms and `post_drain` 29.7 ms per Full on the allocating thread.
- **Measured per Full (median):** stop-the-world 120.9 ms, `prepare_trace` 110.3 ms, suspend 1.2 ms, increments 3.3 ms, decrements 2.7 ms; plus 29.7 ms `post_drain`.
  52 Fulls: 6.1 s stop-the-world + 1.6 s `post_drain`.
- Java, list suffix (a different program, so only a shape): Full = 33 ms median with 13 workers; `Closure` stage 23 ms. The live data is about 6 MiB (262 144 nodes of 24 bytes, **Inferred**).
  Per MiB of live data our Full is about 1.5 times slower in wall time and uses 1 thread against 13 (**Inferred**, different programs).

### 6.4 What the Java LXR does at the same state

- **Code.** It uses the same rule. In a Full it also defragments: `select_mature_evacuation_candidates` takes **every** non-free block as a candidate in a Full, sorts by dead lines,
  and copies as much as the budget `max(total - used, 5 % headroom)` allows. The copy gets pages from the page resource (a shared pool), so it can run when no block is free.
- **Measured** (Java log): in requests it never reaches the state. After an RC pause the reserved heap is a median of 29 MiB of 52 MiB (56 %); P15 measured a peak of 70 %. 0 Full in 112 pauses.
  The reason is the young copy: survivors are packed into dense blocks and all other nursery blocks are returned with one call.
- In list suffix (heap 96 % full after an RC pause) the Java LXR does run a Full every second pause: 75 Full in 155 pauses, each 33 ms.

### 6.5 Why non-moving cannot get out, and why mature evacuation does not help (Measured)

- Non-moving: blocks are never made empty by a pause (young survivors stay), so the block count (which drives the rule) never falls. A Full does not reclaim blocks either
  (block states are the same before and after).
- `lxr-defrag` (mature evacuation on, no young copy): 16.6 s, with 52 Fulls of 222 ms (median). `results/trace-requests-defrag-r2.txt`: per Full, **about 2 360 eligible blocks, 0 selected, 0 objects copied**.
  `SelectMatureCandidates` limits the budget to `freeBytes` (the free blocks), and there is none; the code comment says that, unlike the Java page resource, only admitted blocks are usable as to-space.
So defrag cannot start in exactly the state where it is needed. The Full pause then costs more (`prepare_trace` 150 ms instead of 110 ms; the cause is **Open**, **Inferred**: remembered-set work).
- `lxr-mature`/`lxr-nursery`: the young copy works because the trigger keeps a to-space reserve (947 pages), and survivors are packed.
- `results/cells/e8-*` tried the same cells without `LXRManagedHeapHardLimitMB`. The results are the same (RSS 204 MiB): the capacity stays fixed because `LXRLegacyCapacity=1` is set.
  So I could **not** test the overshoot (to-space beyond the admitted heap, P17 S5b). **Open.**

### 6.6 Answer to question 2

The Full pauses come from the emergency rule, which counts non-empty blocks. In non-moving mode every block is non-empty after the first fill, the Immix space is bigger than 85 % of the heap, and nothing can empty a block.
The Java LXR has the same rule but avoids the state by copying young objects, and by defragmenting in the Full. Our Full is a one-thread trace, so each of the 52 costs 121 ms (plus 30 ms of lazy work).

## 7. The 1:1 mapping table (Code, Measured)

Legend: **same** = same behaviour. **different** = a deliberate or accepted change. **missing** = the Java LXR has it, we do not. **bug** = a defect that I consider a mistake.
"Consequence" = expected effect on speed; the ones with a number are Measured above.

| # | Element of the Java LXR | What we do (function) | Status | Consequence |
|---|---|---|---|---|
| 1 | Allocation: bump in a block or a hole; young object has count 0 | stock helper bumps in a buffer = one hole (`LxrGcHeap::Alloc`, `PrepareFreshBlock`, `PrepareRecycledHole`) | same idea, different cost | the refill does 5 metadata planes (RC, valid, unlog, mark, straddle) per hole: `ResetRangeQuiescentCore` 4 to 8.6 % of CPU; requests nursery: `LxrGcHeap::Alloc` is 17.8 % of samples (1.2 s of CPU), `AcquireBlock`/`IsLineDead` 9.9 % |
| 2 | Young objects: no work until the pause; first increment counts them | same (`ProcessIncrement`) | same | none |
| 3 | Lazy increments: slot buffers of 1 024 become packets during mutator time | the log streams are cut into packets in the pause (`PrepareWork`) | different | small: `prepare_work` 0.4 ms |
| 4 | Coalescing write barrier, unlog bit per field, CAS to log, old value to the decrement buffer | same semantics (`LogBeforeWrite`, P21 barrier) | same | none seen (`LxrBarrierSlowPath` 0.4 % of samples) |
| 5 | Transitive increments **in the same packet** (loop over levels; split at 1 024) | children are flushed as a new packet at the **end** of the packet (`ReferenceBatch::Flush`): one packet per level | different | deep chains: one handoff per level. Requests nursery: 27 to 36 packets and 9 ms wall for 10 500 increments (300 us per packet) |
| 6 | Nursery evacuation at the first RC pause (default on) | optional (`LXREvacuateNursery`); the cells "nonmoving" turn it off; the copy needs admitted free blocks, or a reserve (S5a) | different | requests: nonmoving 10.7 s, nursery 4.3 s. List suffix: the copy stops after pause 11 (no free page), so the nursery arm is not better than non-moving |
| 7 | Release of all nursery blocks with one call | `ReleaseNurseryBlocks` (P17 S4), same idea | same | `release_epoch` 0.2 to 1.5 ms |
| 8 | Decrement processing: recursion in the same packet; dead blocks go to a list; `SweepBlocksAfterDecs` | recursion by packet per level; `m_mature_candidates` flags; lazy `Sweep`/`RefreshBlock` | different | decrement packets are small; completion counters per child (`SubmitTrackedChild`) |
| 9 | Lazy decrements run beside the program; the next pause only logs a warning | `Drain` before the pause and `PostDrain` after it, on the allocating thread | different | list suffix (trace off): 1.5 s of 5.8 s; requests nursery: 0.38 s |
| 10 | Mature RC: counts of 2 bits (sticky at 3), per 16 bytes | counts of 8 bits (default `LXRRCBits`=8), per 8 bytes; sticky at 255 | different | RC plane is 8 times bigger per heap byte; more cache misses; fewer stuck objects (**Inferred**) |
| 11 | SATB trace on the concurrent workers; packets close over children in a loop; marked objects are skipped | `TraceObjects`/`TraceScan` packets; spin lock per object; `lock cmpxchg` per field | different | trace work per object is high (`ProcessTraceObject` 7 to 12 % of samples) |
| 12 | Black allocation; **no trace packet for a promoted object** | black allocation yes; **one trace packet per promoted object** in an RC pause | **bug** | list suffix: 22 s of 25 s gap; binary trees InitialMark 142 to 159 ms (`publish_trace` 106 ms) against 1.4 ms in the Java LXR |
| 13 | Packets that wait for a pause are moved to a lock-free injector (`pause_concurrent_marking_work_packets_during_gc`: steal and push, also proportional to the backlog but cheap) | `PublishTraceWork` walks the whole `m_work` list and `Submit`s each packet under the global mutex, with `notify_all` | different (the cost per packet is much higher) | 204 ms per pause with a backlog |
| 14 | Forwarding is resolved when a pointer is used (`ProcessDecs`: `is_forwarded`) | `FixForwardingReaders` rewrites roots, decrement logs and every `Work` object, twice per pause, on one thread | different | `prepare_trace` 79 ms per pause with a backlog (1.4 ms without) |
| 15 | Packets that are done are dropped | `RetireCompletedWork` walks the whole list at each pause | different | `suspend_prepare` 32 ms with a backlog |
| 16 | Mixed / defrag mature evacuation at InitialMark and Full, remembered sets, chunk fragmentation, budget `max(total - used, headroom)`, to-space from the page resource | implemented (`SelectMatureCandidates`, `RememberMatureSlot`, `TryCopyMature`); off in all P23 arms; the budget is limited to **free admitted blocks** | different / missing in the full-heap state | requests defrag: 2 357 eligible, 0 selected, 16.6 s |
| 17 | Block and line reuse: lines with count 0; lines in use counted as young allocation; `mature_space_pages` from blocks | same formulas (`GetYoungAllocationBytes`, `GetMaturePages`) | same | the same cliff, when blocks are not made empty |
| 18 | Heap: one budget for Immix and LOS; fixed size; to-space can exceed the budget by the headroom | static split (`LXRLegacyCapacity`: Immix = heap - heap/8, LOS = heap/8); hard limit; to-space only from admitted blocks (reserve in bytes, S5a); overshoot only without a hard limit (S5b) | different (deliberate in P17, P22) | the emergency rule depends on the split (requests: 72/12 Full, 64/20 no Full) |
| 19 | Trigger: heap full + reserve, trace drained, survival, increments, nursery, young limit | the same code (`LxrCollectionPolicy::Trigger`) plus copy reserve | same | none |
| 20 | Pause kind: FinalMark > emergency (Full) > InitialMark > RC | same (`Select`) | same | none |
| 21 | Emergency 15 %, cycle 20 %, survival 128 MiB, headroom 5 % | same constants | same | none; but the data differ (rows 6, 16, 18) |
| 22 | Workers: `ParallelGCThreads` (13); concurrent workers `ConcGCThreads` (3), a subset of them | **4 workers for everything** (constant in `LxrGcHeap::Initialize`); no separate concurrent workers | different | list suffix trace off: 1 worker 10.5 s, 4 workers 5.7 s, 8 workers 4.7 s |
| 23 | Work packets: lock-free `crossbeam` deques; `notify_one` for one packet | one `std::mutex` and one condition variable; `notify_all` at each `Submit` and each completion; local cache of 16 packets; steal under the lock | different | kernel code (mostly futex) is 17 to 37 % of all samples; CPU = 3.5 to 15 times WKS |
| 24 | Packet memory | `new Work` + `new uintptr_t[]` per packet; list `m_work` | different | `NewWork` 8 % self, `malloc`/`free` about 7 % in the list suffix profile |
| 25 | Roots: scanned by worker packets, weak references and finalizers in FinalMark/Full | the runtime callbacks run on one GC thread (`ScanOnGcThread`) and finalizers use a registry (P17 S6) | different | small: `root_capture` 0.2 ms, `scan_*` below 0.5 ms |
| 26 | Full: closure on all workers; sweep of dead cycles in the pause | closure on one thread (`DrainTraceInsidePause`); sweep after the pause (`SweepCycles`) | different | requests Full 121 ms; 110 ms on one thread |
| 27 | Mark table zeroing eagerly and concurrently (`schedule_mark_table_zeroing_tasks`) | `ResetAllDataRangesQuiescent(ObjectMark)` in `Suspend` of InitialMark and Full | different | `suspend_prepare` 34 ms in a Full (P17); small for RC |
| 28 | Prefetching in increment and decrement loops (`PREFETCH_STEP` 8) | none found in the engine (grep) | missing | **Inferred**: part of the 330 ns per increment |
| 29 | Pause budget `MAX_PAUSE_MILLIS` (stops copying) | the setting is in the list of unsupported settings (`LxrGcHeap::Initialize`) | missing | none by default |
| 30 | Concurrent trace ends: counter of trace packets is 0, checked in `collection_required` | `IsTraceExhausted` (`m_trace_pending == 0`), checked by a coordinator thread every 10 ms and in `GetCollectionCause` | same idea | with a backlog the counter does not reach 0 |
| 31 | `Block::is_nursery` is two byte loads | `IsNurseryFast` (S1, S2), 2 to 6 % of samples | same idea | small |

## 8. The other cells (D)

### 8.1 Binary trees (non-moving 2.3 x WKS, nursery 2.4 x; CPU 5.5 x)

Wall time 0.76 s against 0.34 s: a gap of 0.42 s. From section 2.2: GC +0.27 s, mutator +0.13 s, other +0.02 s.

1. **InitialMark (Measured).** `results/trace-binarytrees-nonmoving-r1.txt`: 16 RC pauses of 4.4 ms (median) and one InitialMark of **142 ms** (nursery: 159 ms), with
   `publish_trace` 106 ms: the 131 000 promoted objects of the first pause each got a trace packet, and the pause submits them all. The Java LXR has 2 InitialMark pauses of 1.4 ms in 20 pauses.
   The cycle starts for the same reason as in Java: the policy sees `garbage = mature pages - predicted live` with `predicted live = 0` (it is updated only at a FinalMark or Full), and the
   long-lived tree makes 20 % of the heap (gdb: mature pages 2 064 of 10 240 at record 14). **Measured:** with the trace off, GC is 0.11 s (0.54 s wall, 1.6 x WKS); with the diagnostic skip, 0.26 s GC and 0.65 s wall.
2. **Mutator +0.13 s (Measured and Inferred).** `perf` (`perf-bt-nursery-k.txt`): `LxrGcHeap::Alloc` is 14.5 % of samples (refill and metadata reset, `ResetRangeQuiescentCore` 3.8 %);
   the program is 7 %. RSS is 145 MiB against 54 MiB, so more first-touch page faults (`clear_page_erms` 6.7 % in the non-moving profile). The workers use 64 % of the CPU and the kernel futex
   code is 27 % (`perf-bt-nonmoving-k.txt`): this is the CPU 5.5 x.
3. **Start-up (Measured).** `scripts/startup.sh`: a tiny run takes 81 ms with WKS and 95 ms with LXR (ReadyToRun on), 291 ms and 323 ms with ReadyToRun off. At most 3 % of the gap.

### 8.2 Requests, nursery (2.5 x)

A gap of 2.6 s: GC +0.95 s, mutator +1.5 s, other +0.14 s.

1. **RC pause (Measured).** 62 RC pauses with a median of **12.9 ms** (the Java LXR: 1.58 ms for a similar young volume; ratio about 8) and 10 000 increments each. `inc_wall` 8.9 ms with 27 to 36 packets, `inc_busy` 17 ms:
   **1.7 us of CPU per increment** and 300 us of wall time per packet. This is dominated by the handoff between packets and by per-object cost, not by parallel work: 1 worker gives the same total GC time (1.27 s), 12 workers
   give more (1.71 s).
2. **Mutator +1.5 s (Measured).** `LxrGcHeap::Alloc` is 17.8 % of all samples (about 1.2 s of CPU): `AcquireBlock` 9.9 %, `ResetRangeQuiescentCore` 4.8 %, `IsLineDead` 3.9 % (`results/reports/perf-alloc.txt`).
   In requests many blocks are recycled, so each refill scans the RC table for holes. The program itself is 14 % of the samples.
3. **Trace:** 3 InitialMark and 3 FinalMark pauses of 16 to 22 ms; with the trace off the cell is 4.08 s (0.24 s better).

### 8.3 List suffix, non-moving (5.7 x; CPU 15 x; unstable)

Same mechanism as section 5. Trace off: 5.8 s (1.6 x), CPU 12.4 s (3.5 x). The instability (11 s in P20, 15 to 21 s now) matches the bistable trace state: a traced run in which the cycle ended
took 16.3 s (`results/trace-listsuffix-nonmoving-r1.txt`, InitialMark 1, FinalMark 2). **Inferred.**

### 8.4 CPU against wall time

- **Measured** (thread CPU in the summary line, `results/cells/*.txt`). List suffix nursery, default: main thread 10.1 s, 4 workers 14.3 to 15.1 s each (user 9.7 to 11.0 s, **system 4.1 to 4.6 s**); total 66.6 s for 26.8 s elapsed.
  With the trace off: workers 3.2 to 3.3 s each, total 14.3 s.
- 4 workers on 16 cores: the workers are not spinning in user code. They sleep and wake on the mutex. The two top kernel symbols (futex wake and wait) are 29.6 % of all samples (default list suffix; all kernel code is 37 %),
  17.4 % (clean list suffix), 17.0 % (requests nursery) and 27.3 % (binary trees non-moving).
- The CPU ratio to WKS (WKS has one thread): binary trees 5.5 (workers 64 % of CPU), requests non-moving 7.3 (a Full on one thread uses 1 core, so this is lower), requests nursery 4.3, list suffix 15 to 19.

### 8.5 What WKS does at the same state (Inferred from the measured shapes; I did not read the WKS source in this study)

WKS made 87, 277 and 192 gen0 GCs in the three programs (pauses of about 0.9, 0.8 and 10 ms). It has no side metadata, no per-object atomic operations in the collection,
no logging barrier, and survivors of gen0 are promoted once and not touched again. In list suffix WKS also spends 58 % of its time in GC (2.0 s of 3.5 s), so the program is hard for every collector:
Java LXR 59 %, Java Serial 60 %, WKS 58 %. The list suffix gap to WKS comes from the trace overhead (the default run is 3.5 to 4.5 times slower than the same run with the trace off), not from the idea of reference counting.

## 9. Ranked causes with the wall time that each explains (Measured)

Gap = the LXR median wall time minus the WKS median wall time. "Recovered" = how much of the gap one change removes.

| cell | gap (s) | cause 1: trace backlog (trace off / skip-black variant) | cause 2: emergency rule (`RCStopPercent=10`) | remaining |
|---|---|---|---|---|
| list suffix non-moving | 16.8 | 14.6 s (87 %) / 14.3 s (85 %) | none | 2.2 s |
| list suffix nursery | 25.2 | 22.3 s (89 %) / 19.9 s (79 %) | none | 2.9 s (trace off) |
| requests non-moving | 9.0 | 0 | **6.6 s (73 %)** | 2.4 s |
| requests nursery | 2.6 | 0.24 s (9 %) | none | 2.4 s |
| binary trees non-moving | 0.42 | 0.22 s (52 %) / 0.13 s | none | 0.2 s |
| binary trees nursery | 0.48 | 0.22 s (46 %) | none | 0.26 s |

What is left after causes 1 and 2 (about 2.2 to 2.9 s in the three large cells, 0.2 s in binary trees): allocation refill and hole search in the mutator (requests: 1.2 s CPU), the wait for lazy decrements
(`post_drain`, 1.5 s in list suffix), the per-object RC cost (330 ns of CPU per increment; 1.7 us in requests) and the scheduler cost per packet. These are causes 3, 4, 5 in section 1. I have no experiment
that separates their wall time exactly: **Open.**

## 10. Proposed fixes (not implemented)

Order = my priority. "Java LXR behaviour" = what the fix restores. "Class" = bug, deliberate choice, or missing feature.

| # | Fix | Expected effect | Risk | Java LXR behaviour | Class |
|---|---|---|---|---|---|
| F1 | In `ProcessIncrement`, queue an object for tracing only if its mark bit is 0 (roots and unmarked objects). Better: do not queue promoted objects at all, only roots and SATB entries | list suffix 28.8 s to about 8.9 s and 20.4 s to about 6.1 s (diagnostic variant, 2 runs each); binary trees InitialMark 142 ms to far less | medium: correctness of SATB with copied objects and objects that were young before InitialMark (they have mark 0, so they are still traced); needs the conformance and stress suites | no packet for a promoted object; black allocation | bug |
| F2 | Make the pause cost independent of the backlog: keep trace packets in a separate lock-free list; `PublishTraceWork` moves a list; stop rewriting queues in `FixForwardingReaders` (resolve forwarding at use, as `ProcessDecs` does); free completed packets from the workers | removes up to 315 ms per pause when a backlog exists; also bounds the bad state when F1 is not enough (trace off gives 5.7 s as the upper bound) | medium (forwarding invariants of P9, P22 decision 7) | forwarding resolved at use; injector | bug |
| F3 | Process increments and decrements of the children in the same packet (loop) and split at 1 024 slots | fewer packets for chains; requests nursery pause from 12.9 ms toward the clean scheduler cost; **not measured** | low to medium | `ProcessIncs::do_work`, `ProcessDecs::do_work` | different |
| F4 | Scheduler: per-worker deques, lock-free steal, `notify_one`, no `notify_all` at completion; worker count from the core count (the Java default is 13 + 3) | kernel futex code (17 to 37 % of CPU) goes away; list suffix trace off 5.7 s toward 4.7 s (8 workers); requests nursery stops getting worse with more workers; **not measured as a change** | medium (the scheduler has its own unit tests, `lxrworkpacketschedulertest.cpp`) | lock-free `crossbeam` | different |
| F5 | Emergency rule in non-moving mode: count live bytes or free lines (not blocks), or compare with the Immix capacity (not the whole heap), or keep the Java rule with a unified budget (no static split); fall back to Full only if an allocation fails | requests non-moving 10.7 s to 4.2 s (`RCStopPercent=10` shows the effect) | medium: heap may fill with no early Full; needs the allocation-failure fallback (it exists: `Alloc` calls `CollectForPressure(emergency)`) | the rule is the same; the data differ | different (a consequence of non-moving mode) |
| F6 | Let mature evacuation use to-space that is not an admitted free block (borrow, as the Java page resource) so that it can start in a full heap | `lxr-defrag` from 2 357 eligible, 0 selected to a real defrag; effect **not measured** | high (hard limit semantics, S5b) | to-space from the page resource | missing |
| F7 | Run the Full closure on all workers; cheaper field reads in the trace (no `lock cmpxchg` per slot) | Full 121 ms to about 40 ms (**Inferred**) | medium | `Closure` bucket | different |
| F8 | Do not make the allocating thread wait for lazy decrements after the pause; wait only when memory is needed | list suffix trace off: up to 1.5 s of 5.8 s; requests nursery 0.38 s | low to medium (finalizer and heap statistics order, P17 S4) | lazy decrements overlap the program | different |
| F9 | Cheaper refill: per-block free-line summary instead of scanning the RC table; fewer metadata planes cleared per hole | requests nursery mutator: up to 1.2 s of CPU | low | lines found from the RC table, but only the needed range | different |
| F10 | Prefetch in the increment and decrement loops; 2-bit counts as an option; eager or concurrent mark table zeroing | 5 to 15 % of the pause (**Inferred**) | low | `PREFETCH_STEP` 8, `LXRRCBits`=2 | missing / different |
| F11 | One budget for Immix and LOS (no static split) | removes the split cliff (E5) | medium | one page resource | different (deliberate in P22) |

Deliberate design choices of our port (reason in the P-documents, not re-checked here): the non-moving mode; the static Immix/LOS split under a hard limit (P22, P17); the reserve in bytes instead of the Java page resource (P17 S5a);
8-bit counts; 4 workers (the first prototype of P3.1). **Bugs** (I would not call them design): F1, F2. **Missing**: parallel Full, prefetch, `MAX_PAUSE_MILLIS`, to-space beyond admitted blocks.

## 11. Open points and limits

1. I measured the effect of F1 and of the trace-off switch only through a diagnostic variant and a latency mode. I did not run the conformance suites on the diagnostic variant. F1 is **not** proven correct. **Open.**
2. Why the trace of the first cycle starts in the warm-up and never ends in 5 of 6 runs, but ends in about one of 12: I show the bistable behaviour and the growing backlog, not the exact race. **Open.**
3. After causes 1 and 2, the 2.2 to 2.9 s that remain are split by perf shares and phase timers, not by an experiment that removes each part. **Open.**
4. The list suffix nursery arm with the diagnostic skip (8.0 s) is slower than with the trace off (5.7 s): the reason is not known. **Open.**
5. Overshoot (to-space beyond the admitted heap) could not be tested through the P23 settings (`LXRLegacyCapacity=1`). **Open.**
6. The Java numbers are from one run per program; the pause counts match P14 (113, 153, 20 pauses). I did not count the promoted objects per pause in the Java LXR, so "5 times cheaper per object" is **Inferred**.
7. Windows, Debug and Server GC details were not studied. Mature evacuation was only measured in the two arms `lxr-defrag` and `lxr-mature`, with one trace of `lxr-defrag`.
8. The `perf` runs sample at 499 Hz and take the user stacks with frame pointers; the "main thread" split in `perf-*.txt` is by thread CPU in the program summary, not by `perf --tid` (the filter did not work with this `perf`).

## 12. Files

- `scripts/`: `run1.sh` (one run), `matrix.sh`, `matrix-table.py`, `split-table.py` (tables of section 2), `trace1.sh`, `trace2.sh`, `trace3.sh`, `repeat-trace.sh`, `phases.py`, `perpause.py` (S0 phase tables),
  `perfrun.sh`, `perfrep.sh`, `perfcallers.sh`, `perfcallers2.sh`, `perfmain.sh`, `perfalloc.sh` (perf), `gdb-trace.sh` + `trace-state.py` (the trace backlog), `gdb-policy.sh` + `policy-states.py` + `policy-table.py` (policy per pause),
  `e2.sh` to `e8.sh` (experiments), `java-lxr.sh`, `e7-java.sh`, `java-summary.py` (Java LXR), `startup.sh`, `build-diag.sh` (library from the worktree), `build-diag-variant.sh` + `diag-variant.patch` (diagnostic variant),
  `build-tracer.sh` + `tracer/` (the P17 tracer with `GCBENCH_LATENCY=batch` and thread CPU), `collect.sh`, `sync.sh`.
- `results/`: raw runs (`matrix1.jsonl`, `matrix2-batch.jsonl`), traces (`trace-*.txt`, `trace2-*`, `trace3-*`, `rep-ls-nur/`), gdb output, Java logs (`java/`), `cells/` (one file per run of `e2` to `e8`), and `reports/` (tables made by the scripts).
  The `perf` data files (100 MB) are not committed; the reports made from them are.

To repeat: build with `scripts/build-diag.sh <worktree>`, then run `scripts/matrix.sh out.jsonl 3 "wks server lxr-nonmoving lxr-nursery lxr-defrag lxr-mature" "binarytrees requests listsuffix"`.
