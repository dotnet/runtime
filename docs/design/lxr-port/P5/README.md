# P5: selectable LXR host, SATB tracing and cycle reclamation

This is one integrated, opt-in **nonmoving prototype**, not a renamed stock GC,
an observer, a production collector, or an implementation of P6 evacuation.
`clrlxrgc.dll` / `libclrlxrgc.so` implements `IGCHeapInternal`, loads through the
existing GCPath contract, uses the real handle manager, and owns managed memory
through `LxrReferenceCounting`. Neither WKS nor Server allocation/collection is
constructed by this module.

## Source and oracle identity

The implementation commit is
`ac1da112dd84639e6c369bec999e7178d0842a53`, on top of accepted P4
`fe93f7801dab5a7ead6060b763078aa59be160ef`. The input manifest
[`validated-inputs.csv`](validated-inputs.csv) binds the existing P4 inputs plus
the integrated host/engine/fixtures/study runner. All 47 entries were compared on the Windows
worktree and the native Linux source snapshot. Later test/record changes are
identified by that manifest and this record's containing commit; they do not
change the collector implementation.

The retained oracle is the coupled HEAD binding
`0682434ae725787e38defbc17bd66dc918e4bdb7` / core
`304ce69d43aae87de501111fceb8cbd33173a03a`. Reference reads used revision-qualified
`git show`, not checkout files:

| Oracle source at that core commit | Mechanism used |
|---|---|
| `src/plan/lxr/cm.rs` | `ProcessModBufSATB`, `LXRConcurrentTraceObjects`, retained packet ownership |
| `src/plan/lxr/rc.rs` | `ProcessDecs`, RC death and tracing interaction |
| `src/plan/lxr/global.rs`, `gc_work.rs` | pause schedules, pause-end marking state, release/trace ordering |
| `src/policy/immix/rc_work.rs`, `block.rs`, `immixspace.rs` | dead-cycle sweep and block/line lifetime |
| `src/policy/largeobjectspace.rs`, scheduler, `lib.rs` | LOS and concurrent packet accounting |

These are the flat HEAD plan paths. The tracing bodies are not commented out
or gated behind `nogc_no_zeroing`; additional sanity assertions are not confused
with the default mechanism. No new MMTk application measurements were performed.
The extended paper, arXiv:2210.17175, 1 November 2022, supplies the mandatory
mark-and-scan-on-delete invariant. The historical PLDI comparison tuple and P0
ledger remain unchanged; neither the secondary `9625c174` tree nor the simplified
branch replaces HEAD. Paper pause tables are not application-latency evidence.

## Host and storage lifetime

The new CMake switch is `CLR_CMAKE_ENABLE_LXR_GC`, **OFF by default**, restricted
to standalone Windows/Linux AMD64 CoreCLR. The module reports GC interface
**5.15**, build **5**, name **LXR nonmoving prototype**. Existing single-heap
`GC_HEAP_WKS` classification is EE compatibility, not WKS execution. The
`clrlxrgc` source list excludes the stock collector and the P4 managed observer.
There is no new VM/JIT/GC-EE entry/admission interface.

The engine reserves a contiguous 8 MiB Immix + 16 MiB LOS arena. Physical
allocation/ObjHeader starts are `Object* - 8`; managed references and logged
slots remain actual `Object*` / `Object**` values. Complete extents are aligned,
at least 24 bytes, with inclusive LOS admission at 32768 bytes. Normal,
large and pinned/POH allocations use these arenas. The VM receives no
unregistered fast allocation spans: its allocation pointers stay null, and the
existing allocation-complete callback publishes headers/extent metadata before
managed publication. Empty-string raw size 22 is aligned rather than rejected.

