# P28: allocation refill and RC loops, as in the Java LXR (F9 and F10)

Status: product changes in 8 local commits (`F9:` and `F10:`). Nothing was pushed. No pull request was opened.
Branch: `kkokosa-lxr-f9-f10-parity`, made from `kkokosa-lxr-scheduler-f2-f4b-f8` (tip `e05f55b959e`). No other branch was touched
(`lxr` e8d2d23 and `origin/lxr-gc` e05f55b959e are unchanged).

Rule of this work (from the user): the goal is the same behavior as the Java LXR. I did not invent my own designs. For each change I read
the Java code first, then I made our code do the same thing in the same way. Where .NET forces a difference, I kept the difference
and I wrote it down as **forced by .NET**, with the reason. Ideas that I think could be faster than the Java way are not done.
They are in section 8 (**ideas not taken**).

Labels: **Measured** = I ran it and saw the result. **Code** = I read it in the source (file and function are named).
**Inferred** = I conclude it from other facts, and it can be wrong. **Open** = not known, or not done.

Words used here:

- **the Java LXR** = the LXR collector of MMTk in OpenJDK: mmtk-openjdk `0682434ae725`, mmtk-core `304ce69d43aa`. I read the source of exactly
  these versions (exported with `git archive`). Files that I read: `policy/immix/immixspace.rs`, `block_allocation.rs`, `block.rs`, `line.rs`,
  `util/alloc/immix_allocator.rs`, `util/rc.rs`, `plan/lxr/rc.rs`, `plan/lxr/cm.rs`, `args.rs`, `util/heap/blockpageresource_nosweep.rs`,
  `plan/lxr/global.rs`. I did **not** read the OpenJDK side that zeroes object memory.
- **Refill** = the allocator gets a new range of memory for the thread buffer (a free block, or a hole in a used block).
- **Hole** = a run of free lines in a block. **Line** = 256 bytes. **Block** = 32 KiB (128 lines).
- **Count** = the reference count of an object (RC table). **Unlog / mark / ValidObject / straddle** = the other side metadata tables.
  See P16 and P22.
- **Pause** = the time when the program threads are stopped. **RC pause** = a pause that does only reference counting work.
  **InitialMark / FinalMark** = the pauses that start and end a concurrent trace. **Full** = a pause that traces the whole heap.
- **Control** = a deliberately broken variant of the code (a build define). The test must fail on it.
- **x WKS** = ratio of the median to .NET Workstation GC in the same session. **CPU** = process CPU time (user + system). **Wall** = elapsed time.
- **Compare Java and .NET only through ratios. This document has no Java time.**

## 1. Result in short

All numbers are **Measured** on WSL (16 logical cores), Linux Release, ReadyToRun on, heap 4 times the smallest WKS heap, 3 repetitions,
natural collections only, one run at a time, nothing else running. All checksums are equal in all runs and all cells.

### 1.1 Wall time and CPU

Two builds ran in one session (`scripts/bench-all28.sh`): `base` = the P26 final (the start of P28), `final` = the tip of this branch.
The column `P26 text` holds the numbers that the P26 README gives (another session; only the ratios are comparable).

| cell | P26 text: wall s / x WKS / CPU x WKS | base wall s | base x WKS | base CPU x WKS | final wall s | final x WKS | final CPU s | final CPU x WKS |
|---|---|---|---|---|---|---|---|---|
| binary trees, non-moving | 0.49 / 1.3 / 2.4 | 0.48 | 1.4 | 2.6 | 0.46 | 1.3 | 0.71 | 2.8 |
| binary trees, nursery | 0.62 / 1.7 / 5.1 | 0.58 | 1.7 | 4.9 | 0.49 | 1.4 | 0.86 | 3.4 |
| requests, non-moving | 3.59 / 1.8 / 2.4 | 3.33 | 1.9 | 2.7 | 2.61 | 1.4 | 2.92 | 1.7 |
| requests, nursery | 3.73 / 1.9 / 3.0 | 3.59 | 2.1 | 3.4 | 2.64 | 1.4 | 3.19 | 1.8 |
| list suffix, non-moving | 3.49 / 0.8 / 2.4 | 3.22 | 0.8 | 2.7 | 2.45 | 0.6 | 7.46 | 1.9 |
| list suffix, nursery | 4.28 / 1.0 / 3.1 | 4.38 | 1.2 | 3.6 | 2.78 | 0.7 | 7.63 | 1.9 |

WKS in the final session: 0.34 / 1.87 / 3.86 s wall (binary trees / requests / list suffix) and 0.25 / 1.74 / 3.92 s CPU.
WKS in the base session: 0.34 / 1.71 / 3.81 s wall. The P26 session had 0.37 / 1.99 / 4.28 s.
Files: `results/s28-final-bench.out`, `results/s28-base-bench.out`, `results/p26-final-bench.out`, `results/s28-*-matrix.jsonl`.

What the table shows (**Measured**):

