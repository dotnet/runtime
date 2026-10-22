# P21: LXR write barrier under the Specialized ABI (step P18-D1)

Status: done on branch `kkokosa-lxr-d1-specialized-barrier`. Local commits only. Nothing was pushed.

Labels: **Measured** = I ran it and saw the result. **Code** = I read it in the code. **Inferred** = I conclude it from
other facts. **Open** = not known or not done.

## 1. Summary

- **Code.** The write barrier of LXR is now assembly. The runtime installs it through the mechanism of PR #3316
  (`IGCHeap::GetAssignRefFunctions`). This is the **only** way a collector changes the write barrier. The user asked
  for this: "the 3316 PR's needs to be the only one".
- **Code.** Our own mechanism is gone: the two `WriteBarrierManager` types (slot log and its write-watch twin), the five
  patched fields, the JIT flag `USE_STANDARD_WRITE_BARRIER_ABI`, the Standard-ABI paths in the JIT and the VM, the
  separate pre-store calls, `ErectWriteBarrierRangePre`, and the test-only build option.
- **Code.** The JIT now treats the LXR barrier like the stock one. It knows which registers the helper may change
  (the Specialized ABI). So a store stays small. ReadyToRun code works.
- **Measured.** ReadyToRun is **on** in all managed tests of this step. "R2R must be off" is removed.
- **Measured.** The code of the default collector is unchanged (hash of the stock helpers is equal to main plus the PR).
- **Open.** ARM64 is not done (section 10). Windows Debug is not built (user decision).
- **Open (known limit).** The slow path does not save the APX registers r16 to r31 (section 3). The runtime does not use them today.

## 2. How the barrier works now

A diagram in words. The JIT emits a call to `CORINFO_HELP_ASSIGN_REF` (or `CORINFO_HELP_CHECKED_ASSIGN_REF`) for
every reference store into the heap. The helper is the LXR code. It is **not** the stock helper.

1. The call arrives at `LxrCheckedAssignRef` (checked variant) or `LxrAssignRef` (unchecked variant).
   Input: destination `dst` in the first argument register, new value in the second.
2. The checked variant subtracts the heap start (`HeapLow` slot) from `dst` and compares with the heap size
   (`HeapSize` slot). If `dst` is outside the LXR heap, it stores and returns. Nothing else happens.
3. The unchecked variant (and the checked variant for a destination inside the heap) computes the side-metadata byte:
   `byte = MetadataBase[dst >> 6]` and `bit = (dst >> 3) & 7`. One bit stands for one 8-byte field slot.
4. A **clear** bit means "this field was already logged in this epoch". The barrier stores the new value and returns.
   This is the fast path. It does not call C++ code. It reads one byte of metadata. It does not read the old value.
5. A **set** bit means "not logged yet". The barrier jumps to the slow path (section 4). The slow path calls the C++
   function `LxrBarrierSlowPath(dst)`. That function reads the **old value** from `*dst`, claims the bit with an atomic
   compare-exchange, and writes (slot, old value) into the chunk of the thread. Then the barrier stores the new value
   and returns.
6. There is no post-store work. The hook `write_barrier` of the PR (called after the store by the VM for stores
   that do not use the helper) does nothing for LXR.

**Code.** Why the old value is read first in C++: when two threads store into one field, the thread that loses
the claim stores later than the winner has claimed. The winner must have read the value before any other store lands.
The test `old-value-race` checks this. Its control (`LXR_WB_CONTROL_...` "read old value after the claim") fails
as required.

Because LXR logs a field once per epoch, the slow path is rare. A C++ call with a full register save is acceptable.

## 3. Register contract

**Code.** This is the contract that the JIT assumes (`RBM_CALLEE_TRASH_WRITEBARRIER`).

| Platform | Arguments | The helper may change | The helper must keep |
|---|---|---|---|
| System V x64 | rdi = destination, rsi = new value | rax, rcx, rdx, rsi, rdi, r8, r9, r10, r11, flags | all other general registers and **all vector state** |
| Windows x64 | rcx = destination, rdx = new value | rax, rcx, rdx, r8, r9, r10, r11, flags | all other general registers and **all vector state** |

