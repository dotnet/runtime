# P19 - The integrated LXR branch

**Status:** built, tested and measured on Linux x64 and Windows x64. Not pushed. Nothing public was done.
**Date:** 2026-10-21.
**Branch:** `kkokosa-lxr-integrated` (local). It is one linear series on top of `dotnet/runtime` main.
**Source of the work:** the stack that ends at P17 S6 (`kkokosa-lxr-s6-finalizer-registry`, `69e02a55607`).

## Update P28 (branch `kkokosa-lxr-f9-f10-parity`)

This update is on top of P26 (`e05f55b959e`). Eight local commits (`F9:` and `F10:`). The details are in `P28-refill-and-rc-loop-parity/README.md`. The rule of this work: do what the Java LXR does, in the same way; keep only the differences that .NET forces.

- **F9 (code).** The hole search reads the count words as `RCArray::is_dead` does and reads only one hole. The refill no longer clears the count and straddle planes (it checks that they are zero in Debug builds).
  The block search has forward-only cursors that are reset at each pause; every non-free block is a candidate. **Measured:** `AcquireBlock` 8.5 percent of the requests nursery samples becomes 0.6; `ResetRangeQuiescentCore` 6.0 and 10.1 percent become about 0.
- **F10 (code).** An acquire load is a plain load (no fences). Counts use a byte compare-exchange. An in-place promotion fills the unlog bits by lines. **The default count width is 2 bits** (the Java default; P24 row 10 said "per 16 bytes"; the Java count is per 8 bytes).
  Software prefetch with step 8 in the increment, decrement and trace loops. Counted children are incremented in place. Copy counters are per worker. **Measured:** the acquire load and the 2-bit default were the large wins; prefetch and the order of the per-object checks had no effect.
