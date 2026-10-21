# P8 speed and scale vertical

This is one integrated Windows/Linux x64 CoreCLR **nonmoving selectable LXR
prototype** vertical. It is not WKS/Server delegation, production qualification,
P6 evacuation, or P9 parity/performance convergence. P1.7 remains
FAILED/INCONCLUSIVE. No VM/JIT/GC-EE interface was added or changed.

## Baseline and provenance

The worktree was corrected by the authorized fast-forward from
`17ac93d30d5ebc08aeda0ddf24e562d506d72230` to accepted P5 record
`d9d08a7630d723ad93866d6147d388e97e01bb5b`. The unrelated linker XML retained
SHA256 `63c1d0e88ee63036ebdfeeb0be740e00a9e0b1aa2c86fcf8e816c3e182253e31`;
its bytes converged with the accepted commit.

All 47 P5 inputs matched on Windows and the preserved native Linux source.
Before product edits, the exact optimized framework and both fixture versions
were independently copied, not hardlinked, to:

- Windows: this worktree's `artifacts/P8/baseline-windows`.
- Linux: `/root/lxr-p8-baseline`.

The original P5 worktree and `/root/lxr-p5-1205f569` were not modified.
Full copied-file identities are `baseline-windows/identities.csv` and
`/root/lxr-p8-baseline/identities.sha256`. The original timed fixture remains
`40b5552013d4793f7bd44ccb5eb1b4afde245fcbb85444d7c3c8834d22e963f8`;
the preserved final correctness fixture remains
`2d75edd3f943df4170b64b07c9c4e7412ce95029450feef01a07c1f3e82396d1`.
Preserved LXR Release modules remain Windows
`2553db4d8a2609e905688adc4187734175952ee3c30cf3a4f49a8fd75a286e99` and Linux
`a9b9a9d9d9cbd969cc14a75fd8513c0befae6da07494b64ca3b7ff5ce2d88eba`.

Current inputs and executed modules are bound by the manifests in this folder.
The canonical input extent is 53 unique files; the shared Linux product/native
fixture subset is 42 unique files. `module-identities.csv` gives the exact
physical framework root for all 30 bindings, including the separate
`/root/lxr-p8-baseline/framework` baseline.
CoreCLR, JIT, CoreLib, libraries and CoreRun remain the accepted P5 bytes.
The changed module reports interface **5.15**, build **8** and
`P8 speed and scale engine`. Debug frameworks were separately verified against
P5 identities; the directory name `net11.0-*-Release-x64` was not used as
evidence of native Release configuration.

## Measured costs and implementation

The preserved frameworks first passed 100627 managed checks and 10 OOM checks
on each OS. Fresh P5 fixed-study medians were Windows LXR 759.630 ms,
WKS 10.686 ms, Server 14.050 ms; Linux LXR 1171.216 ms, WKS 12.181 ms,
Server 16.037 ms. Each normal input still includes eight explicit full
collections and checksum 49152.

A scoped Linux `perf` CPU-clock recording of the **owned process and its
threads**, including startup/warmup, captured 2698 samples with zero lost
samples. The process's timed section was 1231.661 ms. The largest leaf cost
was kernel wakeup work: `_raw_spin_unlock_irqrestore` 24.39%, with stacks
through scheduler submit/worker mutex release and futex wakeup. Task switching
was 8.41%; demand-zero page work 7.15%; metadata Load/Store 4.08%/3.26%;
malloc 2.30%; dense object walking 2.26%; NewWork 1.78%; heap statistics
1.19%; cycle sweep 1.07%. These are whole-process sampled CPU shares, not
exclusive timed-workload fractions or per-barrier estimates.

Implementation against those costs:

- Aggregate nursery increment children and death decrement children across
  an executing packet, retaining child completion registration and SATB-before-
  decrement ordering. No recursive shadow tracing algorithm was added.
- Reuse the existing word-wise metadata reset for exclusively owned fresh/
  recycled spans instead of an atomic store per eight-byte field slot.
- Maintain an atomic active-start bitmap and iterate actual object starts.
  Statistics group those starts in one pass, rather than repeatedly searching
  the remaining heap from every empty block. Sweeps still use metadata/extents,
  never dead payload layouts.
- Export initialized, zeroed Immix holes through existing allocation contexts.
  The already-selected P1.4 notifying object/array/string helpers execute
  genuine fast allocations. Every successful birth still publishes exact
  `Object* - 8` extent, RC/ValidObject, flags and a birth token before return.
  GC enumeration invalidates spans and the generic EE clears combined limits;
  detach and refill reconcile tails and standard allocation byte accounting.
