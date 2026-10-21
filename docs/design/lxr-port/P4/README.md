# P4: integrated coalescing reference counting

P4.1-P4.5 are one experimental vertical, not independently certified collectors.
`LxrReferenceCounting` is the shared collector-internal implementation used by
the native epoch tests and the managed logging observer. It owns real metadata,
Immix allocations, LOS allocations, work packets and completion registrations
in its owned-arena mode. Its observer mode shares logging only: **Workstation
and Server GC still own all managed memory**.

The implemented scope is Windows and Linux x64 CoreCLR. The single product
switch, `CLR_CMAKE_ENABLE_LXR_RC_VERTICAL`, defaults off and rejects unsupported
targets. ReadyToRun is disabled for the managed smoke. There is no selectable
LXR `IGCHeap` yet. Mono, NativeAOT, WASM, other architectures, moving evacuation
and production qualification remain outside this vertical.

## Integration and provenance

The reconciled prerequisite commit is
`796054f46494529a2dfdb8cf9a6bd61727932bbd`. It preserves accepted P2.3, P2.4,
corrected P2.5, P3.4 and P3.2 histories on the existing LXR lineage. It does not
replace their immutable source/evidence records. Reconciliation preserved both
the RC inconsistency result and the LOS metadata plane, removed duplicate source
inclusions, and resolved the scheduler's incompatible completion protocols:
finish/seal while suspended, actually resume the EE, record resume, publish lazy
work, drain tracked ownership, then retire the generation before the next pause.

The coupled P4 oracle is the HEAD binding
`0682434ae725787e38defbc17bd66dc918e4bdb7` and its pinned core
`304ce69d43aae87de501111fceb8cbd33173a03a`. This is an explicit prototype choice,
not a rewrite of the historical Group A/B PLDI-specifies/HEAD-validates ledger.
The comparison PLDI pair remains binding
`abbdd1dbbe7277e4b0dfd1d37ba613c53f5ca50d` / core
`df8d30a39237a5bf5a8e27ca5a6f46acc6080c94`.

Operative HEAD paths are flat: `src/plan/lxr/barrier.rs`, `rc.rs`, `global.rs`,
`cm.rs`, `gc_work.rs`, plus `src/policy/immix/rc_work.rs` and
`src/policy/largeobjectspace.rs`. There is **no**
`304ce69d:src/plan/lxr/gc_work/` directory. The later `9625c174` layout remains
secondary evidence, not this vertical's oracle.

PLDI's zero-RC pre-overwrite assertion is
`df8d30a3:src/plan/barriers.rs:315-319`, condition 316, under sanity/debug
configuration. HEAD removed the barrier assertion; its remaining check is
`304ce69d:src/util/sanity/sanity_checker.rs:311-316`, condition 312, under nondefault
sanity checking. The owned engine executes the invariant in the barrier and
rejects a zero-count old target before overwrite even in Release. A successful
HEAD Release run is not evidence for that invariant, or for SATB tracing.

