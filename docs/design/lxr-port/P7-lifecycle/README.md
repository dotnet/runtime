# Lifecycle closure and full-cycle execution cost

One integrated Windows/Linux x64 vertical on exact accepted moving tip
`99faa6aec19570bafe192b323e4925aeb55ac7db`. It qualifies the tested P7.1-P7.3
lifecycle contracts, fixes two concrete gaps, and reduces measured full-cycle
execution cost through the existing EE scan-thread contract. It does not
claim the full P9.1 ledger, production stress, throughput/footprint parity,
or the separate log/publication-memory vertical.

## Source, reference and contract mapping

All reference symbols below were read with revision-qualified `git show` or
`git grep` in read-only `C:\github\lxr-reference\mmtk-core`, at core
`304ce69d43aae87de501111fceb8cbd33173a03a`. Binding
`0682434ae725787e38defbc17bd66dc918e4bdb7:mmtk/Cargo.toml` pins that exact core.
The checked-out tree, simplified implementation and PLDI comparison are not
the oracle. Paper references mean extended arXiv:2210.17175, 1 November 2022,
SHA256 `7240a23970550d3fe4551a5d987a11cbc32cb2ebd28cfa5c68c4a1a95bd270fe`.
Sections 3.2.1, 3.2.2 and 3.3 require increments-before-decrements,
snapshot/reader retention and mark-and-scan-before-death; they do not specify
CoreCLR short/long weak, dependent, critical-finalizer or utility-thread semantics.

| Group / exact pinned source and symbol | Current gap / bounded translation | .NET contract, invariant and checks |
|---|---|---|
| Root snapshots: `src/plan/lxr/global.rs::process_prev_roots`, `release`; `cm.rs::LXRStopTheWorldProcessEdges::process_slots`; paper 3.2 | Existing copied root banks and nontracing lifecycle leases already implement this mechanism. Qualify, do not replace with all-target tracing roots. | `gcscan.cpp::GcScanHandles`, `objecthandle.cpp::PromoteRefCounted`, profiler callback renewal and actual owning-slot relocation. Old root leases retire before address reuse. Three movement modes and existing source-incarnation/native-reader tests remain required. |
| Weak/dependent/finalization: `global.rs::schedule_concurrent_marking_final_pause`, `schedule_emergency_full_heap_collection`; `cm.rs::LXRWeakRefProcessEdges`; `scheduler/scheduler.rs::schedule_ref_proc_work` | Existing Full/FinalMark weak/dependent closure is not an empty P7 implementation. Audit exact stage ordering and one-collection fixed points. | `gcscan.cpp::GcShortWeakPtrScan/GcWeakPtrScan/GcDhInitialScan`; `objecthandle.cpp::Ref_ScanDependentHandlesForPromotion` owns callback slot validity and its internal fixed point. Short weak clearing precedes f-reachability; dependent closure repeats after promotion; long weak/SyncBlock cleanup follows it. |
| Finalizer discovery: `src/util/finalizable_processor.rs::FinalizableProcessor::scan/forward_finalizable` | Host `QueueFinalizer` currently marks each candidate immediately. That can mark a second initially unreachable finalizable object before its discovery. Test before repair; separate complete discovery from pending-root promotion if reproduced. | `src/coreclr/gc/finalization.cpp::CFinalize::ScanForFinalization` first moves all unreachable candidates, then `GcScanRoots` promotes f-reachable objects. Preserve eager finalization, suppression, normal-before-critical delivery and real EE finalizer-thread waits. Do not invent normal-finalizer order. |
| Execution ownership: `src/scheduler/worker.rs::GCWorker::run`, `drop(work)`; paper Figure 2 | Host `RunScan` creates a utility thread for every operation and busy-yields until a stack work pointer completes. This is a candidate cost, not an attributed bottleneck yet. Existing RC scheduler workers are ordinary `std::thread`, not automatically registered EE scan threads. | `vm/gcenv.ee.cpp::CreateNonSuspendableThread` establishes `ThreadType_GC`, stress-log state and startup readiness; its utility-thread handle is closed by the EE. Reuse must retain that contract, fresh `ScanContext`, serialized publication, operation completion and shutdown without dangling stack work. No worker pool is preselected. |

## Independent baseline preservation

The authorized ancestry-only fast-forward corrected stale app HEAD `17ac93d`
to exact local `refs/heads/lxr == 99faa6a`; no other ref was advanced.
The 321-byte XML converged to committed bytes with unchanged SHA256
`63c1d0e88ee63036ebdfeeb0be740e00a9e0b1aa2c86fcf8e816c3e182253e31`.
The known589-byte C# file was not edited and its final bytes match
`9d643c1e46daabae92f3b7a5ffa6bae5e260c4c542120d1089c8b14c24e371e5`;
only the XML was individually hashed before the initial fast-forward.

