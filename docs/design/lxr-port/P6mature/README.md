# Pin-aware mature evacuation vertical

Implementation `daa247720d464da9a3fdd48d398fd8b0ae267733`, on exact accepted
nursery `5eb593a902d808082a31c36ddde41ba16b2b3256`.

This integrated Windows/Linux x64 standalone CoreCLR vertical implements mature
selection, remembered-set replay, positive-RC relocation, source-block release,
required .NET pin/interior adaptations and reference pressure/headroom accounting.
The collector remains a default-off build/GCPath selection, not WKS/Server
delegation. It is not production qualification, complete P7/P9, P10, DAC
qualification or throughput/RSS parity.

**Actual whole source blocks are released and subsequently allocated again.
Copying nevertheless regresses the measured default-allocation workloads.**
Native-plane commitment and Linux shared-allocator retention remain unresolved.

## Reference binding and per-group mapping

The sole coupled oracle is binding
`0682434ae725787e38defbc17bd66dc918e4bdb7`, whose revision-qualified
`mmtk/Cargo.toml` pins core
`304ce69d43aae87de501111fceb8cbd33173a03a`. Reads used `git show`/`git grep` in
read-only `C:\github\lxr-reference\mmtk-core`, never its checked-out `9625c174`.
Historical PLDI binding `abbdd1dbbe7277e4b0dfd1d37ba613c53f5ca50d` /
core `df8d30a39237a5bf5a8e27ca5a6f46acc6080c94` is comparison only.
Paper sections below mean extended arXiv:2210.17175, 1 November 2022, SHA256
`7240a23970550d3fe4551a5d987a11cbc32cb2ebd28cfa5c68c4a1a95bd270fe`.

Every substantive group was preceded by actual pinned implementation/paper
reads and followed by a diff check. Exact symbols below are at core `304ce69d`.

