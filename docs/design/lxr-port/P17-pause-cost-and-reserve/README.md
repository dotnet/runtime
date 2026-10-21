# P17: Pause cost, to-space reserve, block gate and inline metadata access (steps S0, S5a, S1 and S2 of P16)

Status: implemented on a local branch. Nothing is pushed. The accepted port stays at `e8d2d23`.

This page reports the first layer of the [P16](../P16-remove-per-object-bookkeeping/README.md) plan. It covers S0 (phase timers), S5a (to-space reserve in bytes) and S1 (block gate and shared counters). S2 (inline fast path for the hot side-metadata accesses) has its own section below ("S2"), and so has S3 (remove the per-object size table, live bitmap and counters; section "S3"), S4 (the pause does not touch every block; section "S4"), S5b (to-space beyond the admitted heap when no hard limit is set, with give-back; section "S5b") and S6 (the finalizer registry; no pause walks the heap; section "S6") and S7 (the pin list; the last per-object tables are gone; section "S7").

Labels: **Measured** (a run), **Code** (read in the source), **Inferred** (my conclusion; can be wrong), **Open** (not known yet). "The Java LXR" means the MMTk LXR collector used by OpenJDK.

## Short answers

1. **S0 works and costs nothing measurable.** A collection takes 130 to 1000 timestamps (largest: 2650). One timestamp costs 19 to 20 ns. This is at most 0.005% of the collection time (Measured). The matched benchmark shows no difference: geometric mean of the wall-time ratio is 0.990 (range 0.935 to 1.043, 24 cells). A first version that also timed every trace packet was too costly (up to +11% wall on `listsuffix`). I removed that timing (section S0).
2. **S5a does what the plan expected, but the cause was not only the unit mismatch.** The trigger also compared the reserve with *admitted minus used* pages. That number counts free large-object pages, which cannot hold a to-space. With the reserve compared against free Immix pages, the full collections of `requests` at a 4x heap go from 52 to 0 (Measured). With the byte-sized reserve alone, they did not go away (section S5a).
3. **S5a exposed a second problem: `ImmixAllocationManager::AcquireBlock`.** When few blocks are reusable, each refill scans every block twice. With S5a alone, the mutator became slower and the wall time got worse in 8 of 22 cells. A small change in the allocator (skip a search that already failed in this epoch) removes this. It is **part of the S5a commit**. Without it S5a is not a net gain (section S5a).
4. **One configuration got worse and must be decided by you:** `requests` at a 2x heap (42 MiB) in the moving modes. The accepted build finishes in about 48 s. The changed builds do not finish in 180 s (12 of 12 runs). The accepted build also sits in a bad state there (many tiny pauses). S5a makes it worse (section S5a, Open). **The follow-up section explains the cause: a storm (fixed) and a cliff of free lines (not fixed).**
5. **S1: the gate was never needed at a flip.** The assertion did not fire in any suite, in Debug or Release, on Linux (Measured). I removed the gate from the read-only queries. The hot counters are per worker now. Wall time against the S5a build: geometric mean 0.932 (range 0.715 to 1.036); several cells are inside the noise (section S1).

## Commits

| Step | Commit | Subject |
|---|---|---|
| S0 | `83122d3a4fd` | Add per-phase wall-clock timers to LXR collections (P17 S0) |
| S5a | `c06e085a02d` | Size the LXR to-space reserve in bytes from measured survival (P17 S5a) |
| S1 | `3bdf639d78f` | Remove the block-operation gate and shared counters from the pause loops (P17 S1) |
| Follow-up | see the handoff message | P17 S5a follow-up: reserve only the bytes a pause can copy |
| S2 | see the handoff message | P17 S2: inline fast path for the hot side-metadata accesses |
| S3 | `c48770edcb6`, `24d320076d5`, `495254ac9e0`, `9bf510f573c`, `4408ad43f32` | P17 S3a to S3f (see section "S3") |
| S4 | `d7f539de93d`, `ca896eae778`, `6bd5e3bc694`, `ba6f1f758ef`, `b8fc123586a` | P17 S4a to S4e (see section "S4") |
| S5b | `7014e537207`, `805544a4934`, `aac2e8f16fa`, `795dea990a5`, `8090810baae` | P17 S5b-a to S5b-e (see section "S5b") |
| S6 | `64baf08f849`, `834a1f743d5`, `6bfc69b4384`, `813dfc8807c`, `ca5496a4439`, `4271e3bc2ab`, `1eb1112e02d`, `9b06c3f9de1`, `9b52be8e383`, `f505dcd1450`, `c00a5e14465` and the report commit | P17 S6a to S6k (see section "S6") |
| S7 | `55713457437`, `c41f6c5c6f5` and the S7c and S7d commits | P17 S7a to S7d (see section "S7") |

Branch of S0, S5a, S1 and the follow-up: `kkokosa-lxr-pause-cost-s0-s5a-s1`, on top of `e8d2d23ef60`. I tested S0 and S5a as `1bf696c114f` and `0a57635daed`. I then only rewrote the commit messages; `git diff` between the tested and the final commits is empty (Measured).

The S2 commit is on its own branch, `kkokosa-lxr-s2-metadata-fast-path`, on top of `396e69f0685` (the tip of the branch above).

## How it was measured

- Machine: WSL2 Ubuntu 22.04, 16 logical processors (8 cores). Windows host. The accepted build is `/root/lxr-batching-d2eccaea/after-Release-linux`. Each changed build is the same layout with only `libclrlxrgc.so` replaced. I rebuilt `e8d2d23` with the same toolchain as a control: it gives the same behavior as the accepted binary (57 pauses, 51 full on `requests` at 84 MiB, both).
- Program: the unchanged P14 `gcbench.dll`. Same sizes, operations, checksums. Natural collections only. No forced `GC.Collect`. Heaps are 2x, 4x and 8x of the minimum (20/40/80 MiB for `binarytrees` and `listsuffix`, 42/84/168 MiB for `requests`). Three repetitions. Modes: non-moving, nursery, mature.
- **Deviation from P14:** the three repetitions run at the same time, each in its own set of 5 cores (`taskset`), so a matrix takes about 2 hours instead of 6. In each repetition, all builds run in a shuffled order under the same load. The absolute times are therefore not comparable with P14. Ratios between builds in one matrix are fair. Medians of three runs are noisy: per-repetition ratios of one cell vary by up to +-15%.
- Environment: `env -i PATH=/usr/bin:/bin` and only the `DOTNET_` settings of `run-matrix.py`.
- All checksums are equal in all runs of each workload (3 checksums, none differ).
- Matrices (raw runs are in `results/`): the S0 matrix with 3 builds (`matrix-s0-s5a-first.jsonl`) and the final matrix with `base`, `s5e` (S5a) and `s1d` (S1) (`matrix-final-base-s5a-s1.jsonl`, timeout 180 s).
- Windows: only builds and correctness suites were run (see Tests). **No Windows performance was measured.**

### Failed and excluded runs (final matrix: 39 of 243)

| Runs | Cause | Same for the accepted build? |
|---|---|---|
| 27: `listsuffix` 2x (20 MiB), all modes and builds, all repetitions | `Out of memory.` at start | **Yes**, 9 of 9 accepted runs fail too |
| 12: `requests` 2x (42 MiB), nursery and mature, builds S5a and S1, all repetitions | Timeout at 180 s | **No**. The accepted build ends in about 48 s |

No run was removed. The medians in the tables use the runs that finished; the `ok` column of `results/summary-*.md` shows the count. The earlier S0 matrix (first version, with trace-packet timing) had 28 failures of the same two kinds plus one out-of-memory exception in the tracer of a `listsuffix` 4x nursery run (timers off; the accepted build did not fail there). Its first, unpinned partial run was stopped and is not used.

## S0: per-phase timers

**What is added (Code).** `LxrPauseTimers` (in `lxr_reference_counting.h`) keeps wall-clock time per phase of one collection. The engine and the heap take a few timestamps at phase and packet boundaries. The values are published once per collection under a sequence number. They are read with `GC_LxrPrototype_GetMetric`:

| Metric id | Meaning |
|---|---|
| 240 | Sequence number (odd while the values are written) |
| 241 | Pause kind of the last collection (0 Full, 1 RefCount, 2 InitialMark, 3 FinalMark) |
| 242 | Timestamps taken in the last collection |
| 243 | Cost of one timestamp in ns (measured on demand, never in a pause) |
| 244 | Timers enabled (`DOTNET_LXRPhaseTimers=0` switches them off) |
| 256 + i | Phase i of the last collection, ns (or packet count) |
| 340 + i | Phase i summed over all collections |

Phases: lock wait, policy drain, drain before the pause, suspend (runtime, EE, prepare), root capture, work preparation, `ReleaseEpoch` with `RetireForwardedSources`, `ReclaimNursery`, `Sweep` and the epoch flip, every `ExecuteScan` kind with its steps (`LeaseFinalizer`, `QueueFinalizer` and `TracePendingFinalizer` walks, roots, handles, weak scans, `GetHeapStatistics`), resume, the whole stop-the-world time, the drain after the pause, and the total. Packet groups (increments, decrements, SATB, prepare trace, weak closure, epoch closure, nursery sweep, mature sweep) report wall span, busy time and packet count. `GcTrace.cs` prints all of this per collection.

**Cost (Measured).**

| Run (heap 4x) | Collections | Timestamps per collection (median / max) | Share of collection time |
|---|---|---|---|
| `binarytrees` non-moving | 18 | 141 / 610 | 0.0035% |
| `requests` non-moving | 58 | 182 / 906 | 0.0011% |
| `requests` nursery | 58 | 199 / 882 | 0.0009% |
| `listsuffix` non-moving | 66 | 979 / 2388 | 0.0052% |
| `listsuffix` nursery | 83 | 1010 / 2648 | 0.0035% |

The share is timestamps times the measured 19 to 20 ns, divided by the summed collection time. The matched benchmark (S0 build against the accepted build, 24 cells, 3 repetitions): geometric mean of the median wall-time ratio **0.990**, minimum 0.935, maximum 1.043 (`results/compare-wall-s0-vs-base.md`). This is noise around 1.

**A first version failed this test (Measured).** It timed every work packet. A trace cycle has more than 170 thousand packets in one pause on `listsuffix`. That version took 349 thousand timestamps per collection (1.7% of the collection time by calculation). In the first matrix it was slower than the accepted build on `listsuffix` 4x: ratios 1.06, 1.11 and 1.07 with timers on, against 1.02, 1.02 and 1.01 with timers off (`results/compare-wall-s0-first-version-*.md`). The cause is the shared atomics at each packet end (Inferred). The final version does not time trace packets. This is the version in the S0 commit.

**Where a pause spends its time (Measured, heap 4x, medians of the pauses of one run, ms; `results/phases-s0.md`).**

| Run and pause kind | STW pause | Root capture (of it: finalizer walk) | Increments (wall) | `ReleaseEpoch` (of it: `ReclaimNursery` / `Sweep`) | `GetHeapStatistics` | Other |
|---|---|---|---|---|---|---|
| `binarytrees`, RefCount (16) | 84 | 15.7 (15.4) | 2.6 | 62.7 (48.2 / 14.5) | 1.3 | |
| `requests` non-moving, RefCount (4) | 104 | 18.6 (18.4) | 5.7 | 72.5 (53.2 / 19.1) | 4.6 | |
| `requests` non-moving, Full (52) | 315 | 17.4 (17.2) | 5.2 | 62.0 (50.3 / 11.4) | 24.2 | prepare trace 148, suspend prepare 34 |
| `listsuffix` non-moving, RefCount (65) | 375 | 10.9 (10.7) | 100.7 | 32.1 (27.6 / 4.6) | 5.6 | suspend prepare 28; about 200 ms is in no timed phase |

- **Code and Measured:** in a young pause without a trace cycle, about 90% is `ReleaseEpoch` (`ReclaimNursery` alone is 50%), the `LeaseFinalizer` heap walk (15 to 18%) and `GetHeapStatistics` (1 to 4.4% in a young pause, 8% in a Full). The increments take 3 to 6%. This matches P15 findings 1A, 1B. The drain before the pause is 0 in all runs (the drain happens after the previous collection; "post drain" is 1.7 ms in `binarytrees`, 7 ms in `requests`, up to 50 ms in a Full).
- **Inferred, Open:** in `listsuffix` the pause has about 200 ms that no timer covers. Trace packets are not timed any more. I think the pause waits for the concurrent trace slices. This needs a check.
- In a Full pause, `suspend prepare` is 34 ms. It contains the reset of the mark bits (Code).

## S5a: to-space reserve in bytes

**This is a deliberate port-specific deviation from the Java LXR formula.** Reason: the Java LXR lets GC threads take pages beyond the heap budget during a pause. The port copies only into blocks that are already admitted, so the to-space must be free when the pause starts.

**What changed (Code).**

1. `LxrCollectionPolicy::CollectionReservedPagesForCopy`: reserve = young bytes since the last pause x measured survival ratio x 2, in bytes, rounded up to whole 4 KiB pages. (The follow-up changed "young bytes" to "clean nursery bytes": see the follow-up section.) The old function stays for the non-moving mode (no to-space needed there) and for its tests.
2. The measured ratio is the bytes of young objects that received their first increment in the pause, copied **or promoted in place**, divided by the young bytes. The Java predictor counts copied bytes only, so it falls to zero when copying fails. Per-worker counters hold the bytes (a new `WorkerCounters` array, also used by S1).
3. First sample replaces the prior. Later samples rise 3:1 and fall 1:3. Before the first pause every young byte is assumed to survive. A minimum ratio of 5% remains in quiet phases. There is no setting for it. `LXRDefragHeadroomPercent` still drives mature selection and the non-moving trigger.
4. The trigger compares the reserve with **free Immix pages** (`LxrReferenceCounting::GetImmixFreePages`), not with admitted minus used pages.
5. `ImmixAllocationManager::AcquireBlock`: a mutator search for recycled blocks that scanned every block and found none is not repeated while the block-state token is unchanged. The token (`ImmixBlockManager::GetStateToken`) counts epoch flips and never wraps.
6. New metrics: 245 measured ratio (x1e9), 246 survivor bytes of the last pause, 247 reserve pages at the last pause start, 248 free Immix pages at the last pause start.
7. Tests: new policy checks in `lxr-rc-validation.cpp` (reserve in bytes and pages, first sample, 3:1 and 1:3, minimum ratio, in-place survivors, Immix-free trigger). The `ReserveUnits` control still applies to the old function.

### What I found on the way (Measured unless marked)

**Finding 1: the free-page measure.** With the byte-sized reserve and the old comparison (admitted minus used), the full collections did not go away. `requests` 4x, nursery mode, one run each, pinned to one core (`sensitivity.sh`; the numbers are copied here, the raw output is not kept):

| Minimum ratio, factor | Comparison | Pauses | Full | GC time (s) |
|---|---|---|---|---|
| 5%, 2 (as built) | admitted minus used | 57 | 50 | 21.7 |
| 10%, 2 | admitted minus used | 64 | 47 | 23.2 |
| 13%, 2 | admitted minus used | 67 | 2 | 10.4 |
| 15%, 2 | admitted minus used | 70 | 0 | 9.2 |
| 25%, 2 | admitted minus used | 77 | 0 | 9.7 |
| 50%, 2 | admitted minus used | 103 | 0 | 11.1 |
| 100%, 2 | admitted minus used | 155 | 0 | 13.4 |
| 5%, 4 | admitted minus used | 63 | 47 | 22.6 |
| **5%, 2** | **free Immix pages** | **70** | **0** | **9.7** |
| 10%, 2 | free Immix pages | 81 | 0 | 10.1 |
| 15%, 2 | free Immix pages | 82 | 0 | 9.9 |
| 25%, 2 | free Immix pages | 94 | 0 | 10.6 |

In the first rows the trigger fired when the Immix space was already full, because about 10 MiB of unused large-object pages counted as free. The reserve must be compared with the free Immix pages. With that, the 5% minimum and factor 2 are enough, and larger values only add pauses. **Inferred:** the first big pause of `requests` (table build: 15 MiB of 69 MiB young survive, 22%) cannot be predicted from the history. It fits because the trigger now waits for real free Immix pages.

**Finding 2: the allocator scan (Measured; cause Inferred).** With S5a without the allocator change, the mutator became slower. `requests` 8x nursery: 54 s against 29 s for the accepted build, with the GC time falling from 13.5 s to 8.0 s. A `perf` profile of the final S5a source with the new skip switched off (`requests` 8x nursery, one core, 499 Hz, user samples, symbols from the unstripped module) puts 94% of the samples in the collector module. The block-search functions have these self shares: `SideMetadataManager::Load` 34.9%, `Store` 9.0%, `GetLocation` 8.1%, `CompareExchange` 7.0%, `AcquireBlock` 5.8%, `ImmixBlockManager::TryAcquire` 5.8%, `GetState` 3.4%; together 74% (`results/perf-s5a-without-allocator-skip-requests-x8.txt`). I did not take a profile of the accepted build, so I cannot say how much of this is new (Open). `AcquireBlock` scans all blocks for a recycled block, and then all blocks for a fresh one (Code). With reusable blocks rare, the first scan fails at every refill (Inferred). Result of the change on `requests` 4x nursery, one run each, 5 cores: accepted 34.0 s, S5a without the change 39.5 s, S5a with the change 17.6 s.

**Finding 3: a defect in my first version of that change (Measured).** I first stored the epoch value as the "search failed" mark. The epoch counter wraps (254 to 1). Three native selectors failed (`--mature-remset-wrap`, `--mature-remset-same-shape`, `--mature-remset-boundaries`: "each counter-wrap step is real same-line address reuse"). A stale mark would also skip searches in a real run after 127 collections. The flip counter in the token does not wrap. All 25 native selectors pass with it.

### Results of S5a (final matrix, medians of 3, wall time in ms; ratio to the accepted build)

All cells (`results/compare-wall-s5a-vs-base.md`; `ratio` below 1 is faster):

| Workload | Heap | Mode | Accepted | S5a | Ratio |
|---|---|---|---|---|---|
| binarytrees | 2x | nonmoving | 6199 | 6227 | 1.005 |
| binarytrees | 2x | nursery | 7813 | 8593 | 1.100 |
| binarytrees | 2x | mature | 7697 | 10291 | **1.337** |
| binarytrees | 4x | nonmoving / nursery / mature | 5418 / 5703 / 6037 | 4727 / 4959 / 4806 | 0.873 / 0.869 / 0.796 |
| binarytrees | 8x | nonmoving / nursery / mature | 5430 / 5809 / 6000 | 3532 / 4038 / 4041 | 0.650 / 0.695 / 0.673 |
| listsuffix | 4x | nonmoving / nursery / mature | 22477 / 48884 / 49832 | 21197 / 39638 / 42150 | 0.943 / 0.811 / 0.846 |
| listsuffix | 8x | nonmoving / nursery / mature | 22907 / 30671 / 31671 | 23294 / 38227 / 38509 | 1.017 / **1.246** / **1.216** |
| requests | 2x | nonmoving | 40670 | 41571 | 1.022 |
| requests | 2x | nursery / mature | about 48000 | **timeout (6 of 6)** | |
| requests | 4x | nonmoving / nursery / mature | 31671 / 35261 / 39808 | 28551 / 18209 / 17987 | 0.901 / 0.516 / 0.452 |
| requests | 8x | nonmoving / nursery / mature | 25786 / 28520 / 31941 | 19492 / 16281 / 15387 | 0.756 / 0.571 / 0.482 |

Geometric mean over the 22 finished cells: **0.818**. Part of the gain in the non-moving cells comes from the allocator change, not from the reserve (the reserve is not used in that mode; Inferred).

Pause and memory numbers at a 4x heap, one traced run per cell (`results/pause-tables-x4.md`), moving modes:

| Workload, mode | Build | Collections (full) | Pause time per kind: median (max), ms | Total pause ms | RSS MiB |
|---|---|---|---|---|---|
| `requests` nursery | accepted | 58 (52) | Full 399 (546); RefCount 137 | 22182 | 378 |
| `requests` nursery | S5a | 69 (0) | RefCount 122 (268); InitialMark 198; FinalMark 161 | 9083 | 368 |
| `requests` nursery | S1 | 70 (0) | RefCount 120 (202); InitialMark 181; FinalMark 146 | 8826 | 368 |
| `listsuffix` nursery | accepted | 64 (0) | RefCount 571 (1074) | 36213 | 315 |
| `listsuffix` nursery | S5a | 252 (0) | RefCount 85 (338) | 24826 | 271 |
| `listsuffix` nursery | S1 | 239 (0) | RefCount 60 (288) | 18823 | 253 |
| `binarytrees` nursery | accepted | 17 (0) | RefCount 111 (166) | 2344 | 235 |
| `binarytrees` nursery | S5a | 19 (0) | RefCount 99 (198) | 2182 | 204 |

**Cost of the reserve (Measured, `requests` 4x nursery, S5a traced run).** At the start of a pause the free Immix space is 3.9 MiB (median) and the reserve is 3.9 MiB (median; largest 12.5 MiB) of 74 MiB of Immix space (5%). Median young bytes per pause are 38.6 MiB, median survivors 0.5 MiB (largest 8.8 MiB). Copying worked in 66 of 69 pauses (3 copy failures; 47 MiB copied in total). The number of pauses grows from 58 to 69 and, for `listsuffix` 4x nursery, from 64 to about 250 (the young pauses there are small: median 3.2 MiB young, 85% survive). RSS did not grow: it fell by 3 to 14% in the moving cells above.

### Where S5a is worse, and what is open

- `requests` 2x, nursery and mature: 12 of 12 runs time out (180 s; earlier at 400 s with the first S5a build). The accepted build finishes in 48 s with 535 pauses. Both builds reach the same state: the live set (16 MiB) is spread so that every Immix block holds some live data (mature pages 8769 of about 8700), and the young bytes per pause fall. In the accepted build they level off near 3.5 MiB (pause 450). In the S5a build they fall to 1.4 MiB (pause 450) and 0.2 MiB (pause 1300). **Inferred:** nursery copying in the first 90 pauses spreads the survivors more than the in-place promotion of the accepted build. I tried copying only into fresh blocks and a rule that stops enforcing the reserve when the previous collection left less than 1/8 of the Immix space free. Neither helped, so neither is in the commit. **Open.**
- `binarytrees` 2x mature 1.34x, nursery 1.10x; `listsuffix` 8x nursery 1.25x and mature 1.22x. Not investigated (Open). The 8x cells have 15 to 30 collections, and the change in the number of pauses may be the cause (Inferred).
- The machine runs the three repetitions in parallel. A cell with a ratio between 0.9 and 1.1 should be read as "no difference".
- **This section is the state at the first handoff. The follow-up section below replaces its two open items.**

## Follow-up: root cause of the regressions after S5a

Commit: `P17 S5a follow-up: reserve only the bytes a pause can copy` (hash in the handoff message). The three first commits are unchanged.

### Short answers

| Cell | Result of the follow-up |
|---|---|
| `binarytrees` 2x, `listsuffix` 8x | **No regression any more** (matrix: 0.89 to 0.91 and 0.85 to 0.92 of the accepted time). Cause: the collection storm of the reserve (Finding A). In the first final matrix the `binarytrees` 2x cell also had noise: S5a and S1 are almost the same code, but `mature` gave 10291 ms and 7650 ms. |
| `requests` 2x nursery and mature | **Still does not finish in 180 s (6 of 6 runs; mature now stops with `Out of memory.`, nursery times out).** I could not fix it. The cause is not the reserve. It is the erosion of free lines, which the accepted build also has: it finishes the 600000 requests just before the cliff (Finding B). This needs a design decision. |
| All other cells | Faster than the accepted build, 0.44 to 1.07 (geometric mean 0.741 over 22 cells). |

### Finding A: collection storm (Measured; the first hypothesis was right)

`requests` 2x nursery, S1 build, one core, a debug print per pause (not in the commit; see `diag-and-experiments.patch`). After pause 1200 the Immix space has **no free block**. The reserve is at least one page whenever one young byte exists, so `reserve >= free` is true and the trigger fires at once. Each pause then takes 23 ms for about nothing.

| Pauses | Pauses per second | Young MiB per pause | Free Immix pages at start | Reserve pages | Pause ms |
|---|---|---|---|---|---|
| 1 to 100 | 10 | 10.4 | 272 | 272 | 53 |
| 301 to 400 | 17 | 1.8 | 44 | 48 | 43 |
| 801 to 900 | 28 | 0.28 | 8 | 8 | 31 |
| 1301 to 1400 | 42 | 0.0 | 0 | 2 | 23 |

**Why the reserve was wrong (Code, Measured).** Young bytes in recycled blocks (holes of old blocks) are never copied. `TryCopyNursery` copies only objects in a nursery block, and only fresh blocks are nursery blocks. In the stuck state `clean nursery bytes` is 0 and all young bytes are in recycled blocks. The reserve counted them (`GetYoungAllocationBytes`). The Java formula counts only the clean nursery bytes. The measured survival ratio had the same fault: it counted in-place survivors of recycled blocks against all young bytes.

**Fix.**

1. `CollectionReservedPagesForCopy` gets the clean nursery bytes (`GetCleanNurseryBlocks` times the block size). With no clean block the reserve is 0.
2. `LxrCollectionPolicy::BeginPause(allocated, evacuable)`: the measured ratio is survivors divided by the clean nursery bytes of the pause. A pause with no clean block gives no sample.
3. The engine counts a survivor only if it was copied or lies in a nursery block (`ProcessIncrement`).
4. The policy test has three new checks and the engine tests two (see Tests).

Result: after the first ~100 pauses the reserve is 0 and the pauses are triggered by allocation failure only, as in the accepted build (cause `HeapFull`, 3300 of 3303 pauses).

Single runs, 5 cores, S1 build against the follow-up build (the follow-up build here still has the allocator skip; ms):

| Cell | Accepted | S1 | Follow-up |
|---|---|---|---|
| `binarytrees` 2x, nursery+mature | 7254 | 7719 | 5622 |
| `listsuffix` 8x, nursery | 28877 | 29322 | 21994 |

### Finding B: why `requests` 2x still does not finish (Measured and Inferred)

I ran the follow-up with one switch at a time (debug build, `requests` 2x nursery, 5 cores; the accepted build takes 43.8 s and 545 pauses). These debug builds have the allocator skip switched off (`if (false && ...)` in `diag-and-experiments.patch`). The build with the skip on collapsed in the same way (timeout at 150 s), so the skip is not the cause:

| Variant | Result |
|---|---|
| Reserve switched off | 43.6 s, 549 pauses (same as accepted) |
| Reserve on, copying switched off | 44.0 s, 547 pauses |
| Copying only in pauses 1 to 6 | 48.8 s, 566 pauses |
| Copying only from pause 26 (no clean block exists then) | 47.3 s, 544 pauses |
| **Copying in pauses 1 to 25** | **timeout (903 pauses at 70 s)** |
| Copying without recycled destination lines | timeout |
| Prior survival 0.05 instead of 1.0 | timeout |
| Safety factor 1 instead of 2 | 72.7 s, 1074 pauses |
| Default (follow-up build) | timeout at 150 s |
| One Full pause at pause 300 | free lines do not recover (17694 to 17711 lines without copying; 10049 to 9972 with copying) |

So the reserve and the trigger are not the cause. **Copying in pauses 7 to 25 is.** These pauses move only about 300 KB each, but later the free lines in the recycled blocks shrink faster. The heap is the same size and the live data is the same (16 MiB).

The state at the cliff (`available lines` means free lines in reusable blocks; one line is 256 bytes; the heap has 139264 lines):

| Cumulative young allocation | Accepted-like (copying off): available lines | Copying on: available lines |
|---|---|---|
| 1000 MiB | 40090 | 26555 |
| 1500 MiB | 28884 | 21694 |
| 2000 MiB | 22153 | 11097 |
| 3000 MiB | 14295 | 2145 |

Each pause allocates only `available lines x 256` bytes, so the young size per pause falls with it. When it is below 1 MiB a pause (25 to 50 ms) is spent for each MiB. The accepted build ends at 3376 MiB with 13k lines left and 3.2 MiB per pause. With the copying on, the same state is reached at about 2500 MiB, before the end of the program.

**The accepted build has the same cliff, only later (Measured):**

| Requests | Accepted | Follow-up |
|---|---|---|
| 400000 | 23.4 s | 33.8 s |
| 500000 | not run | 61.0 s |
| 600000 | 43.8 s (matrix: 45.7 s) | timeout (180 s) |
| 800000 | 72.1 s | not run |
| 1500000 | timeout (400 s) | not run |

**Inferred cause.** The live data is 47% of the Immix space, in small clusters (24 objects of 30 to 150 bytes). A free hole next to a live line loses one more line, because the hole finder skips the first dead line after a live line (`TryFindNextHole`). Copying packs the survivors of the table growth densely. When an entry of a dense block is replaced, its neighbors are live, so the freed hole is smaller. A scattered entry has dead neighbors, so its hole merges with them. Per replaced entry, the copying layout loses about 0.66 line and the scattered layout about 0.42 line (computed from the table above). I did not prove this. The Full pause test shows that the lines are really fragmented (no stale mark).

**Not possible quickly:**

- Removing the skip of the first dead line crashes the process in the first pauses (segmentation fault and abort, Measured). Straddle marks do not cover every live object. Do not remove it without a proof.
- `LXRDefragHeadroomPercent=20` or `35` does not help in mature mode (`Out of memory.`).

**Design decision needed (Open).** Options:

1. Accept this cell as a cliff case. The accepted build fails the same way with 1.5 million requests, and the 4x and 8x cells gain 0.44 to 0.55.
2. Evacuate the nursery only while it cannot hurt. I found no rule that is not a tuned constant: the damage comes in pauses 7 to 25, when the live set is still small and the free space is large.
3. Fix the line-fragmentation model: exact line marks for the hole finder, or a defragmenting mature collection with its own headroom (P16 S5b and later). This is the real fix.

### New and unchanged failures (Measured, matrix m6, 162 runs)

| Runs | Cause |
|---|---|
| 18: `listsuffix` 2x, all modes, both builds | `Out of memory.` at start, as before (also the accepted build) |
| 3: `requests` 2x nursery, follow-up | Timeout, 180 s |
| 3: `requests` 2x mature, follow-up | `Out of memory.` (exit -6). The S1 build timed out in this cell. It is the same collapse: allocation fails after the emergency collection. |
| 1: `listsuffix` 4x mature, follow-up, repetition 1 | `Fatal error. E_FAIL` in `WaitSubsystem.NewHandle` at the start of a thread, after 57 s. This is an allocation failure at the hard limit. The accepted build had 0 of 9 such runs in this matrix; the S0 build had one such crash in its first matrix. **Inferred:** intermittent and old. |

No run was removed. All checksums are equal (three values).

