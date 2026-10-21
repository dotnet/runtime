# P11: Full trace batching, with fresh before-and-after results

The user authorized **recommended batching only** after reviewing P10.
This change reduces Full-collection packet allocation without an allocation
pool, metadata optimization, changed ingress/admission/defaults, new VM/JIT
hook, or removal of reader/owner/shape/range/incarnation checks. It does not
close the remaining large gap to the built-in collectors.

## Scope and ownership

The baseline is accepted `9f25603ec751a1407808d4306b70198435873549`,
with collector implementation `46c366e0845737c8cf20eea3106aa28bfe1b2937`
and scope5 test repair `66aba3094482c81b402f06dcf2ccffe5b0cbed66`.
P10 layouts, report and evidence bundles were not changed.

The pinned H pair remains binding
`0682434ae725787e38defbc17bd66dc918e4bdb7` and core
`304ce69d43aae87de501111fceb8cbd33173a03a`. At that exact core,
`src/plan/lxr/rc.rs:611-631` selects the Full trace context and
`cm.rs:661-700` carries fields between small objects with an 8192-edge
limit. This port applies the same cross-object accumulation principle to
owned object targets.

`TraceBatch` is packet-local and **Full-only**. RC increment targets and
small-object descendants accumulate before publication. Each batch holds
an explicit trace-pending lease, publishes a copied Work payload, and
flushes before its owner returns or drops completion ownership. No hidden
worker-local tail survives idle, closure, resume, epoch change or shutdown.
The existing work-byte current/high-water counters include active batch
capacity; five additional internal metrics expose batch counts and bytes.

Only Full trace target packets can be produced at the new 8192-target
size. Mutator ingress, RC children, both log banks and array descriptor
slices retain their 1024 limits. Full direct scanning keeps the old
per-object scan limit rather than creating small-object descriptors when a
larger packet exhausts a shared budget. Separate non-inlined Full helpers
keep the 64 KiB local buffer out of non-Full stack frames. Concurrent,
InitialMark and FinalMark batching are **not** claimed.

## Frozen goals and method

`DESIGN.md` in the owned evidence root records the design and goals before
product edits: at most 50,000 Work allocations and 16 MiB combined
packet-plus-pending high water for the primary diagnostic case; at least
15% lower primary body wall and process CPU in every mode/OS; no more than
10% pointer-free body wall/CPU regression. All **18 goals per OS pass**.
The smaller scaling cases are not subject to the primary 15% threshold.

These are fresh interleaved original-versus-batched observations, not new
absolutes compared to P10 medians. Each OS has 72 normal processes:
original/batched times three modes plus same-runtime Workstation/Server;
references262144 four times per arm, references65536/131072 twice, and
pointer-free262144 once. Admission stays H128:64 MiB Immix+64 MiB LOS.
Windows then Linux ran sequentially in a signaled quiet block. No process
or unattractive observation was retried or removed.

Every normal process uses the immutable log4 DLL
`5201b0ccc41ab3cd51313643d62eda6509cbc2e9c17e7aa0131d021f9913cae9`,
unchanged mutations/checks, eight actual blocking Full requests, two
warmups and one separate cleanup. Actual collector mode/module, native
runtime/JIT/host paths, CoreLib input and frozen sources are checked.
Inherited `DOTNET_`/`COMPlus_` settings are removed case-insensitively.
Counters and profiles are separate. There is no exclusive-GC CPU estimate.

## Primary results

Medians of four counter-off processes per arm, body wall/process CPU in ms:

| OS | Mode | Wall before | Wall after | Wall reduction | CPU before | CPU after | CPU reduction |
|---|---|---:|---:|---:|---:|---:|---:|
| Windows | Nonmoving | 2752.586 | 1567.569 | 43.05% | 4367.188 | 2335.938 | 46.51% |
| Windows | Nursery | 3858.649 | 2007.599 | 47.97% | 5476.563 | 2828.125 | 48.36% |
| Windows | Mature | 5867.229 | 3812.078 | 35.03% | 7578.125 | 4632.813 | 38.87% |
| Linux | Nonmoving | 3349.742 | 2380.349 | 28.94% | 4893.956 | 3187.452 | 34.87% |
| Linux | Nursery | 4093.529 | 2933.294 | 28.34% | 5718.809 | 3791.493 | 33.70% |
| Linux | Mature | 7174.246 | 5644.194 | 21.33% | 8974.027 | 6584.446 | 26.63% |