- Native root/lifetime scan workers wait for the creating call to return.
  This closes a Linux Debug utility-thread naming/exit race exposed by shorter
  scans, without changing VM/PAL code or ignoring an assertion.

## Capacity, commitment and limits

Defaults reserve 128 MiB Immix and 256 MiB LOS, initially admitting
8 MiB/16 MiB. Immix commits real additional blocks; LOS commits payload pages
on allocation after admitting new free spans. The physical allocator, side
metadata, extent/scan/flag planes, active bitmap, birth streams and flat
increment/decrement token streams all cover admission before mutators resume.
Addresses and immutable callback descriptors never move.

Plane storage is virtually reserved at stable addresses and committed only for
admitted ranges. Birth streams use the minimum complete object extent bound;
increment/decrement streams retain the full once-per-owned-slot bound. Growth
is suspended behind the existing lazy/trace ownership contracts. It first
services actual space pressure and retries reclamation, then grows if needed.
LOS growth accounts for a coalescible free tail. Geometric growth is preferred,
not required: admission can choose a smaller aligned increment within the
native-plane budget.

Failed admission does not publish capacity or poison old objects. Successfully
committed partial planes/metadata may remain committed for a subsequent retry,
bounded by reservations; they are not presented as a complete physical-memory
rollback. Tests inject commit failure at six distinct plane/stream/metadata/
payload points and verify recovery. Immix does not shrink commitment after
reclamation; LOS payload decommit/reuse remains real.

**The standard GC hard-limit contract includes bookkeeping. LXR does not
implement that total-GC contract and explicitly rejects its entire family:**
absolute/percent and SOH/LOH/POH variants. It must not silently reinterpret
`GCHeapHardLimit` as managed bytes. Instead:

| Setting | Default | Meaning |
|---|---:|---|
| `DOTNET_LXRImmixInitialMB` / `LXRLOSInitialMB` | 8 / 16 | Initial admitted capacity |
| `DOTNET_LXRImmixReserveMB` / `LXRLOSReserveMB` | 128 / 256 | Stable reservation ceilings |
| `DOTNET_LXRManagedHeapHardLimitMB` | Reservation total | Managed admission ceiling, not bookkeeping-inclusive |
| `DOTNET_LXRNativePlaneLimitMB` | 4096 | Committed engine/flag/log plane ceiling |
| `DOTNET_LXRLegacyCapacity` | 0 | Reservation equals initial capacity for old boundary/reuse checks |
| `DOTNET_LXRFastAllocation` | 1 | Genuine helper path; 0 is an explicit slow-path comparison control |

The native-plane budget excludes side metadata, packets, handles, mutator
chunks, native stacks and runtime/library memory. Total process working set
and private memory are measured independently. Neither a managed arena size
nor the plane counter is advertised as total process/native memory.
The 1024 simultaneous-context, 131072 copied-root and 256 frozen-lifetime
limits remain unchanged. No admission proof was silently widened.

LXR numeric environment knobs are **decimal**, unlike the stock GC's
hexadecimal environment convention. Public `System.GC.LXR...` runtimeconfig
numbers are decimal; Booleans accept 0/1 or true/false. Empty, malformed,
negative, overflowing, conflicting and out-of-range settings fail startup.
Standard hard-limit settings fail explicitly rather than acquire a different
base or meaning. Limits are startup contracts; refresh remains unsupported.

## P8 policy and nonmoving adaptation

The coupled oracle remains binding
`0682434ae725787e38defbc17bd66dc918e4bdb7` / core
`304ce69d43aae87de501111fceb8cbd33173a03a`. Reads used revision-qualified
`git show`/`git grep`: `src/plan/lxr/mod.rs`, `global.rs`, `rc.rs`,
`src/args.rs`, and Immix allocation accounting. The extended
arXiv:2210.17175 paper (1 November 2022) and historical GroupA/B ledgers remain
the same basis; PLDI and secondary/simplified trees did not replace HEAD.

Survival formulas preserve default 3:1 weighting, weighted rising/falling
bias, harmonic alternatives, clamping, zero allocation, and Rust `f64::min`
NaN behavior. Allocation volume is fresh nursery blocks plus the reference's
page-rounded recycled-line accounting, captured at pause start; prediction
updates before resume. **The reference feeds only copied promotions. P6-off
therefore has zero actual copied volume. This is a P6-dependent/dormant
copy-survival estimate, not measured young/in-place survival prediction.**
Live-heavy growth/hard-limit controls demonstrate that this estimate cannot
override actual allocator capacity, commitments or OOM.

