# Mechanism conformance and log storage vertical

One integrated Windows/Linux x64 vertical from
`039e630ed1866505ccf75d87ca48cbfd321563a7`. This record does not close the
historical parity ledger, qualify production compatibility, or turn commitment
into RSS or CPU. The storage goal passes on both OSes at unchanged admission;
CPU/wall changes are mixed point estimates, not a throughput qualification.

## Independent audit before product changes

The audit extent is the **27 unique live IDs** in P0.3: A01-A05, B01-B05,
C01-C05, D01-D02, E01-E02, S01-S03, R01-R05. C06 is a separately checked
retirement redirect to R05, not an extra mechanism. The older prose saying
26 rows is historical, not the denominator. Each row below was checked against
its declared revision and the reachable owning implementation, not merely its
name, a status summary, a test observer, or a count of green checks.

`P` below means binding `abbdd1dbbe7277e4b0dfd1d37ba613c53f5ca50d` /
core `df8d30a39237a5bf5a8e27ca5a6f46acc6080c94`; `H` means binding
`0682434ae725787e38defbc17bd66dc918e4bdb7` /
core `304ce69d43aae87de501111fceb8cbd33173a03a`. Revision-qualified
`git show`/`git grep` in read-only `C:\github\lxr-reference` supplied the
reference code. Both binding Cargo pins were checked. Checkout9625c174 and
simplified trees are not oracles. Groups A/B retain P-declared/H-validation
history; the integrated engine's H basis is an explicit adaptation, not a
retroactive ledger edit. Paper citations mean extended arXiv:2210.17175,
1 November 2022, sections3.1-3.4, with P0.2's identity and measurement limits.

Port paths in the table are under `src/coreclr/gc`, unless prefixed `vm/`
or `jit/`; all name **039e630** before any product change here. Native check
names refer to the existing P4 executable's actual selectors. These tests
qualify their mechanisms, not every historical closure obligation.

