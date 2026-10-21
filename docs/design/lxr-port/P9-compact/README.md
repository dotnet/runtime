# Compact nonmoving LXR vertical

Windows/Linux x64 collector-internal representation, birth, RC/SATB packet,
trace and auxiliary-retention work on accepted P8
`e7e6594f89cd83aad7f0cd98bd8916b6ae6563e6`. This is a selectable, genuinely
owning nonmoving prototype, not WKS/Server delegation, paper parity, production
qualification or P6. No VM/JIT/GC-EE interface or public managed API changed.
P1.7 remains FAILED/INCONCLUSIVE; nursery copying and mature evacuation remain
deferred.

## Results and unmet targets

The targets were declared before changed measurements: 30% lower native-plane
commitment and 25% lower working set on fixed retained growth; 10% lower
allocation and 25% lower forced-workload median; 50% fewer combined
Work/TraceScan allocation requests; zero collector-owned completed
Work/TraceScan bytes after full-cycle drain. They were not widened.

**Allocation, footprint, allocation-request and owned-retirement targets pass.
The 25% forced-workload target fails on BOTH OSes. Shared allocator physical
retention after pointer-rich quiescence remains unresolved.** Logical frees
are not presented as physical-release qualification.

Matched normal medians, six observations per arm, immutable portable P8 fixture
`a8fb7abdca38c21b4acb9fd5a89f1d8fd824d8729c1dcadb3b52c415cfbf2c30`.
Baseline below is P8 plus the separately frozen diagnostic/test-contract
adaptation, with accounting and lifetime probes **off** in timed processes.
Runtime/CoreLib/JIT/CoreRun are unchanged P8/P5 bytes.

| OS / workload | Baseline ms | Compact ms | Change | WKS ms | Server ms |
|---|---:|---:|---:|---:|---:|
| Windows allocation | 125.620 | 91.736 | -27.0% | 10.385 | 11.572 |
| Windows mutation | 3.436 | 3.336 | -2.9% | 3.240 | 3.282 |
| Windows automatic | 263.853 | 240.127 | -9.0% | 20.977 | 31.711 |
| Windows forced | 388.238 | 300.918 | -22.5% | 12.590 | 17.088 |
| Linux allocation | 195.368 | 126.590 | -35.2% | 16.087 | 15.014 |
| Linux mutation | 3.776 | 3.620 | -4.1% | 3.462 | 3.500 |
| Linux automatic | 438.824 | 388.709 | -11.4% | 25.340 | 45.984 |
| Linux forced | 422.573 | 365.678 | -13.5% | 15.268 | 19.189 |

Forced inputs retain eight real explicit full requests and checksum 49152.
Allocation retains 524288 operations/checksum 1073479680; mutation retains
524288 stores/checksum 1073479680. Built-ins are characterization on the same
runtime, not equal-heap fairness. LXR remains much slower/larger than built-ins.
Short mutation effects and noisy arms are not precision claims: baseline
Windows mutation A/A is -12.93%; Linux Server automatic A/A is -31.47%.
All injected 200 ms matched controls are retained, without retries.

The fixed growth input still retains 1200 x 16 KiB plus 40 x 1 MiB arrays:
1240 operations, checksum 306460, five growth steps, 384 MiB reservation,
128 MiB admitted, about 104.2 MiB managed commitment and 59 MiB live.
The actual admitted split is **64 MiB Immix / 64 MiB LOS**. An earlier 32/96
inference was incorrect and was corrected; the old all-byte-granular
component total did not depend on that split.

| OS / measurement | Baseline | Compact | Change |
|---|---:|---:|---:|
| Both / native-plane committed bytes | 1137360896 | 774049792 | -31.94% |
| Windows / working set bytes | 667815936 | 392456192 | -41.23% |
| Linux / working set bytes | 664524800 | 390297600 | -41.27% |
| Windows / private bytes | 1313026048 | 947472384 | -27.84% |
| Linux / private bytes | 1395720192 | 1032132608 | -26.05% |
| Both / managed committed bytes | 109215744 | 109215744 | unchanged |
| Windows / growth elapsed ms | 323.447 | 291.741 | -9.8% |
| Linux / growth elapsed ms | 764.306 | 479.092 | -37.3% |

