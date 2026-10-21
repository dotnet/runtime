# Full collection: the Java LXR and our .NET port (technical record)

This file is the technical record with source tables. For an explanation from
the basics, with diagrams, read the [HTML report](index.html) first. It uses
plain words. In this record, **H** is the Java LXR, **N** is our accepted .NET
port, and the older paper-era Java LXR is **P**.

## Verdict

We knew several differences, but did **not** already have the complete
execution-path comparison needed to choose the next optimization. The
accepted port implements LXR mechanisms without reproducing the Java LXR's
Full execution structure.

The comparison below follows the request through preparation, roots, RC,
transitive tracing, weak-pointer processing, reclamation and resumption. It
finds concrete additional work beyond the previously identified metadata
wrappers and serial trace drain:

- locked read-modify-write operations to read each traced field;
- an unconditional global atomic counter beside that read;
- target-based work with late duplicate rejection, versus slot-based work
  and early marked-target filtering;
- 1024-slot resumable array slices on Full, versus the Java LXR's 8192;
- repeated initialization and decommit of dense trace-support planes;
- whole-object lifecycle/flag passes, including over ordinary objects;
- repeated root/reader repair and a separate heap-statistics walk.

These are **source-proven execution differences**, not a measurement
assigning the complete slowdown to them. No new collector build, workload,
profile or timing observation was made for this reconstruction. No
optimization is implemented by this report.

## Exact comparison basis

| Name | Binding | Core / runtime | Role |
|---|---|---|---|
| H | `mmtk-openjdk` `0682434ae725787e38defbc17bd66dc918e4bdb7` | `mmtk-core` `304ce69d43aae87de501111fceb8cbd33173a03a`; OpenJDK `7caf8f7d19b19a9fd53be6f909db805246790807` | Primary: the Java LXR that works and that we ran |
| P | `mmtk-openjdk` `abbdd1dbbe7277e4b0dfd1d37ba613c53f5ca50d` | `mmtk-core` `df8d30a39237a5bf5a8e27ca5a6f46acc6080c94` | Paper-contemporaneous differences, not silently substituted for H |
| N | CoreCLR port | `e8d2d23ef60e44ba7efd35a7cb3f4a76271adc6a` | Accepted .NET baseline, including Full target batching |
| E | Rejected checked-pair experiment | `a23d3598b57837f3e858622f83b8ffbc13e0de58` | Explains what the latest experiment did and did not address; **not** the accepted implementation |

In source citations below, **H** means the exact H core above, **B** its
binding, **J** its pinned OpenJDK, and **N** the accepted port. A path and
line range are relative to the indicated source tree, not a current
upstream branch. N citations never name the rejected experiment's working tree.

H's binding sets Release LTO and one code-generation unit. Its binding
default feature list is empty, but that does not disable core defaults:
H core also declares dependencies, `ix_always_defrag` and release-log
filtering as defaults (`Cargo.toml:104-118`). Instrumentation, sanity and
extreme assertions are not enabled by these defaults. The generic
`ix_always_defrag` feature must not be confused with LXR's separate mature
evacuation selection.

H's normal constants select RC2, 1024 RC ingress, nursery and mature
evacuation, lazy decrements outside Full, and prefetching
(`src/util/rc.rs:12-28`, `src/args.rs:88-150`). The port supports RC2/4/8
but defaults to RC8. Its nonmoving/nursery/mature benchmark modes are
separate configurations, not three copies of H's default configuration.

The Java LXR working checkouts are not at the core oracle revisions.
The comparison therefore uses `git archive` of the pinned objects into
owned, ignored `artifacts/P13-full-reference-sources` snapshots. The
Java LXR checkouts, previous experiments and accepted branch were not
modified. Archive bindings are in
[`source-bindings.json`](source-bindings.json).

## 1. The complete control path

### Java LXR (H)