Remaining body-wall gaps to the same-runtime Workstation control are
32.17/41.20/78.22x on Windows and 46.16/56.88/109.45x on Linux.
This is a useful reduction, not a 100x repair or general GC throughput claim.

Empirical request p95, over 32 inclusive Full-call wall durations per arm:

| OS | Nonmoving before/after ms | Nursery before/after ms | Mature before/after ms |
|---|---:|---:|---:|
| Windows | 352.45 /203.59 | 555.12 /263.17 | 820.30 /582.19 |
| Linux | 486.74 /308.35 | 550.73 /362.89 | 953.78 /713.23 |

These are small-sample request tails, not production pause guarantees.
Normal live-boundary RSS on Linux falls from 516.71/525.65/586.06 MiB to
188.08/196.47/251.09 MiB; Windows remains close to 179/188/237 MiB before
and 178/187/236 MiB after. These are snapshots, not peak RSS. Private-memory
values and definitions are retained separately; they are not equated
between OSes or with admitted/committed bytes.

## Work and storage conservation

The **same separate observation DLL** is used in old/new diagnostic arms.
It preserves the log4 graph, mutations/checks and eight calls but adds
counter/result allocations. Its intervals are not the earlier P10/GDB
4,208,687 cohort. Baseline-only unavailable batch counters remain null.
The DLL is
`b7b4ff8d9dde40ccf52df4118cafed8f79ec053ba7ee98a0b160b47b1bfc576a`;
its PDB and all five actual compiler documents, including emitted P/Invoke
source, are directly bound in `diagnostic-compiler-bindings.json`.

| OS/mode | Work allocations before/after | Combined packet+pending peak before/after bytes |
|---|---:|---:|
| Windows nonmoving/nursery | 4,222,323 /16,660 | 101,411,152 /8,900,360 |
| Windows mature | 4,222,330 /16,667 | 101,411,336 /8,900,544 |
| Linux nonmoving | 4,208,707 /16,594 | 101,059,568 /8,840,304 |
| Linux nursery | 4,208,707 /16,592 | 101,059,568 /8,839,880 |
| Linux mature | 4,208,720 /16,605 | 101,059,752 /8,840,064 |

Pending-buffer peak is 262,240 bytes: four 65,560-byte active instances,
already included in the combined column. There is no new uncounted pool.
For each mode, old/new traced objects and references are identical within
each OS's diagnostic cohort: Windows2,121,312 objects/4,305,112 references;
Linux2,101,102 objects/4,215,474 references. Detailed RC, SATB, split, copy,
remembered-set and smaller-graph counters are retained in
`diagnostic-deltas.csv`; source/mode checks are not replaced by these totals.

The fixed-H128 scaling data retains both smaller graphs and the pointer-free
control. Linux mature at65536 improves wall12.66%, below the primary-case
threshold but not a declared goal miss. Regressions are explicit:
pointer-free Linux nursery body wall+5.51%, CPU+5.67%, request p95+12.36%;
Windows pointer-free nursery body wall+1.82%, CPU+5.00%, and mature p95+13.51%.
The pointer-free arm has one process/eight calls; no confidence interval or
causal explanation is claimed, and none was discarded or tuned away.

## Automatic pressure and profiles

Alongside, not instead of, the forced-Full test, 16 processes per OS use the
original524288-operation allocation loop, checksum1073479680, with no
explicit collection inside its body. A warmup and post-body collection
remain outside it. Both stages use the same observation DLL. LXR admission
and managed capacity limit are24 MiB; built-in standard24 MiB hard limits
include bookkeeping and are not claimed to be identical capacity contracts.
All LXR arms record two body collections.

With two processes per arm, before/after body-wall medians are
91.974/88.871,107.766/100.658,109.313/102.181 ms on Windows and
129.267/134.454,147.021/140.587,186.686/147.219 ms on Linux.
Linux nonmoving regresses4.01% wall/2.21% CPU; retain that point estimate.
These results do not establish a broad automatic-pressure improvement or
attribute every automatic collection to the newly batched Full path.

Six fresh profiles per OS use the unchanged normal-timing module copies.
There are **no new FP builds**: original and changed Linux Release commands
already preserve frame pointers, separately bound in
`profile-linux-fp/frame-pointer-proof.json`. Both use199 Hz `cpu-clock:u`
and `--call-graph fp`. A16 KiB DWARF policy was rejected before recording
because the new local frame is64 KiB. P10's old DWARF data remains separate;
there is no invented truncated recording or transferred denominator.