Growth matrix above predates only the final cleared-span birth optimization;
that input has zero/one fast births, and the final-module single diagnostic
reconfirms the exact commitment/admission and similar residency. It is not
silently relabelled as a final-module timing matrix. Linux baseline growth
A/A is 20.51%, and its injected control is inconclusive. No sample was retried
or threshold changed.

Its exact retained compact frameworks are this worktree's
`artifacts/P9/final-Release-windows` (collector
`8dd3bf19f1ddb522c46c3a7171e8615881d248d72cc8eaa14d34fd2d0c269051`)
and `/root/lxr-compact-collector/artifacts/P9/final-Release-linux` (collector
`6638a4c10af70cfe8d79c46e8f142fbb48fc68c5affd208b3f43e5f2dc99636e`).
Every file in these physical roots is now bound in `module-identities.csv`.
`source-stage-bindings.csv` and `source-stages/pre-birth/README.md` link the
unchanged growth rows to their original contemporaneous native source records
and an explicitly labelled, hash-checked birth-only source reconstruction.
Neither growth module is mapped to the final `f288f524...`/`8e3f55c9...` module.

**Tradeoff:** releasing/reinitializing private trace scratch raises the growth
input's post-full median from 13.377 to 39.260 ms Windows and 17.983 to
59.724 ms Linux in that matrix. This is a real regression, not hidden in the
throughput table. The bounded footprint improvement does not imply all real
collection kinds became faster.

## Baseline preservation and a necessary .NET test repair

The authorized ancestry-checked fast-forward corrected the stale worktree
from `17ac93d30d5ebc08aeda0ddf24e562d506d72230` to exact e7e6594 after
verifying local `refs/heads/lxr`. No reset/stash/rebase/cherry-pick/clean,
main-checkout or abandoned runtimelab access was used. Previous worktrees
and `/root/lxr-p8-speed-scale` were read/copied, never modified.

Initial unrelated long-path status entries had no content diff with
`core.longpaths=true`. Bytes before/after convergence remain:

| File suffix | Bytes | SHA256 |
|---|---:|---|
| `EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1_NotMatchingName.xml` | 321 | `63c1d0e88ee63036ebdfeeb0be740e00a9e0b1aa2c86fcf8e816c3e182253e31` |
| `OverrideOfAbstractIsKeptNonEmptyLibraryWithNonEmpty.cs` | 589 | `9d643c1e46daabae92f3b7a5ffa6bae5e260c4c542120d1089c8b14c24e371e5` |

All 53 P8 inputs matched before edits; original manifest SHA256 is
`a169dda876f2009810576fe0e450ae49c4f0aad33199748031d48bb514dd885d`.
Baseline source archive `artifacts/P9/baseline-source.tar` is
`ab2351c91a41ff4aa17422a156f50cc373fd146b52100f9f3f74ff0267fe317f`.
Original P0-P8 records, oracle ledgers and immutable modules/fixtures remain
unchanged. Baseline frameworks were independently copied, not hardlinked, into
`artifacts/P9/baseline-windows`, `baseline-debug-windows` and the corresponding
`baseline-linux`, `baseline-debug-linux` under `/root/lxr-compact-collector`.
Additional copied layouts are physical copies, not independent observations.

The exact original a8fb fixture with exact P8 Linux Debug collector
`72c69dffac26b5f5e76d9ca6aab0cd0855a7beaf6c7fc10631850b55bc0e9b39`
failed `VerifySyncBlockLifetime`: "An unreachable SyncBlock referent did not
retire during weak cleanup." Its process remains **causally unresolved** at
`artifacts/P9/baseline-Debug-linux-suite-corrected.log` on Linux. Three later
instrumented passes did not erase or explain it.

A deterministic counterfactual then proved the historical extent-only
assertion unsound: retire a real SyncBlock-backed 64 KiB array, retain a new
same-type array at its exact Object*, observe extent 65560 but one original
reclaim plus one subsequent birth. The historical assertion fails on both OSes;
the corrected identity/lifecycle assertion passes eight checks. A gated,
single-address, non-rooting probe uses an integer start and two counters,
not a dense identity plane, payload-after-death reads, retained roots or a
VM interface. The positive control now also requires zero reclaims through
the four SyncBlock-only RC epochs, then at least one after full weak cleanup.
Thus it strengthens liveness protection without confusing replacements.
The original failure is not retrospectively attributed to reuse.