Before product edits, all 61 source inputs and all 4352 original framework
file bindings matched. Independently copied Debug/Release frameworks are
`artifacts/P7-lifecycle/baseline-{Release,Debug}-windows` here and
`/root/lxr-lifecycle-cost/artifacts/P7-lifecycle/baseline-{Release,Debug}-linux`.
The copied portable fixture remains
`d3f4cbcc1e80c75a59d4492dc113ce1a6e28b54e1eaebdb4d8553b5e5e164e4e`.
Copy verification explicitly rejects shared file identity/hardlinks.
The exact source archive is `artifacts/P7-lifecycle/baseline-source.tar`,
SHA256 `2a2a19175f1db6f00828d2d44e12d7c26b5d1b784fe3d42bf5c4496fd674e305`.
Preservation JSONs bind all actual copied files.

Both OSes completed all 28 original selected-host baseline children, including
100631 lifetime checks in every movement mode/configuration. Linux's surrounding
shell returned 2 because WSL argument transport lost its final exit variable;
all 28 persisted child rows were independently checked as exit 0 with nonzero
counts. This wrapper failure is not relabelled a passing shell invocation.
Windows Debug/Release native replays pass original30979, trace8358,
mature3324, reader47, completed-descriptor45, byte-wrap1375 and nursery-reader48.

Initial WSL path conversion and native-manifest path parsing stopped before
execution; corrected bounded invocations are separate. The first Windows
configure wrapper lost `=1` from its CMake argument and stopped before
configuration. Original tool outputs/logs remain failures, not collector results.

## Scope boundaries

No new copying mechanism, VM/JIT/EE/public managed API, admission/log bound,
allocator pool, backing redesign, platform expansion, public GitHub action,
push or merge-back is authorized. Required fresh reuse events and metadata-live
owner/exact-field remembered-slot validation remain load-bearing. The literal
reference reservation-unit limitation is preserved. Full P9 convergence,
production stress and flat-stream memory redesign remain future work.

## Minimal reader test-contract seal (before execution-cost edits)

The first preserved Linux Release native executable
`0047602e70d0c76b4d821a92def6ecfc5fcb8c00d998480937834083f5bf8d14`
fails **46/47** at the actual active-descriptor range/user receipt. All 56
native source bindings were separately verified. Its retained failure is
`/root/lxr-lifecycle-cost/artifacts/P7-lifecycle/baseline-native-Release-linux/mature-reader-replay.log`,
SHA256 `dbe90f0f4d819c57b60c370752047cff570c21edc3cba54b15eccc5a50eb8704`.
This is not a flakiness waiver or retrospectively a traced execution schedule.

The old test's happens-before assumption is demonstrably unsound:
`RunPause` clears the real scan checkpoint before `StartPause` closes resumable
work admission. The released scanner may finish its continuation, after which
the suspended `RetireCompletedWork` correctly removes the completed descriptor.
The new **test-only** `MatureReaderCompletion` counterexample forces this
permitted completion window and retains the old missing-active-receipt failure:
source/destination/range/users are zero, while payload, once-only packet
consumption and both actual source-address replacements still pass. This
demonstrates the unsound premise, not the exact uninstrumented failing schedule.

The positive test now calls the existing `HoldTracePacketsForTest(true)` while
the first actual slice is still checkpointed, **before** `FinishTrace` releases
it. The next actual continuation is constructed but not published; its real
descriptor user survives through copying. No assertion was conditionalized or
weakened in the positive test. Debug/Release on both OSes pass **47/47**, with
exact source/destination/range and nonzero users printed. Existing
completed-descriptor **45/45** remains a separate natural-completion lane.
The unchanged `MatureScan` omitted-range mechanism fails **46/47** at the same
receipt with users=1 and the range still in old source storage on both OSes
and configurations. The forced-completion old-assumption counterexample also
fails **46/47** in all four configurations.

Exact commands/build/input manifests and raw logs are under
`artifacts/P7-lifecycle/reader-seal-{Release,Debug}-windows{,-omitted,-completion-control}`
and `/root/lxr-lifecycle-cost/artifacts/P7-lifecycle/reader-seal-{Release,Debug}-linux-{None,MatureScan,MatureReaderCompletion}`.
No product scan/relocation/scheduling semantics changed: the forced-completion
code is compiled only under both `LXR_SIDE_METADATA_TEST_PLATFORM` and its
explicit control macro. This minimal test baseline is frozen separately
before any performance change.

## Decisive finalization discovery regression