Mature-live prediction updates only after real Full/FinalMark lazy completion,
at 3:1 current/previous weight. The overall-drained callback retires tracing,
then feeds prediction/hints; the next selector waits for that same generation.
Ordered triggers are heap/space full, exhausted SATB, survival, noncontiguous
space availability, increments, clean nursery cap, total young cap. This
collector's reservation is contiguous; that oracle branch remains tested but
is not falsely claimed as an active noncontiguous host mechanism.

Selection finishes an exhausted in-flight cycle first, then handles emergency/
user/emergency-hint requests, then cycle hint, otherwise RC. Forced requests
finish an in-flight trace with a blocking FinalMark rather than inventing a
new Full counter; the OOM fixture's assertion counts both truthful full-cycle
completion kinds. Blocking forced completion is not misreported as background
completion. Explicit GC.Collect remains real, never an ignored hint.

| `DOTNET_` LXR setting | Reference default / implemented behavior |
|---|---|
| `LXRTraceThreshold`, `LXRRCStopPercent`, `LXRMaxSurvivalMB` | 20, 15, 128 |
| `LXRSurvivalPredictorWeighted`, `LXRSurvivalPredictorHarmonicMean` | 0, 0; weighted takes precedence |
| `LXRIncrementsLimit`, `LXRNurseryBlocks`, `LXRYoungLimitMB` | Absent means unbounded |
| `LXRNurseryRatio` | Optional hard-budget block partition |
| `LXRMinReuseLines` | 1, the exact args.rs default rather than the old prototype's 2 |
| `LXRBufferSize` | 1024; logical 256/512 also supported within preallocated 1024-entry storage |
| `LXRLazyDecrements` | 1; 0 routes decrement work through enabled STW stages |
| `LXRNoLineRecycling`, `LXRNoMutatorLineRecycling` | 0; disable hole reuse when requested |
| `LXRRCBits` | Existing prototype 8; selectable 2/4/8 |

Moving evacuation/defrag/max-evacuation/pause-bound settings and transparent
huge-page requests fail closed when enabled. They do not expose the reference's
enabled moving defaults as ignored switches. P6, defrag headroom and platform
huge-page realization are explicit deviations/deferred work.

## Verification, measurement and reproduction

On **both Windows/Linux x64**, final Debug and Release native runs pass
30979 original integrated checks, 1952 fast-span/growth/commit-failure checks,
45 predictor/trigger/selection/real post-lazy integration checks, and 8358
focused trace checks. Managed Debug/Release each pass 100627 lifetime checks,
10 old-capacity OOM/recovery checks, and 32 additional managed birth/helper/
simultaneous scan-allocation checks. Each OS passes 31 actual environment/
runtimeconfig JSON controls. Wrong predictor updates and trigger order, missing
SATB/deletion marking, early root decrements and sweep payload queries fail
deliberately. Original P0-P5 evidence was not overwritten.

One integrated review and one bounded follow-up found and fixed bookkeeping
contract misrepresentation, LOS free-tail admission, typed config/base parsing,
fixed-boundary small-heap classification, VMA-based residency understatement,
minimum affordable growth and the standard diagnostics flag. The real post-lazy
hook gained decisive integration assertions. Build/test failures, malformed
commands, symbol mismatch, unsuccessful GDB sampling, the Debug naming failure,
the invalid standard-limit preflight and interrupted pre-final study are retained
under `artifacts/P8`; none is counted as a passing measurement.

The coordinating session independently replayed 13 correctness processes per
OS against the same frozen source/modules/portable fixture, confirming both
managed configurations, storage/policy, actual allocation hits, retained
growth and the decimal 64 MiB managed ceiling. It also recomputed all 432
scheduled matched/growth rows and 48 summary groups from the raw CSVs.
These are independent acceptance/consistency checks, not extra timing samples.

`run-vertical.py` predeclares rotations, A/B replicates and a 200 ms injected
control, verifies identical runtime/CoreLib/JIT/CoreRun, checks checksums and
collector identity, preserves every process, and never retries samples.
Throughput compares P5 versus changed LXR at explicit old 24 MiB capacity;
the fast-off arm uses the same changed binary/policy. Built-in characterization
uses that same runtime, but is not labelled equal-capacity heap fairness.
Growth is a separate larger-live-set study; baseline capacity failures remain
explicit controls rather than excluded successful timings. Linux residency is
page-granular `mincore`; Windows uses `QueryWorkingSetEx`. Non-owned residency
includes collector metadata and runtime/native/code/stack memory.