| Step | Reachable path and work | Source anchor |
|---|---|---|
| Application request | `JVM_GC` honors `DisableExplicitGC`, then calls the installed heap with `_java_lang_system_gc` | J `src/hotspot/share/prims/jvm.cpp:457-462` |
| VM binding request | `MMTkHeap::collect` calls `handle_user_collection_request`; ordinary `System.gc()` uses `force=false`, other causes may force it | B `openjdk/mmtkHeap.cpp:340-363`; `mmtk/src/api.rs:267-270` |
| Trigger and caller blocking | Memory-manager API enters `MMTK::handle_user_collection_request`; GCTrigger checks collection/ignore settings, records user-triggered state and requests work; a non-null mutator blocks for GC | H `src/memory_manager.rs:686-691`; `src/mmtk.rs:436-456`; `src/util/heap/gc_trigger.rs:143-169` |
| Collection kind | User/emergency request selects `Full` when no concurrent mark is active; with an active concurrent mark it selects `FinalMark` | H `src/plan/lxr/global.rs:610-662` |
| Schedule Full | Disable lazy decrements for this collection, arrange previous-root decrements, enqueue StopMutators, global Prepare/Release and weak-pointer processing work | H `src/plan/lxr/global.rs:697-718,769-815` |
| Stop and flush | Binding companion thread establishes the JVM safepoint; mutator visitor flushes/prepares buffers; stack-scan jobs are divided across workers | H `src/scheduler/gc_work.rs:238-293`; B `openjdk/mmtkUpcalls.cpp:173-195` |
| Early preparation | Mark metadata clearing and allocated-chunk/block preparation are scheduled work, not a per-object generic API loop | H `src/plan/lxr/global.rs:213-218,559-568`; `src/policy/immix/immixspace.rs:532-581,779-799` |
| Roots and increments | `RCImmixCollectRootEdges` invokes root-kind ProcessIncs; root slots are retained for the Full closure; mature/nursery increments perform their own RC work | H `src/plan/lxr/rc.rs:527-661,916-966` |
| Global preparation | Prepare LOS/Immix, release LOS nursery as appropriate, select mature evacuation and prepare/reset copy resources | H `src/plan/lxr/global.rs:248-269`; `src/policy/immix/immixspace.rs:555-600` |
| Transitive closure | `LXRStopTheWorldProcessEdges<VM,true>` processes slots, marks/copies targets, visits fields and publishes 8192-edge Full work to local/global worker queues | H `src/plan/lxr/rc.rs:611-631`; `src/plan/lxr/cm.rs:399-700` |
| Worker execution | Workers consume local packets, global open buckets and stolen work; newly flushed Full packets are ordinary GC worker work | H `src/scheduler/worker.rs:175-240,251-319` |
| Weak/final processing | Weak/final/phantom stages invoke binding-discovered lists of weak pointers and can publish more closure packets; no list means no packet for that list | H `src/scheduler/scheduler.rs:311-350`; B `mmtk/src/reference_glue.rs:181-287` |
| Release and reclamation | Plan release performs VM/class cleanup, nursery release, drains postponed decrements eagerly for Full and schedules mature cycle/LOS sweep jobs | H `src/plan/lxr/global.rs:272-297`; `src/policy/immix/immixspace.rs:602-638` |
| Decrement/cycle sweep | RC descendant processing and chunk-batched cycle sweeps run through workers; allocated blocks are checked using direct RC/mark metadata | H `src/plan/lxr/rc.rs:692-913`; `src/policy/immix/rc_work.rs:181-292` |
| Finish and resume | Last-worker completion closes buckets, runs final hooks, updates page/statistical state, ends the pause, resets trigger state and resumes the JVM | H `src/scheduler/scheduler.rs:682-697,824-912`; B `openjdk/mmtkUpcalls.cpp:248-275` |

The `nogc_no_zeroing` LXR request override is **not** the normal H entry.
Modern H's actual request logic is in MMTK/GCTrigger, not an inferred
BasePlan override. Features, comments and compiled reachability matter.

### Accepted port N