- The fast path is a leaf routine. System V: it changes only rax, rcx and r8. Windows: it changes only rax, r8 and r9.
  All of these are in the set that the helper may change.
- The slow path calls C++ code. C++ code can change every vector register. So the slow path saves the whole vector
  state with `xsave` and restores it with `xrstor` (section 4).
- **Open.** APX registers r16 to r31 are not saved. The runtime does not use APX today. If it does, add them.

## 4. The slow path

**Code.** In the order of the instructions (`lxr_writebarrier.S` and `.asm`):

1. Push `rbp`. Keep `dst` and the new value on the stack (the call changes the argument registers).
2. Reserve 2816 bytes and align the stack to 64 bytes.
3. Clear the 64-byte header of the xsave area.
4. Read the xsave mask from the patchable slot `XsaveMask`. The mask is `XCR0 & 0xE7`: x87, SSE, AVX, opmask,
   upper ZMM, ZMM16-31. If the mask is zero (the OS has no xsave), use `fxsave` and `fxrstor` instead.
5. `xsave`. Then `vzeroupper` if AVX is on, so the C++ code does not pay for mixed SSE and AVX state.
6. Call `LxrBarrierSlowPath` through the patchable slot `SlowPath`. The stack is 16-byte aligned at the call
   (Windows adds 32 bytes of shadow space).
7. `xrstor` (or `fxrstor`). Restore `rsp`, `rsi`, `rdi`, `rbp`. Do the store. Return.

The slow path has unwind information (CFI on Linux, `.pdata` on Windows). The runtime unwinds copied code through the
original address (`AdjustWriteBarrierIP`). So an exception or a stack walk inside the C++ call works.
A fault at the store reports the exact address (`...AVLocation` labels in the list of access-violation locations).

This is the change that reviewers asked for. Satori saves a few registers by hand. LXR saves everything.

## 5. How the runtime installs the barrier (PR mechanism)

**Code.**

- LXR returns `GC_HEAP_CUSTOM` from `GetGCHeapType`. It returns two block descriptions from `GetAssignRefFunctions`:
  the checked and the unchecked helper, plus the list of access-violation locations and the list of patchable values.
- The runtime copies the block `[LxrCheckedAssignRef, LxrWriteBarrierBlock_End)` to an executable page and installs
  the copy as `CORINFO_HELP_CHECKED_ASSIGN_REF` and `CORINFO_HELP_ASSIGN_REF` (`WRITE_BARRIER_CUSTOM`).
- Five 8-byte slots live inside the block: `MetadataBase`, `HeapLow`, `HeapSize`, `SlowPath`, `XsaveMask`.
  The runtime copies the current value of a GC global into each slot after the copy and at every
  `StompWriteBarrier`. The globals are in `lxr_write_barrier.cpp` (`g_lxr_write_barrier_globals`).
- Inside the block the code uses only local labels and RIP-relative slots. So a copy at another address works.
- `lxr_gcload.cpp` is unchanged. `g_heap_type` is set after `GC_Initialize`, so the first `StompWriteBarrier` only sets
  globals. The custom barrier is installed later in `InitJITWriteBarrierHelpers`.
- A custom heap reports `IsServerGC = true` (the PR includes CUSTOM in `IsServerHeap()`). The managed fixture does
  not assert the opposite any more.

## 6. Hooks at the VM call sites: one set

**Code.** The PR gives three hooks in `FastGCFunctions`: `check_escape(dst, ref)`, `write_barrier`,
`bulk_move_with_write_barrier`, plus `is_in_gc_heap`. LXR uses them like this:

| Hook | LXR implementation |
|---|---|
| `check_escape(dst, ref)` | Called **before** the store at the VM sites (`SetObjectReferenceUnchecked`, the two Interlocked exchanges, `CompareExchangeValueInHandle`, array copy). Rule: it may read `*dst`, because the store did not happen yet. It logs the old value if the field is not logged. |
| `write_barrier` | Nothing. The old value was logged before the store. |
| `bulk_move_with_write_barrier` | Logs the old values of the destination range of a plain reference range (metadata-byte scan). The VM then copies. |
| `is_in_gc_heap` | Range test of the LXR heap. Used by the conservative scan and by `IsObjRefValid`. |

Answer to design item 2. **Code.** There is no `JIT_ByRefWriteBarrier` in this tree. The JIT uses the checked helper for
byref stores. So the PR interface already covers every helper that LXR must customize. No new helper hook was needed.

## 7. What was removed

**Code.** Compared with the tip `a1ddb3fd4a1`, the tracked source files lose about 2800 lines and gain about 430
(`git diff --stat`). The new assembly and its C++ glue are new files.

- `WRITE_BARRIER_SLOT_LOG64` and the write-watch twin in `WriteBarrierManager`, their five patched fields,
  `ConfigureWriteBarrier` code for them, `slotlogwritebarrier.S` and `SlotLogWriteBarrier.asm`.
- `writebarriermanager.{cpp,h}`, `jithelpers_fastwritebarriers.{S,asm}`, `excep.cpp`, `gcenv.ee.cpp`,
  `loaderallocator.cpp`, `arraynative.inl`, NativeAOT `gcenv.ee.cpp`: back to the PR version.
- The JIT flag `USE_STANDARD_WRITE_BARRIER_ABI` and every use of it: kill-set special cases (`codegencommon.cpp`,
  `emit.cpp`), the larger minimum argument area (`lower.cpp`), `gcinfo.cpp`, `assertionprop.cpp`, `importercalls.cpp`,
  the interpreter, and the SuperPMI names. `lsrabuild.cpp` is stock again.
- The JIT-EE GUID is new: `5eb70a2c-175c-4d4d-b12d-95d48caeabf9`.
- The GC interface minors 6.1 to 6.4 (shape, complete-store, epoch reset, bulk scan) and the parameters
  `WriteBarrierShape`, side-metadata fields, slow-path typedefs. Minor 6.1 now means the old-value hooks (section 8).
- `ErectWriteBarrierPre`, `ErectWriteBarrierRangePre`, the dependent-edge hook, `ClassifyWriteBarrierBulk`.
- The test-only build option `CLR_CMAKE_ENABLE_WRITE_BARRIER_STANDARD_ABI_TEST`, its exports files, and
  `standardwritebarriertest.*`. `CLR_CMAKE_ENABLE_LXR_RC_VERTICAL` does not force it on any more.
- Software write watch is **not used** for a custom heap (PR behaviour). LXR has its own logging, so we did not keep it.
- The "ReadyToRun must be off" gate in `ConfigureAllocationNotification` and the same text in LXR initialization.
  **Measured.** LXR runs with ReadyToRun on and the allocation notification works (all tests below).
- The epoch-reset callback. It was only a sanity check.

## 8. What stays as an "LXR extension" and why

These are **not** a second barrier mechanism. They are small additions on top of the PR mechanism. LXR needs the
**old** value of every overwritten field. The PR hooks cover only the escape of the **new** value. Each item below
is a candidate for an upstream proposal.