| ID / declared oracle / exact reference source and symbol | Disposition and reachable port evidence at039e630 | Tests and remaining limit |
|---|---|---|
| A01 / P `util/rc.rs::{RC_TABLE,RC_STRADDLE_LINES}`; `metadata/side_metadata/spec_defs.rs` | **Explicit adaptation.** `side_metadata.cpp::LxrSideMetadataLayout::Create`, `reference_count.cpp::Initialize` and engine `Initialize` use physical Object*-8 and an8-byte lattice, not P's16-byte RC lattice. RC reservation allows8-bit maximum, active width remains selectable. Straddle values occupy a byte rather than P's bit. | Integrated widths2/4/8, object planes, poisoned-dead-layout and boundary tests. Material metadata overhead is real; no claim of identical paper geometry. |
| A02 / P `util/rc.rs::{LOG_REF_COUNT_BITS,MAX_REF_COUNT}` | **Explicit adaptation.** `lxr_gc.cpp::Initialize` selects2/4/8, default **8**, versus reference default2. `ReferenceCountTable` propagates active width through saturation and metadata; the field barrier does not read RC width. | Integrated tests cover all widths. The ledger's emitted-barrier-width-difference obligation is not satisfied: this barrier's claim protocol is RC-width independent. Default8 is not silently called paper-default parity. |
| A03 / P `util/rc.rs::RC_STRADDLE_LINES` and promotion/unmark paths | **Explicit adaptation.** `reference_count.cpp::{MarkStraddleLinesQuiescent,ClearStraddleLinesQuiescent,DecrementWithDeath}` and engine `ProcessIncrement/Death` keep paired RC/continuation sentinels;24-byte minimum and physical starts are CoreCLR-derived. | Original30979, all widths, multiline/copy/reuse/dead-layout cases. Geometry differs, required straddle lifetime does not disappear. |
| A04 / P `util/rc.rs::{ProcessIncs,ProcessDecs}`; H `plan/lxr/rc.rs` | **Faithful core invariant; explicit ownership adaptation.** `LogBeforeWrite` checks nonzero old RC in Debug **and Release** with `OldTargetChecks`; `ProcessIncrement`, `ReferenceBatch`, `ProcessDecrements`, `Death` register children before parent completion. Sticky maximum is preserved. | Zero-RC wrong control, original/trace suites, root lease/death/cycle tests. Focused loads are not a new DaCapo-equivalent sustained qualification. |
| A05 / P `policy/immix/{block.rs::BlockState,line.rs::Line}` | **Explicit adaptation.** `immix_block.{h,cpp}` implements H's state/phase/owner/lock unit, as already declared in P2.2, rather than mixing P's Nursery/Reusing byte tags with H epochs. `StartGcPause/ReleaseGcPause` and `TryReleaseIfRcDead` are reached from the owning engine. | Original/state/hole/headroom tests; fixed32KiB blocks/256-byte lines. P's alternative block-size features are not supplied. |
| B01 / P binding `barriers/mmtkFieldLoggingBarrier.cpp` interpreter/C1/C2; H binding `mmtkFieldBarrier.cpp` | **Real representation/qualification gap.** `jit/codegenxarch.cpp::genCodeForStoreInd` calls the write-barrier helper; `vm/amd64/JitHelpers_FastWriteBarriers.asm` and `.S` supply patched leaf templates plus `SlotLogWriteBarrier` slow stubs. This is not the ledger's in-method, JIT-emitted three-tier sequence. | Existing scalar/bulk/CAS coverage is mechanism evidence, not closure of the inlining obligation. P1.7 remains FAILED/INCONCLUSIVE; no barrier-overhead/paper-throughput parity. Fixing this would expand VM/JIT scope and is not attempted here. |
| B02 / P `plan/barriers.rs::FieldLoggingBarrier::slow/flush`; H `plan/lxr/barrier.rs::flush_decs_and_satb` | **Explicit adaptation with accidental overhead.** `Append/PublishReferences/PrepareWork` use two fixed ingress chunks/context and four admission-backed streams instead of allocating/transferring Vecs in the callback. Sealed decrement spans are already shared by RC/SATB readers. | First-store162, >1024 sequential identities, partial flush/contention, trace8358, growth1952. No per-epoch identity quota. Four flat payload/publication streams cost5x admitted bytes; this is the storage target. |
| B03 / P `plan/barriers.rs::log_edge_and_get_old_target`; declared H deviation `plan/lxr/barrier.rs::log_slot_and_get_old_target` | **Faithful recorded H deviation.** Old target precedes the single slot claim; `LogBeforeWrite` uses SeqCst field-bit CAS, no separate RC lock plane. VM scalar/CAS/bulk funnels retain the same pre-store contract. | Same-slot writers/failed CAS/bulk/first-store controls. Inert `LOG_BYTES_PER_RC_LOCK_BIT` is not ported. |
| B04 / P `util/cm.rs::ProcessModBufSATB`; H `plan/lxr/cm.rs` | **Explicit ownership adaptation.** `PrepareWork` shares sealed decrement spans; `Execute(Satb)`, `QueueTrace`, `FinishDyingTraceObject`, `ScanDecrements` retain independent descendant copies and mark/scan before death. `DrainEpoch/RunPause/ReleaseEpoch` bound bank reuse. | Sticky-target lost-SATB control, deletion control, held actual scan, interruption and source-reuse receipts. Existing borrowed-span optimization is baseline, not a new win. |
| B05 / P `args.rs::CM_LARGE_ARRAY_OPTIMIZATION`, `util/cm.rs`; H `plan/lxr/cm.rs::enqueue/trace_arrays` | **Explicit adaptation.** `ProcessTraceObject/ScanTraceLocked/ProcessTraceScan` scan GCDesc ranges in1024-reference slices and retain a descriptor with explicit users; primitive payload is skipped and collectible loader edges included. H's chunking is unconditional; dead flag not ported. | Trace8358, nursery-reader48, mature-reader47 and completed-descriptor45. Descriptor/range rebase and current-owner guard are required .NET safety deltas, not literal Java-array storage parity. |
| C01 / H `scheduler/work_bucket.rs::WorkBucketStage`, `scheduler.rs`, `worker.rs::run` | **Explicit adaptation.** `lxr_work_packet.h`, `lxr_pause_schedule.cpp`, scheduler `AdvanceBucketsLocked/WorkerLoop`, coordinator `CompletePause` retain active ordered stages and defer only after real resume. Disabled Java-only stages are not invented .NET work. `ConcurrentResumable` remains declared additive secondary evidence, not H content. | Ordered/deferred/completion tests and integrated trace interruption. Native worker packet execution is not automatically EE scan-thread registration; the separate registered executor owns EE calls. |
| C02 / H `policy/immix/rc_work.rs::SweepBlocksAfterDecs` | **Faithful release ordering with explicit arena representation.** `ReclaimNursery/ReleaseEpoch/Sweep` and `TryReleaseIfRcDead` release nursery after increments; mature release needs the completion guard. RC-table holes, not dead MethodTables, drive reuse. | Original30979, storage1952, aux-retirement12307, poisoned layouts, early-release controls. No sweeping payload parse. |
| C03 / H `policy/immix/rc_work.rs::SweepDeadCycles` | **Faithful core mechanism.** `PrepareTrace/TraceRoot/SweepCycles` reclaim unmarked nonzero/sticky cycles after complete trace, clear old root leases before address reuse and avoid decrementing arbitrary dead-cycle edges. | Trace8358 and selected-host cycle/weak/dependent suites, repeated physical reuse. Not broad production leak/stress qualification. |
| C04 / H `vm/collection.rs::stop_all_mutators/resume_mutators` | **Explicit required VM adaptation.** `lxr_gc.cpp::Suspend/Resume` call existing EE SuspendEE/RestartEE; root capture runs on existing EE-created GC-special thread. Suspension/scan serialization replaces H's per-mutator visitor. | Selected-host roots, all modes, actual held scans; no new suspension or GC-EE contract. Executor failure injection/hosting races remain unperformed as in P7. |
| C05 / H `util/heap/gc_trigger.rs::handle_user_collection_request`, `plan/lxr/global.rs::select_collection_kind` | **Explicit policy decision.** `GarbageCollect` requests truthful blocking Full/FinalMark via `CollectForPressure`; generations/modes do not create fictitious ephemeral collection. H default user-full behavior is retained rather than P's commented-out handler. | Full-call/count/diagnostic lifecycle tests. Ignore-user-GC option and per-generation optimization are not implemented; broad public-GC API compatibility is not claimed. |
| D01 / H `plan/lxr/{mature_evac.rs,remset.rs}`, `policy/immix/{rc_work.rs,defrag.rs}` | **Explicit safety/ownership adaptation.** `SelectMatureCandidates/RememberMatureSlot/ReplayMatureRememberedSlots/TryCopyMature/RetireForwardedSources` preserve selection, half-live estimate and RC transfer. Selected retained owners retain outgoing repair; fresh reuse events, current live owner/exact GCDesc field checks, active descriptor repair and incarnation-authoritative release deliberately strengthen literal H. | Mature3324, retained22, incarnation117, reader47, completed45, whole532, wrap1375, same-shape1376, boundaries5505; omitted-shape/range controls remain. Pin/failure retained blocks are not unconditionally released. |
| D02 / H `policy/immix/block_allocation.rs::BlockAllocation`, `util/alloc/immix_allocator.rs` | **Explicit arena adaptation.** `ImmixAllocationManager/ImmixBumpAllocator` own exact fresh/recycled ranges; engine `AllocateCopy` separates small reusable holes from fresh-only multiline overflow. Private cleared-span witness avoids duplicate fast-birth validation. | Original/storage/copy/source-reuse and all-width tests. Contiguous fixed reservation differs from H page resources/global block buffer; no generic allocator change here. |
| E01 / H `plan/lxr/global.rs::LXR`, options Plan selection | **Faithful selection intent; explicit platform boundary.** `gc/CMakeLists.txt::clrlxrgc` excludes stock GC engine; `lxr_gcload.cpp::GC_Initialize` constructs owning LxrGcHeap and real handle manager, GC interface5.15. | Real GCPath ownership and default-off source wiring. Windows/Linux AMD64 only; no R2R/other architecture/Mono/NativeAOT expansion or unused-cost requalification. |
| E02 / H `args.rs::RuntimeArgs::default` and actual consumers | **Real bounded omissions/default differences, explicit below.** `lxr_gc.cpp::Initialize`, `lxr_collection_policy.h` consume implemented knobs; `FinishLazy` precedes hints, and only actual nursery copied bytes feed prediction. | Policy50/headroom131, environment/JSON controls, actual modes. Max-pause/huge-page/prefetch behavior is not silently ported; no full tuning parity. |
| S01 / H `util/object_forwarding.rs::{attempt_to_forward,try_forward_object}` | **Explicit object-header adaptation.** `TryCopyNursery/TryCopyMature/ResolveForwarded` use negotiated claim2/publish3, complete destination metadata before publication, source first payload forwarding and preserve sync/hash/MethodTable. In-place fallback establishes RC under claim. | Racing aliases/all widths, native readers, managed hash/monitor/pins/source reuse. No read barrier or new header/interface contract. |
| S02 / H `vm/object_model.rs::ObjectModel` | **Explicit VM adaptation, full trait closure unverified.** `gcref.h::{GCReferenceRanges,GCReferenceRangeIterator}`, engine `CompleteAllocation/GetObjectSize`, generic enumeration/header negotiation and host LoaderAllocator callbacks implement required layout/scan/copy operations. Java type descriptors/debug dumps and every trait method do not have individual conformance tests. | Real strings/arrays/mixed value types/collectible/interiors/notifying births; existing generic tests and selected-host suites. Object*=physical+8,24-byte minimum,8-byte alignment. No assertion of full VM-binding API parity. |
| S03 / H `policy/largeobjectspace.rs::{initialize_object_metadata,release_rc_nursery_objects,release_object}` | **Explicit arena/page adaptation.** `LxrLargeObjectSpace::{Allocate,InitializeAllocationMetadata,TryMarkAndPromote,ReleaseAfterDecrements,SweepNursery}` owns page-aligned spans, two state bits, wrapping page reuse, zeroing and independent page decommit. Threshold32768 inclusive remains declared P2.5 override. | Exact threshold/interior/wrap/reuse/neighbor/owner/failure and managed OOM tests. Supplemental start storage is page-indexed; field RC/log lattices are not made page-granular. |
| R01 / none | **Explicit platform decision, not reference parity.** Owning Initialize rejects beyond47-bit arena; `side_metadata.cpp` rejects non64-bit pointers. Object*/Object** are full native references. | Windows/Linux x64 only; no compressed/32-bit reference claim. |
| R02 / none; H class-unloading path is historical-only existence | **Required .NET adaptation; broad closure unverified.** LoaderAllocator edges enter increment/decrement/trace and owning handles relocate them; `GCScan` runtime notifications remain. | Collectible ALC and GC/non-GC static weak-interior cases; not every unload/profiler/runtime suite. No measured MMTk unloading provenance invented. |
| R03 / none | **Required .NET adaptation; broad closure unverified.** Host `ExecuteScan` uses real handle-table fixed point/short-weak/discovery/pending trace/repeated dependent/long-weak/SyncBlock order; nontracing leases include null-secondary owners. Normal precedes critical; actual finalizer thread consumes pending flags. | P7 focused weak73, dependent139, null20, identity13, reregister5, refcounted15, churn1050/nested3 and full suite100632. Finalizer discovery and dependent-owner fixes stay; this is not every CoreCLR handle/finalizer suite. |
| R04 / none | **Explicit CoreCLR derivation.** `gcref.h`, `ReferenceCountTable::MinimumObjectSize`, side layout, block geometry, Start/Reference derive8-byte header/slot and24-byte aligned extent; LOS32768,32KiB blocks/256-byte lines are asserted prototype choices. | Static assertions, boundary/physical-start/interior/layout tests. H's MiB survival term added to page headroom remains literal64, not16384; true admission/copy limits stay independent. |
| R05 / none; C06 retired | **Required .NET adaptation, no reference realization.** H LXR contexts use UnsupportedProcessEdges and disable pin stages. Host discovers pin/interior/POH/frozen facts before copy; retained block repair and RC-dead-only release handle immovable heap residents. | Genuine managed fixed/pinned/native buffer/header/monitor cases, native retained/release controls. Not arbitrary interop/hosting qualification and not Java-pinning parity. |