Portable fixture2 SHA256
`2b5e7865bee71a235d903beb3165166c3e5342bef12875e58a0b68408a4121ab`
creates a parent and finalizable child in a NoInlining helper and returns only
long weak references. Both objects are initially unreachable; the first
completed full collection plus finalizer wait must discover both. The test
does not request another collection before checking delivery, nor assert
ordinary-finalizer ordering. All six accepted LXR mode/configuration arms per
OS fail the single-collection discovery assertion. Same-runtime WKS and Server
pass in Debug/Release on both OSes with the same fixture. Existing isolated
weak/finalizer and strengthened reverse32-dependent closure pass the baseline.
All 44 process outcomes, including the twelve LXR failures, remain in
`baseline-new-lifecycle-{windows,linux}`.

The bounded product remedy separates complete unreachable-candidate discovery
from pending-finalizer graph tracing, exactly as CoreCLR
`CFinalize::ScanForFinalization` and pinned MMTk `FinalizableProcessor::scan`
do. The second walk runs only when pending finalizers exist. It retains
short-weak clearing before discovery, dependent closure after promotion and
long-weak/SyncBlock cleanup afterward. No ordinary finalizer order is added.

The correctness-only `closure1` stage passes all 22 affected lifecycle/category
processes and all 28 original accepted-lifetime processes **per OS**, including
100631 checks in every movement mode/configuration. No performance edits are
in that stage. Collector SHA256:

| Configuration | Windows | Linux |
|---|---|---|
| Release | `d9cfc556172be9685c27814751bc4a09ca660136e6c3beb178f8db5ce4807a65` | `a24f67ff17569eaa45b16103b0315e61c4ecc83826c2cb513e2ad0d99948da9c` |
| Debug | `7991edf611da673f4bbc758cd3561ce24f0b8ba77febd405fbf1d5a36efe6fd6` | `10e44984cd766a63a600c1946c90dab1fd044994d9b8d45a9e90c7f4a9281661` |

These independent frameworks and their logs are retained under
`artifacts/P7-lifecycle/closure1-{Release,Debug}-windows` and the corresponding
Linux artifact root. The initial Release-only runner invocation retained its
11 completed successes before stopping on the not-yet-created Debug path;
the complete 22-process matrix is separately named `closure1-final-lifecycle`.
Product/source ownership and the pinned finalizer-discovery order were rechecked
after the diff. The old accepted finalizer-heavy timings cannot be an
equivalent-work performance denominator: subsequent execution-cost work must
compare this correctness-fixed work against the same work plus optimization.

## Attribution before choosing an execution change

`attribution1` adds opt-in scan scopes and counters on the correctness-fixed
collector, not a performance optimization. `run-cost.py --phase attribution`
predeclares 63 processes per OS: empty-ish repeated full collections, 4096
real strong-handle roots with child edges, and eight cohorts with 128 actual
finalizers plus 64 real dependent handles each. Each measured region contains
exactly eight explicit blocking full requests. The finalizer input requires
all 1024 finalizers to execute and post-f-reachability dependent closure to
complete in the same respective collection. Fixed checksums are 8, 67092480
and 4608. WKS and Server use the same fixture/runtime. Every process is retained.

Normal runs disable diagnostics; diagnostic runs separately read actual
current-thread CPU using GetThreadTimes/clock_gettime and wall ticks. Metric
118+i is scan wall/count i; 162+i is its current-thread CPU nanoseconds, for
44 entries. Kinds 0..3 counts, 4..7 inclusive dispatch, 8 EE creation/naming/
readiness, 9 caller wait, 10..13 execution by kind, 14 finalizer lease/pin walk,
15 roots, 16..19 handle/lease passes, 20 frozen roots, 21 dependent closure,
22 short weak, 23 finalizer discovery, 24 pending-finalizer trace, 25 dependent
closure after promotion, 26 long weak/dependent clearing, 27 SyncBlock cleanup,
28..29 relocation, 30 statistics, 31 Before/After/promotions notifications;
32..40 work counters and
42 yield iterations. Counter entries do not have meaningful CPU durations.
These scopes are nested and run on different actors: **do not sum them as
exclusive GC CPU**. Windows CPU is heavily quantized, with zero small-scope
deltas and larger caller-creation deltas; it is not zero worker work.
Native-thread OS retirement after the EE callback returns is not separately
timed by this existing no-handle-return interface.
Counter32 records initialization rounds plus successful external rescans,
not every external call or CoreCLR's internal table-loop passes. The latter
are unmeasured, not zero. Diagnostic fixture6 emits null for reserved wall/
counter entries41/43 and all non-duration CPU entries; older exploratory
fixture3 arrays contained zero padding there. `measurement-limits.json`
defines these distinctions and the unmeasured startup/retirement boundaries.

Diagnostic common costs over eight collections (single intrusive observations,
not additive shares or performance distributions):