The original timed DLL remains independently preserved in this **Windows
worktree** at `artifacts/P9/original-timed/runtime-tests.dll`, SHA256
`40b5552013d4793f7bd44ccb5eb1b4afde245fcbb85444d7c3c8834d22e963f8`.
On Linux, its actual retained path is
`/root/lxr-p8-baseline/study-harness/runtime-tests.dll`, with the same verified
SHA256 and its companion deps/runtimeconfig files in that directory. This
vertical did not create `/root/lxr-compact-collector/artifacts/P9/original-timed`
or replay the original timed DLL as a new timing sample. An independent Linux
acceptance attempt stopped before execution because it assumed that absent
copy path; this was an apparatus-path preflight stop, not a collector failure.
The immutable a8fb performance fixture is separate from the repaired 100628
correctness/retention fixture
`7799af7c68e9d9c0d449d2c082f824fe6da2ca3fa144caed0a2b1aedfafdabef`.

## Compact per-edit-group oracle mapping and post-diff checks

Every substantive group was preceded by revision-qualified reads in read-only
`C:\github\lxr-reference\mmtk-core`, at **core
304ce69d43aae87de501111fceb8cbd33173a03a**, coupled to binding
0682434ae725787e38defbc17bd66dc918e4bdb7 (`mmtk/Cargo.toml`).
Checked-out 9625c174, historic PLDI and simplified trees were not substituted.
Paper citations are the extended arXiv:2210.17175, 1 November 2022,
Sections 3.1/3.2.1/3.2.2 and Figure 2, not proceedings numbering.