- Requests and list suffix: wall time is 22 to 37 percent lower than in the base build. CPU is 27 to 45 percent lower.
- Binary trees: wall time is 4 to 16 percent lower. The nursery arm CPU went down from 1.24 to 0.86 s. The non-moving CPU did not change
  (0.67 to 0.71 s; this is inside the noise).
- The base-to-final change is the effect of 8 commits. Section 2 gives the effect of each step.
- Binary trees in the nursery arm still have the highest CPU ratio (3.4 x WKS). Binary trees run only 0.46 s,
  so start-up and 19 pauses are a large share. **Open:** I did not find the reason.

### 1.2 Pauses, Full counts, CPU against wall

Full pauses: 0 in all 12 cells of both builds (`results/s28-*-bench.out`, column `full`).

Median of the pause time per kind, from one traced run per cell (`pausemed.py`; the field is `total_us`, the time of the whole collection call).
The wall time of a traced run is not a result.

| cell | base RefCount ms | final RefCount ms | base InitialMark ms | final InitialMark ms | base FinalMark ms | final FinalMark ms |
|---|---|---|---|---|---|---|
| binary trees, non-moving | 4.51 | 4.07 | - | 7.13 | - | - |
| binary trees, nursery | 5.84 | 4.68 | 15.50 | 12.62 | - | - |
| requests, non-moving | 7.77 | 6.45 | 7.30 | 6.81 | 8.55 | 6.60 |
| requests, nursery | 14.09 | 6.79 | 14.66 | 7.15 | 14.91 | 8.08 |
| list suffix, non-moving | 16.49 | 12.78 | 21.70 | 13.78 | 21.05 | 14.07 |
| list suffix, nursery | 24.06 | 12.40 | 81.61 | 23.00 | 27.90 | 16.92 |

`n` of the pauses is 16 to 75 for RefCount and 1 to 2 for InitialMark and FinalMark (`results/s28-*-pauses.txt`). Medians of 1 or 2 pauses are weak.
CPU against wall: list suffix uses 7.5 s CPU in 2.5 to 2.8 s wall (3 trace and RC workers). Requests uses 2.9 to 3.2 s CPU in 2.6 s wall.

### 1.3 Share of the named symbols (perf)

**Measured.** `perf record -F 499 -e cpu-clock --call-graph fp`, one run per cell, nursery arm. The share is the self share of all samples.
The total number of samples is not equal in the two builds (the final build uses less CPU), so the table gives the sample count too
(count = share x total). Files: `results/perf/`.

| symbol | list suffix base | list suffix final | requests base | requests final |
|---|---|---|---|---|
| all samples | 6789 | 4110 | about 2.8 thousand (from the CPU time) | 1762 |
| `TryCopyNursery` | 18.4 % (1252) | 12.3 % (506) | 6.8 % | 4.7 % (82) |
| `DecrementWithDeathCore` | 10.0 % (676) | 18.9 % (776) | 0.6 % | 1.1 % (20) |
| `ProcessIncrement` | 9.6 % (648) | 5.4 % (221) | 3.2 % | 1.5 % (26) |
| `IsNurseryFast` | 7.6 % (518) | 0.7 % (30) | 1.1 % | 0.2 % (3) |
| `LxrGcHeap::Alloc` (self) | 0.1 % | 0.3 % | 0.4 % | 0.2 % |
| `LxrGcHeap::Alloc` (with callees) | 5.4 % | 6.7 % | 21.2 % | 23.8 % |
| `ResetRangeQuiescentCore` | 10.1 % (685) | 0.0 % (0) | 6.0 % | 0.3 % (6) |
| `AcquireBlock` | 0.9 % | 0.1 % | 8.5 % | 0.6 % |
| `IsLineDead` (now `TryFindNextHole`, word reads) | 1.5 % | 0.9 % | 4.1 % | 2.0 % |

What it shows:

- **Measured:** `ResetRangeQuiescentCore`, `AcquireBlock` and `IsNurseryFast` are almost gone. The sample count of `TryCopyNursery` is 60 percent lower
  in list suffix. `ProcessIncrement` is 66 percent lower.
- **Measured:** `DecrementWithDeathCore` did **not** get cheaper in list suffix (676 to 776 samples). It is now the largest function
  (cache misses on the count table and the object; the code of this function was not changed). Its share is high because other work went away.
  **Open:** the Java LXR code for this loop is the same shape. I did not find a way to make it faster that the Java LXR already uses.
- **Measured:** in requests, `LxrGcHeap::Alloc` with callees is 21 to 24 percent in both builds, but its callees changed:
  `TryRefill` went from `AcquireBlock` and `ResetRangeQuiescent` to `PrepareRecycledHole` and `TryFindNextHole`.
  The memset of the data memory (libc `memset`, 8 percent) and the kernel spin lock (11 percent) are now the top self entries.
  **Inferred:** the memset is the zeroing of the data memory at the refill; see section 5 (forced by .NET).

## 2. What changed, step by step