No newly reproduced collector correctness defect has been established in this
audit. That is not proof of absence. B01, A02's literal emitted-code closure,
E02 omissions and broad S02/R02/R03/R05 qualification remain open; expanding
VM/JIT/API scope to erase them is excluded. Known safety adaptations are not
new blockers to the accepted prototype and are not removed for performance.

The post-diff revision-qualified policy recheck also retains a literal D01
selection difference: H's two selection passes can select a first block with
zero budget or another block after the first pass reaches its estimate. The
port caps a combined selection against actual admitted Immix to-space and
selects none at zero budget. This is an existing fixed-arena admission/policy
adaptation, not literal selection-set parity or a new correctness fix here.
H's young-allocation trigger really uses Immix clean/recycled allocation,
not the separately printed LOS-young total; the port does not silently add LOS
volume to that predictor/trigger denominator.

### Defaults, units and omitted configuration

All entries below were traced from H declarations to consumers, not inferred
from their printed names. Port controls are decimal `DOTNET_LXR...`/existing
runtimeconfig keys through existing GetString/GetIntConfigValue seams.

| H setting / consumed default |039 port mapping / disposition |
|---|---|
| `INCS_LIMIT` / unbounded entries | `LXRIncrementsLimit` / unbounded entries |
| `NO_MUTATOR_LINE_RECYCLING`, `NO_LINE_RECYCLING` / false | Same two port controls; copy recycling remains independent |
| `NURSERY_BLOCKS` / unbounded blocks | `LXRNurseryBlocks`; fixed32KiB block units |
| `YOUNG_LIMIT{,_MB}` / unbounded MiB | `LXRYoungLimitMB`, overflow-checked conversion to bytes |
| `NURSERY_RATIO` / absent | `LXRNurseryRatio`; admitted hard-limit-derived block budget |
| `MAX_PAUSE_MILLIS` / absent | **Unsupported nonzero** `LXRMaxPauseMillis`; H `rc.rs::ProcessIncs::do_work` really uses it to stop copying, so this is an omission, not an inert knob |
| `MAX_YOUNG_EVAC_SIZE` / usize maximum | `LXRMaxYoungEvacSize`; inclusive size exclusion |
| `RC_STOP_PERCENT` /15 | `LXRRCStopPercent` /15; percentage and1GiB-in-pages cap retained |
| `MAX_SURVIVAL_MB` /128 | `LXRMaxSurvivalMB` /128; MiB threshold, not actual heap admission |
| weighted/harmonic predictors /false | Same controls, weighted precedence and literal formulas |
| `TRACE_THRESHOLD{2}`, `CM_THRESHOLD` /20 | `LXRTraceThreshold` /20 percent; legacy spelling aliases not added |
| `MIN_REUSE_LINES` /1 | `LXRMinReuseLines` /1 line |
| `CHUNK_DEFARG_THRESHOLD` /32 | `LXRChunkDefragPercent` /32 percent, Full doubles/caps100 |
| `TRANSPARENT_HUGEPAGE`/`HUGEPAGE` /true | **Unsupported nonzero**; H `util/memory.rs` actually issues advice. No huge-page parity |
| buffer feature sizes256/512/1024/2048, default1024 | Runtime256/512/1024;2048 omitted, ingress physical maximum1024 unchanged |
| `LAZY_DECREMENTS`, nursery/mature evacuation /true | Same default toggles in explicitly selected collector |
| RC feature widths2/4/8, default2 | Port2/4/8, default8; material metadata/CPU difference |
| `PREFETCH{,_HEADER,_MARK}`, step8 /enabled | **Not implemented**; real RC/CM consumers exist, not an inert declaration |
| `LOG_BYTES_PER_RC_LOCK_BIT`, `CM_LARGE_ARRAY_OPTIMIZATION` | Inert at H; deliberately not replicated |
| `ignore_system_gc` /false | No ignore option; explicit calls remain truthful Full/FinalMark |
| defrag headroom /5% occupied pages | `LXRDefragHeadroomPercent` /5; only actual admitted Immix to-space usable |