### Targeted P9.3 results

Normal medians, six samples per arm; milliseconds. All checksums/operation
counts match. Allocation is 524288 escaping objects; mutation is 524288
coalescible scalar stores after setup; automatic allocation is 128 MiB of
array churn; forced workload preserves eight explicit full calls. These are
whole-collector workloads, not isolated nanosecond barrier benchmarks.

| OS / workload | P5 LXR | P8 legacy | P8 fast off | WKS | Server |
|---|---:|---:|---:|---:|---:|
| Windows allocation | 392.233 | 128.238 | 144.570 | 10.911 | 11.101 |
| Windows mutation | 3.498 | 3.376 | 3.341 | 3.237 | 3.304 |
| Windows automatic | 2656.820 | 270.487 | 278.254 | 23.678 | 33.727 |
| Windows forced | 707.642 | 414.246 | 409.308 | 12.599 | 15.936 |
| Linux allocation | 551.888 | 194.177 | 248.104 | 14.870 | 15.183 |
| Linux mutation | 3.947 | 3.863 | 3.928 | 3.441 | 3.623 |
| Linux automatic | 3590.761 | 438.658 | 456.689 | 29.102 | 50.502 |
| Linux forced | 1164.668 | 420.855 | 452.532 | 15.963 | 19.806 |

The combined vertical reduces allocation elapsed time by about 67%/65%,
automatic churn by 90%/88%, and forced-cycle workload by 41%/64% on
Windows/Linux. Automatic frequency falls from approximately 152 P5
collections to 27, so that result is explicitly **not** wholly fast-path
attribution. The same-binary fast-off comparison characterizes an additional
allocation improvement of about 11%/22%. It does not independently isolate
every other optimization. Mutation differences are small/noisy and are not
claimed as a barrier convergence win. LXR remains substantially slower and
larger than the built-ins.

The preserved original P5 timed DLL was also replayed unchanged. Windows
LXR/WKS/Server medians are 407.529/10.653/13.961 ms; Linux
476.783/11.995/15.945 ms. Normal LXR still allocates about 6292480 bytes,
performs eight explicit collections, copies real RC entries and returns
checksum 49152. Relative to the fresh P5 replay, LXR improves about 46%/59%;
it remains about 38x/40x WKS elapsed time on that original workload.

The changed scoped Linux CPU profile captures 1116 samples, zero lost,
with timed section 478.206 ms versus the baseline profiled 1231.661 ms.
The wakeup leaf falls from 24.39% to 5.11% of sampled whole-process CPU.
Dense walking/statistics/sweep are no longer leading leaf costs. Remaining
leaders include demand-zero page work (14.70%), metadata Load (8.24%),
nursery classification (3.58%), NewWork/GetLocation/Store (3.05% each),
and malloc (2.51%). Shares are not additive phase timers, and higher relative
shares do not establish higher absolute cost. The profile identifies residual
metadata/initialization/trace-packet costs rather than claiming convergence.

Managed codegen includes `CORINFO_HELP_NEWSFAST`; source selection is the
unchanged accepted P1.4 notifying family. The measured allocation arm records
523248 genuine fast births out of 524288 operations (about 99.8%). The
fast-off arm records zero. No Linux DisassemblyDiagnoser was used.

### Targeted P9.4 results and tradeoffs

The live-heavy workload retains 1200 x 16 KiB arrays plus 40 x 1 MiB arrays.
P5 cannot complete it: its retained partial capacity-control failures are
not compared as throughput wins. P8 completes all 1240 allocations through
five growth steps on each OS, with 384 MiB reservation, 128 MiB admitted
capacity, approximately 104.2 MiB actual managed commitment and
reported live bytes of about 59.1 MiB Windows/58.9 MiB Linux.
Its growth medians are 336.506/936.193 ms.

| OS / collector | Working set MiB | Private memory MiB | Managed committed MiB | Native planes MiB |
|---|---:|---:|---:|---:|
| Windows P8 | 636.8 | 1252.2 | 104.2 | 1084.7 |
| Windows WKS | 50.8 | 74.0 | 59.6 | Not exposed |
| Windows Server | 61.6 | 91.8 | 73.3 | Not exposed |
| Linux P8 | 633.7 | 1331.0 | 104.2 | 1084.7 |
| Linux WKS | 52.7 | 131.2 | 59.2 | Not exposed |
| Linux Server | 62.7 | 157.5 | 72.1 | Not exposed |