| # | commit | step | **Measured** effect (A/B, wall s, median of 3, same session; `results/ab-*.txt`) |
|---|---|---|---|
| 1 | `fed689f2b07` | F9: hole search by words, byte fills, no RC/straddle clear at refill | requests non-moving 3.52 to 3.01, nursery 3.69 to 3.46; list suffix 3.57 to 3.24 and 4.11 to 3.88; binary trees none |
| 2 | `d824b6c4f51` | F10: SeqCst load is an acquire load | list suffix non-moving 3.27 to 2.82 (CPU 10.8 to 7.9 s), nursery 4.05 to 3.30 (GC 1.62 to 1.04 s); requests 3.19 to 2.82 and 3.56 to 3.13 |
| 3 | `51bd7c1b6fe` | F9: forward-only block cursors, reset at the pause | requests nursery 3.00 to 2.81 (CPU 4.4 to 3.7 s); `AcquireBlock` 9.2 to 0.3 percent of samples; others none |
| 4 | `c9e274efa6a` | F10: byte CAS for counts; in-place unlog by lines | list suffix nursery 3.17 to 2.91, non-moving 2.58 to 2.48 (8-bit counts); requests none |
| 5 | `2ba8e86b092` | F10: default count width 2 bits | list suffix non-moving 2.48 to 2.32, nursery 2.91 to 2.86; requests 2.69 to 2.63 and 2.81 to 2.74; binary trees none |
| 6 | `5effc8cdc46` | F10: tests for 2-bit counts, one managed test fixed | (tests only) |
| 7 | `2e782cd7913` | F10: RC loops (prefetch, inline counted children, skip dead children, copy destination, per-worker counters) | prefetch alone: none (list suffix nursery 2.85 against 2.88 s, requests nursery 2.75 against 2.68 s); loop changes D1 to D4: none (5 repetitions: list suffix nursery 2.87 against 2.95 s, requests nursery 2.82 against 2.82 s); per-worker counters: binary trees nursery CPU 1.41 to 0.89 s, requests nursery CPU 3.66 to 3.28 s, list suffix nursery GC 0.89 to 0.77 s |
| 8 | `47d58a6a401` | F10: one test line made MSVC /W4 clean | (tests only) |

Noise: **Measured** differences below 0.1 s wall are inside the run-to-run noise (the three runs of one cell differ by up to 0.15 s).
A 3-way run (`results/ab-f10b.txt`) used the same binary with `DOTNET_LXRRCBits=8` and with the default (2) to isolate step 5 from step 4.

The order of the steps is the order of the work. Step 2 is an F10 step (memory order), but it was the first large win, and it needed
no Java-specific design (see 4.1).

## 3. F9: the refill, as in the Java LXR

### 3.1 The basics

A mutator thread allocates by moving a pointer ("bump") in its own buffer. When the buffer is full, the thread asks for a new buffer.
This is the refill. The refill has three jobs: find free memory, make the metadata of that memory ready, and hand the range to the thread.
If the refill is slow, every allocation path is slow. P24 found that our refill was slow:
`AcquireBlock` 8.5 percent and `ResetRangeQuiescentCore` 6.0 percent of the requests nursery samples (base build, 3.1 above).

### 3.2 The Java LXR code (**Code**)

- `immixspace.rs: rc_get_next_available_lines(copy, search_start)`. It reads the RC table of the block with `RCArray::is_dead(i)` for the lines from the
  search line. The first dead line of a hole is skipped when the line before it is live (the "conservative" rule: `find_free_line`), unless it is line 0.
  Then it reads forward until the first live line. The range must be at least `min_reuse_lines`; if not, it searches again from the end of that range.
  So the search reads only the lines from the search start to the end of **one** hole. Nothing else is read.
- On a found range it clears the **unlog** table (or sets it to "unlogged" for a copy), adds to a counter, and **only while the concurrent trace runs** (`cm_in_progress_or_final_mark`)
  it sets the mark table to marked and increments the line reuse counts. It does **not** clear the RC table and it does **not** clear the straddle table.
  **Code:** the Java LXR has a straddle table too (`util/rc.rs`: `RC_STRADDLE_LINES`, one byte per line; `mark_straddle_object` at promotion of an object larger than one line,
  `unmark_straddle_object` when the object dies). It also sets the count of the first word of such a line to 1, so the line looks live to `is_dead`.
- `line.rs: RCArray::is_dead(i)` reads the RC table with plain loads. It reads whole words (the table is 2 bits per 8 bytes, so one line is 64 bits).
- `block_allocation.rs: initialize_new_clean_block` and `immixspace.rs: get_clean_block` prepare a fresh block. `blockpageresource_nosweep.rs` holds the block cursors.
  The cursors move forward only. They are reset at each pause (`prepare_gc`, `reset`). A recycled-block candidate is any block with state not `Unallocated`
  and not "reusing". There is no filter on a "reusable" state and no min-lines filter in the search of the block (**Code**).
- The block batch of 32 and the per-thread block lists exist in the Java LXR. We do not have them (see section 6).

### 3.3 What our code did before (**Code**, `immix_allocation.cpp`)