### Baseline preservation and storage proof

The worktree was clean at17ac93d; local lxr was exactly039e630 and an ancestor
check preceded the authorized fast-forward of this worktree only. No other ref
or prior artifact was changed. P7's63 inputs at each declared physical source
root,6528 framework bindings,11 stage modules per OS, portable fixtures and
1308/1252 raw-catalog files matched their manifests. Current Windows source
inputs matched those63 bytes before changes; fresh Linux archive/compiler
bytes are bound separately rather than called identical CRLF inputs.
All12 retained PDB Program/Nursery checksums were independently reread; all46
fixture/native source-stage rows were independently checked against canonical
Git blob hashes or retained corrected2 source/patch bytes. These are different
checks, not guessed newline equality. Historical drafts without full source
are not reconstructed. Accepted final2 modules were built from564,
not039. This vertical separately builds exact039 native-only collectors in all
four OS/configurations and preserves independent runtime layouts.

The first fresh Windows configure stopped for absent generated `_version.h`
before compilation. Accepted generated version inputs are copied with separate
producer/path/hash bindings; their inherited version/@Commit text is **not**
039 source provenance. Original stopped log remains. Linux builds use a new
owned `/root/lxr-conformance-logs` source snapshot; prior roots are read/copy-only.

For admitted managed bytes `H`, each of two banks has an increment-slot and an
old-target stream of `H/8` entries. Every stream commits `H` payload bytes plus
`H/4` publication bytes. Thus total is `5H`:640MiB at128MiB admission, independent
of pointer density. Reservation follows maximum reservation, commitment follows
actual admission. Payload pages are not eagerly initialized; uint16 publication
entries are. Occupancy and occupied ranges do not justify shrinking this bound.