| # | Extension | Why LXR needs it | Where |
|---|---|---|---|
| 1 | `FastGCFunctions.flags` with `FASTGC_REQUIRES_OLD_VALUE` and `pre_store_range(ctx, dst, src, count)` (minor 6.1) | The VM must know that the collector needs a call **before** stores of a range. The PR has only `bulk_move_with_write_barrier`. | `gcinterface.h`, `gcheaputilities.*` |
| 2 | JIT flag `WRITE_BARRIER_REQUIRES_OLD_VALUE` and the intrinsic `RequiresOldValueWriteBarrier` | The JIT must not drop a barrier for a store of null, a frozen object or a provably safe value, because LXR logs the **old** value. Today the JIT drops those barriers. | `jitee.h`, `jit/*`, `jitinterface.cpp` |
| 3 | Layout bulk helpers `CORINFO_HELP_BULK_WRITEBARRIER_WITH_LAYOUT` and `..._CLEAR_WITH_LAYOUT`, and the JIT lowering that uses them | A struct copy, clear or fill with references needs the exact positions of the references. The stock bulk helper has only an address range. | `lower.cpp`, `jithelpers.h`, `gchelpers.cpp`, `comutilnative.cpp`, `arraynative.cpp` |
| 4 | CoreLib paths `*OldValueWriteBarrier` and their FCalls (`Buffer`, `Array.Clear`, fill, copy) | Managed code that clears or copies reference memory without a barrier (`Span.Clear`, `Array.Clear`) must log the old values. | CoreLib, `comutilnative.cpp` |
| 5 | ReadyToRun image flag `READYTORUN_FLAG_OLD_VALUE_WRITE_BARRIERS` (0x4000), crossgen2 option `--old-value-write-barriers`, R2R helper IDs 0x35 and 0x36 | Precompiled code has the barrier decisions of its compile time (section 9). | `readytorun.h`, `readytoruninfo.cpp`, crossgen2 |
| 6 | Allocation notification, header bits, object enumeration (minors 6.5 to 6.7) | Not part of the barrier. They are the D2 and tracing contracts of earlier steps. Unchanged. | `gcinterface.h` |

The helper `ErectWriteBarrierObjectRangePre` and the layout functions return `false` for a collector that does not
set `FASTGC_REQUIRES_OLD_VALUE`. So the default collector and Satori take their old paths.

## 9. ReadyToRun: the finding and the decision

**Measured.** I first built a CoreLib with stock crossgen2 and ran the new cell `r2r-stores`
(section 11). The cell **failed**: the decrement entries were 105 of 2000 for `List.Clear`. A precompiled method
drops the barrier for a store of null, and it folds `RequiresOldValueWriteBarrier()` to false. LXR then misses
the old value.

**Decision (Code).** Precompiled code is allowed only when it was compiled for an old-value collector:

- crossgen2 has the new option `--old-value-write-barriers`. It sets the JIT flag and the image flag.
  `crossgen-corelib.proj` passes it when you build with `/p:CrossgenOldValueWriteBarriers=true`.
- The runtime rejects a ReadyToRun image **without** the flag when the collector needs the old value
  (fail closed; it falls back to the JIT, which is the same as "R2R off" for that image). The test-only setting
  `DOTNET_ReadyToRunAcceptUnmarkedImages=1` accepts such images. The control of the cell uses it.
- Only CoreLib of the product build is precompiled in a frame. The other libraries are IL in our frames.

### 9.1 How to build the framework with the flag

**Code.** The coordinator approved this design (fail closed).

- CoreLib of the product build: add the property to the normal build command.
  `./build.sh -s clr -c Release -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1" /p:CrossgenOldValueWriteBarriers=true`
  (Windows: `build.cmd -s clr+libs -c Release -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1" /p:CrossgenOldValueWriteBarriers=true`).
  The property adds `--old-value-write-barriers` to the crossgen2 call in `src/coreclr/crossgen-corelib.proj`.
- Any other assembly, by hand: pass `--old-value-write-barriers` to crossgen2. Example (Linux, used for the control):
  `dotnet crossgen2.dll System.Private.CoreLib.dll -o out/System.Private.CoreLib.dll -r "<IL dir>/*.dll" --targetos:linux --targetarch:x64 -O --old-value-write-barriers`.
- **ALL framework assemblies that are precompiled need the flag**, not only CoreLib. Every ReadyToRun image that has
  stores of references can drop a barrier for null, frozen or provably safe values. The same is true for user
  assemblies and for composite images. In our frames only CoreLib is precompiled, so only CoreLib needs the flag there.