- `TryFindNextHole` called `IsLineDead` line by line. Each call went through the checked accessor of the metadata layer.
- The refill cleared the RC range, the straddle range, the unlog range, the mark range and the ValidObject range, each with a separate range reset
  (`ResetRangeQuiescent`). This was `ResetRangeQuiescentCore`.
- `AcquireBlock` scanned the block table from the start for each call, tested the state with an atomic operation, and kept "exhausted" tokens.
  About 70 candidate blocks were tested per recycled-block call (**Measured** with a temporary counter: 4.25 million tests in 60 thousand calls).

### 3.4 What we did (steps 1 and 3)

Step 1 (commit `fed689f2b07`):

- The hole search reads the RC words directly (`IsLineDeadFast`; 1, 2 or 4 words per line for 2, 4 or 8 bits per count; see the note below).
  It stops at the first live line after the hole. It has the same conservative first-line rule as `rc_get_next_available_lines`.
- New `SideMetadataManager::FillRangeBytes` clears the unlog, mark and ValidObject planes with a byte fill (a `memset`).
- The refill no longer clears the RC plane and the straddle plane. They must be zero already (a dead line has count zero; the death path
  clears the straddle bits, as `unmark_straddle_object` does in the Java LXR). In Debug builds and in the test hooks the refill **checks** that the range is zero and reports `InconsistentState` if not.
- `IncrementLineReuseRange` is a plain byte loop that wraps at 255, as the Java line reuse counts do.

Note on the words per line: a line is 256 bytes and the count is per 8 bytes, so a line has 32 counts. With 2 bits each that is 64 bits = 1 word.
With 4 bits it is 2 words; with 8 bits it is 4 words (`m_rc_words_per_line` = 1, 2 or 4).

Step 3 (commit `51bd7c1b6fe`):

- `AcquireBlock` has forward-only cursors (`m_next_fresh_block`, `m_next_recycled_block`, `m_next_pinned_block`, `m_next_copy_block`).
  A caller scans with plain loads and claims the found range with one compare-exchange.
- `ResetCursors()` runs at the same places as the Java LXR: in `Suspend` after the mutators are flushed, in `ReleaseEpoch` after the epoch flip,
  at heap growth and shrink, and at initialization.
- A recycled candidate is any block with state not Unallocated that is not `IsReusingFast` (new in `immix_block.h`) and matches the pinned flag.
  The "Reusable state plus min-lines" filter and the exhausted tokens are removed.
- **Forced by .NET:** after the lazy sweep, `LxrGcHeap::Alloc` resets the cursors once and retries (`LxrReferenceCounting::ResetAllocationCursors`).
  Our arena cannot add a chunk like the Java LXR can, so a retry is the way to find the blocks that the sweep freed after the cursor passed them.

### 3.5 Forced by .NET (F9)

| difference | why |
|---|---|
| The ValidObject plane is cleared at each refill (`FillRangeBytes`). | .NET needs to find an object from an interior pointer (P16, P22). The Java LXR has no vo_bit. The clear is on the **same schedule** as the Java LXR unlog and mark tables (at the refill). I checked if it can move to the pause or to the block release: it can not, because a refill reuses a hole inside a block that is not released, and the dead objects of the hole must not be found as valid. |
| The data memory of a fresh block or a hole is zeroed at the refill (`memset`). | The .NET allocation helpers expect zeroed memory. **Inferred:** in OpenJDK the object memory is zeroed per object by the VM side, which I did not read. |
| The straddle plane exists in ours. | **Not forced by .NET.** The Java LXR has the same kind of table (`RC_STRADDLE_LINES`, see 3.2). Ours has the same job (P16, P22). I kept it and I did not change its code. It is zero at the refill and the refill does not clear it, as in the Java LXR. **Open:** I did not compare our straddle code with `mark_straddle_object` line by line. |
| The copy-log ranges of the retiring sources. | Part of the nursery copy design (P22). |

### 3.6 Tests and controls (F9)

Tests (P2.4 suite `immix-allocation-validation.cpp`, run by `run-immix-allocation-validation.sh`; P4 selectors):

- Scan equivalence: the lazy hole search returns exactly the holes that the eager search of the Java LXR returns (all positions, random counts).
- Lazy scan probe count: one search of one hole reads only the lines from the search line to the end of the hole.
- Refill schedule: the refill clears unlog, mark and ValidObject, and does not clear RC and straddle.
- A dirty free block (count or straddle not zero) is reported and not handed out.
- Cursors: forward only; a block is found again after `ResetCursors`; 8 threads take each block exactly once.
- P4 S4: `ValidatePauseReleaseAndLazyReuse(bool allocateWhileHeld)` runs two scenarios.

Controls (each must make a test fail; **Measured**, `results/controls-f9.txt`, `results/controls-engine.txt`):