Each admitted field can contribute at most one increment and one nonnull old
target per epoch. Partial flush/detach reserves **actual entries**, not1024 slots,
so worst-case one-entry flushes do not consume a hidden identity quota.
Independently held RC/SATB readers, copied roots/trace descendants and physical
source reuse have different lifetimes. Existing immutable bank borrowing already
avoids RC/SATB duplicate payload arrays; it is not counted again.

H `plan/tracing.rs::VectorQueue::take`, `barrier.rs::flush_decs_and_satb`,
`rc.rs::ProcessDecs::new_arc`, `cm.rs` Arc snapshots and scheduler worker
`drop(work)` transfer/share owned payloads. Directly introducing that Vec refill
inside CoreCLR's no-GC/no-allocation first-store callback is not valid: it
requires allocation there or a new runtime admission seam. This vertical
therefore retains the full existing admission-backed banks and their readers.

**Proposed bounded representation:** co-locate a completed range's count in the
unused upper17 bits of its first64-bit entry, retaining the exact47-bit owned
slot/old-target address below it. Count1..1024 fits; count0 means unpublished.
This is a port-only publication representation, not an MMTk algorithm, identity
scheme, new barrier, smaller buffer, allocator pool or changed admission bound.
The owning engine already rejects arenas beyond47 bits, and discards classified
external immortal old targets before logging. Observer mode must retain the
existing full-width representation instead of silently narrowing its inputs.

Reservation remains separate from the release store of the completed header;
consumption acquires that header and faults on holes/invalid counts. Native x64
aligned64-bit loads/stores use the repository VolatileLoad/VolatileStore contract.
Only the first value is decoded by borrowed consumers; descendant trace copies
must contain decoded pointers. Forwarding must preserve count bits while
rebasing integer old targets. Reset clears only completed headers after the
same existing all-reader generation drain. Stale interior payload cannot look
published because its high bits are zero, and old headers are cleared before
bank reuse. Growth commits the same complete payload capacity before admission;
failed commits do not publish capacity. No callback commits, allocates, blocks,
changes GC mode, enters the scheduler lock or waits for consumers.

The intended exact backing change is640->512MiB at128MiB admission, with no
removed legitimate logging entry, bank, independent lifetime or observed-only
capacity heuristic. This is a20% stream-commitment reduction, not a claim of
20% RSS or CPU. Occupancy/high-water and actual changed measurements still need
qualification before this design can be accepted.

## Frozen qualification protocol

Before changed study execution, the goals are **640->512MiB committed log
backing at128MiB admission**, and **774049792->639832064 native-plane bytes**
(17.34% rounded) on the matched64MiB Immix/64MiB LOS census. The same20% log reduction
must hold for reference-rich and pointer-free inputs and24/64/128MiB admission.
The remaining512MiB is deliberately retained legitimate backing. RSS/private,
startup, reset traffic, request tails and completed-cycle CPU are measured and
reported, including regressions; no RSS/CPU percentage win is preclaimed.

The managed fixture uses65536/131072/262144 nodes for24/64/128MiB admission.
Reference inputs retain an array and linked nodes, rewrite **every** outgoing
Next field each round, complete eight explicit blocking full collections, and
check every resulting edge/value. Byte inputs retain an approximately matched
payload volume of12KiB arrays and check both endpoints after each collection.
All arms at each size use identical source/fixture, counts, capacities and
checksums. Two warmup full calls and one cleanup full call are separately
declared; neither is hidden in the eight-call region. Actual collection count
must be eight in that region. Process CPU is region all-thread CPU, not
exclusive GC CPU. Whole-process elapsed includes setup and startup as well.

The primary schedule is two interleaved A/B pairs (four normal observations),
one200ms injected observation and one separately intrusive diagnostic per
arm/input/size. Arms are flat-publication diagnostic baseline and in-band
candidate in all three modes, plus same-runtime WKS/Server characterization:
288 processes per OS, no retries. Four-sample medians are point estimates,
not CIs, SLA or throughput/paper parity. Diagnostics-on processes do not enter
normal medians. A/A variation and all injected/noisy/failed observations stay.

A separately frozen flat-publication **instrumentation-only** source stage
will provide actual per-stream capacity/reservation/high-water/flush/copy/reset
counters on the same representation as039. It must not be called an unmodified
039 module; original039 and accepted564 modules remain independently preserved.
The in-band source is preserved before this temporary diagnostic-only build,
then restored exactly before candidate qualification. Runtime/CoreLib/JIT are
not rebuilt and configured native build trees are reused. Unsupported counters
on original modules remain null, never fabricated zeros.

Native qualification covers widths1..1024 (including zero first/internal
values), every possible entry as a one-entry partial flush, corrupt counts,
holes, high-bit rejection and legacy observer full-width values. Actual held
lazy RC readers must survive next-epoch detach publication, at all RC widths.
All1024 simultaneous logical contexts retain their original admission bound;
existing >1024 sequential identity and first-store-before-allocation tests stay.
Wrong publication/decoder/capacity/reuse/drop controls must fail their actual
seams. Existing held moving readers, current-owner/tag-wrap controls and
lifecycle/full-suite checks must pass in native/managed Debug/Release on both
OSes after the last change.

The first candidate's storage failure1951/1952 is retained: an old injected
failure after28 commits no longer failed, because removing four publication
planes changes this fixture's successful growth path from29 to25 commits.
The test is strengthened to fail **every** actual commit position0..24 and
require success at25, not weakened to tolerate successful defective growth.
Those25 calls are five object planes, four payload streams, fifteen side
metadata planes and one managed-heap commit. Partial physical backing can
remain after failure; admitted capacity/old payload must stay unchanged and a
subsequent admission must recover.

## Implemented representation and independent review