| Group / exact oracle and paper | Port gap and translation | Preserved invariant, .NET adaptation and decisive check |
|---|---|---|
| P6.3 selection: `src/policy/immix/rc_work.rs::SelectDefragBlocks`, `MatureEvacuationSet::select_mature_evacuation_candidates`; `defrag.rs::defrag_headroom_pages`; 3.3 | Select before SATB, prioritize sparse 4 MiB chunks, then high dead-line scores; 32% chunk threshold doubles for Full and caps at 100%; ordinary fragmented blocks require half their lines dead, Full admits dense blocks. Preserve the reference half-live-line copy estimate and Full versus InitialMark budget distinction. | The owned contiguous arena supplies actual block occupancy rather than pretending to use MMTk's page resource. Only admitted Immix storage is usable copy space; budgets cannot borrow separately reserved LOS pages. Native3324 includes positive prior-RC survivors, sparse cycles, exact old-address reuse, pins and real partial exhaustion. |
| P6.4 recording: `src/plan/lxr/remset.rs::MatureEvecRemSet::record`, `RemSetEntry::encode`; `cm.rs::scan_and_enqueue/trace_slice`; `rc.rs::record_mature_evac_remset`; 3.3/3.4 | Real slot/tag packets at existing trace, promotion/increment and death-scan events; byte tags at Immix-line and LOS-page granularity. The mutator barrier remains unchanged. | GC-side publication reserves slots separately from release/acquire publication and detects holes. Necessary retained-owner delta: also remember selected-to-selected fields, because pinned or failed owners need repair without moving. Fully scanned late-pin and actual exhaustion owners fail20/22 before, pass22/22 after. |
| Replay safety: `mature_evac.rs::address_is_valid_oop_slot/process_slot`; `line.rs::inc_reuse_counts`; 3.3 | Cheap tag/state checks precede slot payload. Two explicit, coordinator-approved safety deviations are required: fresh CM/FinalMark spans also advance existing line tags; replay proves a current metadata-live owner and exact current GCDesc reference field before dereference. | Literal HEAD omits the fresh event and its byte tag can wrap. Real whole-block replacement fails527/529 before the event extension, passes532/532 finally. Real256 same-line reuses fail1371/1373 with tag-only replay, pass1375/1375 with shape validation. Same-shape duplicates pass1376, exact RC2 and single copies; live header/MethodTable/wrong-interior-field and poisoned dead-owner cases pass5505. No wider identity, dense identity plane, stronger barrier or new pressure policy was added. |
| Mature movement: `immixspace.rs::trace_forward_rc_mature_object`; `src/util/object_forwarding.rs::attempt_to_forward/try_forward_object`; 3.3 | Ordinary STW closure claims the existing negotiated source header, copies the aligned complete extent, transfers the full RC including sticky state, publishes destination metadata/flags/header, repairs fields and follows forwarding aliases. | Object* and physical Object*-8 layout, minimum24 and inclusive LOS32768 remain. The first source payload word carries forwarding while MethodTable/synchronization state survives. Copy-off/nursery-only/mature modes share source. Managed155 includes genuine aged movement, hash, inflated monitor, live interior writes and real native memcpy consumption of16 pinned buffers across four collections. |
| Scoped roots and lifetimes: `global.rs::prev_roots/curr_roots/release`; `cm.rs` owned queues; pinning oracle **none**; 3.2/3.3 | Rebase copied root/decrement/SATB targets and owned packet readers while suspended; rewrite actual fresh EE root/handle callbacks only while their slots are valid. Transfer native flags/finalizer cursor state; update standard weak/dependent/refcounted/SyncBlock owners through existing relocation scans. | All pins are discovered before first copy; ordinary roots are not blanket-pinned. POH/LOS/native-exposed pins stay stationary, movable interiors retain offsets, frozen read-only outgoing targets are pinned rather than written. LoaderAllocator callback slots remain read-only; actual owning handle relocation supplies updates. Existing full lifetime100631 passes in all three modes and both native configurations/OSes. |
| Interrupted readers: `cm.rs::trace_arrays`, scheduler owned vectors; 3.2.2/Figure2 | Rebase real active TraceScan object/range/iterator state without moving descriptors or relaxing explicit users/drain. Completed descriptors finish without consulting dead/reused former owners; active starts/extents are checked before header claims. | Mature47 uses an actual checkpointed GCDesc iterator, an actual source-bearing packet, destination-range receipt with outstanding users and exact later reuse of both source addresses. The packet drains during ordinary FinalMark while sources remain valid; unlike nursery48, mature does **not** claim consumption after source reuse. Missing-range rebase fails at the actual receipt. Real completed/dead owner replaced by a larger object over an RC1 continuation interior passes45; old-behavior counterfactual fails. |
| Source release: `MatureEvacuationSet::sweep_mature_evac_candidates`, `block.rs::deinit`; 3.3 | Clear copied source metadata only after incoming references/readers are repaired. Release only actually RC-empty blocks; pins and failed/partial sources remain. Allocator BlockDefrag is authoritative for the current incarnation, and supplemental membership retires at actual release. | Reference no-pinning unconditional release is not safe for .NET and is not copied. A selected block dying during CM and later serving as copy to-space fails115/117 before the incarnation fix, passes117/117 after. Mature release counters now require a real copied source in that incarnation, not mere selected dead-block release. They remain collector-wide repeated release events, not cohort/distinct-block counts. |
| P8 pressure: `global.rs::get_collection_reserved_pages/collection_required`; `defrag.rs::defrag_headroom_pages`; 3.2.1 | Preserve literal survival floor/truncation/doubling and real defrag pages; use actual occupied/admitted capacity separately. Occupied-block and active/quarantined LOS-span totals update at existing acquire/release transitions, eliminating repeated pressure walks. | Nursery blocks consume reserved pages despite their Unallocated mark-state encoding. Actual nursery headroom fails130/131 before, passes131/131 after. Literal reserve/predictor/trigger suite50 passes; corrected-unit counterfactual fails. Nursery prediction consumes nursery copied promotions only, never mature copy/pinned/in-place volume. Forced Full/FinalMark remains truthful/blocking; standard bookkeeping-inclusive hard-limit family remains rejected. |
| P9 observability and protocol | Genuine nursery/mature bytes, failures, pins, selection/replay/invalid-slot/release counters and opt-in allocation/RC-initialization/payload/closure/scan/root ticks; truthful compacting diagnostics include mature movement. Capability-qualified fixture emits null for unsupported/unmeasured surfaces. | Timers are intrusive nested wall-clock scopes, not additive/exclusive GC CPU. Ordinary measurements keep them off. A reviewed protocol amendment preserves the stopped candidate14 zero-hit diagnostic instead of classifying its allocator placement as collector failure. Current-output semantic failures are persisted before rejection; the invalid-output counterfactual verifies this. |