| control | what it breaks | result |
|---|---|---|
| `LXR_F9_SCAN_ONE_WORD_CONTROL` | the search reads only one word | P2.4 fails (8 lines) |
| `LXR_F9_EAGER_SCAN_CONTROL` | the search reads the whole block | P2.4 fails (5 lines) |
| `LXR_F9_CLEAR_AT_REFILL_CONTROL` | the refill clears RC and straddle | P2.4 fails (3 lines) |
| `LXR_F9_NO_VERIFY_CONTROL` | the dirty-range check is off | P2.4 fails (2 lines) |
| `LXR_F9_PREFILTER_CONTROL` | the old Reusable filter comes back | P2.4 fails (1 line) |
| `LXR_F9_NO_CURSOR_RESET_CONTROL` | the cursors are not reset | P2.4 fails (3 lines) |
| `LXR_F9_NO_PAUSE_RESET_CONTROL` | no reset at the pause | P4 default fails (3), `--nursery` fails (15) |

## 4. F10: the RC loops, as in the Java LXR

### 4.1 Memory order of the metadata load (commit `d824b6c4f51`)

- **Code, the Java LXR:** the count operations (`util/rc.rs`) use `Relaxed` order. Where the Java LXR uses `SeqCst` for a metadata load, it is one plain `mov` on x64. There is no fence.
- **Code, ours before:** `SideMetadataAccessor::LoadWord` for a non-relaxed order did a full barrier, the load, and a full barrier.
- **What we did:** a non-relaxed load is `VolatileLoad` only (an acquire load; a plain `mov` on x64).
- **Measured:** this was the largest single win (list suffix nursery 4.05 to 3.30 s).
- **Different from the Java LXR (kept, low risk):** the Java LXR `SeqCst` is stronger than our acquire. On x64 both are the same instruction. On ARM64 they differ. **Open:** I did not test ARM64.
- Test: P2.4 `TestF10LoadOrder` (a store-buffer test, 300000 rounds). Control `LXR_F10_PLAIN_STORE_CONTROL`: it shows the both-zero outcome (12 in 100000 rounds on the first run). **Measured.**

### 4.2 Count update on one byte (commit `c9e274efa6a`)

- **Code, the Java LXR:** `util/rc.rs` (`inc`, `dec`, `stick`) updates a count with `fetch_update` on the RC table: a compare-exchange on one byte, `Relaxed` order. Counts of 2 bits share a byte.
- **Ours before:** a word-wide compare-exchange on the 32-bit word around the count.
- **What we did:** `IncrementSaturating`, `DecrementNonZeroNonSaturated` and `CompareExchange` use a byte-wide compare-exchange for entries of 8 bits or less
  (only for relaxed order and little-endian; `LocateByte`).
- Test: P2.4 `TestF10ByteCompareExchange` (all 3 widths, many threads on the same byte). Control `LXR_F10_BYTE_PLAIN_CONTROL`: P2.4 fails (6 lines).

### 4.3 In-place promotion: unlog bits by lines (same commit)

- **Code, the Java LXR:** `plan/lxr/rc.rs: scan_nursery_object` for an in-place promotion writes `0xffffffff` (one `u32`) for each 256 bytes of the object
  from `(start + header)` aligned down. It does nothing for a value array.
- **Ours before:** `ResetRangeQuiescent` on the unlog plane for the whole object. This was 10.6 percent of the list suffix samples (**Measured**).
- **What we did:** `SetUnloggedLines` does the same `u32` stores. It is skipped when the type has no GC pointers (`!ContainsGCPointers()`).
- Control `LXR_F10_NO_INPLACE_UNLOG_CONTROL`: P4 default fails (45 checks).

### 4.4 Two-bit counts as the default (commit `2ba8e86b092`)

- **Code, the Java LXR:** `util/rc.rs: LOG_REF_COUNT_BITS` = 1, so a count has **2 bits and covers 8 bytes** (`MIN_OBJECT_SIZE` 8). A count of 3 (`MAX_REF_COUNT`) is **sticky**:
  an increment does nothing and a decrement does nothing. An object with a sticky count is freed only by a trace.
  **Correction of P24 row 10:** P24 wrote "per 16 bytes". That was wrong. The Java count is per 8 bytes.
- **Ours before:** `LXRRCBits` default 8.
- **What we did:** the default is 2 (`lxr_gc.cpp`). The overflow code was already there (`IncrementSaturating` and the sticky decrement); it now runs in the default.
- **Interplay (Code and Measured):**
  - An object that has two roots reaches 3 for a short time, because a root is counted at each pause before the previous root is decremented. Such an object dies by trace only. This is the same in the Java LXR (**Inferred** from the same rules; I did not run the Java LXR).
  - One managed cell failed with 2-bit counts: `held-deletion`. The parent was held by a local variable **and** by a GCHandle, so its count was 3 (sticky) and the delete did not free it. I changed the test (`P5/runtime-tests/Program.cs`: a new `AllocateDeletionHandle` helper; the graph is now held by the handle alone). The behavior of the collector is the Java behavior; the test had an 8-bit assumption. All 3 widths pass.
  - **Inferred:** the straddle plane and the trace do not depend on the count width. The tests with straddle objects pass at all 3 widths (`--f10-rc`, the managed cells).
