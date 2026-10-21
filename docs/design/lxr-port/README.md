# LXR port — design record

Working notes for a from-scratch port of the **LXR** garbage collector to .NET/CoreCLR.

LXR is described in Zhao, Blackburn & McKinley, *Low-Latency, High-Throughput Garbage Collection*
(PLDI 2022). It combines coalescing reference counting with a concurrent SATB trace over an Immix
heap. The reference implementation is the `lxr` branch of MMTk (`mmtk-core` + `mmtk-openjdk`).

The copy of the paper held locally is the **extended version**,
[arXiv:2210.17175](https://arxiv.org/abs/2210.17175) of 1 November 2022 — *not* the PLDI proceedings
paper, and its table and figure numbering cannot be assumed to match. Cite it accordingly; see
[`P0.2-paper-targets.md`](P0.2-paper-targets.md) §1.

These documents record research, design decisions, and opt-in collector
substrate. P2.1, P2.2, and P2.5 do not select or ship an LXR collector.

## Parity contract

Full parity with the reference is a requirement, not an aspiration. Approximations, heuristics and
GC-side workarounds are not acceptable substitutes for a reference mechanism.

Two revisions are in scope as **parity oracles**, and an oracle is a **shipped build pair** — a
binding together with the `mmtk-core` revision that binding actually pins — not a tag or branch name:

| oracle | mmtk-openjdk | mmtk-core (**the oracle**) | roadmap name | relationship |
|---|---|---|---|---|
| PLDI 2022 | `abbdd1d` | **`df8d30a3`** | `lxr-pldi-2022` = `4d4e516c` | the named revision is 9 commits **behind** |
| `lxr` head | `0682434` | **`304ce69d`** | `lxr` head = `9625c174` | the named revision is 9 commits **ahead** |

Neither roadmap-named revision is what its binding builds. All eighteen skew commits are classified
in [`P0.1-mechanism-diff.md`](P0.1-mechanism-diff.md) §18.

The oracle is chosen **per mechanism**, and each choice is recorded with its reason. Coupled
mechanisms share one oracle. `lxr-x/simplified` is explicitly excluded.

> **Three conventions worth carrying forward.** `src/plan/lxr/…` paths exist **only at HEAD** — at PLDI
> LXR is a compile-time feature configuration of the Immix plan, so a missing PLDI path is never a
> missing mechanism. MMTk revisions must be ordered with `git merge-base`, never by author date:
> `df8d30a3` is dated *earlier* than `4d4e516c` yet is nine commits *after* it, having been rebased.
> And **no working tree anywhere is checked out at either oracle** — every `mmtk-core` working tree
> sits at a *named* revision nine commits away, so a citation taken from a checked-out tree silently
> names the wrong revision. Cite instead with `git show <rev>:<path>` against the read-only reference
> clone, which holds all four revisions as objects:
> `git -C C:\github\lxr-reference\mmtk-core show 304ce69d:src/plan/lxr/barrier.rs`. That is read-only,
> needs no checkout, and is reproducible on any machine that has the clone. See
> [`P0.3-oracle-probes.md`](P0.3-oracle-probes.md) §1.1; `scripts/verify-ledger.sh` enforces it.

> **A fourth convention, and the one that has cost the most.** **A citation is not evidence unless
> the code is compiled in the configuration under discussion.** A line can exist at the right
> revision, at the right path, read exactly as the argument needs it to read, and still be
> `#[cfg(…)]`-gated out or commented out — in which case it describes nothing that ran. This is the
> single failure mode that has recurred in every step of P0 so far: a TLS failure generalized into
> "pip is blocked" without running pip; a positional column index that read the wrong column; a
> table's membership argued from prose when the page could be rendered; and now two
> `handle_user_collection_request` overrides cited as the oracles' behaviour when both are under
> `#[cfg(feature = "nogc_no_zeroing")]`, which neither oracle enables. The sharpest example is PLDI's
> `plan/global.rs:590–598`, whose body is *entirely commented out*: the code is present, correct and
> inert. Before a citation becomes evidence, establish that its feature gate is satisfied by the
> build recipe and that it is not commented. `scripts/verify-ledger.sh` §1c flags gated and commented
> citations, but cannot decide intent — a flagged citation must be either labelled as
> not-compiled or replaced.

> **Convention — a check must audit the tree it is run in.** The same failure mode reaches tools, and
> there it is more dangerous, because a tool that reads the wrong artifact still produces a clean,
> confident result. `scripts/verify-ledger.sh` defaulted its documents directory to an absolute path
> into one worktree, so running it from a fresh extract silently audited a different — and dirty —
> checkout. It now derives the default from its own location. Any script in this folder that takes a
> tree as input must default to the tree it ships in, never to a machine-specific path, and must be
> demonstrated to fail when that tree is perturbed. A gate that has never been seen to fail has not
> been shown to be reading anything. `scripts/verify-gate.sh` is that demonstration for
> `verify-ledger.sh`: it runs the committed gate on a clean `git archive` extract, then on a copy
> differing by one deleted index row, and requires the same gate to pass on one and fail on the
> other. Step B is the whole point — A and C alone would have passed just as happily while the path
> was hardcoded.

## Documents

| document | contents |
|---|---|
| [`P0.1-reference-build.md`](P0.1-reference-build.md) | What was pinned, the resolved (mmtk-core, mmtk-openjdk, OpenJDK, rustc) tuple per revision, the exact build recipe, and what could not be built or run |
| [`P0.1-mechanism-diff.md`](P0.1-mechanism-diff.md) | The mechanism-organised diff between the two oracles — direct input to the P0.3 parity ledger. **Its location tables cite the roadmap-*named* revisions on both sides**; P0.3 re-derives every citation at the oracles |
| [`P0.1-benchmarks.md`](P0.1-benchmarks.md) | Reference pause and throughput behaviour on both revisions, and the characterisation of the PLDI instability |
| [`P0.1-stock-baselines.md`](P0.1-stock-baselines.md) | Same-JDK G1, Parallel, Serial, and ZGC follow-up against the original HEAD-LXR DaCapo/Hiccup run, with raw TSVs and logs |
| [`P0.2-paper-targets.md`](P0.2-paper-targets.md) | The paper's own acceptance targets — throughput vs G1, pause p50/p95, the reclamation split and barrier overhead — each cited to a table and page, with the measurement context and the paper's errata |
| [`P0.3-parity-ledger.md`](P0.3-parity-ledger.md) | **The mechanism parity ledger** — 26 rows, each with its declared oracle and reason, coupled group, validation oracle, citation at the declared oracle revision, provenance tag, required .NET realization and closure evidence. This is the artifact P9.1 audits |
| [`P0.3-oracle-probes.md`](P0.3-oracle-probes.md) | The evidence behind the ledger: the citation-basis correction, three P0.1 open items resolved from source, the two oracle probes, and the two questions P0.3 had to close |
| [`P1.1-write-barrier-shape-contract.md`](P1.1-write-barrier-shape-contract.md) | The generic GC-requested write-barrier shape, startup failure contract, ordinary-ABI slow-call strategy, compatibility matrix, and executable/codegen/performance evidence |
| [`P1.2-x64-slot-log-barrier.md`](P1.2-x64-slot-log-barrier.md) | The generic Windows/System V x64 slot-log family, complete reference-store surface, oracle parity, execution matrix, and performance evidence |
| [`P1.3-store-path-coverage-and-bulk-barrier.md`](P1.3-store-path-coverage-and-bulk-barrier.md) | The complete current-main store-path ledger and generic family-level metadata-word bulk operation |
| [`P1.4-allocation-complete-notification.md`](P1.4-allocation-complete-notification.md) | The generic exact allocation-complete callback, complete managed-birth path ledger, x64 fast-helper family, fail-closed image contract, and performance evidence |
| [`P1.5-object-reference-enumeration.md`](P1.5-object-reference-enumeration.md) | The generic GC-side reference range iterator, collectible per-type resolver, compatibility matrix, real-layout parity, and native plus scenario-integrated scan performance evidence |
| [`P1.6-gc-reserved-object-header-bits.md`](P1.6-gc-reserved-object-header-bits.md) | Generic 64-bit GC object-header state negotiation, atomic claim/publish protocol, complete sync-block coexistence ledger, compatibility, stress, and lock/hash evidence |
| [`P2.1-side-metadata-framework.md`](P2.1-side-metadata-framework.md) | Collector-owned fixed-address side metadata, atomic bit/byte/word operations, selectable RC width, bulk reads, and contention/performance evidence |
| [`P2.2-block-line-state.md`](P2.2-block-line-state.md) | Checked 32 KiB block/256-byte line geometry, the block lifecycle state machine, ownership, and phase-epoch predicates |
| [`P2.3-sticky-reference-counts.md`](P2.3-sticky-reference-counts.md) | Selectable sticky reference counts on the eight-byte lattice, exact object-start validation, and paired continuation-line sentinels |
| [`P2.4-allocation-holes.md`](P2.4-allocation-holes.md) | RC-table hole discovery, fresh/recycled ownership, bump allocation, zeroing, and concurrent-mark line reuse generations |
| [`P2.5-large-object-space.md`](P2.5-large-object-space.md) | Default-off page-granular large-object space, LOS mark/nursery state, wrapping page reuse, and independent release |
| [`P3.1-work-packet-scheduler.md`](P3.1-work-packet-scheduler.md) | Reference-bearing work packets, persistent workers, work stealing, and strictly ordered buckets |
| [`P3.2-pause-taxonomy.md`](P3.2-pause-taxonomy.md) | Four live pause kinds, exact per-kind bucket schedules, concurrent interruption, and runtime suspension coordination |
| [`P3.3-deferred-concurrent-bucket.md`](P3.3-deferred-concurrent-bucket.md) | Double-buffered deferred concurrent work with explicit post-resume publication |
| [`P3.4-lazy-completion-counters.md`](P3.4-lazy-completion-counters.md) | RAII overall/decrement completion ownership, mature-sweep release gating, and next-GC result ordering |
| [`P4/README.md`](P4/README.md) | Integrated P4.1-P4.5 RC engine, shared-source epoch evidence on Windows/Linux x64, honest managed observer, and immediate selectable-host/P5 path |
| [`P5/README.md`](P5/README.md) | Selectable owned LXR collector host, integrated SATB/concurrent tracing/cycle reclamation, managed lifetime evidence and characterization on Windows/Linux x64 |
| [`P13-full-reference-first/index.html`](P13-full-reference-first/index.html) ([technical record](P13-full-reference-first/README.md)) | Plain-language explanation, from GC basics, of how a full collection works in the Java LXR and in our .NET port, with diagrams, sixteen differences, what .NET requires, exact source versions, and what we have not measured |
| [`P14-natural-gc/index.html`](P14-natural-gc/index.html) | Matched benchmark without forced collections: three programs written in C# and Java, run on the Java LXR, our port and the standard collectors of both runtimes; slowdown, stalls, kinds of collections, failures, raw results and scripts |
| [`P15-pauses-and-full-gcs/README.md`](P15-pauses-and-full-gcs/README.md) | Research only: why our pauses are 143 ms to 1.9 s and why our port runs so many full collections; measurements, code reading and the tracer scripts |
| [`P16-remove-per-object-bookkeeping/README.md`](P16-remove-per-object-bookkeeping/README.md) | Design only: remove the per-object tables, sweep only the needed blocks in the background, finalizer list, pin list, remove the block gate, give the copy enough room |
| [`P18-satori-abi-comparison/README.md`](P18-satori-abi-comparison/README.md) | Research only: the GC-to-runtime ABI and write barriers compared with PR dotnet/runtimelab#3316 (custom GC support with Satori); how our port did it, what the PR merged, what we must change |
| [`lxr-gc-explained.html`](lxr-gc-explained.html) | Standalone illustrated guide from RC, coalescing, SATB, and Immix fundamentals through the exact MMTk oracles and current CoreCLR port status |
| [`scripts/`](scripts/) | The clone, build, sanity, benchmark and probe scripts actually used, plus `verify-ledger.sh` (the ledger's gate) and `verify-gate.sh` (the gate's own positive control) |

## P0.1 outcome in one paragraph

Both revisions were pinned as independent clones and built on OpenJDK in WSL2/Ubuntu 22.04, release
and fastdebug, as the **shipped pairs** above; the roadmap-named revisions were additionally built via
a path override as a buildability experiment, and those numbers are quarantined separately.
**HEAD meets P0.1's correctness criterion**: all 8 DaCapo 2006 benchmarks, 3 invocations × {1, 5}
iterations at 500 MB and 2000 MB, plus a fastdebug pass — 96/96 runs, no crashes, and no assertion
failure in any of seven log directories. **PLDI does not.** It completes single-iteration runs of 5
of 8 benchmarks, crashes with SIGSEGV under sustained execution, and — the finding that matters most
for the port — in fastdebug it **fails its own barrier assertion**, `assert!(old.is_null() ||
rc::count(old) != 0)`, i.e. a field being overwritten still refers to an object whose reference count
has already reached zero. Release builds compile that check out, which is why the same defect appears
there as a segfault. The paper-contemporaneous oracle therefore remains the clearest **statement** of
the algorithm but is not a runnable specification: a port cannot be differentially tested against it.
Every mechanism recommendation that selects PLDI carries that caveat explicitly.

## P0.3 outcome in one paragraph

P0.3 converted P0.1's prose diff into a **26-row ledger** and, in doing so, corrected the basis on
which P0.1's citations rested: no working tree is checked out at either oracle, and P0.1's location
tables describe the roadmap-*named* revisions on **both** sides, so every citation was re-derived at
the oracle revisions. Two probes were run rather than reasoned about. **Probe 1** found that P0.1's
claim "HEAD never trips the `zero rc count` assertion" was *vacuous* — the assertion is absent from
HEAD's source and, verified with `strings`, from HEAD's shipped binary — so the invariant was
re-instated at the HEAD oracle and run under load: **48/48 DaCapo runs pass with zero violations** at
2000 MB and 500 MB. Root-causing PLDI's violation is therefore **not** on the critical path, and the
obligation is transferred to the port, which must carry the assertion from its first commit.
**Probe 2 resolved the hsqldb p99 anomaly rather than retiring it.** It is not an LXR mechanism
difference at all: hsqldb's `MemoryWatcherThread` is the only caller of `System.gc()` in DaCapo 2006,
and the two oracles differ in kind — HEAD **services** the request and promotes it to a full-heap
stop-the-world `Pause::Full`, while PLDI **cannot service one at all** because the body of
`BasePlan::handle_user_collection_request` is commented out, so its user-triggered flag is never set.
Running HEAD with `-XX:+DisableExplicitGC` removes all 25 pauses and 39 % of wall time. Seven
candidate causes were eliminated with evidence along the way. The finding is directly
portability-relevant — `GC.Collect()` is a public .NET API, and the port must choose deliberately
among ignoring the request (which HEAD ships as the runtime option `ignore_system_gc`), servicing it
without promotion, or servicing and promoting to full-heap — and became its own ledger row.
The ledger's sharpest single finding is about **pinning**: LXR does not merely leave it uncompiled,
it declares it unsupported — `PinningProcessEdges = UnsupportedProcessEdges` in both work contexts,
the pinning work buckets `set_enabled(false)`, and `is_pinned()` compiled to a constant `false`, all
of it ungated and present in every measured run. So its unconditional source-block release during
evacuation is sound only because nothing can be immovable. .NET cannot make that choice — pinned
objects are heap-resident there, not merely roots — so **there is no reference mechanism to port**
and the row carries declared oracle `none` (R05).



This is design documentation and prototype validation in a `dotnet/runtime` fork. Bulk build state —
clones, OpenJDK trees, JDK images, benchmark jars — lives outside version control. P5's selectable
collector is an explicitly enabled runtime target; the documentation and standalone fixtures are not
part of the default runtime build.