Owned streams now allocate **only payload backing**. Every input entry is
explicitly checked for forbidden high bits before reservation. Only payload
words1..count-1 are memcpy'd; one aligned64-bit repository VolatileStore publishes
the first address and completed length. No non-atomic memcpy of the header
precedes an acquiring header read. Observer streams keep their separate atomic
uint16 tokens and full-width payload; their foreign-value control passes.
ReadPublishedRange checks stream bounds, holes, count<=1024 and range overshoot.
Borrowed Work decodes its first pointer on access, while owned descendant
trace work decodes before copying can escape. Forwarding preserves the count
while repairing an old-target integer. Bank flip/reset and all independent
reader drains are unchanged. No bank decommit, pool, smaller ingress, new
callback admission seam or managed-reference compression was introduced.

The one integrated independent code review checked actual consumers, x64
volatile/ownership rules and changed log rows against the pinned reference.
It found no new collector correctness defect, but correctly blocked study
qualification because the first runner recorded initial hashes then compared
children against recomputed hashes. The remedy freezes collector/runtime/
fixture expectations, checks files before and after every child, and checks
actual loaded LXR path/hash too. A preflight replacement/unreadable input
persists its raw failure and false row without executing; invalid child output
is also retained before rejection. Seven bounded mocked runner controls exercise
replacement, omission, unreadable input and failed-row retention; they are
explicitly **not collector or timing observations**.

The same review's single bounded follow-up independently checked those remedies
and closed the finding, with no remaining blocker in that scope. It did not
certify performance or every historical ledger obligation. Native positives
include new2115 codec/held-bank/context checks and2132 growth/birth checks;
all five wrong log mechanisms reject on both OSes/configurations. Omitted
decoder controls first report the actual payload mismatch and subsequently
fault when that bad address reaches the real consumer; those crash exits/logs
remain failures, not successful collector execution.

Fixture development failures remain separate: an initial PowerShell PDB
extraction command did not parse; a later build rejected Assembly.Location
under the existing single-file analyzer; a smoke then demonstrated that CoreRun
does not place CoreLib in its app-assembly TPA list. No analyzer was suppressed.
The fixture now records actual loaded native modules and separately labels
the framework CoreLib **input**, without pretending that a guessed TPA path is
a loaded-module observation. Final fixture4 was rebuilt and its Program/Nursery
compiler-byte hashes checked against its portable PDB; identical bytes run on
Linux, not an independent Linux managed build.

### Additional Debug/compiler and producer-bound checks

Building an actual **managed Debug assembly**, rather than only running the
Release fixture on a Debug native runtime, exposed a pre-existing test-scope
assumption. The final embedded `new DependentHandle(new Node(8001), null)` in
the dependent qualifier leaves a real compiler temporary rooted in Debug.
The unchanged Debug fixture fails the same death assertion on original039
and encoded collectors on both OSes. A NoOptimization before/after control
also fails the direct-constructor assumption on same-runtime WKS and Server,
and passes when a NoInlining factory returns only the native-handle struct.
The existing death assertion is preserved; no collector or closure algorithm
is changed. Separate scope5 Release/Debug correctness fixtures carry this
small repair. Immutable Release timing fixture4 and its46c366e source remain
unchanged, so no defective-work denominator is replaced by a cheaper input.

The exact fixture4 compiler Program bytes were recovered from the retained
fixture3 source archive plus the recorded CoreLib-binding-only edit, and
**verified against fixture4's own PDB checksum**. This is an explicitly labelled
PDB-verified reconstruction, not a contemporaneous snapshot or guessed
Git-LF/working-CRLF equality. Scope5 compiler bytes match actual current files.

A final producer-bound check also strengthened the1024-context probe: initial
newborn stores do not log, so payload survival alone was not proof of partial
ingress traffic. The owner is now promoted first, then every simultaneously
attached context replaces a real counted field;1023 have never allocated, while
one context supplied the setup objects.
Assertions require exactly1024 claims/old-target checks,1024 consumed increments,
1024 non-root decrements and2048 partial-buffer flushes. Earlier2115-check
results remain historical; the strengthened current suite passes2121 checks.
Both original and strengthened probes keep all1024 logical contexts live;
neither is a claim of1024 physical OS threads executing simultaneously.

## Final result: backing and scaling

The entire frozen schedule completed **once**:288 convergence observations per
OS,576 total/96 groups; four normal observations per group, plus one injected
and one separately intrusive diagnostic. The separate039-versus-flat-counter
characterization completes72 processes per OS/24 groups total. No child,
outlier, failed exploratory attempt or old missed goal was replaced or retried.
All actual fixture/collector/loaded-native identities, inputs, checksums and
eight completed full requests match the frozen expectations.

**All36 LXR size/input/mode/OS groups pass the exact20% stream-commitment goal.**
At128MiB admission the four streams commit640->512MiB, while every stream
still admits16777216 entries. Native planes, including host flags but excluding
separately measured side metadata/system allocator bookkeeping, are exactly
**774049792->639832064 bytes**, a134217728-byte reduction (**17.34% rounded**).
The384MiB managed reservation is unchanged. Log virtual reservation also falls
from1920->1536MiB because the separate publication arrays no longer exist;
this is not a larger-reservation or statistics-only trick.

Actual pre-region publication-plane residency in **all72 intrusive LXR
snapshots** is precisely24/64/128MiB for flat storage and zero for in-band
storage. These are OS page-residency observations, independent of the commitment
counter. The source-defined eager publication initialization similarly loses
H bytes at admission; that is logical program initialization, **not** a measured
memory-bus/writeback counter or exclusive startup CPU claim.