- **Open.** I did not add the option to the shared crossgen2 setup of the libraries (the SDK task for `libs`).
  Before a product uses precompiled libraries with LXR, that setup must pass the option to every image.

### 9.2 What happens with an unmarked image

**Code and Measured.** The check is in `ReadyToRunInfo::Initialize` (`readytoruninfo.cpp`). It runs when the
collector reports `FASTGC_REQUIRES_OLD_VALUE` and the image header does not have `READYTORUN_FLAG_OLD_VALUE_WRITE_BARRIERS`.

- The runtime does not use the image. It returns "no ReadyToRun image" and compiles the methods with the JIT.
  The program runs and gives the right result (**Measured:** checksum 14985972, same as the default collector).
- The message is **not printed to the console**. It goes to the ReadyToRun log file only. Set
  `DOTNET_ReadyToRunLogFile=<path>` (the process id is added to the name). **Measured**, the line in the file is:
  `Ready to Run disabled - image was not compiled with old-value write barriers: "<path of the image>".`
- Startup cost: every method of the rejected image goes through the JIT. **Measured, one run per case, binary trees
  16 with LXR on Linux, whole process:** rejected CoreLib 2135 ms, flagged CoreLib 1763 ms, CoreLib not used at all
  (`DOTNET_ReadyToRun=0`) 1900 ms. This is one run each and the machine is noisy, so the numbers show only the
  direction. The cost is mostly JIT time at start.
- `DOTNET_ReadyToRunAcceptUnmarkedImages=1` turns the check off. **This makes LXR unsound.** It exists only for
  the control of the cell `r2r-stores`.

### 9.3 Upstream proposal

This flag is an **upstream-proposal item** (extension 5 in section 8). It belongs to the old-value contract: a
collector that needs the old value must tell crossgen2, and the runtime must refuse images that did not follow it.
A smaller upstream design could use one collector-neutral image flag, for example "barriers are never elided".

## 10. ARM64 status

**Open.** ARM64 is not done. This was agreed (ARM64 is not required now). LXR builds only for x64.

What remains for ARM64:
1. Write `lxr_writebarrier.S` for ARM64 under the ARM64 Specialized contract. Use `gc_satori/arm64/writebarrier.S`
   as the template. Save all vector registers that C++ code can change (v0 to v31 low parts that the contract
   keeps, and the full state if the contract says so). Check the contract in `RBM_CALLEE_TRASH_WRITEBARRIER` for ARM64.
2. Patchable slots and relocation: the ARM64 PR code uses `adrp`/`ldr` into the block. Follow it.
3. Memory ordering: the claim and the store need the right barriers (`dmb`) on ARM64.
4. Run the native barrier test with the ARM64 assembly. The test is written for x64 register names.
5. Crossgen2 for ARM64 accepts the same flag, so no extra work is expected there (Inferred).

## 11. Tests and results

All results are for this branch. "Linux" is WSL Ubuntu 22.04. "Windows" is x64 with MSVC.

### 11.1 New tests

