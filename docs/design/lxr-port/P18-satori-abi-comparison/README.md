# P18 - Satori PR #3316 and our LXR port: the GC-to-runtime ABI compared

**Status:** research only. No product code changed. Nothing was built or run for this report.
**Date:** 2026-10-20.
**Question (from the user):** "looking at the whole discussion around the GC - runtime ABI, and write
barriers, and their implementation of Satori port - PR #3316
(https://github.com/dotnet/runtimelab/pull/3316), explain how we did it, how it compares to what is
merged by that PR and what we will need to change/extend."

## 0. How to read this report

### 0.1 Labels

Every claim has one label.

| Label | Meaning |
|---|---|
| **Code** | I read it in source at the named commit. The path and the line or function follow. |
| **Quoted** | It comes from the PR discussion or from issue #3318. A link follows. |
| **Measured** | A number. Either I produced it in this session (a `git`, `gh` or `grep` count), or it was recorded in one of our earlier P1.x documents. The second kind is marked "from P1.x, not re-run". |
| **Inferred** | My conclusion from the facts above. It can be wrong. |

### 0.2 What was read

| Item | Identity |
|---|---|
| Our port | branch `lxr`, commit `e8d2d23ef60` (the accepted baseline). This report branch starts there. |
| Our upstream base | `df9c6988c20`, a dotnet/runtime commit of 2026-08-06. **Measured:** `git merge-base lxr upstream/main`. |
| The PR | dotnet/runtimelab PR #3316, "Custom GC support with Satori", author `janvorli`. Merged 2026-10-19 as `68135571b69` into branch `feature/gc/baseline`. Two commits: `432092f8e05` and `25880504544`. |
| The tree before the PR | `93ff17fc48f` (2026-10-08). |
| How I read the PR code | A shallow clone of dotnet/runtimelab in a temporary folder, outside this repository, at `68135571b69`. The folder is deleted. Paths marked "PR:" or named without a prefix in the PR sections are paths in that tree. |
| Discussion | The 40 inline review comments, 7 issue comments and the review list of the PR, and the body of issue [#3318](https://github.com/dotnet/runtimelab/issues/3318) (it has no comments). Read with `gh api`. |

### 0.3 Words that this report uses

The reader knows .NET GC basics. These words are specific to the two designs.

| Word | Plain meaning |
|---|---|
| Write barrier | A small piece of code that runs on every store of an object reference into a heap field. The JIT calls a helper for it. |
| Card marking | The stock barrier. It records "this memory area was written". The GC later scans only marked areas. |
| Unlog bit (LXR) | One bit in a side table for every field slot. If the bit says "not yet logged", the first store to the slot must log the **old** value in the slot. The Java LXR and our port use this to count references and to keep a snapshot for tracing. |
| Old value | The object reference that the store overwrites. Card marking never needs it. LXR always needs it, also when the new value is null. |
| Escape tracking (Satori) | Satori keeps young objects private to one thread. The barrier checks whether a store makes a private object reachable by another thread (the object "escapes"). It looks at the **new** value only. |
| Allocation context | A per-thread record (`gc_alloc_context`) with `alloc_ptr` and `alloc_limit`. The runtime helpers allocate by moving `alloc_ptr` up (bump-pointer allocation) and call the GC only when the buffer is full. |
| Standalone GC | A GC built as a separate shared library. The runtime loads it at startup (`DOTNET_GCName`). It implements `IGCHeap` and uses `IGCToCLR` to call the runtime. |
| Specialized barrier ABI | The calling rules of the stock x64 barrier helpers. The JIT assumes the helper changes only some integer scratch registers. SIMD registers stay unchanged. |
| Standard barrier ABI | Our P1.1 mode. The JIT treats the barrier call like an ordinary C call. The helper may change every volatile register. |
| R2R | ReadyToRun. Precompiled framework code. It embeds the Specialized barrier contract. |
| The Java LXR | The original LXR in MMTk and OpenJDK. |

## Short answer

1. **The PR and our port solve different problems.** The PR lets the runtime host a second full GC
   (Satori) whose barrier needs only the **new** value of a store. It changes no JIT code. Our port
   needs the **old** value of every overwritten field, also for null stores, so it changes the JIT,
   CoreLib and the allocation helpers. (Code: the PR touches no file under `src/coreclr/jit` or
   `src/coreclr/inc`; see section 3.1.)
2. **Barrier selection differs.** The PR makes the GC return two assembly functions
   (`GetAssignRefFunctions`). The runtime copies them to an executable page and installs them as
   `CORINFO_HELP_ASSIGN_REF` and `CORINFO_HELP_CHECKED_ASSIGN_REF`. Values that change at run time
   (card tables, barrier state) live in "patchable slots" inside the copied code. We instead added
   two new barrier *types* to the stock `WriteBarrierManager` (slot-log, and slot-log with software
   write watch) and patch five fields in their templates.
3. **The barrier ABI differs.** Satori keeps the Specialized ABI. Its C++ slow path is called after
   the assembly code saves registers by hand. Reviewers said this is fragile because a compiler may
   one day use SIMD registers there (Quoted, section 3.4). We chose the Standard ABI, so the C++
   slow path is safe. The price is a JIT flag, more code in every store, and "R2R must be off".
4. **Allocation differs most.** The PR leaves all allocation helpers unchanged. Satori hands the
   stock helpers a zeroed buffer and does no work per ordinary object on the fast path. We built a family of notifying
   helpers that call the GC once for every object. **Measured (from P1.4, not re-run):** this
   costs 14% (Workstation) and 24% (Server) of throughput in the churn scenario.
5. **Interface numbers collide.** The PR sets GC interface 6.0 and EE interface 6. We are at 5.15
   and EE 4. Current dotnet/runtime main is at 5.9 and EE 5 (**Measured**, `gh api`, 2026-10-20),
   so our minor numbers 5.9 to 5.15 already clash with upstream. Both the PR and our port add new
   `IGCHeap` methods at the end of the vtable, so the slots clash too.
6. **Both changes edit the same code.** 25 files are changed by both (**Measured**). About five VM
   call sites get a hook in both (`SetObjectReferenceUnchecked`, the two `Interlocked` exchanges,
   `LoaderAllocator::CompareExchangeValueInHandle`, the array memmove helper), with different hooks.
7. **What we can reuse from the PR:** the custom-GC heap type, `is_in_gc_heap`, the patchable-slot
   method for barrier code, the bulk-move callback, the pre-store call sites, ARM64 support, and the
   Satori build as a comparison baseline.
8. **What the PR does not give LXR:** old-value semantics (the JIT must not drop null, frozen or
   proven-safe stores), logging for `Clear`, `Fill`, box and struct copy with exact layout, an
   allocation-complete step, negotiated header bits, and object-range enumeration.
9. **The first decision for the coordinator** is the barrier ABI: use the PR's mechanism with an
   LXR-written assembly barrier and Specialized ABI (R2R works, slow path must be assembly or must
   save all SIMD state), or keep our Standard ABI. Section 7 proposes a measured experiment.
10. **Rebase meaning.** The PR is in an *experiment* branch of dotnet/runtimelab. Its code is not in
    dotnet/runtime. Our fork follows dotnet/runtime. We must carry the PR runtime changes as a patch
    series and renumber our interface (section 7.4).

## 1. The GC-to-runtime ABI in three plain sections

"ABI" here means every agreement between the GC and the runtime: the C++ interfaces, the data
shapes, and the machine code that the runtime patches or the JIT calls. The tables show the stock
state (dotnet/runtime at our base), what the PR changed, and what our port changed.

### 1.1 What the runtime asks the GC (`IGCHeap`)

`IGCHeap` is a C++ abstract class with about 90 virtual methods (**Code:** `src/coreclr/gc/gcinterface.h`).
The runtime calls it through `g_pGCHeap`. A standalone GC returns its object from `GC_Initialize`.

| Group | Examples (methods) | PR change | Our change |
|---|---|---|---|
| Allocate | `Alloc(gc_alloc_context*, size, flags)`, `PublishObject(obj)`; flags `GC_ALLOC_FINALIZE`, `_ZEROING_OPTIONAL`, `_LARGE_OBJECT_HEAP`, `_PINNED_OBJECT_HEAP` | New flag `GC_ALLOC_IMMORTAL = 128` (PR: `gc/gcinterface.h:1178`). Satori uses it inside (`gc_satori/SatoriAllocator.cpp:301`). The VM never sets it (**Code:** `git grep` in PR `vm` and `gc`). | None. |
| Collect and state | `GarbageCollect`, `FixAllocContext`, `SetGCInProgress`, `WaitUntilGCComplete` | None. | None. |
| Handles | `IGCHandleManager`, `IGCHandleStore` (create, destroy, store, dependent handles) | None in the interface. Shared handle-table sources get `#ifdef FEATURE_SATORI_GC` blocks (PR: `gc/objecthandle.cpp`, `gc/handletable.cpp`). | None. |
| New: fast callbacks | `GetFastGCFunctions(FastGCFunctions*)` (PR: `gc/gcinterface.h:148,1139`) | Fields: `context`, `write_barrier`, `is_in_gc_heap`, `check_escape`, `bulk_move_with_write_barrier`. | Not present. We use negotiation structures instead (last row). |
| New: heap type | `GetGCHeapType()`, new value `GC_HEAP_CUSTOM = 3` (PR: `gc/gcinterface.h:624,1142`) | Added. The runtime stores it in `g_heap_type`. | We report `GC_HEAP_WKS` (**Code:** `src/coreclr/gc/lxr_gcload.cpp:50`). |
| New: barrier code | `GetAssignRefFunctions(...)` returns two code addresses and sizes, a list of AV (fault) addresses, and patchable slots (PR: `gc/gcinterface.h:1153`) | Added. | Not present. |
| New: counters | `GetTotalAllocatedBytesPrecise()` | Added, because Satori counts bytes inside its own contexts. | None. |
| Negotiation | `GetAllocationNotificationParameters()` (5.13), `GetObjectReferenceEnumerationParameters()` (5.14), `GetObjectHeaderBitsParameters()` (5.15) (ours: `gc/gcinterface.h:1342,1351,1360`) | None. | Added. Each returns a descriptor. The runtime writes `Accepted` or `Unsupported` into it before `IGCHeap::Initialize`. |

**Version and slot facts (Code):**

- Stock at our base: GC 5.8, EE 4 (`git show df9c6988c20:src/coreclr/gc/gcinterface.h`).
- PR: GC **6.0**, EE **6** (`gc/gcinterface.h:9,14,18`). The loader rejects a standalone GC with a
  lower major number (`vm/gcheaputilities.cpp:295`). Every older standalone GC must be rebuilt.
- Ours: GC **5.15**, EE 4. Minors 5.9 to 5.15 are our own (`gc/gcinterface.h:16-22`).
- Both sides append new `IGCHeap` methods after `NullBridgeObjectsWeakRefs`. The PR adds four. We
  add three. **Inferred:** whichever lands second moves the other's slots, so a merge needs one
  agreed order.

### 1.2 What the GC asks the runtime (`IGCToCLR`, and `IGCToCLREventSink`)

`IGCToCLR` (class `GCToEEInterface` on the runtime side) has about 50 methods
(**Code:** `src/coreclr/gc/gcinterface.ee.h`).

| Group | Methods | PR change | Our change |
|---|---|---|---|
| Suspension | `SuspendEE`, `RestartEE(bFinishedGC)` | None. | `RestartEE` calls our epoch-reset callback when the GC finished (`vm/gcenv.ee.cpp:93-103`). |
| Roots | `GcScanRoots`, `GcStartWork`, `BeforeGcScanRoots`, `AfterGcScanRoots`, `GcDone`, `RefCountedHandleCallbacks`, `SyncBlockCache*` | New: `GcScanCurrentStackRoots(fn, sc)` and `GcPoll()` (EE interface 6). Implemented in the VM (`vm/gcenv.ee.cpp:307,600`). NativeAOT has empty `TODO` stubs. | None. |
| Threads and contexts | `GetThread`, `GetAllocContext`, `GcEnumAllocContexts`, `EnablePreemptiveGC`, `DisablePreemptiveGC`, `CreateThread` | None. | None. |
| Barrier | `StompWriteBarrier(WriteBarrierParameters*)` with `WriteBarrierOp` | New op `StompCustom` and a field mask `parameters_mask`. The VM copies only the masked fields into its globals (`vm/gcenv.ee.cpp:1244`). | New tail in `WriteBarrierParameters` (5.9 to 5.12): shape, side-metadata base, slow-path callbacks, epoch reset, bulk-scan range (`gc/gcinterface.h:296-378`). Used only on `Initialize`. |
| Diagnostics | `Diag*`, `AnalyzeSurvivors*`, `FireGC*` through `EventSink()` | None. | None. |
| Config | `GetBooleanConfigValue`, `GetIntConfigValue`, `GetStringConfigValue` | None. | LXR reads its `DOTNET_LXR*` switches through these. |

`IGCToCLREventSink` is a second interface inside `gcinterface.ee.h`. It has about 35 `Fire...`
methods, one for each GC event (`FireGCStart_V2`, `FireBGCBegin`, `FireSetGCHandle` and so on). The
runtime forwards them to ETW and EventPipe. Neither the PR nor our port changes it (**Code:**
`gcinterface.ee.h` is unchanged in our diff; the PR adds only the two methods above).

**Quoted:** VSadov notes that a custom GC may want events of its own and that Satori has them in
`vm/gctoclreventsink.cpp`. He says this has "no impact on the current PR"
([discussion](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4233041919)).

### 1.3 What is patched into JIT or VM code

This is the part that is not a C++ interface. It is machine code and data that the JIT and the
runtime both depend on.

| Item | How it works in stock dotnet/runtime | Who must agree |
|---|---|---|
| Write barrier helpers `CORINFO_HELP_ASSIGN_REF`, `CORINFO_HELP_CHECKED_ASSIGN_REF` | Assembly code in a "patched code" region. At startup the VM copies it to a fresh executable page (`WriteBarrierManager`) and writes constants (card table address, heap bounds, region shift) into the copy as immediate values. The JIT calls the helper with the target slot in `RDI` (System V) or `RCX` (Windows) and the new value in `RSI` or `RDX`. | JIT, `WriteBarrierManager`, GC (it supplies the values through `StompWriteBarrier`), R2R and NativeAOT images. |
| Register contract of the barrier | The JIT assumes the x64 helper changes only integer scratch registers. SIMD and mask registers stay valid. **Code:** `jit/targetamd64.h:205-223` (`RBM_CALLEE_TRASH_WRITEBARRIER = RBM_INT_CALLEE_TRASH_INIT`). | JIT register allocator and every barrier implementation. |
| JIT elision rules | The JIT removes the barrier when the new value is null, a frozen-object handle, or proven to be a non-heap value (`jit/gcinfo.cpp:240`, `jit/assertionprop.cpp`). A card-marking barrier does not need those stores. | JIT and GC. |
| `JIT_CheckedWriteBarrier` (x64) | Stock helper in the patched region. It compares the target address with `g_lowest_address` and `g_highest_address`, then jumps to the main barrier. Outside the heap it stores only (**Code:** `vm/amd64/patchedcode.S:80-101`). | VM globals and GC heap bounds. |
| Byref barrier | **Code:** there is no `CORINFO_HELP_ASSIGN_BYREF` in `inc/jithelpers.h` in either tree. Stores through unknown byrefs use the checked helper. | - |
| Bulk helper `CORINFO_HELP_BULK_WRITEBARRIER` | Calls managed `Buffer.MemCopyGC`, then the native `memmoveGCRefs`, which copies and marks cards (`vm/arraynative.inl`, `InlinedMemmoveGCRefsHelper`). | CoreLib, VM and GC. |
| Allocation helpers `CORINFO_HELP_NEWSFAST`, `NEWARR_1_*`, string allocator | Assembly (`RhpNewFast`, `RhpNewArrayFast`, ...). They move `alloc_ptr` of the thread's `ee_alloc_context` and write the MethodTable. They call the GC only when the buffer is full. | JIT (helper IDs and inlined sequences), VM, GC (it fills `alloc_ptr` and `alloc_limit`). |
| GC globals used by code | `g_card_table`, `g_card_bundle_table`, `g_lowest_address`, `g_highest_address`, `g_ephemeral_low`, `g_ephemeral_high`, `g_region_to_generation_table`, `g_region_shr`, `g_write_watch_table`, `g_sw_ww_enabled_for_gc_heap`. | GC (writes them through `StompWriteBarrier`), barrier code, VM checks such as "is this address in the heap". |
| Object header | 8 bytes before the MethodTable pointer: a 32-bit padding word `m_alignpad` and the 32-bit sync-block word. Lock, hash and GC bits share the second word. | VM (locks, hash), GC (pin and finalizer bits). |

## 2. How our port did each part

### 2.1 Shape of the port

- **Code:** our GC is a standalone library `clrlxrgc` (`libclrlxrgc.so`). It is built from
  `src/coreclr/gc` (files `lxr_*.cpp`, `immix_*`, `large_object_space*`, `side_metadata*`,
  `reference_count*`). CMake switch `CLR_CMAKE_ENABLE_LXR_GC`, off by default
  (`src/coreclr/gc/CMakeLists.txt:344`). It reports interface 5.15 and heap type `GC_HEAP_WKS`
  (`gc/lxr_gcload.cpp:24,50`). The P5 document says that this host adds no new VM, JIT or GC-EE
  interface beyond the P1 capabilities ([`P5/README.md`](../P5/README.md)).
- **Measured:** `git diff --name-only df9c6988c20..lxr` lists 97 files in `src/coreclr` (outside
  `gc` and `gc_lxr`), `src/native`, `src/libraries`, `src/installer` and `eng`. That includes build
  and test scaffolding. For runtime, JIT, interpreter, tools, NativeAOT and CoreLib code only
  (`git diff --shortstat` on those folders, without test files and export lists) the port changes
  80 files: +4,267 and -143 lines. The PR changes 47 files outside Satori: +1,214 and -315 lines
  (`git diff --numstat`).
- **Roadmap rules R1 to R5** (`.github/extensions/lxr-gc-roadmap/roadmap.md`, operating rule 5):
  generalize the mechanism, not the client; zero measured cost when unused, chosen at init time
  rather than by a branch; additive and version-gated; independently defensible without LXR;
  performance evidence per change.

### 2.2 The logging write barrier and its fast path

**What LXR needs (Code, P1.2 document).** Before a field is overwritten, test the unlog bit of the
field slot in a side table. If the bit says "work", call the collector with the destination, the old
value and the new value. The collector logs the old value. The store follows.

**What we built:**

1. The GC asks for the shape `SideMetadataFieldLog` in the `Initialize` call of `StompWriteBarrier`
   (**Code:** `gc/lxr_gc.cpp:464-476`: metadata base, granularity shift 3, "work when bit is set",
   callbacks, bulk-scan range).
2. The runtime validates the request and answers `Accepted` or `Unsupported` before it changes any
   global (**Code:** `vm/gcenv.ee.cpp:1151-1230`). It rejects the request if R2R is on, if barrier
   copying is off, or if the architecture is not x64.
3. `WriteBarrierManager` gets two new types, `WRITE_BARRIER_SLOT_LOG64` and
   `WRITE_BARRIER_WRITE_WATCH_SLOT_LOG64`, and patches five fields in the template: metadata base,
   slow-path address, byte shift, bit shift, polarity (**Code:** `vm/writebarriermanager.cpp:214-232`
   and the `UpdatePatchLocations` cases near line 1153).
4. The fast path is a leaf routine of about ten instructions. It loads the old value, computes the
   metadata byte address, loads the byte, tests the bit and stores. Only if the bit says "work" does
   it jump to a slow stub (**Code:** `vm/amd64/jithelpers_fastwritebarriers.S:797-830`, and the MASM
   twin).
5. The slow stub is a normal function with unwind information. It calls the collector's C++
   callback `LxrGcHeap::WriteBarrier`, which logs the old value into a per-thread chunk
   (**Code:** `vm/amd64/slotlogwritebarrier.S:7-24`, `gc/lxr_gc.cpp:777`).
6. The JIT treats the call as an ordinary C call: **Standard ABI** (below).

**Measured, from P1.2, not re-run (Windows x64, one machine):**

| Case | Field store | Array store |
|---|---:|---:|
| Stock card barrier | 3.033 ns | 4.092 ns |
| Slot-log, bit already claimed (fast path) | 2.161 ns | 3.822 ns |
| Slot-log, slow path with empty callback | 14.39 ns | 16.12 ns |

An end-to-end low-allocation run was 0.922 (Workstation) and 0.933 (Server) of stock, but the
document states this is below the 8.35% host resolution floor.

**The Standard ABI (P1.1).** The runtime selects the mode once at startup. The JIT receives two
flags (**Code:** `inc/corjitflags.h`, `jit/jitee.h`): `CORJIT_FLAG_USE_STANDARD_WRITE_BARRIER_ABI`
and `CORJIT_FLAG_WRITE_BARRIER_REQUIRES_OLD_VALUE`. With Standard ABI the barrier call kills all
volatile registers (`jit/codegencommon.cpp:921-926`, `jit/emit.cpp`, `jit/lsrabuild.cpp:4550`).

**Measured, from P1.1, not re-run:** managed vector-store methods grow from 108 to 155 bytes
(`Vector128`), 111 to 236 (`Vector256`) and 129 to 260 (`Vector512`). A field store grows from 225
to 233 bytes. An empty C++ callback costs +2.1 ns per store over stock before any metadata test.

**Why we chose it (Code and Inferred).** The stock Specialized contract keeps SIMD registers alive
(`targetamd64.h:205-223`, the result of dotnet/runtime PR 128778, merged 2026-06-02, **Measured:**
`gh pr view`). A C++ slow path may use those registers. P1.1 rejected an XSAVE stub and chose a
second compiler-visible contract. The price: R2R and NativeAOT code embed the Specialized contract,
so the mode **fails closed** with them. Framework assemblies are R2R by default, so every LXR run
sets `DOTNET_ReadyToRun=0` (**Code:** `vm/gcheaputilities.cpp:248-252`; `P4/README.md:258`: "Set
`DOTNET_ReadyToRun=0`").

**Old-value semantics in the JIT (Code).** With the flag set, the JIT keeps barriers for null,
frozen-handle and proven stores: `jit/gcinfo.cpp:238-240`, `jit/assertionprop.cpp` (the
`VNVisitReachingVNs` removal is skipped), `jit/importercalls.cpp` (exchange and compare-exchange
intrinsics). It folds the new intrinsic `RuntimeHelpers.RequiresOldValueWriteBarrier()` to a
constant (`jit/importercalls.cpp`, `interpreter/compiler.cpp`).

### 2.3 Bulk-copy, clear, fill, box and struct-copy barriers

All of these write references **without** going through the scalar helper. A card-marking GC needs
only a "mark the range" call after the copy. LXR must log the old values **before** the copy,
including when the source is null (clear).

What we added (**Code**):

- A range classifier `ClassifyWriteBarrierBulk`. It reads the side metadata in native words with
  exact end masks and answers `CardTable`, `AllClaimed` or `NeedsSlowPath`
  (`vm/gchelpers.cpp:1713-1815`). `AllClaimed` skips the callback.
- Pre-store entries `ErectWriteBarrierPre`, `ErectWriteBarrierRangePre` and layout variants
  (`vm/gchelpers.cpp:1816-1993`). The range callback runs once before the first destination byte
  changes. A null source means "clear".
- `memmoveGCRefsWithLayout` (`vm/arraynative.cpp`). It walks the MethodTable's GC descriptor, so
  non-reference words in mixed structs are copied but never reported as references.
- Two new JIT helper IDs `CORINFO_HELP_BULK_WRITEBARRIER_WITH_LAYOUT` and
  `CORINFO_HELP_BULK_WRITEBARRIER_CLEAR_WITH_LAYOUT` (`inc/corinfo.h`, `inc/jithelpers.h`), and JIT
  lowering for large GC-containing struct copy, clear and `initobj` (`jit/lower.cpp`, function
  `ShouldUseLayoutBulkHelper` at line 10562 and the bulk-copy and bulk-clear lowering near it;
  `jit/layout.cpp` and `jit/layout.h` keep the GC layout identity).
- CoreLib code: `Buffer.BulkMoveWithOldValueWriteBarrier`, `ClearWithOldValueWriteBarrier`,
  `BulkFillWithOldValueWriteBarrier`, and the call sites in `Array`, `Span.Clear`,
  `SpanHelpers.Fill`, `Object.MemberwiseClone`, box and unbox in `CastHelpers`, and `BoxCache`.
- The same hooks in the interpreter (`vm/interpexec.cpp`, `interpreter/compiler.cpp`).

**Measured, from P1.3, not re-run:** the all-claimed early-out makes 16-reference `Array.Copy` 0.170
of the P1.2 per-slot time. A bounded helper for 48 KB structs is 1.5 to 1.7 times slower than the
per-slot code. We accepted that to bound JIT code size (106,418 bytes of generated code became 49).

### 2.4 Checked and byref barriers

- **Code:** the checked helper is the stock `JIT_CheckedWriteBarrier`. It tests the target against
  `g_lowest_address` and `g_highest_address` and jumps to the slot-log barrier
  (`vm/amd64/patchedcode.S:80-101`). Our GC publishes its contiguous reservation as those bounds
  (`gc/lxr_gc.cpp:443-444`).
- **Code (P1.3 ledger):** no byref helper exists. A store through a byref goes to the checked
  helper. Byrefs into the stack or into frozen memory fall outside the bounds, so no log happens.
- **Inferred:** a design that does not use one contiguous reservation could not reuse this test.

### 2.5 Allocation: notification, bump pointers, allocation contexts

**What LXR needs (Code, P1.4):** a callback after the runtime has written the MethodTable (and array
length) and before the object is visible. The callback receives the object, the aligned size and
facts (`ContainsReferences`, `Finalizable`, `LargeObjectHeap`, `PinnedObjectHeap`, `Array`,
`String`, `Collectible`, `PayloadMayBeUninitialized`, `FrozenObjectHeap`). LXR uses it to set a
"valid object" bit, an extent and per-object flags.

**What we built (Code):**

- Interface 5.13: `GetAllocationNotificationParameters()` and the callback type
  (`gc/gcinterface.h:145-195`). The runtime accepts it only on x64 with R2R off
  (`vm/gcheaputilities.cpp:208-256`).
- New fast helpers `RhpNewFast_Notify`, `RhpNewArrayFast_Notify`, `RhpNewPtrArrayFast_Notify`,
  `RhNewString_Notify`. They publish the pending `alloc_ptr`, then call `RhpAllocationComplete`
  (`runtime/amd64/AllocFastNotification.S:31-140`, and the `.asm` twin). `InitJITAllocationHelpers`
  binds them once at startup (`vm/jitinterfacegen.cpp:57-68`).
- The slow path funnels through `PublishObjectAndNotify` (`vm/gchelpers.cpp:665`). The frozen heap
  and `RuntimeTypeHandle::InternalAllocNoChecks_FastPath` call the notification too
  (`vm/frozenobjectheap.cpp`, `vm/runtimehandles.cpp:1195`).
- On the GC side, `LxrGcHeap::AllocationComplete` finds the thread's mutator record, records the
  fast allocation, completes the birth metadata and stores a flag word
  (`gc/lxr_gc.cpp:732-775`).
- **Bump pointers:** when `LXRFastAllocation` is on (default `true`, `gc/lxr_gc.h:238`), the GC hands
  the thread an Immix span through `alloc_ptr` and `alloc_limit` (`gc/lxr_gc.cpp:636-650`).

**Measured, from P1.4, not re-run:** an empty notification callback adds 3.6 to 4.5 ns per small
allocation (1.5x to 1.8x). In the paired churn scenario the registered path keeps 0.858 (Workstation)
and 0.756 (Server) of unregistered throughput.

### 2.6 Roots and handles

- **Code:** LXR uses the stock root enumeration. It suspends the runtime with
  `GCToEEInterface::SuspendEE`, then calls `GCScan::GcScanRoots` and `GCScan::GcScanHandles` with its
  own callbacks (`gc/lxr_gc.cpp:990-1010,1174-1175`). Stack, static, thread-static, interior,
  pinned and handle roots come from the runtime.
- **Code (P5 README):** LXR uses the real handle table through `IGCHandleManager`. Weak, dependent
  and finalizer targets are kept alive by "leases" until a tracing pause decides their fate. Frozen
  objects stay runtime-owned. See [`P5/README.md`](../P5/README.md).
- **Code:** we added no `IGCToCLR` method for roots.

### 2.7 Object enumeration (P1.5)

- **Code:** `gc/gcref.h` adds an inlinable range iterator over real object fields (`GCReferenceRanges`)
  that uses the existing `CGCDesc`. Interface 5.14 adds one descriptor so the GC can resolve the
  LoaderAllocator slot of a collectible type without one `IGCToCLR` call per object
  (`gc/gcinterface.h:195-225`; runtime side `vm/gcheaputilities.cpp:278-327`).
- **Code (P1.5 document):** the gap before P1.5 was narrow. `go_through_object` already existed. The
  one real per-object runtime call was `GetLoaderAllocatorObjectForGC` for collectible objects.

### 2.8 Forwarding and other bits in the header (P1.6)

- **Code:** the 32-bit `m_alignpad` word before the sync-block word is renamed `m_GCReservedBits`
  (`vm/syncblk.h:736`). Interface 5.15 negotiates two bits in it, with the states
  `00 clear`, `01 invalid`, `10 transition`, `11 published` and atomic load, compare-exchange, store
  and wait operations (`gc/gcinterface.h:226-295`, `vm/gcheaputilities.cpp:369-470`). 64-bit only.
- **Code:** the old zero-only assertion `ObjHeader::IllegalAlignPad` is deleted (`vm/syncblk.cpp`).
- The negotiation does not copy objects and does not install a forwarding pointer. The pointer
  temporarily overwrites the first payload word (P6 nursery document).

### 2.9 Every runtime, JIT and VM change outside `src/coreclr/gc`

Grouped (**Code**, from `git diff --name-only df9c6988c20..lxr`):

| Group | Files | What the change does |
|---|---|---|
| Interface negotiation | `vm/gcheaputilities.{h,cpp}`, `vm/gcenv.ee.cpp`, `nativeaot/Runtime/gcheaputilities.{h,cpp}`, `nativeaot/Runtime/gcenv.ee.cpp`, `nativeaot/Runtime/clrgc.enabled.cpp` | Check the GC version tail by tail. Validate each request. Write `Accepted` or `Unsupported`. NativeAOT accepts only the card-table shape and refuses the rest. |
| Barrier code | `vm/writebarriermanager.{h,cpp}`, `vm/amd64/jithelpers_fastwritebarriers.S`, `vm/amd64/JitHelpers_FastWriteBarriers.asm`, `vm/amd64/slotlogwritebarrier.S`, `vm/amd64/SlotLogWriteBarrier.asm`, `vm/excep.cpp` | Two new barrier types and templates, the slow stub, and fault-address ranges so a null target becomes a `NullReferenceException`. |
| JIT contract | `inc/corjitflags.h`, `jit/jitee.h`, `inc/corinfo.h`, `inc/jithelpers.h`, `inc/jiteeversionguid.h`, `jit/{codegencommon,emit,lsrabuild,gcinfo,assertionprop,importercalls,fgbasic,namedintrinsiclist,gentree,utils}.cpp`, `jit/compiler.h`, `tools/Common/JitInterface/CorInfoHelpFunc.cs`, `CorInfoImpl.{RyuJit,ReadyToRun}.cs`, `tools/superpmi/...` | Two flags, two helper IDs, Standard ABI register sets, old-value semantics, the `RequiresOldValueWriteBarrier` intrinsic, a new JIT/EE GUID, SuperPMI support. |
| JIT struct lowering | `jit/lower.{h,cpp}`, `jit/layout.{h,cpp}` | Layout-aware bulk copy, clear and init for large GC structs. |
| Interpreter | `interpreter/compiler.cpp`, `interpreter/intrinsics.cpp`, `vm/interpexec.cpp` | Fold the intrinsic to a constant. Log async continuation copies exactly. |
| VM store funnels | `vm/object.cpp`, `vm/loaderallocator.cpp`, `vm/comutilnative.cpp`, `vm/arraynative.{cpp,inl}`, `vm/gchelpers.{h,cpp}`, `vm/reflectioninvocation.cpp`, `vm/common.h`, `vm/corelib.h`, `vm/ecalllist.h`, `vm/runtimehandles.cpp` | Pre-store callbacks for scalar, atomic, range and layout stores. Bounded native helpers for CoreLib. |
| CoreLib | `System/Buffer.CoreCLR.cs`, `Array.CoreCLR.cs`, `Object.CoreCLR.cs`, `CastHelpers.cs`, `RuntimeType.BoxCache.cs`, `Array.cs`, `Buffer.cs`, `Span.cs`, `SpanHelpers.T.cs`, `RuntimeHelpers.cs` | Old-value paths for clear, fill, copy, box, unbox, clone. |
| Allocation | `runtime/amd64/AllocFastNotification.{S,asm}`, `vm/jitinterfacegen.cpp`, `vm/gchelpers.cpp`, `vm/frozenobjectheap.cpp`, `vm/runtimehandles.cpp` | Notifying helper family and the slow-path funnel. |
| Header | `vm/syncblk.{h,cpp}`, `vm/object.h`, `nativeaot/Runtime/ObjectLayout.{h,cpp}` | `m_GCReservedBits` and atomic operations on it. |
| Build and tests | `CMakeLists.txt` files, `dlls/mscoree/*.src`, `vm/objectheaderbitstest.cpp` | Opt-in options, validation exports. |

## 3. What PR #3316 merged for the same items

### 3.1 What the PR is, and where it lives

- **Code and Quoted:** the PR targets `feature/gc/baseline` in dotnet/runtimelab. The README of that
  branch says it "starts from `dotnet/runtime` commit `f92f93994a587f69d6f7510d5390e68bd5290b2b`",
  "includes the full runtime source and runtimelab publishing setup, with no experimental GC
  changes", and that "GC implementations are developed in runtimelab". The NuGet label is `gcbase`.
- **Measured:** 90 files, +19,046 and -315 lines. 43 files (+17,832) are the new `src/coreclr/gc_satori`
  folder. 47 files (+1,214, -315) are the runtime side: `gc` 21 files, `vm`, `debug` and `nativeaot`
  25 files, and CMake.
- **Code:** no file under `src/coreclr/jit`, `src/coreclr/inc`, `src/coreclr/interpreter`,
  `src/coreclr/tools` or `src/libraries` changes. Allocation helpers do not change either.
- **Code:** a large part of `vm/threads.cpp` (-267 lines) is moved, not removed. The barrier copy code
  (`s_barrierCopy`, `GetWriteBarrierCodeLocation`, `IsIPInWriteBarrierCodeCopy`) moves into
  `vm/writebarriermanager.cpp`.
- **Code:** Satori is built as its own shared library `satorigc` for AMD64 and ARM64 only
  (`gc_satori/CMakeLists.txt`, `src/coreclr/CMakeLists.txt`). It reuses the handle-table sources of
  `src/coreclr/gc` with `-DFEATURE_SATORI_GC` (compile-time `#ifdef` blocks).

### 3.2 How a standalone GC is loaded

- **Code:** the loader is the stock one (`vm/gcheaputilities.cpp`, function `LoadAndInitializeGC`).
  The user names the library with `DOTNET_GCName` or `System.GC.Name`. The runtime calls
  `GC_VersionInfo`, rejects a lower major version (`:295`), then calls
  `GC_Initialize(IGCToCLR*, &heap, &manager, &dacVars)`.
- **Code:** `FinalizeLoad` is new at this point: it calls `gcHeap->GetGCHeapType()` into `g_heap_type`
  and `GetFastGCFunctions` into `s_fastGCFunctions` (`:118-146`). The condition
  `MajorVersion > ... || MinorVersion >= GC_INTERFACE_MINOR_VERSION` is always true for minor 0.
  **Inferred:** harmless, because the major-version check rejects older GCs first.
- **Code:** Satori's `GC_VersionInfo` names it "Satori GC" and refuses to load if the runtime EE
  interface is lower than 6 (`gc/gcload.cpp:64,92`). `g_gc_heap_type` is set to `GC_HEAP_CUSTOM` there
  (`:132`).
- **Code:** `ceemain.cpp` keeps a provisional `g_heap_type` (workstation or server) until the GC
  reports its type. It moves `InitThreadManagerTracingData()` after the JIT-helper initialization
  (`vm/ceemain.cpp:918`).
- **Code:** `IsWriteBarrierCopyEnabled()` returns true for custom GCs, because custom barriers need
  an executable copy (`vm/threads.h:5436`).

### 3.3 How the write barrier is selected and patched (`WriteBarrierManager`)

1. `InitJITWriteBarrierHelpers` calls `g_pGCHeap->GetAssignRefFunctions(...)` when the heap type is
   custom (**Code:** `vm/writebarriermanager.cpp:1442`). It receives: the address and size of the
   assign-ref and checked-assign-ref code, a list of fault addresses (`g_customWriteBarrierAVLocations`,
   `vm/excep.cpp`), and an array of `GCWriteBarrierPatchableValue { Slot, Source }`.
2. The two code blocks must form one **relocatable block**. Internal branches stay inside it. No
   instruction may use a PC-relative address of an outside global. Each outside value is read from a
   patchable 8-byte slot inside the code (**Quoted** from the interface comment,
   `gc/gcinterface.h:156-172`).
3. The runtime copies the block to a fresh executable allocation (`ExecutableAllocator`), preserving
   the offsets of both entries (same function, after the page-size checks).
4. `UpdateCustomWriteBarrierGlobals()` copies the current value of each source global into its slot
   (`:234-258`). It runs after the copy and on **every** `StompWriteBarrier` call
   (`vm/gcenv.ee.cpp:1039`). The slot must be 8-byte aligned so that the update is one atomic store.
5. `SetJitHelperFunction(CORINFO_HELP_ASSIGN_REF, ...)` and `(CORINFO_HELP_CHECKED_ASSIGN_REF, ...)`
   install the copies (`:1515-1516`). `JIT_WriteBarrier_Loc` also points to the copy.
6. The stock types of `WriteBarrierManager` get a new `WRITE_BARRIER_CUSTOM` value. Most
   `WriteBarrierManager` operations return early for it (`:435,478,658,700,779,929,972,1373`). The
   functions `StompWriteBarrierEphemeral`, `StompWriteBarrierResize`, `SwitchToWriteWatchBarrier`
   and `SwitchToNonWriteWatchBarrier` do nothing when `g_heap_type == GC_HEAP_CUSTOM`.
7. Perf map and ETW report the copied barriers by name (functions `ReportCopiedWriteBarriersTo*`).
8. A fault inside the barrier is a null-reference exception. `IsIPInMarkedJitHelper` checks the GC's
   fault list (`vm/excep.cpp`).

`WriteBarrierOp::StompCustom` with a mask lets the GC change single globals later. Satori uses it to
switch its barrier state: it puts `1`, `2` or `0` into the "write watch table" global and sets
`g_sw_ww_enabled_for_gc_heap` (**Code:** `gc_satori/SatoriRecycler.cpp:63-87`).

### 3.4 The barrier ABI that the PR keeps

- **Code:** helper entry points and registers are the stock ones. The JIT assumes that only integer
  scratch registers change (`targetamd64.h`). The PR changes no JIT code.
- **Code:** Satori's C++ slow path (`SatoriRegion::EscapeFn`) is reached by an indirect call from
  `RecordEscape`. Before it the assembly code aligns the stack, pushes `rsp`, `rdi`, `rsi`, `rdx`,
  `r10`, and saves **only `xmm0`** (`gc_satori/amd64/writebarrier.S:191-227`).
- **Quoted:** [jkotas](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4223830320):
  "This is invalid assumptions. Assumptions like this have come back to bite us many times over the
  years." [EgorBo](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4224938300): "now JIT
  relies on WB to never touch any SIMD reg." (that is dotnet/runtime PR 128778.)
  [VSadov](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4232549400): "It was validated
  (by copilot looking through binaries) that no vector registers are used on the barrier path"; and
  "the main motivation for this solution was to fit into existing barrier ABI - since I did not want
  to break R2R compat."
  [jkotas](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4232582036): "We will either
  need an option to disable this optimization when the GC write barriers are implemented in C++; or
  have the GC write barriers implemented in asm where we can guarantee no SIMD registers are used."
- **Quoted:** VSadov was "unsure what will be the way forward for the barriers ABI"
  ([r4232911140](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4232911140)). So the ABI
  is **not settled**.
- **Code:** thread tag. The Satori barrier reads a per-thread value to see who owns a region:
  `fs:[0]` on Linux x64, `gs:[0]` on macOS, `gs:[30h]` (the TEB) on Windows, `TPIDR_EL0` and
  friends on ARM64 (`gc_satori/amd64/writebarrier.S:62`, `writebarrier.asm:71`,
  `arm64/writebarrier.S:69-74`).

### 3.5 What Satori's barrier does

(**Code:** `gc_satori/amd64/writebarrier.S`.)

1. **Checked entry:** test the page map for the target address (`shr 30`). If the target is not in the
   Satori heap, store and return.
2. **Source check:** under `FEATURE_SATORI_EXTERNAL_OBJECTS`, a source outside the Satori heap
   (frozen or other runtime objects) is stored without tracking.
3. **Thread-local check:** the source region (2 MB aligned) stores the owner thread tag. If the
   owner is the current thread, and source and target are in the same region, and the target slot is
   not "exposed", store only.
4. **Escape:** otherwise call `SatoriRegion::EscapeFn` (through a function pointer stored in the
   region) to mark the source as escaping.
5. **Cards:** store, then read the patched barrier state. State `2` skips cards. Otherwise set or
   dirty a card (and group and page bytes) in a page-based card table. State `1` (concurrent marking)
   always dirties.

So Satori's barrier is generational card marking plus thread-local escape tracking. It uses the new
value only. Three patched 8-byte immediates carry the card bundle table, the card table and the
barrier state (`PatchableValue_*` labels).

### 3.6 What changed in `gcinterface.h` and `gcenv.ee`

Listed in section 1. In short: major 6, EE 6, `StompCustom`, `WriteBarrierParametersMask`,
`FastGCFunctions`, `GCWriteBarrierPatchableValue`, `GC_HEAP_CUSTOM`, `GC_ALLOC_IMMORTAL`, four new
`IGCHeap` methods, two new `IGCToCLR` methods, `sw_ww_enabled_for_gc_heap` in
`WriteBarrierParameters`. **Inferred from field sizes (not compiled):** the two new fields of
`WriteBarrierParameters` fit inside the 6 bytes of padding that end the old 80-byte prefix, so they
do not overlap our tail, which starts at byte 80.

### 3.7 What Satori's allocator does, and how it uses the interface

- **Code:** the runtime's stock helpers (`RhpNewFast`, array and string helpers) bump `alloc_ptr` in
  the thread's `gc_alloc_context`. When the buffer is full they call `IGCHeap::Alloc`
  (`SatoriGC::Alloc`, `gc_satori/SatoriGC.cpp:614`), which calls `SatoriAllocator::Alloc`
  (`SatoriAllocator.cpp:262`). Small objects: if `alloc_ptr + size <= alloc_limit` it takes the next
  bytes; otherwise `AllocRegular` extends `alloc_limit` inside the thread's region, zeroes the new
  chunk (zeroing is skipped for `GC_ALLOC_ZEROING_OPTIONAL`), and registers finalizable objects.
- **Code:** it uses the same `gc_alloc_context` fields as stock. `gc_reserved_1` and `gc_reserved_2`
  hold the regular and large regions (`SatoriAllocationContext.h`).
- **Code:** large (32 KB and more), pinned and immortal objects use separate paths. Large and pinned
  objects are marked "unfinished" in the header until `PublishObject` runs
  (`SatoriAllocator.cpp:895-900,1059-1064`; `SatoriObject.inl:260-275`: bit `0x80000000` of the
  sync-block word). Immortal objects come from their own region (`AllocImmortal`).
- **Quoted:** VSadov describes the object size classes: "ordinary" (bump pointer), "large" (32K+),
  "huge" (about 2M+) ([r4235635229](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4235635229)).
  He also says that a GC steers what reaches it by sizing the buffer
  ([comment](https://github.com/dotnet/runtimelab/pull/3316#issuecomment-6176005321)).
- **Code and Quoted:** the VM still uses the constant `LARGE_OBJECT_SIZE` (85000) and then
  `GetLOHThreshold()` (`vm/gchelpers.cpp:556,796,994,1144`). The header comment says:
  "TODO: Satori has LARGE_OBJECT_SIZE ((size_t)(32 * 1024))" (`gc/gcinterface.h:304`).

How Satori uses each new part:

| New part | Satori's use (Code) |
|---|---|
| `GetAssignRefFunctions` | Returns `RhpAssignRef`, `RhpCheckedAssignRef`, their end labels, three fault labels and five patchable slots (`SatoriGC.cpp:441-453`). |
| `FastGCFunctions.write_barrier` | `ErectWriteBarrier`: C++ card marking for VM stores such as `ErectWriteBarrier(dst, ref)` (`vm/gchelpers.cpp:1382`). |
| `is_in_gc_heap` | Replaces the `g_lowest_address` test in the conservative stack scan (`vm/gcenv.ee.cpp:178`), in the debug check `Thread::IsObjRefValid` (`vm/threads.h:3109`) and in the array copy helper (`vm/arraynative.inl:329`). |
| `check_escape` | Called before a store from five VM sites: `SetObjectReferenceUnchecked` (`vm/object.cpp:337`), `COMInterlocked::ExchangeObject` and `CompareExchangeObject` (`vm/comutilnative.cpp:1716,1735`), `LoaderAllocator::CompareExchangeValueInHandle` (`vm/loaderallocator.cpp:981`), `SyncBlockCache::NewSyncBlockSlot` (`vm/syncblk.cpp:740`). It takes `(dst, new_ref)`. |
| `bulk_move_with_write_barrier` | In `InlinedMemmoveGCRefsHelper`: if the destination is in the heap the GC does the copy and the escape or card work (`vm/arraynative.inl:303-346`). The interpreter's async-continuation copy uses it too (`vm/interpexec.cpp:4786`). |
| `GetGCHeapType` and `GC_HEAP_CUSTOM` | 34 lines in `vm/*` name it (**Measured:** `grep`). Examples: `IsServerHeap()` is true for custom; `MarkShouldCompeteForStatics()` is false; `ErectWriteBarrierForMT` is `UNREACHABLE()`; `SetMethodTableForUOHObject` stores plainly (`vm/object.h:159`). |
| `GetTotalAllocatedBytesPrecise` | The runtime skips its own suspend-and-sum and asks the GC (`vm/comutilnative.cpp:932,967`). |
| `GcScanCurrentStackRoots`, `GcPoll` | Thread-local generation 0 collection on the allocating thread without suspending the others (`SatoriRegion.cpp:1190,1466`, `SatoriRecycler.cpp:778,1897`). |
| `StompCustom` | Switches barrier state (concurrent or not) (`SatoriRecycler.cpp:63-87`). The `Initialize` call passes dummy bounds: `lowest_address = 1`, `ephemeral_low = -1` "to make asserts happy" (`SatoriHeap.cpp:37-59`). |

### 3.8 Where the PR authors say something is temporary or unfinished

| Where | Statement | Label |
|---|---|---|
| `gc/gcinterface.h:304` | "TODO: Satori has LARGE_OBJECT_SIZE ((size_t)(32 * 1024))". | Code |
| `vm/gcheaputilities.h` (`IsServerHeap`) | "TODO: Satori is closer to SVR than WKS ... Need a better API though." | Code |
| `vm/gcenv.ee.cpp` (`ScanStackRoots`) | `// TODO: generalize this` on the `thread_under_crawl` assertion. | Code |
| `vm/gcenv.ee.cpp` (`StompCustom`) | A block of commented-out code (`StartConcurrentMarkingSatori`, `StopConcurrentMarkingSatori`). | Code |
| `vm/writebarriermanager.cpp` | "TODO: is this method ever called for custom GC?"; "Satori TODO: is this necessary?". | Code |
| `vm/syncblk.cpp` | "TODO: I've derived this from EscapeCheckOnHandleCreation(), but it is not 100% match." | Code |
| `nativeaot/Runtime/gcenv.ee.cpp` | `GcScanCurrentStackRoots` and `GcPoll`: "TODO: Implement". NativeAOT has no Satori support. | Code |
| `vm/writebarriermanager.cpp` | `StompResize`, `StompEphemeral` and write-watch switch are skipped for custom GCs. | Code |
| `gc_satori/README.md` | Roadmap items still unchecked: explicit memory limits, perf tuning, diagnostics ("support for debuggers and profilers"), NUMA. | Code |
| `SatoriGC.cpp` | `DiagWalkHeap`, `DiagWalkObject`, `ValidateObjectMember`: "NYI" or empty. | Code |
| Issue 3318 | Seven open design questions (section 6). | Quoted |
| [janvorli](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4223851372) | "this is Satori as is taken from the feature/satori only rebased on recent main, there are more issues in it." | Quoted |
| [VSadov](https://github.com/dotnet/runtimelab/pull/3316#issuecomment-6085302091) | "I think it is ok to do integration with old copy ... We can catch up with the recent bits afterwards." | Quoted |
| [janvorli](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4233242804) | "I didn't want to have a split or change the default implementation for this PR. **LXR GC is going to be plugged in very soon.** We can decide later which way to go." | Quoted |

## 4. Side-by-side table

"Our files" are paths in this repository at `lxr`. "PR files" are paths in the PR tree.
Risk is the risk of moving to the PR mechanism: **L** low, **M** medium, **H** high.

| # | Mechanism | Our implementation (files) | Merged by PR #3316 (files) | Gap | What we must change or extend | Risk |
|---|---|---|---|---|---|---|
| 1 | Standalone GC loading | `vm/gcheaputilities.cpp` (negotiation per feature), `gc/lxr_gcload.cpp` | `vm/gcheaputilities.cpp` (`FinalizeLoad`), `gc/gcload.cpp` | Same loader. Different version scheme and the major-version check. | Renumber to the PR scheme (6.x). Rebuild our GC. | M |
| 2 | GC and EE interface numbers | GC 5.15, EE 4 (`gc/gcinterface.h:9-27`) | GC 6.0, EE 6 | Collision with each other and with dotnet/runtime 5.9/EE 5. | One agreed numbering. Our features become minors above the PR base. | H |
| 3 | `IGCHeap` vtable tail | +3 slots (negotiation) | +4 slots (fast functions, heap type, precise bytes, assign-ref) | Slot order clash. | Fix one order. Keep our slots after the PR's. | M |
| 4 | Heap type | `GC_HEAP_WKS` (`lxr_gcload.cpp:50`) | `GC_HEAP_CUSTOM` and 34 VM lines that name it | LXR is not "custom" for the VM. | Audit all `IsServerHeap()` and `g_heap_type` users before switching. | M |
| 5 | Scalar barrier install | New types in `WriteBarrierManager`: `SLOT_LOG64`, `WRITE_WATCH_SLOT_LOG64` (`vm/writebarriermanager.cpp`, `vm/amd64/*`) | `GetAssignRefFunctions` + copy + `SetJitHelperFunction` (`vm/writebarriermanager.cpp:1442-1520`) | Two install paths in one file. | LXR supplies its own assign-ref assembly (Windows and System V). Remove our template types after parity. | M |
| 6 | Barrier ABI | Standard ABI via JIT flag (`jit/codegencommon.cpp`, `emit.cpp`, `lsrabuild.cpp`) | Specialized (no JIT change) | Slow-path C++ safety. R2R and NativeAOT. | Decision D1 (section 7.1). | H |
| 7 | Barrier fast path | Leaf: unlog-bit test, patched immediates (`jithelpers_fastwritebarriers.S:797`) | Escape tracking and cards (`gc_satori/amd64/writebarrier.S`) | Different algorithm. The PR gives the container, not the content. | Write the LXR fast path as the custom block. Patch values with `GCWriteBarrierPatchableValue`. | M |
| 8 | Barrier slow path | Static `NESTED_ENTRY` stub with unwind data (`vm/amd64/slotlogwritebarrier.S`) | Inline stack switch in a `LEAF_ENTRY` without unwind data | PR slow path has an unwind gap (issue 3318, last item). | Keep our unwind-correct stub design, or fix the PR's. | M |
| 9 | Patched global values | Five patch fields per template, `WriteBarrierManager::ConfigureWriteBarrier` | `GCWriteBarrierPatchableValue`, `UpdateCustomWriteBarrierGlobals` | Better in the PR: works on ARM64, no far-address problem. | Adopt the PR method. | L |
| 10 | Checked and byref | Stock `JIT_CheckedWriteBarrier` uses VM bounds (`patchedcode.S:80`) | GC supplies its own checked entry (page map) | If LXR supplies assign-ref, it must also supply checked. | Write the checked entry. Keep the contiguous-reservation test or use a page map. | L |
| 11 | JIT drops null, frozen, proven stores | `jit/gcinfo.cpp:238`, `jit/assertionprop.cpp`, `jit/importercalls.cpp` flag `REQUIRES_OLD_VALUE` | Nothing. | PR cannot host LXR without it. | Keep. Upstream proposal. | H |
| 12 | Bulk copy | Range classifier and callbacks (`vm/gchelpers.cpp:1713+`), `memmoveGCRefsWithLayout`, JIT layout helpers | `FastGCFunctions.bulk_move_with_write_barrier` in `InlinedMemmoveGCRefsHelper` | PR passes no type and sees only the destination range. | Use the PR callback for pure reference ranges. Keep layout, clear and fill paths. | M |
| 13 | `Clear`, `Fill`, box, clone | CoreLib `*OldValueWriteBarrier` paths | Nothing (null stores need no Satori work). | Not covered. | Keep. Upstream proposal. | H |
| 14 | Pre-store hook at VM sites | `ErectWriteBarrierPre` and variants at about 20 sites (**Measured:** `grep`, definitions included) | `check_escape` at 5 sites; `write_barrier` post-store | Same sites, different hooks. | Make one hook `pre_store(ctx, dst, new)`. The GC can read `*dst` itself. | M |
| 15 | Collectible type edge | Dependent-edge callback (`vm/gchelpers.cpp:2113`) | `SetMethodTableForUOHObject` stores plainly | LXR needs the edge. | Keep our callback. | L |
| 16 | Allocation fast helpers | `AllocFastNotification.{S,asm}`, `vm/jitinterfacegen.cpp` | Unchanged stock helpers | Per-object callback cost (P1.4). | Decision D2 (section 7.1). | H |
| 17 | Allocation completion | `AllocationComplete` callback, `PublishObjectAndNotify` | `PublishObject` (stock; "unfinished" bit used inside Satori) | LXR needs per-object birth data or an alternative. | Issue 3318 discusses a "completion protocol". Align with it. | M |
| 18 | Large-object threshold | LXR large-object space from 32,768 bytes (`gc/lxr_gc.cpp` `Alloc`; P5 README) | VM constant 85,000 plus `GetLOHThreshold()` | Same as Satori's problem. | Follow issue 3318. | L |
| 19 | Frozen and immortal objects | VM frozen heap, GC enumerates their outgoing slots (P5) | Default frozen heap. `GC_ALLOC_IMMORTAL` unused by VM | Open design question. | Wait for the decision. | L |
| 20 | Roots and handles | Stock `GcScanRoots`, stock handle table | Same, plus `GcScanCurrentStackRoots`, `GcPoll`, `#ifdef FEATURE_SATORI_GC` in handle table | No gap for LXR. | None for LXR now. | L |
| 21 | Object enumeration | `gc/gcref.h`, descriptor 5.14 | None (Satori uses its own walker) | Only ours. | Keep. Upstream proposal. | L |
| 22 | Header bits | `m_GCReservedBits`, 5.15 negotiation (`vm/syncblk.{h,cpp}`) | Satori sets bit `0x80000000`; `IllegalAlignPad` assert skipped for custom | Conflict in `syncblk.cpp` and `.h`. | Keep ours. Merge the assertion changes. | M |
| 23 | Epoch reset | `RestartEE` calls reset (`vm/gcenv.ee.cpp:93`) | Not needed | Removable (**Inferred:** the GC itself calls `RestartEE`, `lxr_gc.cpp:1007`). | Move reset into the GC before `RestartEE`. | L |
| 24 | R2R and NativeAOT | Fail closed. R2R off. | R2R works (stock ABI). NativeAOT: TODO stubs. | Large for LXR. | Decision D1 decides. | H |
| 25 | Architectures | x64 only (Windows, Linux) | x64 and ARM64 (Windows, Linux, macOS) | We lack ARM64. | Later. | M |
| 26 | Diagnostics | Pause-end snapshots; no DAC layout (P5) | `GC_HEAP_CUSTOM` accepted by the DAC; heap walks are `NYI` | Similar limits. | Follow issue 3318 item "diagnostics". | L |
| 27 | Build | `CLR_CMAKE_ENABLE_LXR_GC`, target `clrlxrgc` in `gc/` | Target `satorigc` in `gc_satori/` | Conflict in `gc/CMakeLists.txt` and `src/coreclr/CMakeLists.txt`. | Keep both targets. | L |

## 5. What LXR needs that the PR does not give yet

Each item says what is missing and where our current code covers it.

1. **A store hook that sees the old value, also for null.** The PR hooks see only the new value
   (`check_escape(dst, ref)`), and the JIT removes barriers for null and frozen values. LXR logs the
   old value of every overwritten field. Today we cover it with the two JIT flags and the
   pre-store functions. **Need:** an upstream flag and a pre-store function that is allowed to read
   `*dst`.
2. **A reference-store barrier that tests an unlog bit in side metadata and logs the old target.**
   The PR gives the install method (`GetAssignRefFunctions`) and not the algorithm. LXR must write
   its own assembly. Satori's code is the template for the shape and relocation rules.
3. **A safe slow path.** Options in section 7.1. The PR offers no help for SIMD safety.
4. **Barriers for array copy, struct copy and `Buffer.MemoryCopy`.** The PR's
   `bulk_move_with_write_barrier` covers an all-reference range. It cannot see the type. LXR also
   needs exact layout for mixed structs, clear with null source, `Span.Fill`, boxing, nullable,
   `MemberwiseClone` and async continuations. We cover these (section 2.3).
5. **Interior pointers and byrefs.** Root reporting for them is stock. The LXR need is on the GC
   side: find the object that contains an interior address, also for young objects. We use a
   per-object "valid object" bit set at allocation. Satori keeps an index entry for every 2 KB of a
   region (`INDEX_GRANULARITY_BITS = 11`, `gc_satori/SatoriUtil.h:51`; filled by
   `SetIndicesForObject`) and has no per-object callback. **Inferred:** an
   index with a forward walk would remove the per-object callback, but it parses objects at root
   scan time (our operating rule 4 concerns sweeping, not root scan). Stores through byrefs use the
   checked helper, so LXR needs its own checked entry (table row 10).
6. **Allocation-complete publication.** The PR gives only `PublishObject`. See issue 3318 and
   decision D2.
7. **Header forwarding bits.** Not in the PR. Our negotiation stays.
8. **Per-thread log buffers and their flush at suspension.** No new interface is needed. We use
   `gc_alloc_context.gc_reserved_1` to find the thread's record and `FixAllocContext`,
   `GcEnumAllocContexts` for flush (**Code:** `gc/lxr_gc.cpp:705-725,990-991`). Satori uses the same
   reserved fields. **Inferred:** an assembly fast path that appends to the thread's chunk without
   a C++ call would need the thread's record from assembly. The allocation helpers already do this
   through `INLINE_GET_ALLOC_CONTEXT_BASE` (`AllocFastNotification.S:36`). The comment there says
   that the macro "trashes volatile registers". That is more than the Specialized barrier contract
   allows, so this must be checked before it is trusted.
9. **Root and handle enumeration shape.** Stop-the-world enumeration is enough today. The PR's
   `GcScanCurrentStackRoots` and `GcPoll` serve thread-local collection and are not needed by LXR.
   The `#ifdef FEATURE_SATORI_GC` edits of the handle table do not apply to the `clrlxrgc` build.
10. **Frozen objects and pinned-object-heap objects.** Both GCs treat frozen objects as external.
    LXR already enumerates their outgoing slots (P5). The open question "who allocates immortal
    objects" (issue 3318) can change this.

### 5.1 Which of our runtime changes the new mechanism could replace

| Our change | Replace with | Condition |
|---|---|---|
| `WRITE_BARRIER_SLOT_LOG64` and write-watch twin, five patch fields, `ConfigureWriteBarrier` | LXR-owned assign-ref and checked-assign-ref via `GetAssignRefFunctions` and patchable slots | Same or better fast-path cost. Decision D1. |
| Bound tests with `g_lowest_address` and `g_highest_address` in VM code | `FastGCFunctions.is_in_gc_heap` | None. |
| Epoch-reset callback in `RestartEE` | GC calls its own reset just before `GCToEEInterface::RestartEE` | **Inferred:** only the GC calls `RestartEE(true)`. Verify. |
| Range pre-barrier for plain reference ranges (`ErectWriteBarrierRangePre`) | `bulk_move_with_write_barrier` | GC must do old-value logging, then the copy. |
| Scalar pre-store calls at five VM sites | A generalized `check_escape` / pre-store hook | New signature or documented use of `*dst`. |
| `GC_HEAP_WKS` classification | `GC_HEAP_CUSTOM` | After the audit in table row 4. |
| Version and negotiation scheme | The PR's version scheme | Renumbering. |

### 5.2 Which of our changes need an upstream proposal

All are justified without LXR only if shown to be generic (rule R4):

- the JIT flags `REQUIRES_OLD_VALUE` and `USE_STANDARD_WRITE_BARRIER_ABI`, the intrinsic, and the two
  layout helper IDs (JIT/EE change; new JIT/EE GUID; SuperPMI);
- the CoreLib old-value paths;
- the allocation notification (if kept);
- the header-bit negotiation;
- the object-range enumeration (`gcref.h` plus the 5.14 descriptor).

**Quoted:** [jkotas](https://github.com/dotnet/runtimelab/pull/3316#issuecomment-6151422277): "It
goes back to the point that it is not desirable to try to build a complete GC abstraction." The
upstream owners prefer a small surface. Our additions outside the GC (+4,267 lines) are about 3.5
times the PR's non-Satori additions (+1,214). Expect pressure to split them.

## 6. Open points of issue #3318 and of the PR discussion that affect us

### 6.1 Issue [#3318](https://github.com/dotnet/runtimelab/issues/3318) (open, "Custom GC integration: follow-up design decisions")

| Item (Quoted from the issue body) | Effect on LXR |
|---|---|
| **GC helpers discovery:** "should `FastGCFunctions` and `GetAssignRefFunctions` become one interface?" | Our extension must fit one getter. [jkotas](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4253600417) asks for one method. [janvorli](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4258374096) says the two differ (pointers versus code plus sizes, fault lists, patch lists). [kkokosa](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4258284864) suggested a `GCRuntimeContract`-like struct that later also holds allocation helpers. Design our barrier and notification entries so they can move into such a struct. |
| **Allocation policy:** how the GC influences helper choice; separate runtime requirements from GC decisions (large objects); "Consider an allocation-formatting-completion protocol generalizing `PublishObject`." Define "completion guarantees for both direct VM allocations and GC-owned fast helpers." | Our `AllocationComplete` is a version of the completion step. See D2. |
| **Handle-table mechanics reuse:** which components custom collectors reuse. | LXR uses the stock table. Our RC handle needs may add requirements. |
| **Diagnostics and validation:** preserve profiler/event behavior; "May want to add new GC collector specific events." | Our P1.4 ordering rules (callback before profiler event) are a data point. |
| **Ownership of immortal object allocation.** | Affects frozen-heap handling in `lxr_gc.cpp`. Still open. |
| **Repurposed GC globals:** `write_watch_table` and `ephemeral_low` become misleading. | We would want proper names for LXR's use. See [r4235339750](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4235339750); [kkokosa](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4257676905) votes for `custom_...` names; [janvorli](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4247610485) notes that debugging code reads them. |
| **Write barrier unwind on the slow path:** "`RhpAssignRef` uses `LEAF_ENTRY` despite changing `rsp` and calling another function on the slow path without unwind annotations." (raised by [kkokosa](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4257644738)) | If LXR writes assign-ref assembly, it needs a correct unwind design. Our stub has one. |

### 6.2 PR discussion

| Thread | Effect on LXR |
|---|---|
| C++ code and SIMD registers in the barrier ([r4223830320](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4223830320) and replies) | This is the core of decision D1. Proposals in the thread: a post-build check for SIMD use, or an assembly version produced by a checked-in script ([jkotas](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4232798826); [kkokosa](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4257663443) agrees). |
| R2R compatibility as the reason for the Specialized ABI ([VSadov](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4232549400)) | Our Standard ABI needs R2R off. The reviewers will see this as a cost. |
| `LARGE_OBJECT_SIZE` and the VM's "large" test ([r4235635229](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4235635229), [r4247577028](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4247577028), [r4250496843](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4250496843), [r4261520195](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4261520195)) | LXR's own large-object threshold needs the same fix. |
| "VM call sites for the helpers are not very interesting ... we can just get rid of all of them and replace them by managed code" ([jkotas](https://github.com/dotnet/runtimelab/pull/3316#issuecomment-6151422277)) | Our ~20 VM-site hooks may disappear in future. Prefer investing in JIT-called and managed paths. |
| "I think the mechanism where GC provides a buffer for pointer-bump allocation and JIT consumes it is hard to beat in practice and every GC should use it for small objects." ([VSadov](https://github.com/dotnet/runtimelab/pull/3316#issuecomment-6176005321)) | Direct challenge to a per-object callback. Our P1.4 data supports the concern. |
| Handle table: shared stock components; "registered arrays" prototype ([jkotas](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4253369290); [kkokosa](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4257937125)) | Our RC handles could follow the same direction. |
| Satori snapshot is old ([hez2010](https://github.com/dotnet/runtimelab/pull/3316#issuecomment-6084283350)) | Satori in the PR may change. Treat it as a comparison baseline only. |
| `DhContext` scan-context assignment in `objecthandle.cpp` ([r4239684482](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4239684482)) | Only in the `FEATURE_SATORI_GC` build. No effect on us. |
| Satori "gets patched constants in the barriers. ... It has been a long-standing TODO." ([VSadov](https://github.com/dotnet/runtimelab/pull/3316#discussion_r4233154237)) | The patchable-slot method is welcome. A good basis for us. |

## 7. Recommended plan

### 7.1 Two decisions to make first

**D1. The barrier ABI.** Two ways to host LXR's barrier:

- **A. PR mechanism plus an LXR-written Specialized-ABI barrier.** R2R works. No JIT contract flag.
  The JIT still needs the old-value flag. The slow path must be assembly-only for the common case
  (append `(slot, old)` to the thread's chunk) and may call C++ only after it saves all
  vector state. **Inferred design:** an assembly fast path that appends to a per-thread buffer is
  what the Java LXR does. The C++ call then happens only when the chunk is full. This keeps
  SIMD safe. It adds work: assembly for Windows and System V, and a thread-record lookup.
- **B. Keep our Standard ABI.** Less new assembly. The C++ slow path stays easy. Costs: the JIT
  contract flag, larger code at each store site, "R2R off", NativeAOT unsupported.

Measure both on the same machine before choosing (section 7.3).

**D2. Allocation.** Two ways:

- **Keep the notifying helpers** and make them cheaper (P16 in the coordinator notes already
  proposes removing per-object tables).
- **Move to the PR model:** stock bump-pointer helpers and **no** per-object callback. Then the GC
  finds objects by an index, or by a bit set in bulk at refill time. This matches Satori and the
  VSadov statement in section 6.2, and it matches the issue 3318 direction.

### 7.1a Decisions taken (coordinator, 2026-10-20)

- **Barrier ABI (P18-D1): the Specialized ABI of the PR.** We use `GetAssignRefFunctions` and write the LXR barrier in assembly. The common path (test the unlog bit, append `(slot, old value)` to the thread's chunk) is assembly only. C++ is called only when the chunk is full, and only after all vector state is saved. R2R keeps working. The JIT flag for "old value needed, also for null stores" stays; it is an upstream proposal (section 5.2). The Standard ABI work is not extended.
- **Allocation (P18-D2): move to the stock bump-pointer helpers**, as Satori does. See "How the GC learns about objects" below.
- **Dependency: carry PR #3316 as a patch series on our dotnet/runtime-based fork.** We do not move the fork onto runtimelab.

#### How the GC learns about objects when the helpers do not call it

The stock fast helper only moves a pointer inside a buffer that the GC gave to the thread. It never calls the GC for an ordinary object. The GC learns about allocation at these points:

1. **Buffer refill (the slow-path callback).** When the buffer is used up, the helper calls `GCHeap::Alloc`. The GC then hands out the next buffer. This is where LXR does its bulk work for the whole buffer: zero the memory, clear the RC range, the field-unlog range, the straddle range and the valid-object range (this is what `InitializeFieldLogRange` does today for a hole or block), mark the range as marked if a concurrent trace is running, and record the buffer in a list of spans allocated since the last pause. This is per buffer, not per object.
2. **Slow-path allocations.** Objects with a finalizer, collectible types, pinned-object-heap objects and large objects do not use the fast helper. They already call the GC for each object. So the registry list (finalizers), the pinned-object blocks and the large-object space keep their per-object hook. These are rare.
3. **Pauses.** The GC sees each thread's buffer (`FixAllocContext`, `GcEnumAllocContexts`) and can cut off the unused part.

What we lose is the per-object "valid object" bit that today is set at allocation. Two things replace it:

- **Survivors.** A young object that survives is found by a root or a logged slot, and the increment code reads its header and size directly. No start record is needed.
- **Interior pointers into young objects.** Find the span that contains the address. Then walk forward from the span start (or from a start hint) by `GetSize()` until the walk reaches the address. This parses objects, but only for interior roots, and at most one buffer's worth of objects (about 200 objects for an 8 KB buffer). This is what the stock GC does with its brick table. It is safe because a thread cannot be suspended inside the fast helper, so every object in the used part of a buffer is complete. Satori does the same kind of lookup with an index entry for every 2 KB. Promoted objects (RC above 0) are found through the RC table, as before.
- Heap walks for profilers parse the spans in the same way.

Rule 4 of the roadmap concerns sweeping. This parse happens at root scan, not in a sweep. The risks are in the ordered steps below: the buffer size, the cost of interior-root lookups on a program that uses many byrefs, and the object-mark rule during concurrent marking for objects that are allocated without a per-object call.

### 7.2 Ordered steps

1. **Record D1, D2 and the dependency choice** (section 7.4) with the coordinator.
2. **Rebase our fork first onto current dotnet/runtime main** with no PR code. Fix the interface
   numbering clash (our 5.9 to 5.15 versus upstream 5.9, EE 5). Build the default runtime and check
   that stock behavior and stock helper bytes are unchanged (rule R2).
3. **Import the PR runtime side** (the 47 non-Satori files, from the two PR commits) as a separate
   patch series. Resolve the 25 overlapping files. Add the Satori build as an **optional** comparison
   target. Check that the default GC is unchanged.
4. **Renumber.** Adopt GC 6.x and EE 6. Express our features as minors above the PR base. Fix the
   `IGCHeap` slot order. Add `static_assert` checks for the `WriteBarrierParameters` layout (the
   Inferred non-overlap in section 3.6).
5. **Hook unification.** Replace our scalar pre-store call sites with one pre-store function in the
   PR's callback structure. Keep behavior the same. Compare allocation-free store throughput and
   the P1.2 numbers.
6. **Barrier move (only after D1 = A).** Write LXR's assign-ref and checked-assign-ref blocks with
   patchable slots and unwind-correct slow paths. Run the P1.1 to P1.3 validation. Remove the
   slot-log templates only when parity is shown.
7. **Bulk and layout paths.** Use `bulk_move_with_write_barrier` for plain reference ranges. Keep the
   layout, clear and fill paths. Prepare upstream proposals (section 5.2) one concern at a time.
8. **Allocation** per D2.
9. **Epoch reset** into the GC. **Heap type** switch after the audit.
10. **Optional:** ARM64 barrier and R2R-on validation.

### 7.3 What to measure

Same machine, same binaries rules as roadmap rule 6; interleave arms.

| Step | Measurement |
|---|---|
| 2 and 3 | Byte hash of stock barrier and allocation helper code before and after (P1.1 recorded 1,300 bytes for the stock barrier; the value will change after a rebase, so re-record). Default-GC test pass counts. |
| 5 | Scalar, array and checked store micro-benchmarks. Baseline values from P1.2 are in section 2.2. Treat a change below the 8.35% host floor as unresolved. |
| 6 | Same micro-benchmarks, plus the code size of store-heavy methods (P1.1 sizes in section 2.2), plus slow-path cost with a full chunk. Run with R2R **on** for the Specialized variant and **off** for the Standard variant. Report the difference in startup and steady throughput. |
| 7 | `Array.Copy`, `Array.Clear`, `Span.Fill`, 64-reference and 48 KB struct copy (P1.3 table). |
| 8 | Allocation micro-benchmarks and the paired churn scenario (P1.4). Compare with Satori's allocator on the same harness. |
| All | Pause and throughput against Workstation GC, Server GC and Satori on the P0.4 harness. |

### 7.4 What "the PR targets `feature/gc/baseline`, our fork is based on dotnet/runtime" means

- **Code and Quoted:** the PR branch is an experiment branch of dotnet/runtimelab. Its README says it
  starts from a dotnet/runtime commit and adds runtimelab publishing and GC-validation pipelines
  (most of its README text). It has the label `gcbase`.
- **Code:** the PR changes the GC interface to **6.0**. dotnet/runtime main is **5.9** now. So the
  PR's runtime changes are **not** in dotnet/runtime, and nothing in the PR says they will be
  upstreamed.
- **Measured:** the runtimelab baseline contains dotnet/runtime commit `5b82fc4ae86` (2026-09-23). Our
  base is `df9c6988c20` (2026-08-06). That is about seven weeks of upstream change between the two.
  dotnet/runtime main itself (2026-10-20) has GC 5.9 and EE 5. The 5.9 that main added is not our 5.9.
- **Consequence 1:** we need the PR changes as a **patch series** on our fork. We cannot merge from
  runtimelab without also taking its pipeline files and branch history.
- **Consequence 2:** upstream proposals for LXR must start from dotnet/runtime, not from runtimelab.
  The Satori work and our work would then have to agree on interface numbers there (one GC interface
  6.0 in runtimelab versus 5.9 in dotnet/runtime).
- **Consequence 3:** if we move our fork onto the runtimelab branch instead, we get Satori and the PR
  for free, but our work then depends on an experiment branch and its churn.
- **Inferred recommendation:** keep the dotnet/runtime base and carry the PR as a patch series
  (step 3).

### 7.5 Risks of the rebase

| Risk | Detail | Mitigation |
|---|---|---|
| Interface number clash | Three-way: ours 5.9 to 5.15, upstream 5.9, PR 6.0. | Step 4. |
| Hard conflicts | 25 files changed by both, including `vm/writebarriermanager.cpp` (the PR also moves code into it), `vm/gcenv.ee.cpp`, `vm/gcheaputilities.cpp`, `vm/gchelpers.cpp`, `vm/syncblk.cpp`, `gc/gcinterface.h`. | Do step 2 and step 3 as separate, reviewed commits. |
| Behavior drift on stock GC | PR adds `g_heap_type != GC_HEAP_CUSTOM` branches in the VM. Our rules say "no branch when unused". | Measure; prefer init-time selection. |
| Satori and LXR as two custom GCs | Both use the same hooks with different meaning (`check_escape` for new value, our pre-store for old value). | One hook, documented semantics. |
| Specialized ABI needs a safe slow path | A compiler may use SIMD later (jkotas). | D1 and a post-build check of the barrier object code. |
| The PR is itself unfinished | Section 3.8. | Pin the PR commit. Do not depend on its TODO areas. |
| Evidence invalidation | Our P1.x evidence (hashes, benchmarks) was recorded on the old base. | Re-run the key P1.x runs after each rebase step. |
| JIT/EE GUID | Our JIT interface changes force a new GUID, and `jiteeversionguid.h` conflicts on each rebase. | Regenerate at the end. |

## 8. Not checked

- **I did not build or run anything**: neither the PR tree nor our tree. All numbers from P1.x are
  copied from our documents and not re-run. I did not check whether they still hold at `e8d2d23ef60`.
- I read the PR's non-Satori diff in full. I read Satori only at the places named here
  (`SatoriGC.cpp`, parts of `SatoriAllocator.cpp`, `SatoriRecycler.cpp` `ToggleWriteBarrier`,
  `SatoriHeap.cpp` write-barrier init, `SatoriObject.inl`, the amd64 barrier in full, ARM64 only by
  grep). I did not read the Satori recycler, region logic or the ARM64 barrier in detail.
- I did not read the NativeAOT parts of the PR beyond the two `TODO` stubs.
- I did not read the Satori upstream repository (VSadov/Satori). hez2010's claim about newer
  commits is not verified.
- I did not read our P0.x, P2.x, P3.x, P7 to P9 documents in detail, nor the roadmap beyond its
  operating rules and the P1 steps. Statements about those parts rely on the README and the P5 and
  P6 documents only.
- I did not audit every user of `IsServerHeap()` and `g_heap_type`. I counted 34 lines in the PR's
  `vm` folder that name `GC_HEAP_CUSTOM`.
- I did not check what `GetLOHThreshold()` returns in our `lxr_gc.cpp`.
- **Inferred and unverified:** the vtable slot clash, the non-overlap of the `WriteBarrierParameters`
  fields, "only the GC calls `RestartEE(true)`", and the assembly fast-path idea in D1. None was
  compiled or run.
- The PR's CI status, and the runtimelab branch's CI, were not checked.
- I did not check how SOS or DAC tools behave with `GC_HEAP_CUSTOM`. Only the assertion change in
  `debug/daccess/request.cpp` was read.
- macOS and Windows ARM64 barrier behavior was not read.
- I deleted the temporary clone of dotnet/runtimelab. Downloaded PR and issue JSON files stayed in
  the session folder outside the repository.