Matched normal medians at128MiB admission, n=4. Region times include fixed
application mutation/checks and eight actual blocking full requests. RSS and
private memory are post-region live-process observations, inMiB.

| OS / input / mode | Flat / in-band wall ms | Flat / in-band process CPU ms | Flat / in-band RSS MiB | Flat / in-band private MiB |
|---|---:|---:|---:|---:|
| Windows bytes / nonmoving |310.296 /315.083|312.5 /312.5|303.52 /175.50|861.87 /733.59|
| Windows bytes / nursery |315.942 /316.027|320.313 /312.5|318.35 /190.32|861.92 /733.68|
| Windows bytes / mature |525.341 /518.276|523.438 /523.438|347.71 /218.76|861.79 /733.69|
| Windows references / nonmoving |2676.415 /2694.454|4234.375 /4320.313|307.24 /179.26|866.53 /738.75|
| Windows references / nursery |3617.465 /3643.186|5234.375 /5273.438|315.58 /187.79|867.13 /738.52|
| Windows references / mature |5553.378 /5575.397|7164.063 /7195.313|364.61 /236.54|867.57 /738.73|
| Linux bytes / nonmoving |395.461 /393.166|428.323 /426.346|299.16 /171.17|941.44 /813.45|
| Linux bytes / nursery |390.699 /396.073|422.243 /426.548|315.82 /188.97|941.45 /813.43|
| Linux bytes / mature |704.452 /695.742|737.929 /728.452|350.79 /222.83|941.45 /813.44|
| Linux references / nonmoving |3259.919 /3277.799|4835.723 /4877.684|651.06 /522.02|1299.81 /1172.72|
| Linux references / nursery |4144.200 /4107.762|5800.406 /5759.586|663.92 /534.21|1303.11 /1173.87|
| Linux references / mature |7429.808 /7166.363|9238.876 /8982.449|676.38 /582.43|1258.91 /1167.07|

Windows live RSS/private generally lose about24/64/128MiB at the respective
admissions. Linux also loses material physical memory, but the128MiB mature
reference case's live RSS loses only93.95MiB, not128MiB; shared allocator/runtime
retention is still visible and is not explained away by the plane reduction.
Its separately intrusive quiescent RSS is658.91->530.37MiB. The remaining
512MiB legitimate payload commitment and substantial Linux quiescent private/RSS
retention remain. **This does not solve total footprint or establish P9 parity.**

## CPU, traffic, startup and tails are separate

Across all sizes/modes, normal wall point changes range-2.89..+2.18% Windows
and-4.18..+3.42% Linux. Reference-heavy128MiB Windows nonmoving/nursery/mature
wall regresses0.67/0.71/0.40%; CPU regresses2.03/0.75/0.44%. Linux nonmoving
reference wall/CPU regress0.55/0.87%, while mature wall/CPU improve3.55/2.78%.
These are retained point estimates, not a CPU win inferred from commitment.
The largest absolute A/A wall difference is5.28% Windows and9.83% Linux.
Short Windows CPU values are clock-quantized; its apparent-25% small-byte
CPU point is not zero-cost work or a precision result. **No small throughput
effect, CI, production latency bound or P1.7 qualification is claimed.**

The128MiB reference diagnostics preserve exactly4097 flushes and33554488
copied log bytes Windows /33554648 Linux in both arms and all three modes.
Reset-header traffic rises7170->28680 program bytes: an8-byte in-band word
replaces a2-byte token, **four times the reset bytes**, not free publication.
High-water/capacity/reservation/flush/copy/reset fields are per actual stream
in `stream-census.csv`. Linux128MiB byte-mature diagnostics copy280 versus264
log bytes, a small real runtime-work difference; workload inputs and requested
work remain fixed. Do not report collector-wide runtime activity as exactly
identical merely because required workload work matches.

The32 normal full-call durations per group and cleanup-call costs remain in
raw records and `comparisons.csv`. For example, Windows reference nonmoving's
observed request maximum grows325.153->368.331ms; Linux reference mature's
grows1047.823->1105.579ms despite its lower region median. Those are retained
sample extremes, not estimated production percentiles. Some cleanup costs also
regress, e.g. Linux reference nursery149.58->154.18ms. Region medians do not hide
those tails/backlog/retirement boundaries.

Parent process wall retains runtime startup, graph construction, two warmups,
recording/hashing and the declared cleanup full call. It is not pure startup;
subtracting region wall does not isolate startup or OS thread retirement.
At128MiB Windows reference nonmoving parent median is4591.21->4574.28ms,
Linux5399.51->5258.62ms. The counter-only overhead characterization's wall point
effects span-2.83..-0.12% Windows and-3.03..+1.56% Linux; it does not prove a
zero-cost diagnostic branch or +/-0.5% sensitivity.

All48 Windows injected groups show a positive185.18..242.20ms point effect.
**One Linux injected group is inconclusive:**128MiB references/flat mature has
normal median7429.808ms but injected7168.263ms, a-261.545ms difference despite
the explicit200ms delay. It remains in the population and is not rerun or
relabelled. Every other Linux injected group remains recorded too.

Same-runtime built-ins remain far faster: at128MiB bytes, WKS/Server region
medians are2.036/4.378ms Windows and2.783/5.590ms Linux; references46.883/42.113ms
and49.445/49.781ms. This is characterization, not equal-heap fairness or a
transfer of the paper's OpenJDK/G1 targets.

## Final qualification, identities and replay

After the last relevant test edit, each OS passes **184 scope5 managed
processes**: Release and Debug assemblies, each64 lifecycle plus28 full
acceptance children across both native configurations. Full suites retain
100632 checks in all three modes; dependent139, weak/finalizer73, null20,
identity13, reregister5, refcounted15, churn1050 and nested3 remain. Original039,
flat-counter and immutable fixture6/8 acceptance stages remain separate.