### A literal reference accounting limitation

HEAD `global.rs:299-307` uses `clean_nursery_mb() * ratio`, truncates to usize,
doubles it, then adds actual defrag-headroom **pages**. `clean_nursery_mb()` really
returns MiB and the multiplier is1. Thus64 MiB clean/ratio0.5/headroom0 returns
**64 in the page-count field, not16384**. This apparent unit mismatch is a
preserved shipped-reference limitation, not a numerically correct byte reserve
or a paper formula. Sub-MiB/truncation/nonzero-headroom cases distinguish it.
It is not propagated into unrelated MiB trigger comparisons. Actual admission,
commitment, copy allocation, retained-source fallback and OOM remain independent.

## Defaults, contracts and independent baseline

`DOTNET_LXREvacuateNursery=1` and `DOTNET_LXREvacuateMature=1` match the moving
HEAD defaults **inside the explicitly selected collector**.
`LXRDefragHeadroomPercent=5` (0..50) and `LXRChunkDefragPercent=32` (0..100)
are real decimal controls, including actual runtimeconfig JSON. Both0 toggles
provide the same-source nonmoving comparison; nursery1/mature0 is nursery-only.
No max-pause/timer/huge-page policy was introduced.

The existing1024 simultaneous-mutator,131072 copied-root and256 frozen-record
bounds are unchanged, as are the two1024 ingress chunks, once-per-owned-slot
admission, explicit publication holes, independent RC/SATB reader ownership,
black-newborn/mark-before-death and lazy-completion ordering. Managed admission
and native-plane limits remain explicitly partial; standard GC hard-limit
absolute/percent/SOH/LOH/POH families remain unsupported.

An authorized ancestry-only fast-forward corrected the stale app checkout
`17ac93d` to exact local `refs/heads/lxr == 5eb593a`. Origin/lxr and main were
not changed. Before edits, all58 accepted input hashes and4352 framework file
bindings across the original eight roots matched. Accepted nursery Debug/Release
frameworks and all three fixture versions were independently copied, never
hardlinked, to this worktree's `artifacts/P6mature/baseline-*-windows` and
`/root/lxr-mature-evacuation/artifacts/P6mature/baseline-*-linux`.
Original worktrees/artifacts were read/copied, never modified.

Baseline managed24 processes per OS pass, including100628 lifetimes on/off,
OOM10, birth32, source-reuse8, fresh/weighted/harmonic27, default-policy35 and
retention1245/524292. Native baseline30979 passes in both configurations/OSes.
The preserved whole-source archive is
`artifacts/P6mature/baseline-source.tar`, SHA
`52b6bd2c4936529b9b368c07fe4e7c4d9b098bb2d67a6ce9b5616b3b329cd986`.
Exact retained SDK11.0.100-preview.6.26359.118 host/SDK/MSBuild hashes were
verified; fixtures were compiled on Windows and copied unchanged to Linux.

The unrelated321-byte XML and589-byte C# bytes remain
`63c1d0e88ee63036ebdfeeb0be740e00a9e0b1aa2c86fcf8e816c3e182253e31` and
`9d643c1e46daabae92f3b7a5ffa6bae5e260c4c542120d1089c8b14c24e371e5`.

## Final verification and identities

The qualified physical layouts are **candidate15**, not directory-name-inferred
configurations: Windows `artifacts/P6mature/candidate15-{Release,Debug}-windows`;
Linux `/root/lxr-mature-evacuation/artifacts/P6mature/candidate15-{Release,Debug}-linux`.
Unchanged CoreCLR/JIT/CoreLib/CoreRun are bound in the module manifests.