| Test | What it checks | Result |
|---|---|---|
| `lxr-write-barrier-test.cpp` (Linux and Windows) | fast path does not call C++ and stores; unlogged field logs (slot, old value) once; null old and null new value; bit positions; checked variant inside and outside the heap; all vector registers kept across the slow path (the stub clobbers them with `vzeroall`; xmm0-15 and the full xsave image are compared); stack alignment at the C++ call; fault location; concurrent stores into one field and into different fields; old value under racing stores; slots start empty and take the patched values; copy at another address; new metadata base and new heap bounds after a slot update | **Measured.** Pass on Linux and on Windows. |
| Controls (5) | `NO_VECTOR_SAVE` breaks `vector`; `MISALIGN` breaks `alignment`; `NO_STORE_AFTER_SLOW` breaks `store-after-slow`; "read old value after claim" breaks `old-value-race`; "no patch" breaks `patch` | **Measured.** Each control fails the named check on Linux and on Windows (the `no-patch` control crashes on Linux, which also counts as a failure). |
| Managed cell `r2r-stores` (`P5/runtime-tests/R2RStores.cs`) | 16 framework store paths clear references from precompiled CoreLib code: `List.Clear`, `List.RemoveAt`, `Array.Clear`, `Span.Clear`, `Span.Fill(null)`, `Array.Copy` and `Span.CopyTo` of nulls, `Array.SetValue(null)`, `Dictionary.Remove`, `Array.Clear` of a struct array, `Array.Copy` of empty structs, a struct copy into an array element, `Interlocked.Exchange`, `Interlocked.CompareExchange`, a struct store into a boxed struct, a field store into a boxed struct (`Unsafe.Unbox`). For each path the cell needs at least 90% of the old values as decrement entries and the same share of deaths. | **Measured.** Pass (66 checks) on Linux (all three modes, Debug collector) and on Windows. |
| Control of `r2r-stores` | A CoreLib compiled without the flag, with `DOTNET_ReadyToRunAcceptUnmarkedImages=1`. | **Measured.** The cell fails (`List.Clear`: 105 decrement entries of 2000, 0 deaths). Without the setting the runtime rejects the image and the cell passes. |


### 11.2 Existing suites

| Suite | Result |
|---|---|
| Native selectors, Linux Release (33 selectors) | **Measured.** failed=0 |
| Native selectors, Linux Debug (33 selectors) | **Measured.** failed=0 |
| Native selectors, Windows Release (33 selectors) | **Measured.** failed=0 |
| Legacy P2.1 to P3.4 (Linux, 9 programs) | **Measured.** failed=0 |
| Managed cells, Debug collector, ReadyToRun **on** (Linux, 81 runs: 3 modes x 27 cases) | **Measured.** 81 of 81 pass, including `r2r-stores`. |
| Smoke, Linux Release: binary trees, LXR, ReadyToRun on | **Measured.** Checksum 14985972, equal to the default collector. |
| Smoke, Linux full **Debug** runtime (asserts on), LXR, ReadyToRun on | **Measured.** Checksum 14985972. |
| Smoke, Windows Release, LXR, ReadyToRun on | **Measured.** Checksum 14985972. |
| Satori (opt-in build), binary trees and requests | **Measured.** Builds and runs with the PR mechanism. Checksums equal to the default collector. |

Notes:
- The `full` managed case now has 100631 checks (before: 100632). One check was removed on purpose: "the collector is
  not the server collector". A custom collector reports `IsServerGC = true`. `run-managed.py` has the new number.
- **Open.** In the first run of the managed matrix one case (`weak-churn`, mature mode) ended with
  `E_FAIL` from `GC.Collect` (exit -6). It passed 26 of 26 reruns, and in the second full run of the matrix.
  The first run happened while a Windows build used all cores. I did not find the cause. I did not test the tip
  `a1ddb3fd4a1` for the same effect. Treat it as a possible flake that exists in the collector, not in the barrier
  (Inferred: the error comes from the collection path, not from a store).
- The built Linux Release tree differs from the final sources only in one local variable name in `comutilnative.cpp`
  (`useSlotLog` was renamed). I did not rebuild after it.

### 11.3 Default collector unchanged

**Measured.** I built `clr.runtime` for main plus the PR commits (`1f64164273c`, the baseline) and for this branch.
The script `P19-integration/scripts/helperhash.py` disassembles the stock write barrier and allocation helpers
and hashes the text.

| Tree | TOTAL |
|---|---|
| main + PR (baseline, re-recorded in this step) | `46cee8bd87fd6252f5265267148d0a34` |
| this branch (D1) | `46cee8bd87fd6252f5265267148d0a34` |

The per-helper dumps are also identical (`diff -r` is empty). The value is the same as in P19.
**Not run:** the hash on Windows (the script is Linux only), the comparison of the JIT output with `jit-compare.sh`,
and the library tests `gc-library-tests.sh` (they need the host and pretest builds).

## 12. Benchmarks (information only; no gate)