| OS / nonmoving input | Region wall / process CPU / STW ms | 48 scan-thread creations wall ms | Caller busy-wait wall / CPU ms | Scan body wall ms, kinds 0/1/2/3 |
|---|---|---:|---|---|
| Windows empty | 70.477 / 62.5 / 55.598 | 8.993 | 5.134 / quantized0 | 1.806 / 1.257 / 1.957 / 0.003 |
| Windows roots | 136.132 / 203.125 / 103.495 | 10.155 | 9.068 / quantized0 | 4.027 / 1.921 / 3.038 / 0.006 |
| Windows lifetimes | 69.371 / 62.5 / 55.911 | 8.585 | 7.786 / quantized0 | 1.659 / 4.105 / 1.955 / 0.004 |
| Linux empty | 95.582 / 113.971 / 73.441 | 15.846 | 5.381 / 5.396 | 1.401 / 2.016 / 1.871 / 0.004 |
| Linux roots | 167.113 / 224.217 / 132.416 | 16.329 | 12.535 / 12.552 | 3.885 / 3.545 / 5.005 / 0.006 |
| Linux lifetimes | 97.861 / 123.599 / 77.323 | 14.118 | 7.160 / 7.175 | 1.398 / 3.691 / 1.947 / 0.003 |

All three modes execute 8 root, 8 weak/lifetime, 8 epoch and 24 relocation
operations: 48 new EE utility threads per eight full requests. Moving modes
also retain creation/busy-wait costs: Linux root-heavy mature creation/wait
21.404/21.541 ms; Windows 11.457/16.200 ms. Thus accidental dispatch cost is
present even without copying, but does not explain the entire nonmoving gap.
The native RC/trace/metadata work remains material and is not promised away.

Instrumentation-off/previous correctness-only observations and diagnostic
on/off comparisons remain in the 63-row records. They are noisy and too small
to certify a small overhead percentage; the instrumentation is not enabled
in primary timed arms. No baseline processes were retried. Injected delays
are retained. Primary comparison will use **attribution1 off versus changed
off**, with identical fixture and required work; closure1 remains a separate
uninstrumented correctness-only characterization.

### Bounded execution translation and predeclared goal

The measured candidate is one reusable scan executor created by the **existing
EE CreateThread**, not ordinary scheduler-thread reuse or a pool. The pinned
`GCWorker::run` owns registered worker identity, per-operation work and drops
completed work; CoreCLR `CreateNonSuspendableThread` is authoritative for
GC-special registration, stress-log and utility-thread startup/naming. The
port will retain all 48 operations and fresh local ScanContexts, existing
Before/After/promotions callbacks and exact lifecycle order, with serialized
release/acquire handoff, blocking event waits and no borrowed stack work.
Stop/exit handshakes must follow collection serialization and native-worker
drain; startup/operation/exit failures must surface, never return false success.
No log, allocation, liveness or pinning bounds change.

**Goal frozen after attribution, before changed measurements:** at least 10%
lower normal median full-region wall and STW for nonmoving empty and lifetime
inputs on each OS. Linux process CPU must also improve in both primary inputs.
Windows short CPU results are reported with clock quantization rather than a
precision target. All other modes, root-heavy and four unchanged aged mature
inputs are measured/reported, including any regressions; this is not a promise
of 10% everywhere or P9 parity. The matched convergence schedule predeclares
four normal observations (two interleaved A/B pairs), one injected and one
diagnostic per arm/input, all three modes plus WKS/Server. No tuning, dropped
processes, retries or widened targets are permitted.

### Exploratory first changed Windows result (goal not widened)

**The integrated review subsequently found the null-secondary dependent lease
hole below. All 336 Windows and 336 Linux reuse1 comparison rows remain
exploratory, not final equivalent-correct-work qualification.** A new denominator
must include both lifecycle repairs. This does not erase the first missed
Windows goals, and the same10% target remains in force.

The declared 336-process Windows schedule completed without retries.
Normal medians, four observations/arm/input, fixed attribution1 versus reuse1:

| Input / mode | Wall fixed / reuse ms | Wall change | STW fixed / reuse ms | STW change |
|---|---|---:|---|---:|
| Empty / nonmoving | 63.553 / 57.400 | -9.68% | 49.916 / 43.617 | -12.62% |
| Lifetimes / nonmoving | 66.993 / 60.708 | -9.38% | 53.668 / 46.977 | -12.47% |
| Roots / nonmoving | 115.200 / 113.049 | -1.87% | 88.890 / 86.162 | -3.07% |
| Empty / nursery | 66.888 / 58.340 | -12.78% | 53.349 / 44.348 | -16.87% |
| Lifetimes / nursery | 70.873 / 65.325 | -7.83% | 57.784 / 51.615 | -10.68% |
| Empty / mature | 86.642 / 80.410 | -7.19% | 73.200 / 65.698 | -10.25% |
| Lifetimes / mature | 93.692 / 86.742 | -7.42% | 80.145 / 72.388 | -9.68% |