- Tests: P4 `ValidateF10Overflow(bits)` and the selector `--f10-rc` (72 checks, for 2, 4 and 8 bits; also run by the default selector). Control `LXR_F10_NO_SATURATE_CONTROL` (the overflow is not sticky): `--f10-rc` fails (6 checks), the default selector fails (9), `--nursery` fails (5), and P2.4 fails.
- **Measured:** list suffix non-moving 2.48 to 2.32 s; list suffix nursery 2.91 to 2.86 s; requests 2.69 to 2.63 and 2.81 to 2.74 s.

### 4.5 Software prefetch (commit `2e782cd7913`)

- **Code, the Java LXR:** `args.rs`: `PREFETCH` on, `PREFETCH_STEP` 8, `PREFETCH_HEADER` on, `PREFETCH_RC` **off**. In `rc.rs: process_incs` and `process_decs`, and in `cm.rs`
  (the trace loops), after the work on entry `i`, the object of entry `i + 8` is prefetched (`Address::prefetch_read`, hint `_MM_HINT_NTA`).
  For slot entries the slot is loaded to get the object (`s.load()`). Nothing is prefetched past the end. The RC table is **not** prefetched.
- **What we did:** new `lxr_prefetch.h` with `LxrPrefetch::Ahead` (`Step` = 8, `_MM_HINT_NTA`). It is used in `ProcessIncrementWork`, `ProcessChildIncrements`,
  both loops of `ProcessDecrements`, and both loops of `ProcessTraceWork`. `-DLXR_NO_PREFETCH` removes it.
- **Measured:** no effect on wall time or CPU in any cell (3 repetitions; `results/ab-f10b-f10c.txt`). **Code:** this is what the Java LXR does, so we keep it.
  **Inferred:** our loops spend their time in cache misses on the count table and the lock-prefixed instructions, not on the object header, so the prefetch of the header has little to hide.
- A prefetch must not change a result: the P4 default, `--f8-lazy` and `--f4b-scheduler` selectors pass with and without `-DLXR_NO_PREFETCH` (**Measured**).
- Test: P2.4 `TestF10Prefetch` (a test hook sees every prefetch address: entry i + 8, none past the end, none for null, the flag bits removed for root entries).
  Control `LXR_F10_PREFETCH_OFFBYONE_CONTROL`: P2.4 fails (4 lines).
- Not done: the Java LXR `cm.rs` also prefetches in `process_slots_impl` (the pause trace of roots and slices). Our trace takes object entries from packets and has
  the same two loops with prefetch. **Inferred:** this covers the same work. **Open:** I did not compare the roots of the pause trace line by line.

### 4.6 Per object, line by line (`ProcessIncrement`, `TryCopyNursery`, `IsNurseryFast`, `DecrementWithDeathCore`)

The Java code: `plan/lxr/rc.rs` `process_inc_and_evacuate`, `dont_evacuate`, `promote`, `scan_nursery_object`, `process_decs`; `util/rc.rs`.

| # | the Java LXR does | ours before | ours now | status |
|---|---|---|---|---|
| D1 | `dont_evacuate` tests the **count first** (`rc.count(o) != 0` means "mature, no copy"), then `is_nursery()` | `TryCopyNursery` first, then the count | `ProcessIncrement` tests `count == 0` before it calls `TryCopyNursery` | fixed |
| D2 | `scan_nursery_object`: for each child, if the child has a count, **increment it in place** (unless at max); only a child with count 0 is queued as a new increment | every child was queued | `ScanIncrements` increments in place when the child is in Immix, is not a mature candidate, and has count not 0; only young children are queued. It also queues a trace when `NeedsIncrementTrace` | fixed (the trace queue is our design, see section 6) |
| D3 | `process_decs`: `is_dead_or_stuck(o)` skips an entry with count 0 or at max | every child entry was queued | `ScanDecrements` skips children with count 0 or at max when no trace runs | fixed in part: **Open** why ours still queues them while a concurrent trace runs; the Java loop skips them in every phase |
| D4 | the Java LXR does nothing to the metadata of a copy destination before it counts the copy (`rc.rs: process_inc_and_evacuate`: `inc(new)`); it has no ValidObject bit | `InitializeObjectQuiescent` on the destination (it re-checked and re-cleared the range) | `RegisterObjectInClearedSpan` sets only the ValidObject bit; the copy refill already prepared the range (F9). Debug builds and hooks verify the range | fixed. The ValidObject bit stays (**forced by .NET**). Control `LXR_F10_COPY_NO_VALID_CONTROL` makes `--nursery` abort (exit 134) |
| D5 | **Inferred:** the copy statistics are per worker (`crate::stat`) | 3 shared atomics (`CopiedObjects`, `CopiedBytes`, `PauseCopiedBytes`) | per-worker counters; `GetNurseryMetrics` sums them | fixed. **Measured:** binary trees nursery CPU 1.41 to 0.89 s. Control `LXR_F10_SHARED_COPY_COUNTERS_CONTROL`: `--nursery` fails (15 checks) |
| D7 | the object header is prefetched (PREFETCH_HEADER) | no prefetch | step 8 | fixed (4.5) |
| D8 | the in-place unlog is a `u32` store per 256 bytes | a range reset | `SetUnloggedLines` | fixed (4.3) |
| D9 | `promote` checks no type flags | `Collectible` and `HasFinalizer` are read from the method table at promotion | same | **forced by .NET** (finalizer and collectible loader registration) |
| D10 | a forwarded object: read the forwarding pointer from the header word | `ResolveForwarded` reads the header | same | **forced by .NET** (the header layout; P22) |
| D11 | `rc.inc` is a CAS loop on one byte with `Relaxed` order | word CAS, fences | byte CAS (relaxed), acquire load | fixed (4.1, 4.2) |
| D12 | line reuse counts increment only for **recycled** lines while a trace runs | also for fresh blocks while a trace runs | same | **not forced; kept as a safety step of the port.** **Open:** I did not find a case that needs it. See section 6 |

