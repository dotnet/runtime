# Moving nursery vertical

Implementation commit `48b33d906e9be39b0789733c2af1cef2d6bc5e29`;
current-fixture capacity-recognition repair
`6d36d2ee8a2f46672b44b0724e2525f4a1b97629`.

Windows/Linux x64 standalone CoreCLR, on exact accepted compact tip
`a4490a35040da2eae25cebb0c38cd32f46d209e0` (implementation
`a818ad9949ca499f12572e8f7df574bc16cd555a`). Collector selection remains
default-off. This vertical implements promotion-time nursery movement and its
required .NET lifetime adaptations; it is not mature evacuation, complete P6,
complete P7.4, production qualification, or full P9/paper parity.

## Reference and change mapping

The sole coupled implementation oracle is binding
`0682434ae725787e38defbc17bd66dc918e4bdb7`, which pins core
`304ce69d43aae87de501111fceb8cbd33173a03a`. All reference reads used
revision-qualified `git show`/`git grep` in the read-only reference clone.
Checked-out `9625c174` is not the oracle. Historical PLDI binding
`abbdd1dbbe7277e4b0dfd1d37ba613c53f5ca50d` / core
`df8d30a39237a5bf5a8e27ca5a6f46acc6080c94` is comparison only. Paper citations
refer to extended arXiv:2210.17175, 1 November 2022, sections 3.1, 3.2.1,
3.2.2 and 3.3, not proceedings numbering.

| Change / gap | Exact oracle at core 304ce69d | Preserved mechanism and .NET delta | Post-diff evidence |
|---|---|---|---|
| Real copy/forwarding replaces in-place-only promotion | `src/util/object_forwarding.rs::attempt_to_forward`, `spin_and_get_forwarded_object`, `forward_object`; `src/plan/lxr/rc.rs::ProcessIncs::process_inc_and_evacuate`, `promote`, `process_slot` | SeqCst clear/claim/publish, one winning copy, counted destination and metadata before publication, increments and recursive field rewriting rather than a separate mark-copy collector. Existing 5.15/P1.6 negotiated padding-word state; forwarding address temporarily occupies the first source payload word, preserving MethodTable and synchronization header. No read barrier or new VM/JIT/GC-EE method. | All RC widths, 1025 racing aliases, copied cycles, exact payload/header/count checks, genuine managed root/field/handle movement. |
| Copy allocation and failure | `src/util/alloc/immix_allocator.rs::alloc`, `overflow_alloc`, `acquire_clean_block`; `src/policy/immix/immixspace.rs::ImmixCopyContext`, `post_copy` | Hot cursor first, distinct fresh-only multiline overflow cursor, recycled small-copy holes, existing admitted/committed storage only. To-space exhaustion retains complete objects in place, clears the claim and disables further copying for that pause. Metadata errors remain errors. Mutable fallback decisions and their first source increment are serialized under the claim. | Fresh-only versus wrong recycled-overflow discriminator; exhausted two-block heap, later recovery/reuse; integrated review's fallback race fixed. |
| Source retention and closure | `src/policy/immix/block.rs::set_as_in_place_promoted`; `block_allocation.rs::sweep_nursery_blocks`; `src/plan/lxr/global.rs::release`; `rc.rs` root snapshots; `cm.rs` owned trace queues | Old targets are not slots. Actual owned fields are rewritten during increments; copied integer root/trace/decrement targets are resolved while all raw readers are suspended/drained. Fresh EE relocation scans rewrite root/handle slots only during callback validity, before source release. RC-positive resumable scan owners do not move. Copied sources lose VO/extent only after reader repair; partially retained blocks are released only when RC-dead. | Non-rooting watched logical identity survives exact old-address replacement, then retires while that replacement remains live. Real GCDesc scan emits a held source-bearing packet; RC copies its target, physically reuses the source, and the same packet dispatches the destination exactly once. Missing-only-that-packet rebase fails despite correct black marking and surviving payload. |
| Nursery pinning and interiors | **No pinning oracle**: `src/plan/lxr/gc_work.rs` work contexts use `UnsupportedProcessEdges`; binding `mmtk/src/scanning.rs` supplies runtime roots | Necessary .NET-only adaptation. Discover all fixed/pinned/interior/handle pins before any copy; retain POH and LOS. Preserve relocatable interior offsets. Read-only frozen outgoing slots pin their owned targets instead of writing frozen memory. LoaderAllocator callback slots are read-only; existing handle relocation updates their actual owners. | Both native discovery orders, ordinary alias before late frozen discovery, actually read-only registered frozen memory, separate genuine movable object, frozen-only liveness/unregistration, fixed/POH/pinned handles, beyond-first-page interiors, hash and thin/inflated monitor preservation. Wrong frozen pin and late-observed-pin controls fail. |
| Actual copied survival | `src/plan/lxr/rc.rs::promote`; `mod.rs::SurvivalRatioPredictorLocal::record_copied_promotion`, `sync`, `SurvivalRatioPredictor::update_ratio`; `global.rs::gc_pause_start`, `release` | Actual complete copied extents only, reference fresh-block/page-rounded recycled-line denominator at pause start, reference predictor ordering/formulas. No in-place or pinned volume is disguised as copied survival. Copy-off feeds zero. Real capacity/hard limits remain independent. | Paired copied/source-retired bytes and pause-volume checks, rising/falling weighted/harmonic integration, zero-allocation native epoch, omitted copied-feed control, existing policy/growth/commit failure suites. |