Existing [dotnet/runtime#132305](https://github.com/dotnet/runtime/issues/132305)
is provenance for runtime barrier-capability negotiation, **not** a tracker for
a new collector or RC implementation. The coordinator narrowed the issue-first
gate to new uncovered host/VM/JIT edits; this is not a user-authorized procedural
waiver. This vertical adds no new VM lifecycle contract: it uses the existing
`FixAllocContext(context, nullptr, nullptr)` thread-cleanup call before TLS reset.
VM CMake changes provide source inclusion/compile visibility only. A genuinely
new host/VM/JIT surface in a subsequent vertical remains gated.

## Engine contract

| Constituent | Implemented behavior |
|---|---|
| P4.1 | Per-mutator 1024-entry increment-slot and old-target decrement buffers; full, pause and detach publication; exact pre-overwrite capture |
| P4.2 | Current target load before exact slot unlogging; zero-to-one promotion arms fields and straddles; real `GCReferenceRanges`/`GCDesc` descendant closure drains first |
| P4.3 | 2/4/8-bit sticky counts; one-to-zero child registrations before parent completion drops; generation-guarded mature/LOS release; no death-time payload clearing |
| P4.4 | Copied current/previous root targets; release-time swap; one-epoch leases and previous-root decrements in the next epoch |
| P4.5 | Eight-byte-lattice RC/straddle metadata sweeping; no size, MethodTable or GCDesc queries during sweeping; allocator hole reuse and allocation-record-only nursery LOS release |

CoreCLR allocations include the eight-byte `ObjHeader` preceding `Object*`.
Valid starts and RC metadata address that **physical allocation start**.
Reference slots and targets remain actual managed-layout addresses. Complete
sizes include header and padding, are eight-byte aligned and at least 24 bytes.
LOS begins at complete size 32768 inclusive; the existing primitive validator
separately covers the unaligned 32769 boundary.

The pre-store path neither allocates nor waits on the scheduler mutex, triggers
GC, throws, nor silently discards entries. It claims a slot once per epoch and
publishes pre-zeroed chunks to lock-free ingress. Each bank reserves
`2 * ceil(admittedSlots / 1024) + 4 * maximumMutators` chunks. In addition to the
simultaneous live-context limit, **at most `maximumMutators` new attachments are
admitted per epoch/window**. Admission fails explicitly outside the callback.
The budget resets after suspended release or quiescent observer drain; a future
host must request an epoch before retrying exhausted admission. Native records
outlive TLS and flush partial buffers before identity reuse. This bounded
prototype contract is not unlimited thread churn.

Young reclamation happens after increment closure. Mature deaths occur in the
tracked lazy tail after actual mutator resume. The next producer bank is not
consumed by the current generation. Continuation clearing publishes marker zero
before RC zero, both SeqCst, so an allocating mutator cannot observe an available
line with stale continuation state. Recycled allocation tries successive holes
and fresh fallback rather than failing at the first too-small hole.

Owned-arena execution rejects finalizable, collectible and frozen births.
Interior roots are normalized; roots are not blanket-pinned instead of counted.
Cycles and saturated sticky garbage are deliberately retained pending P5.
The observer admits ordinary managed startup objects only because the built-in
collector retains full responsibility for their lifetime.

## Executed validation

The final native input hashes and product/hook input hashes are recorded in
[`validated-inputs.csv`](validated-inputs.csv). These identify the P4 working
sources atop the reconciled base without mislabeling the base as P4 source.
Windows/Linux copied product inputs were hash-checked. Native checks use real
product classes and CoreCLR descriptors, not an alternate graph algorithm.

| Check | Windows x64 | Linux x64 |
|---|---:|---:|
| Shared final Debug epoch runner | 22459/22459 | 22459/22459 |
| Shared final Release epoch runner | 22459/22459 | 22459/22459 |
| Existing RC cases / checks | 37/37 / 1882 | 37/37 / 1882 |
| Existing allocator checks | 27165/27165 | 27165/27165 |
| Existing LOS checks | 201/201 | 201/201 |
| Enabled incremental Debug product | Passed | Passed |
| Managed observer, WKS and Server | Both passed | Both passed |
| Default-off native product and WKS/Server collection smoke | Passed | Passed |

Existing Windows primitive runners also executed their one-check x86 fail-closed
cases; this does not enable or qualify x86 P4. Consolidation tests passed on both
OSes: combined pause/deferred 380 checks, completion-tracked 384, existing
scheduler/deferred/completion 642/357/57.

Adversarial cases cover acyclic child death, one-epoch root retention, repeated
reuse, poisoned unreachable nursery layouts, sticky counts, retained cycles,
LOS reclamation, 1023/1024/1025/multiple-chunk logging, thread exit, same-slot
writers, attachment churn, too-small recycled holes, and allocation concurrent
with lazy continuation clearing. Three same-engine controls detected violations:
early root decrement and forbidden sweep layout query exit 1 on focused Debug
cases; a deliberately unaccounted zero-RC old target exits 1 in Release before
overwrite on both OSes. Existing P3.4 tests exercise invalid release authority.

The managed smoke uses strong handles and a successful no-GC region. Real
scalar, bulk and failed-CAS stores produce exactly **5124 increments and 5124
decrements**, with four actual thread exits. Root counts are positive and
process-dependent, not fixed acceptance thresholds. A GC-created nonmanaged
worker enumerates every heap partition while the EE is suspended, copies root
targets and normalizes interior roots. It does **not** call mark-dependent
`BeforeGcScanRoots`/`AfterGcScanRoots` lifetime notifications. A changed built-in
GC count fails the observation window. Output explicitly says observer-only.

One integrated review found and corrected continuation publication order,
attachment-reserve exhaustion, recycled-hole fallback, unsafe observer lifetime
notifications and missing Server root partitions. The same review's bounded
follow-up found no significant remaining issue in those fixes. Current affected
checks were rerun after the final edits; this is not production certification.

## Build history and failures retained

Both baselines started from exact reconciled source `796054f4`, before P4 behavior
edits. Windows `clr+libs+host` passed with 39 visible audit warnings and no errors.
Linux's original broad baseline exited 1 on advisory-only restore (the original
wrapper additionally exited 2); the authorized NU1903/NU1904-only retry exited 1
on six NativeAOT `E_INVALIDARG` compilation errors. **Neither failed run passed.**
No NativeAOT code, dependency manifest or audit setting was changed.