**Measured:** the per-object code changes D1, D2, D3 and D4 did not change the wall time or the CPU in a way that I could see
(`results/ab-f10c-f10d.txt`, 5 repetitions: 2.87 against 2.95, 2.82 against 2.82 s). The big wins in F10 came from other parts:
memory order (4.1), the unlog fill (4.3), the shared counters (D5) and the 2-bit counts (4.4).
**Inferred:** the per-object work is bound by cache misses and locked instructions, so the order of the checks changes little.
**Inferred:** this agrees with the profile: `DecrementWithDeathCore` stayed at the same sample count.

### 4.7 Zeroing of the mark table

- **Code, the Java LXR:** `plan/lxr/global.rs` and the prepare code make **packets** that zero the mark table by chunk, in parallel. They run early and concurrently
  when a concurrent cycle is planned (`hint_cycle_gc`), or inside the InitialMark or Full pause.
- **Ours:** `ResetAllDataRangesQuiescent(ObjectMark)` on one thread inside `Suspend`. **Not changed.**
- **Measured (upper bound):** the trace field `suspend_prepare_us` holds the zeroing and other prepare work. It is 304 us in the InitialMark pause and 336 us in the FinalMark pause
  of list suffix nursery (pause totals 23.0 and 16.9 ms), and 553 and 335 us in requests non-moving (pause totals 6.8 and 6.6 ms). In an RC pause of list suffix nursery,
  which does not zero the mark table, the field is about 175 us. So the zeroing costs at most about 0.4 ms in a mark pause (1.5 to 8 percent of it).
  A run has only 1 or 2 mark pauses among 20 to 70 pauses.
- **Status: not done, not forced by .NET.** It is a difference from the Java LXR. I did not do it because the measured cost is small and a change needs
  concurrent work before the pause, which is a larger design. If you want strict parity here, it is a separate step. **Open.**

## 5. Forced by .NET (all differences that I keep)

1. ValidObject plane clear at the refill (3.5). The straddle plane is **not** forced; see 3.5.
2. Zeroing of the data memory at the refill (3.5).
3. The method-table checks at promotion (D9) and `ResolveForwarded` (D10).
4. The cursor reset and retry after the lazy sweep in `LxrGcHeap::Alloc` (3.4).
5. The `MarkPromotedStart` and ValidObject stores use CAS loops (the ValidObject plane is shared by neighbouring objects).

## 6. Remaining differences from the Java LXR (ranked; "F" = forced by .NET, "N" = not forced)

Rank is my estimate of the cost, from the profile.

1. **N** Block batching of 32, per-thread block lists, owner marking and stealing are not implemented. We take one block at a time from the shared cursor.
   `TryAcquire` and the lock are still 5 percent of the requests profile in the base build (**Measured**), and less now (`TryAcquire` 0.8 percent incl.).
2. **N** `TryAcquire` still uses the checked metadata layers and sequentially consistent operations.
3. **N** The mark table zeroing is single-threaded in the pause (4.7). **Measured** at most about 0.4 ms in 1 or 2 mark pauses per run.
4. **N** D12: line reuse counts also for fresh blocks while a trace runs (the Java LXR does it for recycled lines only).
5. **N** D3: dead and stuck decrement children are queued while a trace runs.
6. **N** The per-block `Reusable` state is still kept for the policy numbers (the lazy sweep sets it). It is not used by the block search any more.
7. **F** Items of section 5.
8. **N (design of earlier steps, unchanged)** The Full pause is single threaded; the injector spin lock; `MAX_PAUSE_MILLIS`; the deliberate deviations of P25 and P26
   (F1 weak targets, F5 occupied pages, the floor of 2 trace workers).
9. **N** The `SeqCst` store of the line reuse count is a release store in ours (Java: SeqCst). Same on x64.

## 7. Tests that ran, and tests that did not run

**Ran (Measured), on the final tree** (the commit before `47d58a6a401` for Linux; `47d58a6a401` only changes one test cast, and I ran `--f10-rc` and the default selector again on Linux Release after it):