### Matrix m6: follow-up against the accepted build (3 repetitions, medians, ms)

Three pinned parallel streams as before. `fa` is the follow-up. `compare` ratios below 1 are faster. (`results/summary-m6.md`, `results/compare-m6.md`.)

| Workload | Heap | Mode | Accepted | Follow-up | Ratio | Pauses (full) accepted | Pauses (full) follow-up | RSS MiB accepted | RSS MiB follow-up |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2x | nonmoving | 5502 | 4817 | 0.875 | 67 (0) | 68 (0) | 146 | 147 |
| binarytrees | 2x | nursery | 7018 | 6408 | 0.913 | 68 (0) | 79 (0) | 146 | 154 |
| binarytrees | 2x | mature | 6991 | 6249 | 0.894 | 68 (0) | 75 (0) | 147 | 154 |
| binarytrees | 4x | nursery | 5286 | 4360 | 0.825 | 17 (0) | 20 (0) | 229 | 221 |
| binarytrees | 4x | mature | 5594 | 4214 | 0.753 | 18 (0) | 19 (0) | 229 | 226 |
| binarytrees | 8x | nursery | 5535 | 3674 | 0.664 | 7 (0) | 8 (0) | 314 | 259 |
| binarytrees | 8x | mature | 5497 | 3681 | 0.670 | 7 (0) | 8 (0) | 314 | 259 |
| requests | 2x | nonmoving | 37954 | 38085 | 1.003 | 535 (0) | 531 (0) | 184 | 184 |
| requests | 2x | nursery | 45668 | no finish | | 535 (0) | | 184 | |
| requests | 2x | mature | 45649 | no finish | | 530 (0) | | 184 | |
| requests | 4x | nonmoving | 28970 | 27237 | 0.940 | 57 (51) | 57 (51) | 368 | 370 |
| requests | 4x | nursery | 33161 | 16709 | 0.504 | 57 (51) | 68 (0) | 369 | 359 |
| requests | 4x | mature | 38357 | 16766 | 0.437 | 57 (51) | 66 (0) | 368 | 382 |
| requests | 8x | nursery | 27370 | 15005 | 0.548 | 22 (16) | 25 (0) | 658 | 657 |
| requests | 8x | mature | 30746 | 14296 | 0.465 | 22 (16) | 26 (0) | 658 | 654 |
| listsuffix | 4x | nonmoving | 21840 | 17232 | 0.789 | 54 (0) | 53 (0) | 302 | 302 |
| listsuffix | 4x | nursery | 47588 | 29618 | 0.622 | 63 (0) | 58 (0) | 305 | 283 |
| listsuffix | 4x | mature | 48781 | 52207 (2 of 3 runs) | 1.070 | 62 (0) | 80 (0) | 305 | 304 |
| listsuffix | 8x | nursery | 28884 | 24911 | 0.862 | 15 (10) | 18 (0) | 528 | 519 |
| listsuffix | 8x | mature | 29586 | 25288 | 0.855 | 15 (10) | 21 (0) | 528 | 516 |

(The nonmoving cells of binarytrees 4x and 8x, listsuffix 8x nonmoving and requests 8x nonmoving are in `results/summary-m6.md`: ratios 0.84, 0.63, 0.92 and 0.70; the follow-up does not touch the non-moving mode, so those differences come from S1 and the allocator skip.) **`listsuffix` 4x mature is the only cell above 1.00 (1.07), with one of three follow-up runs lost to the crash above; per-repetition ratios 1.13 and 0.99. I read it as noise, not as proof.**

Pause times per kind (one traced run each, heap 4x, not pinned, so not comparable with the matrix times; ms, median):

| Workload and mode | Accepted | S1 | Follow-up |
|---|---|---|---|
| `requests` 84 MiB nursery | Full 399 (n 52), RefCount 137 (n 3) | RefCount 120 (n 64) | RefCount 119 (n 63), total 7.7 s against 21.1 s |
| `listsuffix` 40 MiB nursery | RefCount 571 (n 62) | RefCount 60 (n 234) | RefCount 525 (n 70) |
| `binarytrees` 40 MiB nursery | RefCount 111 (n 15) | RefCount 100 (n 18) | RefCount 96 (n 18) |

**The small RefCount pauses of `listsuffix` in the S1 and S5a builds (60 to 85 ms, 234 to 250 pauses) were the storm. They are gone.** The follow-up has the pause shape of the accepted build there. The wall-time gain of `listsuffix` 4x nursery (0.62) comes from elsewhere (the matrix shows 20.7 s of collection time against 38.3 s); I did not trace it (Open).

Reserve cost with the follow-up (`requests` 84 MiB nursery): median reserve 3.3 MiB (largest 11.6 MiB), median free Immix space 3.4 MiB at the trigger, 4 copy failures in 69 pauses, 47 MiB copied. `listsuffix` 40 MiB: median reserve 0, because there is no clean nursery block in most pauses.

### Real fix of the allocator scan (P16 S4 follow-up)

The skip of a failed recycled-block search is a stopgap (see Finding 2). The real fix is a counter of reusable blocks per epoch, so that `AcquireBlock` never scans all blocks to learn that none exists. It belongs to P16 S4. The S5a commit keeps the stopgap (decision of the coordinator). **S4 result (Code):** the stopgap stays, but its token now also changes when a block is made Reusable, because blocks become Reusable in the lazy sweep after the pause. I did not build a counter or a queue. The reason is in section "S4", part "The stopgap for recycled-block searches".

## S1: block gate and shared counters

### (a) The epoch gate

**Code.** `ImmixBlockManager::BlockOperationScope` adds and subtracts one shared counter (`m_active_operations`, sequentially consistent) around every block operation. The epoch flips (`StartGcPause`, `ReleaseGcPause`) wait until it is zero.

**Assertion first (Measured).** I added two process-wide counts: operations in flight when a flip starts (`GetFlipOverlaps`), and operations that start during a flip and must wait (`GetFlipBlockedOperations`). Debug builds assert on both (`_ASSERTE`). The counts are metrics 249 and 250 and are printed by the native validation program.

| Where | Result |
|---|---|
| Native validation, 25 selectors, Linux Debug and Release (Windows: see Tests) | `flip_overlaps=0 flip_blocked=0` in every selector |
| Managed scope-5 fixture, all 84 cells (3 modes, native Debug and Release, Debug asserts armed) | pass, no assertion |
| Matrix runs with the Release counters (traced runs of `binarytrees`, `requests`, `listsuffix`, 4x, two modes each) | 0 and 0 |
| Positive control (`P2.2` validation, new `TestFlipOverlapDetection`): a thread runs block operations while the main thread flips 20000 times | the counts rise above 0 (the control detects an overlap) |

**Result: the assertion never fired.** Case 1 of the assignment. **I did not find a worker that overlaps a flip.** This is evidence from tests and runs, not a proof. The runs include concurrent marking cycles (`listsuffix`, `binarytrees`, `requests`).

**Change.** `IsNursery`, `IsReusing` and `IsGcReusing` take no gate now. `GetState` and `IsNurseryOrReusing` never took one. Mutating operations (acquire, return, release, sweep, mark, reusable, promote) keep the gate. Debug builds count the readers with a `ReaderScope` and assert at each flip that none is in flight (Code), so the Debug assertion still covers the read-only queries. Release builds have no counter for readers.

### (b) Per-worker counters

**Code.** The eight hot counters (`IncrementEntries`, `DecrementEntries`, `Promotions`, `ObjectScans`, `ObjectSizeQueries`, `Deaths`, `RootIncrements`, `RootDecrements`) are kept per worker (one array of 24 counters per worker, aligned to a cache line, written with plain relaxed stores by that worker). `GetMetrics` sums them. A shared last slot with atomic additions serves callers that are not workers (the observer drain in the tests). The worker index is passed down (`ProcessIncrement`, `ScanIncrements`, `ProcessDecrements`, the `Death` context, `GetObjectSize`). `m_live_bytes`, `m_reclaimed_bytes` and the extent table are **not** touched.

**The numbers in the existing tests are identical (Measured):** all native selectors that check metric values pass (Linux and Windows, Debug and Release), and the 84 managed cells pass.

### Results of S1 (final matrix; wall time against the S5a build and the accepted build)

(`results/compare-wall-s1-vs-s5a.md`, `results/compare-wall-s1-vs-base.md`)

| Comparison | Cells | Geometric mean | Range |
|---|---|---|---|
| S1 / S5a | 22 | **0.932** | 0.715 to 1.036 |
| S1 / accepted | 22 | **0.762** | 0.433 to 1.109 |

Largest gains against S5a: `listsuffix` 4x mature 0.715, nursery 0.865, nonmoving 0.880; `listsuffix` 8x 0.86 to 0.93; `requests` nursery 0.90 to 0.94. Cells with ratios between 0.95 and 1.05 are noise. **I did not separate (a) from (b)**: the matrix measures both together (Open). Time of `listsuffix` nursery 4x pauses (median RefCount pause) in the traced run: accepted 571 ms, S5a 85 ms, S1 60 ms.

## S2: inline fast path for the hot side-metadata accesses

Status: implemented on the local branch `kkokosa-lxr-s2-metadata-fast-path`, on top of the layer-1 tip `396e69f0685`. Nothing is pushed. The accepted port stays at `e8d2d23`. S3 to S7 are not started.

### Short answers (S2)

1. **What was done (Code).** The hot metadata kinds have an inline accessor (`SideMetadataAccessor`, in `side_metadata.h`). The accessor keeps the base address, the two shifts and the value mask of one kind. `SideMetadataManager::GetAccessor` computes them once at initialization, and the owner of a hot loop keeps a copy. One access is two shifts, one add and one atomic word access. The checked `SideMetadataManager` functions are unchanged. They stay for the slow path, for the tests and for callers with untrusted addresses.
2. **The share of the checked functions fell from about a quarter of the CPU samples to one or two percent (Measured).** `Load`, `Store` and `GetLocation` together were 25.7%, 22.5% and 24.2% of the samples of `binarytrees`, `requests` and `listsuffix` (4x heap, nursery mode, one `perf` recording each). After S2 they are 1.8%, 1.5% and 0.8%. Mature mode: 29.2%, 22.2%, 23.8% before; 1.6%, 2.0%, 0.9% after.
3. **Wall time (Measured).** Against the layer-1 tip, the geometric mean of the wall-time ratio is **0.852** over 22 cells (range 0.743 to 0.963). Against the accepted build it is 0.642 (range 0.376 to 0.857). In 17 of 22 cells all three paired repetitions are below 0.95. The other 5 cells are inside the noise of this host (section "Wall time").
4. **Pause times fall too (Measured).** The median RefCount pause of `listsuffix` (4x heap, non-moving) is 323 ms in the accepted build, 104 ms in the layer-1 tip and 65 ms after S2. For `binarytrees` it is 83, 83 and 57 ms. In the nursery-copy mode the gain is smaller (section "Pause times").
5. **The failed runs of the matrix are the same cells as in layer 1 (Measured).** 40 of 243 runs failed. They are: `listsuffix` 2x, all builds, all modes (27 runs, as in layer 1); `requests` 2x nursery and mature, layer-1 tip and S2 (12 runs, the known cliff-edge case); and one run of the **accepted** build (`listsuffix` 4x mature, `Out of memory.`). All checksums are equal. All suites pass (section "Tests (S2)"). Note: at `requests` 2x, 9 of the 12 runs end with `Out of memory.` (exit -6) and 3 with a timeout at 180 s. In layer 1 they ended with a timeout. Both builds fail in the same way, so I do not count this as new. **One failure needs your attention: see "Intermittent failure of a traced run" below.**
6. **No memory order was changed (Code).** Each inline access copies the order of the checked call that it replaces. The section "Memory orders" lists three proposals. None is applied.
7. **Debug builds check every inline access (Measured).** The accessor asserts that the address is owned and that the location (word, mask, shift, bit count) is the same as the checked location. Loads also compare the value. A deliberate error in the location formula made the assertion fail at once (positive control).

### What was changed (Code)

| Piece | File | What it does |
|---|---|---|
| `SideMetadataAccessor` | `side_metadata.h` | One object per metadata kind. Inline `Load`, `Store`, `CompareExchange`, `FetchAddWrapping`, `IncrementSaturating`, `DecrementNonZeroNonSaturated`, `IsRangeZero`, `Locate`. |
| `SideMetadataManager::GetAccessor(kind)` | `side_metadata.cpp` | Computes the accessor from the layout. It is invalid when the kind is not enabled. |
| `ReferenceCountTable::LoadFast`, `IncrementFast`, `DecrementWithDeathFast`, `MarkStraddleLinesFast`, `ClearStraddleLinesFast` | `reference_count.h`, `.cpp` | Fast forms of the checked table functions. `SetValidObject` and the straddle loops use the accessors. |
| `ImmixBlockManager::IsNurseryFast`, `GetStateFast` | `immix_block.h` | Fast forms of the block queries. The block-byte helpers (`LoadByte`, `StoreByte`, `CompareExchangeByte`, `LoadOwner`, `StoreOwner`) still check the block address and use the accessors after the check. |
| Accessor members | `lxr_reference_counting.h`, `immix_allocation.h`, `immix_block.h`, `reference_count.h` | The owners of the hot loops keep a copy of the accessors that they use. |
| `--inline-metadata` selector | `docs/design/lxr-port/P4/lxr-rc-validation.cpp` | Test: the accessor and the checked functions give the same results for every kind, every operation and both orders (465 checks). |

**The location formula (Code).** The layout is chosen at run time (the width of the reference count is a setting), so the numbers are not compile-time constants. `GetAccessor` stores them once.

- Bit position = `(address >> log2 bytes per entry) << log2 bits per entry`.
- Word address = base + `(bit position >> 6) * 8`.
- Shift = `bit position & 63`. Mask = value mask `<< shift`. Value = `(word >> shift) & value mask`.
- The base address of every kind is aligned to a word (`AddSpec` aligns it), so this is the same location as the one that `GetLocationUnchecked` computes. On a big-endian target, `Locate` uses `ComputeWordShiftForByteOrder`. That path is not tested (Open).

### Invariant that makes the missing checks safe

**Code.** In Release builds the inline accessors do not check the address. A caller must pass an address of an object, a line or a block that the engine has already admitted, with the alignment that the object model guarantees.

Why this holds in the engine (Code):

1. `Initialize` and `GrowSuspended` call `CommitDataRange` first. Only after that they publish the new capacity (`m_immix_capacity.store(..., release)`, `m_large.GrowQuiescent`, `m_allocation.GrowQuiescent`). So an address that passes `IsImmix` or `IsLarge` has committed metadata for every kind.
2. Every call site below either checks `IsImmix`, `IsLarge` or `OwnsAddress` before it uses the address, or takes the address from a table that holds only objects of the engine (the live-object bitmap, the log streams, the allocator).
3. For the slots of the log streams, the checked `CompareExchange` in `LogBeforeWrite` has already shown that the slot is owned.

### Audit of the call sites

**Converted to the fast path:**

| Function (file) | Accesses | Why the address is admitted |
|---|---|---|
| `TryCopyNursery` (`lxr_reference_counting.cpp`) | `LoadFast` x2, `IncrementFast` x3, `IsNurseryFast`, `MarkStraddleLinesFast` | `ProcessIncrement` calls it only after `IsImmix(start)`. The copy start comes from the copy allocators. |
| `ProcessIncrement` | `IncrementFast`, `IsNurseryFast`, `MarkStraddleLinesFast` | `!IsImmix && !IsLarge` returns early. The block and straddle calls are in the `IsImmix` branch. |
| `ProcessIncrementWork` | `FieldUnlogged` store | Slots of kind `Slots` come from log streams. `LogBeforeWrite` has checked each slot. |
| `ProcessDecrements` | RC load, `DecrementWithDeathFast` | `OwnsAddress(Start(object))` is checked before. |
| `Death` | `ClearStraddleLinesFast` | Called by `DecrementWithDeathFast` for an object that passed `OwnsAddress`. |
| `ReclaimNursery`, `ReclaimLargeNursery` | `LoadFast` | The slot comes from the live-object bitmap. |
| `ProcessTraceObject` | RC load, mark load, mark store x2 | `OwnsAddress` and the alignment check come first. |
| `FinishDyingTraceObject`, `IsMarked`, `SweepCycles` | mark and RC loads and stores, `ClearStraddleLinesFast` | Object of a dying decrement, or an object with an extent. `IsMarked` checks `GetObjectExtent != 0`, which includes `OwnsAddress` and the alignment. |
| `CompleteAllocation` through `RegisterObjectInClearedSpan` | `ValidObject` store (`SetValidObject`) | `IsImmix(start)` is checked before. |
| `RetireForwardedSources` | `LoadFast`, `ClearStraddleLinesFast`, RC store, `ValidObject` store | The slot comes from the live-object bitmap. |
| `IsMatureCandidate`, `RememberMatureSlot`, `ReplayMatureRememberedSlots`, `TryCopyMature`, `SelectMatureCandidates` | defrag, reuse, mark and RC loads; mark store; `GetStateFast`, `IsNurseryFast` | `IsImmix` or `OwnsAddress` is checked first. The remembered slots passed `OwnsAddress` when they were added. The owner comes from `FindContainingObject`. |
| `FindContainingObject` | `ValidObject` loads inside a block | The address passed `IsImmix`, and the large branch returned before. The scan stays in the block of the address. |
| `ImmixAllocationManager::IsLineDead` | `IsRangeZero` of one line | `TryFindNextHole` checks that the block is aligned and in the arena. The arena is an owned range. |
| `ImmixAllocationManager::AcquireBlock` | `GetStateFast`, `IsNurseryFast` | The candidate index is taken modulo the block count of the arena. |
| `ImmixBlockManager::TryReleaseIfRcDead` | `IsRangeZero` of a block | `TryLock` has validated the block. |
| `ImmixBlockManager` byte helpers | accessor after `ValidateBlock` | The helpers still call `ValidateBlock` first, so every block operation returns the same error codes as before. |

**Left on the checked path, with the reason:**

| Function | Reason |
|---|---|
| `LogBeforeWrite` (`CompareExchange` of `FieldUnlogged`, RC load of the old target) | The slot comes from the write barrier of the runtime. It is the first check of a slot, and a wrong slot must give an error. The profile does not show it. |
| `AddRoot`, `LoadCount`, `CompleteAllocation` (large branch), `ArmLifetimeProbe` | Addresses from the runtime or from tests. A wrong address must give an error. |
| `InitializeObjectQuiescent`, `CopyObjectQuiescent`, `ResetObjectQuiescent`, `ResetRangeQuiescent`, `ResetBlockQuiescent` | Validating or range functions. They are 1% to 3% of the samples. `InitializeObjectQuiescent` also reads `ValidObject` with a sequentially consistent load. I did not remove that fence on the hot path. |
| `SideMetadataManager::VisitWords`, `IsRangeZero`, `ResetRange*`, `CopyRange*` | Range functions with a visitor or with validation. See "What is left in the profile". |
| Large object space (`large_object_space.cpp`) | Not hot in the profiles. |

### Memory orders

**Code.** Every inline access uses the order of the checked call that it replaces. I compared each converted site with the original line. The inline `Store` and the other read-modify-write functions have the same shape as `SideMetadataManager::Update`: an atomic load of the word with the given order, then a `CompareExchange` loop. For `SequentiallyConsistent` the load has a full fence before and after, as before.

Proposals (not applied, not measured):

| # | Proposal | Argument | Expected effect |
|---|---|---|---|
| P1 | In `Update`, `Store` and the inline read-modify-write functions, drop the two fences around the first load when the order is `SequentiallyConsistent`. | The first load only gives the comparand. The `CompareExchange` that follows is a full barrier on x64 and ARM64, and it retries with the value that it saw. A stale first load cannot change the result. | `RegisterObjectInClearedSpan` is 11% of the samples of `binarytrees` (one store per allocation, three serializing instructions). This could remove two of the three. |
| P2 | In `IsRangeZero` and `VisitWords` with `SequentiallyConsistent`, use one fence before and one after the whole range, not two per word. | A block has 128 words. The block sweep runs with the block locked. Open: the order of the word loads against concurrent writers needs a written argument. | `IsRangeZero` is 5% to 8% of the samples. |
| P3 | In the block reads (`IsNurseryFast`, `GetStateFast`), use acquire loads. | The epoch is flipped only in a quiescent pause (the S1 result: the flip assertion never fired). Open: needs the same written argument. | Each `IsNurseryFast` has four fences today. |

### Profile before and after (Measured)

Method: `perf record -F 499 -e cpu-clock:u -g`, one run per cell, 4x heap (40 MiB for `binarytrees` and `listsuffix`, 84 MiB for `requests`). "l1" is the layer-1 tip, "s2" is the S2 build. The share is a part of all user-CPU samples of the process. The columns: self share of the three checked functions; sum of all checked point accesses (these three and `IsDataAddressOwned`, `Update`, `CompareExchange`, `IncrementSaturating`, and so on); inclusive share of `Load`; share of the range functions (`VisitWords`, `IsRangeZero`, `ResetRange*`). Scripts: `perf-three.sh`, `perf-mature.sh`, `perf-report.sh`, `perf-shares.py`. Raw reports: `results/perf-s2/`, table: `results/perf-shares-s2.md`.

| profile | Load self | Store self | GetLocation self | all checked point accesses | Load inclusive | range functions |
|---|---|---|---|---|---|---|
| l1 binarytrees nursery | 6.3% | 12.2% | 5.8% | 25.7% | 9.2% | 5.0% |
| s2 binarytrees nursery | 0.7% | 0.4% | 0.5% | 1.8% | 1.0% | 5.2% |
| l1 requests nursery | 6.9% | 7.5% | 6.6% | 22.5% | 9.8% | 9.8% |
| s2 requests nursery | 0.6% | 0.3% | 0.3% | 1.5% | 0.8% | 8.5% |
| l1 listsuffix nursery | 15.1% | 3.0% | 4.7% | 24.2% | 17.9% | 2.5% |
| s2 listsuffix nursery | 0.2% | 0.1% | 0.2% | 0.8% | 0.3% | 2.9% |
| l1 binarytrees mature | 7.6% | 11.5% | 8.6% | 29.2% | 12.1% | 4.8% |
| s2 binarytrees mature | 0.3% | 0.5% | 0.5% | 1.6% | 0.6% | 5.1% |
| l1 requests mature | 7.0% | 7.9% | 5.9% | 22.2% | 9.7% | 9.8% |
| s2 requests mature | 1.0% | 0.3% | 0.4% | 2.0% | 1.2% | 8.8% |
| l1 listsuffix mature | 15.1% | 2.8% | 4.6% | 23.8% | 17.7% | 2.8% |
| s2 listsuffix mature | 0.3% | 0.1% | 0.2% | 0.9% | 0.4% | 2.9% |

These shares are not a speed-up by themselves: the samples that left `Load` and `Store` are replaced by the same work in the callers. The wall-time table below is the speed measurement.

**What is left in the profile (Measured).** In the S2 profiles the largest symbols are: `ReferenceCountTable::RegisterObjectInClearedSpan` 10.9% (`binarytrees`, one `SequentiallyConsistent` store per allocation), `SideMetadataAccessor::IsRangeZero` 4.6% to 7.0% (`binarytrees`, `requests`; the block sweep), `GetObjectExtent` 12.9% (`listsuffix`; the extent table, S3), `ReclaimNursery` 8.5% and `CompleteAllocation` 8.4%. Inferred: the cost of the first two is the memory order, not the checks (the hottest instructions follow an `mfence`). This is the subject of P1 and P2.

**The order of the work (Measured).** I converted in rounds and profiled after each round. The first round (the pause loops and the RC engine) took the checked share of `binarytrees` from 30.8% (all `SideMetadataManager` symbols, this count includes the range functions) to 9.2%. The allocator line scan and the block queries (second round) and the mature-copy paths and the block-byte helpers (third round) took the rest. I did not measure the gain of each round in wall time (Open).

### Wall time (Measured)

Method: the P17 matrix scripts. Three builds (the accepted `e8d2d23` binary, the layer-1 tip `396e69f0685` and S2), 3 workloads, heaps 2x, 4x and 8x, 3 modes, 3 repetitions (243 runs). The three repetitions run at the same time, each on its own 5 cores, and the builds inside one repetition run in a shuffled order (the same deviation from P14 as in layer 1, so absolute times are not comparable with P14). All checksums are equal. Raw runs: `results/matrix-s2-acc-l1-s2.jsonl`. Tables: `results/summary-s2.md`, `results/wall-table-s2.md`, `results/compare-*.md`. Median wall time in ms. A cell is "all 3 below 0.95" when each of the three paired repetition ratios (S2 / layer 1) is below 0.95. All other cells are "mixed": the noise of the host can explain the median.

| workload | heap x | mode | accepted ms | layer 1 ms | S2 ms | S2 / layer 1 | S2 / accepted | paired repetitions S2 / layer 1 | reading |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | 6021 | 4964 | 4313 | 0.87 | 0.72 | 0.85, 0.85, 0.88 | all 3 below 0.95 |
| binarytrees | 2 | nursery | 7070 | 6694 | 5944 | 0.89 | 0.84 | 0.88, 0.93, 0.83 | all 3 below 0.95 |
| binarytrees | 2 | mature | 7319 | 6262 | 5835 | 0.93 | 0.80 | 0.70, 1.04, 0.93 | mixed |
| binarytrees | 4 | nonmoving | 4815 | 4007 | 3300 | 0.82 | 0.69 | 0.74, 0.83, 0.82 | all 3 below 0.95 |
| binarytrees | 4 | nursery | 5420 | 4580 | 4111 | 0.90 | 0.76 | 0.86, 0.98, 0.93 | mixed |
| binarytrees | 4 | mature | 5470 | 4237 | 3806 | 0.90 | 0.70 | 0.78, 1.15, 0.89 | mixed |
| binarytrees | 8 | nonmoving | 5102 | 3312 | 2728 | 0.82 | 0.53 | 0.82, 0.86, 0.81 | all 3 below 0.95 |
| binarytrees | 8 | nursery | 5495 | 3594 | 3028 | 0.84 | 0.55 | 0.83, 0.88, 0.84 | all 3 below 0.95 |
| binarytrees | 8 | mature | 5414 | 3611 | 3109 | 0.86 | 0.57 | 0.85, 0.87, 0.86 | all 3 below 0.95 |
| requests | 2 | nonmoving | 38119 | 38009 | 28246 | 0.74 | 0.74 | 0.76, 0.75, 0.74 | all 3 below 0.95 |
| requests | 4 | nonmoving | 29098 | 27447 | 23880 | 0.87 | 0.82 | 0.87, 0.87, 0.88 | all 3 below 0.95 |
| requests | 4 | nursery | 33943 | 17116 | 14439 | 0.84 | 0.43 | 0.84, 0.91, 0.82 | all 3 below 0.95 |
| requests | 4 | mature | 38665 | 17119 | 14539 | 0.85 | 0.38 | 0.84, 0.87, 0.85 | all 3 below 0.95 |
| requests | 8 | nonmoving | 24609 | 18414 | 14886 | 0.81 | 0.60 | 0.83, 0.81, 0.78 | all 3 below 0.95 |
| requests | 8 | nursery | 29289 | 15081 | 12498 | 0.83 | 0.43 | 0.83, 0.80, 0.94 | all 3 below 0.95 |
| requests | 8 | mature | 29955 | 14567 | 12858 | 0.88 | 0.43 | 0.91, 0.86, 0.88 | all 3 below 0.95 |
| listsuffix | 4 | nonmoving | 19918 | 17645 | 13573 | 0.77 | 0.68 | 0.76, 0.78, 0.78 | all 3 below 0.95 |
| listsuffix | 4 | nursery | 46627 | 43465 | 32729 | 0.75 | 0.70 | 0.76, 0.40, 0.81 | all 3 below 0.95 |
| listsuffix | 4 | mature | 50429 | 44891 | 43227 | 0.96 | 0.86 | 0.94, 0.96, 0.98 | mixed |
| listsuffix | 8 | nonmoving | 20753 | 19020 | 16311 | 0.86 | 0.79 | 0.81, 0.86, 0.86 | all 3 below 0.95 |
| listsuffix | 8 | nursery | 28271 | 23709 | 21357 | 0.90 | 0.76 | 0.90, 0.89, 0.95 | all 3 below 0.95 |
| listsuffix | 8 | mature | 30458 | 26109 | 23056 | 0.88 | 0.76 | 0.85, 0.90, 0.88 | all 3 below 0.95 |

| Comparison (22 cells) | Wall time | Collection time (`gc_ms`) | CPU time (`cpu_ms`) |
|---|---|---|---|
| S2 / layer 1, geometric mean (range) | 0.852 (0.743 to 0.963) | 0.794 (0.650 to 0.963) | 0.866 (0.740 to 1.117) |
| S2 / accepted, geometric mean (range) | 0.642 (0.376 to 0.857) | not computed | not computed |
| layer 1 / accepted, geometric mean (range) | 0.753 (0.443 to 0.997) | not computed | not computed |

Cells that did not finish are not in the table: `listsuffix` 2x (all builds, 27 runs) and `requests` 2x nursery and mature (layer 1 and S2; the accepted build finishes in about 46 s). Both are known from layer 1. The cell `requests` 2x non-moving finishes in all builds. **Cells that are not better:** none is worse (median ratio below 1 in all 22 cells). The cell `listsuffix` 4x mature (0.96) is a gain of at most 4% and its repetitions are 0.94 to 0.98. The `mature` cells of `binarytrees` 2x and 4x have repetitions from 0.70 to 1.15, so their medians are noise. In the first S2 matrix, before the last conversions, `binarytrees` 8x mature showed one outlier cell (median 1.15); ten repetitions of that cell alone gave a stable 0.855. That cell is 0.86 in this matrix (Measured).

### Pause times (Measured)

Traced runs, 4x heap, three repetitions per build and cell, interleaved (`trace-s2.sh`, `pause-table.py`, `pause-compare.py`). The table pools the pauses of the three runs: n, median and maximum in ms. The accepted build uses the P15 tracer, the others the P17 tracer. The kind is the kind of the pause. "Full" pauses are the full collections of `requests` (non-moving; the moving modes have none in the layer-1 tip and in S2). Raw: `results/pause-tables-s2-x4.md`, `results/pause-compare-s2.md`, `results/trace-runs-s2.txt`.

| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | layer 1: n, median, max ms | S2: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 45, 83, 119 | 48, 83, 252 | 48, 57, 243 |
| binarytrees | non-moving | InitialMark | 3, 265, 266 | 3, 256, 257 | 3, 210, 227 |
| binarytrees | non-moving | FinalMark | 3, 167, 168 | 3, 85, 90 | 3, 39, 40 |
| binarytrees | nursery copy | RefCount | 46, 107, 338 | 54, 96, 186 | 54, 73, 162 |
| binarytrees | nursery copy | InitialMark | 3, 322, 335 | 2, 287, 301 | 2, 280, 307 |
| binarytrees | nursery copy | FinalMark | 3, 227, 228 | 1, 110, 110 | 2, 140, 143 |
| requests | non-moving | RefCount | 12, 103, 178 | 12, 101, 127 | 12, 68, 89 |
| requests | non-moving | InitialMark | 3, 178, 194 | 2, 204, 213 | 2, 136, 160 |
| requests | non-moving | FinalMark | 2, 184, 184 | 2, 207, 208 | 2, 155, 158 |
| requests | non-moving | Full | 157, 308, 662 | 158, 301, 392 | 158, 257, 330 |
| requests | nursery copy | RefCount | 11, 137, 278 | 191, 119, 193 | 187, 95, 157 |
| requests | nursery copy | InitialMark | 4, 200, 270 | 9, 185, 243 | 9, 166, 193 |
| requests | nursery copy | FinalMark | 3, 238, 249 | 9, 136, 169 | 9, 121, 149 |
| requests | nursery copy | Full | 156, 387, 716 | - | - |
| listsuffix | non-moving | RefCount | 176, 323, 670 | 175, 104, 419 | 165, 65, 383 |
| listsuffix | non-moving | InitialMark | 4, 512, 522 | 3, 463, 600 | 3, 414, 486 |
| listsuffix | non-moving | FinalMark | 2, 260, 280 | 3, 215, 225 | 3, 186, 199 |
| listsuffix | nursery copy | RefCount | 187, 552, 1177 | 180, 457, 666 | 175, 435, 663 |
| listsuffix | nursery copy | InitialMark | 4, 680, 693 | 3, 467, 472 | 3, 415, 434 |
| listsuffix | nursery copy | FinalMark | - | 6, 276, 471 | 5, 161, 256 |

The cells with only 1 to 3 pauses (InitialMark, FinalMark) are not statistics; I list them for completeness. The RefCount pauses and the Full pauses have enough samples. Counts of pauses differ between builds by a few percent because the pause schedule depends on timing. The counts are n of three runs pooled, and a failed run (see below) has fewer events.

### Intermittent failure of a traced run (Measured)

**Update: the cause is found and fixed. See the section "P17 fix: lazy release of dead large objects during a concurrent trace" below. The text of this section is the state before the fix.**

One of the traced runs of the S2 matrix did not finish: `listsuffix`, 4x heap (40 MiB), nursery-copy mode, `Fatal error. Error 2147500037 (0x80004005 (E_FAIL))`. I looked at it, because a new failure must be reported.

| What | Result |
|---|---|
| Traced run (tracer on, no debugger), 15 runs per build | accepted: 1 failed (the benchmark had printed its result; the failure was in the exit path); layer 1: 0; S2: 2 |
| Same cell, tracer off, no debugger (matrix + 16 extra runs per build) | accepted, layer 1, S2: 0 failures in 19 runs each |
| Traced run under `gdb` with a breakpoint on the fatal-error message (`catch-fatal-s2.sh`) | layer 1: 1 of 60 runs; S2: 9 of 88 runs |
| Traced run under `gdb` with a watchpoint on the engine failure code (`catch-fail-site.sh`) | layer 1: 5 of 80 runs; S2: 7 of 160 runs |

Where it fails (Measured, 7 of 7 watchpoint catches in S2 and 3 of 5 that I read in layer 1): the engine writes `MetadataFailure` to its failure code in `LxrReferenceCounting::Sweep`, in the lazy sweep of dead large objects (the branch that has a block-release guard). The engine goes to the phase `Faulted`. Then every later call fails, and the host prints one of these messages: `LXR space-pressure collection failed before growth admission.`, `LXR allocation-complete metadata publication failed.`, `LXR pre-store logging failed before the managed field was overwritten.`, `LXR owned allocator failed an allocation invariant.` The trace phase is `Concurrent` in all caught runs.

What I know (Code): in that branch `LxrLargeObjectSpace::ReleaseAfterDecrements` returns `InvalidPhase` when the reference count of the dead large object is not zero, or its nursery bit is set, or the large-object phase is neither Mutator nor Release. Any other result of `GetAllocation` or `ReleaseAfterDecrements` has the same effect. I did not find out which of these happens, and why. I did not change `large_object_space.cpp`, `Death` for large objects, or the sweep.

What I conclude (Inferred): the failure is not caused by S2. The same site fails in the layer-1 tip, and the rates are not different in a way that I can show (layer 1 4.3% of 140 gdb runs, S2 6.5% of 248; the difference is not significant). The accepted build also fails at a traced `listsuffix` 4x run (1 of 15), and in layer 1 the report noted one such run of an early S0 build. I cannot rule out that S2 changes the timing of a latent race, because the faster pauses change the timing of everything. **Open: I recommend to find the cause before the work on the sweep (S4) and before S3, because both depend on the same lazy release.** The condensed gdb output is in `results/failure-sites-watchpoint.txt` and `results/failure-sites-fatal-message.txt` (the file names `sa`, `sb` are S2 runs, `la`, `lb` are layer-1 runs; the rows from `catch-fatal` with the prefixes `s2a`, `s2b`, `l1a`, `l1b` are the same). The repeat runs are in `results/repeat-untraced-listsuffix-4x-nursery.jsonl` and `results/repeat-traced-listsuffix-4x-nursery.txt`. Scripts: `repeat-cell.sh`, `repeat-trace.sh`, `catch-fatal-s2.sh`, `catch-fail-site.sh`, `extract-catches.sh`. The `Out of memory.` runs of the matrix are a different failure.

### Tests (S2)

All on the final tree (S2 commit). "Pass" lists only what ran. Linux is WSL Ubuntu 22.04 (clang 14); Windows is MSVC (VS 18 preview).

| Suite | Linux Release | Linux Debug | Windows Release | Windows Debug |
|---|---|---|---|---|
| Native validation, 26 selectors (the 25 of layer 1 and the new `--inline-metadata`), all checks pass, `flip_overlaps=0 flip_blocked=0` | pass | pass (asserts armed) | pass | pass (asserts armed) |
| New selector `--inline-metadata` (465 checks over 3 reference-count widths and 15 kinds) | pass | pass | pass | pass |
| Managed scope-5 fixture, 84 cells (3 modes, collector Release and Debug, managed Release and Debug, full, finalizer, dependent and 8 lifecycle cases) | pass | pass (the Debug collector runs with asserts armed) | not run | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 (P3.3 357 of 357) | not run (the scripts build without `_DEBUG`) | not run | not run |
| `clrlxrgc` module build | pass | pass | pass | pass |

- **Positive control (Measured).** I made a deliberate error in the location formula (`shift ^ 1` in the mask). The Debug native program then stopped with `Assertion 'checked.mask == location.mask' failed` in the first selector that I ran. I removed the error. The Release binary of the final tree has no such code.
- **A test bug that Windows found (Measured).** The first version of the new test wrote one element beyond a vector for the kinds with large entries (a test error, not a collector error). The MSVC debug iterators stopped the Windows Debug run (`vector subscript out of range`). The Linux runs had passed by chance. I fixed the test and ran everything again.
- **P3.3** (`lxr-deferred-bucket-validation`) passed 357 of 357 in each of the three full legacy runs of this work (I ran them without load). The intermittent failure of layer 1 is still Open.
- Not run: the Windows managed suite (I did not find the accepted Windows layout on this machine) and the older P2.x and P3.x suites on Windows (the layer-1 report did not run them either).

### Not measured, open items (S2)

| Item | Label |
|---|---|
| Windows performance of S2 | Not measured |
| Windows managed suite | Not measured |
| The big-endian path of `SideMetadataAccessor::Locate` | Not tested (Code only) |
| Gain of each conversion round in wall time | Not measured |
| Cost of the Debug checks (every inline access also does the checked access) | Not measured. The Debug managed cells finished without a timeout. |
| P1, P2, P3 (memory orders) | Open: proposals with arguments, not applied |
| `RegisterObjectInClearedSpan` (11% of `binarytrees`) and `IsRangeZero` (5% to 8%) | Open: the cost of the memory order; P1 and P2 |
| `ResetRangeQuiescent` of `FieldUnlogged` per promoted object (2% to 3% of `listsuffix`), `InitializeObjectQuiescent` (1% to 2%) | Not converted. Candidates for a later round. |
| Value comparison of the Debug check for stores and read-modify-write | Not done: another thread can change the value between two accesses. The location is compared for every access. Loads and range checks also compare the value (repeated up to 16 times). |
| The `requests` 2x cliff (OOM or timeout in layer 1 and S2) | Open (accepted for now; P16 plan) |
| The accepted build failed once at `listsuffix` 4x mature (`Out of memory.`) | Open (known from layer 1, one cell) |
| Noise: the repetitions of one cell differ by up to +-15% (one repetition 0.40) | Measured; the 5 "mixed" cells are not evidence of a gain |
| Intermittent engine failure in the lazy sweep of dead large objects (`Sweep` with a guard; see "Intermittent failure of a traced run") | Open: found in the layer-1 tip and in S2, not caused by S2 as far as I can show; the cause is not found |

## P17 fix: lazy release of dead large objects during a concurrent trace

Status: committed locally on the branch `kkokosa-lxr-s2-metadata-fast-path`, on top of the S2 commit. Nothing is pushed.

### Short answers (fix)

1. **Root cause (Measured and Code).** `LxrLargeObjectSpace::ReleaseAfterDecrements` accepted only the phases `Mutator` and `Release`. A concurrent trace keeps the space in the phase `ConcurrentMark` between two pauses. The lazy sweep of a RefCount pause runs after the pause. If a large object died in that pause while a trace was active, and the lazy sweep ran after the end of the pause, the call returned `InvalidPhase`. `Sweep` turned that into `MetadataFailure`, and the engine went to `Faulted`. Every later call then failed, and the host printed the `Fatal error` messages.
2. **It is not the list and not the counts (Measured).** The diagnostic output of the failure shows: the reference count of the object is 0, the object is in the dead list once, and the list entry has the same scheduler generation as the sweep packet. So the list was not reset or overwritten by a later pause. Only the phase was wrong (`phase=1`, `ConcurrentMark`).
3. **Deterministic reproduction (Measured).** The new native selector `--lazy-large-release` makes the failure in every run: a large object is promoted, a concurrent trace starts, a RefCount pause kills the object, and the lazy sweep runs. Unfixed: the selector fails in Debug and Release with the same diagnostic values (`wrong LOS phase 1`, `trace=2`, `pause=1`). Fixed: it passes in Debug and Release.
4. **Fix (Code).** `ReleaseAfterDecrements` now rejects only the phase `Shutdown`. This is the whole change in the collector (one condition and a comment in `large_object_space.cpp`).
5. **Failure rate (Measured).** Traced `listsuffix`, 4x heap, nursery-copy mode, two parallel streams, same machine state, same script (`trace-loop.sh`): the build without the fix failed in 4 of 80 runs (5%). The build with the fix failed in 0 of 160 runs (two batches of 80). Fisher's exact test for 4 of 80 against 0 of 160 gives p = 0.012 (one-sided). The deterministic selector is the proof; the sample only supports it.
6. **The accepted build has the same bug (Code, Measured).** `e8d2d23` has the same phase condition in `ReleaseAfterDecrements`. It failed in 1 of 15 traced runs (the failure was in the exit path). I did not run the accepted build for 60 more runs.

### Why the failure is rare (Inferred)

The lazy sweep can run in two places. If it runs inside the pause (the space is in the phase `Release`), the call is accepted. If it runs after the pause has ended, the space is in `ConcurrentMark` (a trace is active) or in `Mutator`. Only the first of the two later cases fails. The failure needs three things together: a concurrent trace is active, a large object dies in a RefCount pause of that trace, and the lazy sweep of that pause starts after the pause ended. `listsuffix` has few large objects, so this is a few percent of the runs. In 3 of the 4 failed runs of the unfixed build the benchmark had already printed its result: the failure was at the end of the run.

### Is the release during a concurrent trace safe? (Code, Inferred)

- A dying object is marked and its scan is finished before its count reaches zero (`Death` calls `FinishDyingTraceObject` when a trace is active). So the trace holds no pending scan of the object.
- A stale reference to the object in a trace queue reads only the metadata first. `ProcessTraceObject` skips an object with count 0, with the mark bit set or without an extent.
- The release takes the lock of the large-object space, as the other operations do. The release decommits the pages and resets the object metadata, and it keeps the page reuse history. A new object in the same pages is allocated black during the trace, as in the other case.
- The Immix lazy release (`TryReleaseIfRcDead`) is already allowed during a concurrent trace. The large-object release was the only lazy release that was not.
- The design note P2.5 says that `Release` allows release. That note describes the eager release inside a pause. The lazy release was added later (P3.4) with the check for `Mutator` and `Release`, and `ConcurrentMark` was left out (Inferred: an omission).

### Tests (fix)

| Suite | Result |
|---|---|
| New selector `--lazy-large-release` (14 checks) on the unfixed tree | FAIL in Linux Debug and Release (8 of 10 checks pass; the two failing checks are the lazy sweep and the engine fault) |
| Same selector on the fixed tree | pass 14 of 14, Linux Debug and Release |
| Linux native suite, fixed tree, Release (27 selectors) | pass 27 of 27 (includes `--inline-metadata` 465/465 and `--lazy-large-release` 14/14) |
| Linux native suite, fixed tree, Debug (27 selectors) | pass 27 of 27 |
| Windows native suite (MSVC, `/W4 /WX`), Release and Debug (27 selectors each) | pass 27 of 27 in each configuration |
| Linux managed cells with the Debug collector, fixed tree | 84 of 84 cells ok |
| Legacy suites P2.1 to P3.4, fixed tree (Linux) | 9 of 9 pass (P3.1 642/642, P3.2 380/380, P3.3 357/357, P3.4 57/57) |
| Windows managed suite, Windows build of `clrlxrgc` | Not run in this step |

The fix does not touch a hot path (it is one comparison in a rare function), so I did not run the P17 matrix again. The S2 numbers above stand.

### Not done, open items (fix)

| Item | Label |
|---|---|
| 60 or more traced runs of the accepted build, to measure its rate of the same failure | Not measured |
| A test of the release of a large object that dies during the final-mark pause, and of the cycle sweep with large objects | Not written. The cycle sweep adds large objects to the same list, but it runs in the phase `Mutator`. |
| The comment in the design note P2.5 about the phases | Not updated |

## S3: remove the per-object size table, the live bitmap and the per-object counters

Status: committed locally on the branch `kkokosa-lxr-s3-remove-per-object-tables`, on top of the S2 tip `37adf16d1f6` (layer 1, S2, large-object fix). Nothing is pushed. `lxr`, the layer-1 branch and the S2 branch are not changed.

### Short answers (S3)

1. **The size table (`m_extents`), the live bitmap (`m_live_objects`) and the per-object byte counters are gone from the product** (Code). `m_live_bytes` and `m_reclaimed_bytes` stay only in the test platform, as an oracle for the tests. The .NET memory APIs use a block-count estimate (blocks in use times block size, plus large-object bytes).
2. **The pause no longer visits dead young objects** (Code). `ReclaimNursery` has no per-object work now. Its timer reads 0.0 ms. In S2 it took 19% to 35% of a RefCount pause on `binarytrees` and `requests` (Measured, table "Phase shares").
3. **Measured effect on memory.** Peak RSS is lower in all 22 cells compared with S2; the geometric mean of the RSS ratio S3/S2 is 0.81 (range 0.49 to 0.93). On `requests` the RSS is 19% to 24% lower. The committed native planes of the 40 MiB heap shrink by 32.6 MiB (244.7 to 212.0 MiB); for the 84 MiB heap by 75.3 MiB (531.6 to 456.3 MiB) (Measured, metric 38).
4. **Measured effect on speed.** Wall time S3/S2: geometric mean 0.768 over 22 cells (range 0.478 to 1.059). Collection time (`gc_ms`) S3/S2: 0.643. RefCount pauses are 35% to 58% shorter at the median on `binarytrees` and `requests` (6% to 21% shorter on `listsuffix`). Two cells are within noise or worse than S2: `listsuffix` 8x mature (1.06; repetitions 1.23, 0.75, 1.01) and `listsuffix` 8x non-moving (0.99). **I do not claim a gain there.**
5. **The cycle sweep reads the size of a dead object from the object (decision D1, option 1).** This is safe only if the runtime cannot free the type data (the MethodTable of a collectible type) before the sweep ends. The runtime frees it only on the finalizer thread. I hold finalization while a cycle sweep is pending. This is entirely inside the GC; no new EE or VM interface (Code; section "Guard for D1").
6. **Two bugs were found by the matrix and the managed suite after the first S3 commits, and are fixed in S3e and S3f.** Both came from reading an object header that is not valid any more. Section "Bugs found after the first commit". Both have a native test that fails without the fix.
7. **No new failure and no new slowdown in the final matrix.** The 39 failed runs are the known ones: `listsuffix` 2x (27 runs, all builds, `Out of memory.`) and `requests` 2x nursery and mature (12 runs, S2 and S3, `Out of memory.`). All checksums are equal.
8. **Known cost:** `GetHeapStatistics` is a walk over the ValidObject bits now. It takes 1.6x to 2.4x longer in a pause (for example `requests` 4.3 ms to 10.4 ms median), because it has no size table. S4 makes it incremental. The cost is small against the gain in `ReclaimNursery` (Measured).

### Commits

| Part | Commit | What |
|---|---|---|
| S3a | `c48770edcb6` | The byte counters exist only in the test platform. New block-count estimate for the memory APIs. |
| S3b, S3c | `24d320076d5` | Walkers and queries use the ValidObject plane; the size table and the live bitmap are removed from the product; the forwarded-source size is kept in a per-worker copy log; the D1 guard. |
| S3d | `495254ac9e0` | Tests: new selector `--s3-bookkeeping`, managed case `collectible-cycles`, positive controls. |
| S3e | `9bf510f573c` | Fix: an interior root into a forwarded array source takes the size from the copy. |
| S3f | `4408ad43f32` | Fix: remembered-slot replay does not read a stale ValidObject start. |
| docs | this commit | S3 section, result files, scripts. |

### What the size table and the live bit served, and what replaces them (Code)

| Use | Before S3 | After S3 |
|---|---|---|
| (a) Is this address the start of a live object? (`IsMarked`, `ResolveForwarded`, `TryCopyNursery`, `TryCopyMature`, `ProcessTraceObject`, `ArmLifetimeProbe`, `ArmScanCheckpoint`, the finalizer and handle checks in `lxr_gc.cpp`) | `GetObjectExtent != 0` | `IsObjectStart`: the ValidObject bit. `IsMarked` also needs a count above zero, because a black-allocated young object that died keeps its mark and its bit. |
| (b) Size of a forwarded source (`RetireForwardedSources`, `WalkForwarded`) | the size table | A per-worker copy log: each copy writes (source, size) before the source is overwritten. The retire step reads the log. The log has `workers + 1` slots; the last one is locked for the mature copy. |
| (c) Size of a dead cycle object (`SweepCycles`) | the size table | From the object itself, with the D1 guard. The sweep also clears the ValidObject bit of the swept object. |
| (d) Walkers: `WalkObjects`, `FindNextObject`, `GetHeapStatistics`, `GetMarkedBytes`, diagnostic walks, `RetainPinnedMatureBlocks`, the finalizer-flag walks | `FindNextOccupiedSlot` over the size table | `FindNextValidSlot`: a word-at-a-time scan of the ValidObject plane. Walkers that need live objects take only bits with a count above zero and skip forwarded sources. `WalkObjectStarts` is the raw walk (no size) for the finalizer-flag walks. |
| (e) Large objects | the size table | `GetLargeObjectSize`: the page count of the large-object allocation record. |
| (f) `GetHeapStatistics` | the size table | A walk over the ValidObject bits with a count above zero. **Meaning changed:** dead young objects are no longer counted. |
| (g) `FindContainingObject` (interior pointers) | the size table | The nearest ValidObject bit at or below the address, the size from the object. See the stale-bit section. |
| (h) .NET memory APIs (`GetTotalBytesInUse`, `GetCurrentObjSize`, memory info, metric 3) | `m_live_bytes` | `GetUsedBytesEstimate()`: blocks in use times block size plus large-object committed bytes. Updated at block state changes only. |
| (i) The lifetime probe (young deaths) | `ReclaimNursery` per object | One check per pause in `ClearAllocatedBlocks` (`NoteProbeReclaim`, flag `m_probe_retired`). |

Native commit: the size table (8 bytes per 8 bytes of heap) and the live bitmap are not committed any more (`CommitPlanes`, `InitializeBuffers`, `GrowSuspended`, shutdown). `m_flags`, `m_trace_locks` and `m_trace_scans` stay (S6 and S7). `m_allocated_block_storage` stays (S4).

### Check C3: every reuse path clears the ValidObject plane and the count bytes (Code and Measured)

A stale ValidObject bit remains for a dead young object until its range is reused or its block is released. The walkers and the queries must never trust such a bit alone. Paths that reuse a range:

| Path | What clears the range |
|---|---|
| Fresh block handed to an allocator | `InitializeFieldLogRange` clears ValidObject (and the field log state) for the span |
| Recycled holes (line recycling) | the same call, for the hole that is taken; holes are made only from lines with count zero |
| Copy destination (nursery and mature) | the same call, before the copy |
| Lazy release of a block with a dead range (`TryReleaseIfRcDead`) | resets ValidObject, FieldUnlogged and straddle bits |
| Large-object allocation and release | the record is reset at both |
| Cycle sweep | clears the bit of each swept object |

The count bytes are zero by construction: a hole is made from lines whose count is zero, and a block is released only when its count range is zero. The Debug build checks this at every allocation (Code).

**Proof by test (Measured).** The selector `--s3-bookkeeping` runs a random sequence (60 rounds, non-moving and nursery copy) with reuse of holes and blocks. It compares the ValidObject plane, the counts, the walkers, the heap statistics and random interior lookups with the oracle of the test platform. 19954 checks pass in Linux Debug. **Positive control:** with `-DLXR_VALID_CLEAR_CONTROL` (hole reuse does not clear the ValidObject plane) the selector fails 7 checks.

### Stale bits and `FindContainingObject` (Code, Measured)

- A valid interior pointer always finds the start bit of its own object first. No stale bit lies inside the range of a live object, because every reuse clears the range before only the start bit of the new object is set. So the nearest bit at or below a valid interior pointer is the right start (Code; random test in `--s3-bookkeeping`).
- A lookup for an address that is in no live object has an undefined answer. S2 and the accepted build were safe there, because the size table was exact. Now the nearest bit can be stale, and its memory can be overwritten.
- The first S3 commit read the size from that stale object. **That crashed `requests` (nursery and mature copy, 4x heap) in the replay of remembered slots.** The slot of a dead holder lay in memory that a new object had overwritten, and the nearest bit was an older stale bit with a zero MethodTable (Measured, `gdb`). Fix S3f: `FindContainingObject(address, &object, requireCounted)`. With `requireCounted` set, a candidate with count zero is rejected before its header is read. The replay uses it. Roots and the EE keep the old behavior (their addresses lie inside live objects).
- **Open (hardening):** a caller that passes an address inside no live object, without `requireCounted`, can still read a stale header. The EE callers (`GetContainingObject`, root scan) pass only interior pointers of live objects. I did not harden them further.

### Bugs found after the first commit (Measured)

| Bug | How found | Cause | Fix | Test |
|---|---|---|---|---|
| Fatal `E_FAIL` in the managed `dependent` case (Release managed build, mature copy) | The managed suite of the first S3 build: 2 of 84 cells failed; the cell failed in 3 of 3 standalone runs | An interior root pointed into a forwarded array. The source had lost its length (first field replaced by the forwarding pointer), so the size read from the source was wrong and `FindContainingObject` failed | S3e: the size is read from the copy | `--s3-bookkeeping`: interior roots into forwarded array sources, nursery and mature. Fails without the fix (9 of 19943 checks). |
| `SIGSEGV` in `requests` 4x mature (3 of 3 runs), 2x mature (all, but that cell is a known failure) and 8x mature (1 of 3) | The first S3 matrix | The stale bit above | S3f | `--s3-bookkeeping`: a stale bit over overwritten memory. The selector crashes without the fix. |

I ran the full chain again after each fix. The first matrix (with the crash) is kept in `results/matrix-s3-first-with-segv.jsonl`. It is not used for the numbers below.

### Guard for D1: the cycle sweep reads sizes from dead objects (Code)

The problem: a dead cycle object of a collectible type has its type data in a loader allocator. The sweep runs lazily after the pause. If the runtime freed the loader allocator before the sweep read the object, the read would touch freed memory.

Where the runtime frees it (Code, `src/coreclr/vm`): only `LoaderAllocator::GCLoaderAllocators`. It runs from `LoaderAllocator::Destroy` (the finalizer of `LoaderAllocatorScout`) and from `DynamicMethodDesc::TryDestroy`. Both run on the finalizer thread. The weak handle of the managed `LoaderAllocator` object is cleared in the tracing pause, but the free happens later, on the finalizer thread.

The guard (all in `lxr_gc.cpp`, no new EE or VM interface):

- `GetNextFinalizable` returns null while `IsCycleSweepPending()`.
- `Resume` records `m_finalization_deferred = true`. It signals finalization only when no sweep is pending and the flag was set.
- `OverallDrained` (the end of the lazy sweep) clears the pending state first. Then `FinishLazy` clears the flag and calls `EnableFinalization`.

Side effect (Inferred): finalizers start a little later, until the lazy sweep ends. `GC.WaitForPendingFinalizers` called at the same time can return early. The managed finalizer tests pass (all lifecycle cases, 90 cells).

**Tests.**

| Test | What it shows |
|---|---|
| Native `--s3-bookkeeping`, gate model | A model of the finalizer thread frees the type data only when `IsCycleSweepPending()` is false. The sweep must read all sizes first. **Positive control:** `-DLXR_CYCLE_GATE_CONTROL` makes the gate always open; the selector fails 3 checks. |
| Native `ValidateCycleReclamation` | Poisons the types after the sweep, as the runtime would. (The old version poisoned them before; that is the rule 4 relaxation of decision 1.) |
| Managed `collectible-cycles` | 40 unloadable `AssemblyLoadContext` rounds, each with dead cycles of collectible-type objects and a 150000-node ballast; at least 30 must unload. Passes in all 6 cells (3 modes, Debug and Release collector). |

**Measured limit of the managed test.** It also passes 40 of 40 on a build with the gate switched off (`ctl-Release-linux`). The race window is short, so the managed test does **not** discriminate the gate. Only the native test does. I report the managed test as a smoke test of the unload path with dead cycles, not as proof of the gate.

### Results (Measured)

Matrix: 3 workloads x 2x, 4x, 8x heap x 3 modes x 3 repetitions, three builds in one interleaved run (`matrix-parallel.sh m-s3f`, 243 runs): accepted `e8d2d23` (`acc`), S2 tip `37adf16d1f6` (`s2`) and S3 tip (`s3`, collector built from `4408ad43f32`). Same method as above. Raw runs and tables: `results/matrix-s3-acc-s2-s3.jsonl`, `results/summary-s3.md`, `results/wall-table-s3.md`, `results/compare-*-s3-*.md`. Checksums are equal in all runs (3 checksums).

Wall time and peak RSS (medians of 3; "repetitions" are the S3/S2 ratios of the three paired repetitions; cells with a range that includes 1.0 are inside the noise):

| workload | heap x | mode | wall ms acc / S2 / S3 | S3 / S2 (repetitions) | peak RSS MB acc / S2 / S3 |
|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | 5864 / 4062 / 3515 | 0.87 (0.85, 0.90, 0.87) | 146 / 151 / 139 |
| binarytrees | 2 | nursery | 7028 / 5162 / 4493 | 0.87 (0.85, 0.84, 0.98) | 146 / 156 / 142 |
| binarytrees | 2 | mature | 7009 / 5634 / 4188 | 0.74 (0.81, 0.71, 0.74) | 148 / 154 / 143 |
| binarytrees | 4 | nonmoving | 4708 / 3297 / 2653 | 0.80 (0.75, 0.80, 0.83) | 229 / 224 / 190 |
| binarytrees | 4 | nursery | 5652 / 3662 / 2744 | 0.75 (0.72, 0.61, 0.78) | 229 / 221 / 183 |
| binarytrees | 4 | mature | 5538 / 3894 / 2271 | 0.58 (0.58, 0.74, 0.56) | 229 / 226 / 111 |
| binarytrees | 8 | nonmoving | 5114 / 3122 / 2100 | 0.67 (0.68, 0.78, 0.57) | 314 / 314 / 243 |
| binarytrees | 8 | nursery | 5592 / 3057 / 2190 | 0.72 (0.68, 0.98, 0.72) | 314 / 259 / 188 |
| binarytrees | 8 | mature | 5515 / 3103 / 2109 | 0.68 (0.75, 0.68, 0.69) | 314 / 259 / 188 |
| requests | 2 | nonmoving | 39142 / 30548 / 23069 | 0.76 (0.79, 0.76, 0.72) | 184 / 184 / 149 |
| requests | 4 | nonmoving | 30731 / 26484 / 23935 | 0.90 (0.93, 0.92, 0.85) | 341 / 370 / 295 |
| requests | 4 | nursery | 37634 / 15090 / 10660 | 0.71 (0.73, 0.69, 0.71) | 368 / 356 / 282 |
| requests | 4 | mature | 41340 / 15221 / 10718 | 0.70 (0.64, 0.75, 0.71) | 368 / 382 / 304 |
| requests | 8 | nonmoving | 26964 / 16882 / 15650 | 0.93 (0.94, 0.84, 0.96) | 658 / 658 / 509 |
| requests | 8 | nursery | 28606 / 12896 / 10414 | 0.81 (0.85, 0.67, 0.81) | 658 / 658 / 508 |
| requests | 8 | mature | 30938 / 13444 / 9465 | 0.70 (0.70, 0.77, 0.66) | 658 / 650 / 497 |
| listsuffix | 4 | nonmoving | 22102 / 15187 / 13129 | 0.86 (0.83, 0.86, 1.04) | 299 / 306 / 270 |
| listsuffix | 4 | nursery | 51351 / 41363 / 19765 | 0.48 (0.49, 0.53, 0.33) | 305 / 290 / 242 |
| listsuffix | 4 | mature | 51523 / 41050 / 35509 | 0.87 (0.85, 0.84, 0.90) | 303 / 285 / 254 |
| listsuffix | 8 | nonmoving | 22577 / 17534 / 17272 | 0.99 (0.99, 0.92, 1.04) | 489 / 491 / 458 |
| listsuffix | 8 | nursery | 29318 / 23098 / 16040 | 0.69 (0.74, 0.77, 0.58) | 530 / 524 / 456 |
| listsuffix | 8 | mature | 33189 / 22654 / 23997 | 1.06 (1.23, 0.75, 1.01) | 533 / 515 / 447 |