**Measured.** Linux x64 Release in WSL, 16 cores. Baseline `tip` = the frame of `a1ddb3fd4a1` (same runtime sources;
collector built from the same GC sources; ReadyToRun off, because the tip needs it). `d1` = this branch (ReadyToRun on).
Heap 4x of the minimum. Two repetitions in two parallel streams (cores 0-4 and 5-9), builds interleaved in a shuffled
order. One run per cell and repetition. The noise is large.

| Workload | Mode | tip elapsed ms (rep 1, rep 2) | d1 elapsed ms (rep 1, rep 2) | Checksum |
|---|---|---|---|---|
| binary trees | nonmoving | 2227, 2188 | 2252, 1999 | equal |
| binary trees | nursery | 2224, 2491 | 2116, 1679 | equal |
| requests | nonmoving | 15845, 15991 | 16024, 15468 | equal |
| requests | nursery | 7048, 7154 | 6517, 6459 | equal |
| list suffix | nonmoving | 10461, 9427 | 11302, 10846 | equal |
| list suffix | nursery | 11879, 22479 | 17649, 29918 | equal |

- **Measured.** All 24 runs finished. All checksums are equal across builds and modes.
- **Inferred.** Binary trees and requests: no clear difference (d1 is equal or a little faster; ReadyToRun gives faster
  start). List suffix nonmoving: d1 is about 11% slower on the mean. List suffix nursery: the two builds differ
  by more than the spread between their own repetitions, so I make no claim.
  The log of the data is in `/root/d1/bench`; I did not keep a copy in the repository.
- **Open.** I did not find the source of the list suffix difference. The collector code is the same. The barrier
  code differs (fast path reads one metadata byte in both). One candidate is the cost of the C++ call with the
  `xsave` and `xrstor` pair in the slow path: it costs about 100 to 300 cycles, and list suffix has many first
  stores to mature fields in each epoch (Inferred; not measured separately).

### Store micro benchmark

`StoreBench` (`P21-specialized-barrier/StoreBench`). It stores references in a loop without allocation: scalar
fields (two stores per iteration), array elements, and struct copies (two references per element). Best of 5
repeats per process, 5 processes per build, pinned to one core. Numbers are the range over the 5 processes.

| Build | scalar ns/store | array ns/store | struct copy ns/reference |
|---|---|---|---|
| tip, LXR (ReadyToRun off) | 1.94 to 1.99 | 1.79 to 1.89 | 2.11 to 2.17 |
| d1, LXR (ReadyToRun on) | 1.96 to 2.04 | 2.03 to 2.12 | 2.14 to 2.33 |
| d1, LXR (ReadyToRun off) | 1.98 to 2.02 | 1.87 to 2.04 | 2.07 to 2.16 |
| tip, default collector | 2.76 to 2.79 | 2.39 to 2.41 | 2.24 to 2.29 |
| d1, default collector | 2.82 to 2.89 | 2.38 to 2.42 | 2.24 to 2.28 |

- **Measured.** Scalar stores are equal on tip and d1 (about 2.0 ns). The LXR fast path is faster than the stock
  barrier in this loop (**Inferred:** the stock barrier also checks generations and writes card bytes).
- **Measured.** Array stores with ReadyToRun on are about 12% slower than the tip (2.03 to 2.12 against 1.79 to 1.89).
  With ReadyToRun off on the same frame the difference is mostly gone (1.87 to 2.04). **Inferred:** the array store goes
  through the helper `StelemRef` in CoreLib. In the d1 frame it is precompiled code. In the tip frame the JIT
  compiled it with full optimization.
- **Measured.** Code size of the store methods (`DOTNET_JitDisasm`, full opts): `ScalarStores` 161 bytes,
  `ArrayStores` 129 bytes, `StructCopies` 200 bytes. These are equal for tip and d1. The call to the helper has
  the same shape. Only the kill set (the registers that the JIT must treat as changed at the call) differs in
  principle: before, the Standard ABI killed the full caller-saved set. In these small loops the register allocator
  had no live value that this change affects.