Linux before/after sample counts are1229/801,1576/1039,2187/1630, all with
zero recorded losses. They cover four native workers and the EE executor.
Packet create/delete plus native-allocation leaf samples fall from
24.49/21.64/13.49% to0.87/0.38/0.25%. Metadata/RC leaves remain
49.94/38.59/49.88% after. Reader/forwarding/extent leaves remain
1.37/16.27/18.28%. These are whole-process user-CPU sampled leaves,
including setup/warmups/cleanup; not exclusive subsystem CPU or the
eight-call body denominator. Native/libc tail/unwind limits, unresolved
managed code and inherited VM/JIT symbol mismatches remain explicit.

Windows retains six native wall censuses, not unbiased CPU profiles:
728/416,806/442,1105/754 nonempty before/after stacks, with native workers,
EE execution, waits, per-thread CPU totals and read failures retained.
WPR/xperf policy limits from P10 remain; no privileges were changed.
Idle/wait census shares are not CPU cost or precise wait durations.
Only directly matched collector PDB/ELF companions are trusted; inherited
VM/JIT companions are not forced onto the loaded modules.

## Qualification and retained failures

After the final product/test edit, native Debug/Release pass on both OSes:
313569 new batch/slice checks;30979 original;6170 root;8358 trace;2121 log
including full1024-producer proof; and all nursery/mature, moving-reader,
retained-owner, completed-descriptor, shape/range/wrap/incarnation, storage,
growth/policy and lifetime selectors. Every Drop/Unbatched/Early control
fails as intended in all four OS/config cells. The new small fixtures
enforce actual packet ceilings,512 KiB combined and4 KiB descriptor bounds,
final partial publication, explicit pending ownership and moving payloads.

All264 managed processes pass: both immutable scope5 assemblies across
both native configs/all3 modes on both OSes;100632 full checks per cell,
plus every accepted P7 lifecycle case. The eight added seams ran after
TIMING END without repeating the already passing full/finalizer/dependent
cells. No assertion/test was disabled or denominator reinterpreted.

One integrated review found a missing upper storage bound in the new tests.
It was added; the bounded follow-up closed it; final matrices ran afterward.
Retained unexpected attempts include a native Field declaration error,
an incorrect forward signature, a misplaced per-object Flush caught by
the packet-count bound, a missing generated version input, and an
unemitted generated PDB document. They were fixed, not accepted as results.
All normal observations and the final qualification/profiles are retained.

## Exact records and replay

Windows root:
`C:\Users\konradkokosa\.copilot\repos\copilot-worktrees\runtime-fork\kkokosa-congenial-happiness\artifacts\P11-trace-batching`.
Linux root: `/root/lxr-batching-d2eccaea`.
`study-inputs.json` freezes both modules/common runtime/fixture/source
expectations and schedule; each phase saves the actually executed runner.
`module-identities.json` binds all four before/after Debug/Release
collectors and their directly matched primary companions per OS.
`comparisons.csv`, `goals.csv`, `diagnostic-deltas.csv`,
`pressure-comparisons.csv`, profile analyses and `raw-catalog.csv` locate
the results. Actual source manifests, patches, build commands/status/logs,
portable-PDB bindings and failure attempts remain beside them.

Use existing owned layouts and a **new** output root for a replay.
`run-study.py --root ROOT --phase prepare` freezes its exact inputs;
`--phase timing`, `pressure`, `diagnostic` run the declared schedules once.
Do not overwrite a completed phase or substitute the observation DLL for
log4 normal timing. `profile-native.py` and `analyze-profiles.py` keep
profiles separate. `summarize.py` retains goal misses.
`run-managed.py` runs full/closure/dependent cells; the unchanged P7
`run-qualification.py` with the eight repeated `--case` arguments supplies
the remaining lifecycle cells. `audit-records.py` verifies expected counts
and frozen identities without rerunning performance observations.

For collector builds, replay the retained configure/build commands in new
owned directories and target only `clrlxrgc`; use the existing P4 native
runners and recorded selectors/controls. No full CLR/JIT/CoreLib/libs build
was needed. Inherited `@Commit` version strings are not source proof.

No public action, push, merge-back, or main/origin/local-lxr ref update was
made. The27 live ledger IDs, C06-to-R05 redirect, Group A/B history, pin
and .NET adaptations, P1.7 limits and broad production/platform gaps are
not rewritten or newly closed. Final independent coordinator acceptance
is separate from these local results.