| Comparison (22 cells) | Wall time | Collection time (`gc_ms`) | Peak RSS |
|---|---|---|---|
| S3 / S2, geometric mean (range) | 0.768 (0.478 to 1.059) | 0.643 (0.320 to 1.111) | 0.809 (0.493 to 0.932) |
| S3 / accepted, geometric mean (range) | 0.492 (0.259 to 0.779) | not computed | 0.796 (0.486 to 0.974) |

- **Within noise (not a gain):** `listsuffix` 8x mature (1.06), `listsuffix` 8x non-moving (0.99), `listsuffix` 4x non-moving (one repetition 1.04), `binarytrees` 2x nursery and 8x nursery (one repetition 0.98). In `listsuffix` 8x mature S3 also made 33 pauses against 21 in S2 (median of 3); I did not find out why (Open).
- **Large gains are real in this run, but I did not isolate all causes.** `ReclaimNursery` and `GetObjectExtent` are gone from the profile (see below), and the collector commits about 20% less memory. Wall time falls more than the pause time alone explains for some cells (`listsuffix` 4x nursery 0.48; `binarytrees` 4x mature 0.58). The cause is Open. Inferred: fewer cache misses and fewer page faults.
- **Bimodal RSS (Open):** `binarytrees` 4x mature: 111 MB in two repetitions, 195 MB in one. Not investigated.
- **Failures:** 39 of 243 runs, all known (see short answer 7). No new failure. The first S3 matrix had 4 more (`requests` 4x and 8x mature, `SIGSEGV`); fixed in S3f.

### Pause times (Measured)

Traced runs, 4x heap, three repetitions per build and cell, interleaved (`trace-s3.sh`, `s3-tables.py`; 54 runs, all finished, all checksums equal). Pooled pauses of the three runs: n, median and maximum in ms. The accepted build uses the P15 tracer, the others the P17 tracer. Raw: `results/trace-tables-s3-x4.md`, `results/trace-runs-s3.txt`.

| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S2: n, median, max ms | S3: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 46, 84, 259 | 48, 57, 226 | 48, 33, 212 |
| binarytrees | non-moving | InitialMark | 3, 286, 291 | 3, 220, 234 | 3, 221, 223 |
| binarytrees | non-moving | FinalMark | 3, 141, 164 | 3, 58, 94 | 3, 46, 62 |
| binarytrees | nursery copy | RefCount | 48, 108, 356 | 55, 73, 111 | 54, 31, 135 |
| binarytrees | nursery copy | InitialMark | 3, 313, 331 | - | 2, 246, 293 |
| binarytrees | nursery copy | FinalMark | 3, 84, 143 | - | 2, 111, 126 |
| requests | non-moving | RefCount | 11, 103, 184 | 12, 68, 87 | 12, 44, 65 |
| requests | non-moving | InitialMark | 3, 180, 181 | 2, 177, 179 | 3, 138, 145 |
| requests | non-moving | FinalMark | 2, 176, 178 | 2, 164, 168 | 3, 153, 161 |
| requests | non-moving | Full | 157, 313, 615 | 158, 271, 341 | 156, 229, 282 |
| requests | nursery copy | RefCount | 11, 140, 283 | 189, 95, 158 | 189, 53, 110 |
| requests | nursery copy | InitialMark | 4, 207, 271 | 9, 167, 192 | 9, 123, 161 |
| requests | nursery copy | FinalMark | 3, 236, 258 | 9, 124, 147 | 9, 63, 95 |
| requests | nursery copy | Full | 156, 393, 497 | - | - |
| listsuffix | non-moving | RefCount | 178, 356, 711 | 163, 65, 425 | 156, 61, 412 |
| listsuffix | non-moving | InitialMark | 4, 516, 547 | 3, 452, 473 | 3, 429, 450 |
| listsuffix | non-moving | FinalMark | 1, 269, 269 | 4, 207, 228 | 3, 126, 222 |
| listsuffix | nursery copy | RefCount | 187, 562, 1151 | 214, 438, 580 | 166, 345, 671 |
| listsuffix | nursery copy | InitialMark | 4, 499, 740 | 3, 423, 444 | 3, 394, 411 |
| listsuffix | nursery copy | FinalMark | - | 4, 155, 377 | 6, 213, 318 |

Cells with 1 to 6 pauses are not statistics. The `listsuffix` nursery-copy FinalMark median is higher in S3 (213 against 155 ms, n = 6 against 4): small n, not a conclusion (Open). The InitialMark and FinalMark counts differ between builds because the pause schedule depends on timing.

Phase shares and times in RefCount pauses (S0 timers; the accepted build has no timers). The phases overlap: `release_epoch` contains `reclaim_nursery` and `sweep_blocks`; `root_capture` contains `scan_lease_finalizers`. So the shares do not add up to 100%.

| workload | mode | build | RefCount pauses | median pause ms | ReclaimNursery % of pause | ReclaimNursery median ms | LeaseFinalizer median ms | GetHeapStatistics median ms | release_epoch median ms |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | S2 | 48 | 57 | 35.1 | 24.1 | 15.1 | 1.2 | 37.0 |
| binarytrees | non-moving | S3 | 48 | 33 | 0.0 | 0.0 | 13.3 | 1.9 | 13.0 |
| binarytrees | nursery copy | S2 | 55 | 73 | 27.7 | 21.2 | 22.5 | 1.3 | 43.9 |
| binarytrees | nursery copy | S3 | 54 | 31 | 0.0 | 0.0 | 12.0 | 2.6 | 11.6 |
| requests | non-moving | S2 | 12 | 68 | 35.2 | 24.8 | 17.5 | 4.3 | 38.3 |
| requests | non-moving | S3 | 12 | 44 | 0.0 | 0.0 | 13.7 | 10.4 | 14.0 |
| requests | nursery copy | S2 | 189 | 95 | 19.3 | 18.7 | 24.5 | 5.3 | 52.6 |
| requests | nursery copy | S3 | 189 | 53 | 0.0 | 0.0 | 11.7 | 9.2 | 19.4 |
| listsuffix | non-moving | S2 | 163 | 65 | 7.9 | 10.1 | 11.4 | 5.6 | 12.9 |
| listsuffix | non-moving | S3 | 156 | 61 | 0.0 | 0.0 | 11.5 | 10.9 | 2.7 |
| listsuffix | nursery copy | S2 | 214 | 438 | 2.0 | 9.4 | 17.8 | 5.5 | 22.6 |
| listsuffix | nursery copy | S3 | 166 | 345 | 0.0 | 0.0 | 11.0 | 12.9 | 2.8 |

The `LeaseFinalizer` phase does not become slower with the extra stale bits (11 to 14 ms in S3, equal or lower than S2). The new cost is `GetHeapStatistics`.

### Native plane commit size (Measured)

Metric 38 (engine planes plus `m_flags`), maximum over a traced run, median of three runs. The managed committed bytes are the same in both builds.

| workload | heap MiB | mode | S2 native planes (MiB) | S3 native planes (MiB) | saved (MiB) | managed committed (MiB) |
|---|---|---|---|---|---|---|
| binarytrees | 40 | non-moving | 244.7 | 212.0 | 32.6 | 32.0 |
| binarytrees | 40 | nursery copy | 208.7 | 212.0 | -3.4 | 32.0 |
| requests | 84 | non-moving | 531.6 | 456.3 | 75.3 | 74.3 |
| requests | 84 | nursery copy | 531.6 | 456.3 | 75.3 | 74.3 |
| listsuffix | 40 | non-moving | 244.7 | 212.0 | 32.6 | 32.0 |
| listsuffix | 40 | nursery copy | 244.7 | 212.0 | 32.6 | 32.0 |

The saving equals the committed managed heap (one byte of size table per byte of heap) plus the live bitmap (Code and Measured). **Open:** the planes that remain are still 6 to 7 times the committed heap (212 MiB for 32 MiB). I did not split them by plane. The cell `binarytrees` nursery copy shows 208.7 MiB for S2 and 212.0 MiB for S3: S2 committed less at that moment; I did not find out why. **The metrics 7 and 10 in `trace-s3.sh` (`extent_commit`, `live_commit`) are other counters (blocks released, initial marks); ignore those columns in the raw traces.**

### Profile before and after (Measured)

`perf record` (user time, 499 Hz, call graph), 4x heap, nursery-copy mode, one run per build and workload. Self time of the collector module; samples are all user samples of the process (fewer samples = less CPU). Raw: `results/perf-s3/`.

| workload | S2 samples | S3 samples | S2 top functions (self %) | S3 top functions (self %) |
|---|---|---|---|---|
| binarytrees | 2247 | 1667 | `RegisterObjectInClearedSpan` 11.4, `ReclaimNursery` 9.0, `CompleteAllocation` 7.6, `RecordFastAllocation` 5.5, `RetireForwardedSources` 4.6 | `RegisterObjectInClearedSpan` 16.5, `RecordFastAllocation` 7.6, `IsRangeZero` 6.3, `AllocationComplete` 6.2, `LeaseFinalizer` 4.6 |
| requests | 8515 | 6135 | `CompleteAllocation` 9.4, `ReclaimNursery` 8.0, `IsRangeZero` 7.1, `RegisterObjectInClearedSpan` 6.7, `GetObjectExtent` 4.4 | `IsRangeZero` 9.7, `RegisterObjectInClearedSpan` 9.3, `AllocationComplete` 7.8, `LeaseFinalizer` 5.3, `ResolveForwarded` 4.4 |
| listsuffix | 30235 | 25454 | `GetObjectExtent` 12.4, `ProcessTraceObject` 9.1, `NewWork` 6.5, `FixForwardingReaders` 6.3, `Reclaim` 5.2 | `ProcessTraceObject` 11.1, `NewWork` 8.4, `FixForwardingReaders` 8.3, `ResolveForwarded` 7.7, `FinishDyingTraceObject` 5.4 |

`ReclaimNursery`, `GetObjectExtent`, `Reclaim` and `RetireForwardedSources` leave the top lists. `LeaseFinalizer` enters at 4.6% and 5.3% (it is the pre-increment walk over the ValidObject plane, which now also sees stale bits). The percentages of the other functions grow because the total is smaller. The profile is one run per cell; it shows where the time goes, not a precise share.

### Tests (S3)

All on the final tree (`4408ad43f32` plus the docs commit). Linux is WSL Ubuntu 22.04 (clang 14); Windows is MSVC.

| Suite | Linux Release | Linux Debug | Windows Release | Windows Debug |
|---|---|---|---|---|
| Native validation, 28 selectors (the 27 of S2 and the new `--s3-bookkeeping`) | pass | pass (asserts armed) | pass | pass (asserts armed) |
| New selector `--s3-bookkeeping` (19954 checks in Linux Debug) | pass | pass | pass | pass |
| Managed scope-5 fixture: the 84 cells of S2 and 6 new cells of `collectible-cycles` (90 cells, Release and Debug collector) | pass 90 of 90 | pass (the Debug collector runs with asserts armed) | not run | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 (P3.1 642/642, P3.2 380/380, P3.3 357/357, P3.4 57/57) | not run (the scripts build without `_DEBUG`) | not run | not run |
| `clrlxrgc` module build | pass | pass | pass | pass |

- The `--nursery` selector has a check count that varies by run (285, 287, 291). This is timing dependence that was already in S2 (Measured; all counts pass).
- **Positive controls (Measured):** `LXR_CYCLE_GATE_CONTROL` fails 3 checks; `LXR_VALID_CLEAR_CONTROL` fails 7 checks; the S3e fix reverted fails 9 of 19943 checks (the mature array evacuation stops); the S3f fix reverted crashes the selector.
- **Test errors that Windows found (Measured):** a `std::max` macro clash and an uninitialized pointer in the new test (MSVC). Both fixed.
- The first S3 build (before S3e) failed 2 of the 84 managed cells (`dependent`, Release managed build, mature copy). The suites on `4408ad43f32` pass all cells.

### Not measured, open items (S3)

| Item | Label |
|---|---|
| Windows performance of S3 | Not measured |
| Windows managed suite | Not measured |
| Hardening of `FindContainingObject` for addresses that lie in no live object (only the remembered-slot replay is guarded) | Open |
| A young pinned-object-heap object that dies keeps its `Pinned` flag; the pre-increment `LeaseFinalizer` walk can count it as a pin. Benign (a pin only prevents a move). S7 redoes pins. | Open |
| `GetHeapStatistics` is 1.6x to 2.4x slower in a pause (a walk without a size table). S4 makes it incremental. | Measured, S4 |
| `LeaseFinalizer` is now 4.6% to 5.3% of user time in the profile; S6 and S7 remove the walk | Measured, S6 and S7 |
| Heap statistics meaning: dead young objects are not counted any more | Code (changed on purpose) |
| The remaining 212 MiB of native planes for a 32 MiB heap (which plane?) | Open |
| `listsuffix` 8x mature: 33 pauses (S3) against 21 (S2); `binarytrees` 4x mature bimodal RSS | Open |
| Finalizers start later (until the lazy cycle sweep ends); `GC.WaitForPendingFinalizers` at the same time can return early | Inferred (Code) |
| The managed `collectible-cycles` case does not discriminate the gate | Measured |
| The P3.3 intermittent failure of layer 1 | Open (9 of 9 legacy scripts passed in this step) |

## S4: the pause does not touch every block

Status: committed locally on the branch `kkokosa-lxr-s4-background-sweep`, on top of the S3 tip `c665b8887c5`. Nothing is pushed. `lxr`, the layer-1 branch, the S2 branch and the S3 branch are not changed. The accepted port stays at `e8d2d23`.

### Short answers (S4)

1. **The pause no longer visits every block, and it scans no line** (Code). Before S4, `ReleaseEpoch` called `Sweep(nullptr)`. That call looped over every admitted block: a range check of its counts and, for a block that stayed, a scan of all 128 lines. Now the pause loops over the list of blocks that mutators took since the last pause. A nursery block that no object promoted is released in one operation. A block with a promoted object keeps its state.
2. **The work moved to the lazy sweep, it was not dropped** (Code). The lazy sweep counts the free lines of the blocks that need it and makes them `Reusable`. Only flagged blocks are looked at: blocks with a death, blocks that the pause promoted in place, blocks that an allocator took as recycled blocks or for copying, and retired evacuation sources. A block that is not ready gets its flag again.
3. **Measured effect on the pause (RefCount pauses, medians of three traced runs, 4x heap).** `SweepBlocks` was 11.4 to 18.7 ms in `binarytrees` and `requests`, and 2.7 ms in `listsuffix`. It is 0.0 ms now. `ReleaseEpoch` falls from 11.6 to 19.2 ms to 0.5 to 1.4 ms in those two programs. `GetHeapStatistics` was 1.9 to 13.2 ms and is 0.0 ms. The RefCount pause is 45% to 57% shorter in `binarytrees` and `requests` (for example `requests` nursery copy 53.4 ms to 25.2 ms), and 21% shorter in `listsuffix` non-moving (table "Pause times").
4. **Measured effect on the whole run (matrix, 22 cells, three paired repetitions).** S4/S3 wall time: geometric mean 0.881 (range 0.629 to 1.072). Collection time: 0.687. In 15 of 22 cells all three repetitions are faster than S3. In 7 cells the repetitions are on both sides of 1.0: these are inside the noise, and I do not claim a gain there. No cell is slower in all three repetitions. S4 against the accepted build: wall time geometric mean 0.416. Peak RSS S4/S3: 1.005 (range 0.940 to 1.108).
5. **The lazy sweep got longer, and this is visible.** The thread that starts a pause waits for the lazy sweep before it returns (`PostDrain`). That wait is 5.0 ms to 14.7 ms in `requests` non-moving and 3.2 ms to 6.2 ms in `requests` nursery copy. The line scans moved there. The sum of pause and wait is still shorter (`requests` non-moving: 50.0 ms to 34.3 ms), but the gain for a single-threaded program is smaller than the gain of the pause alone. A program with more threads gains more, because the other threads do not wait.
6. **Heap statistics and policy inputs read counters** (Code). Blocks that are not free are counted at every state change of a block. `GetMaturePages` reads this counter. `GetHeapStatistics` reads it too, and adds the free lines of `Reusable` blocks. **The meaning of the two statistics changed on purpose** (section "Heap statistics").
7. **I kept the stopgap for recycled-block searches, and made its token correct** (Code). Blocks now become `Reusable` in the lazy sweep, so a search that failed early in an epoch must not stay valid for the whole epoch. The token also changes when a block is made `Reusable`. I did not build a counter or a queue (section "The stopgap for recycled-block searches").
8. **The 200 ms that no S0 timer covered are attributed** (Measured). `PublishTraceWork`, called in `Resume`, takes a median of 155.8 ms in `listsuffix` nursery copy while a concurrent trace runs. It is inside the stop-the-world time. I did not change it (section "The unattributed time of the listsuffix pauses").
9. **No new failure.** The 39 failed runs of the matrix are the known ones: `listsuffix` 2x (27 runs, all builds) and `requests` 2x nursery and mature (12 runs, S3 and S4), all `Out of memory.`. All checksums are equal.
10. **Tests.** The Linux native suites pass in Release and Debug (29 selectors, one new). The Windows native suites pass in Release and Debug (29 selectors). The 90 managed cells pass with the Release and the Debug collector. The nine older suites pass. Every new test has a control that makes it fail (section "Tests (S4)").

### Commits

| Part | Commit | What |
|---|---|---|
| S4a | `d7f539de93d` | Block counters, release of untouched nursery blocks, lazy recount, acquire notes |
| S4b | `ca896eae778` | The pause releases only allocated blocks; the lazy sweep counts the lines; statistics from counters; allocation retry |
| S4c | `6bd5e3bc694` | Tests and controls (selector `--s4-sweep`) |
| S4d | `ba6f1f758ef` | Two more pause timers (`resume_gap`, `publish_trace`) |
| S4e | `b8fc123586a` | Remove the Debug cross-check of the counters |
| docs | this commit | S4 section, result files and scripts |

### What the pause did, and what it does now (Code)

| Step | Before S4 | After S4 |
|---|---|---|
| Blocks that mutators took | `ClearAllocatedBlocks`: a loop over all blocks with one atomic exchange on a flag per block | A list. `AcquireBlock` adds the block when a mutator takes it (once per block and epoch, because a flag makes the entry unique). The pause reads the list only. |
| Nursery block, no promoted object | `TryReleaseIfRcDead`: lock, read the 4 KiB of counts, clear three planes, store the state | `TryReleaseNursery`: lock, clear three planes, store the state. It does not read the counts. |
| Nursery block, with a promoted object | `TryReleaseIfRcDead` (not dead), then 128 line checks, then `SetReusable` | Nothing. The block is `Unmarked` since `TryPromoteInPlace`. The promotion sets the refresh flag. |
| Mature block, no death | The same two steps, for every block, every pause | Nothing |
| Mature block with a death | As above | The death sets the refresh flag. The lazy sweep does the work. |
| Evacuation source that lost its objects | Released in `Sweep(nullptr)` | Released in the loop over the source flags in `ReleaseEpoch` (it exists already; it runs only in Full and FinalMark pauses of the mature mode). A source that stays is flagged for the lazy sweep. |
| Large nursery objects | `Sweep(nullptr)` | `SweepLargeNursery` (unchanged work) |
| `GetHeapStatistics` | A walk over all counted objects | A read of counters |

**Why a nursery block without a promoted object has no counts (Code and Measured).** A young object gets its first count in `ProcessIncrement`. The same function calls `TryPromoteInPlace` for the block of that object when the block is a nursery block. The call changes the state of the block from `Unallocated` to `Unmarked`. So a block that is still `Unallocated` and nursery at the end of the pause holds no counted object. A copied object has its count in the destination block, and its source stays uncounted. `TryReleaseNursery` asserts this in Debug builds (`IsRangeZero` on the block). The assertion did not fire in any Debug run: the native suites on Linux and Windows, the managed cells with the Debug collector, and the random counter run (about 46000 checks per run).

### The lazy sweep (Code)

For each flagged block the sweep does these steps. The first step takes the flag, so a flag that is set again later is kept.

1. `ReleaseBlockIfDead`: the existing release when the counts of the whole block are zero. A held block (owner set) is flagged again.
2. `RefreshBlock`, for a block that is not dead: if an allocator took the block in this epoch, flag it again, because its young objects have no counts yet. Otherwise find the free lines with the existing hole finder (`TryFindNextHole`).
3. `TrySetReusableLazily`: under the block lock and the release guard, set `Reusable` with the line count. It refuses a block that an allocator holds or took in this epoch (the answer is "flag again").

**Why an unprocessed block cannot be handed out in a wrong state (Code and Measured).** The allocator takes a recycled block only when its state is `Reusable`. A block that the pause promoted is `Unmarked` until step 3. The line count in a `Reusable` block is only a filter: the allocator finds the real holes with the hole finder when it takes the block. So a count that is old, or too optimistic, can cost one wasted acquisition, never a wrong allocation. The test `--s4-sweep` holds the lazy sweep, allocates two blocks of objects, and checks that no object lands in a promoted block and that its state is still `Unmarked`. Control `LXR_S4_EARLY_REUSABLE_CONTROL` (the pause makes a promoted block `Reusable` at once) fails these checks.

**Why the allocator never waits in a way that can deadlock (Code).** The allocator never waits for the sweep. It skips a block that is not `Reusable`. The next pause waits for the sweep in `Drain` before it flips the epoch, exactly as before S4, so the sweep and the flip never overlap (the Debug assertion of the flip would fire otherwise). A failed allocation in `LxrGcHeap::Alloc` now waits for the sweep once and tries again before it asks for a collection. This wait runs under the collection lock, in preemptive mode, with no block lock held, and the sweep needs neither the lock nor the mutator: the same wait already ran at the start of every pressure collection (`UpdatePolicyHints`). The retry is needed because promoted blocks are `Reusable` only after the sweep, and after an InitialMark pause (which has no post-pause wait) the first allocation can come before the sweep ends.

**Why the counting is done in the lazy sweep and not by the allocator at the first touch (Code, Measured; the alternative is not measured).** The allocator picks blocks by state (`Reusable` and enough free lines). If the allocator counted the lines itself, it would have to look at every `Unmarked` block to find holes, and it would count on the critical path of the mutator. The lazy sweep already exists, runs on the collector threads, and the next pause already waits for it. The cost of this choice is measured: `RefreshBlock` is 1.73% of the CPU samples of `requests` (0.23% in `listsuffix`), and the wait after the pause grows by 0 to 10 ms (item 5 above). I did not build the first-touch design, so I have no number for it.

### Heap statistics

| Value | Before S4 | After S4 |
|---|---|---|
| `GetMaturePages` (policy input) | A loop over all blocks: count the blocks whose state is not `Unallocated`, plus mature large pages | A counter in `ImmixBlockManager`, updated at the block transitions that change "is free". **Same number for the same heap state.** The test compares it with the loop at the start of every pause and after every sweep (random runs in three modes). |
| `HeapSizeBytes` of the memory-info API | End of the last live object in each block with a counted object, plus pages of live large objects | Mature blocks times 32 KiB, plus pages of mature large objects. **Larger or equal to the old value.** |
| `FragmentedBytes` of the memory-info API | Dead bytes below the last live object of each block, plus the unused tail pages of large objects | Free lines of `Reusable` blocks times 256, plus the unused tail pages of mature large objects. **A different quantity.** It counts free lines (found by the lazy sweep), not dead bytes inside used lines. A block that the last pause promoted is counted after the lazy sweep has looked at it. |

The memory-info figures are not policy inputs: the policy reads `GetMaturePages`, the used pages and the free pages (all counters). The two figures are published at the end of the pause, and again when the lazy sweep ends (`FinishLazy`), so that the "before" figure of the next collection is complete. I did not find a way to keep the old fragmentation figure without a walk of the paused heap. A walk outside a pause is not safe, because young objects are being initialised. **Not measured:** the real values of `GC.GetGCMemoryInfo` for S3 and S4 on the same program. The managed cells check only that the values are positive.

### The stopgap for recycled-block searches

S5a made `AcquireBlock` skip a search for recycled blocks that had failed in this epoch (token = epoch flip counter). S4 breaks the assumption of that token ("blocks become `Reusable` only in a pause"). A search that fails right after the pause would hide the blocks that the lazy sweep makes `Reusable` a little later. Control `LXR_S4_OLD_TOKEN_CONTROL` shows it: the test fails because the made-`Reusable` block is not reused in the same epoch.

**Decision: keep the stopgap, and let the token also count the blocks made `Reusable`.** This is simple and safe: a failed search stays valid only while no block was made `Reusable` and no epoch flip happened. I did not build a counter or a queue of reusable blocks, for these reasons (Inferred):

- A counter of available blocks must be reset at every epoch flip (all `Reusable` blocks become available again), decreased at every acquisition, and increased at every publication. A mistake in one of the three makes the counter too low, and then the allocator would stop reusing blocks and use fresh blocks: a silent memory regression. The token cannot fail in that direction.
- A queue needs a rule for every block that is taken out and not used (locked, held, taken in this epoch, selected for evacuation). Each such block must come back later. I found four such cases and no simple proof that none is lost.
- With the token, a search that finds nothing is repeated only after a new `Reusable` block exists. The lazy sweep makes such blocks in one burst after the pause, so I expect few repeated searches. I did not count them.

**Open:** the search is still a scan over all blocks when it must run. If the number of blocks is large and `Reusable` blocks are rare, a queue would still be better.

### The unattributed time of the listsuffix pauses (Measured)

S0 left about 200 ms of the stop-the-world time of a `listsuffix` RefCount pause without a timer. Two new timers (S4d) split it: `resume_gap` (end of `ReleaseEpoch` to the engine `Resume` callback) and `publish_trace` (the time of `PublishTraceWork` inside `Resume`). In a run of `listsuffix` 4x nursery copy (57 RefCount pauses with a concurrent trace running, S4d build): stop-the-world 340.2 ms, increments 75.3 ms (wall), prepare trace 68.0 ms, suspend 26.5 ms, root capture 11.2 ms, **`publish_trace` 155.8 ms**, `resume_gap` 0.7 ms. So `PublishTraceWork` is 155.8 ms of the 159.2 ms that the older timers did not cover in this run. The pauses of this program run almost always with a trace active: 56 of 57 (S3) and 61 of 62 (S4) RefCount pauses in the first traced pair. The result is in `results/trace-tables-s4-x4.md` and can be rebuilt with `by-trace-phase.py`.

**Open (not changed):** `PublishTraceWork` could run after the runtime has resumed, if the trace protocol allows it. I did not check that. The timers cost two timestamps per pause.

### Pause times (Measured)

Traced runs, 4x heap, three repetitions per build and cell, interleaved (`trace-s4.sh`, `s4-tables.py`; 54 runs, all finished, all checksums equal). Pooled pauses: n, median and maximum in ms. The accepted build uses the P15 tracer, S3 the P17 tracer, S4 the S4 tracer. Raw: `results/trace-s4/`, `results/trace-tables-s4-x4.md`.

| workload (4x heap) | mode | pause kind | accepted: n, median, max ms | S3: n, median, max ms | S4: n, median, max ms |
|---|---|---|---|---|---|
| binarytrees | non-moving | RefCount | 46, 83.2, 327 | 48, 31.7, 232 | 48, 17.5, 201 |
| binarytrees | non-moving | InitialMark | 3, 252.2, 268 | 3, 198.1, 199 | 3, 175.9, 182 |
| binarytrees | non-moving | FinalMark | 3, 153.5, 177 | 3, 54.0, 88 | 3, 25.2, 53 |
| binarytrees | nursery copy | RefCount | 46, 105.5, 345 | 54, 31.3, 156 | 55, 16.9, 85 |
| requests | non-moving | RefCount | 11, 106.2, 182 | 12, 45.0, 68 | 12, 19.5, 42 |
| requests | non-moving | Full | 155, 340.7, 666 | 156, 246.1, 280 | 162, 199.5, 249 |
| requests | nursery copy | RefCount | 11, 142.4, 282 | 191, 53.4, 106 | 192, 25.2, 78 |
| requests | nursery copy | InitialMark | 3, 220.5, 273 | 9, 103.5, 113 | 9, 76.8, 125 |
| requests | nursery copy | FinalMark | 2, 253.6, 264 | 9, 78.7, 90 | 9, 23.2, 48 |
| listsuffix | non-moving | RefCount | 172, 334.0, 841 | 162, 60.1, 395 | 172, 47.2, 405 |
| listsuffix | non-moving | InitialMark | 4, 499.6, 520 | 4, 422.4, 430 | 3, 425.1, 475 |
| listsuffix | nursery copy | RefCount | 189, 560.3, 1141 | 160, 324.6, 492 | 171, 64.3, 455 |

The InitialMark and FinalMark counts differ between builds, because the schedule depends on timing; rows with 1 to 6 pauses are not statistics. The `listsuffix` nursery-copy row is **not a comparison**: this cell has a fast and a slow mode (section "Results"), and in this traced set S3 ran in the slow mode (324.6 ms median) and S4 in the fast mode (64.3 ms). In the first traced pair the modes were the other way round (S3 368.9 ms, S4 425.8 ms).

Phases that S4 changes, RefCount pauses (median ms; the phases overlap, `release_epoch` contains `reclaim_nursery` and the large-nursery sweep):

| workload | mode | build | stw pause | release_epoch | reclaim_nursery (release of nursery blocks) | sweep_blocks | scan_heap_statistics | post_drain |
|---|---|---|---|---|---|---|---|---|
| binarytrees | non-moving | S3 / S4 | 31.73 / 17.51 | 12.93 / 0.53 | 0.01 / 0.52 | 12.92 / 0.00 | 1.86 / 0.00 | 1.26 / 1.03 |
| binarytrees | nursery copy | S3 / S4 | 31.27 / 16.94 | 11.61 / 0.53 | 0.01 / 0.43 | 11.42 / 0.00 | 2.60 / 0.00 | 1.33 / 1.54 |
| requests | non-moving | S3 / S4 | 45.04 / 19.53 | 14.17 / 0.61 | 0.01 / 0.61 | 14.16 / 0.00 | 10.42 / 0.00 | 5.00 / 14.73 |
| requests | nursery copy | S3 / S4 | 53.44 / 25.21 | 19.24 / 1.35 | 0.01 / 0.80 | 18.72 / 0.00 | 9.57 / 0.00 | 3.17 / 6.19 |
| listsuffix | non-moving | S3 / S4 | 60.08 / 47.15 | 2.75 / 0.20 | 0.01 / 0.19 | 2.74 / 0.00 | 10.77 / 0.00 | 25.34 / 28.79 |