| Step | Reachable path and work | Source anchor |
|---|---|---|
| Application request | `GCInterface_Collect` QCall enters cooperative mode and invokes GC-EE `GarbageCollect`, which enters the collection lock/policy path; explicit collection uses the user-request branch | N `src/coreclr/vm/comutilnative.cpp:958-973`; `src/coreclr/gc/lxr_gc.cpp:914-922,967-981` |
| Kind and dispatch | Policy chooses Full or completion of an active trace; CollectLocked calls `CollectFull`/`FinishTrace` | N `src/coreclr/gc/lxr_collection_policy.h:166-178`; `src/coreclr/gc/lxr_gc.cpp:847-904`; `src/coreclr/gc/lxr_reference_counting.cpp:1943-1961` |
| Prior epoch | Drain prior work, retire/delete completed Work, configure stages and interrupt concurrent work | N `src/coreclr/gc/lxr_reference_counting.cpp:1963-2051`; `src/coreclr/gc/lxr_pause_coordinator.cpp:58-148` |
| Suspend | SuspendEE, retire allocation contexts, snapshot policy, flush mutators, commit trace-support planes, clear marks and initialize collection state | N `src/coreclr/gc/lxr_gc.cpp:984-1000`; `src/coreclr/gc/lxr_reference_counting.cpp:1825-1892` |
| Capture roots | One registered EE scan executor runs object-flag/finalizer leasing, roots, handles, weak/dependent/sync-block leases and frozen roots | N `src/coreclr/gc/lxr_gc.cpp:1109-1223,1335-1431` |
| Seed work | Convert logs and current/previous root banks into Work packets; schedule PrepareTrace, WeakClosure, EpochClosure and release | N `src/coreclr/gc/lxr_reference_counting.cpp:1711-1822` |
| Parallel RC work | Scheduler workers process root/log increments, copies/promotions and descendant RC work; Full increment work also accumulates object targets | N `src/coreclr/gc/lxr_reference_counting.cpp:2911-3040,3601-3623,3744-3807` |
| Prepare/repair | A PrepareTrace work item repairs readers, invokes EE root/handle relocation, handles LOS/pins, then prepares tracing and repairs readers/roots again | N `src/coreclr/gc/lxr_reference_counting.cpp:3628-3681` |
| Full transitive closure | QueueTrace leaves Full target/descriptor work on `m_trace_todo`; DrainTraceInsidePause processes it directly until pending work is empty | N `src/coreclr/gc/lxr_reference_counting.cpp:4728-4759,4804-4859,5281-5337` |
| Object/field visit | Resolve/copy/check target, acquire per-object trace lock, read checked RC+mark, store mark, enumerate GCDesc ranges and atomically read each field | N `src/coreclr/gc/lxr_reference_counting.cpp:5014-5114,5116-5226` |
| Weak/final closure | EE scan executor runs dependent fixed points, short weak cleanup, another object walk for finalizers, optional pending-finalizer walk, another dependent fixed point and long weak cleanup | N `src/coreclr/gc/lxr_gc.cpp:1194-1223`; `src/coreclr/gc/lxr_reference_counting.cpp:3685-3707` |
| Release | Retire forwarded sources, reclaim nursery and sweep blocks/LOS; root/log banks are rotated | N `src/coreclr/gc/lxr_reference_counting.cpp:3434-3595` |
| Eager decrements and sweep | Process previous roots/log decrements; completion guard schedules cycle sweep and block/LOS release | N `src/coreclr/gc/lxr_reference_counting.cpp:3122-3359,3809-3838,5369-5454` |
| Final EE work | Policy release, promotions/root-scan hooks and a separate occupied-object heap-statistics walk | N `src/coreclr/gc/lxr_gc.cpp:1241-1268`; `src/coreclr/gc/lxr_reference_counting.cpp:4581-4646` |
| Resume and return | Overall completion sets trace inactive; coordinator resumes EE; CollectLocked/Drain deletes Work and decommits trace-support planes before request return | N `src/coreclr/gc/lxr_reference_counting.cpp:3828-3846,2048-2069`; `src/coreclr/gc/lxr_pause_coordinator.cpp:150-220`; `src/coreclr/gc/lxr_gc.cpp:1002-1028,889-900` |

**Not all Full work is single-threaded.** RC/log buckets use workers.
The transitive Full target drain is not distributed through that worker
queue: it runs directly in the thread executing the relevant
PrepareTrace/closure operation, often a scheduler worker. Calling this
"caller-driven" must not imply that it always runs on the managed thread
which called `GC.Collect`.

