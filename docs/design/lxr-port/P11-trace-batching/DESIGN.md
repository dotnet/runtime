# Authorized trace batching: fixed design and goals

Recorded before product edits or changed timings, from clean accepted
`9f25603ec751a1407808d4306b70198435873549`. P10 artifacts, layouts, report,
catalogs and bundles are read-only inputs. The user authorized recommended
batching and before/after benchmarks, not the other proposed optimizations.

## Coupled reference mapping

Oracle H is binding `0682434ae725787e38defbc17bd66dc918e4bdb7` with core
`304ce69d43aae87de501111fceb8cbd33173a03a`. Revision-qualified reads reconfirm
`plan/lxr/rc.rs:611-631`: Full instantiates the `true` trace context.
`plan/lxr/cm.rs:661-700` accumulates small-object fields across objects
with an 8192-edge Full limit. The port currently publishes one trace target
per processed increment and a new trace packet at each small object's
local scan end. Its 1024 maximum does not accumulate across those objects.

This change is deliberately **Full-only**. Concurrent, InitialMark and
FinalMark batching are not claimed. Add an internal 8192-target Full trace
batch, separate from the unchanged 1024 mutator ingress, RC children,
two log banks, publication encoding and full legal log capacity.

An executing RC packet accumulates increment-generated **object targets**.
An executing trace packet accumulates descendants across the small objects
it processes. Full trace target packets may hold 8192 targets; other work
kinds retain their existing 1024 bound. Targets are copied to owned Work
payloads on publication; no borrowed stack storage or saved writable slots.
Large-object descriptor slicing and its users remain unchanged.

Batching must not turn small objects into thousands of persistent scan
descriptors merely because a larger input packet exhausts the old shared
direct-scan budget. For Full only, apply the existing 1024 direct-scan
budget per object, as it was effectively applied by the former one-target
packets. Larger objects retain descriptor-based slicing. Concurrent
packet budgets are unchanged.

## Completion and ownership

Each active Full batch holds an explicit `m_trace_pending` lease. Pending
targets therefore cannot disappear from trace completion, including before
their first publication. A successful final Flush copies and registers
work before that lease can be dropped. Every batch is packet-local and
flushed before its owning packet returns: worker idle, weak/dependent/
finalizer closure, pause handoff, resume, epoch retirement and shutdown
cannot inherit a hidden worker-local tail.

No buffer is retained between packets or epochs. Errors remain latched;
an unflushed successful scope is an invariant failure, not silent loss.
The owner packet/trace count remains live until descendants are registered.
Required forwarding repair sees published owned targets before source
retirement. Mark-before-death, trace-scan descriptor users, LoaderAllocator,
pin/frozen handling, exact owner/shape/range/reuse/incarnation checks,
and EE closure ordering are not removed or weakened.

Diagnostics count active batch capacity in the **same current/high-water
work-byte counters** as packets, not an uncounted pool. Separate internal
counters expose batches, buffered targets, flushes, active batch bytes and
peak batch bytes through the existing prototype metric entry point.
Total packet-plus-pending-buffer high water is the primary footprint
denominator; scan descriptor high water is also reported separately.

## Quantitative goals, frozen before edits

All goals use fresh matched original9f-versus-batched observations, not a
comparison of new absolutes to P10's earlier machine state.

| Quantity | Baseline evidence | Fixed goal |
|---|---|---|
| H128 references262144 Work allocations over eight full calls | Linux 4,208,687 nonmoving/nursery; 4,208,700 mature | At most 50,000 in each mode on each OS where directly observed |
| Peak packet plus active batch storage | About 101 MB per mode; no original pending batch | At most 16 MiB in each mode; report descriptor storage too |
| Counter-off primary body median wall and process CPU | P10 tables bind prior scale, not the new denominator | At least 15% lower than fresh matched baseline, each mode and OS |
| Pointer-free body median wall and CPU | Separate base-cost control | No more than 10% regression; report misses without retry/tuning |
| Correctness and work conservation | Frozen checks and required native controls | No lost work, hidden pending tail, disabled test, changed admission, or reduced validation |

No 100x closure is promised. Request p50/p95/max, RSS/private snapshots,
remaining Workstation/Server gaps and all goal misses are reported.

## Bounded qualification and comparison

Native tests use the existing P4 validation file and build runners: batch
boundaries, cross-object accumulation, partial final flush, explicit
pending ownership, four-worker collection, moving targets and actual
packet/storage bounds. Omitted-final-flush, unbatched and early-completion
controls must fail. Existing root6170, trace8358, log/full1024-producer,
moving/retained-owner/shape/range/wrap/incarnation and lifetime suites stay.

Build only clrlxrgc and the standalone native fixtures in new owned
Windows/Linux Debug/Release directories. Use copied accepted runtime
layouts and scope5 managed correctness assemblies; do not rebuild CLR,
JIT, CoreLib or libraries. After the final relevant edit, run native
Debug/Release on both OSes and managed Release/Debug scope5 across native
configs and all three modes. One integrated review precedes final records.

The fresh timing schedule has eight arms: original/batched times three
modes, plus same-runtime Workstation and Server. At fixed H128 run
references262144 four times per arm; references65536 and131072 twice;
pointer-free262144 once. Interleave and reverse the prewritten arm order.
This is 72 processes per OS. Preserve fixture-log4 SHA5201b0cc, eight full
requests, two warmups, one cleanup, counts, mutations and checks. Diagnostics
and profiles are separate. Add a small declared automatic-pressure run
without replacing the forced-full test. Signal coordinator quiet START/END.

Final source/module/fixture/raw catalogs, retained failures and exact replay
paths go to the coordinator for independent acceptance. Commit locally on
this branch only; no public action, push, merge-back or ref movement.