## 13. Risks

1. **Slow path cost.** `xsave` of the 2688-byte state, `xrstor`, and a C++ call. About 100 to 300 cycles
   (Inferred, from the instruction costs; not measured). It happens once per field and epoch. A workload with
   many first stores per epoch pays more (list suffix may show it).
2. **Fail-closed R2R.** A framework that is not compiled with `--old-value-write-barriers` runs through the JIT
   (slow start, correct). The test knob that accepts unmarked images makes LXR unsound. Do not use it outside the test.
3. **JIT-EE interface change.** The GUID changed. Any external JIT must be rebuilt.
4. **The 6.1 interface tail.** `FastGCFunctions` grew by two fields. A GC built for the earlier layout is handled:
   the runtime clears the tail for a GC with a lower minor version.
5. **Native test platform.** The barrier test sets the patchable slots by itself. It checks the same layout as the
   runtime through a `static_assert` in `lxr_gc.cpp`, but it does not run inside the runtime. The managed cells do.
6. **APX and future registers.** Not saved (section 3).
7. **Unknown flake** in `weak-churn` (section 11.2).
8. **The extension is large.** The old-value contract touches the JIT, the VM, CoreLib and crossgen2. An upstream
   proposal needs a smaller design (for example: a collector flag "needs old value" and one generic helper).

## 14. Decisions I made (and why)

1. Keep the JIT flag and the intrinsic. The JIT must keep barriers for null, frozen and safe stores. The flag is
   set from `GCHeapUtilities::RequiresOldValueWriteBarrier()`, which reads `FASTGC_REQUIRES_OLD_VALUE` from the PR
   structure. (Conservative: one source of truth, no new GC call.)
2. The fast path does not read the old value. The slow path reads it before the claim (race rule, section 2).
3. Save **all** vector state with `xsave` instead of a hand list. The cost is paid only in the slow path.
4. No assembly-only append to the chunk of the thread. The Specialized contract is hard to prove for that code
   (`INLINE_GET_ALLOC_CONTEXT_BASE` changes volatile registers). This stays an optional later step.
5. The R2R image flag, fail closed (section 9). The other choice was to keep "R2R off", which the task forbids.
6. Software write watch is removed for LXR (not needed).
7. Allocation notification works with ReadyToRun on: the gate was removed. The R2R allocation helpers use the same
   helper table as the JIT, and the managed cells pass.
8. `CLR_CMAKE_ENABLE_LXR_RC_VERTICAL` does not turn on a test option any more.

## 15. How to repeat the work

The scripts are in `P21-specialized-barrier/scripts`. They show the paths that I used (WSL `/root/d1`, Windows
`C:\lxr-d1`). Change the paths for your machine.

- Build Linux: `scripts/build-clr.sh Release` (and `Debug`), `scripts/build-libs.sh Release`. The build command is
  `build.sh -s clr -c Release -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1" /p:CrossgenOldValueWriteBarriers=true`.
- Build Windows (`subst W: <repo>`): `build.cmd -s clr+libs -c Release -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1" /p:CrossgenOldValueWriteBarriers=true`.
  **Measured.** 0 warnings, 0 errors, 19 minutes.
- Frames: `scripts/mkframe-r2r.sh <repo> Release d1 r2r`, `scripts/mkframe-dbg.sh`, `scripts/mk-unflagged.sh` (the
  control CoreLib).
- Native barrier test: `native-barrier-linux.sh <repo> <out>` and `native-barrier-windows.ps1 -Output <dir>`.
- Managed cells: `scripts/mkfixtures.sh`, then `scripts/run-managed-d1.sh <frame> <out> Debug`.
  One cell: `scripts/run-case.sh <frame> r2r-stores 0 0`.
- Hash: `scripts/run-hash.sh`. Benchmarks: `scripts/bench-d1.sh`, `scripts/sum-bench.py`, `scripts/bench-store.sh`.
- Satori: `scripts/mk-satori.sh`.