| Group / oracle file and symbol at core 304ce69d | Port overhead and implemented translation | Preserved invariant / post-diff check and evidence |
|---|---|---|
| Birth accounting: `src/policy/immix/immixspace.rs::initialize_object_metadata`, `block_allocation.rs::sweep_nursery_blocks`, `global.rs::release`; `src/plan/lxr/gc_work.rs::ReleaseLOSNursery`, `largeobjectspace.rs::release_rc_nursery_objects`; paper 3.1/3.2.1 | Immix has no per-object birth list in its initialization path; port had two extra flat birth/reference/publication banks. Removed those, tracking current-epoch allocated Immix blocks once per owned refill (one byte/block, not a per-birth stream). Retire RC-zero active starts only in those regions, plus existing LOS ownership, at suspended release before existing sweeps. | Not a general heap-wide RC-zero heuristic: old mature `Death` retires its extent before ValidObject invalidation/zero publication; previous lazy/cycle generations drain first. Root/recursive increments finish before release. LOS keeps active spans/nursery state and its independent sweep. Post-diff checks include black newborn/white child, held active-scan deletion, sticky, reclaimed layout, finalization/resurrection and stale leases; original30979/storage1952/trace8358 and managed100628 pass. |
| Supplemental granularity: `src/policy/largeobjectspace.rs::LargeObjectSpace`, `get_super_page`, `rc_mature_objects`; `src/util/metadata/side_metadata/spec_defs.rs::LOS_PAGE_REUSE_COUNT`; paper 3.1/2.7 | Replaced pointer-sized supplemental extent/scan-pointer/lock/.NET-flag entries over every LOS payload word with entries at actual page-aligned allocation starts. Immix remains eight-byte indexed. This is a faithful bounded C++ ownership representation plus .NET extent/flag adaptation, not a new collector policy/hash/pool. | Real managed addresses, Object*-8, complete extents>=24, LOS>=32768, stable reservations and metadata-before-escape unchanged. Per-field RC/log lattices and full mutation bounds unchanged. Interior lookup uses active span ownership, never rounding an interior into a start. Exact headers, beyond-first-page roots, independent allocations and reclamation pass18 checks; deliberately omitted interior guard fails. |
| Birth initialization: `src/policy/immix/block.rs::clear_rc_table/clear_striddle_table`, `immixspace.rs::initialize_object_metadata`, `src/util/rc.rs::RefCountHelper`; paper3.1/3.2.1 | Port repeatedly revalidated already-cleared owned spans per fast birth. Existing notifying helper validates exact contiguous consumption; it now supplies a private, one-shot matching start/size witness. Matching active same-engine context registers ValidObject on the already RC-zero/non-continuation span. Slow/unwitnessed paths retain old validation; Debug retains full span checks. | No notification bypass, RC-width change, public capability or in-place survival feed. Duplicate extents fail; witnesses are consumed/cleared on attach/flush and cannot be forged through public fields. Resulting diff rechecked against allocator hole exclusion and dead-block RC-zero guarantee. Native20k fast-birth samples preserve20000 fast notifications +14 slow refills/480336 bytes; medians ~3.284→1.455ms Win, ~4.264→2.501ms Linux. Debug/Release storage and managed helper/lifetime suites pass after final edit. |
| RC/SATB ownership: `src/plan/tracing.rs::VectorQueue::take`, `src/plan/lxr/barrier.rs::flush_decs_and_satb`, `rc.rs::ProcessDecs::new_arc`, `cm.rs::LXRConcurrentTraceObjects::new_arc`; paper3.2.1/3.2.2 | Reference transfers Vecs and shares Arc snapshots. Port RC/SATB packets now borrow immutable sealed-bank spans instead of duplicating both payload arrays. Trace descendants that can outlive bank readers retain independent copies. | Reservation/release-publication/acquire consumption, explicit holes and once-per-owned-slot bound unchanged. SATB reader finishes its STW stage; lazy RC reader and completion generation drain before the bank is reused. Root snapshots retain existing independent copies. Shared-SATB, first-store/churn, overflow and wrong-lifetime controls pass/fail as intended; no required independent snapshot was removed. |
| Trace/packet costs: `src/plan/lxr/cm.rs::ObjectQueue::enqueue/scan_and_enqueue/new_ref_arrays`; paper3.2.2 | Reference directly scans ordinary objects, skips primitive payload and splits large arrays. Port uses bounded direct stack scanning where a conservative complete-extent reference bound fits the remaining1024-reference packet budget, preserving collectible loader edges; larger scans remain heap-owned/resumable. Root seeds are batched rather than one copied packet/root. | Mark before scan/death, black-newborn closure, deletion scans, atomic field loads, large-array slicing and checkpoints preserved. Review found a repeated-prefix batching defect; exact-copy test failed before, then6170/6170 for1/2/1023/1024/1025 roots passes all OS/configurations. Native trace8358 and real managed concurrent tests pass. |
| Retention: `src/scheduler/worker.rs::do_work_with_stat/drop(work)`, `src/plan/lxr/cm.rs` owned vectors/slices; Figure2 | Port retained completed work until another inactive pause. At suspended/quiescent boundaries it now removes completed headers, rebuilds pending lists and releases completed scan descriptors only when explicit Work users are zero. Inactive all-reader drain releases remaining work and decommits private scan/lock planes; next initial/full trace recommits before work publication. | No mutator-visible descriptor moves or live metadata decommit. Pending scans/RC/SATB readers survive interruption. Admission reserves the **full future trace-scratch budget** even while scratch is physically decommitted; partial commit/decommit is not advertised as full rollback.12307 pointer-rich native retirement checks and real byte/pointer-rich managed reclaim pass; deleting pending work deliberately fails. Shared system-allocator RSS is a remaining limit, not qualified away. |

## Memory, ownership and timing evidence

The old `storage-census.csv` exactly reconciles to the historical
1137360896-byte plane counter. New diagnostic range inventory measures
page residency, including native arrays and side metadata over the complete
managed reservation coverage (including retained partial commits in it).
Coverage/reservation is not commitment. Requested Work/TraceScan bytes are
not CRT/glibc arena commitment or residency. Root/mutator requested capacity
and actual snapshot/context occupancy are recorded separately.

Baseline named-array + side-metadata residency is 531202048/530857984 bytes
Win/Linux; final diagnostic is 257200128/257024000. Private memory, actual
managed commitment/residency, native-plane commitment and whole process RSS
are distinct. The whole-GC bookkeeping-inclusive hard-limit family is still
explicitly unsupported; managed admission/native planes remain partial budgets.