The copy allocator's initially recycled multiline refills were found by a
reference recheck and corrected before qualification. A review also found a
real fallback race: an alias could choose in-place outside another worker's
outstanding forwarding claim. Every RC-zero Immix copy-versus-in-place choice
now holds that claim; in-place RC is established before clearing it. The review's
bounded follow-up confirmed the ownership fix.

## Supported modes and unchanged bounds

`DOTNET_LXREvacuateNursery` is a real Boolean toggle, default **1**, matching
HEAD's enabled nursery evacuation. `0` is the explicit same-source nonmoving
comparison. `DOTNET_LXRMaxYoungEvacSize` is the reference's inclusive size
exclusion, default unbounded. Collector selection/build remains opt-in.
Mature evacuation, defrag/headroom/pause-limit and huge-page settings still
reject enabled values. No timer policy was invented.

Only RC-zero objects in clean nursery blocks are evacuation candidates.
Recycled mature-hole births are stationary by the reference's rule.
In-place promotion retains the source block, so a pin or copy failure can
suppress further copies from that block. It does not pin all ordinary roots.
Small-copy recycling remains available with
`LXRNoMutatorLineRecycling=1`; `LXRNoLineRecycling=1` disables both.

Decisive fresh-source tests and the matched survival study explicitly use
`LXRNoMutatorLineRecycling=1` identically across LXR on/off/baseline arms.
These are **not default-policy results**. A separate default-policy cohort
requires actual recycling flags unchanged and proves genuine movement after
natural allocation, without that diagnostic switch.

The 1024 simultaneous mutator, 131072 copied-root and 256 frozen-record bounds
are unchanged. Existing 128/256 MiB reservations and 8/16 MiB initial admission,
managed-admission/native-plane budgets and the standard bookkeeping-inclusive
GC hard-limit rejection remain. Copy slack is obtained from real free admitted
storage, not an unreported reserve or limit lift.

## Verification and retained failures

Final product layouts are independently copied accepted runtime/library/JIT
bytes plus the changed collector under `artifacts/P6nursery/final-{Release,Debug}-windows`
and `/root/lxr-moving-nursery/artifacts/P6nursery/final-{Release,Debug}-linux`.
The portable fixture is compiled once on the verified Windows SDK and copied
to Linux; it is not an independent Linux fixture build.

The frozen movement/survival fixture is
`32a273fa52aa471a76c698a59229cd577b1a2bff4dacd6ece819832d018b5e69`
at `candidate3-fixture/runtime-tests.dll`. The separately rebuilt, capability-
correct current diagnostic fixture is
`63c5f4ba850b26d3ad7983eb8ec5efdbeefe06e3e2e8afb99d6f0e79b87950a5`
at `capacity-fixture/runtime-tests.dll`. All current-fixture acceptance and
31 environment/actual-JSON controls were rerun on both OSes after that repair.
Neither fixture replaces the immutable a8fb/40b/7799 accepted inputs.

| Final collector | SHA256 |
|---|---|
| Windows Release | `117c38d7c30ebe630081cde5257a914fde59b492cf4adcf6309d7914781f0a06` |
| Windows Debug | `ed8ed7d15a23b9caf6fef9468f000687add14e45e2588b844e58a45bfb544657` |
| Linux Release | `f08453dd075bb523741e8f65f0d2390a8430b236c1aba272891f6ac381c45948` |
| Linux Debug | `702633e1fa391963ac3257dc194dcd10ebddef6b435f295b5ce3e4c3779d1e95` |

The manifests bind58 unique current inputs and4352 file bindings across eight
canonical physical baseline/final framework roots. File bindings are not
independent builds or observations.

Final managed Debug/Release, each OS:

| Case | Checks per process |
|---|---:|
| Existing complete lifetime suite, moving and explicit nonmoving | 100628 each |
| OOM / genuine notifying births / watched LOS source reuse | 10 / 32 / 8 |
| Fresh nursery on/off, weighted and harmonic feed | 27 each |
| Unchanged-default-policy movement | 35 |
| Retention bytes / reference-rich closure | 1245 / 524292 |