**The Windows 10% primary wall goal misses on both inputs; STW passes.**
The target remains unchanged and the missed result is not retried or rounded
into success. Empty process CPU is unchanged at a quantized62.5 ms median;
lifetime CPU falls85.9375->62.5 ms. Root-heavy nonmoving wall is a small effect,
not a broad throughput win. All other mode/input distributions remain in raw
rows, including the unchanged aged fragmented/dense/pinned/high-survival inputs.
The default mature study's nonmoving wall changes are -12.19/-7.88/-12.03/-8.47%;
mature changes are -4.76/-7.55/-8.50/-5.43%.

Diagnostic fixed/current lifetime nonmoving retain exactly
8/8/8/24 operations, 1024 delivered finalizers, 512 dependent promotion callbacks,
376 ordinary root callbacks and 809 weak/lease callbacks. Region utility-thread
creations48->0 and caller yields82281->0. Process thread count is constant
12->12 in fixed and13->13 in reuse: one persistent registered worker is an
intentional resource, not a zero-thread claim. Idle100ms process CPU is0 in
these observations, at Windows clock resolution.
Total ordinary RC/trace/object-walk counts differ slightly: e.g. root
increments2793->2801 and traced objects23928->23944. The Process.Threads
diagnostic materializes runtime thread-info objects for the extra persistent
worker, but exact attribution of those additional objects is not proved.
These actual differences are retained, not falsely reported as identical
collector-wide runtime work. Required workload roots,
dependent/finalizer work, checksums and explicit requests are unchanged.

## Review finding: null-secondary dependent lifetime

The one integrated independent review found that the profiler dependent
enumerator calls its callback only when **both** primary and secondary are
non-null (`objecthandle.cpp::TraceDependentHandle`). A dependent handle with
a null secondary therefore had no nontracing primary lease. RC could physically
reclaim the primary while the uncleared native handle still exposed it. A
subsequent Full hid the interval by clearing the handle.

A NoInlining producer now creates `DependentHandle(byte[65536], null)` and
returns only that native-handle struct and an integer address. A temporary
normal handle arms the existing non-rooting identity probe and is freed before
return. Both OSes' reuse1 modules fail immediately after the first RC-only
collection, with original reclaims=1, before any target getter or Full.
Fixture4 is `c6346dac95f77806e46fbd0e1fb21dbeec9ba52224eabce4c1122573d3389c61`.
Logs are `null-secondary-before-{windows,linux}/Release-nonmoving-dependent-null.log`.
The failure is original-allocation retirement, not an extent/reused-address
guess or a broken apparatus.

The surgical translation uses existing
`Ref_ScanDependentHandlesForRelocation` as a **nonmutating owning-slot visitor**
while suspended, with `promotion=false` for that pass. `UpdateDependentHandle`
visits both actual owner slots even with a null secondary; the callback copies
only their current values into `AddRoot(..., trace=false)`. No slot address is
retained or written. The filtered profiler dependent pass is replaced, not
added; the general profiler handle pass explicitly excludes dependent types,
so this does not double-lease dependent pairs. Null values remain no-ops.
The bounded review follow-up checked this existing-interface use against the
actual implementation; no simpler complete dependent-only enumerator exists.
No VM/JIT/EE interface or strong/permanent tracing lease was introduced.

The test is two-sided: four RC-only collections must preserve the original
identity; one Full weak/dependent cleanup must clear/reclaim it; a real
same-address replacement must then survive old-root retirement. The watched
birth counter counts births **after arming**, not the original earlier birth;
the replacement proof therefore requires at least one observed subsequent
birth plus the exact actual handle address and independent payload identity.
The non-null reverse chain, secondary replacement/nulling, primary clearing,
post-finalization dependent closure and unmarked-primary death remain tested.

`corrected2` freezes both lifecycle repairs **without** executor reuse.
Only the isolated, committed executor patch was temporarily unapplied, after
checking for overlap; source snapshots and its patch are retained, and the
optimized source is restored byte-for-byte before building the new candidate.
No reset/stash/rebase/cherry-pick/clean, prior baseline mutation or other-ref
movement is involved. Its product difference from attribution1 is just the
complete owning-slot dependent lease. The same10% primary wall/STW goal
applies unchanged to corrected2 versus the corrected reusable executor.

### Observable behavior change, local prototype only