Real stack, static, thread-static, interior, pinned and handle roots are copied
from the EE. Callback-local root slots are never retained. Interior containment
uses exact birth extents. Nonmoving pinning is not blanket liveness: pinned
objects still need RC/tracing reachability. Collectible types include their
LoaderAllocator edge. Frozen objects remain EE-owned, are classified for EE
validation, and have outgoing owned references enumerated. Frozen reader
leases protect unregister; at most 256 lifetime records are admitted, including
retired address classifications. Their payload is not retained or reread after
unregister.

Strong/pinned/variable/ref-counted handles use the real handle table.
Nontracing RC leases protect weak/dependent/finalizer/SyncBlock targets until
suspended tracing can resolve their lifetimes. FinalMark performs dependent
fixed-point closure, short weak clearing, eager wrapper finalization, pending
finalizer tracing, another dependent closure, then long weak/dependent and
SyncBlock cleanup. Normal finalizers precede critical finalizers; suppression,
re-registration and resurrection are exercised. Finalizer cursors amortize
native flag-plane scanning and are rewound only during suspended queue
construction, while consumption is cooperative.

### First-write admission without a VM hook

Every admitted mutator has two preallocated 1024-entry chunks. A completed or
partial chunk is copied into a separately preallocated epoch stream:

1. A CAS reserves a disjoint range. Reservation **does not publish entries**.
2. `memcpy` fills the range; a release completion token publishes its length.
3. Consumers acquire each token and reject holes/invalid ranges. They never
   consume the reservation frontier as initialized payload.

Only after publication is the per-mutator chunk recycled. RC and SATB packets
own independent copies. Pause/thread-exit flush, bank flip, record retirement,
packet completion and block/LOS release retain their separate ownership
boundaries. Epoch banks are reset while suspended, after prior ownership drains;
mutator records are not reused while readers can name their storage.

This removes the **per-epoch identity attachment cap**, not finite storage or the
**1024 simultaneous-context cap**. A first barrier can attach a preallocated
record with atomics, without malloc, GC, a mutex, or a blocking fallback. Existing
`FixAllocContext` flushes before TLS cleanup; allocator-side admission can
complete an epoch and retry. Beyond the supported simultaneous cap, the
prototype explicitly fails with a diagnostic rather than dropping a store.
The supported workload must stay below that cap; arbitrary oversubscription
is not claimed. More than 1024 sequential identities are supported and tested.

The once-per-owned-slot bound applies only to the claimed scalar/CAS/bulk slot
protocol. Failed CAS and bulk stores participate in that protocol. Native
strong/weak/dependent/ref-counted handle observations are separately bounded
copied root batches, not falsely charged to owned-slot uniqueness. The existing
host root-batch limit is 131072 targets, including lifetime leases. The existing
native collectible-edge callback admits newborn MethodTable initialization;
native metadata-edge replacement/foreign destinations fail explicitly. No
unbounded, unclaimed native old-target stream is silently admitted.

## The whole trace path

| Constituent | Implementation and lifetime boundary |
|---|---|
| P5.1 InitialMark | Incremented, copied tracing roots seed work. Concurrent marking turns on at pause release, before actual EE resume, not at pause start. |
| P5.2 concurrent trace | P3 `ConcurrentResumable` packets scan real GCDesc ranges, with 1024-reference slices, child/continuation accounting, atomic once-processing and retained work ownership. Interrupted work is preserved. Exhaustion requests FinalMark. |
| P5.3 RC/SATB safety | Independently copied SATB old targets are processed before decrement work. Dying unmarked objects are marked; children are forced into trace before zero/reuse, and any partial scan is finished under its object lock. |
| P5.4 FinalMark/full | Flush and clear marking while suspended, rescan real roots and complete required lifetime closures. Explicit/emergency full GC finishes an in-flight trace. Evacuation/remset work is empty/deferred, not reported as P6. |
| P5.5 cycles/sticky garbage | Sweep unmarked nonzero/sticky objects using extents and metadata, without parsing dead object layouts or recursively decrementing dead-cycle edges. Block/LOS reuse waits for trace and lazy-reader ownership. |