In S3 the time of the block loop was in `sweep_blocks`. In S4 the release of nursery blocks is the only block work, and it is in `reclaim_nursery` (0.2 to 0.8 ms). Share of these phases in all RefCount pause time (S3 → S4): `binarytrees` non-moving 29.1% + 4.9% → 1.9%; `requests` non-moving 30.1% + 20.8% → 2.5%; `requests` nursery copy 34.4% + 17.1% → 5.4% (`release_epoch` including `reclaim_nursery`).

### Profile before and after (Measured)

`perf record` (user time, 499 Hz, call graph), 4x heap, nursery-copy mode, one run per build and workload. Inclusive share of all user samples. Raw: `results/perf-s4/`, `results/perf-shares-s4.md`.

| workload | samples S3 → S4 | `Sweep` S3 → S4 | `ReleaseEpoch` S3 → S4 | `GetHeapStatistics` S3 → S4 | `RefreshBlock` (S4) | `TryFindNextHole` S3 → S4 |
|---|---|---|---|---|---|---|
| binarytrees | 1661 → 1488 | 6.98% → 0.54% | 7.34% → 0.74% | 1.93% → 0 | 0 | 0.30% → 0.13% |
| requests | 6149 → 5246 | 12.64% → 2.94% | 12.16% → 1.01% | 5.40% → 0 | 1.73% | 2.99% → 3.35% |
| listsuffix | 25853 → 26874 | 0.57% → 0.36% | 0.55% → 0.16% | 1.49% → 0 | 0.23% | 0.55% → 0.53% |

What remains of `Sweep` in S4 is the lazy sweep (`RefreshBlock`, `ReleaseBlockIfDead`) and `SweepCycles` (0.7% in `requests`). The `listsuffix` run has 4% more samples in S4: this program has a fast and a slow mode (see below), and one run per build is not enough to compare them.

### Results (Measured)

Matrix: 3 workloads x 2x, 4x, 8x heap x 3 modes x 3 repetitions, three builds in one interleaved run (`matrix-parallel.sh m-s4`, 243 runs): accepted `e8d2d23` (`acc`), S3 tip (`s3`, the collector of `c665b8887c5`) and S4 tip (`s4`, the collector of `b8fc123586a`). Same method as in "How it was measured". Nothing else ran on the machine. Raw runs and tables: `results/matrix-s4-acc-s3-s4.jsonl`, `results/summary-s4.md`, `results/wall-table-s4.md`, `results/compare-*-s4-*.md`. Checksums are equal in all runs (3 checksums).

Wall time, collection time (`gc_ms`), pauses and peak RSS (medians of 3; the repetitions are the paired S4/S3 wall-time ratios; a cell with a ratio on both sides of 1.0 is inside the noise):

| workload | heap x | mode | wall ms acc / S3 / S4 | S4 / S3 (repetitions) | gc ms S3 / S4 | pauses S3 / S4 | peak RSS MB acc / S3 / S4 |
|---|---|---|---|---|---|---|---|
| binarytrees | 2 | nonmoving | 5847 / 3438 / 3151 | 0.92 (0.92, 0.88, 0.96) | 1477 / 1081 | 68 / 68 | 146 / 134 / 134 |
| binarytrees | 2 | nursery | 6828 / 4491 / 3784 | 0.84 (0.98, 0.84, 0.68) | 2552 / 1793 | 86 / 75 | 147 / 143 / 144 |
| binarytrees | 2 | mature | 7074 / 4405 / 3992 | 0.91 (1.14, 0.85, 0.86) | 2572 / 2065 | 81 / 84 | 147 / 141 / 143 |
| binarytrees | 4 | nonmoving | 4815 / 2724 / 2417 | 0.89 (0.89, 0.88, 0.97) | 1062 / 738 | 18 / 18 | 229 / 192 / 189 |
| binarytrees | 4 | nursery | 5327 / 2517 / 2350 | 0.93 (1.05, 0.95, 0.84) | 989 / 723 | 19 / 20 | 229 / 182 / 182 |
| binarytrees | 4 | mature | 5443 / 2712 / 2282 | 0.84 (0.86, 0.80, 0.81) | 1075 / 691 | 19 / 19 | 227 / 185 / 181 |
| binarytrees | 8 | nonmoving | 5125 / 2123 / 1895 | 0.89 (0.88, 0.92, 0.89) | 486 / 261 | 7 / 7 | 314 / 243 / 243 |
| binarytrees | 8 | nursery | 5516 / 2101 / 1942 | 0.92 (0.91, 0.92, 0.90) | 531 / 336 | 8 / 8 | 314 / 188 / 188 |
| binarytrees | 8 | mature | 6445 / 2140 / 1934 | 0.90 (0.85, 0.93, 0.90) | 540 / 340 | 8 / 8 | 314 / 188 / 188 |
| requests | 2 | nonmoving | 40647 / 23457 / 18693 | 0.80 (0.81, 0.80, 0.75) | 11488 / 5400 | 532 / 516 | 184 / 149 / 149 |
| requests | 4 | nonmoving | 30873 / 22965 / 20758 | 0.90 (0.85, 0.96, 0.90) | 13267 / 10910 | 57 / 57 | 368 / 295 / 295 |
| requests | 4 | nursery | 34840 / 10277 / 8448 | 0.82 (0.82, 0.96, 0.82) | 4069 / 2104 | 69 / 69 | 368 / 281 / 281 |
| requests | 4 | mature | 39397 / 10402 / 8575 | 0.82 (0.82, 0.84, 0.81) | 4535 / 2546 | 67 / 66 | 367 / 303 / 305 |
| requests | 8 | nonmoving | 26857 / 14819 / 14919 | 1.01 (1.04, 0.93, 0.94) | 6710 / 6361 | 22 / 22 | 658 / 509 / 508 |
| requests | 8 | nursery | 32561 / 9061 / 7872 | 0.87 (0.86, 0.87, 0.82) | 3167 / 1693 | 25 / 25 | 658 / 508 / 508 |
| requests | 8 | mature | 33591 / 9073 / 7477 | 0.82 (0.81, 0.74, 1.04) | 3305 / 1792 | 26 / 26 | 658 / 502 / 500 |
| listsuffix | 4 | nonmoving | 20580 / 12216 / 11694 | 0.96 (0.93, 0.98, 0.96) | 6022 / 5125 | 52 / 52 | 303 / 268 / 267 |
| listsuffix | 4 | nursery | 50199 / 13395 / 14358 | 1.07 (1.98, 0.90, 0.86) | 6953 / 7282 | 53 / 54 | 303 / 238 / 258 |
| listsuffix | 4 | mature | 51609 / 26304 / 16535 | 0.63 (0.63, 0.66, 0.71) | 19184 / 9617 | 65 / 64 | 300 / 257 / 242 |
| listsuffix | 8 | nonmoving | 21815 / 16545 / 17151 | 1.04 (1.04, 1.06, 0.98) | 10685 / 10819 | 15 / 15 | 491 / 419 / 464 |
| listsuffix | 8 | nursery | 28447 / 17021 / 16853 | 0.99 (0.95, 0.97, 1.03) | 11732 / 11534 | 19 / 17 | 528 / 454 / 454 |
| listsuffix | 8 | mature | 29886 / 23970 / 17366 | 0.72 (0.65, 0.85, 0.65) | 17941 / 11734 | 35 / 19 | 528 / 447 / 455 |

| Comparison (22 cells) | Wall time | Collection time (`gc_ms`) | Peak RSS |
|---|---|---|---|
| S4 / S3, geometric mean (range) | 0.881 (0.629 to 1.072) | 0.687 (0.470 to 1.047) | 1.005 (0.940 to 1.108) |
| S4 / accepted, geometric mean (range) | 0.416 (0.218 to 0.786) | not computed | not computed |

- **Faster in all three repetitions (15 cells):** `binarytrees` 2x non-moving and nursery, 4x non-moving and mature, all three 8x cells; `requests` 2x non-moving, 4x all three modes, 8x nursery; `listsuffix` 4x non-moving, 4x mature, 8x mature.
- **Inside the noise (7 cells, not a gain and not a loss):** `binarytrees` 2x mature and 4x nursery; `requests` 8x non-moving and 8x mature; `listsuffix` 4x nursery, 8x non-moving and 8x nursery.
- **Bimodal cells (Open).** `listsuffix` 4x nursery has a fast mode (10 to 14 s) and a slow mode (26 to 31 s). In this matrix S4 repetition 0 was slow (26.4 s) and the others fast. I ran the cell eight more times per build in one pinned stream (`results/extra-listsuffix-4x-nursery-s3-s4.jsonl`): S3 27.2 to 31.2 s (median 28.5 s), S4 10.4 to 28.8 s (median 26.6 s, one run fast, one at 18.7 s). So the slow mode is not new in S4. `listsuffix` 4x mature and 8x mature (S3 26.3 s and 24.0 s against S4 16.5 s and 17.4 s) are probably the same effect, and I do not claim that gain. The cause of the two modes is Open.
- **Peak RSS.** The geometric mean is 1.005. One cell is higher in all three repetitions: `listsuffix` 8x non-moving, 464 MB against 419 MB (+10% to +11%). S4 makes 10 full collections there, S3 makes 12, and the S3 matrix measured 458 MB for the same cell (see the S3 section), so the cell moves between two levels. Cause Open.
- **Failures:** 39 of 243 runs, all known: `listsuffix` 2x, all modes and builds (27 runs), and `requests` 2x nursery and mature, S3 and S4 (12 runs), all `Out of memory.`. The accepted build finishes `requests` 2x nursery and mature (this is the cliff-edge case of the follow-up, not changed). No new failure.

### Tests (S4)

| Suite | Linux Release | Linux Debug | Windows Release | Windows Debug |
|---|---|---|---|---|
| Native validation, 29 selectors (the 28 of S3 and the new `--s4-sweep`) | pass | pass (asserts armed) | pass | pass (asserts armed) |
| New selector `--s4-sweep` (46100 checks) | pass | pass | pass | pass |
| Managed scope-5 fixture, 90 cells (Release and Debug collector) | pass 90 of 90 | pass (the Debug collector runs with asserts armed) | not run | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 (P3.1 642/642, P3.2 380/380, P3.3 357/357, P3.4 57/57) | not run (the scripts build without `_DEBUG`) | not run | not run |
| `clrlxrgc` module build | pass | pass | pass | pass |

All on the final tree (`b8fc123586a` plus this documentation commit). The Debug check of the counters (in the tree of the commits S4b to S4d) ran in the Debug native suites on Linux and Windows and in the managed cells with the Debug collector; it never fired. Then I removed it (S4e) and ran all suites again on the final tree. The Release native compile in `suite-chain.sh` was killed twice by a hangup of the shell that had started it. I ran the same step again by hand, and it passed.

New tests (selector `--s4-sweep`) and the control of each:

| Test | What it shows | Control (makes the test fail) | Failed checks with the control |
|---|---|---|---|
| Nursery block with and without a promoted object | The pause released exactly the block without a promoted object (counter and metric). The two promoted blocks keep `Unmarked`. The counter holds two non-free blocks. | `LXR_S4_NO_NURSERY_RELEASE_CONTROL`: the pause releases no nursery block | 1 |
| In-place promoted block is `Reusable` after the sweep and is reused | After the sweep both blocks are `Reusable` with 1 to 4 unavailable lines, and the counters equal a walk. An allocation in the same epoch uses them. | `LXR_S4_NO_REFRESH_CONTROL`: the sweep does not set `Reusable` | 4 |
| No promoted block is handed out unprocessed | With the sweep held, 2800 allocations do not use the promoted blocks, and their state stays `Unmarked`. | `LXR_S4_EARLY_REUSABLE_CONTROL`: the pause sets `Reusable` at once | 3 |
| Block counters equal the old loop after random operations | At the start of every pause and after every lazy sweep, the counters equal a walk over all blocks, in three modes (non-moving, nursery copy, mature evacuation), with large objects. `GetMaturePages` equals the heap size figure. | `LXR_S4_COUNTER_CONTROL`: a promotion does not update the counter | 97 |
| A pause that starts while the sweep runs | The sweep is held after it has started. A pause that is requested then does not start (phase stays `Mutator`, epoch count unchanged for 300 ms). After the release it runs, and when it starts the sweep is finished, the promoted block is `Reusable` and the counters hold. | `LXR_S4_NO_SWEEP_WAIT_CONTROL`: the pause does not wait for the sweep | 4 |
| A block that the sweep made `Reusable` is found in the same epoch | Part of the second test: a search that failed while the sweep was held does not hide the blocks that it makes `Reusable`. | `LXR_S4_OLD_TOKEN_CONTROL`: the token does not count `Reusable` publications | 1 |

The controls were built in Release (`controls-s4.sh`; the Debug build asserts at some of them). The output is in `results/controls-s4-release.txt`. The S3 heap-statistics check of the random selector now compares with the block-state walk.

### Not measured, open items (S4)

| Item | Label |
|---|---|
| Windows performance of S4 | Not measured |
| Windows managed suite | Not measured |
| The first-touch alternative (the allocator counts the lines) | Not built, not measured |
| Real values of `GC.GetGCMemoryInfo` (`HeapSizeBytes`, `FragmentedBytes`) before and after S4 | Not measured; the meaning changed (section "Heap statistics") |
| `SelectMatureCandidates` still walks every block and scans its 128 lines, in the InitialMark and Full pauses of the mature mode. It is 16.7 ms of a 145 ms InitialMark pause in `requests` 4x mature (median of 3), 4.1 ms in `binarytrees`. The loop over the evacuation-source flags in `ReleaseEpoch` is also a walk over all blocks (byte loads). Both belong to the mature copy, which this step did not change. | Measured, Open |
| `PublishTraceWork` takes 155.8 ms of a `listsuffix` pause while a trace runs; it could move after the resume | Measured, Open |
| The recycled-block search is still a scan over all blocks when it runs | Open |
| Why `listsuffix` has a fast and a slow mode (4x nursery, 4x and 8x mature) | Open |
| `listsuffix` 8x non-moving: peak RSS +10% against S3 in all three repetitions, with fewer full collections (10 against 12) | Measured, cause Open |
| The wait after the pause (`PostDrain`) is longer by up to 10 ms in `requests`. A parallel sweep, or a smaller refresh set, could shorten it. | Measured, Open |
| The pause now has no per-block work. The remaining pause time is root capture (the finalizer walk, 11 to 18 ms), increments and, in tracing modes, the trace work. These are S6, S7 and later. | Code |
| A block counter drift (a transition that bypasses `NoteRawStateChange`) would show in `BlockCountersMatchWalk`; no test found one. The Debug check was removed after it never fired. | Code |

## S5b: to-space beyond the admitted heap (no hard limit), with give-back

Status: committed locally on the branch `kkokosa-lxr-s5b-to-space-overshoot`, on top of the S4 tip `de6434c2861`. Nothing is pushed. `lxr`, the layer-1 branch, S2, the large-object fix, S3 and S4 are not changed. The accepted port stays at `e8d2d23`. Decision D2 of P16 applies: this works only when no hard limit is set. With a hard limit, the S5a byte reserve is exactly as before (Measured: the hard-limit matrix is inside the noise, and the native tests check that the path is unchanged).

### Short answers (S5b)

1. **"No hard limit" is a configuration that already exists** (Code). It means: `LXRManagedHeapHardLimitMB` is not set, `LXRLegacyCapacity` is off, and the Immix reservation (`LXRImmixReserveMB`) is larger than the initial Immix size (`LXRImmixInitialMB`). The hard limit is then the size of the whole reservation, and the admitted capacity can grow into the reserved range. I added one setting, `LXROvershootToSpace` (default on). It only switches the new behavior off. See "What 'no hard limit' means".
2. **At the start of a pause that copies, the pause admits the missing blocks** (Code). The need is the S5a prediction (clean nursery bytes x measured survival x safety factor). If it is more than the free Immix pages, the pause admits the missing blocks from the reserved range. It uses the existing `GrowSuspended` and `AdmitRange` code, with a new rule: no zero-fill of the new storage, because nothing reads it before the copy writes it. This lowered the median cost of the admission from 1 to 20 ms to 67 to 91 microseconds (Measured, pilots).
3. **At the end of the pause, the pause gives back the free tail** (Code). This is new: before S5b nothing lowered the admitted capacity. Only the blocks above the highest block that is in use can go. The pause releases the OS memory, the flag storage and the side-metadata planes of that range, and then lowers the capacity. This happens before the epoch flip. Section "Give-back and its order" has the order and the guards.
4. **Give-back works, but the capacity does not fall back to the initial size** (Measured). The give-back succeeds in 94% of the attempts in `binarytrees` and `requests` (100% and 94%) and in 80% to 84% in `listsuffix`. "Complete" (back to the initial capacity) happened in 0 of 6 cells. Survivors stay in the blocks that were borrowed, so the capacity after a pause is 4 to 87 MiB above the initial size. The median borrow in one pause is 3 to 14 MiB (maximum 64 MiB).
5. **A new placement rule was needed** (Measured, Code). Without it, copied survivors were spread over the borrowed range and mutators used the free tail as nursery. The capacity then rose without limit (peak 242 to 301 MiB from a 32 MiB start in `listsuffix`). Now mutators take fresh blocks only below the initial capacity, and the copy takes the lowest free block (section "Placement").
6. **A bug that only showed under load was found and fixed** (Measured). 1 of 216 matrix runs failed. A repeat run of the cell showed 28 failures in 180 runs of the S5b build (15%); the S4 build, the accepted build and the S5b build with overshoot off had 0 in 180 each. The cause: queued trace work named an object of a returned block, and the ownership check failed. The fix is one check (section "The failure that only S5b had"). After the fix, 0 failures in 480 runs of the fixed tree (240 with a diagnostic build, 240 with the final build), and 0 in the 54 S5b runs of the final matrix.
7. **Hard-limit matrix: no change beyond the noise** (Measured). S4 against S5b, 22 cells, 3 paired repetitions: wall time geometric mean 0.987 (range 0.867 to 1.293), collection time 1.022, peak RSS 1.026. The 9 non-moving cells use the same code in both builds; their ratios are 0.87 to 1.03. That is the noise level of the machine for this matrix. The 30 failed runs are the known ones (`requests` 2x nursery and mature, `listsuffix` 2x all modes: `Out of memory.`), the same in both builds.
8. **No-limit matrix: fewer collections, shorter total run, but more memory** (Measured). S5b against S4 (18 cells): wall time geometric mean 0.727 (range 0.129 to 1.202), collection time 0.641, peak RSS 1.325 (range 0.694 to 2.994). In 13 cells all repetitions are faster and in 5 they are mixed. In no cell are all repetitions slower. `listsuffix` uses 2 to 3 times more RSS (499 to 575 MB against 162 to 259 MB).
9. **`requests` at 2x changes in this mode** (Measured): S4 needs 88.6 s (nursery) and 44.6 s (mature) with 4281 and 1943 collections. S5b needs 11.4 s and 10.4 s with 136 and 108 collections. The reason is **not** only the removed reserve (Inferred): with no hard limit, the retained borrowed capacity makes the heap larger than the "2x" that the cell names. The comparison is not at equal memory. The RSS is 217 and 240 MB against 170 MB. With a hard limit these cells still do not finish, as before.
10. **Tests.** The Linux and Windows native suites pass in Release and Debug (30 selectors, one new: `--s5b-overshoot`). The 90 managed cells pass with the Release and the Debug collector. The nine older suites pass. The 12 controls make the new selector fail. Windows performance is not measured.

### Commits

| Part | Commit | What |
|---|---|---|
| S5b-a | `7014e537207` | Decommit of planes, flag storage and side metadata; shrink of the Immix arena (building blocks and their tests) |
| S5b-b | `805544a4934` | Borrow at the start of the pause, give-back before the epoch flip, trigger without reserve, metrics 410 to 429 |
| S5b-c | `aac2e8f16fa` | Native tests (selector `--s5b-overshoot`), controls, matrix scripts for no hard limit |
| S5b-d | `795dea990a5` | Placement of fresh blocks, no zero-fill of borrowed storage, `VirtualDecommit` in the test platform |
| S5b-e | `8090810baae` | Queued trace and decrement work tolerates a returned block (the bug of short answer 6), test and control |
| docs | this commit | S5b section, result files, scripts |

### What "no hard limit" means (Code)

| Setting | Default | Role |
|---|---|---|
| `LXRImmixInitialMB`, `LXRLOSInitialMB` | 8 and 16 MiB | Capacity that is admitted at start |
| `LXRImmixReserveMB`, `LXRLOSReserveMB` | 128 and 256 MiB | Address range that is reserved. Larger than the initial size. |
| `LXRManagedHeapHardLimitMB` | not set | If not set, the hard limit is the whole reservation (`m_hard_limit = reservation`). If set, it is the ceiling, and it must be at most the reservation. |
| `LXRLegacyCapacity` | off | If on, the reservation is set equal to the initial size. Nothing can be admitted. |
| `GCHeapHardLimit` and the `GCHeapHardLimit*` family | not set | Rejected as `Unsupported` at start, with a message that names `LXRManagedHeapHardLimitMB`. They include bookkeeping, so they are not a managed-capacity ceiling. |
| `LXRNativePlaneLimitMB` | 4096 | A limit on the committed native planes. `AdmitRange` checks it before it commits. |
| `LXROvershootToSpace` (new) | on | Off switch only |

The engine enables overshoot when all of these are true: the setting is on, a copy mode is on (`LXREvacuateNursery` or `LXREvacuateMature`), the hard limit equals the reservation (no limit set), and the Immix reservation is larger than the initial Immix size. In all other cases the engine behaves as in S4 (the native test `ValidateOvershootHardLimit` runs three kinds of such cases: hard limit, legacy capacity, switch off). No new environment setting was needed to get a reservation larger than the admitted capacity. The matrix sets the initial size with `LXRImmixInitialMB` and `LXRLOSInitialMB` and the reservations to 1024 and 256 MiB.

The "floor" is the capacity at start, plus the regular growth of `GrowSuspended` (after an allocation failure). Give-back never goes below the floor. In overshoot mode, regular growth doubles from the floor, not from the borrowed capacity.

### Borrow (Code)

1. `LxrGcHeap::Suspend` asks the engine for `m_pause_reserve_pages` (the S5a prediction) with `RequestBorrow`, only when the nursery copy is on and overshoot is on.
2. `LxrReferenceCounting::Suspend` calls `ResetCopyCursorQuiescent`, runs the regular growth if a mutator asked for it, and then `BorrowSuspended`. If the free Immix pages are fewer than the request, `AdmitSuspended` admits the difference, rounded up to blocks.
3. The admitted size is limited by the end of the Immix reservation, by `hard limit - admitted bytes`, and by `canAdmitRange` (the native plane limit). If one limit stops the admission, the engine takes as many blocks as fit and counts a refusal; the copy then falls back to its existing failure path.
4. `AdmitSuspended` commits the flag storage and the side-metadata planes of the range (`CommitPlanes`) and adds the range to the owned data ranges. The borrow does not write zeros to the new storage (`initialize=false`): the pages are new, so they read as zero.

### Give-back and its order (Code)

`GiveBackSuspended` runs in `ReleaseEpoch` after the loop over the evacuation sources and **before `ReleaseGcPause`**. The epoch is still the pause epoch. No mutator runs, and the lazy sweep is not running: a pause waits for the sweep before it starts (S4).

1. Start at the top block of the capacity and go down to the floor. A block is free if `IsFreeBlock` is true: state `Unallocated` and no owner. If a block is a flagged candidate, `ReleaseBlockIfDead` runs first (this releases an empty copy block; it needs the pause epoch).
2. Stop at the first block that cannot be freed. Only a tail can go, so a block in use stops the walk (a control and a test check this).
3. `ReturnTailSuspended`: clear the flags of the range, `ShrinkQuiescent` (the allocator lowers its capacity and drops the blocks above it), store the lower capacity, `releaseRange` (flag storage), `DecommitPlanes`, `DecommitDataRange` (side metadata; bit-level clearing of shared bytes, whole pages decommitted) and `VirtualDecommit` of the heap range.
4. The epoch flip comes after. A mutator that resumes sees only the new capacity. No mutator can hold or allocate into the returned range, because (a) the range held only free blocks, and (b) mutators take fresh blocks only below the floor (Placement).
5. The log streams (the barrier log) never shrink. The new `desired > capacity` guards keep the log code from growing a stream that is already large enough.

Guards that I found necessary (Code and Measured):

- The BlockLog plane has 1 bit per block. Bytes are shared between blocks, so a range that starts or ends inside a byte needs bit-level clearing, not a page release.
- Touching Immix and large-object data ranges are merged into one range. `RemoveDataRange` splits a merged range.
- The top metadata page must be released when no admitted data follows it. A first version kept it. The test "the returned range is not owned" found this.
- `LxrVirtualArray::Decommit` tracks at most two committed ranges and resets the edge elements of a partial page.

### Placement (Code, Measured)

With overshoot on, the allocator has three new rules: (a) mutators take fresh blocks only below the floor, (b) the copy takes the lowest free block (`m_next_copy_block`, reset at every pause), (c) a failed mutator allocation triggers a collection through the existing `heapFull` path. Control `LXR_S5B_NO_PLACEMENT_CONTROL` removes the rules.

Why (Measured, pilot runs): without placement, the round-robin copy cursor put survivors into the borrowed tail, and the mutators then used the retained free tail as nursery. The peak capacity of `listsuffix` was 242 to 301 MiB from a floor of 32 MiB. With placement it is 107 to 126 MiB in the pilots, and 113 to 133 MiB in the final traced runs.

### The trigger (Code)

With overshoot, `CopyReserveExhausted` does not use the byte reserve: the collection starts when the free Immix pages are at most `OvershootFreePages` (8 pages, one block). The hard-limit path still uses the S5a reserve. Control `LXR_S5B_RESERVE_TRIGGER_CONTROL` restores the reserve for the overshoot mode.

### The failure that only S5b had (Measured, Code, Inferred)

| Item | Result |
|---|---|
| Symptom | `Fatal error. E_FAIL`, exit code -6, `binarytrees` 2x, nursery and mature, no hard limit |
| Rate | 1 of 216 matrix runs. Plain loops of 460 runs did not show it. A repeat of the matrix cell (3 parallel pinned streams, 4 builds) showed 28 of 180 S5b runs. 0 of 180 for each of: S4, accepted, S5b with overshoot off. |
| Diagnostic build | Engine failure 9 (`UnsupportedLifetime`), phase `Mutator`. The runtime messages were "epoch publication did not precede the runtime barrier reset" (18), "allocation-complete metadata publication failed" (6), "could not publish its initialized Immix allocation span" (2). They follow from the engine failure. |
| Failing site | `ProcessTraceObject`: the ownership check (`OwnsAddress`) failed for a reference in queued concurrent trace work. Found with a line number in the failure print. |
| Cause (Code) | A trace packet is queued when an object is found. A later pause can free the block of that object (the object is dead) and give the block back. The capacity is lower, so `IsImmix` is false for the address, and the engine took this for an object that is not owned. Before S5b the capacity only grew, so a stale address always passed the ownership check, and the liveness check (count is zero) skipped the object. |
| Fix (S5b-e) | `IsReturnedImmix(address)`: the address is inside the Immix reservation and not below the capacity (overshoot mode only). `ProcessTraceObject` and `ProcessDecrements` skip such a reference: the object is dead. The code does not read the returned memory. `ResolveForwarded` is already guarded by `IsImmix`. |
| Result | 0 of 240 S5b runs with the diagnostic build plus the fix, 0 of 240 with the final build (same cell, same load); 0 failures in the 54 S5b runs of the final no-limit matrix. Raw counts: `results/intermittent-failure-s5b.txt`. |
| Test and control | `ProcessQueuedTraceReferenceForTest` (new test hook) sends a reference into the returned range. Control `LXR_S5B_NO_STALE_TOLERANCE_CONTROL` makes the engine fail on it; the selector then reports 5 failed checks. |

Limits of the fix (Inferred, Open): the decrement path has the same guard, but I did not see it fail in a run, so that guard is by reading (Code), not by a failing run. The OS decommit of the heap range uses `GCToOSInterface::VirtualDecommit`. I did not read the Unix and Windows code to confirm what a stale read of a returned page does (Open); if it faults, any other path that reads a stale address would crash. The matrices (about 700 S5b runs) and the 12 controls show no such crash. The other `UnsupportedLifetime` sites (barrier old target, increment, remembered slot, allocation complete) were instrumented and did not fire in the failing runs.

### Results: hard-limit matrix (Measured; S4 tip against S5b tip, 3 repetitions, medians, wall time in ms)

Source: `results/wall-table-s5b-hl.md`, `results/matrix-s5b-hl-s4-s5b.jsonl`. Builds were run interleaved in a shuffled order, 3 repetitions in 3 pinned parallel streams, timeout 180 s, hard limit set by the matrix script (as in the S4 matrix).

| Workload | Heap | Mode | S4 | S5b | S5b / S4 (repetitions) | RSS MB S4 / S5b |
|---|---|---|---|---|---|---|
| binarytrees | 2x | non-moving | 3835 | 3339 | 0.87 (0.87, 0.85, 1.06) | 133 / 134 |
| binarytrees | 2x | nursery | 4339 | 4144 | 0.96 (0.94, 0.99, 0.88) | 144 / 145 |
| binarytrees | 2x | mature | 3892 | 4248 | 1.09 (1.23, 0.89, 1.05) | 143 / 145 |
| binarytrees | 4x | non-moving | 2777 | 2859 | 1.03 (0.96, 1.14, 1.12) | 190 / 196 |
| binarytrees | 4x | nursery | 2188 | 2829 | 1.29 (1.32, 0.88, 1.29) | 111 / 186 |
| binarytrees | 4x | mature | 2493 | 2609 | 1.05 (0.86, 1.12, 1.11) | 196 / 190 |
| binarytrees | 8x | non-moving | 2232 | 2186 | 0.98 (1.08, 1.08, 0.86) | 243 / 242 |
| binarytrees | 8x | nursery | 2144 | 2120 | 0.99 (0.98, 0.82, 1.05) | 188 / 188 |
| binarytrees | 8x | mature | 2541 | 2219 | 0.87 (0.83, 1.14, 0.74) | 188 / 188 |
| requests | 2x | non-moving | 19142 | 19601 | 1.02 (1.01, 1.01, 1.03) | 150 / 149 |
| requests | 2x | nursery, mature | fail | fail | known failure, both builds | |
| requests | 4x | non-moving | 21927 | 21987 | 1.00 (1.04, 0.99, 0.97) | 294 / 296 |
| requests | 4x | nursery | 8877 | 8787 | 0.99 (1.01, 0.94, 1.00) | 280 / 283 |
| requests | 4x | mature | 8962 | 9140 | 1.02 (1.09, 1.02, 1.00) | 302 / 306 |
| requests | 8x | non-moving | 14210 | 14590 | 1.03 (1.08, 1.00, 1.04) | 509 / 508 |
| requests | 8x | nursery | 8456 | 7855 | 0.93 (0.99, 0.87, 0.98) | 508 / 508 |
| requests | 8x | mature | 8369 | 7559 | 0.90 (1.01, 0.85, 0.90) | 501 / 501 |
| listsuffix | 2x | all modes | fail | fail | known failure, both builds | |
| listsuffix | 4x | non-moving | 12715 | 12442 | 0.98 (0.89, 1.03, 0.94) | 272 / 271 |
| listsuffix | 4x | nursery | 14949 | 14928 | 1.00 (1.30, 1.03, 0.93) | 248 / 249 |
| listsuffix | 4x | mature | 36407 | 34438 | 0.95 (0.95, 0.92, 0.65) | 263 / 264 |
| listsuffix | 8x | non-moving | 18864 | 19344 | 1.03 (1.11, 0.99, 1.03) | 460 / 462 |
| listsuffix | 8x | nursery | 19638 | 17020 | 0.87 (0.93, 0.84, 0.96) | 457 / 459 |
| listsuffix | 8x | mature | 21731 | 21007 | 0.97 (0.98, 1.00, 0.97) | 453 / 453 |