The authorized narrower Linux subset was
`clr.runtime+clr.corelib+clr.nativecorelib+clr.tools+libs+host`, with
`/p:UseNativeAotForComponents=false`; its log verifies `-component runtime`.
CoreCLR built, but archive-based library packaging then exited 1 for missing
repository identity. Matching `libs+host` completed with the exact
`/p:RepositoryCommit=796054f46494529a2dfdb8cf9a6bd61727932bbd`, exit 0, twelve
visible warnings, no errors. The archive's initial native build diagnostic says
`CLI_CMAKE_COMMIT_HASH=N/A`; its verified source archive, not that diagnostic,
establishes baseline source identity.

Exact Linux invocations, recovered from the completed session command records,
ran from `/root/lxr-p4-902afec4-796054f4`:

```bash
# Authorized advisory-only retry: exit 1, deferred NativeAOT compilation errors.
./eng/build.sh --subset clr+libs+host --arch x64 --runtimeConfiguration Debug --librariesConfiguration Release --hostConfiguration Debug --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' --warnNotAsError 'NU1903;NU1904' > artifacts/P4/baseline-linux/retry-build.log 2>&1

# Authorized CoreCLR-only correction: runtime built; packaging exited 1.
./eng/build.sh --subset clr.runtime+clr.corelib+clr.nativecorelib+clr.tools+libs+host --arch x64 --runtimeConfiguration Debug --librariesConfiguration Release --hostConfiguration Debug --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' --warnNotAsError 'NU1903;NU1904' /p:UseNativeAotForComponents=false > artifacts/P4/baseline-linux/coreclr-build.log 2>&1

# Matching library/testhost/host completion: exit 0, twelve visible warnings.
./eng/build.sh --subset libs+host --arch x64 --runtimeConfiguration Debug --librariesConfiguration Release --hostConfiguration Debug --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' --warnNotAsError 'NU1903;NU1904' /p:UseNativeAotForComponents=false /p:RepositoryCommit=796054f46494529a2dfdb8cf9a6bd61727932bbd > artifacts/P4/baseline-linux/layout-build.log 2>&1
```

All advisory/build logs and statuses remain in `artifacts\P4\baseline-windows`
and, in the owned Linux snapshot, `artifacts/P4/baseline-linux`. The only warning
exception is NU1903/NU1904; auditing remains enabled. No further baseline matrix
was run. Later incremental enabled and default-off product builds passed with
zero warnings/errors.