| test | result |
|---|---|
| Linux Debug native (41 selectors) | 41 pass, 0 fail (`results/p28-final-nat-Debug.txt`) |
| Linux Release native (41 selectors) | 41 pass, 0 fail (`results/p28-final-nat-Release.txt`) |
| Legacy P2.1 to P3.4 (9 suites) | 9 pass (`results/p28-final-legacy.txt`) |
| Managed cells, Debug collector, 3 modes (90 cells) | 90 ok (`results/managed-final.txt`) |
| Controls of P2.4 (10) | all 10 fail the test as designed (`results/controls-f9.txt`) |
| Controls of the engine tests (7 controls, selectors default, `--nursery`, `--f4-scheduler`, `--f10-rc`) | each is caught by at least one selector (`results/controls-engine.txt`, `results/control-shared-copy.txt`). `LXR_F10_COPY_NO_VALID_CONTROL` is caught by `--nursery` (abort 134) only; `LXR_F10_SKIP_DECS_CONTROL` by `--f4-scheduler` only |
| `-DLXR_NO_PREFETCH` build | default, `--f8-lazy` and `--f4b-scheduler` pass |
| Small machines (taskset 1, 2 and 4 CPUs; `scripts` `small26.sh`) | equal checksums in all cells: 14985972, 317308156512, 301262627042 (same as the 16-CPU runs); `results/small-final.txt` |
| Stock helper machine-code hash | `46cee8bd87fd6252f5265267148d0a34`, equal to the baseline (`scripts/hash26.sh` on the build output in `/root/d2/main`). The `libcoreclr.so` in the base frame and in the final frame has the same MD5 (`2bff78068bfde1f6af064efd00c6612d`). **Open:** the frame copy has no `.dbg` file, so I did not hash the frame file itself. |
| Windows Release, MSVC (`/W4 /WX`), `subst V:` | 41 of 41 native selectors pass (`results/windows-nat-Release.txt`); `clrlxrgc.dll` builds, SHA256 `68FA261068792689AA41ED807A21B311B1AE21682FFBAB3A15030BF08D91AA85` (`results/windows-clrlxrgc-sha256.txt`). I used `V:` because `W:` was in use for another worktree. The first Windows compile found one `/W4` warning in my test (`int` to `uint8_t`); fixed in `47d58a6a401` |

**Did not run:**

- ThreadSanitizer (not run in P28).
- Windows Debug, and the Windows managed cells.
- The managed cells with a Release collector.
- ARM64, NativeAOT, profiler heap walks, GCStress.
- The engine controls ran in Debug only.
- The Java LXR was not run. There is no Java time in this document.

## 8. Ideas not taken

I did not do these. They are not the Java way.

- Per-block summaries of free lines (to skip the scan of full blocks).
- A cache of the next hole per allocator.
- A bitmap of dead lines kept up to date at the death of objects.
- Moving the ValidObject clear from the refill to the sweep or to a separate thread.
- Zeroing the data memory in a background thread before the refill.
- Making `DecrementWithDeathCore` batch the count-table writes by cache line.
- A bigger prefetch step, or a prefetch of the count table (`PREFETCH_RC` is off in the Java LXR).
- Skipping the first line of a hole in a different way from the Java rule.

## 9. How to reproduce

All scripts are in `scripts/`. They run in WSL with `env -i PATH=/usr/bin:/bin`, and they must have LF line ends (`sync28.sh` copies them to `/root/p28/scripts`).
Frames come from `/root/p25/build.sh <worktree> Release <name>`. A fresh worktree needs the git-ignored `artifacts/obj/_version.c`, `_version.h`,
`NativeVersion.rc` and `runtime_version.h` copied from an older worktree, for Linux and for Windows.

- Native: `/root/p25/scripts/chain-native.sh <tag>`, `natsum.sh <tag>`. Controls: `controls-f9.sh`, `controls-engine28.sh`, `control-one28.sh`.
- Managed: `mkfixtures28.sh`, `managed28.sh <frame> <out>`.
- Benchmark: `bench-all28.sh <tag>` (uses P25 `bench.sh`). A/B: `quick28.sh`. Perf: `perf28.sh`, `perfshare.py`, `perfcount28.sh`.
- Windows: `P17-pause-cost-and-reserve/native-windows.ps1 -Config Release -Output <dir>` and `build-windows.ps1 -Config Release -Label p28`, both from a `subst` drive.
- Java names: `javanames28.sh` finds the functions that this document names.

## 10. Open

1. The mark table zeroing is not as in the Java LXR (4.7).
2. D3 and D12: I did not test whether the two kept differences can be removed.
3. The sticky count of 3 means that objects with two roots die by trace only. This is the Java behavior. Programs with many such objects keep more garbage until the next trace. I did not measure how often this happens.
4. Binary trees nursery CPU is still 3.4 x WKS (1.1).
5. ARM64: the acquire load (4.1) and the byte compare-exchange are not tested there.
6. The perf share of `DecrementWithDeathCore` is now the largest in list suffix (1.3).