What this shows (Measured; the label for each claim is "within noise" unless the text says more):

- Geometric means of S5b / S4: wall 0.987, collection time 1.022, peak RSS 1.026. All repetitions faster in 5 cells, all slower in 2 cells (`requests` 2x non-moving 1.02, `requests` 8x non-moving 1.03), mixed in 15. The two "all slower" cells are non-moving cells, where the engine runs the same code in both builds, so I read them as noise. I claim no speed-up and no slow-down here.
- `binarytrees` 4x nursery (1.29; RSS 111 against 186 MB) and `listsuffix` 4x mature (S4 36 s, S5b 34 s) are the cells that have two modes in other matrices (fast and slow). I do not read them as an effect of S5b. In the hard-limit matrix the S5b build has the S4 code path (overshoot is off by the hard limit); the native test `ValidateOvershootHardLimit` checks this.
- The 30 failed runs are 15 of each build, all `Out of memory.`, the same cells as before: `requests` 2x nursery and mature, `listsuffix` 2x all modes.

### Results: no hard limit (Measured; accepted, S4, S5b with overshoot off, S5b; 3 repetitions, medians)

Source: `results/wall-table-s5b-nl.md`, `results/matrix-s5b-nl-acc-s4-off-s5b.jsonl`. Reservations 1024 MiB (Immix) and 256 MiB (large objects). The initial admitted size is 2x, 4x and 8x of the minimum heap (the same sizes as the other matrices). The accepted and the S4 builds ran with the same settings. They can run in this configuration: they admit blocks by growth after an allocation failure, but they have no borrow and no give-back. "Off" is the S5b binary with `DOTNET_LXROvershootToSpace=0`. Wall time in ms; collections as total (full collections are 0 in all S4 and S5b cells; the accepted build has full collections in `requests` 4x and 8x).

| Workload | Heap | Mode | Accepted | S4 | Off | S5b | S5b / S4 (repetitions) | Collections S4 / S5b | RSS MB S4 / S5b |
|---|---|---|---|---|---|---|---|---|---|
| binarytrees | 2x | nursery | 6663 | 3838 | 3833 | 3448 | 0.90 (0.71, 0.90, 0.93) | 73 / 54 | 145 / 140 |
| binarytrees | 2x | mature | 6312 | 3403 | 3911 | 3018 | 0.89 (0.97, 0.66, 0.82) | 74 / 54 | 144 / 140 |
| binarytrees | 4x | nursery | 5789 | 2631 | 2651 | 2039 | 0.78 (0.76, 0.84, 0.83) | 19 / 14 | 189 / 137 |
| binarytrees | 4x | mature | 5894 | 2543 | 2477 | 2113 | 0.83 (0.76, 0.86, 0.83) | 19 / 14 | 198 / 137 |
| binarytrees | 8x | nursery | 5890 | 1958 | 1954 | 1908 | 0.97 (0.95, 0.98, 0.97) | 8 / 6 | 189 / 244 |
| binarytrees | 8x | mature | 5704 | 2180 | 1975 | 1948 | 0.89 (0.91, 0.87, 0.92) | 8 / 6 | 189 / 244 |
| requests | 2x | nursery | 50223 | 88610 | 96514 | 11407 | 0.13 (0.12, 0.13, 0.13) | 4281 / 136 | 170 / 217 |
| requests | 2x | mature | 50537 | 44602 | 45706 | 10418 | 0.23 (0.22, 0.24, 0.23) | 1943 / 108 | 171 / 240 |
| requests | 4x | nursery | 35518 | 8879 | 8618 | 7936 | 0.89 (0.93, 0.92, 0.89) | 69 / 43 | 281 / 353 |
| requests | 4x | mature | 41171 | 9150 | 8978 | 7926 | 0.87 (0.85, 0.89, 0.87) | 67 / 41 | 302 / 373 |
| requests | 8x | nursery | 29029 | 8552 | 8852 | 7526 | 0.88 (0.88, 1.10, 0.86) | 25 / 19 | 509 / 560 |
| requests | 8x | mature | 34016 | 7720 | 7819 | 7530 | 0.98 (0.86, 1.10, 1.00) | 26 / 19 | 498 / 560 |
| listsuffix | 2x | nursery | 52740 | 15414 | 14704 | 15690 | 1.02 (0.93, 1.15, 1.06) | 139 / 20 | 171 / 499 |
| listsuffix | 2x | mature | 54225 (2 of 3 runs) | 16425 | 16484 | 19747 | 1.20 (0.51, 1.41, 1.20) | 156 / 27 | 172 / 514 |
| listsuffix | 4x | nursery | 50783 | 13317 | 13326 | 13942 | 1.05 (1.05, 1.08, 1.00) | 51 / 15 | 245 / 515 |
| listsuffix | 4x | mature | 52985 | 36656 | 18594 | 17364 | 0.47 (0.45, 0.49, 0.45) | 76 / 19 | 259 / 575 |
| listsuffix | 8x | nursery | 33573 | 15964 | 18423 | 13693 | 0.86 (0.73, 0.84, 0.97) | 17 / 11 | 460 / 536 |
| listsuffix | 8x | mature | 34469 | 20975 | 19483 | 14654 | 0.70 (0.71, 0.82, 0.66) | 19 / 12 | 454 / 557 |

What this shows:

- **Geometric means S5b / S4 (18 cells):** wall 0.727 (0.129 to 1.202), collection time 0.641, peak RSS 1.325 (0.694 to 2.994). All repetitions faster in 13 cells, none slower in all, mixed in 5 (`binarytrees` 8x nursery 0.97 is "all faster" by 3% and is within the noise level I measured above; the five mixed cells are `listsuffix` 2x nursery and mature, `listsuffix` 4x nursery, `requests` 8x nursery and mature).
- **I cannot say how much of the gain comes from the borrow and how much from the larger heap** (Inferred). The no-limit mode does not hold the heap at the initial size: the retained borrowed capacity (4 to 87 MiB after a pause, see below) is live mature data that stays in the high blocks. The heap is larger, so the pause count falls (for example `listsuffix` 51 to 15). The RSS increase shows the same. A comparison at equal RSS is **not measured**.
- **`listsuffix` 2x mature is 20% slower with S5b** (1.20; one repetition is 0.51, one 1.41). RSS is 514 against 172 MB. The median is above the S4 median but the repetitions are on both sides of 1.0. I list it as slower in the median and not a stable result.
- **`listsuffix` 4x mature: 0.47.** The S4 build is in its slow mode (36.7 s); "off" (same binary as S5b) takes 18.6 s. So the gain is against the slow mode of S4, not an effect of the overshoot. S5b against off: 17.4 s against 18.6 s.
- **Outside the borrow, S4 and "off" are equal** within the noise except the two modes above. This is the expected result: with the switch off, the S5b binary runs the S4 code.
- **Failures:** 1 of 216 runs: the accepted build, `listsuffix` 2x mature, repetition 1: `Fatal error. E_FAIL` in the status read at the end of the program. The S4, off and S5b builds finished all of their cells. In this configuration `listsuffix` 2x does not run out of memory (the known failure happens with a hard limit). The cause of the accepted-build failure is Open; it is not in the S5b code.

### Borrowed capacity and cost per pause (Measured; traced runs, 3 repetitions, S5b build; `results/trace-tables-s5b.md`)

Cells: `binarytrees` 40 MiB, `requests` 84 MiB, `listsuffix` 40 MiB as the initial admitted size (about 4x). The numbers pool the pauses of the three repetitions.

| Workload | Mode | Pauses | Pauses that borrow | Borrow per pause MiB (median / max) | Give-back attempts / successes | Complete (back to floor) | Borrowed after the pause MiB (median / max) | Peak capacity MiB (max of runs) |
|---|---|---|---|---|---|---|---|---|
| binarytrees | nursery | 42 | 42 | 3.1 / 63.9 | 42 / 42 | 0 | 4.2 / 8.1 | 96 |
| binarytrees | mature | 42 | 42 | 3.1 / 63.9 | 42 / 42 | 0 | 4.2 / 7.9 | 96 |
| requests | nursery | 132 | 126 | 4.7 / 7.4 | 144 / 135 | 0 | 20.6 / 33.6 | 222 |
| requests | mature | 126 | 120 | 5.8 / 7.4 | 138 / 130 | 0 | 21.8 / 37.5 | 222 |
| listsuffix | nursery | 45 | 37 | 14.3 / 27.0 | 54 / 43 | 0 | 52.9 / 70.8 | 113 |
| listsuffix | mature | 54 | 48 | 14.3 / 29.7 | 63 / 53 | 0 | 48.0 / 87.3 | 133 |

The floors are 32 MiB (`binarytrees`, `listsuffix`) and 74 MiB (`requests`). One pause can have more than one give-back attempt (the attempt that follows a regular growth, or a pause that borrowed nothing). I have not separated these.

| Workload | Mode | Borrow cost, us (median / max) | Give-back cost, us (median / max) | Median pause, us | Share of the median pause |
|---|---|---|---|---|---|
| binarytrees | nursery | 67 / 141 | 97 / 407 | 22820 | 0.7% |
| binarytrees | mature | 67 / 111 | 99 / 324 | 21855 | 0.8% |
| requests | nursery | 72 / 136 | 112 / 487 | 35947 | 0.5% |
| requests | mature | 75 / 165 | 119 / 1902 | 35768 | 0.5% |
| listsuffix | nursery | 91 / 208 | 161 / 759 | 518054 | 0.05% |
| listsuffix | mature | 91 / 168 | 164 / 573 | 428471 | 0.06% |

The cost of admission and of decommit is small against the pause: the sum of the two medians is at most 0.8% of a median pause in every cell. The largest single give-back (1.9 ms) is 5% of a 36 ms pause.

Wall time, collections and pauses in the traced runs (medians of 3; the tracer adds some cost, so the wall times are not those of the matrix):

| Workload | Mode | Build | Wall ms | GC ms | Collections | Median pause ms | Max pause ms | Peak RSS MB |
|---|---|---|---|---|---|---|---|---|
| binarytrees | nursery | S4 | 2831 | 729 | 20 | 17.3 | 254.6 | 199 |
| binarytrees | nursery | S5b | 2364 | 387 | 14 | 22.8 | 59.5 | 138 |
| binarytrees | mature | S4 | 2743 | 784 | 20 | 17.4 | 277.1 | 188 |
| binarytrees | mature | S5b | 2382 | 384 | 14 | 21.9 | 59.3 | 139 |
| requests | nursery | S4 | 9607 | 2194 | 71 | 25.4 | 134.6 | 284 |
| requests | nursery | S5b | 8743 | 1877 | 44 | 35.9 | 166.5 | 354 |
| requests | mature | S4 | 10239 | 2749 | 67 | 26.4 | 309.1 | 308 |
| requests | mature | S5b | 9191 | 2098 | 42 | 35.8 | 314.7 | 369 |
| listsuffix | nursery | S4 | 20581 | 13916 | 60 | 309.0 | 710.4 | 254 |
| listsuffix | nursery | S5b | 13661 | 7717 | 15 | 518.1 | 1121.2 | 538 |
| listsuffix | mature | S4 | 22615 | 14990 | 53 | 196.3 | 1028.8 | 410 |
| listsuffix | mature | S5b | 14241 | 8347 | 18 | 428.5 | 1221.6 | 589 |

- Total collection time is lower in all 6 cells. **The median pause is longer in all 6 cells** (for example `requests` 25.4 to 35.9 ms), because the pauses are fewer and each one handles more. The maximum pause is shorter in `binarytrees` (255 to 60 ms) and longer in `listsuffix` (710 to 1121 ms; 1029 to 1222 ms). A latency-sensitive program would not gain from this mode in `listsuffix`.
- The S5b build with overshoot off (same binary) is in `results/trace-tables-s5b.md`. In `listsuffix` mature the "off" run (34.0 s, 88 collections) is slower than S4 (22.6 s, 53 collections); this is the fast and slow mode of the S4 code in that cell, not an S5b effect (Inferred).

### RSS cost (Measured, Inferred)

Peak RSS is 1.325 times S4 as the geometric mean (0.694 to 2.994). Two causes (Inferred): (a) the retained borrowed capacity, which is live data; (b) the planes. One MiB of added capacity costs about 2.6 MiB of RSS in the pilots: the object flags (4 bytes per 8 bytes of heap), the trace planes (about 9 bytes per 8 bytes, committed while a trace runs) and the other side-metadata planes. I did not run a controlled experiment for the factor 2.6 (Open). In 4 cells the RSS is lower (`binarytrees` 2x and 4x; at 4x it is 137 MB against 189 and 198 MB). I did not find the reason (Open).

### Tests (S5b)

| Suite | Linux Release | Linux Debug | Windows Release | Windows Debug |
|---|---|---|---|---|
| Native validation, 30 selectors (the 29 of S4 and the new `--s5b-overshoot`) | pass | pass (asserts armed) | pass | pass (asserts armed) |
| New selector `--s5b-overshoot` (checks) | pass 9477 | pass 9152 | pass 9483 | pass 9408 |
| Managed scope-5 fixture, 90 cells (Release and Debug collector) | pass 90 of 90 | pass (Debug collector, asserts armed) | not run | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 | not run (the scripts build without `_DEBUG`) | not run | not run |
| `clrlxrgc` module build | pass | pass | pass | pass |

All on the final tree (`8090810baae`, plus this documentation commit, which changes no source). The earlier commits passed the same suites on the tree before the last fix; I ran everything again after the fix.

The selector covers: the policy trigger with and without overshoot; admission in a pause; give-back of a free tail; refusal to give back when a block in the tail is in use; no allocation into a returned range; admission and give-back across the epoch flip (random runs across the epoch wrap, two kinds); a round trip of the planes (decommit, then admit the same addresses and find zero planes; on Linux with `mincore`); the hard-limit, legacy-capacity and switch-off cases unchanged; regular growth; the placement; queued trace work that names a returned block.

Controls (each is a compile define; the selector must report failed checks; `controls-s5b.sh`; output in `results/controls-s5b-release.txt`):

| Control | What it breaks | Failed checks |
|---|---|---|
| `LXR_S5B_NO_BORROW_CONTROL` | The pause admits nothing | 7 |
| `LXR_S5B_NO_GIVEBACK_CONTROL` | No give-back | 4 |
| `LXR_S5B_ALWAYS_RETURN_CONTROL` | Give-back also returns blocks that are in use | 1 (the run then ends abnormally after the FAIL line) |
| `LXR_S5B_NO_ALLOC_SHRINK_CONTROL` | The allocator keeps the old capacity | 14 FAIL lines (abnormal end after them) |
| `LXR_S5B_NO_PLANE_DECOMMIT_CONTROL` | The planes are not decommitted | 7 |
| `LXR_S5B_NO_DATA_DECOMMIT_CONTROL` | The heap range is not decommitted | 1 |
| `LXR_S5B_KEEP_RANGE_OWNED_CONTROL` | The data range stays owned after the give-back | 3 (abnormal end after the FAIL lines) |
| `LXR_S5B_IGNORE_HARD_LIMIT_CONTROL` | Overshoot ignores the hard limit | 2 |
| `LXR_S5B_IGNORE_PLANE_LIMIT_CONTROL` | Admission ignores the native plane limit | 1 |
| `LXR_S5B_RESERVE_TRIGGER_CONTROL` | The overshoot mode uses the S5a reserve in the trigger | 2 |
| `LXR_S5B_NO_PLACEMENT_CONTROL` | No placement rule | 1 |
| `LXR_S5B_NO_STALE_TOLERANCE_CONTROL` | A reference into a returned block is a failure | 5 |

The controls were built in Release. The Debug build asserts at some of them. I did not run the controls on Windows.

### Not measured, open items (S5b)

| Item | Label |
|---|---|
| Windows performance of S5b, and the Windows managed suite | Not measured |
| A comparison at equal RSS or equal heap size between S4 and S5b without a hard limit | Not measured |
| Cause of the RSS factor (about 2.6 MiB per MiB of capacity): flags, trace planes, side metadata | Inferred; not isolated |
| The capacity never returned to the floor (0 "complete" give-backs in 6 cells). A budget-based policy that triggers earlier, or that compacts survivors to the low blocks, could lower the RSS. | Open (design decision) |
| `listsuffix` 2x mature (1.20) and the larger maximum pause in `listsuffix` | Measured; cause Open |
| The decrement guard of the fix (`IsReturnedImmix` in `ProcessDecrements`) is by reading, not by a failing run | Code, not tested by a run |
| What a read of a decommitted page does on Linux and Windows for a path that has no guard | Open |
| Accepted build: 1 `E_FAIL` in 54 no-limit runs (`listsuffix` 2x mature, end of program) | Measured, cause Open |
| The hard-limit mode still uses the S5a byte reserve. With a hard limit the same borrow could use the room under the limit; this is outside D2. | Open |
| A trace of the hard-limit matrix per pause (S4 against S5b) was not done; the check is the wall-time matrix and the native test | Not measured |

## S6: the finalizer registry (no pause walks the heap)

Labels as above. Evidence was reduced at the request of the coordinator: see "What was reduced" at the end of this section.

### Short answers (S6)

1. **No pause walks the heap now (Code, Measured).** The three walks of `ExecuteScan` (`LeaseFinalizer`, `QueueFinalizer`, `TracePendingFinalizer`) are gone. The flags `Registered`, `Pending`, `Suppressed`, `Critical` and `Finalized` are gone from `m_flags`. Finalizable objects are in a registry list. The pin and copy flags that `LeaseFinalizer` reset are cleared from a per-pause list. In the profile, `ExecuteScan` (inclusive) goes from 9.8%, 8.3% and 1.3% of all samples (binary trees, requests, list suffix) to 0.19%, 0.12% and 0.02%. The median RefCount pause of binary trees at a 4x heap goes from 18.1 to 3.8 ms (non-moving) and from 17.3 to 5.3 ms (nursery copy).
2. **The rule "RC pauses decide nothing" was already true for weak handles (Code, check C4).** The old code gave the target of a weak handle an increment (without a trace) in every pause, and it cleared weak handles only in tracing pauses (`ExecuteScan` kind 1). `GcScanHandles` and the weak scans work on the handle table and need no per-object flag. What changed is the finalizable objects: they are no longer found by a walk.
3. **The old code could not hold more than 131072 roots in a pause (Measured).** Each registered object was a root of every pause. A program with 1 million short-lived finalizable objects stopped with `Fatal error` (E_FAIL) in the accepted build and in the S5b tip. The roots array now grows, and the registry keeps its counts in its own list, so no such limit exists.
4. **Cost of the decision D3 option 1 (Measured, Open for a bound).** A finalizer runs after the next tracing pause, not after the next RC pause. In a natural run with one tracing pause for five collections, the mean delay between the last use of an object and its finalizer was 4.2 to 4.5 seconds. The peak RSS was 1232 MiB, against 498 MiB for the same program with objects that have no finalizer. I think the extra RSS comes from the planes that a trace cycle commits; I did not isolate it (Inferred).
5. **Everything that I tested is green on the final tree** (see "Tests (S6)"). A review by a second agent found two real faults in my first version; both are fixed and have tests (section "Review of the first version").

### Commits (S6)

| Commit | Subject |
|---|---|
| `64baf08f849` | S6a: retained roots, unlimited roots and batched trace roots in the engine, and the finalizer registry container |
| `834a1f743d5` | S6b: finalizer registry replaces the finalizer flags and the heap walks; touched list replaces the flag reset walk |
| `6bfc69b4384` | S6c: managed cells for finalizer churn, weak churn, resurrection and explicit collections |
| `813dfc8807c` | S6d: registry glue in one header that the collector and the native tests both use |
| `ca5496a4439` | S6e: native selector `--s6-registry` and its controls |
| `4271e3bc2ab` | S6f: merge duplicate registry entries; counts given up by a decision follow moved objects (review fixes) |
| `1eb1112e02d` | S6g: `FinalizerLag`, the measurement program |
| `9b06c3f9de1` | S6h: `TraceRoots` does not use `std::min` (a Windows.h macro broke the MSVC test build) |
| `9b52be8e383` | S6i: trace, table and perf scripts |
| `f505dcd1450` | S6j: the S6 controls also work in product builds; lag table; managed controls |
| `c00a5e14465` | S6k: the pooled cell runs natural collections while registered objects are alive |

### What the old code did (Code)

Every pause, `ExecuteScan` kind 0 walked all objects (`LeaseFinalizer`: reset of the pin and copy flags, and a root for each registered or pending object), and kind 1 walked them again (`QueueFinalizer`, `TracePendingFinalizer`). The state "registered, pending, suppressed" was a flag per 8 bytes of heap. Each registered object was a root of every pause (an increment, and a decrement at the next pause). A pause had room for `MaximumRoots` = 131072 roots.

### The design that was built (Code)

**The registry** (`lxr_finalizer_registry.h`) has three states for an entry:

| State | Meaning | Count of the registry |
|---|---|---|
| new | Appended by a mutator since the last pause (a lock-free list of chunks of 1022 entries; one atomic add per registration) | none yet |
| counted | The root capture of a pause has given the object one count (`RetainRoot`). The count stays until the entry leaves. | one |
| ready | A tracing pause found the object unreachable. It waits for the finalizer thread. | one, until the finalizer thread takes it |

The entry is the object address; the lowest bit tells that the finalizer is critical. A normal entry is handed out before a critical one.

**Registration.** `AllocationComplete` registers an object with the `Finalizable` flag. `Alloc` does not register any more: the runtime reports every allocation in `AllocationComplete` with the final type, so an object is registered exactly once and its critical bit is known. `GC.ReRegisterForFinalize` registers again. `GC.SuppressFinalize` only sets the header bit `BIT_SBLK_FINALIZER_RUN`. There is no side table.

**Root capture, every pause** (`LxrFinalizerCapture`, in `lxr_finalizer_pause.h`). New entries whose object has the suppress bit are dropped and never get a count. Every other new entry gets one count through the engine call `RetainRoot`. `RetainRoot` is a root of the kind "increment, no trace", and no later pause takes it away. The counts that were given up are handed to the engine with `ReleaseRoot` (a decrement of the kind "previous root"). In a pause that traces (initial mark, final mark, full), the objects that wait for their finalizer also become trace roots. A young object that is registered at allocation has count 0 until this capture; the same pause then promotes it (and copies it in a copying mode).

**RC pauses decide nothing.** `finishTrace` (kind 1) runs only in final mark and full pauses.

**Tracing pause** (`LxrFinalizerDecide`, after marking). The registry looks at every counted entry:

- The object has the suppress bit, or the EE finalizes it eagerly: the entry is dropped. Its count is given back at the next pause. If the object is unmarked, it is traced now (see the table of design choices).
- The object is unmarked: the entry moves to the ready list. All unmarked entries are found first, and only then are they traced, so that f-reachability cannot hide another candidate (the same rule as the old two walks).
- The object is marked: the entry stays.

The weak handle scans of kind 1 are unchanged. The dead-cycle sweep skips the objects of the ready list because they are marked. The finalizer thread gate of S3 (`GetNextFinalizable` returns null while the cycle sweep is pending) is unchanged.

**Finalizer thread.** `GetNextFinalizable` takes the next ready entry (non-critical first). It lets go of the count of the entry (the next root capture hands it to the engine) and skips an object whose finalization was suppressed after the decision. No pause runs between the take and the return, because the finalizer thread is in cooperative mode.

**Relocation.** After the relocation of roots and handles (`ExecuteScan` kind 3), the entries are moved to the new address of a copied object. A nursery copy can move only the entries of the current batch. When mature evacuation is on, a tracing pause can move any entry, so all entries and the ready list are relocated.

**Touched list.** `AddRoot` (pinned root) and `DidCopy` put the object on a lock-free list when they set `ObservedPin` or `CopiedThisPause`. Kind 2 (the end of the pause, before the give-back of S5b) clears the flags from the list. No heap walk is left in `ExecuteScan` (check C4: `GcScanHandles` and the weak scans need no flag).

**Engine changes.** The roots arrays grow (limit 2^27 roots of one kind). `RetainRoot`, `ReleaseRoot` and `TraceRoots` (many trace roots, one drain) are new. They use the existing work kinds `Roots` and `PreviousRoots`, so the relocation of queued work (`FixForwardingReaders`) covers them.

### Design choices that differ from the plan, and why

| Plan | What I did | Reason |
|---|---|---|
| "Use the existing root machinery" for the count of the registry | New engine calls `RetainRoot` and `ReleaseRoot` that use the existing work kinds, with their own arrays | An `AddRoot` root lasts one pause, so a registry of N objects would be N roots of every pause, and the limit of 131072 would stay. I also made the roots arrays grow. |
| Chunks, lock-free or per-thread, merged at the pause | One global chunk list, one atomic add per registration | Simple and safe. A contended counter can cost on a program with many threads that register at once. The 4-thread run showed no problem (7.2 s against 7.1 s for one thread), but I did not look for a worst case (Open). |
| Suppression by the header bit, no side table | Same, plus a lazy drop | A new entry with the bit set is dropped at the next pause and never gets a count (the common `Dispose` pattern). A counted entry with the bit set is dropped at the next tracing pause, so its object stays alive until then (see "Semantic differences"). |
| An entry whose object is unmarked is queued and traced | Also: a dropped (suppressed or eager) unmarked entry is traced | Its count is given back at the next pause, as a decrement of an address. The lazy cycle sweep of this pause could free the object and let a new object use the address before the decrement runs. A marked object is not swept. |
| (not in the plan) | Entries of one object are merged | `SuppressFinalize` then `ReRegisterForFinalize` gave two entries (the old flag was idempotent, and the existing test expects one finalizer). New entries are merged in the capture; counted entries are merged in the next tracing decision; each extra entry gives its count back. |
| `Pinned` flag semantics kept | Kept; the pause no longer counts the objects of the pinned object heap | The old walk added every `Pinned` object to the metric `Pins` of each pause. Without a walk, `Pins` counts only the pinned roots that the pause saw. S7 replaces the flag. |

### Review of the first version (Measured by a read-only review, then fixed)

A second agent reviewed the commits and found two faults (both fixed in `4271e3bc2ab`, with native checks, native controls and a managed cell):

1. The count of a dropped entry was queued at the old address of the object, but the trace of the same pause can copy the object (mature evacuation). The decrement would hit the old slot. The counts now wait inside the registry, where relocation reaches them.
2. `SuppressFinalize` followed by `ReRegisterForFinalize` added an entry and a count on every call. A pooled object that does this a million times would keep a million entries. The entries are now merged (see above). The managed cell `pooled-reregister` runs 200000 such calls on 500 objects: 527 counted entries remain.

A first run of the full managed suite also had one failure (Debug collector, Release fixture, mature mode, `weak-churn`, an assertion in the runtime). I could not reproduce it in 10 runs of the later tree (the later tree includes fix 1, which concerns mature evacuation). The final suite on the final tree is green (see "Tests (S6)"). Cause of the first failure: Open (Inferred: fix 1).

### Semantic differences (honest)

| Behavior | Before | Now |
|---|---|---|
| When a dead finalizable object is finalized | At the next tracing pause (the old walk in kind 1 only decided there) | Same. **No change in timing**; the change is the cost and the limit. |
| Weak handles in natural collections | Cleared at tracing pauses only | Same. |
| An object with `SuppressFinalize` that has survived a pause | Released at the next RC pause (the flag removed its root) | Released at the next tracing pause (its count stays until the registry looks at the entry). Memory is kept longer for objects that were disposed after they survived a pause. A `Dispose` in the same epoch as the allocation costs nothing (the entry is dropped before it gets a count). |
| `GC.ReRegisterForFinalize` on an object that is registered | One registration (flag) | One registration (entries are merged). |
| Critical finalizer of an object registered by `ReRegisterForFinalize` | The `Critical` flag was set only at allocation | The critical bit is read from the type at each registration (more correct). |
| Number of finalizable objects and weak handles between two tracing pauses | At most about 131072 (then a fatal error) | Not limited by the roots array. |
| `GetMemoryInfo` pinned count | Counted the objects of the pinned object heap | Counts only the pinned roots seen by the pause (S7 will give a better answer). |
| An explicit `GC.Collect()` | Full pause: clears weak handles, queues finalizers at once | Same (tested: 200 rounds of 100 objects). |

### Results: pause cost (Measured; traced runs, heap 4x, hard limit, 3 repetitions per cell; `results/trace-tables-s6.md`)

Median RefCount pause (ms), pooled over the runs of a cell:

| Workload | Mode | Accepted | S5b tip | S6 |
|---|---|---|---|---|
| binary trees | non-moving | 85.3 | 18.1 | 3.8 |
| binary trees | nursery copy | 111.5 | 17.3 | 5.3 |
| binary trees | mature copy | 108.7 | 18.2 | 5.1 |
| requests | non-moving | 107.1 | 21.1 | 6.7 |
| requests | nursery copy | 145.6 | 25.4 | 13.8 |
| requests | mature copy | 143.6 | 25.8 | 14.1 |
| list suffix | non-moving | 363.1 | 48.2 | 38.0 |
| list suffix | nursery copy | 734.3 | 372.8 | 325.5 |
| list suffix | mature copy | 608.0 | 76.2 | 51.9 |

Share of the root capture (S0 phase timers, RefCount pauses, S5b tip against S6): binary trees 48% to 1.5% (non-moving), requests 57% to 1.9%, list suffix 9.4% to 0.2%. The step that was `scan_lease_finalizers` (the walk) takes a median of 11 to 14 ms in the S5b tip and 0.00 ms in S6. In tracing pauses, the old `scan_queue_finalizers` took 4 to 12 ms (median); the registry decision takes 0.01 to 0.04 ms in these runs (they have few registered objects).