| Collector | SHA256 |
|---|---|
| Windows Release | `06de84f74717dac9f6dcd60c0d8b51b141c48b1d158c6eca6ee1be61e8beeca5` |
| Windows Debug | `422f197eac0df51b8b92c01a5e71631f877928848ad58261170ff6954a6e4666` |
| Linux Release | `daeb0993888dc3575fb5aac9743de475cdf14e9e98f418d470fcd803a7dc469d` |
| Linux Debug | `3b4fca1ef35298cf2fc25c3810cc987aea784d5bcd3c2933c5aedf5b9864533c` |

Portable `candidate15-fixture/runtime-tests.dll` is
`d3f4cbcc1e80c75a59d4492dc113ce1a6e28b54e1eaebdb4d8553b5e5e164e4e`.
The source/input manifest has61 unique paths; module manifests bind4352 files
across eight canonical physical baseline/current roots. These are bindings,
not4352 independent builds or observations.

Each OS runs28 current selected-host processes: both configurations, all three
lifetime modes100631 each, OOM13, birth32, source-reuse8, nursery/weighted/
harmonic27, default-policy35, mature/native-pin155 and retention1245/524292.
Both configurations/OSes pass native original30979, trace8358, storage1952,
policy50, object planes18, lifetime17, accounting31, root batching6170,
auxiliary retirement12307, mature3324, mature reader47, retained-owner22,
released-incarnation117, nursery-headroom131, completed-descriptor45,
remset100/whole532/wrap1375/same-shape1376/boundaries5505 and nursery reader48.
Nursery totals vary with actual allocations to reach exact reuse; raw counts
are preserved. The mistyped `--storage-accounting` candidate14 invocation
ran the default30979 suite, **not** accounting31; correct selectors were rerun.

Nine isolated wrong mechanisms per OS/configuration detect their actual seams:
stale-tag predicate, omitted replay, omitted current-field shape, missing mature
iterator/range rebase, dead completed-descriptor access, premature retained-source
release, lost destination RC, mature-contaminated nursery feed and corrected-unit
reservation. All36 final counterfactual processes retain failures; assertions
were not disabled or widened. Both OSes pass34 actual environment/JSON controls
using independently copied, hash-matched runtimeconfig hosting layouts.

One integrated review and one bounded follow-up found and corrected retained
candidate outgoing fields, early-release incarnation reuse, completed dead-owner
descriptors, missing nursery headroom and repeated allocation-pressure walks.
Failed-before positives are retained, not retroactively called passes. The
follow-up found no remaining concrete native issue in those remedies. Its
remaining runner-ordering issue was fixed and the invalid-output counterfactual
proves the row is persisted with semantic_valid=False before rejection.
Relevant tests were rerun after the final product edit.

The coordinator then independently verified all61 Windows inputs, the42-input
Linux native subset and4352 framework bindings before/after execution. It
replayed28 managed plus22 new native processes on each OS:100 positive processes
total, including both configurations and all three lifetime modes. Two additional
isolated current-field-shape omission replays fail1372/1375 at the exact poisoned
primitive payload/replay/pre-read receipts on both OSes. It recomputed all392
declared observations/56 groups and inspected the pinned selection/remset/unit/
allocator/release mapping, current-live-owner guard and native exact released-
source allocation proof. These are independent acceptance/consistency checks,
not new timing samples or a production/parity qualification.

## Cost, fragmentation and footprint evidence

Before proposing performance repairs, the immutable accepted nursery matrix
was recomputed: high-reference on/off is20.37%/21.54% slower Win/Linux with
about2.10 MiB copied, zero copy failures and8 collections. High-byte/pinned
on/off have16/8 median copy failures Win/Linux, but **both on and off have16
total collections** against8 primary full requests in those stored medians.
Extra collections are not wholly caused by copying. The older native-plane,
scratch-recommit and allocator-retention results remain unresolved; immutable
modules lack copy-phase timers, so pure memcpy attribution there is unmeasured.