Previously, one collection could progressively discover only part of an
initially unreachable finalizable graph, and null-secondary dependent handles
could expose reclaimed primaries between RC and Full. The selected prototype
now discovers the entire eligible finalizer batch before f-reachability and
keeps every uncleared dependent primary valid until actual weak cleanup.
This is a behavioral correction for `System.GC.Collect`,
`System.GC.WaitForPendingFinalizers` and `System.Runtime.DependentHandle`
observations; it changes no managed API or source/binary signature. Do not
depend on ordinary-finalizer order or restore the unsafe old lease behavior.
The collector remains default-off and no shipping .NET release is assigned.
The breaking-change documentation skill's merged-PR publishing workflow is
not applicable to this unsubmitted local prototype; no public issue/comment
or release-version claim is made.

## Final corrected-work result

The product implementation is frozen at
`564c76e7020370be8d01e06e048417b1856e13f7`: both lifecycle fixes plus reusable
execution. Later commits only add read-only allocator diagnostics, strengthen
a dependent-cycle test, or record evidence. The final **corrected2 versus
final2** comparison uses the same immutable fixture6 on both arms and both
OSes, after both lifecycle repairs. It preserves eight explicit full requests,
fixed inputs/checksums and required same-cycle work.

Each OS completes its entire predeclared336-process schedule: seven inputs,
three baseline/current LXR mode pairs plus WKS/Server, four interleaved normal
observations, one injected and one separately intrusive diagnostic per group.
That is672 retained final observations/112 groups, not a selected subset.
Windows and Linux studies ran separately after compiler/correctness work
finished. The earlier126 attribution and672 exploratory observations remain
separately identified, never substituted for final rows.

Primary nonmoving medians, milliseconds, n=4:

| OS / input | Fixed wall | Reuse wall | Change | Fixed STW | Reuse STW | Change | Fixed / reuse process CPU |
|---|---:|---:|---:|---:|---:|---:|---|
| Windows empty | 60.8194 | 54.0536 | -11.12% | 49.4175 | 42.7273 | -13.54% | 78.125 /62.5 |
| Windows lifetimes | 65.8007 | 58.4278 | -11.21% | 52.0989 | 45.3705 | -12.91% | 78.125 /62.5 |
| Linux empty | 104.2025 | 86.3944 | -17.09% | 82.9745 | 66.8668 | -19.41% | 123.039 /97.4525 |
| Linux lifetimes | 100.9569 | 88.9710 | -11.87% | 79.5882 | 68.5257 | -13.90% | 120.0865 /101.778 |

**The unchanged10% wall/STW goal passes for these four corrected-work groups;
Linux CPU improves in both.** This does not erase the first Windows misses.
Region wall includes the fixed checks and finalizer waits; process CPU is
all-thread CPU during that region, not exclusive GC CPU. Dividing region time
by eight is per-completed-request region cost, not pure GC or request-tail
latency. Actual eight-call duration arrays and full process wall including
startup/setup/shutdown are retained separately.

Other mode/input results are in the complete CSVs. Nursery primary wall changes
are -12.95/-11.73% Windows and -9.44/-7.48% Linux; mature primary changes
-10.01/-8.33% and -10.55/-11.22%. Root-heavy nonmoving wall changes are only
-5.64/-7.28%, and mature root-heavy changes -3.62/-7.95%.
For unchanged default fragmented/dense/pinned/high-survival inputs, mature
wall changes are -6.61/-3.61/-10.31/-8.15% Windows and
-6.22/-6.34/-10.97/-4.95% Linux. These are point estimates, not uniform wins
or small-effect sensitivity claims. The Linux high-survival mature current
A1 sample254.5296 ms remains in the four-sample population.

WKS/Server remain much faster on identical runtime/fixture inputs: Windows
empty2.1543/2.7509 ms and lifetimes3.1366/3.5865 ms versus LXR54.0536/58.4278.
Built-ins are characterization, not equal-heap fairness or P9 parity.
Every A/A difference, injected effect, sample and mode-specific CPU/STW/memory
value is retained. No row, outlier or failed attempt was dropped; no threshold
was widened or allocator/configuration tuned.

The final Windows lifetime diagnostic preserves8/8/8/24 operations,
1024 delivered finalizers,512 dependent promotions,376 ordinary root callbacks
and1321 weak/owning-slot callbacks in both arms. Region utility-thread
creations48->0 and yields76257->0. The new executor performs no work while
idle except waiting on its event; its one persistent thread is explicitly
observed rather than hidden. Initial one-time executor creation occurs during
initialization, outside the steady-state region; parent full-process wall
prevents pretending that startup is free.

## Native retention remains unresolved

Six separately declared intrusive memory processes per OS use the same
fixture6, modes, input and runtime for corrected2/final2. Actual thread counts
are sampled from the owned process when phase output arrives; this is not an
exact print-instruction barrier or a process-wide worker census.