P8's actual owned-page residency is about 78 MiB; roughly 558/556 MiB of
resident memory is outside the managed reservation. Native plane commitment
is a major scalability cost. It is bounded/admitted rather than eagerly
scaled to maximum reservation, but is still **far from footprint parity**.
The growth controls expose this tradeoff instead of equating arena size with
RSS. Fixed-capacity allocation-arm private memory decreases from about
290.7 MiB P5 to 250.6 MiB P8 on Windows; larger live sets still magnify
native overhead.

A/A deltas and every injected result are retained in summary/raw CSVs.
The 200 ms injected control is inconclusive for the slow P5 automatic arms,
Linux P5 forced arm and Linux P8 growth; Linux growth/Server variability is
notable (A/A about 10%/20%). No threshold was widened, sample retried or
failure removed to change those outcomes. Short mutation differences cannot
support small-effect precision claims.

An initial Windows matrix left an uppercase inherited GCPath behind in
case-sensitive Python cleanup and falsely labelled external-collector rows
as built-ins. **All 180 rows are rejected and retained**, not salvaged.
Case-insensitive cleanup, independent managed GCPath/GCName/category checks,
and a deliberately contaminated-label failure control precede the full
corrected matrix. Nine mixed-case DOTNET_/COMPlus_ controls demonstrate the
old sanitizer's defect and the corrected behavior.

Results and exact module/input identities are in the compact CSV records in this
folder; full commands, process logs, profiles and layouts remain in
`artifacts/P8` and `/root/lxr-p8-speed-scale/artifacts/P8`. These targeted studies
do not establish an SLA, paper parity, production tail latency or convergence.

The already-loaded build-and-test/performance skills were used. Exact SDK,
libraries and accepted runtime layouts were reused; native configuration was
performed once per OS/configuration, followed by incremental **clrlxrgc only**
builds. No alljits/CoreLib/libraries matrix was restarted, no dependency changed,
and no audit was disabled or warning demoted beyond the authorized audit rule.

Representative commands (use Windows separators in PowerShell):

```powershell
& $env:ComSpec /d /s /c 'call src\coreclr\build-runtime.cmd x64 release -configureonly -cmakeargs "-DCLR_CMAKE_ENABLE_LXR_GC=1"'
$vs = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\VC\Auxiliary\Build\vcvars64.bat'
$cmake = 'C:\Program Files\Microsoft Visual Studio\18\IntPreview\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe'
$build = Join-Path $PWD 'artifacts\obj\coreclr\windows.x64.Release'
& $env:ComSpec /d /s /c "call `"$vs`" >nul && `"$cmake`" --build `"$build`" --target clrlxrgc --parallel 8"
.\.dotnet\dotnet.exe build docs\design\lxr-port\P5\runtime-tests\runtime-tests.csproj -c Release
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -Configuration Release -OutputDirectory artifacts\P8\record-native-Release-windows
artifacts\P8\record-native-Release-windows\lxr-rc-validation.exe --p8-storage
artifacts\P8\record-native-Release-windows\lxr-rc-validation.exe --p8-policy
```

```bash
bash src/coreclr/build-runtime.sh x64 Release -configureonly -cmakeargs '-DCLR_CMAKE_ENABLE_LXR_GC=1'
cmake --build artifacts/obj/coreclr/linux.x64.Release --target clrlxrgc --parallel 8
bash docs/design/lxr-port/P4/run-lxr-rc-vertical.sh Release "$PWD/artifacts/P8/record-native-Release-linux"
artifacts/P8/record-native-Release-linux/lxr-rc-validation --p8-storage
artifacts/P8/record-native-Release-linux/lxr-rc-validation --p8-policy
```

Repeat native commands with Debug. Managed runs use the corresponding verified
framework's CoreRun, exact GCPath, ReadyToRun/tiering off, and
`DOTNET_LXRLegacyCapacity=1` for the original suites/OOM boundary. Growth runs
clear legacy capacity. The configuration driver uses a separately copied
same-source dotnet/hostfxr layout with the actual Release framework.

```text
python docs/design/lxr-port/P8/run-vertical.py --baseline <preserved-framework> --changed <changed-framework> --test <matched-fixture> --output <throughput.csv>
python docs/design/lxr-port/P8/run-vertical.py --suite growth --baseline <preserved-framework> --changed <changed-framework> --test <matched-fixture> --output <growth.csv>
python docs/design/lxr-port/P8/verify-configuration.py --framework <changed-framework> --json-host <same-runtime-host-root> --test <fixture> --output <controls-directory>
```

No push, PR, issue/comment, EgorBot, merge to `lxr`, main-checkout access,
abandoned runtimelab access, replacement implementation session or moving
collector implementation was performed.