The JVM binding also has safepoint/companion-thread handshakes. Their
mere existence is not a port defect. The difference is which work is
distributed after suspension, and which runtime operations require
registered GC threads.

## 2. One already-live ordinary object and one edge

| Operation | H Full path | Accepted N Full path |
|---|---|---|
| Read an object field | `OpenJDKSlot::load` uses x64 `read_unaligned` and optional compressed-oop decoding | `Interlocked::CompareExchangePointer(slot,nullptr,nullptr)` in the shared scanner |
| Count visited field | No matching unconditional per-field global atomic counter in this Full edge loop; detailed `stat` calls are instrumentation-gated | Unconditional `m_trace_metrics[TracedReferences].fetch_add(1)` |
| Decide whether to enqueue child | Field enumeration skips already-marked non-defrag targets before buffering slots | Every non-null child is appended as an object target; duplicate rejection occurs later |
| Target processing | Full Immix path selects copy or mark; ordinary mature marking uses an atomic mark fetch-update and scans the winning object | Forwarding/ownership/alignment checks, optional copy, per-object byte CAS lock, checked raw RC load, checked mark load and extent check precede scanning |
| Mark metadata | Fixed mark spec; H uses relaxed mark fetch-update | Generic checked word load/store APIs; accepted N fresh marking has six explicit full fences plus word CAS |
| Work representation | Slots/array slices, with write-back only when target moved | Object targets plus separate source/reader/root repair; Full mature scan also writes the slot after its copy check even if unchanged |
| Scan ordinary layout | Rust binding directly reads JVM oop maps; generics/LTO specialize visitors | GC-side C++ GCDesc range iterator; not a VM callback for each field |
| Class-loader edge | JVM CLD policies integrated into the binding visitor | GetLoaderAllocator checks Collectible first; its VM callback is reached only for collectible types |

Anchors: B `mmtk/src/slots.rs:96-115,234-266`;
H `src/plan/lxr/cm.rs:450-513,522-560,619-700`,
`src/policy/immix/immixspace.rs:1072-1110,1189-1225`;
N `src/coreclr/gc/lxr_reference_counting.cpp:3110-3119,5014-5114,5116-5226`;
N `src/coreclr/gc/gcref.h:22-112,114-230`.

H can load a slot more than once: enumeration/filtering, later slot
processing and prefetch can each load it. This is not a claim that the
the Java LXR executes just one ordinary load per edge or has no atomic
metadata operations.

The accepted Linux module
`0909a5b1ce5e6383e944d45ffd17791050acfb62a26b048c4955fb0917b71e14`
was inspected without rebuilding. `ScanTraceLocked` contains a locked
`cmpxchgq` at `0x2c167/0x2c168` for the slot read, followed by a locked
counter increment at `0x2c177/0x2c178`. These are emitted operations, not
an inference from a C++ method name.

E coalesced only two interior read-prefix fences in supported builds.
It retained the field CAS read, global counter, duplicate work, trace
lock, generic access architecture and phase structure. Its measured gains
in some modes are not evidence that those other differences are insignificant.

## 3. Exact gap register

Classification:

- **G**: verified structural difference without an intrinsic requirement
  that LXR use this implementation.
- **A**: runtime adaptation/obligation is real; its current cost is not
  shown to be minimal.
- **P**: policy/default/configuration difference, not an automatic fix.
- **U**: contribution or necessity remains unmeasured/unproven.