The new protocol predeclares four DEFAULT-recycling inputs and seven arms:
accepted nursery off/on, same-source nonmoving/nursery-only/nursery+mature,
WKS and Server on identical runtime/JIT/CoreLib/CoreRun. Two interleaved A/B
pairs and two200 ms nominal injected observations give four normal observations
per group; one separately intrusive diagnostic gives196 processes per OS,
392 total/56 summary groups. Every process, command, setting, hash, checksum,
GC mix, round/full-call distribution and outcome is retained without retries.
The OS studies ran separately after compiler work completed.

The primary input ages512 nodes with4096-byte payloads, retains a64-node cycle,
and reclaims its dead neighbors through real RC under default allocation.
Dense retains512; pin input holds8 real pinned payloads; high-survival adds
eight128-node bursts retaining four cohorts. Each region has8 explicit full
calls; the following256 x4096-byte allocations are a separate reuse observation.
Recovery hits count allocations in original payload blocks, including possible
ordinary holes: they are **not distinct blocks, exact object reuse or proof of
whole-block release**. Native exact-release/reallocation tests supply the latter.
Release counters below are repeated collector-wide copied-source release events,
not64-payload attribution. Runtime objects also move and contribute real costs.

Median milliseconds, n=4 normal per arm/scenario:

| OS / input | Nonmoving | Nursery-only | Nursery+mature | Mature vs nursery | WKS | Server |
|---|---:|---:|---:|---:|---:|---:|
| Windows fragmented | 70.234 | 71.220 | 96.112 | +34.95% | 2.096 | 3.203 |
| Windows dense | 70.572 | 75.314 | 127.186 | +68.87% | 2.115 | 2.978 |
| Windows pinned | 68.425 | 69.661 | 96.162 | +38.04% | 2.284 | 3.295 |
| Windows high-survival | 77.349 | 84.743 | 136.162 | +60.68% | 3.960 | 6.375 |
| Linux fragmented | 84.583 | 85.921 | 91.873 | +6.93% | 1.345 | 3.162 |
| Linux dense | 84.224 | 89.183 | 139.485 | +56.40% | 1.567 | 3.567 |
| Linux pinned | 86.815 | 86.740 | 94.769 | +9.26% | 1.239 | 3.262 |
| Linux high-survival | 100.727 | 109.586 | 161.087 | +47.00% | 4.445 | 6.890 |

**No throughput win or small-effect sensitivity is claimed.** A/A and injected
observations are in the summaries/raw logs. Built-ins are characterization,
not equal-heap fairness; eight GC-call durations are not production request tails.
Whole-process CPU is not exclusive GC CPU, and short Windows CPU samples are
clock-quantized rather than zero work.

Fragmented normal medians: mature copies4,124,784/2,146,048 bytes Win/Linux and
releases197.5/140 copied-source block events over the8 calls. Reported
fragmentation falls829288->6688 and802416->6440 bytes versus nursery-only.
Normal processes also observe actual subsequent original-block address hits;
the final matrix retains each exact count. RSS instead rises
98777088->101318656 Windows and103315456->107016192 Linux. More free blocks
is **not** lower RSS. Dense, pinned and high-survival costs/footprints and every
failed/retained-source count remain visible in adjacent complete CSVs.

Separate intrusive diagnostic examples, milliseconds from actual exported
clock-frequency-normalized ticks:

| OS / input / component | Allocation | RC/VO/straddle initialization | Payload memcpy | STW trace closure |
|---|---:|---:|---:|---:|
| Windows fragmented mature | 1.405 | 5.413 | 1.423 | 25.271 |
| Linux fragmented mature | 0.378 | 2.145 | 1.299 | 6.223 |
| Windows high-survival nursery-only | 0.174 | 0.807 | 0.341 | 12.135 |
| Linux high-survival nursery-only | 0.286 | 1.634 | 0.945 | 4.890 |