Final native Debug/Release, each OS: original30979, trace8358, storage/growth1952,
policy45, exact object planes18, lifetime17, accounting31, root batching6170,
auxiliary retirement12307, and held moving reader48. Moving-nursery totals
vary with the number of actual allocations needed to reach exact source reuse;
each raw result records its exact passed/total count. Assertions are not
disabled when placement differs.

The selected packet latch and receipts exist only under
`LXR_SIDE_METADATA_TEST_PLATFORM`. They add no product test hook/export,
root, read barrier or VM interface. The receipt is recorded before forwarding
resolution and after normal work/pending completion. It cannot confuse a
source-address replacement with the copied logical object.

Six isolated wrong mechanisms per OS fail: forwarding-off, omitted copied
feed, recycled multiline overflow, premature pinned-source release, omitted
frozen pin, and ignored observed pins. The frozen-pin control reaches the
actual read-only frozen-field address assertion. Ignored observed pins can
fail earlier with a fatal relocation invariant in the ordinary graph;
that is general observed-pin enforcement, not a uniquely isolated
late-frozen-order discriminator. Positive early ordinary alias/late frozen
discovery and both native pin orders remain separately decisive.

One integrated review found two material items: the forwarding/fallback race
and insufficient outstanding-reader evidence. Both were addressed, with
bounded follow-ups checking the ownership fix and actual packet latch/receipt.
Final reader positives48/48 and missing-only-that-packet negatives47/48 pass/fail
as intended in Debug/Release on both OSes. The coordinator independently
replayed58 processes (48 managed,8 positive native,2 reader negatives), verified
source/framework/fixture bindings before and after, and recomputed all540
scheduled measurement rows/60 summary groups without another timing study.
After the capacity-detector repair, four additional independent on/off
diagnostic replays verified the actual growth budgets on both OSes with the
63c5 fixture. Those intrusive replays are not timing replacements.

Retained failures include:

- First large-copy native36/39 failures and managed E_FAIL: copied continuation
  metadata was initialized twice; it is now initialized once before publication.
- Harmonic fixture's demand that a recycled-mature interior move: an invalid
  eligibility expectation, not a predictor repair. Fresh-source expectations
  are now explicit; default-hole lifetimes remain tested.
- Global copied-volume equality after a managed helper call: mature addresses
  remained identical, but216 legitimate additional runtime bytes copied from
  a32768-byte allocation denominator. Assertions now identify the actual mature
  graph and reconcile real pause/retirement volumes instead of assuming no other
  managed births.
- First exact-source reuse fixture ignored mutator consumption of mature holes.
  Its declared fresh-source allocator now isolates that ownership proof.
- Native Windows `small` macro compilation failure, wrong artifact output path,
  initial stage-order error found before execution, WSL variable transport,
  relative compile-output control apparatus stops and all control failures.

Every process is retained under the physical artifact roots. Some early
exploratory module paths were overwritten before stage freezing; their logs
remain failures/exploration, not qualification or reconstructed original
measurements. Immutable accepted compact fixtures/modules and previous
sessions were never modified.

## Characterization scope

`run-study.py` predeclares five scenarios, compact baseline, same-source
nursery-off/on, WKS and Server, three interleaved A/B pairs plus injected
controls:225 processes per OS. Inputs contain32 rounds and exactly eight
measured explicit full requests. All processes, module/input hashes, commands,
settings, checksums, allocation/collection/copy/failure/pinning counters,
round/full-call distributions and whole-process wall/CPU/STW/RSS/private
observations are retained. No retries or success-selected samples.

Normal medians, six observations per arm. Times are milliseconds; differences
compare same-source nursery-on against nursery-off, not against WKS/Server.

| Scenario | Windows off / on | Change | Linux off / on | Change |
|---|---:|---:|---:|---:|
| Low-survival references | 83.574 /84.956 | +1.65% | 107.166 /108.548 | +1.29% |
| High-survival references | 184.217 /221.745 | +20.37% | 210.698 /256.090 | +21.54% |
| Low-survival bytes | 86.182 /85.007 | -1.36% | 114.387 /115.558 | +1.02% |
| High-survival bytes | 129.419 /144.102 | +11.35% | 181.463 /208.026 | +14.64% |
| Pin pressure | 126.170 /142.582 | +13.01% | 188.514 /214.286 | +13.67% |

**High-survival and pin-pressure copying regresses throughput/STW in these
inputs. Low-case differences are small/noisy, not a win.** High-reference
moving medians have about2.10 MiB of genuine copied volume; high-byte/pinned
medians about8.2-9.4 MiB, with16/8 copy failures Windows/Linux. All retain eight
primary full requests, but high-byte/pinned totals include additional automatic
RC pauses (median16/20 total collections Windows/Linux). The result is not
wholly an isolated memcpy cost. The same-runtime built-ins remain far faster;
their complete rows, CPU/STW/RSS/private and round/request distributions remain
in the raw CSVs. Windows CPU includes coarse process-clock quantization; zero
short built-in CPU samples are not zero work.