Accounting is opt-in at a drained inactive boundary. It seeds previously owned
bytes, then tracks allocation requests/current/high-water/copied bytes.
The forced Windows diagnostic records combined Work/TraceScan allocations
967843→431624 (-55.4%); Work alone687359→419119, scans280484→12505.
Copied references6427840→5739544 bytes do **not** halve; that was not the
allocation-request target. Current owned Work7299160→0 and scans2579640→0.
High-water Work15281288→9968608, scans4177080→428288.

Diagnostic wall/owned-process CPU/STW, single observations (not exclusive
GC CPU or a benchmark distribution):

| OS / forced diagnostic | Baseline wall/CPU/STW ms | Final wall/CPU/STW ms | Primary full calls |
|---|---|---|---:|
| Windows | 499.079 /734.375 /358.168 | 354.955 /765.625 /260.322 | 8 /8 |
| Linux | 588.894 /794.925 /442.859 | 391.123 /602.969 /303.300 | 8 /8 |

These enabled diagnostics are intrusive; increased Windows CPU is reported,
not hidden behind lower wall/STW. Disabled-instrumentation control medians
also remain: Windows P8 allocation/automatic/forced
134.462/279.184/409.313 versus135.365/274.394/402.459 ms diagnostic-off;
Linux197.439/452.495/453.803 versus210.223/476.460/433.776.
Windows allocation A/A15.52% and Linux automatic A/A9.77% prevent small-effect
overhead precision. No sampled whole-process percentages are presented as
exclusive measured-region bottleneck shares.

Real retention diagnostics use both the fixed1240-array byte input and262144
nodes plus a262144-store reference-rich burst. NoInlining isolates root
lifetimes; reclamation counters must cover the actual payload after the helper
returns. The baseline genuinely reclaims payload but fails the new auxiliary
retirement assertion; final bytes1245 and references524292 checks pass each OS.
After final full drain, owned Work/TraceScan bytes and trace planes are zero.

**Physical limit:** Linux reference-rich quiescent RSS remains595603456 bytes
(baseline628293632), despite collector-owned bytes=0 and managed live65572.
Windows reference-rich quiescent RSS is129503232 (baseline200503296).
Returning allocations to a shared native allocator does not promise OS release;
no process-wide malloc trimming or unsupported pool was introduced. Reservations,
admitted Immix commitment and worst-case RC log capacity also remain retained.

## Final correctness and reproduction

Final affected Debug/Release runs on each OS pass:

| Suite | Checks per configuration |
|---|---:|
| Original integrated native | 30979 |
| Storage/growth/commit-failure | 1952 |
| Policy/actual integration | 45 |
| Focused trace/lifetime | 8358 |
| Exact supplemental starts/interiors | 18 |
| Watched lifetime | 17 |
| Gated accounting | 31 |
| Exact root-copy batching | 6170 |
| Pointer-rich native auxiliary retirement | 12307 |
| Repaired selected-host managed lifetime | 100628 |
| Managed OOM/recovery | 10 |
| Genuine birth/helpers/concurrent allocation | 32 |

Each OS also passes31 actual environment/JSON controls and9 mixed-case
contamination controls. Wrong early-root, dead-payload query, SATB/deletion
marking, predictor/trigger order, watched-reclaim accounting, copy accounting,
interior alias and premature pending retirement controls retain their failures.
Final framework/fixture/input bindings are in the adjacent CSV manifests;
full compile commands, test outputs, process commands/settings, schedules and
raw measurements remain at the physical artifact roots.

The final physical frameworks are this worktree's
`artifacts/P9/birth-final-Release-windows` and `birth-final-Debug-windows`,
and `/root/lxr-compact-collector/artifacts/P9/birth-final-Release-linux` and
`birth-final-Debug-linux`. Collector hashes:

| OS/configuration | Collector SHA256 |
|---|---|
| Windows Release | `f288f524e7a5bfdb1aa267c3a1a890eb154fc0c44b96abb1e47a2fe0504576a2` |
| Windows Debug | `2e39f4db0aeb94133e5f2b0e5794d13476ff0874edfd5ab25f9d36c8e62137e2` |
| Linux Release | `8e3f55c900e090d9b04537a3195e3ed9904917223a67b40811be7ddf2c8cc79a` |
| Linux Debug | `76de7a4d23da34296aa4ae9dee4d875c5ad14710ec20b89e1668be3c5bf55153` |