Final native executables are `candidate4-native-{Release,Debug}-{windows,linux}`.
Each configuration passes original30979, log2121, trace8358, growth2132,
accounting31, mature3324, reader47, nursery-reader48, first-store162,
retained-owner22, incarnation117, headroom131, completed45, whole532, wrap1375,
same-shape1376, boundaries5505, root batching6170, auxiliary retirement12307
and policy50. Post-study exact-exit replay separately retains40 positive
and18 wrong-mechanism processes **per OS**, including five log and four
coupled SATB/deletion/field-shape/range controls in both configurations.
These are acceptance replays, not additional timing observations.

Collector implementation/timing source is `46c366e0845737c8cf20eea3106aa28bfe1b2937`;
later test-only repair/evidence is `66aba3094482c81b402f06dcf2ccffe5b0cbed66`.
`stage-identities-*.csv` bind all six original039/flat/encoded OS/configuration
modules per platform and their matching primary PDB/ELF companions. Windows
RSDS GUID+age and Linux GNU debuglink filename+CRC are independently matched;
all companions also have SHA256 bindings. Inherited other symbol files in
copied frameworks are catalogued but not falsely qualified as current symbols.
All3282 Windows/3252 Linux framework-file bindings still match after execution.

Immutable timing DLL is `fixture-log4/runtime-tests.dll`,
SHA256 `5201b0ccc41ab3cd51313643d62eda6509cbc2e9c17e7aa0131d021f9913cae9`.
Scope5 Release/Debug DLLs and their actual compiler PDB bindings are listed in
`all-fixture-identities-*.csv` and `fixture-compiler-bindings.csv`.
Frozen65-input manifests describe the timed stage; current65-input manifests
explicitly describe later test-only source. Source archives/patches distinguish
original039, counter-only flat, encoded46c366e and scope/test repairs.
The failed pre-growth-test-repair native fixture is an explicit reconstructed
full file whose hash matches its original compiler-input manifest, not a
newly invented contemporaneous snapshot.

Physical artifacts are under this worktree's `artifacts\P9-log-conformance`
and `/root/lxr-conformance-logs/artifacts/P9-log-conformance`. Adjacent CSVs bind
raw catalogs, executed inputs, native/managed outcomes, module/fixture stages,
all observations and summaries; `measurement-limits.json` keeps unavailable
scopes null. Frameworks/SDKs and native build trees are independent copies;
no prior worktree/source/accepted artifact was modified. Only clrlxrgc and
existing native fixtures were built after one configuration per OS/config;
no full CLR/JIT/CoreLib/libs matrix, new allocator policy, public API,
dependency/audit bypass, public GitHub action, push or merge-back occurred.

Representative fresh-output replays from this worktree:

```powershell
artifacts\P9-log-conformance\candidate4-native-Release-windows\lxr-rc-validation.exe --log-storage
python docs\design\lxr-port\P7-lifecycle\run-qualification.py --artifacts artifacts\P9-log-conformance --stage candidate3 --fixture artifacts\P9-log-conformance\fixture-scope5-Debug\runtime-tests.dll --output artifacts\P9-log-conformance\new-Debug-lifecycle-windows
python docs\design\lxr-port\P9-log-conformance\run-study.py --baseline artifacts\P9-log-conformance\flat-Release-windows --changed artifacts\P9-log-conformance\candidate3-Release-windows --fixture artifacts\P9-log-conformance\fixture-log4\runtime-tests.dll --output artifacts\P9-log-conformance\new-study-windows --phase convergence
```

Linux uses the owned root above, `corerun`, `libclrlxrgc.so`, `.sh` native runner
and corresponding-linux paths. The first command must report2121/2121, not an
unknown-selector fallback's30979. Full exact commands/settings and observed
outcomes remain bound in the artifacts. Replaying does not replace any retained
sample or reopen unchanged P1.7/production/compatibility qualifications.

## Independent acceptance seal

Before this final record commit, the coordinator independently passed
**224 positive and18 exact-negative processes on each OS**: all184 scope5
managed Release/Debug assembly cases plus40 native positives and18 actual
wrong seams, explicitly checking `--log-storage`2121 rather than a default
selector fallback. That is448 positive and36 expected-negative acceptance
processes across the two OSes, not extra timing samples.

It independently recomputed each OS's360 study rows/60 groups,720/120 total,
including all36 exact unchanged-capacity20% backing goals, loaded inputs,
per-stream census, A/A, tails, counter-only overhead and the retained negative
Linux injected effect. It verified six primary native symbol bindings and
ten portable-PDB actual-source documents per OS. Windows65 timed physical
inputs were separately checked against canonical46 with explicit bilateral
EOL normalization; Linux's own65-input form, including its retained PS1 bytes
and the two old timed test sources, remains separately bound. Neither
normalization nor inherited native @Commit text is used to invent compiler
provenance.

All39 adjacent physical **and Git-staged** record blobs match the final index,
SHA256 `fd79b23ad5b421401190776b11299d14d9672c4ad9a8d004c73d415e5cd25e3f`.
Scoped byte-preserving attributes prevent checkout CRLF conversion from
invalidating Python/text/CSV/JSON seals. Coordinator-only early parser,
Python-version, path and EOL-assumption stops remain tooling attempts, not
collector/timing failures. It requested no new study, tuning or collector
change and cleared the final local record commit. The open audit rows,
remaining512MiB payload, prior retention/copy regressions and unqualified
production/API/platform boundaries remain open.