P3 completion retains the reconciled order: seal while suspended, actual EE
resume, record resume, publish deferred work, drain completion, then retire/reset.
`WaitForBucketIdle(Unconstrained)` additionally closes the interval between
completion notification and the scheduler retiring its executing packet.
Pending-only trace drainage avoids repeatedly walking already processed RC work.

Collection pressure uses a minimal fixed epoch budget, not P8 prediction.
`GC.Collect` is a real blocking full LXR trace. Exhaustion completes tracing,
retries allocation, and returns managed OOM when capacity is genuinely
insufficient. Concurrent waits honor timeout instead of returning unconditional
success. Memory-info snapshots distinguish blocking/background kinds and retain
both InitialMark/FinalMark pause durations; no ephemeral collection is invented.
Statistics are pause-end snapshots, not a stock generational/DAC layout.

Shutdown drains native workers on the normal path and retains managed
reservations until process exit. Windows DLL detach checks the same
`RtlDllShutdownInProgress` condition used by the VM: after the OS terminated
peer threads, it does not wait on their unsignaled completion events or unmap
managed memory.

## Verification

The primary configuration is Windows/Linux x64 Debug runtime with the portable
managed fixture compiled Release. SDK, unchanged Release libraries and layout
outputs were reused from the verified baseline. Linux ran from
`/root/lxr-p5-1205f569`, not a mounted Windows build tree. Audit checks remained
enabled; only NU1903/NU1904 warning demotion was authorized. No dependencies,
NativeAOT implementation, main checkout, abandoned runtimelab tree, or earlier
raw evidence were changed.

Final Debug results, after the last collector edit:

| Evidence | Windows | Linux |
|---|---:|---:|
| Real GCPath selected-host managed checks, Debug and Release | 100627 each | 100627 each |
| OOM/emergency trace/recovery checks, Debug and Release | 10 each | 10 each |
| Integrated native Debug checks | 30979/30979 | 30979/30979 |
| Integrated native Release checks | 30979/30979 | 30979/30979 |
| Focused trace checks | 8358/8358 | 8358/8358 |
| Missing SATB / missing deletion-mark controls | failed as intended | failed as intended |
| Early-root / sweep-payload-query controls | failed as intended | failed as intended |

The managed suite checks owned startup allocations and GC.Collect execution;
acyclic heap plateau and reclaimed bytes; unreachable two-node/self/sticky
cycles; reachable cycles, strings, arrays, pinned/interior LOS checksums;
weak/finalizer resurrection; dependent closure; 2048 actual thread exits and
barrier-first bootstrap; failed-CAS/bulk SATB overflow; collectible metadata;
external ComWrappers ref-counted roots; and a SyncBlock-only referent with no
remaining handle-table lease. Dependent tests include chained fixed-point
promotion and an unreachable key/value back-edge, not just a reachable key.

Actual managed range reuse is also checked, independently of live-byte counters:
two cohorts of 96 x 100000-byte arrays exceed the 16 MiB LOS reservation in total.
The first cohort's exact owned extents become zero after collection. The second
cohort's still-retained, checksum-checked extents overlap the recorded reclaimed
ranges, and released LOS bytes increase by at least 9600000.

| Final managed run | Allocations | Allocated bytes | Reclaimed bytes | Collections / full traces |
|---|---:|---:|---:|---:|
| Windows Debug | 184390 | 26203304 | 16240120 | 113 / 76 |
| Linux Debug | 111740 | 24451928 | 14710704 | 121 / 83 |
| Windows Release | 104556 | 24258472 | 14295656 | 110 / 75 |
| Linux Release | 107590 | 24107424 | 14365992 | 120 / 83 |

The final counters include the second cohort before its later collection;
remaining live bytes are not a leak assertion. The separate acyclic plateau
check compares equivalent completed phases.