The matched diagnostic-baseline Release frameworks are
`artifacts/P9/diagnostic-baseline-Release-windows` and
`/root/lxr-compact-collector/artifacts/P9/diagnostic-baseline-Release-linux`,
with collectors `6d72146e86810a730b074b8bfbee900fe38befcc790d0dea71bec07818673879`
and `c643932428e60eee8a660d972e5f801c5720724ac11a10dd340e12f62c3b9b9e`.
The repaired correctness fixture is `artifacts/P9/retention-fixtures/runtime-tests.dll`
at each OS artifact root. It is not substituted for
`artifacts/P9/fixtures/runtime-tests.dll` in fixed-input timing matrices.
The component-census fixture is independently frozen at
`artifacts/P9/storage-fixtures/runtime-tests.dll`,
SHA256 `be8b35e1376724559ea3f13c7429d0c6da05e24381d17030467d78d8800bac02`.
`module-identities.csv` binds every file in fourteen canonical physical
framework roots (7616 file bindings): the original twelve roots plus the two
retained pre-birth growth roots. These are not 7616 independent builds or
observations. `validated-inputs.csv` still lists54 unique current input paths;
the separately labelled pre-birth stage does not replace the final input set.

Baseline and final configurations were configured once per OS, then only
`clrlxrgc` incrementally built; accepted SDK/runtime/library layouts were
copied/reused. No broad CLR/JIT/CoreLib rebuild, dependency edit, audit disabling,
new runtime/public interface, push, PR, issue/comment or P6 work was used.

Representative Windows commands (run from this worktree):

```powershell
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -Configuration Release -OutputDirectory artifacts\P9\native-replay
artifacts\P9\native-replay\lxr-rc-validation.exe --trace
artifacts\P9\native-replay\lxr-rc-validation.exe --p8-storage
artifacts\P9\native-replay\lxr-rc-validation.exe --root-batching
artifacts\P9\native-replay\lxr-rc-validation.exe --aux-retirement
python docs\design\lxr-port\P9-compact\run-vertical.py --baseline artifacts\P9\diagnostic-baseline-Release-windows --changed artifacts\P9\birth-final-Release-windows --test artifacts\P9\fixtures\runtime-tests.dll --output artifacts\P9\new-matched.csv
```

Linux equivalents run under `/root/lxr-compact-collector`, with the `.sh`
native runner and `birth-final-Release-linux`/`diagnostic-baseline-Release-linux`.
All process invocations in raw logs use their actual physical roots.
Before managed execution, inherited DOTNET_/COMPlus_ LXR/GC selections and
hard limits are removed case-insensitively; canonical ReadyToRun=0,
TieredCompilation=0, gcServer=0, gcConcurrent=1, GCHeapCount=2 and exact GCPath
are set. LegacyCapacity=1 applies to old-capacity correctness/throughput, not
growth/retention. Runtimeconfig JSON controls use the separately copied
same-runtime dotnet/hostfxr layout.

## Review, failures and exclusions

One integrated review plus bounded follow-ups found the root-prefix copy bug.
The exact-copy control fails on the erroneous implementation and passes after
the fix. The originally requested final birth group was then narrowly checked
against the pinned oracle and its one-shot ownership witness. Relevant suites
were rerun after the last product edit; no disabled tests remain.

The initial stale checkout, long-path probe, invalid `--suite` invocations,
WSL path/variable transport failures, unavailable standalone handle helper,
native output-copy path errors, assertion-only unused variable, incorrectly
nested helper insertion/compiler failures, initial prefix-batching performance
attempt and all missed/noisy targets are retained/classified. Pre-final timing
matrices are not silently promoted to final source. Some earliest exploratory
lifetime-control logs reused names before unique run directories were added;
their tool transcript remains, but those overwritten raw instances are excluded
from qualification/statistics rather than reconstructed as original logs.
The causally unresolved original Linux Debug SyncBlock failure remains separate
from deterministic harness counterfactuals. None is waived as flakiness.