Profile (`perf`, 4x heap, nursery copy, share of all samples; `results/perf-shares-s6.md`):

| Function (inclusive) | binary trees S5b / S6 | requests S5b / S6 | list suffix S5b / S6 |
|---|---|---|---|
| `ExecuteScan` | 9.80 / 0.19 | 8.28 / 0.12 | 1.27 / 0.02 |
| `WalkObjectStarts` | 9.19 / gone | 7.71 / gone | 1.15 / gone |
| `LeaseFinalizer` | 6.47 / gone | 5.40 / gone | 0.88 / gone |

### Results: wall time (Measured; matrix with checksums; `results/wall-table-s6.md`)

The matrix is S5b tip against S6 (and the accepted build as a third arm), 3 workloads, heaps 2x, 4x and 8x, 3 modes, 3 repetitions, hard limit as in the earlier matrices, three pinned parallel streams, interleaved. It ran before the coordinator asked for a smaller plan. All 252 finished runs of the three builds have one checksum per workload. The 4x cells (medians of 3, ms):

| Workload | Mode | Wall S5b | Wall S6 | S6 / S5b per repetition | Collection time S5b / S6 | Peak RSS MB S5b / S6 |
|---|---|---|---|---|---|---|
| binary trees | non-moving | 2474 | 2228 | 0.94, 0.79, 0.97 | 733 / 500 | 188 / 187 |
| binary trees | nursery | 2540 | 2079 | 1.11, 0.82, 0.70 (noise) | 740 / 183 | 182 / 111 |
| requests | non-moving | 20813 | 19518 | 0.94, 0.94, 0.94 | 10935 / 9606 | 294 / 296 |
| requests | nursery | 8626 | 7750 | 0.87, 0.94, 0.90 | 2116 / 1268 | 280 / 281 |
| list suffix | non-moving | 13603 | 11784 | 0.97, 0.86, 0.78 | 6673 / 5249 | 269 / 267 |
| list suffix | nursery | 28845 | 19658 | 0.94, 0.93, 0.68 (see below) | 22393 / 12853 | 256 / 256 |

All 22 cells that finish in all builds (geometric mean S6 / S5b): wall 0.908 (range 0.68 to 1.51), collection time 0.651, peak RSS 1.005 (range 0.61 to 1.66). All three repetitions are faster in 16 cells and slower in none. Six cells have repetitions on both sides of 1.0 and are within the noise: binary trees 4x nursery and mature, requests 8x non-moving and mature, list suffix 8x mature, list suffix 4x mature. S6 against the accepted build: all 22 cells faster (geometric mean 0.405).

- **The peak RSS range (0.61 to 1.66) is not a regression in the registry (Inferred).** It follows whether a tracing cycle ran in that run: a cycle commits the trace planes. Binary trees 4x nursery has no tracing pause in the traced S6 runs (RSS 111 MB) and one in the S5b runs (182 MB); mature is the other way around.
- **List suffix 4x (nursery and mature) is bimodal in both builds** (runs of 12 to 15 s and of 30 to 37 s). With 9 runs each (`results/matrix-s6x.jsonl`): mature S5b median 30.1 s, S6 33.9 s (slow mode in 5 of 9 against 7 of 9 runs); nursery S5b 17.0 s, S6 13.4 s. I cannot tell the two apart; both are within the noise. Cause of the bimodality: Open (it was already in S5b).
- **Failures, the same as in the earlier builds:** list suffix 2x runs out of memory in all builds (9 of 9 each); requests 2x nursery and mature fails in the S5b tip and in S6 (6 of 6 each; the message is `Out of memory.`, the accepted build finishes). No new failing cell.

### Results: finalization delay and memory cost (Measured; `FinalizerLag`; `results/lag-table-s6.md`, 3 repetitions)

`FinalizerLag` creates finalizable objects (each owns a byte array), keeps a ring of them alive, and stores in each object the time at which it left the ring. The finalizer reports the delay. Only natural collections run while it measures. Heap 256 MiB with a hard limit, one producer unless stated.

| Configuration | Mode | Build | Finished | Finalized when the producer ended | Delay mean / max (ms) | Peak RSS MiB | Tracing pauses / collections |
|---|---|---|---|---|---|---|---|
| 2M objects, none disposed | nursery | accepted, S5b tip | 0 of 3 each (`Fatal error`) | | | | |
| 2M objects, none disposed | nursery | S6 | 3 of 3 | 1037411 | 4169 / 5069 | 1232 | 1 / 5 |
| 2M objects, none disposed | mature | S6 | 3 of 3 | 1037411 | 4465 / 5911 | 1235 | 1 / 5 |
| 2M objects, none disposed, 4 threads | nursery | S6 | 3 of 3 | 1036541 | 4455 / 5355 | 1244 | 1 / 5 |
| 2M objects, none disposed | non-moving | S6 | 0 of 3 (`Out of memory.`) | | | | |
| the same, objects without a finalizer (control) | nursery | S6 | 3 of 3 | none | none | 498 | 0 / 1 |
| 2M objects, half disposed | nursery, mature | S6 | 3 of 3 | 0 (no tracing pause ran) | none | 503 | 0 / 2 |
| 120k objects of 4 KiB, none disposed | nursery | S5b tip | 3 of 3 | 112879 | 518 / 1034 | 698 | 3 / 18 |
| 120k objects of 4 KiB, none disposed | nursery | S6 | 3 of 3 | 112884 | 490 / 1060 | 700 | 3 / 18 |
| 120k objects of 4 KiB, none disposed | mature | S5b tip | 3 of 3 | 112883 | 643 / 1170 | 698 | 3 / 18 |
| 120k objects of 4 KiB, none disposed | mature | S6 | 3 of 3 | 112889 | 539 / 1150 | 700 | 3 / 18 |
| the same, objects without a finalizer (control) | nursery | S6 | 3 of 3 | none | none | 498 | 0 / 3 |
| 120k objects of 4 KiB | non-moving | accepted, S5b tip, S6 | 0 of 3 each | | | | |

- **Lost finalizers: 0 in every finished run** (after the final collections, each object was in the ring, finalized, or had its finalization suppressed). The accepted build also failed the 120k run in the copying modes.
- **The delay is the time to the next tracing pause (Measured).** With 3 tracing pauses in 18 collections the mean delay is about half a second; with 1 in 5 it is over four seconds. S5b and S6 behave the same where S5b can run (same number of tracing pauses; mean 518 against 490 ms, and 643 against 539 ms, within the spread of the runs).
- **Memory cost.** The objects that wait for a tracing pause are alive, so the heap holds them. With a 256 MiB hard limit, the non-moving mode failed with 2M finalizable objects (about 260 MB in total; S6 reports Out of memory., the older builds stop earlier at the root limit) and with 120k objects of 4 KiB (about 490 MB; all three builds). The copying modes finished in S6. This is the cost that a bound (a trigger on the registry size) could limit. I did not add one (decision D3, option 1). Peak RSS is 1232 MiB against 498 MiB for the control: Inferred (trace planes of the one tracing cycle), Open.
- One S6 run with the phase timers (nursery, 2M objects, 5 pauses; a single run): stop-the-world time 5.8 s in total; the registry decision 1.7 s, of which tracing the 1M candidates 1.6 s. The rest is the promotion and copy of about 1M registered young objects per pause. The old code also promoted registered objects (through its roots); it could not reach this size at all.

### Tests (S6)

All on the final source tree (`c00a5e14465`, plus this documentation commit, which changes no source). The Linux suites ran once on this tree. Windows Release ran on this tree; Windows Debug ran on the tree of `9b06c3f9de1` (the only later source change is the guard of the controls, which compiles to the same code without a define).

| Suite | Linux Release | Linux Debug | Windows Release | Windows Debug |
|---|---|---|---|---|
| Native validation, 31 selectors (the 30 of S5b and `--s6-registry`) | pass | pass (asserts armed) | pass | pass (earlier tree) |
| New selector `--s6-registry` | pass 293 | pass 293 | pass 293 | pass 293 (earlier tree) |
| Managed scope-5 fixture, 126 cells (3 modes, Release and Debug collector, Release and Debug fixture; includes the 6 new cells with the Release fixture) | pass 126 of 126 | pass (Debug collector, asserts armed) | not run | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 (P3.3 357 of 357) | not run (the scripts build without `_DEBUG`) | not run | not run |
| `clrlxrgc` module build | pass | pass | pass (final tree) | pass (earlier tree) |

Outputs: `results/tests-native-release-s6.txt`, `tests-native-debug-s6.txt`, `tests-managed-s6.txt` and `.csv`, `tests-legacy-s6.txt`, `tests-native-windows-release-s6.txt`. Test runs of the first version of the code (`6bfc69b4384`) had the managed failure that is described above; the earlier Linux runs of the later trees (`1eb1112e02d`) were also green.

**Native checks of S6** (`--s6-registry`, 293 checks): concurrent append from 8 threads across many chunks; the states of the registry on fake addresses (suppressed new entries, counts, decision, duplicates, critical order, relocation of a batch and of all entries, concurrent registration); a reference-counting pause keeps a registered young object (also with a nursery copy) and decides nothing; a registered old object gets one count more; the target of a weak handle gets its increment in a reference-counting pause; the decision of a full pause (unmarked object queued and traced with its child, marked object stays, suppressed and eager entries dropped, finalizer thread takes the object, the next pause kills the object and its child); the cycle sweep skips a cycle that holds a registered object and frees a cycle that does not; relocation of entries after a nursery copy; clearing of the touched list.

**Controls** (compile defines; each must make checks fail; `controls-s6.sh` for the native selector, `controls-s6-managed.sh` for managed cells; outputs in `results/controls-s6-release.txt` and `results/controls-s6-managed.txt`). Native selector, Release, run on the tree before the last two commits (the code of the registry did not change after it):

| Control | What it breaks | Result |
|---|---|---|
| `LXR_S6_UNSAFE_APPEND_CONTROL` | the append takes its slot without an atomic add | fails (291 of 293 pass) |
| `LXR_S6_TOUCHED_DROP_CONTROL` | the drain of a list does not visit the entries (touched list and registry lists) | fails; the run ends abnormally |
| `LXR_S6_NO_SUPPRESS_CONTROL` | a suppressed new entry gets a count | fails (289 of 293) |
| `LXR_S6_MARKED_READY_CONTROL` | a decision treats every entry as unmarked | fails (285 of 293) |
| `LXR_S6_NO_DEDUPE_CONTROL` | duplicate entries are not merged | fails (288 of 293) |
| `LXR_S6_NO_RELEASE_CONTROL` | the finalizer thread does not give the count back | fails (288 of 290) |
| `LXR_S6_NO_RETAIN_CONTROL` | the capture gives no count to new entries | fails (211 of 221) |
| `LXR_S6_RC_DECIDE_CONTROL` | a reference-counting pause decides | fails (204 of 216) |
| `LXR_S6_NO_DECIDE_CONTROL` | no decision | fails (251 of 259) |
| `LXR_S6_NO_TRACE_CONTROL` | candidates are not traced | fails (255 of 259) |
| `LXR_S6_NO_RELOCATE_CONTROL` | entries are not relocated | fails (221 of 225) |
| `LXR_S6_NO_RELOCATE_HELD_CONTROL` | the counts that a decision gives up are not relocated | fails (292 of 293) |

Managed controls (the module is built with the define; the cell must fail): `NO_RETAIN` (`Fatal error` in `finalizer-churn` and `resurrection`), `NO_DECIDE` (`prompt-collect` and `finalizer-churn` fail with the wrong number of finalizers), `MARKED_READY` (`pooled-reregister`: a live object was finalized), `RC_DECIDE` (`pooled-reregister`: `Fatal error`), `NO_DEDUPE` (`pooled-reregister`: 200527 entries for 500 objects), `UNSAFE_APPEND` (`finalizer-churn-concurrent`: 744979 of 750000 finalizers), `NO_TRACE` (`resurrection`, `finalizer-churn`: `Fatal error`), `NO_RELOCATE` (mature mode: `Fatal error` in `resurrection` and `finalizer-churn`; `pooled-reregister` aborts in nursery and mature mode; the nursery runs of `resurrection` and `finalizer-churn` do not fail). The controls `NO_SUPPRESS`, `NO_RELEASE`, `TOUCHED_DROP` and `NO_RELOCATE_HELD` are killed only by the native selector.

**New managed cells** (6, in `Finalization.cs`; run with a heap of 224 MiB because they keep many objects alive until a tracing pause): `finalizer-churn` (3 phases of 1M objects: none disposed, half disposed, a third disposed with finalizers that allocate; each finalizer runs exactly once, none for a suppressed object), `finalizer-churn-concurrent` (4 threads), `weak-churn` (300k short and long weak references and dependent handles; a live target is never cleared; after an explicit collection every dead target is cleared), `resurrection` (3000 objects, 4 lives each, plus critical finalizers), `prompt-collect` (200 rounds: an explicit collection finalizes and clears at once), `pooled-reregister`. Result on three builds (non-moving, one run each; `results/s6-cells-table.txt`):

| Cell | Accepted | S5b tip | S6 |
|---|---|---|---|
| `finalizer-churn`, `finalizer-churn-concurrent`, `weak-churn` | `Fatal error` | `Fatal error` | pass |
| `resurrection`, `prompt-collect`, `pooled-reregister` | pass | pass | pass |

The last three cells check behavior that must not change; they pass on the old builds too (on those builds `pooled-reregister` skips its registry metric).

### What was reduced

- The benchmark evidence is **larger** than the lean plan, not smaller: the full matrix (3 workloads, 3 heap sizes, 3 modes, 3 repetitions, three builds), the traced runs and the profiles had finished before the coordinator asked for a lean plan. I report them as they are. No new benchmark was run after the request, except the tests below.
- Windows: native selectors Release and Debug passed on the tree of `9b06c3f9de1` (the Release run is repeated on the final tree, see "Tests (S6)"). The Windows managed suite and any Windows performance were not run.
- The profile uses one recording per build and workload (nursery copy, 4x). The finalization-delay program ran 3 repetitions per configuration and mode; there is no sweep over rates, payload sizes or thread counts.

### Not measured, open items (S6)

| Item | Label |
|---|---|
| A bound on the delay and on the memory of the registry (a trigger on its size or bytes) | Open (D3 option 1: not added; the measurements above are its input) |
| Non-moving mode with a hard limit and a finalizer backlog runs out of memory: a backlog of ready objects is not released before the allocation fails. A wait for the finalizer thread, or an earlier trigger, could help. | Open (failure Measured, cause Inferred) |
| The 1232 against 498 MiB RSS: share of the trace planes and of the registry | Inferred, not isolated |
| The global chunk list of registrations under a very heavy multi-thread load | Not measured |
| An object with `SuppressFinalize` that survived a pause is kept until the next tracing pause (memory) | Effect on a real program Open |
| The relocation of all entries in each tracing pause in mature mode costs one header read per entry | Code; not measured for a large registry |
| Cause of the single failure of the first full managed run (Debug collector, mature, `weak-churn`) | Open (Inferred: review fault 1, fixed) |
| Bimodal list suffix 4x (nursery and mature) in S5b and S6 | Open (not new) |
| `Pins` metric of `GetMemoryInfo` no longer counts the pinned object heap | Code; S7 |
| The P3.3 intermittent failure under load | Open (not seen in the S6 runs) |

## S7: the pin list (the last per-object tables are gone)

Labels as above. The user asked for lean evidence: the benchmark is small (heap 4x, three workloads, two modes, two repetitions, the integrated tip against the S7 tip) and there are no perf profiles.

### Short answers (S7)

1. **No table with an entry for every 8 bytes of heap is left in the collector module (Code).** `m_flags` (4 bytes per 8 bytes of heap) is gone. The two trace tables are gone too: `m_trace_locks` (1 byte) and `m_trace_scans` (8 bytes). The side metadata planes of the engine are bit planes and the reference count plane; the log streams are per admitted slot (they are not per object tables, and they are the largest native cost that stays; see "Not measured, open items").
2. **Native plane bytes and peak RSS fall (Measured).** Native planes (metric 38, maximum over the run): binary trees 212.0 to 160.0 MiB, requests 456.3 to 336.0 MiB. Peak RSS (matrix, mean of 2): binary trees non-moving 185 to 136 MiB, requests 279 to 159 MiB, list suffix non-moving 274 to 225 MiB. The checksums are equal in every cell.
3. **Wall time and GC time do not get worse (Measured).** In the matrix, the S7 build is faster or equal in all six cells (requests non-moving 19.6 s to 15.2 s; GC time 9.8 s to 6.3 s). **Inferred:** the allocation path no longer writes a flag word for every object, and a pause no longer touches a flag table. I did not profile it.
4. **Pinning uses a short list in the pause and one byte per block (Code).** Pins found in the root scan go into a list. The pause sets one bit in the byte of the block. The copy decision reads the byte first. Only a block with the bit looks in the list.
5. **Objects of the pinned object heap lie in pinned blocks (Code).** Pinned blocks hold these objects only. No pause copies from them, and no other allocation uses them. Large ones lie in the large-object space (it never moves objects).
6. **I found one failure that is not new, and one fact that the plan did not say (Measured).** (a) The pin-heavy program fails now and then when the machine is oversubscribed. It fails on the integrated tip too (section "A failure that is not new"). (b) A pause that reaches a pinned object promotes its block in place, so a neighbour in the same block that the pause reaches later stays. The plan said that the neighbour is copied; it is copied only when the pause reaches it first (section "What the pause does with a pin").
7. **Everything that I ran is green on the final tree** (section "Tests (S7)"), except the Windows managed suite and the Linux Release collector in the managed suite, which I did not run.

### Commits (S7)

| Commit | Subject |
|---|---|
| `55713457437` | S7a: pin list, pinned blocks and scan index replace the per-object pin and trace tables |
| `c41f6c5c6f5` | S7b: native selector `--s7-pins`, managed pinning cells and their controls |
| S7c | native test fix (`small` renamed, MSVC macro), tracer workload `pinheavy`, benchmark and table scripts (the hash is in the handoff message) |
| S7d | results and this report (the hash is in the handoff message) |

The branch is `kkokosa-lxr-s7-pin-list`. It starts at `06d7cda94cb` (the integrated branch). I did not change the runtime (the VM, the JIT and the stock helpers); the hash check is below.

### What the S6 code did (Code)

| Item | Where | What it did |
|---|---|---|
| `Pinned` flag | `Alloc` and `AllocationComplete` | Set for every object of the pinned object heap. |
| `ObservedPin` flag | `AddRoot` | Set for each pinned root; cleared from the touched list at the end of the pause. |
| `CopiedThisPause` flag | `DidCopy` | Set for each copy; read only by the walk of `DiagWalkSurvivorsWithType`. |
| `Born` and `Large` flags | `Alloc` and `AllocationComplete` | **Written and never read.** |
| `IsPinned` callback | engine | Read the flags. Used by the nursery copy decision, by `RetainPinnedMatureBlocks` (a walk over every registered object of every candidate block) and by one metric in `ProcessIncrement`. |
| `m_trace_locks`, `m_trace_scans` | trace | One lock byte and one pointer for every 8-byte slot (for large objects, every page). Committed when a trace starts, decommitted when it ends. |

### The design that was built (Code)

**The pin list** (`lxr_pin_list.h`). Each worker adds to its own shard. When the root capture returns, one call sorts the shards into one array and removes duplicates (`SealPins`). During the pause every thread reads the array without a lock. The end of the pause clears it (`ClearPins`, in `ReleaseEpoch`). The cost follows the number of pinned roots.

**The block byte** (`m_block_flags`, one byte per Immix block). Bit 1 (`PinnedBlockFlag`) tells that the block is a pinned block. Bit 2 (`PausePinFlag`) tells that this pause found a pinned root in the block. `AddPinnedRoot` sets bit 2. `ClearPins` clears bit 2 for the blocks of the list.

**The copy decision** (`IsPinnedObject`). Read the block byte. Byte 0: the object is not pinned. Bit 1: pinned. Bit 2 only: look the address up in the sorted array.

**Mature evacuation.** `RetainPinnedMatureBlocks` goes through the pin list. For each entry in a candidate block that has a count, it marks the block "retained". It does not walk objects any more. A block that `SelectMatureCandidates` finds with `PinnedBlockFlag` is never a candidate.

**Pinned blocks.** A new allocation source `MutatorPinnedFresh` takes a clean block and sets `PinnedBlockFlag`. Every other acquisition of the block clears the flag. A second source `MutatorPinnedRecycled` takes only Reusable blocks with the flag; every other recycled source skips them. One allocator for all threads (`m_pinned_allocator`, under a spin lock) serves the pinned object heap. The pause gives its block back like the allocator of a mutator.

**The copy list.** The walk of `DiagWalkSurvivorsWithType` needs the objects that the pause copied. The engine already keeps a forwarding record for each copy (`WalkForwarded`), so the walk reads the destinations from it and sorts them. `DidCopy` and the flag are gone.

**Trace locks and scans.** The lock protects the mark and the scan of one object (it lets a dying object finish its scan before its children are decremented). It is now one of 16384 spin locks, chosen by a hash of the object address (`TraceLockSlot`). The scan of a split object (a large array that is scanned in slices) is in a hash index with 4096 buckets (`FindTraceScan`, `LinkTraceScan`, `UnlinkTraceScan`, `MoveTraceScan`). A mature copy moves the entry to the new address with both bucket locks held. The fixed size is 16 KiB for the locks and 256 KiB for the index. Nothing is committed when a trace starts and nothing is decommitted when it ends.

### What each flag became (Code)

| Flag or table | Replacement |
|---|---|
| `Pinned` (pinned object heap) | Pinned block (bit 1 of the block byte) for small objects. The large-object space for large objects. |
| `ObservedPin` | The pin list and the pause bit of the block. |
| `CopiedThisPause` | The forwarding records of the engine, read by the survivor walk. |
| `Born` | Removed. It was never read. |
| `Large` | Removed. It was never read. `IsLargeObject` already tests the address range of the large-object space. |
| `m_trace_locks` | 16384 spin locks chosen by hash. |
| `m_trace_scans` | A hash index of the split scans. |
| `isPinned` and `didCopy` callbacks | Removed from the object model. The engine owns the list (`AddPinnedRoot`). |

### What the pause does with a pin (Code, Measured)

- A pinned root in the stack, in a pinned handle or in `fixed` code reaches `AddRoot` with `GC_CALL_PINNED`. An interior pointer is turned into the containing object first, so the list holds object starts.
- The increment of a pinned young object promotes its block in place. An object that the pause reaches later in the same block is **not** copied, because the block is not a clean nursery block any more. The old code did the same. The plan said "the unpinned neighbour is copied". It is copied only when the pause reaches it before the pin. The native test puts the pin last in the root order (Measured).
- At the end of the pause the bit of each pinned block is cleared and the list is empty.

### Check C4 and decision D4

**C4 (weak handle scans need no flag): true, and nothing changed (Code).** **D4 (option 1): built.** Point (e), the Pins metric: it counts the different pinned roots that the last pause saw (stack, handles, and the large objects among them). Objects of the pinned object heap do not count. They did not count in S6 either. `GetMemoryInfo` reports this number as pinned objects.

### Semantic differences (honest)

| Behavior | Before S7 | S7 |
|---|---|---|
| Where a small pinned-object-heap object lies | Any block that has room | A pinned block. A pause is not allowed to copy from it. |
| Cost of a pinned-object-heap allocation | The block allocator of the thread | One shared allocator under a spin lock. **Open:** not measured with many threads. |
| Memory of a pinned block | Same as other blocks | A pause gives back the block that the pinned allocator holds. A pinned block that is part full is reused only by pinned allocation. **Open:** a program with few pinned objects per pause and long-lived objects keeps one block per pause until the objects die. |
| Mature evacuation and objects with the header bit `BIT_SBLK_GC_RESERVE` | The block of such an object was retained | Not retained. No code in this collector sets that bit (only Satori does). The nursery copy decision still reads it. |
| Metric `Pins` | S6: pinned roots, unique | Pinned roots, unique, the same. |
| Trace lock of an object | Its own byte | A lock that two objects can share. A long scan of one array (the forced scan of a dying large array) can make another thread that needs the same lock wait. **Open:** not measured; the lock is held for one object scan only. |

### Results: native plane bytes and peak RSS (Measured)

Heap 4x, hard limit, two repetitions (matrix: `results/wall-table-s7.md`; traced runs: `results/trace-tables-s7.md`). `int` is the integrated tip (the S6 collector), `s7` is the S7 tip. Native planes are the maximum of metric 38 over the run. Both builds use the same runtime.

| Program | Mode | Native planes int (MiB) | Native planes s7 (MiB) | Peak RSS int (MiB) | Peak RSS s7 (MiB) |
|---|---|---|---|---|---|
| binary trees | non-moving | 212.0 | 160.0 | 185 | 136 |
| binary trees | nursery copy | 212.0 | 160.0 | 146 | 108 |
| requests | non-moving | 456.3 | 336.0 | 279 | 159 |
| requests | nursery copy | 456.3 | 336.0 | 280 | 159 |
| list suffix | non-moving | 212.0 | 160.0 | 274 | 225 |
| list suffix | nursery copy | 212.0 | 160.0 | 256 | 202 |
| pin-heavy | non-moving | 284.0 | 256.0 | 204 | 115 |
| pin-heavy | nursery copy | 284.0 | 256.0 | 160 | 115 |

The saving of native planes for a heap of 40 MiB (binary trees) is 52 MiB. **Inferred:** the flag table is 0.5 byte per byte of heap and the two trace tables are 1.125 byte per byte of heap (trace only). The sum for the Immix space of that run is about 57 MiB; the saving that I measured is a little lower. The planes that stay are 160 MiB for a heap of 32 MiB committed. **Inferred:** about 128 MiB of them are the four log streams (they are committed but not touched). I did not split the planes.

### Results: wall time and GC time (Measured; matrix, 2 repetitions, ms; mean and the two runs)

| Program | Mode | Wall int | Wall s7 | GC time int | GC time s7 |
|---|---|---|---|---|---|
| binary trees | non-moving | 2100 (2110, 2091) | 1961 (2008, 1914) | 439 | 438 |
| binary trees | nursery copy | 1950 (1751, 2149) | 1846 (1694, 1998) | 334 | 302 |
| requests | non-moving | 19607 (19491, 19723) | 15236 (15261, 15211) | 9775 | 6271 |
| requests | nursery copy | 7754 (7767, 7742) | 6620 (6566, 6675) | 1349 | 1078 |
| list suffix | non-moving | 12628 (13151, 12106) | 10289 (10111, 10467) | 6536 | 4466 |
| list suffix | nursery copy | 20714 (11934, 29494) | 14194 (17845, 10542) | 14752 | 8616 |

List suffix nursery copy is noisy (the two runs of one build differ by a factor of 2.5). Do not read a gain from that cell. The traced runs (a second set of runs) show the same direction in every cell (`results/trace-tables-s7.md`). Checksums are equal for every workload.

### Results: pauses (Measured; traced runs, 2 repetitions, stop-the-world time of the S0 timers)

Median RefCount pause (ms): binary trees non-moving 4.5 to 4.0, nursery 5.0 to 4.8; requests non-moving 5.1 to 5.2, nursery 12.8 to 11.7; list suffix non-moving 34.6 to 34.4, nursery 53.6 to 51.0. The pause medians of the RefCount pauses are equal inside the noise. Median InitialMark pause: binary trees non-moving 237.6 to 196.5 ms, nursery 308.3 to 147.8 ms; requests non-moving 104.5 to 19.5 ms, nursery 83.5 to 20.9 ms. Median Full pause of requests non-moving: 177.2 to 115.5 ms. **Inferred:** the pauses with a trace were shorter because they no longer commit and touch the two trace tables when a trace starts. The binary trees non-moving cell of the S7 build has no FinalMark pause in the two runs (the run ended while the trace was concurrent); `results/trace-tables-s7.md` has the full table.

### Results: the pin-heavy program (Measured)

The program (`pinheavy` in `GcTrace.cs`): four threads. Each one replaces pinned handles, runs `fixed` blocks that allocate, replaces pinned-object-heap arrays and allocates garbage (2,000,000 operations in all, 64 slots per thread). Heap 64 MiB, hard limit, traced. Four runs per cell on each build (two in the parallel benchmark streams, two serial); one run of each build in non-moving mode ended abnormally (section "A failure that is not new") and is not in the numbers.

| Mode | Wall int (ms) | Wall s7 (ms) | GC time int (ms) | GC time s7 (ms) | RefCount pause median int / s7 (ms) | Peak RSS int / s7 (MiB) | Pinned roots per pause s7 |
|---|---|---|---|---|---|---|---|
| non-moving | 4120 | 3932 | 610 | 551 | 2.2 / 2.2 | 204 / 115 | 249.9 |
| nursery copy | 3629 | 3727 | 319 | 316 | 3.8 / 3.7 | 160 / 115 | 248.7 |

The wall time of the nursery copy mode is 2.7% higher on s7. The four runs of each build overlap (3542 to 3726 and 3660 to 3800). I count it as noise. In the nursery copy mode both builds promoted about 107 and 109 MB of pinned objects in place. Checksums are equal.

### A failure that is not new (Measured)

When the machine is oversubscribed (eight parallel runs of the pin-heavy program on 16 cores), the program fails now and then with `LXR epoch publication did not precede the runtime barrier reset.` A debug build that stops at the first engine failure shows the cause: the root capture finds a root whose object has no ValidObject bit, no reference count and a null method table. The failure code is `InvalidArgument` from `AddRoot`. I saw the objects with root flags 0 and 2.

| Build | Run | Failed runs |
|---|---|---|
| integrated tip | with pinned-object-heap arrays, 8 loops of 10 | 12 of 80 |
| S7 tip | with pinned-object-heap arrays, 8 loops of 10 | 2 of 80 |
| integrated tip | without them (`PINHEAVY_NOPOH=1`) | 4 of 80 |
| S7 tip | without them | 8 of 80 |
| S7 tip | one run at a time, and two loops on pinned cores | 0 of 105 |

**Inferred:** it is not caused by S7 and not by the pinned object heap (the integrated tip fails as often, and the failure stays without these arrays). It looked like a thread that holds an object that the runtime has allocated but has not yet reported to the collector, when another thread starts a pause. **That guess was wrong.** The cause is a race in the block allocator, and it is fixed. See the section "The cause of the failure and its fix" below.

### Tests (S7)

Note: the failure that this section describes has a cause and a fix. They are in the section "The cause of the failure and its fix" and are not part of these S7 tests.