The fixed1240-array growth input still has checksum306460, five growth steps,
402653184-byte reservation,134217728-byte admission,109215744-byte managed
commitment and **774049792-byte native-plane commitment** in both arms/OSes.
The four flat reference/publication streams still commit640 MiB at128 MiB
admission. They were not shrunk, resized, pooled or relabelled as residency.
Linux growth RSS417857536->417460224 is not a memory-win distribution; its
single post-full116.833->109.952 ms observations do not erase prior scratch
decommit/recommit regressions.

Pointer-rich quiescence demonstrates why logical frees are not OS release:

| OS / fixture6 observation | Corrected2 | Final2 |
|---|---:|---:|
| Windows RSS | 137904128 | 138002432 |
| Windows private bytes | 252850176 | 254095360 |
| Linux RSS | 348045312 | 540856320 |
| Linux private bytes | 577466368 | 812433408 |
| Both / tracked Work and TraceScan bytes | 0 /0 | 0 /0 |
| Both / Work high water | 100953752 Linux /101316296 Windows | same |
| Both / TraceScan high water | 26870016 Linux /26878208 Windows | same |

**The Linux fixture6 observation regresses RSS by192811008 bytes. It is retained,
not excused or replaced.** Quiescent thread observations are12/13 Windows
and13/14 Linux, baseline/current; growth samples have the same respective
counts. Live-phase transient counts are also retained.

A separate read-only glibc `malloc_info` diagnostic uses fixture7
`a4b81bc8b92c08808d7f893c83e7ffa62394786828065c06d7500d312934a930`,
with no trim, allocator setting, new pool or collector change. Relevant
MALLOC_/GLIBC_TUNABLES settings were unset. Its quiescent direction differs:
corrected2 RSS662724608 versus final2 RSS476532736. This is **not a replacement
or a selected lower result**. Their glibc XML exposes14 arenas each, current
system heap bytes531931136/345911296 and free “rest” bytes526156221/340020792,
despite collector-owned Work/TraceScan bytes0 and identical100953752-byte
Work/26870016-byte scan high water.

These observations demonstrate substantial variable shared-allocator physical
retention, not a hidden new collector pool or proof that worker reuse releases
memory. Exact arena-to-worker ownership and OS release remain unmeasured.
No footprint improvement/parity is claimed. The original Linux fixture6
regression and all earlier P5/P9/P6 retention limits remain visible. The
two allocator queries are diagnostic-only and do not change final timed modules.

## Final correctness and identities

After the last collector edit, each OS passes64 fixture6 lifecycle/category
processes and28 full selected-host acceptance processes. The latter has
100632 checks in every movement mode/configuration, plus OOM13, birth32,
source-reuse8, nursery/predictor27, default35, mature/native-pin155 and
retention1245/524292. The extra check is real released-ComWrappers target
death, not an assertion-count relabelling.

The64 include short/long weak and resurrection73, reverse dependent closure/
mutation139, real weak identity/address-replacement13, null-secondary primary20,
finalizer re-registration5, collectible GC/non-GC static weak-interior owners10,
refcounted renewal/release15,2048-finalizer churn1050, and actual finalizer-thread
nested collection/wait3. Finalizer closure5 is checked against same-runtime
WKS/Server4. Ordinary finalizer order is never asserted.

Fixture8 additionally closes the reverse dependent chain with an ordinary back
edge and checks that actual mixed cycle, mutation and death in a single
collection. Both execution implementations pass139 in all modes/configurations
on both OSes after that final test edit. It is
`fc5ab8cff9c4faebe104ef4df16ac485c64f9dea84e97d39ba23998a17b433a4`,
source `33820852bb827baf84a0b6a2d16984fdf821b8f7`; it does not replace fixture6
in the final timing/correctness matrix. The old null-primary omission is also
replayed with exact source-bound fixture6 on both OSes, failing before Full at
original reclaims=1. Historical uncommitted fixture drafts remain separately
labelled; retained DLL/PDB bytes are not presented as retained full source.

Final native Debug/Release on each OS pass original30979, trace8358,
mature3324, reader47, retained-owner22, incarnation117, completed-descriptor45,
wrap1375, whole532, same-shape1376, current-field boundaries5505,
nursery-reader48, root batching6170 and auxiliary retirement12307.
Omitted field-shape controls fail1372/1375 at actual primitive-payload/replay
receipts; omitted-range controls fail46/47 with real descriptor users retained.
Earlier forced-completion counterexamples remain separate from those controls.

The one integrated review found the null-secondary hole; its bounded follow-up
verified the existing owning-slot seam. The implementation diff was rechecked
for both actual slots, null handling, no writes/slot retention and no duplicate
dependent leasing. The later diagnostic helper was built/run independently,
and final cycle assertions were rerun. Creation/readiness/operation/exit fault
injection, arbitrary hosting races and production stress were not performed.