One initial Linux smoke copy stopped because source/destination `corerun` were
the same hardlinked file; the retry skips identical files. One primitive-runner
invocation supplied output as the repository argument; the corrected existing
command passed. The first Windows default-off command accidentally grouped
multiple `-D` assignments into one CMake value. Its successful build is **not**
default-off evidence. The corrected single-switch build passed; cache values
and absence of observer exports were checked by the managed smoke.

## Reproduction

Use the current source revision, repository SDK and documented build skill.
These are baseline reproduction commands, not permission to rerun full
baselines after every substep:

```powershell
.\eng\build.ps1 -Subset clr+libs+host -Arch x64 -RuntimeConfiguration Debug -LibrariesConfiguration Release -HostConfiguration Debug -CMakeArgs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' -WarnNotAsError 'NU1903;NU1904'
```

```bash
./eng/build.sh --subset clr.runtime+clr.corelib+clr.nativecorelib+clr.tools+libs+host --arch x64 --runtimeConfiguration Debug --librariesConfiguration Release --hostConfiguration Debug --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' --warnNotAsError 'NU1903;NU1904' /p:UseNativeAotForComponents=false
```

For an exact-source archive, additionally supply its verified
`/p:RepositoryCommit=<archive-commit>`; never substitute another revision.
The audit-preserving broad Linux retry used `clr+libs+host` instead of the
narrow subset and included `--warnNotAsError 'NU1903;NU1904'`; it failed on the
deferred NativeAOT target as recorded above.

Executed final incremental Linux entrypoint (same command with `0` and generic
test switches explicitly `0` for default-off):

```bash
./eng/build.sh --subset clr.runtime --arch x64 --runtimeConfiguration Debug --cmakeargs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' --warnNotAsError 'NU1903;NU1904' /p:UseNativeAotForComponents=false /p:RepositoryCommit=796054f46494529a2dfdb8cf9a6bd61727932bbd
```

Executed Windows incremental entrypoint:

```powershell
.\eng\build.ps1 -Subset clr.runtime -Arch x64 -RuntimeConfiguration Debug -CMakeArgs '-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=1' -WarnNotAsError 'NU1903;NU1904'
```

For default-off Windows use just `-CMakeArgs
'-DCLR_CMAKE_ENABLE_LXR_RC_VERTICAL=0'`; the two generic test cache switches
were already OFF. Do not put several `-D` assignments in that Windows string.

Final kernel, controls and characterization:

```powershell
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -RepositoryRoot R:\ -OutputDirectory R:\artifacts\P4\vertical-windows-Debug-final -Configuration Debug
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -RepositoryRoot R:\ -OutputDirectory R:\artifacts\P4\vertical-windows-Release-final -Configuration Release
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -RepositoryRoot R:\ -OutputDirectory R:\artifacts\P4\control-EarlyRoot -Control EarlyRoot
.\docs\design\lxr-port\P4\run-lxr-rc-vertical.ps1 -RepositoryRoot R:\ -OutputDirectory R:\artifacts\P4\control-SweepQuery -Control SweepQuery
& R:\artifacts\P4\vertical-windows-Release-final\lxr-rc-validation.exe --zero-rc-control
& R:\artifacts\P4\vertical-windows-Release-final\lxr-rc-validation.exe --benchmark
```

```bash
bash docs/design/lxr-port/P4/run-lxr-rc-vertical.sh Debug "$PWD/artifacts/P4/vertical-linux-Debug-final"
bash docs/design/lxr-port/P4/run-lxr-rc-vertical.sh Release "$PWD/artifacts/P4/vertical-linux-Release-final"
artifacts/P4/vertical-linux-Release-final/lxr-rc-validation --zero-rc-control
artifacts/P4/vertical-linux-Release-final/lxr-rc-validation --benchmark
```

The zero-RC control deliberately returns 1; ordinary runs must return 0 and
report all 22459 checks. Each primitive `.sh` runner takes repository root first,
output directory second. Windows `.ps1` equivalents take named arguments.