Separate growth uses the immutable a8fb fixture,45 processes per OS and default
mutator recycling. It retains1240 arrays/checksum306460. Windows growth
overlapped bounded native-control compilations and is explicitly interference-
affected, not clean latency qualification; Linux growth has large A/A spread.
No row was retried, discarded or relabelled. Actual elapsed/RSS/private/live and
GCMemoryInfo commitment remain measured, but the immutable fixture's P8-only
identity detector emits **unsupported capacity counters** for the P6 module.
Its zero reserve/admit/native-plane fields are **unmeasured**, not a zero heap
or a positive capacity assertion.

The current diagnostic fixture now recognizes P6's inherited capacity
surface. Separate `--storage growth` processes, actual bound final modules and
canonical defaults, confirm on BOTH OSes402653184-byte reservation,
134217728 admitted bytes,109215744 actual managed commitment,
774049792 native-plane committed bytes and five growth steps. Moving diagnostic
working sets are398766080 Windows and397606912 Linux; these intrusive single
observations are not substituted into the immutable growth timing matrix.
Exact commands/settings and fixture/module hashes are in the bound diagnostic
logs and adjacent diagnostic CSVs.

Fresh-source characterization is not default-policy or equal-heap built-in
fairness. Whole-process CPU is not exclusive GC CPU;32 round and8 request
durations are not production tail-latency qualification. Actual copy bytes,
in-place pins, overflow and collection mix must accompany timing comparisons.
No performance win is promised.

The compact baseline's missed25% forced target, native-plane overhead,
post-full scratch-recommit regression and Linux shared-allocator RSS retention
remain visible. Copying is not claimed to fix them. Growth/quiescence evidence
must use the actual bound final modules, not the old pre-birth growth matrix.

Final reference-rich quiescent diagnostics still report LinuxRSS597180416
bytes and private811786240, despite owned Work/TraceScan bytes0 and live65992.
WindowsRSS129638400/private252588032 with owned Work/TraceScan0. Native-plane
commitment is193515520 for that input. Shared-allocator retention remains
unresolved. Current immutable-growth post-full medians are about43.16 ms Windows
and114.83 ms Linux moving, versus41.87/113.69 off; the previously retained
13.377->39.260 and17.983->59.724 scratch-recommit regressions are not inferred
away by copying.

## Reproduction and preservation

Use the existing P4 native runner once per OS/configuration, then its executable
with `--nursery`, `--nursery-reader` and the existing selectors.
`run-acceptance.py` runs the managed matrix and records every exact command,
canonical environment, return code and fixture/collector hash. It sanitizes
inherited DOTNET_/COMPlus_ settings case-insensitively.

```powershell
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -Configuration Release -OutputDirectory artifacts\P6nursery\replay-native
artifacts\P6nursery\replay-native\lxr-rc-validation.exe --nursery
artifacts\P6nursery\replay-native\lxr-rc-validation.exe --nursery-reader
python docs\design\lxr-port\P6nursery\run-acceptance.py --artifacts artifacts\P6nursery --stage final --fixture artifacts\P6nursery\candidate3-fixture\runtime-tests.dll --output artifacts\P6nursery\new-acceptance
python docs\design\lxr-port\P6nursery\run-study.py --baseline artifacts\P6nursery\baseline-Release-windows --changed artifacts\P6nursery\final-Release-windows --test artifacts\P6nursery\candidate3-fixture\runtime-tests.dll --output artifacts\P6nursery\new-study.csv
python docs\design\lxr-port\P6nursery\run-study.py --suite growth --baseline artifacts\P6nursery\baseline-Release-windows --changed artifacts\P6nursery\final-Release-windows --test artifacts\P6nursery\fixtures\runtime-tests.dll --output artifacts\P6nursery\new-growth.csv
```

Linux equivalents use the `.sh` native runner, `corerun` and the matching
physical Linux layouts under `/root/lxr-moving-nursery`.
Only `clrlxrgc` was incrementally built after one configure per OS/configuration;
there was no CLR/CoreLib/JIT/library rebuild matrix, package change, audit
disabling, NativeAOT/Mono work, push, PR, public issue/comment or merge-back.
The authorized ancestry-only fast-forward corrected this worktree from stale
`17ac93d` to exact `a4490a3`; main and origin/lxr were untouched.
The unrelated321-byte XML and589-byte C# file retain hashes
`63c1d0e88ee63036ebdfeeb0be740e00a9e0b1aa2c86fcf8e816c3e182253e31`
and `9d643c1e46daabae92f3b7a5ffa6bae5e260c4c542120d1089c8b14c24e371e5`.