| ID | Difference and reachability | Classification and required treatment |
|---|---|---|
| F01 | Full closure is directly drained by one operation executor; H publishes Full slot work to GC workers/local queues/stealing | G. Reproduce the Java LXR's closure execution/termination ownership, not merely increase configured worker count |
| F02 | Locked CAS used to read every field in the shared Full scanner; H x64 slot load is an ordinary read | G/A. Concurrent-reader atomicity must be separated from the STW Full contract; do not substitute plain loads into concurrent code without proof |
| F03 | Global per-field atomic counter beside every field read, and other always-on engine metrics; H detailed `stat` work is compiled out without instrumentation | G. Distinguish required collector state/counters from optional observation; do not pretend every H counter is disabled |
| F04 | Port buffers all non-null target objects and rejects duplicates after trace lock plus checked RC/mark reads; fresh targets also need extent checks; H buffers slots and filters marked children earlier in ordinary-object field enumeration | G/A. Match work representation and liveness/forwarding authority before removing guards; H does not have the same stale-target representation or that early filter on its large-array-slice branch |
| F05 | Strong roots are queued by Full root-increment work and are seeded again by PrepareTrace; logged increment work is also created with trace enabled, seeding its targets | G. H ProcessIncs schedules Full closure from root slots, not a second generic trace of every mature logged-increment target. Establish which root/edge work is genuinely required rather than silently retaining redundant seeds |
| F06 | Port Full arrays use 1024-slot resumable descriptor slices; H Full uses 8192-edge array slices | G. Full batching fixed object-target packet allocation, not this array/descriptor granularity or resumable-state architecture |
| F07 | Every inactive-to-Full entry commits and value-initializes dense trace-lock/scan-pointer planes; Drain decommits them after completion | G/A. These are port-private support structures, not the Java LXR's mark table. Preserve real reader lifetime/cleanup while deciding whether Full needs them, can retain storage or can initialize only used state |
| F08 | Accepted raw tracing uses generic checked RC+mark reads and generic mark write, plus per-object trace locking; H Full marking is fixed-spec and does not perform the same Release RC-zero gate on every Immix target | G/A. The port's late stale-target/death-reader representation may need guards; it does not establish that this exact API stack and six-fence sequence is necessary |
| F09 | A successful checked Increment/non-death Decrement primitive has four ownership walks/three translations and word CAS; Full's DecrementWithDeath can do additional checked work; H uses fixed RC metadata, byte atomic and specialized checks | G/A/P. Validate at the correct collector boundary; retain address/spec/lifetime obligations without defining the generic checked API as the permanent hot kernel |
| F10 | Root capture walks every occupied object for finalizer/pin flags, including a flag `fetch_and`; finalizer discovery performs another full object walk even when most objects are ordinary | A/G. .NET finalization and pin observations are required; repeatedly discovering them through the ordinary heap is a representation choice, not a requirement to copy into a fast port |
| F11 | Movement-enabled pauses repeatedly repair both root banks, decrement logs, owned target packets and descriptors; EE root/handle relocation is invoked multiple times | A/U. Required stale-reader/forwarding safety is real. Determine movement/dirty-set/phase witnesses before skipping passes; the nonmoving mode already exits the engine repair early |
| F12 | Final EE work always calculates heap/fragmentation statistics through occupied objects and checked IsMarked; H maintains space/page state and its extra live-byte aggregation option defaults false | A/G. .NET memory-info semantics are required, but do not mandate this separate object/metadata pass |
| F13 | Port block/line sweep runs general checked state and hole operations; H schedules chunk-batched direct metadata sweep jobs | G/A/U. Both actually sweep; port occupied-object bitmaps can save work relative to H's dense possible-start scan, so this is not a blanket "more objects scanned" claim |
| F14 | Default RC8/word CAS, no equivalent prefetch path, and port-specific evacuation caps differ from H RC2/byte operations/prefetch/custom evacuation policy | P/A. Compare matching modes/defaults separately. Do not change geometry, delete .NET pin safety or restore literal unsafe to-space overshoot merely to match an option name |
| F15 | One registered EE scan executor handles roots/lifecycle/memory-info work serially; H registered collector workers receive stack/list work | A/G. EE thread-registration requirements must be honored. Single-executor architecture is not proven to be the only valid integration |
| F16 | Successful Work packets and copied payloads remain on the port's ownership list after execution, until explicit retirement/Drain; H drops the executed packet immediately | G/A. Root lists and other live readers can still outlive H packets. Reconstruct work ownership/repair before shortening the port lifetime; do not confuse accepted batching with the Java LXR's packet lifetime |

F16 anchors: N `src/coreclr/gc/lxr_reference_counting.cpp:1462-1538,1541-1610,1837,2048-2069`;
H `src/scheduler/worker.rs:284-305`. For a completed normal Full, the
post-pause Drain is the cleanup boundary; RetireCompletedWork also runs
at the next suspension. The ownership list is not an unbounded leak.