- **Final numbers (same session as the base build, x WKS):** wall 1.3/1.4 (binary trees), 1.4/1.4 (requests), 0.6/0.7 (list suffix) for nonmoving/nursery; CPU 1.7 to 3.4 times WKS. The base build of the same session: 1.4/1.7, 1.9/2.1, 0.8/1.2.
- **Tests (Measured).** One new native selector `--f10-rc` and new P2.4 tests with 10 controls, 7 engine controls; 41 native selectors pass on Linux Debug and Release and on Windows Release; 90 managed cells pass with the Debug collector (one managed test had an 8-bit count assumption and was changed). The stock helper hash is unchanged (`46cee8bd87fd6252f5265267148d0a34`).
- **Not done:** the mark table zeroing is not in parallel packets as in the Java LXR; block batches of 32 and per-thread block lists; TSan; Windows Debug.
- **Share files:** `C:\Users\konradkokosa\lxr-integrated-share\kkokosa-lxr-f9-f10-parity.bundle` and `patches\`. The P26 set is in `previous\kkokosa-lxr-scheduler-f2-f4b-f8\`.

## Update P26 (branch `kkokosa-lxr-scheduler-f2-f4b-f8`)

This update is on top of P25 (`c3352e76067`). Six local commits. The details are in `P26-scheduler-and-lazy-decrements/README.md`.

- **F2 (code).** A pause no longer walks the trace backlog: trace packets are not in `m_work` any more, `FixForwardingReaders` does not walk the consumed log bank, forwarding is resolved when a
  packet runs. The Java value of 3 concurrent trace workers is now the default (with a floor of 2 on small machines, a deviation on purpose). **Measured:** list suffix nursery with the limit of 3: 13.7 s and 10.6 s become 4.6 s and 4.4 s.
  The 148 ms FinalMark of P25 was 78 ms wait before the pause and 34 ms wait after it, plus 36 ms of stopped time.
- **F4b (code).** Work-stealing deques and per-stage batch injectors replace the global-mutex queues; the trace packet loops over its children. **Measured:** system time of requests nursery 2.9 s becomes 0.5 s; CPU 9.2 s becomes 6.0 s.
- **F8 (code).** The thread that made an RC or FinalMark pause does not wait for the lazy decrements and the sweep. **Measured:** 7 to 13 percent wall time in requests and list suffix.
- **Tests (Measured).** New selectors `--f2-pause`, `--f4b-scheduler`, `--f8-lazy` with 8 controls; 40 native selectors pass on Linux Debug and Release and on Windows Release; 90 managed cells pass with the Debug collector.
  The flaky P3.4 check was a race in the test (16 of 100 runs failed, now 0 of 100). TSan: no report on the scheduler suites. The stock helper hash is unchanged (`46cee8bd87fd6252f5265267148d0a34`).
- **Final numbers (same session, x WKS):** wall 1.3/1.7 (binary trees), 1.8/1.9 (requests), 0.8/1.0 (list suffix) for nonmoving/nursery; CPU 2.4 to 5.1 times WKS.
- **Open:** the next pause still waits for the last lazy generation; per-object RC cost; Full pause on one thread; the injector is not lock-free.
- **Share files:** `C:\Users\konradkokosa\lxr-integrated-share\kkokosa-lxr-scheduler-f2-f4b-f8.bundle` and `patches\`. The P25 set is in `previous\kkokosa-lxr-parity-fixes-f1-f5-f4\`.
## Update P25 (branch `kkokosa-lxr-parity-fixes-f1-f5-f4`)

This update is on top of the D2 branch and the P24 investigation (`c16710a2c1c`). Four local commits. The details are in `P25-parity-fixes-f1-f5-f4/README.md`.

- **F1 (code).** A pause no longer queues one trace packet for each counted object. A packet is queued only inside the window of a concurrent trace and only
  for an unmarked object. The roots of InitialMark and Full come from `PrepareTrace`, as in the Java LXR. **Measured:** list suffix 20.4 s and 28.8 s become 5.8 s and 6.9 s.
- **F5 (code).** The emergency rule counts the pages that hold data (blocks minus free lines), not whole blocks. **Measured:** requests nonmoving 10.9 s and 52 Full
  pauses become 4.0 s and 0 Full pauses.
- **F4 (code).** Workers follow the Java rule (13 on 16 processors), increments and decrements recurse inside the packet, idle workers wake one at a time.
  **Measured:** list suffix 5.6 s and 6.4 s become 3.7 s and 4.6 s (WKS 3.6 s). CPU of requests is higher (5.9 s and 8.8 s). The concurrent trace limit exists but its default is all workers.
- **Tests (Measured).** Three new native selectors (`--f1-trace`, `--f5-policy`, `--f4-scheduler`) with 9 controls; 37 native selectors pass on Linux Debug and Release
  and on Windows Release; 90 managed cells pass with the Debug collector. The stock helper hash is unchanged (`46cee8bd87fd6252f5265267148d0a34`).
- **Not changed:** the runtime, the JIT, the GC interface and the barrier. Everything is in `libclrlxrgc`.
- **Open:** lock-free queues, the rest of F2, a parallel Full pause, and a flaky check in P3.4 that was already flaky (section 7 of the P25 report).
- **Share files:** `C:\Users\konradkokosa\lxr-integrated-share\kkokosa-lxr-parity-fixes-f1-f5-f4.bundle` and `patches\` (the range `a485c819c17..kkokosa-lxr-parity-fixes-f1-f5-f4`). The D2 set is in `previous\kkokosa-lxr-d2-bump-allocation\`.
## Update D2 (step P18-D2, branch `kkokosa-lxr-d2-bump-allocation`)

This update is on top of `kkokosa-lxr-d1-specialized-barrier` (`74dc9e84445`). The details are in `P22-bump-allocation/README.md`.
Where the text below says "notifying helpers" or "allocation notification", **D2 replaced it**:

- **Code.** LXR allocates through the **stock** bump-pointer helpers. The helper calls the collector only when the buffer
  is full. The collector does bulk work for each buffer and records the used part of each buffer as a span. It does no
  work for a single object on the fast path.
- **Code.** Removed: `AllocFastNotification.S/.asm`, the helper selection in the VM and NativeAOT, the allocation-complete
  flags, the `IGCHeap` notification method, the test collector of P1.4, and `CompleteAllocation`, `RecordFastAllocation`
  and `pendingFast*` in the engine.
- **Code.** The ValidObject bit is set once per survivor at promotion. Interior pointers into young objects are found
  by a walk over the spans.
- **Code.** GC interface minors are renumbered: 6.1 old-value hooks, 6.2 object reference enumeration (was 6.6),
  6.3 object header bits (was 6.7). The minor version is **3**. (The table in section 4 shows the D1 numbers.)
- **Measured.** One allocation costs 11 to 26 ns (D1: 78 to 96 ns). The stock helpers hash equal to main plus the PR:
  `46cee8bd87fd6252f5265267148d0a34`.
- Still not in the branch: ARM64 (P21, section 10), profiler heap walks over spans (P22, section 10).

## Update D1 (step P18-D1, branch `kkokosa-lxr-d1-specialized-barrier`)

This update is on top of `kkokosa-lxr-root-capture-fix` (`a1ddb3fd4a1`). The details are in `P21-specialized-barrier/README.md`.
Where the text below says "Standard-ABI", "slot log" or "R2R must be off", **D1 replaced it**:

- **Code.** The write barrier of LXR is now assembly (`gc/amd64/lxr_writebarrier.S` and `.asm`). The runtime installs it
  through the mechanism of PR #3316 (`GetAssignRefFunctions`, `WRITE_BARRIER_CUSTOM`, patchable slots). It follows the
  Specialized ABI. The PR mechanism is the **only** way to customize the write barrier.
- **Code.** Removed: the two slot-log `WriteBarrierManager` types and their five patched fields, the JIT flag
  `USE_STANDARD_WRITE_BARRIER_ABI` and every Standard-ABI path, `ErectWriteBarrierRangePre` and the separate pre-store
  calls, software write watch for LXR, and the test-only build option. The JIT-EE GUID is new.
- **Code.** One pre-store hook: the PR `check_escape(dst, ref)`. LXR may read `*dst` in it, because it runs before the
  store. The PR `bulk_move_with_write_barrier` handles plain reference ranges. The old-value extras that the PR hooks
  cannot express stay as an "LXR extension" (P21, section 8).
- **Code.** The heap type is `GC_HEAP_CUSTOM` now (open item 2 of section 12 is closed). `GCSettings.IsServerGC` is true.
- **Measured.** ReadyToRun is **on**. Build the framework with `/p:CrossgenOldValueWriteBarriers=true` (it adds the
  crossgen2 option `--old-value-write-barriers`). **All** precompiled framework assemblies need the flag; in our frames
  only CoreLib is precompiled. The runtime rejects an unmarked image for LXR, uses the JIT instead (slower start, same
  result) and writes the reason only to `DOTNET_ReadyToRunLogFile`. The flag is an upstream-proposal item.
  Details: P21, section 9.
- **Measured.** The stock helpers hash equal to main plus the PR: `46cee8bd87fd6252f5265267148d0a34` (re-recorded).
- Still not in the branch: bump-pointer allocation (D2), ARM64 (P21, section 10).
- Build with `/p:CrossgenOldValueWriteBarriers=true` in addition to the commands of section 7. Do not set
  `DOTNET_ReadyToRun=0` for LXR any more (`demo.sh` takes `LXR_READYTORUN`, default 1).

## 0. How to read this report

### 0.1 Labels

Every claim has one label.

| Label | Meaning |
|---|---|
| **Measured** | A number or a result that I produced in this session. The command or file is named. |
| **Code** | I read it in source, or a `git` command shows it. |
| **Inferred** | My conclusion from facts above it. It can be wrong. |
| **Open** | Not checked, not decided, or not working yet. |

### 0.2 Words

| Word | Plain meaning |
|---|---|
| LXR | The collector that this port adds. It uses reference counting and a concurrent trace on an Immix heap. |
| Satori | The second custom collector of PR dotnet/runtimelab#3316. We keep it only as a comparison build. |
| Frame | A flat folder with the runtime, the libraries, `corerun` and the collector library. We start programs from a frame. |
| Standard-ABI barrier | Our former write barrier. The JIT treated the call to it like an ordinary C call. **Removed in D1.** |
| Notifying helpers | Our allocation helpers. They call the collector once for every new object. |
| Specialized-ABI barrier | The barrier model of the PR. **In the branch since D1** (see P21). |

## 1. What the branch contains

1. **Current `dotnet/runtime` main**, commit `a485c819c17` (2026-10-21 14:58 UTC). **Code.**
2. **The runtime side of PR #3316** ("Custom GC support with Satori", author `janvorli`) as a patch series. The two PR commits are cherry-picked with their authors. The `gc_satori` folder is in the tree. Satori is **not** built by default. It is a comparison target (`-DCLR_CMAKE_ENABLE_SATORI_GC=1`). **Code.**
3. **Our runtime changes** in one commit: slot-log write barriers and their write-watch twin, the JIT flags `USE_STANDARD_WRITE_BARRIER_ABI` and `WRITE_BARRIER_REQUIRES_OLD_VALUE`, the `RequiresOldValueWriteBarrier` intrinsic, bulk and layout store helpers, notifying allocation helpers (removed in D2), header-bit negotiation and object reference enumeration. **Code.**
4. **The LXR collector** (P2 to P16), then the measured steps of P17 as separate commits: S0, S5a, S1, S5a follow-up, S2, a large-object fix, S3, S4, S5b and S6. **Code.**
5. **Tests and harness**, and **the documents** (P0 to P19, the roadmap extension and its benchmark results). **Code.**

The README of the shared branch must be honest about three things. They are not in this branch:

- ~~The barrier is still our Standard-ABI barrier.~~ **Superseded by D1:** the barrier is the Specialized-ABI barrier of the PR mechanism.
- ~~Allocation still uses our **notifying helpers**. It does not use the stock bump-pointer helpers.~~ **Superseded by D2:** LXR uses the stock bump-pointer helpers (P22).
- **S7** (the pin list) is **not in the original integrated branch** (`kkokosa-lxr-integrated`, `06d7cda94cb`). It is on the branch `kkokosa-lxr-s7-pin-list`, which adds the S7 commits on top of it (see "S7 addition" at the end of section 11).

Section 11 gives the details and the cost.

## 2. Base hashes

| Item | Value |
|---|---|
| New base: `dotnet/runtime` main | `a485c819c17fe18fca5b6c877ece55c4cb6508c2` (2026-10-21, "Update Zip Encryption mode state checks (#136220)") |
| Old base of our stack: `dotnet/runtime` | `df9c6988c203c5be152937fa1823469397f61dad` (2026-08-06) |
| Fork main before the stack | `70207d8de0a347fea0665fb002d17b75c05a2e0c` |
| Accepted baseline `lxr` | `e8d2d23ef60e44ba7efd35a7cb3f4a76271adc6a` |
| Source tip (P17 S6) | `69e02a55607` |
| PR #3316 merge commit (runtimelab) | `68135571b69dcc573ed19834875edf32eef2e234`, merged 2026-10-19 |
| PR commits | `432092f8e05` "Custom GC support with Satori" and `258805045446db1f4e4b2c1782ee5ec38760ba73` "Use the default immortal object allocation for Satori too" |
| PR base (runtimelab `feature/gc/baseline`) | `93ff17fc48f4eaf7dab7d4df01aba1f4edec84fa` |
| Documents from the coordinator | `0b6d3ed4986` (branch `kkokosa-lxr-gc-roadmap-canvas`) |

**Code.** The PR base is built on `dotnet/runtime` commit `5b82fc4ae86` (2026-09-23). Main is about four weeks newer. Main moved in many places that the PR and our stack both touch (section 6).

## 3. The commits

The series has 22 commits on top of `a485c819c17`. Run `git log --oneline a485c819c17..kkokosa-lxr-integrated` for the hashes.

| # | Title | Phase | Size |
|---|---|---|---|
| 1 | Custom GC support with Satori | PR commit 1 | 91 files |
| 2 | Use the default immortal object allocation for Satori too | PR commit 2 | 2 files |
| 3 | Implement GetPendingBridgeHandles in Satori | Fix for the new main | 2 files |
| 4 | Build the Satori collector only when CLR_CMAKE_ENABLE_SATORI_GC is set | Satori is opt-in | 1 file |
| 5 | LXR runtime support: JIT, VM, CoreLib, NativeAOT and GC-interface changes (P1.x) | Runtime | 109 files |
| 6 | LXR collector sources (P2 to P16) | Collector | 32 files |
| 7 | LXR tests and harness | Tests | 335 files |
| 8 | GC interface 6.x: put the LXR minors above 6.0 and check the WriteBarrierParameters layout | Interface numbering | 5 files |
| 9 | P17 S0: per-phase wall-clock timers | Step | 3 files |
| 10 | P17 S5a: size the to-space reserve in bytes | Step | 10 files |
| 11 | P17 S1: remove the block-operation gate and shared counters | Step | 7 files |
| 12 | P17 S5a follow-up: reserve only the bytes a pause can copy | Step | 4 files |
| 13 | P17 S2: inline fast path for the hot side-metadata accesses | Step | 11 files |
| 14 | P17 fix: lazy release of dead large objects during a concurrent trace | Step | 2 files |
| 15 | P17 S3: remove the per-object size table and live bitmap | Step | 7 files |
| 16 | P17 S4: background sweep, block counters and lazy recount | Step | 10 files |
| 17 | P17 S5b: to-space overshoot with decommit | Step | 14 files |
| 18 | P17 S6: finalizer registry | Step | 10 files |
| 19 | Adapt the LXR collector sources to the current dotnet/runtime main | Fix for the new main | 3 files |
| 20 | LXR design documents, reports, results and roadmap (P0 to P17) | Documents | 1,941 files |
| 21 | LXR design documents: add P13 to P16 and P18 reports, P14 benchmark, roadmap results and the design README rows | Documents | 149 files |
| 22 | P19 integration report, scripts and results | Documents | this report |

**How I made the series.**

1. **Code.** I did not replay the 288 commits of the old stack. I made one squashed commit per phase on the old base (`df9c6988c20`). The files of each commit are the files of the S6 tip. The tree of the last squashed commit is byte-equal to the S6 tip tree. I checked it with `git rev-parse <commit>^{tree}`.
2. **Code.** I cherry-picked these squashed commits onto "main plus the PR". I resolved every conflict by hand. The S6 file state is the truth for LXR code. The PR mechanism is the truth where both do the same thing and the PR one is a drop-in.
3. **Measured.** I checked that the tree of the reordered series equals the tree before the reorder (`git rev-parse HEAD^{tree}`). Only the order of the commits and the squash of the fix commits changed.

**Build state of the commits.** I built and tested only the tip. The runtime commit (5) uses types that the collector commit (6) adds. Commit 5 alone does not link the LXR parts. **Open:** a bisect over the series is not promised.

## 4. Interface numbering

| | `dotnet/runtime` main | PR #3316 | Our S6 stack | This branch |
|---|---|---|---|---|
| GC interface | 5.9 | 6.0 | 5.15 | **6.7** (D1); **6.3** (D2, see below) |
| EE interface | 5 | 6 | 4 | **6** |

**D2 (P22).** The allocation notification (6.5) is removed. The two minors above it move down: object reference
enumeration 6.6 becomes **6.2**, object header bits 6.7 becomes **6.3**. `GC_INTERFACE_MINOR_VERSION` is 3. The table
below shows the numbers of D1.

**Code.** The PR sets 6.0. We adopt it. The LXR minors move above it, in the same order:

| LXR feature | Old minor | New minor |
|---|---|---|
| Write-barrier shape | 5.9 | 6.1 |
| Complete store | 5.10 | 6.2 |
| Epoch reset | 5.11 | 6.3 |
| Bulk scan | 5.12 | 6.4 |
| Allocation notification | 5.13 | 6.5 |
| Object reference enumeration | 5.14 | 6.6 |
| Object header bits | 5.15 | 6.7 |

- **Code.** `IGCHeap` has a fixed method order. Both the PR and our stack add methods at the end. In this branch the order is: main's `GetPendingBridgeHandles`, the PR's `GetFastGCFunctions`, `GetGCHeapType`, `GetTotalAllocatedBytesPrecise`, `GetAssignRefFunctions`, then our `GetAllocationNotificationParameters`, `GetObjectReferenceEnumerationParameters` and `GetObjectHeaderBitsParameters`.
- **Code.** `WriteBarrierParameters` has the PR block (`sw_ww_enabled_for_gc_heap`, `parameters_mask`) first. Our tail follows it. Two `static_assert` checks in `gcinterface.h` keep this order.
- **Code.** The loader rejects a collector with a lower major number. Every older standalone collector must be rebuilt. This is the PR rule.
- **Code.** I found one bug in the merge of the loader. The PR check "minor version >= the minor of the EE" is always true for 6.0. With our minor 7 it would be false for a 6.0 collector, and the heap type would not be read. I changed the check to "major >= 6" (`vm/gcheaputilities.cpp`).
- **Open.** The JIT-EE version identifier has a new GUID. I made it by hand. Make a final one when the changes are final.
- **Open.** The P1.1 compatibility tests for a 5.8 collector (`legacy-5.8-guard-page.patch`, `major-mismatch-gc.cpp`) were written for major 5. I did not run them. Under major 6 a 5.8 collector is rejected before any tail is read.

## 5. Evidence that the default collector is unchanged (rule R2)

**Measured.** Linux x64 Release. I built `clr.runtime` for three trees and hashed the machine code of the stock write barrier and allocation helpers with `scripts/helperhash.py`. The script disassembles each helper with `objdump`. It replaces addresses by symbol names. It hashes the text.

| Tree | Total hash |
|---|---|
| `dotnet/runtime` main `a485c819c17` | `46cee8bd87fd6252f5265267148d0a34` |
| main plus the PR commits (with the Satori fix) | `46cee8bd87fd6252f5265267148d0a34` |
| This branch (tip) | `46cee8bd87fd6252f5265267148d0a34` |

The hash covers 10 barrier variants (card table, write watch, server, region) and `JIT_WriteBarrier`, `JIT_CheckedWriteBarrier`, `RhpNewFast`, `RhpNewArrayFast`, `RhpNewPtrArrayFast`, `RhNewString` and `JIT_Patchpoint`. The helpers `JIT_ByRefWriteBarrier`, `RhpNewFastAlign8` and the `*_MP_FastPortable` helpers do not exist as symbols in this build. They are not in the hash.

**Measured.** JIT output. I compiled the 13 methods of the demo program with `DOTNET_JitDisasm` on the default collector. I used the upstream runtime and this branch. The two outputs are byte-identical after address replacement (2,724 lines, `scripts/jit-compare.sh`). The methods include 28 calls to the write-barrier and allocation helpers.

**Measured.** Default-collector tests on Linux x64 Release with this branch's runtime: `System.Runtime.Tests`, classes `System.Tests.GCTests` and `System.Tests.GCExtendedTests`. Result: **38 run, 0 failed, 0 skipped.** Command: `scripts/gc-library-tests.sh`. The test host has the same `libcoreclr.so` as the frame (same SHA-256).

**Not checked (Open):** Windows hash of the stock helpers, ARM64, the `src/tests/GC` tests, R2R and NativeAOT applications. The Windows build compiles. I ran no stock-collector test on Windows except the demo programs.

## 6. The overlap with the PR

### 6.1 The 25 files that both the PR and our stack change

**Code.** `git diff --name-only` of the PR (without `gc_satori`, 48 files) and of our runtime commit shows 25 common files. The decision for each:

| File | What happens | Decision |
|---|---|---|
| `CMakeLists.txt` (`src/coreclr`) | PR adds `gc_satori`. We add LXR options. | Both. Satori is behind `CLR_CMAKE_ENABLE_SATORI_GC`. |
| `gc/env/gcenv.os.h`, `gc/unix/gcenv.unix.cpp`, `gc/windows/gcenv.windows.cpp` | Main adds `VirtualReserve(location, size, useTHP)`. We add `VirtualReserveAt`. | Both functions stay. |
| `gc/gcconfig.h` | PR adds Satori config keys. We add test config keys. | Both. |
| `gc/gcimpl.h`, `gc/gcinterface.h` | Both add `IGCHeap` methods at the end, and both extend `WriteBarrierParameters`. | Slot order and field order as in section 4. PR mechanism kept for heap type, fast functions and patchable slots. |
| `gc/interface.cpp` | Main replaces `QueryPerformanceFrequency` by `minipal_hires_tick_frequency`. We add the three negotiation checks. | Both. |
| `nativeaot/Runtime/gcenv.ee.cpp` | Both add small changes. | Merged by git, no conflict. |
| `vm/arraynative.inl` | Bulk move hooks. | Merged by git. Our layout-aware path stays. |
| `vm/comutilnative.cpp` | Both hook `Interlocked.Exchange` and `CompareExchange`. | Both calls run: first the PR `check_escape`, then our `ErectWriteBarrierPre`. `check_escape` is null for LXR. |
| `vm/excep.cpp` | PR: fault addresses of the custom barrier. Ours: fault ranges of the slot-log barrier. | Merged by git. Both kept. |
| `vm/frozenobjectheap.cpp` | Allocation-complete call for frozen objects. | Merged by git. |
| `vm/gcenv.ee.cpp` | PR calls the barrier update for custom collectors. We add side-metadata validation and the epoch reset in `RestartEE`. | Merged by git. I renamed the unused parameter back to `bFinishedGC` because our code reads it. |
| `vm/gcheaputilities.{h,cpp}` | PR adds `FinalizeLoad` with heap type and fast functions. We add the version tails and negotiation. | Merged by git. One fix (section 4). |
| `vm/gchelpers.{h,cpp}` | Main removed the global allocation context. The PR guards stock paths with `g_heap_type != GC_HEAP_CUSTOM`. We changed `Alloc` and the publish step. | By hand. `Alloc` keeps our `sampledInfo` and uses the single thread context of main. `ErectWriteBarrierForMT` keeps the PR guard and our dependent-edge pre-store. |
| `vm/interpexec.cpp` | Async continuation copy. | By hand. PR `bulk_move_with_write_barrier` for custom collectors. Our exact logging for LXR. Cards are set only if neither applies. |
| `vm/loaderallocator.cpp` | `CompareExchangeValueInHandle`. | Both hooks run (as for `Interlocked`). |
| `vm/object.cpp`, `vm/object.h` | PR: `SetObjectReferenceUnchecked` hook. Ours: layout store changes. | Merged by git. |
| `vm/syncblk.cpp` | PR guards `IllegalAlignPad` for custom collectors. We removed `m_alignpad` (header bits). | Ours. The function is gone. |
| `vm/writebarriermanager.{h,cpp}` | PR adds `WRITE_BARRIER_CUSTOM` and moves the barrier copy code here. We add two slot-log barrier types. | Both sets of cases in every `switch`. |

For rows that say "merged by git" I did not review every line. The two builds, the tests and the hashes in section 5 are the check.

### 6.2 Files where only main conflicts with us

**Code.** Main changed these places after our old base. I resolved each by hand.

| File | What main did | Decision |
|---|---|---|
| `inc/corjitflags.h`, `jit/jitee.h`, SuperPMI (`methodcontext.cpp`, `spmidumphelper.cpp`) | Main took flag bits 32 and 33. | Our two flags move to **34 and 35**. |
| `inc/jiteeversionguid.h` | Main changed the GUID. | New GUID (**Open:** final one later). |
| `jit/layout.cpp`, `jit/compiler.h` | Main removed the layout-number API. | We keep only our extended `typGetCustomLayout` with the GC layout class handle. |
| `jit/lower.cpp` | Main added `CORINFO_HELP_BULK_WRITEBARRIER_SMALL`. | Stock path uses main's helper. Our old-value path uses `..._WITH_LAYOUT`. |
| `jit/gentree.cpp` | Main added the small helper to the list of helpers that need `vzeroupper`. | All four helpers are in the list. |
| `vm/jitinterfacegen.cpp` | Main removed the single-processor allocation helpers. | We keep only the stock and the notifying choice. |
| `gc/*` (LXR files) | Main removed `QueryPerformanceCounter`, `QueryPerformanceFrequency`, `GetLowPrecisionTimeStamp`. | `minipal_hires_ticks`, `minipal_hires_tick_frequency`, `minipal_lowres_ticks`. |
| `vm/syncblk.h` | Main removed `__SwitchToThread`. | `SwitchToThread()`. |
| CoreLib `RuntimeType.BoxCache.cs` | Main removed `MethodTable.InstantiationArg0`. | `NullableType.AsMethodTable()`. |
| `nativeaot/Runtime/Pal.h` | `E_INVALIDARG` was not defined for Unix NativeAOT. | Defined. |
| `nativeaot/Runtime/Full/CMakeLists.txt` | Main restructured the server collector target. | Our test defines are added inside the new structure. |
| `vm/reflectioninvocation.cpp` | Main moved reflection invocation to managed code. | Our one-line change (`memmoveGCRefsWithLayout` for the return buffer) has no place any more. **Open** (section 13). |
| Satori `SatoriGC` | Main added `IGCHeap::GetPendingBridgeHandles` after the PR base. | Satori returns no handles. |
| `LxrGcHeap` | Same new methods. | Stock heap type, no fast functions, no custom barrier code, no bridge. |

## 7. How to build and run

### 7.1 Linux x64 (Release)

```bash
git switch kkokosa-lxr-integrated
./build.sh -s clr -c Release -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1"
./build.sh -s libs -c Release
bash docs/design/lxr-port/P19-integration/scripts/mkframe.sh "$PWD" Release int
```

- **Measured.** I used exactly these calls in a WSL Ubuntu 22.04 copy of the tree (clang 14, 16 cores). `clr` took 17 minutes and `libs` 11 minutes while other jobs ran.
- **Measured.** I did the build in an exported tree without a `.git` folder. The package projects stop with "RepositoryCommit must be specified". The product files are complete. A build in a git clone does not need this. In an exported tree add `/p:RepositoryCommit=<any commit id>` to every `build.sh` call.
- `-DCLR_CMAKE_ENABLE_LXR_GC=1` builds `libclrlxrgc.so`. Without it the build has no LXR collector library.
- `mkframe.sh` makes `$HOME/lxr-frames/int-Release-linux`.
- Debug collector (for the managed cells): `bash .../mkdbgcollector.sh "$PWD" int`.
- Optional Satori comparison: `cmake -DCLR_CMAKE_ENABLE_SATORI_GC=1 .` in the CoreCLR build folder, then `ninja satorigc`. Start with `DOTNET_GCName=libsatorigc.so`.

### 7.2 Windows x64 (Release)

```powershell
subst W: <path of the repository>
cd W:\
.\build.cmd -s clr+libs -c Release -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1"
```

- **Measured.** Result: "Build succeeded, 0 warnings, 0 errors" in 22 minutes (other jobs ran at the same time). It builds `coreclr.dll`, `clrjit.dll`, `System.Private.CoreLib.dll`, all libraries and `clrlxrgc.dll`.
- **Measured.** Use a short path (`subst`). In a deep worktree path the NativeAOT descriptor targets fail with `C1083: Cannot open compiler generated file`.
- A frame is `artifacts\bin\runtime\net11.0-windows-Release-x64`, plus the `*.dll`, `*.exe` and `*.json` files of `artifacts\bin\coreclr\windows.x64.Release`, plus `IL\System.Private.CoreLib.dll`.

### 7.3 Select the collector

| Setting | Meaning |
|---|---|
| (nothing) | The default collector. |
| `DOTNET_GCPath=<frame>/libclrlxrgc.so` (Windows: `clrlxrgc.dll`) | Load LXR. |
| `DOTNET_GCName=libsatorigc.so` | Load Satori (comparison build only). |
| `DOTNET_TieredCompilation=0` | Used in all our runs. (**D1:** `DOTNET_ReadyToRun=0` is no longer required. The framework is compiled with `--old-value-write-barriers`.) |
| `DOTNET_gcServer=0`, `DOTNET_gcConcurrent=1`, `DOTNET_GCHeapCount=2` | The settings of all our runs. |
| `DOTNET_LXRLegacyCapacity=1`, `DOTNET_LXRManagedHeapHardLimitMB=<heap>`, `DOTNET_LXRImmixInitialMB`, `DOTNET_LXRLOSInitialMB` | Fixed heap size. |
| `DOTNET_LXREvacuateNursery=0\|1`, `DOTNET_LXREvacuateMature=0\|1` | Mode: `0,0` non-moving, `1,0` nursery copy, `1,1` mature copy. |

### 7.4 The three demo programs

The programs are the three workloads of `GcBench.cs` (P14): binary trees, requests and list suffix. Build the program outside the repository folder, because the repository `Directory.Build.props` applies inside it:

```bash
mkdir -p ~/gcbench/src && cp docs/design/lxr-port/P14-natural-gc/GcBench.* ~/gcbench/src/
(cd ~/gcbench/src && "$OLDPWD/.dotnet/dotnet" build GcBench.csproj -c Release -o ~/gcbench/net)
```

**Measured.** This builds `gcbench.dll`. Its checksums equal those of the binary that all earlier measurements used.

Linux, one run (the script prints one JSON line):

```bash
D=docs/design/lxr-port/P19-integration/scripts/demo.sh
F=$HOME/lxr-frames/int-Release-linux
bash $D $F default binarytrees 16 0
bash $D $F lxr     binarytrees 16 0
bash $D $F lxr     requests    32768 600000
bash $D $F lxr     listsuffix  4096  800000
```

Windows (PowerShell):

```powershell
$fr = 'C:\lxr-int\frame-win-Release'
$env:DOTNET_GCPath = "$fr\clrlxrgc.dll"
$env:DOTNET_TieredCompilation = '0'; $env:DOTNET_ReadyToRun = '0'; $env:DOTNET_gcConcurrent = '1'
$env:DOTNET_GCHeapCount = '2'; $env:DOTNET_gcServer = '0'; $env:DOTNET_LXRLegacyCapacity = '1'
$env:DOTNET_LXREvacuateNursery = '0'; $env:DOTNET_LXREvacuateMature = '0'
$env:DOTNET_LXRImmixInitialMB = '80'; $env:DOTNET_LXRLOSInitialMB = '20'; $env:DOTNET_LXRManagedHeapHardLimitMB = '100'
& $fr\corerun.exe C:\lxr-int\gcbench\gcbench.dll binarytrees 16 0 run
```

**Measured.** On Linux and Windows the three programs print the same checksum with LXR and with the default collector (binary trees 14985972 on both). Windows requests and list suffix used the nursery mode with heaps of 84 MiB and 40 MiB.

## 8. Test results

All on the integrated tip. Linux numbers are from WSL Ubuntu 22.04.

| Suite | Platform | Result |
|---|---|---|
| Native selectors (32: the default one and 31 options), `P17-pause-cost-and-reserve/native-linux.sh` | Linux Release | **32 of 32 pass.** |
| Native selectors | Linux Debug | **32 of 32 pass.** |
| Native selectors, `P17-pause-cost-and-reserve/native-windows.ps1` | Windows Release | **32 of 32 pass.** |
| Managed cells with the **Debug collector** (P17 `run-managed.py` with `LXR_NATIVES=Debug`, see `scripts/managed-debug-collector.sh`): 3 modes, 18 cases with the Release fixture and 3 with the Debug fixture | Linux | **63 of 63 pass**, including the S6 finalizer cases. The `full` case needs 100,632 checks and has them. |
| Legacy suites P2.1 to P3.4 (9 scripts) | Linux | 8 pass. P3.1 failed once ("non-owner work stealing observed", 641 of 642) while two builds ran. On an idle machine it passed **8 of 8** runs, and the S6 source passed 8 of 8 as well. **Inferred:** the check depends on timing and on free cores. |
| Debug runtime (Debug `libcoreclr`, `libclrjit`, `CoreLib`): demo program with the default collector and with LXR | Linux | Both run. Same checksum. |
| Build of `clr.runtime` Debug (JIT with asserts), `clr` and `libs` Release | Linux | No compile errors. (The package projects stop on `RepositoryCommit` in a tree without `.git`, see section 7.1.) |
| Optional Satori build, started with `DOTNET_GCName` | Linux | Loads and runs binary trees, same checksum. |
| Default-collector tests | Linux | See section 5 (38 of 38). |

**Not run (Open):** managed cells with the Release collector and on Windows, the legacy suites on Windows, Windows Debug builds, ARM64, the P1.x barrier and allocation validation programs, SuperPMI, crossgen, the JIT test suite. The managed fixtures (`runtime-tests.dll`) are the ones that I built from the S6 sources earlier. I did not rebuild them.

## 9. Benchmark

Question: do the numbers of the accepted build and of the S6 tip survive the move to the new main?

**Setup (Measured).** Linux x64 Release, WSL, 16 cores. Heap 4x of the minimum heap. Three programs. Modes non-moving and nursery copy. Hard heap limit, as in the P17 matrices. Two repetitions in two parallel streams, each stream on its own five cores. Inside one repetition the three builds run in a shuffled order. Command: `scripts/bench-wall.sh`.

| Label | What it is |
|---|---|
| `acc` | The accepted `e8d2d23` build (its own runtime and collector). |
| `s6` | The S6 tip collector on the accepted runtime (the way P17 measured S6). |
| `int` | This branch: new main, the PR runtime side, our runtime, S6 collector. |

All checksums are equal for each program (binary trees, requests and list suffix).

**Wall time, ms. Mean of two runs, then the two runs.**

| Program | Mode | acc | s6 | int |
|---|---|---|---|---|
| binary trees | non-moving | 4809 (4670, 4949) | 2205 (2301, 2110) | 2277 (2416, 2137) |
| binary trees | nursery | 5492 (5712, 5271) | 2170 (1836, 2504) | 2072 (1916, 2229) |
| requests | non-moving | 33651 (35478, 31824) | 21657 (20957, 22357) | 21252 (21091, 21414) |
| requests | nursery | 42142 (43017, 41267) | 8317 (7813, 8820) | 8207 (8409, 8004) |
| list suffix | non-moving | 27573 (23224, 31921) | 19504 (24363, 14644) | 13832 (12847, 14817) |
| list suffix | nursery | 66128 (74025, 58231) | 31726 (21080, 42373) | 21859 (27776, 15942) |

**GC time, ms (runtime counter).**

| Program | Mode | acc | s6 | int |
|---|---|---|---|---|
| binary trees | non-moving | 1869 | 539 | 525 |
| binary trees | nursery | 2533 | 392 | 366 |
| requests | non-moving | 20471 | 10892 | 10701 |
| requests | nursery | 27826 | 1314 | 1356 |
| list suffix | non-moving | 16231 | 10618 | 6178 |
| list suffix | nursery | 54900 | 23968 | 13008 |

**Peak RSS, MiB.**

| Program | Mode | acc | s6 | int |
|---|---|---|---|---|
| binary trees | non-moving | 229 | 194 | 189 |
| binary trees | nursery | 229 | 155 | 146 |
| requests | non-moving | 370 | 296 | 264 |
| requests | nursery | 369 | 280 | 280 |
| list suffix | non-moving | 299 | 272 | 273 |
| list suffix | nursery | 331 | 257 | 256 |

**Pause table (Measured).** Traced runs, `scripts/bench-trace.sh` and `scripts/pause-pool.py`. The pause is the stop-the-world time of the S0 timers (for `acc`: the runtime pause counter). Each cell: number of pauses, median, maximum, in ms, pooled over the two repetitions.

| Program | Mode | Pause kind | acc | s6 | int |
|---|---|---|---|---|---|
| binary trees | non-moving | RefCount | 30, 98, 171 | 32, 5, 221 | 31, 4, 239 |
| binary trees | non-moving | InitialMark | 2, 316, 325 | 2, 219, 231 | 2, 233, 246 |
| binary trees | non-moving | FinalMark | 2, 162, 186 | 2, 50, 64 | 3, 20, 218 |
| binary trees | nursery | RefCount | 30, 115, 179 | 37, 5, 147 | 36, 6, 130 |
| binary trees | nursery | InitialMark | 2, 376, 377 | 1, 294, 294 | 1, 217, 217 |
| requests | non-moving | RefCount | 7, 106, 117 | 8, 6, 44 | 8, 6, 36 |
| requests | non-moving | Full | 105, 359, 766 | 106, 203, 332 | 106, 202, 314 |
| requests | nursery | RefCount | 6, 148, 185 | 126, 15, 75 | 127, 14, 79 |
| requests | nursery | InitialMark | 4, 241, 316 | 6, 77, 97 | 6, 70, 97 |
| requests | nursery | FinalMark | 2, 256, 265 | 6, 16, 31 | 6, 9, 32 |
| list suffix | non-moving | RefCount | 103, 166, 599 | 115, 43, 463 | 109, 46, 596 |
| list suffix | non-moving | InitialMark | 4, 488, 830 | 2, 590, 603 | 2, 531, 573 |
| list suffix | nursery | RefCount | 128, 690, 1539 | 119, 428, 680 | 103, 63, 532 |

The full table (all kinds) is in `results/pause-table.txt`. In the requests nursery cell, `s6` and `int` did not run any full collection. The accepted build ran 104.

**What the numbers show.**

- **Measured.** `int` is close to `s6` in every non-noisy cell: binary trees, requests and peak RSS. The differences are inside the spread of the two runs. The move to the new main does not remove the S6 gain over the accepted build.
- **Measured.** List suffix is noisy. The two runs of one build differ by up to a factor of 2.7 (`s6` nursery: 21080 and 42373 ms). `int` is faster in the mean, but the spread is as large as the difference. **Inferred:** do not read a gain from the list suffix cells.
- **Measured.** The nursery median of the RefCount pause in list suffix differs between `s6` (428) and `int` (63). The same cell has maxima of 680 and 532 ms. Two runs are too few for this median.
- **Open.** Two repetitions are lean. P17 used three. I did not repeat the cells.

## 10. Known failures that are not new

From P17, and I did not re-run them here:

- List suffix at a heap of 2x runs out of memory.
- Requests at 2x with a hard limit does not finish.

**Measured.** I found no new failure in the runs of this report, except the P3.1 timing failure in section 8.

## 11. What is not in this branch

| Item | State | Cost or effect |
|---|---|---|
| **Specialized-ABI barrier** (`GetAssignRefFunctions`, assembly barrier with patchable slots) | **Done in D1** (P21). | NativeAOT is still not supported for LXR. |
| **Bump-pointer allocation** (stock helpers, GC learns about objects at buffer refill) | **Done in D2** (P22). | **Measured:** 11 to 26 ns per allocation (D1 notifying helpers: 78 to 96 ns). The P1.4 churn scenario (14 percent Workstation, 24 percent Server) was not re-run: its harness needs the removed test collector. |
| **S7** (the pin list) | Not in `kkokosa-lxr-integrated`. Done on the branch `kkokosa-lxr-s7-pin-list` (see below). | |
| **R2R** | **On in D1** for a framework compiled with `--old-value-write-barriers` (P21, section 9). | An unmarked image is rejected for LXR (it runs through the JIT). |
| ARM64 | No LXR barrier. The PR ARM64 files for Satori are in the tree and not built here. | |

### S7 addition

The branch `kkokosa-lxr-s7-pin-list` holds the 22 commits of the integrated branch and the S7 commits (P17 S7a to S7d). **Code.** The S7 commits change only the collector module, the native and managed tests, and the documents. They do not change the runtime. **Measured:** the machine-code hash of the stock helpers is still `46cee8bd87fd6252f5265267148d0a34`.

- The per-object tables `m_flags`, `m_trace_locks` and `m_trace_scans` are removed. Pinning uses a pin list per pause, one byte per Immix block, and pinned blocks for the pinned object heap.
- **Measured** (heap 4x, two repetitions): native planes 212 to 160 MiB (binary trees) and 456 to 336 MiB (requests); peak RSS 185 to 136 MiB and 279 to 159 MiB; wall and GC time equal or lower; equal checksums.
- A pin-heavy program failed now and then under CPU oversubscription. The integrated tip failed in the same way, and so did the accepted build. The cause is found (a race of two threads that get one fresh block) and fixed on the branch `kkokosa-lxr-root-capture-fix` (see "Root capture fix addition" below).
- The details, the controls and the open items are in the P17 README, section "S7". Build and test commands of this report are unchanged.

### Root capture fix addition

The branch `kkokosa-lxr-root-capture-fix` holds the S7 branch plus the commits of the root capture investigation (P17 README, section "The cause of the failure and its fix"). **Measured:** without the fix, 32 of 416 oversubscribed runs of the pin-heavy program failed; with the fix, 0 of 416. **Code:** the fix is one condition in `ImmixBlockManager::TryAcquire` (a fresh mutator acquire refuses a nursery block under the block lock). The accepted build `e8d2d23` has the same code and the same failure (5 of 120 runs). The runtime is not changed. **Inferred:** before the fix, the race could also corrupt memory without a crash.

## 12. Open items

1. **Epoch reset in `RestartEE`.** The callback stays in `GCToEEInterface::RestartEE`. It is not moved into the collector. (Step 5 of the plan: left as it is.)
2. **Heap type.** (Closed in D1: LXR reports `GC_HEAP_CUSTOM`.) Before D1, LXR reported `GC_HEAP_WKS`. The PR guards use `g_heap_type != GC_HEAP_CUSTOM`. So LXR takes the stock paths where the PR has a custom-collector branch. This works in all tests. No audit was done.
3. **Reflection invocation.** Main moved it to managed code. Our logging for the return-buffer copy is not ported. **Inferred:** a value type with references that is returned through reflection may copy references without an old-value log. No test covers it.
4. **Final JIT-EE GUID**, and a decision about the flag bits 34 and 35 if upstream takes them first.
5. **The 5.8 compatibility tests** of P1.1.
6. **Hook unification.** (Done in D1: one hook, the PR `check_escape`, plus the LXR extension of P21 section 8.)
7. **A bisect build** of every commit.

## 13. How to share the branch

I did not push, and I did not open a pull request. You decide.

**Files for sharing without a push** (outside the repository): `C:\Users\konradkokosa\lxr-integrated-share\`

| File | Content |
|---|---|
| `kkokosa-lxr-d2-bump-allocation.bundle` | `git bundle` of `a485c819c17..kkokosa-lxr-d2-bump-allocation` (the integrated branch, S7, the root capture fix, the D1 commits and the D2 commits). The receiver needs `dotnet/runtime` at `a485c819c17` (fetch `upstream/main`). |
| `patches\*.patch` | `git format-patch` series of the same range (one file per commit). |
| `SHA256SUMS.txt` | Hashes of the files above. |
| `previous\kkokosa-lxr-d1-specialized-barrier\` | The bundle, the patch series and the hashes of the branch before D2. |
| `previous\kkokosa-lxr-root-capture-fix\` | The bundle, the patch series and the hashes of the branch before D1. |
| `previous\kkokosa-lxr-s7-pin-list\` | The bundle, the patch series and the hashes of the S7 branch before the root capture fix. |
| `previous\` | Also the bundle, the patch series and the hashes of the integrated branch before S7. |

Use of the bundle:

```bash
git clone https://github.com/dotnet/runtime.git && cd runtime
git checkout a485c819c17
git fetch ../kkokosa-lxr-d2-bump-allocation.bundle kkokosa-lxr-d2-bump-allocation:kkokosa-lxr-d2-bump-allocation
git switch kkokosa-lxr-d2-bump-allocation
```

To publish it later: `git push <remote> kkokosa-lxr-d2-bump-allocation`. I advise a branch name that tells people that it is an experiment, for example `lxr-experiment`. The PR commits keep the authorship of `janvorli`. Say in the description that they come from PR #3316 and that they are not yet in `dotnet/runtime`.

## 14. Not checked

- Anything that I list as Open or Not run above.
- Whether a clean `git clone` builds with the two commands of section 7.1 in one call. I built `clr` and `libs` in two calls in an exported tree.
- Whether the series builds at every commit (section 3).
- The accepted-build and S6 arms of the benchmark use the accepted runtime. The `int` arm uses a different runtime (new main). So the benchmark compares runtime plus collector together, not the collector alone.

## 15. Scripts

All in `docs/design/lxr-port/P19-integration/scripts/` (the scripts of D1 are in `P21-specialized-barrier/scripts/`, the scripts of D2 in `P22-bump-allocation/scripts/`): `helperhash.py` (section 5), `jit-compare.sh`, `gc-library-tests.sh`, `mkframe.sh`, `mkdbgcollector.sh`, `demo.sh`, `managed-debug-collector.sh`, `bench-wall.sh`, `bench-trace.sh`, `summarize.py`, `pause-pool.py`. The raw results are in `results/` (`wall.jsonl`, `pause-table.txt`, `helper-hashes.txt`).