The active-reader control is not merely a held, unstarted trace. A selected
100000-reference array reaches a checkpoint **after a real concurrent scan
slice**, outside its object lock. A real managed writer completes stores while
that packet remains executing. The RC pause releases/intercepts the checkpoint,
quiesces the worker and preserves continuation work; full GC then verifies all
array values and the completed wait. A zero-timeout wait during the checkpoint
returns `0x800705B4`, not success.

The old P4 22459-check run was retained. Before the ingress change, the new
first-store/churn control failed (72/73); the fixed engine passed 22621 aggregate
and 162 focused checks on each OS before further trace cases were added.
Final native coverage includes all RC widths, exact-address stale-lease reuse,
black-newborn/white-child closure, poisoned dead layouts, LOS and root leases.
The SATB failure control specifically loses a still-sticky old target, so
mark-on-delete cannot accidentally mask missing SATB consumption.

One integrated review and one bounded follow-up found and repaired:
stale weak/dependent root-bank leases decrementing address replacements;
allocation-black newborns hiding older white children; ref-counted handles
skipping lease renewal; and SyncBlock-only objects dying before weak cleanup.
The follow-up corrected the SyncBlock callback's `ScanContext*` argument and
removed a WeakReference that had masked the managed regression. All affected
native/managed cases were rerun. Linux timeout failures were retained; stack
samples identified repeated full-list trace drainage and repeated flag-plane
finalizer scans, rather than being discarded as passing runs.

### Reproduction commands

Run from this source tree. On Windows, `R:` below was an owned temporary short
mapping to this worktree, not the main checkout.

```powershell
# Existing Debug baseline, once:
.\eng\build.ps1 -Subset clr+libs+host -Arch x64 -RuntimeConfiguration Debug -LibrariesConfiguration Release -HostConfiguration Debug -CMakeArgs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' -WarnNotAsError 'NU1903;NU1904'
# Enabled incremental product:
.\eng\build.ps1 -Subset clr.runtime -Arch x64 -RuntimeConfiguration Debug -CMakeArgs '-DCLR_CMAKE_ENABLE_LXR_GC=1' -WarnNotAsError 'NU1903;NU1904'
.\.dotnet\dotnet.exe build docs\design\lxr-port\P5\runtime-tests\runtime-tests.csproj -c Release
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -RepositoryRoot R:\ -OutputDirectory R:\artifacts\P5\native-final-Debug -Configuration Debug
# Repeat native runner with -Configuration Release and a distinct output.
# Controls use -Control TraceSatb, TraceDeletion, EarlyRoot, or SweepQuery.
```

```bash
./eng/build.sh --subset clr.runtime --arch x64 --runtimeConfiguration Debug --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_GC=1' --warnNotAsError 'NU1903;NU1904' /p:UseNativeAotForComponents=false /p:RepositoryCommit=ac1da112dd84639e6c369bec999e7178d0842a53
bash docs/design/lxr-port/P4/run-lxr-rc-vertical.sh Debug "$PWD/artifacts/P5/native-final-Debug"
# Release is the first argument for optimized native tests.
# Third argument selects TraceSatb, TraceDeletion, EarlyRoot, or SweepQuery.
```

For an empty Linux layout, the supported CoreCLR-only baseline is
`clr.runtime+clr.corelib+clr.nativecorelib+clr.tools+libs+host` with
`/p:UseNativeAotForComponents=false`. This run reused its previously verified
SDK/libraries/layout, rather than repeating that baseline per milestone.
The missing Release runtime configuration was attempted once per OS:

```powershell
# Native compilation passed; CoreLib crossgen failed, exit 1 (repair recorded below).
.\eng\build.ps1 -Subset clr.runtime+clr.corelib+clr.nativecorelib+clr.tools -Arch x64 -RuntimeConfiguration Release -CMakeArgs '-DCLR_CMAKE_ENABLE_LXR_GC=1' -WarnNotAsError 'NU1903;NU1904'
```