Build `runtime-smoke\runtime-smoke.csproj` in Release with the repository SDK,
then run its DLL using matching `corerun` and framework artifacts from this source.
Copy the just-built CoreCLR/GC/JIT into the actual
`shared\Microsoft.NETCore.App\11.0.0` directory (skip identical hardlinks on
Linux), not the testhost's top directory. Pass that loaded CoreCLR library path
as the sole argument. Set `DOTNET_ReadyToRun=0`, `DOTNET_TieredCompilation=0`,
`DOTNET_GCWriteBarrierTestClobber=0`, `DOTNET_GCWriteBarrierTestBitMeaning=1`,
`DOTNET_GCWriteBarrierTestClaimBits=1`, `DOTNET_GCAllocationNotificationTest=1`;
repeat with `DOTNET_gcServer=0` and `1`. Default-off smoke adds `--builtin` and
requires observer exports to be absent while forced compacting collections
preserve 4097 live references.

Final logs: `vertical-windows-Debug-final`, `vertical-windows-Release-final`,
`control-EarlyRoot`, `control-SweepQuery`, `runtime-smoke-windows-final-{0,1}.log`,
`default-off-windows-{0,1}.log`; corresponding Linux final folders/logs live in
the verified session-owned snapshot `/root/lxr-p4-902afec4-796054f4`.
`R:` was only an owned short mapping to this worktree, not another checkout.

## Focused performance characterization

This is a native shared-kernel Release study, **not** a comparison with a
selectable managed GC. Each policy has 24 cycles, eight warmup and sixteen
measured cycles, 64 field overwrites, three internal launches, and medians.
Final measured-source results:

| OS / launch | Base A barrier / epoch ns | Base B barrier / epoch ns | Injected barrier / epoch ns |
|---|---:|---:|---:|
| Windows / 0 | 4600 / 220400 | 5300 / 231900 | 152900 / 288000 |
| Windows / 1 | 5200 / 244400 | 5900 / 240500 | 153200 / 272600 |
| Windows / 2 | 5300 / 222200 | 5900 / 270400 | 153200 / 236300 |
| Linux / 0 | 6699 / 1185851 | 6500 / 1260149 | 44698 / 1356445 |
| Linux / 1 | 6499 / 981060 | 6400 / 934862 | 43898 / 1151053 |
| Linux / 2 | 8200 / 1174651 | 9199 / 1269161 | 45699 / 1291961 |

Every row allocates 37392 bytes, observes 1536 leaf deaths, and releases no
whole blocks: 36864 dead leaf bytes are reused as holes around the 528-byte live
parent. The injected per-store dependency chain is visibly distinguishable
from A/A noise. Epoch medians vary materially; these shared-host observations
do not establish a small regression bound or throughput parity. Earlier runs,
including a Linux run concurrent with a build, remain separate raw logs.
P1.7 is still **FAILED/INCONCLUSIVE**, with its unresolved +/-0.5% sensitivity.
The prototype exception does not turn it into a release-quality pass.

## Immediate selectable-host and P5 path

The next vertical must implement a real default-off `IGCHeap`/`IGCHandleManager`
host that consumes this same engine: owned managed allocation contexts and birth
notifications; correct generic barrier negotiation; suspension/epoch triggering
and admission retry; VM roots, strong/dependent/weak handles, interior and pinned
semantics; bootstrap objects, finalization/interop/collectible lifetimes; OOM,
shutdown, heap walking and diagnostic answers. Observation is not that host.
Do not silently exclude objects that arise during normal CoreCLR startup.
Any required new uncovered VM/JIT/host contract needs its narrowly scoped issue.

Then P5 adds the actual trace/cycle path and explicit collection behavior.
RC-only retained cycles/sticky garbage are expected here; a general-purpose
managed prototype cannot indefinitely ignore them. Moving evacuation and broad
qualification follow later. These are critical-path dependencies, not a promise
that a selectable collector will meet a particular week's deadline.