F09 anchors: N `src/coreclr/gc/reference_count.cpp:295-363,504-548`,
`src/coreclr/gc/side_metadata.cpp:390-507,596-715`;
H `src/util/rc.rs`, including fixed RC geometry and `inc/dec/count`.
The four-walk count is the primitive's successful ordinary access path,
not the total decrements or callback work for an entire Full collection.

This table is an execution-gap map, **not** sixteen independently measured
causes of the full slowdown. An optimization proposal must name which
row it addresses and which other rows remain.

The serial trace drain, checked metadata layers, atomic representation
and default differences were already known. The per-edge RMWs,
early-versus-late filtering/seeding, array continuation granularity,
dense-plane churn and whole-heap integration passes are now made
explicit in the end-to-end map; none was repaired by the checked-pair
experiment.

### Two source-derived work volumes

At the existing H128 configuration with 64 MiB admitted Immix and 64 MiB
admitted LOS, N `CommitTracePlanes` uses:

- Immix: 8,388,608 entries per trace plane, at one byte per atomic lock
  and eight bytes per scan pointer.
- LOS: 16,384 page-indexed entries per plane, not a dense pointer for
  every eight LOS bytes.
- Combined: **75,644,928 bytes (72.140625 MiB)** and **16,809,984
  value-initialized cells** for the two planes per Full entry.
- Eight such requests project **605,159,424 initialized bytes**.

These are counts from
N `src/coreclr/gc/lxr_reference_counting.cpp:659-677,1846-1849,2048-2066` and
`src/coreclr/gc/lxr_virtual_array.h:37-61,67-84`, assuming the stated admission and
64-bit target. `Commit` does not skip initialization of already-committed
cells. They are not an observed wall-time percentage or a claim that all
bytes become resident simultaneously. Mark-table clearing is separate.

For an array of 262,144 pointer fields, 1024 versus 8192 granularity means at
least **256 port slot slices versus 32 H slices**. The port can also need
a continuation which discovers iterator completion after a final exactly
full slice. This is not the complete packet count for the collection:
RC buffers, roots, descendants and other VM objects also produce work.

## 4. Required .NET differences, and claims we must not make

These obligations cannot be removed by appealing to a JVM collector:

- pinned heap objects, POH and interior roots;
- frozen objects and native/collectible class-loader edges;
- short/long weak pointers, dependent handles, finalization ordering,
  resurrection and reentrant collection;
- stable admission/growth, failed commit and current source-incarnation
  authority for raw remembered slots and readers;
- correct GC-EE thread/suspension/root scanning contracts.