```bash
# Passed, exit 0.
./eng/build.sh --subset clr.runtime+clr.corelib+clr.nativecorelib+clr.tools --arch x64 --runtimeConfiguration Release --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_GC=1' --warnNotAsError 'NU1903;NU1904' /p:UseNativeAotForComponents=false /p:RepositoryCommit=ac1da112dd84639e6c369bec999e7178d0842a53
```

Linux archive builds have a native version stamp
`N/A`; `/p:RepositoryCommit` is not falsely presented as changing that stamp.
The qualified input hashes bind the actual source; binary hashes bind the
actual modules. Windows Debug's embedded stamp is P4 because those binaries
were built before the local implementation commit.

Copy just-built CoreCLR/JIT/LXR native libraries into the **actual**
`artifacts/bin/testhost/net11.0-<os>-Release-x64/shared/Microsoft.NETCore.App/11.0.0`
framework directory (Windows separators as appropriate), skipping identical
hardlinks. Use that directory's `corerun`, not a different installed runtime:

```powershell
$env:DOTNET_GCPath = "$framework\clrlxrgc.dll"
$env:DOTNET_ReadyToRun = '0'
$env:DOTNET_TieredCompilation = '0'
& "$framework\corerun.exe" $testDll $env:DOTNET_GCPath
& "$framework\corerun.exe" $testDll --oom $env:DOTNET_GCPath
```

Linux uses the same environment and absolute paths to `corerun`, the portable
test DLL and `libclrlxrgc.so`, with bounded `timeout 120s` / `60s` runs.
`DOTNET_ReadyToRun=1` fails initialization (Windows exit -1, Linux 255).
`--unsupported-loh` fails explicitly rather than pretending to compact
(Windows -2147467259, Linux 134). A real Linux CMake configure with
`-DCLR_CMAKE_TARGET_ARCH=arm64 -DCLR_CMAKE_ENABLE_LXR_GC=1` failed at the selectable
collector's AMD64 guard, exit 1. No unsupported-target binary was built.
Default WKS/Server runs clear GCPath; both Release caches had
`CLR_CMAKE_ENABLE_LXR_RC_VERTICAL=OFF`, so the managed observer is not evidence
for these runs.

## Characterization and final identities

The first Windows Release command compiled the native runtime but **failed**
CoreLib crossgen with `DllNotFoundException` (exit 1). Its original log/status
remain failed. The ordinary `clr.jit+clr.nativecorelib` retry also failed:
Windows `jit` does not install the standalone cross-JIT. The authorized narrow
repair was:

```powershell
.\eng\build.ps1 -Subset clr.alljits+clr.nativecorelib -Arch x64 -RuntimeConfiguration Release -CMakeArgs '-DCLR_CMAKE_ENABLE_LXR_GC=1' -WarnNotAsError 'NU1903;NU1904'
```

This succeeded, exit 0, reusing the compiled runtime/libraries. There was no
matrix restart, package/source change or wider warning demotion. Crossgen2 was
then observed live loading
`R:\artifacts\bin\coreclr\windows.x64.Release\x64\crossgen2\clrjit_win_x64_x64.DLL`.
Its hash matched the root Release artifact:
`75a7d4ecf8745e0eb0891cdd6e99cab61f9199133822bcdb3f3dcb10decd17cd`,
native version `42,42,42,42424 @Commit: ac1da112dd84639e6c369bec999e7178d0842a53`.
The identity compilation exited 0. The Debug DLL was not substituted.
The independent Linux Release command succeeded, exit 0 (six audit warnings,
zero errors); it was not restarted for the Windows repair.

Exact loaded LXR module hashes (the banner also verifies initialized heap state):