Scopes are nested worker wall spans. Allocation includes refill metadata;
initialization excludes extent/flag publication; payload measures actual
memcpy; trace closure includes descendant work/copying, scan/root scopes
overlap it. They cannot be summed as exclusive GC phase CPU. They show that
allocation/metadata/closure cost is material, not that mature copying cures
the prior nursery regressions. These fixed inputs have8 collections and zero
observed copy failures in the diagnostic examples; this does not erase the
old fresh-source high-byte/pin failures.

Growth/quiescence diagnostics are separately intrusive single observations,
not substituted for timing rows. Both OSes/mature off/on still retain1240
arrays/checksum306460, five growth steps,402653184 reserve,134217728 admission,
109215744 managed commitment and **774049792 native-plane commitment**.
Linux growth mature on RSS417857536 versus397987840 off; post-full117.426 versus
72.041 ms. This is not an A/B latency distribution or a scratch-recommit fix.
Pointer-rich quiescent Linux mature on RSS662609920/private868200448 despite
owned Work/TraceScan bytes0 and live65848; off522780672/private751640576.
Windows on137965568/private254005248 versus off129871872/private254406656.
Shared native allocator retention and native planes remain separate problems.
The prior597180416 Linux nursery observation is preserved, not replaced by a
new claim of physical release or improved parity.

### Retained experiment and apparatus failures

The first candidate14 study stops after46 CSV rows and retains its47th raw
process: an intrusive mature diagnostic exits0 with correct graph/copy/release
activity but zero original-block hits in the bounded recovery window.
The original per-process placement gate was unjustified; protocolv2 explicitly
amends classification, not allocation inputs or heap policy. The stopped study
and its zero remain exploratory, not silently retried/discarded or final timing.
The runner-ordering counterfactual is separate synthetic tool validation,
not a collector observation.

All stage failures remain under the physical artifact roots: stale app checkout
correction, WSL argument-transport/wrapper stops, MSVC definite-assignment and
nested cost-scope compilation errors, incorrect control selector dispatch,
fresh-span/tag-wrap failures, review-before regressions and every wrong control.
The Linux candidate14 matrix's28 children passed but its shell wrapper returned2
on an escaped `exit $code`; candidate15's corrected wrapper exited0.
No failed attempt was relabelled as a build/test pass.

## Reproduction and exclusions

Load the build-and-test skill; configure once per OS/configuration, then build
only `clrlxrgc` incrementally. Exact SDK and accepted runtime/library/JIT layouts
were reused; no full CLR/JIT/CoreLib/libs matrix, dependency/audit change,
NativeAOT/Mono/R2R activation or platform expansion was performed.

```powershell
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -Configuration Release -OutputDirectory artifacts\P6mature\replay-native-Release-windows
artifacts\P6mature\replay-native-Release-windows\lxr-rc-validation.exe --mature
artifacts\P6mature\replay-native-Release-windows\lxr-rc-validation.exe --mature-reader
artifacts\P6mature\replay-native-Release-windows\lxr-rc-validation.exe --mature-remset-wrap
python docs\design\lxr-port\P6mature\run-acceptance.py --artifacts artifacts\P6mature --stage candidate15 --fixture artifacts\P6mature\candidate15-fixture\runtime-tests.dll --output artifacts\P6mature\new-acceptance-windows
python docs\design\lxr-port\P6mature\run-study.py --baseline artifacts\P6mature\baseline-Release-windows --changed artifacts\P6mature\candidate15-Release-windows --fixture artifacts\P6mature\candidate15-fixture\runtime-tests.dll --output artifacts\P6mature\new-study-windows.csv
```

Linux uses the existing `.sh` native runner, `corerun` and matching physical
Linux roots. Repeat native/current-host correctness with Debug. The runners
sanitize inherited DOTNET_/COMPlus_ variables case-insensitively, set canonical
settings and assert actual server/category/owning initialized module/hash/modes
before using capabilities. Unsupported/unmeasured counters are null, not zero.
All exact commands/settings/results are in the raw records/manifests.

No public issue/PR/comment/review/EgorBot, push, merge-back, main-checkout access,
abandoned runtimelab access or replacement implementation session was used.
Historical P0-P6nursery and P1.7 FAILED/INCONCLUSIVE remain unchanged.