| Collector | Corrected2 (both fixes, no reuse) | Final2 |
|---|---|---|
| Windows Release | `00bb970fa6aa3681e0db973ada1127c52fb1475b6ce382e73e41117c853ab6ac` | `412052ed370649e96254b6183adf61fbba14ae712122c64cc07cd806f9f0a53e` |
| Windows Debug | `b20abd8339cdd0ad157939971ece039a954359bb9c5562454e6bac226468db68` | `3f197b7474bb3f2b4032ab1485de689d036ce8b2229698f93161077784cfa399` |
| Linux Release | `d77b1507b00257f706aaa0da8e94bb5c69103f15b03d0b33abf89bd4f325eab7` | `bf4f9d0a5e671dff1703467abf118e063cb8f1d2f52444e92e00886b54a84e61` |
| Linux Debug | `7f4713050d8ad37baf1c0add31baf6fabd940e4a62cf56f0dd1fce4b27215b21` | `4e01885e12d044e24c12fd33d2c6a5260d16c98947eadfab97fcb206866789f2` |

All63 current source inputs,6528 file bindings across12 canonical accepted/
corrected/final framework roots, fixtures, final results and raw catalogs are
bound by the adjacent CSVs. Bindings are not independent builds/observations.
Runtime/JIT/CoreLib/CoreRun remain accepted bytes. Collector debug companions
in copied frameworks are inherited and are **not** qualified as matching the
new collector; neither symbols nor native @Commit strings establish source
identity. Actual native source snapshots, commands and DLL/SO hashes do.
Portable fixtures were built once per version with the verified Windows SDK
and copied unchanged to Linux.
The working/compiler inputs use CRLF while Git objects are canonical LF.
`fixture-source-bindings.csv` records both hashes and verifies the actual
Program/Nursery compiler-byte hashes against portable-PDB documents, rather
than silently treating Git blob bytes as the compiled inputs.
`native-source-stages.csv` likewise distinguishes working and Git forms and
binds the contemporaneous corrected2 source/patch. The retained SDK host,
dotnet/MSBuild/compiler assemblies and native compiler inputs are separately
recorded; the retained Linux SDK host was verified but not used to build the
portable fixtures.

Representative replay commands from this worktree (use fresh output paths):

```powershell
python docs\design\lxr-port\P7-lifecycle\run-qualification.py --artifacts artifacts\P7-lifecycle --stage final2 --fixture artifacts\P7-lifecycle\fixture6\runtime-tests.dll --output artifacts\P7-lifecycle\replay-lifecycle-windows
python docs\design\lxr-port\P7-lifecycle\run-cost.py --baseline artifacts\P7-lifecycle\corrected2-Release-windows --changed artifacts\P7-lifecycle\final2-Release-windows --fixture artifacts\P7-lifecycle\fixture6\runtime-tests.dll --output artifacts\P7-lifecycle\replay-cost-windows --phase convergence
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -Configuration Release -OutputDirectory artifacts\P7-lifecycle\replay-native-windows
artifacts\P7-lifecycle\replay-native-windows\lxr-rc-validation.exe --mature-reader
artifacts\P7-lifecycle\replay-native-windows\lxr-rc-validation.exe --mature-remset-wrap
```

Linux uses `/root/lxr-lifecycle-cost`, corresponding `-linux` roots, `corerun`
and the existing native `.sh` runner. Actual configure/build commands, settings,
fixture/module hashes, failures and every child output remain under each
`artifacts/P7-lifecycle` root. Only the collector target was incrementally built
after one configuration per OS/configuration; no full CLR/CoreLib/JIT/libs
matrix, dependency change, audit disabling, public action, push or merge-back
was performed.

## Independent acceptance seal

Before the final record commit, the coordinator independently passed92 positive
processes on **each** OS:76 lifecycle processes including the mixed-cycle
qualifier on both corrected/reusable implementations, six three-mode100632
full-suite processes and10 native processes. Three isolated negatives per OS
reproduced actual old-null-primary RC death, field-shape1372/1375 and range46/47
seams. These184 positives and six expected failures are acceptance replays,
not additional timing samples.

It verified all63 physical inputs per platform,6528 framework bindings,
retained fixtures, common/platform record indices and all1308 Windows/1252
Linux catalog files before/after execution. It independently recomputed all672
final rows/112 groups and the unchanged wall/STW/Linux-CPU goals, and inspected
executor handoff/fail-fast and required lifecycle ordering. Its checker
explicitly distinguishes1024 workload finalizers from the Linux global delivery
counter1048, which includes runtime finalizers and matches both arms.
No remaining material finding or new study/rebuild was requested. The plain
Linux RSS regression, separate allocator diagnostic, earlier misses and all
unmeasured boundaries remain part of acceptance, not conformance or memory wins.