| Module | SHA256 |
|---|---|
| Windows Debug `clrlxrgc.dll` | `41728de81b8dc0e7b68cde983a81e00c7b559c534cf77a6019b119ae01368777` |
| Linux Debug `libclrlxrgc.so` | `99dd574bbb6cdf4d59968ea750f5a6293da8cf30f4a268567fc3c56460eaa338` |
| Windows Release `clrlxrgc.dll` | `2553db4d8a2609e905688adc4187734175952ee3c30cf3a4f49a8fd75a286e99` |
| Linux Release `libclrlxrgc.so` | `a9b9a9d9d9cbd969cc14a75fd8513c0befae6da07494b64ca3b7ff5ce2d88eba` |

Runtime/library identities used by the three collectors in each study:

| Input | Windows Release SHA256 | Linux Release SHA256 |
|---|---|---|
| CoreCLR | `db98fe95afc5ad71b94a71820b8a816c63ac7ce21afe04b1bd2d23fda852fb3d` | `e0f2e01f19eeee42aa510bc969fc196d7c3e0973396962fd9e04f5914d1ba56f` |
| JIT | `c1168bf4f7dbc27577fe7ce949db33fcf9e60eb88dd58cd46d71f6ddc354147a` | `79955ab1900b3c7ac257effdddca4f94b4721bf0b4f0e09a7eef2fcc09e6b855` |
| CoreLib | `560b984cb96642899272beb13ffe636760e944267dbf5a2110922ec1885fcb37` | `56f321a1e4aeebc30bccf969d69ca0c94437e8cffaa7dbf1084da3c7fb7ce582` |
| CoreRun | `ed48678ae69c1f4f400ccab2fe0702896e7287dc617c852979de0fcd0f6f91e4` | `c7fc0b8f599016b85f6a34b117d565b8d0ad5d02b3f3ad489f9d416b753dbbb2` |

Debug CoreCLR/CoreLib hashes are respectively
`29cb7f57032a947a102fd7c11540f8a9e9abaeac6578ec17263de0023dcb90f4` /
`d88fbea02998ab8d660c071e1b6e01ad50439ef09fb782fbeb660b9a61ac4f93`
on Windows, and
`09cf0aa213e17a5d580e4fa0804d2f8444507349b5bc324ccf0ce723b9db20d0` /
`0dba8d2542176904b4501b41d268c468a2fa75e1ceebc769afa7e90e4d4474de`
on Linux.

The fixed study runner is [`run-managed-study.py`](run-managed-study.py). It
uses the same modified CoreCLR, CoreLib, libraries and workload for LXR, WKS and
Server, rotates collector order, records three A/B normal pairs per collector,
and injects 32 x 20 ms sleeps as a deliberately large sensitivity control.
It never retries/selects samples. R2R and tiering are off, concurrent GC is on,
and the same heap-count setting 2 is supplied (WKS/LXR remain single-heap).
Timing excludes module hashing/setup and includes the fixed allocation,
mutation, cycles and eight explicit full collections. Checksum is exactly
49152, with allocation and copied-RC-entry counts retained. Each OS completed
27 processes, all exit 0. Per normal process there were eight explicit full
collections and about 6292480 allocated bytes (Windows LXR recorded 192
additional runtime bytes). LXR consumed 255295 copied RC entries on Windows,
254927 on Linux; this is actual flat-stream traffic, not a zero-copy claim.

| OS / collector | Normal median ms, n=6 | A/A median delta | Injected median ms | Sensitivity |
|---|---:|---:|---:|---|
| Windows LXR | 766.178 | +2.69% | 1714.462 | detected |
| Windows WKS | 11.095 | +5.47% | 995.295 | detected |
| Windows Server | 14.471 | +0.53% | 999.900 | detected |
| Linux LXR | 1209.061 | -1.54% | 1879.118 | detected |
| Linux WKS | 12.631 | +2.67% | 663.770 | detected |
| Linux Server | 17.148 | -5.59% | 674.419 | detected |