| Suite | Linux Release | Linux Debug | Windows Release |
|---|---|---|---|
| Native selectors (33: the 32 of S6 and `--s7-pins`) | pass 33 of 33 | pass 33 of 33 | pass 33 of 33 |
| New selector `--s7-pins` | pass 8120 | pass 8120 | pass 8120 |
| Managed cells with the Debug collector (3 modes; 63 runs of the S6 suite and 15 runs of the 5 new cells) | not run | pass 78 of 78 | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 | not run | not run |
| Build of `clr` and `libs` | pass (clr 3:23, libs 1:41, incremental) | pass (clr 5:55, libs 6:36) | pass (`build.cmd -s clr+libs`, 17:44) |
| `clrlxrgc` module | pass | pass | pass |

Files: `results/tests-native-release-s7.txt`, `tests-native-debug-s7.txt`, `tests-native-windows-release-s7.txt`, `tests-managed-s7.txt` (and `.csv`), `tests-legacy-s7.txt`. The older native selectors changed in two ways: the pin callback is now `AddPinnedRoot` in the capture, and the test that counts commit calls of a failed growth expects 23 and not 25 (two trace table commits are gone).

**Default collector unchanged (Measured).** The machine-code hash of the stock write barrier and allocation helpers (`helperhash.py`, as in P19 section 5) is `46cee8bd87fd6252f5265267148d0a34` on the S7 tree. It is the same value as for `dotnet/runtime` main and for the integrated tip. I did not change the runtime, so I expected it (`results/helper-hash-s7.txt`).

**New native checks (`--s7-pins`, 8120 checks).** Pin list: append, merge, duplicates, lookup and clear (`ValidateS7PinListUnit`); eight workers with 20000 adds each (`ValidateS7PinListConcurrent`). Nursery: a pinned object in a nursery block is not copied; the neighbours that the pause reaches first are copied; the block is promoted in place; inside the pause exactly one block has the bit and the list holds the pin; after the pause the bit is clear, the pause counted one pinned root; a pause without a pin sets no bit. Interior pointer into a pinned object. Four workers add the pins of 200 objects, each object twice, and 100 objects move. Pinned blocks: the pinned-object-heap objects lie only in pinned blocks and the others do not; a young pause and a full pause with mature evacuation do not move them; a block with no live object is released; an ordinary allocation that takes a released pinned block clears the flag; the holes of a part full pinned block go to pinned allocation only. Each of these checks runs for 2, 4 and 8 count bits. The mature tests of the older selectors cover "a mature block with a pin is retained, a block without a pin is evacuated" (`--mature`, `--mature-retained-owner`).

**Controls.** Each control is a compile define that breaks one rule. Each must make a selector fail (`controls-s7.sh`, `results/controls-s7-release.txt`).

| Control | What it breaks | Result |
|---|---|---|
| `LXR_S7_NO_DEDUPE_CONTROL` | the merge keeps duplicates | `--s7-pins` fails (8115 of 8120) |
| `LXR_S7_NO_CLEAR_CONTROL` | clear leaves the list | `--s7-pins` fails (8115 of 8120) |
| `LXR_S7_UNSAFE_ADD_CONTROL` | workers share a shard, no lock | `--s7-pins` ends with a segmentation fault |
| `LXR_S7_IGNORE_PIN_CONTROL` | the pause never knows a pin | `--s7-pins` fails (8081 of 8117) |
| `LXR_S7_NO_BLOCK_BYTE_CONTROL` | no pause bit is set | `--s7-pins` fails (8090 of 8117) |
| `LXR_S7_NO_BLOCK_CLEAR_CONTROL` | the pause bit stays | `--s7-pins` fails (8111 of 8120) |
| `LXR_S7_NO_RETAIN_CONTROL` | a pinned mature block is not retained | `--mature` fails (2085 of 2088), `--mature-retained-owner` fails (21 of 22) |
| `LXR_S7_PIN_INTERIOR_CONTROL` | the interior address is listed, not its object | `--s7-pins` fails (8114 of 8120) |
| `LXR_S7_POH_NORMAL_CONTROL` | pinned-object-heap allocation uses ordinary blocks | `--s7-pins` fails (8114 of 8126) |
| `LXR_S7_POH_RECYCLE_CONTROL` | other allocation may use the holes of a pinned block | `--s7-pins` fails (8118 of 8120) |
| `LXR_S7_SCAN_LINK_CONTROL` | the scan index is never filled | `--mature-reader` fails (46 of 48), `--mature-completed-scan` fails (7 of 8) |
| `LXR_S7_SCAN_MOVE_CONTROL` | the index entry does not follow a mature copy | `--mature-reader` fails (47 of 48; the new check of the index) |

**Managed cells** (`Pinning.cs`; run with a heap of 96 MiB initial and 128 MiB hard limit; natural collections only). `pin-handles-churn` (1500 pinned handles of many sizes replaced all the time; addresses and content checked), `fixed-pressure` (four threads, `fixed` pointers and interior pointers while garbage forces collections), `poh-churn` (300 long-lived and 120000 short-lived pinned-object-heap arrays; 1.2 GB through the pinned blocks under a hard limit of 128 MiB), `pin-finalizers-weak` (4000 finalizable owners of pinned buffers and weak references to the buffers) and `pin-concurrent-trace` (pins before and during a concurrent trace, then the final mark and a full collection). All five pass in the three modes. Managed controls (`controls-s7-managed.sh`, `results/controls-s7-managed.txt`; the module is built with the define): `IGNORE_PIN` (`pin-handles-churn` and `fixed-pressure` end with a fatal error), `NO_BLOCK_BYTE` (the same cells fail), `POH_NORMAL` (`poh-churn` ends with a fatal error) and `NO_RETAIN` (`pin-concurrent-trace` in mature mode ends with a fatal error). The other controls are detected only by the native selector: they change the pin list in a way that a managed program cannot see.

### What was reduced

The user asked for lean evidence. Small benchmark (two repetitions, heap 4x, two modes, no perf profiles). I did not run the Windows managed suite, the managed suite with the Release collector, or the older suites on Windows and in Debug.

### The cause of the failure and its fix

Labels as above. This section replaces the guess in "A failure that is not new".

**Short answers**

1. **Cause (Measured).** Two threads receive the same fresh 32 KiB block in the same epoch. The second thread clears the block, and the clear erases the objects of the first thread. The root capture of the next pause then finds a root (a pinned handle or a pinned local) whose object has no ValidObject bit and a null method table. `AddRoot` returns `InvalidArgument`, the engine goes to the failed state, and the runtime prints the misleading message "epoch publication did not precede the runtime barrier reset".
2. **It is not the allocation helpers (Measured).** My first idea was an object that the runtime returns before the collector publishes it. The dumps below show the opposite: the notification `AllocationComplete` of the failing object had finished before the pause. The object was erased later. The runtime is not the cause and I did not change it.
3. **Fix (Code, Measured).** One condition in `ImmixBlockManager::TryAcquire` (`immix_block.cpp`): a fresh mutator acquire now refuses a nursery block under the block lock. The unlocked check in `AcquireBlock` already refused it. It was not enough alone.
4. **Before and after (Measured).** Same machine, same program, same runtime, 8 parallel loops of 13 runs, two rounds, the two builds interleaved. Without the fix 32 of 416 runs failed (11 of 208 with pinned-object-heap arrays, 21 of 208 without). With the fix 0 of 416 failed. The accepted build `e8d2d23` fails too (5 of 120).
5. **It is a memory corruption, not only a crash (Inferred).** The crash needs a root capture that touches an erased object. A program that holds an erased object in a field does not need one. I did not measure silent corruption. Both threads also allocate objects in the same memory in the interval before the pause (Code).
6. **A hot path is not touched (Code).** The block acquire runs once for each 32 KiB block and the check is one more comparison under a lock that the call already holds. I ran no benchmark. The checksums of the pin-heavy program are equal on the fixed and the unfixed build (1472954008).

**How the cause was found (Measured).** I built a diagnostic collector with a ring of 1 million events (`diag-root-capture.patch`, define `LXR_ROOT_DIAG`). The events are: slow allocation, publication of a span, retirement of a span, `AllocationComplete`, suspension, start of a scan, acquire of a block, return of a block, clear of a block. At the first failing `AddRoot` the collector prints the object, its flags, the slot, the 256 bytes around it and every ring event that touches the same block, then it aborts. I ran the oversubscribed loop (8 or 12 loops of 10 runs, 16 cores) until it failed. The failure rate with the diagnostic build was 1 of 80 and 6 of 120.

All dumps have the same shape. The root has the kind "handle" (`rootkind=3`), the flags are 2 (pinned), and the 256 bytes around the object are zero. This is `diag-failure-3-unfixed.txt`, shortened. Times are before the failure. Block `…630000` is the block of the failing object.

| Time | Thread | Event |
|---|---|---|
| −5.44 ms | A | acquires the block as a fresh block (owner 6) and clears it |
| −5.43 ms | A | publishes a span and makes 5 fast allocations. Each ends with `AllocationComplete`. The last is the later failing object (`…630df0`, 0x490 bytes) |
| −5.37 ms | A | returns the block (it is full) |
| −0.39 ms | B | acquires **the same block** as a fresh block (owner 4) and clears the whole block |
| −0.12 ms | A | starts a pause |
| −0.09 ms | root scan | finds the pinned handle to `…630df0`: no ValidObject bit, method table null. `AddRoot` fails |

There is no pause between the return by A and the acquire by B. The file `diag-failure-2-unfixed.txt` shows the same order: B acquires the block 0.6 ms after A returned it.

**Why the allocator allows it (Code).**

1. `ImmixAllocationManager::AcquireBlock` picks a candidate. It reads the block state and the nursery flag with no lock (`GetStateFast`, `IsNurseryFast`). A fresh block that a thread took in this epoch is `Unallocated` and a nursery block, so this check refuses it.
2. `ImmixBlockManager::TryAcquire` then takes the block lock. It checked only that the state is `Unallocated` and that the block has no owner. The nursery check existed only for the kind `GcCopyFresh`.
3. A thread that is slow between step 1 and step 2 (the machine is oversubscribed) loses the order. Thread A takes the block, fills it and returns it. A returned fresh block has no owner and stays `Unallocated` until the pause. The slow thread B now passes the checks of step 2. `PrepareFreshBlock` of B clears the memory of the block (`memset`) and resets the ValidObject bits of the range.
4. The collector kinds are not open to this: a collector acquire changes the state to `Unmarked`, so a second acquire fails the state check. Recycled mutator blocks are checked under the lock (`reusing`). Only the fresh mutator kind had the gap.

The code is the same in the accepted build `e8d2d23` (`git show lxr:src/coreclr/gc/immix_block.cpp`). **Measured:** the accepted build fails with the same fatal error in 5 of 120 runs (`results/root-capture/other-runs.txt`). I did not run the diagnostic build on the accepted code, so for that build the same cause is **Inferred**.

**Fix (Measured).** `TryAcquire` refuses a fresh mutator acquire when the block is a nursery block of the current epoch, and answers `Stale`. `AcquireBlock` goes on to the next candidate, as it does for any other stale answer. In the next epoch the block is no longer a nursery block and can be taken again. The diagnostic build with the fix (`diag-denied-count.patch` counts the refusals) refused one acquire in each of 8 of 120 runs, and none of the 120 runs failed. So the race is real in this program: about 7% of the runs meet it.

**Before and after (Measured).** `rcf-before-after.sh`; results in `results/root-capture/before-after-summary.txt`. Release builds of the collector from the same sources, with and without the fix. Loop: 8 parallel runs of the pin-heavy program (`pinheavy 64`, heap 64 MiB, natural collections, non-moving mode) on 16 cores. One round is 104 runs for each arm. Two rounds.

| Arm | Round 1 | Round 2 | Total |
|---|---|---|---|
| Without the fix, with pinned-object-heap arrays | 7 of 104 | 4 of 104 | 11 of 208 |
| With the fix, with pinned-object-heap arrays | 0 of 104 | 0 of 104 | 0 of 208 |
| Without the fix, ordinary arrays (`PINHEAVY_NOPOH=1`) | 11 of 104 | 10 of 104 | 21 of 208 |
| With the fix, ordinary arrays | 0 of 104 | 0 of 104 | 0 of 208 |

Of the 32 failures without the fix, 30 end with "Fatal error. Error 2147500037 (0x80004005 (E_FAIL))". Two end with a core dump and no message. **Inferred:** the two have the same cause. I did not look at them. **Not shown:** that the fix removes every failure of this kind. Zero in 416 runs gives a rate below about 1% at 95% confidence, and the rate without the fix is about 8%.

**Tests (Measured).**

- Native selector `--fresh-block-race` (10 checks). It uses a test hook (`SetAfterPrecheckHookForTest`, only in the test platform) that runs between the unlocked check and the locked acquire. In that window the test plays thread A: it takes the candidate block, writes a pattern into all 32 KiB, and returns the block. Then the allocating thread B continues. The test checks that B does not get that block, that the pattern is intact, that a direct locked fresh acquire of the block answers `Stale`, and that after an epoch flip the block can be taken again. The test is deterministic.
- **Control** `LXR_FRESH_NURSERY_RACE_CONTROL` (`controls-rcf.sh`, `results/root-capture/control-fresh-block-race.log`): the locked check is the old one. Result: `--fresh-block-race` fails (7 of 10 checks pass). B takes the block and erases the pattern.
- The managed side has no new cell. The failure needs a pause at a bad moment and it shows in about 8% of the runs of a 4-thread program on an oversubscribed machine. A managed cell would not be reliable. The loop `rcf-stress.sh` is the managed check, and `rcf-before-after.sh` ran it.

| Suite | Linux Release | Linux Debug | Windows Release |
|---|---|---|---|
| Native selectors (33, the 32 of S7 and `--fresh-block-race`) | pass 33 of 33 | pass 33 of 33 | pass 33 of 33 |
| New selector `--fresh-block-race` | pass 10 | pass 10 | pass 10 |
| Managed cells with the Debug collector (3 modes; 78 runs as in S7) | not run | pass 78 of 78 | not run |
| Older suites P2.1 to P3.4 (9 scripts) | pass 9 of 9 (P2.2 after a test update, see below) | not run | not run |
| Build of `clrlxrgc` | pass | pass | pass |

Files: `results/tests-native-release-rcf.txt`, `tests-native-debug-rcf.txt`, `tests-native-windows-release-rcf.txt`, `tests-managed-rcf.txt`, `tests-legacy-rcf.txt`, `tests-legacy-rcf-first-run.txt`.

**One older test changed (Measured).** The first run of the older suites failed in P2.2 (2205 of 2206 checks, "neighboring block updates all win"). The test acquires `DataStart` in an epoch, resets the owner of the block by hand, and acquires the block again in the same epoch. Since the fix, a fresh mutator acquire refuses a block that is a nursery block of the current epoch, and this block was one. The product cannot reach this state: the unlocked check in `AcquireBlock` refused such a block before the fix as well, and the pause releases nursery blocks in a later epoch. I changed the test to reset the epoch byte of the block too (one check added: 2207 of 2207). **Inferred:** no other code depends on the old behavior of `TryAcquire`, because the engine calls it only through `AcquireBlock`. The Windows build of this test (`MSVC`) and the P2.2 script on Windows were not run. The Windows `clrlxrgc` build is the Release module (`build-windows.ps1`). I did not run the Windows Debug build, the Windows managed suite, or the managed suite with the Release collector. The first compile of the Linux Release native program was killed by a hangup of the shell (a `clang` message, not a code error). The second compile passed.

**Default collector unchanged (Code).** The fix is in `src/coreclr/gc/immix_block.cpp`. The test hook is in `immix_allocation.h` and `immix_allocation.cpp` under `LXR_SIDE_METADATA_TEST_PLATFORM`, so the product build does not contain it. I did not change the runtime (`src/coreclr/vm`, the JIT, CoreLib). So the machine-code hash of the stock helpers (`46cee8bd87fd6252f5265267148d0a34`) cannot change. I did not run `helperhash.py` again.

**Open items (root capture).**

| Item | Label |
|---|---|
| Silent corruption by the same race before the fix (an erased object that no root capture touches) | Inferred; not measured |
| The two failures without a message (core dump) | Open; same cause Inferred |
| The same cause in the accepted build `e8d2d23` | Inferred (same code, same failure signature and rate) |
| The engine reports this as a generic fatal error with a message about epochs. A message that names the failing root would make the next failure easier to read | Open; not changed |
| Other places where an unlocked check is the only guard of a block state | Open; I looked only at `AcquireBlock` and `TryAcquire` |
| The kind `GcMatureEvacuationFresh` has no nursery check in `TryAcquire`. It is safe because its acquire changes the state to `Unmarked` | Code; not tested for this race |

The files of this investigation are in the table "Files in this folder": `rcf-stress.sh`, `rcf-before-after.sh`, `controls-rcf.sh`, `diag-root-capture.patch`, `diag-denied-count.patch` and `results/root-capture/`.

### Not measured, open items (S7)

| Item | Label |
|---|---|
| The failure of the pin-heavy program under CPU oversubscription: the place of the cause | Found and fixed (section "The cause of the failure and its fix") |
| The shared pinned allocator and its lock with many threads that allocate pinned-object-heap arrays at the same time | Open (not measured) |
| A part full pinned block for each pause when a program pins few long-lived small objects | Open (Inferred from the code; no program measured) |
| The remaining native planes (160 MiB for a heap of 32 MiB): the share of the four log streams | Inferred, not split |
| The trace locks: two objects can share a lock. Cost of a wait behind a forced scan of a very large array | Open |
| Windows performance and the Windows managed suite | Not measured |
| The `BIT_SBLK_GC_RESERVE` bit is not honoured by mature retention any more (no code sets it) | Code | in a second worktree; S1 on the final tree. "Pass" lists only what ran.

| Suite | S0 (tested as `1bf696c`) | S5a (tested as `0a57635`) | S1 (final tree) |
|---|---|---|---|
| Linux native, 25 selectors, Release | pass | pass | pass |
| Linux native, 25 selectors, Debug | pass | pass | pass |
| Linux managed scope-5 fixture, 84 cells (3 modes, 2 native configs, 2 managed configs, full + finalizer + dependent + 8 lifecycle cases) | pass | pass | pass |
| Linux older suites P2.1 to P3.4 | pass (all 9) | pass (all 9) | 8 pass; P3.3 failed once (see notes) |
| Windows `clrlxrgc` Release build | pass | pass | pass |
| Windows native, 25 selectors, Release | pass | pass | pass |
| Windows native, 25 selectors, Debug | not run on the commit (the first S0 version passed) | not run | pass |
| Windows managed, 84 cells | not run on the commit (the first S0 version passed) | not run | pass |
| Windows `clrlxrgc` Debug build | built for the first S0 version and for S1 | not run | pass |

Notes:

- S0 and S5a were tested on their own commits in a second worktree. S1 was tested on the final tree. Linux Debug and Release both means: the native program is compiled in that configuration, and the managed fixture runs with both a Debug and a Release collector module.
- Windows native selectors compile 11 collector sources with MSVC and run the same 25 selectors; they do not include `lxr_gc.cpp`. The Windows `clrlxrgc` build covers `lxr_gc.cpp`.
- **P3.3 (`lxr-deferred-bucket-validation`) failed with 356 of 357 checks ("publication accounts active packets") once on a rebuilt accepted tree `e8d2d23` and once on the S1 tree. Both ran while a benchmark matrix used the machine.** The same binaries pass 10 of 10 runs when run alone (accepted, S0 and S1). The S0 and S5a suite runs passed (357 of 357). **Measured:** the test is intermittent. **Inferred:** it depends on timing under CPU load, and it is not caused by this work. I did not investigate it.
- The first version of the allocator change failed three native selectors (Finding 3). Those are the tests that caught it.
- The native suites contain the new checks for the reserve, the Immix-free trigger and the epoch gate. The P2.2 suite has the positive control for the gate counters.
- The first S0 version (with trace-packet timing) and the S5a build without the allocator change were also tested and passed. They are not the commits in the branch.

**Follow-up commit (tested on the working tree that was then committed, Measured):**

| Suite | Result |
|---|---|
| Linux native, 25 selectors, Release and Debug | pass; `flip_overlaps=0 flip_blocked=0` in every selector; `--p8-policy` has 62 checks (3 new), the engine checks include 2 new ones |
| Linux managed scope-5 fixture, 84 cells (Release and Debug collector) | pass |
| Linux older suites P2.1 to P3.4 | pass (all 9, P3.3 357 of 357) |
| Windows `clrlxrgc` Release build | pass |
| Windows native, 25 selectors, Release and Debug | pass |
| Windows managed, Windows Debug build | not run |

- **P3.3 intermittent failure: Open, not investigated (decision of the coordinator).** Evidence: 356 of 357 ("publication accounts active packets") failed once on the rebuilt accepted tree `e8d2d23` and once on the S1 tree, both while a matrix loaded the machine. The same binaries passed 10 of 10 runs alone (accepted, S0, S1). The S0, S5a and follow-up suite runs passed 357 of 357.

## Not measured, open items

| Item | Label |
|---|---|
| Windows performance of any step | Not measured |
| Cause of the 200 ms without a timer in `listsuffix` RefCount pauses | Open (Inferred: waiting for trace slices) |
| `requests` 2x nursery and mature: finishes with the accepted build, not with the follow-up (cliff of free lines, Finding B). Needs a design decision | Open |
| `binarytrees` 2x, `listsuffix` 8x: no regression after the follow-up (the first S5a numbers were the storm and noise) | Measured |
| Windows managed suite and Windows Debug build for the follow-up | Not measured |
| Why `listsuffix` 4x nursery is faster (collection time 20.7 s against 38.3 s) while the traced pause total is not smaller | Open |
| `listsuffix` 4x mature: 1.07, one run lost to an allocation failure at the hard limit | Open (one cell, noise level) |
| P3.3 intermittent failure | Open (evidence in Tests) |
| Share of (a) and (b) in the S1 gain | Open |
| The recycled-block scan skip is a stopgap. The real fix is a counter of reusable blocks (P16 S4 follow-up) | Inferred. S4 kept the stopgap and made its token count the blocks made `Reusable` (section "S4"). |
| The decision D2 of P16 (to-space beyond the admitted heap, S5b) still stands. S5a costs about 5% of the Immix space as free reserve at the trigger, and more pauses | Measured. S5b implements D2 for the no-hard-limit mode (section "S5b"); the hard-limit mode keeps the S5a reserve. |
| 5% minimum ratio and factor 2 are not tuned. The sensitivity table shows no gain from larger values with the Immix-free comparison, on one program | Measured, one program |

## Files in this folder

| File | Use |
|---|---|
| `build-linux.sh`, `build-windows.ps1` | Build `clrlxrgc` and make a runnable frame or module |
| `native-linux.sh`, `native-windows.ps1` | Compile and run the native validation, all selectors |
| `run-managed.py` | Managed scope-5 qualification with a replaced collector module |
| `suite-chain.sh`, `legacy-linux.sh` | Native + managed chain; older P2.x and P3.x suites |
| `run-matrix.py`, `matrix-parallel.sh`, `summarize.py`, `compare.py` | The matched benchmark in three pinned parallel streams, medians, paired ratios |
| `GcTrace.cs`, `GcTrace.csproj`, `trace-runs.sh`, `trace-final.sh`, `phases.py`, `pause-table.py` | Per-collection tracer and its tables |
| `run-one.sh`, `short-run.sh`, `sensitivity.sh`, `perf-run.sh` | Single runs, reserve sensitivity, `perf` sampling |
| `full-suite.sh` | Build Release and Debug, then native, managed and older suites for one source tree |
| `dg.sh`, `dgsum.sh`, `dgcmp.sh`, `dgcum.sh` | Runs and tables for the debug build with per-pause prints (follow-up) |
| `diag-and-experiments.patch` | The debug prints and the environment switches of the follow-up experiments (`LXR_DIAG`, `LXR_NOCOPY`, `LXR_NORESERVE`, ...). Not part of the collector. Apply on the follow-up commit only for the experiments. |
| `perf-three.sh`, `perf-mature.sh`, `perf-report.sh`, `perf-callers.sh`, `perf-annotate.sh`, `perf-shares.py` | S2: `perf` recordings of the three workloads (nursery and mature mode), self and inclusive tables, callers, hottest instructions, share of the checked functions |
| `trace-s2.sh`, `pause-compare.py`, `wall-table.py`, `s2-tables.sh` | S2: traced runs of the three builds, side-by-side pause and wall-time tables, all result tables |
| `repeat-cell.sh`, `repeat-trace.sh`, `catch-fatal-s2.sh`, `catch-fail-site.sh`, `extract-catches.sh` | S2: repeat runs and `gdb` catches for the intermittent engine failure |
| `trace-loop.sh` | Fix: repeated traced `listsuffix` 4x nursery runs, to count the failure of the lazy large-object release. The native selector `--lazy-large-release` (in `native-linux.sh` and `native-windows.ps1`) reproduces the failure in one deterministic run. `results/lazy-large-release-diagnostics.txt` holds the diagnostic output of the root-cause search. |
| `results/` | Small result files (summaries, comparisons, pause tables, raw matrix runs). S2: `summary-s2.md`, `wall-table-s2.md`, `compare-*-s2-*.md`, `pause-compare-s2.md`, `perf-shares-s2.md`, `perf-s2/`, `matrix-s2-acc-l1-s2.jsonl`, `failure-sites-*.txt`, `repeat-*`. S3: `matrix-s3-acc-s2-s3.jsonl`, `matrix-s3-first-with-segv.jsonl`, `summary-s3.md`, `wall-table-s3.md`, `compare-*-s3-*.md`, `trace-tables-s3-x4.md`, `trace-runs-s3.txt`, `perf-s3/`, `tests-*-s3f.txt` |
| `trace-s3.sh`, `s3-tables.py`, `s3-wall-table.py` | S3: traced runs of the accepted build, the S2 tip and the S3 build; pause, phase and plane tables; wall-time and RSS table |
| `trace-s4.sh`, `s4-tables.py`, `s4-wall-table.py`, `phase-all.py`, `by-trace-phase.py`, `build-tracer.sh` | S4: traced runs of the accepted build, the S3 tip and the S4 build (the S4 tracer prints `resume_gap` and `publish_trace`); pause and phase tables; wall-time, collection-time and RSS table; all phases of one cell side by side; pause time split by trace phase |
| `perf-s4.py`, `native-one.sh`, `controls-s4.sh` | S4: shares of the sweep-related functions in `perf` recordings (`perf-three.sh` and `perf-report.sh` make the recordings); one native selector with extra compiler flags; the six S4 controls |
| `results/` (S4) | `matrix-s4-acc-s3-s4.jsonl`, `summary-s4.md`, `wall-table-s4.md`, `compare-*-s4-*.md`, `extra-listsuffix-4x-nursery-s3-s4.jsonl`, `trace-tables-s4-x4.md`, `trace-s4/`, `perf-s4/`, `perf-shares-s4.md`, `controls-s4-release.txt`, `tests-*-s4.txt` |
| `run-nolimit.sh`, `smoke-nolimit.sh`, `trace-s5b.sh`, `s5b-tables.py`, `s5b-wall-table.py`, `controls-s5b.sh` | S5b: one run with no hard limit; a short smoke run; traced runs of the accepted build, S4, S5b and S5b with overshoot off; borrow and cost tables; wall-time and RSS table for any builds; the 12 controls. `run-matrix.py --no-hard-limit IMMIX_MB,LOS_MB` runs the matrix without a hard limit. `GcTrace.cs` has two new phases, `borrow` and `give_back`. |
| `results/` (S5b) | `matrix-s5b-hl-s4-s5b.jsonl`, `wall-table-s5b-hl.md`, `matrix-s5b-nl-acc-s4-off-s5b.jsonl`, `wall-table-s5b-nl.md`, `trace-tables-s5b.md`, `trace-s5b/`, `intermittent-failure-s5b.txt`, `controls-s5b-release.txt`, `tests-*-s5b.txt` |
| `FinalizerLag.cs`, `FinalizerLag.csproj`, `lag1.sh`, `lag-matrix.sh`, `lag-table.py`, `lagfmt.py` | S6: the finalization-delay program (natural collections; object timestamp; peak RSS; tracing pauses), one run, the matrix of configurations, the table |
| `trace-s6.sh`, `s6-tables.py`, `perf-s6.sh`, `perf-s6.py`, `extra-cells.sh` | S6: traced runs of the accepted build, the S5b tip and S6; pause and phase tables; perf recordings and shares; extra repetitions of noisy cells |
| `cell1.sh`, `cells.sh`, `s6cells.sh`, `rep.sh`, `s6-cells-table.sh`, `tests6.sh` | S6: one managed cell on one build; the S6 cells with their heap; repeat a cell; the S6 cells on three builds; Release and Debug builds and all suites for one tree |
| `controls-s6.sh`, `controls-s6-managed.sh` | S6: the native controls and the managed controls (module built with a control define) |
| `controls-s7.sh`, `controls-s7-managed.sh` | S7: the 12 native controls and the 4 managed controls (module built with a control define) |
| `bench-s7.sh`, `bench-wall-s7.sh`, `bench-pinheavy-serial.sh`, `build-tracer-s7.sh`, `s7-tables.py` | S7: traced runs and wall matrix of the integrated tip and the S7 tip; serial runs of the pin-heavy program; the tracer build (workload `pinheavy` in `GcTrace.cs`); the tables |
| `results/` (S7) | `matrix-s7-int-s7.jsonl`, `wall-table-s7.md`, `trace-tables-s7.md`, `trace-s7/`, `helper-hash-s7.txt`, `controls-s7-release.txt`, `controls-s7-managed.txt`, `tests-*-s7.txt`, `tests-managed-s7.csv` |
| `rcf-stress.sh`, `rcf-before-after.sh`, `controls-rcf.sh`, `diag-root-capture.patch`, `diag-denied-count.patch` | Root capture failure: the oversubscribed pin-heavy loop (counts failures, keeps the output of each failed run); the interleaved before and after runs; the control for the native selector `--fresh-block-race`; the diagnostic ring (apply on the S7 tip, build with `-DLXR_ROOT_DIAG`; it aborts at the first failing `AddRoot` and prints the events of the block); the counter of refused acquires (apply on the fixed tree after the first patch) |
| `results/root-capture/`, `results/tests-*-rcf.txt` | Root capture failure: `before-after-summary.txt`, `other-runs.txt`, three diagnostic dumps of failures without the fix, `control-fresh-block-race.log`, test results of the fix |
| `results/` (S6) | `matrix-s6.jsonl`, `matrix-s6x.jsonl`, `wall-table-s6.md`, `wall-table-s6-vs-acc.md`, `trace-tables-s6.md`, `trace-s6/`, `perf-shares-s6.md`, `perf-s6/`, `lag-s6.jsonl`, `lag-table-s6.md`, `controls-s6-release.txt`, `controls-s6-managed.txt`, `s6-cells-table.txt`, `tests-*-s6.txt` |