The [existing parity record](../README.md#p03-outcome-in-one-paragraph)
documents that the Java LXR pinning contexts are unsupported and
its pin predicate is constant false. Copying that behavior would not
produce a correct .NET collector.

But "required semantics" is not evidence that all current generic
validation, dense planes, scans, locks or atomic operations are required
at every field/object in a stopped-world phase. Each needs a specific
contract and a minimal lifetime/phase witness.

The reconstruction also rules out several misleading explanations:

1. **Not a VM callback per ordinary object/field.** GetLoaderAllocator
   returns before its callback for non-collectible types. GCDesc range
   enumeration is GC-side C++ code. Ordinary fixture Nodes must not be
   assigned a hypothetical per-object loader callback cost.
2. **Not unconditional timing-system-call instrumentation.** CostScope
   and ScanCostScope clocks are diagnostic-gated. Their existence does
   not mean normal timing pays for the diagnostic clocks.
3. **Not a zero-filled 64 KiB target array per batch.** TraceBatch's
   target array has no value initializer. Its stack frame, completion
   lease and copied payload remain costs, but a gigabyte of invented
   batch zeroing is not evidence.
4. **Not every Full task serial.** RC/log buckets use the scheduler;
   the exact gap is the directly drained transitive target closure.
5. **Not all Java LXR bookkeeping is free.** H allocates packets, uses
   atomics, root lists, safepoint handshakes and actual sweeps too.
6. **Not a 16 KiB LOS default inferred from a comment.** H's constant
   has a "half a block" comment but defaults to `Block::BYTES` unless
   `lxr_los_16k/32k` selects otherwise
   (`src/policy/immix/mod.rs:11-22`). Do not invent a mismatch from the
   stale comment.
7. **Not proof from a worker count or a source function's presence.**
   The queue must actually be submitted in Full and the feature path
   must be compiled.

## 5. P is a separate historical oracle

P's LXR lives under the Immix feature configuration, not H's
`src/plan/lxr`. P schedules `FullTraceFast` through
`schedule_immix_collection<RCImmixCollectRootEdges,...>`
(`src/plan/immix/global.rs:315-365`). Its normal user-request handler
body is commented out (`src/plan/global.rs:590-598`); the
`nogc_no_zeroing` override is not the ordinary build. Thus the shipped P
pair is not an oracle for a benchmark which assumes eight serviced
`System.gc()` requests. H does service them unless explicitly disabled.

P uses a 16-byte RC lattice; H derives an eight-byte x64 lattice. Both
default to RC2. P's mark source uses SC atomics whereas H uses relaxed
fetch-update; it is wrong to attribute H's orders to P, or to infer that
a P SC source operation emits the port's pair of explicit full fences.
Neither difference authorizes changing the .NET physical `Object* - 8`
or atomic-width contract without a separate proof.

The existing qualification record also notes P's sustained-run
failure and H's working shipped-pair runs. This audit does not promote P
to a stable runnable specification or silently mix P/H code.

## 6. What should be reconstructed first

The next implementation should be a **Full path shaped like the Java LXR's**,
not a third optimization of a helper inside the existing generic path:

1. Match the actual Full work graph: roots/log RC preparation, slot/slice
   closure work, worker publication/termination and eager reclamation.
   Make F01/F04/F05/F06 explicit rather than preserve object-target
   resumption machinery by default.
2. Separate STW Full scanning from concurrent resumable/death-reader
   duties. Account for ordinary slot loads, optional observation and
   trace-support state in F02/F03/F07, with the phase/lifetime proof
   required by real .NET callbacks and readers.
3. Reproduce the fixed metadata kernel under validated collector
   invariants, not a public unchecked API. Resolve F08/F09 and the
   representation/default questions explicitly.
4. Preserve .NET lifecycle/root semantics while removing unconditional
   ordinary-heap discovery/statistics work where maintained indexes or
   already-required passes can provide it. Treat F10/F12/F15 as
   integration architecture, not "Java does not need finalizers".
5. Retain forwarding/owner/incarnation/pinning proofs. Optimize F11/F13
   only after identifying which producer/reader/dirty set actually
   requires each pass. Do not delete the safety obligations.

For each proposed change, the comparison gate should state oracle work,
port work, exact intentional .NET delta and a testable operation-count
budget. Throughput and both stock .NET baselines remain measurements,
not replacements for this fidelity check.

## 7. Limits of this reconstruction

- This is complete for the audited normal H Full request and accepted
  N Full control path, including mode-conditional copying and lifecycle
  boundaries. It is not every feature combination or proof that all
  collector mechanisms match.
- Existing .NET forced-Full observations and H DaCapo/paper performance
  results are different workload/runtime experiments. This report
  identifies path differences; it does not manufacture a matched
  cross-runtime throughput ratio.
- No exclusive timing is assigned to F01-F16, and their costs must not
  be added as independent percentages. Some overlap, and some port
  representations also avoid work the Java LXR performs.
- Prior profiles include setup/warmups/cleanup and unresolved frames.
  A whole-process metadata leaf fraction is not exclusive Full cost.
- The serial closure is not, by itself, an explanation of the gap
  against Workstation GC. Scalar operation costs and extra work remain
  essential comparison dimensions.
- The latest rejected metadata experiment addresses only a small part
  of F08. Its failed goals and regressions remain in the P12 record.
- Accepted .NET source remains e8d. No source patch, build, benchmark,
  push, merge, ref promotion, default change, production qualification
  or P1.7/full-P9 closure is made by this report.