**LXR is substantially slower in this workload**, not a performance improvement.
Dense metadata work, slow-only managed allocation and untuned collection are
prototype costs. Windows sleep quantization makes the injected delay closer
to a second than the nominal 640 ms. The large injected effect is detectable;
A/A spread does not establish sensitivity to small changes. No tuning or
repeat-until-green run was done.

For cross-OS study identity, both studies deliberately used the same preserved
100146-check harness DLL:
`40b5552013d4793f7bd44ccb5eb1b4afde245fcbb85444d7c3c8834d22e963f8`,
source snapshot SHA256
`4a6f9f94d89b3b00a0e6be6c31e25a5220743a80ab93c4d2eaa058ba1d4d09af`.
Those exact bytes remain in `artifacts/P5/study-harness`. The later range-reuse
case was not added to the timed path; `StudyBody` and inputs are unchanged.
The final 100627-check correctness DLL is
`2d75edd3f943df4170b64b07c9c4e7412ce95029450feef01a07c1f3e82396d1`,
with final fixture source hash in the input manifest.

Run the fixed characterization on a separate Release framework layout:

```text
python docs/design/lxr-port/P5/run-managed-study.py --corerun <corerun> --test <study-dll> --lxr <LXR-module> --coreclr <CoreCLR-library> --corelib <CoreLib> --output <study.csv>
```

Use Windows path separators on Windows. The actual outputs are
`artifacts/P5/study-windows.csv`, `study-linux.csv`, their summaries and 27 raw
process logs per OS.

The native `--copy-study` uses the **actual** `PublishReferences` implementation:
atomic reservation, memcpy and release publication, followed by untimed
acquire/payload verification. Widths 1/64/1024 include partial/full chunks;
sample zero is warmup, six later samples are reported. It is single-thread,
warm-cache protocol cost, excluding barriers, contention, scheduling and RC.
Median time per actual publication, excluding warmup:

| Width / copied bytes | Windows ns | Linux ns |
|---|---:|---:|
| 1 / 8 | 10.55 | 10.94 |
| 64 / 512 | 16.02 | 22.07 |
| 1024 / 8192 | 138.02 | 156.27 |

The host's 24 MiB managed arena is **not** its total process footprint:
preallocated streams, publication tokens, mutator chunks, extent/flag/scan
planes and work packets add native memory. The copy probe is not an estimate
of that memory overhead or contested barrier latency.
The retained `--benchmark` additionally characterizes the whole RC-only path
with A/A and injected work. Neither study establishes production latency,
paper parity, an SLA, or a +/-0.5% effect.

## Explicit prototype limits

The supported execution is the tested nonmoving, fixed-reservation x64
configuration. P6 movement/evacuation/remset optimizations and P8 tuning are
not implemented. ReadyToRun, unsupported target architectures, GCStress,
moving LOH compaction, guaranteed no-GC regions, full-GC notifications,
cross-runtime Java bridge reclamation and stock DAC layouts are not claimed.
Limits are explicit rejection/failure, not permission to corrupt lifetimes or
silently retain arbitrary admitted garbage. No broad runtime test suite,
profiler/DAC conformance, arbitrary 1024-plus simultaneous-mutator workload,
or every P7 optimization is certified by these fixtures.

Raw build/test/failure/measurement records remain under `artifacts/P5` in the
worktree and Linux snapshot; earlier P0-P4 records remain intact. No push,
upstream issue/PR, merge to `lxr`, or main-checkout modification was performed.
Compact Linux final logs/CSVs were also copied to
`artifacts/P5/linux-final-records` in the worktree. All bounded runtime/build
helpers exited. The owned `R:` mapping, scratch stack walkers and private
321966626-byte shutdown dump were removed after process/target checks.
The unrelated 321-byte linker XML still has SHA256
`63c1d0e88ee63036ebdfeeb0be740e00a9e0b1aa2c86fcf8e816c3e182253e31`;
its original backup remains in session artifacts.
