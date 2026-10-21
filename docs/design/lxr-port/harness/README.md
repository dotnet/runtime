# LXR port benchmark harness

The P0.4 benchmark harness for the LXR garbage collector port. Full specification, methodology,
control evidence and the scenario matrix live in [`../P0.4-harness.md`](../P0.4-harness.md).

## What it is

A zero-package-dependency GC benchmark harness with ten scenarios, open-loop
coordinated-omission-free latency measurement, collector-identity verification from inside the
process under test, and configuration pinning with observed-versus-requested readback.

It runs on three hosts: the repository's bootstrapped SDK, the locally built runtime laid out as a
testhost, and the locally built runtime under CoreRun. The last two are what a future LXR arm will
use.

## Running it

```powershell
# smoke: proof of function across all three hosts and both collector arms
pwsh scripts/run-smoke.ps1

# one host, plus the seven controls
pwsh scripts/run-smoke.ps1 -Hosts sdk -Controls

# lay the ASP.NET shared framework over the built testhost, so the flagship
# scenario reaches the locally built runtime
pwsh scripts/compose-testhost-aspnet.ps1
```

The runner has four verbs — `hosts`, `matrix`, `controls` and `conformance`:

```powershell
dotnet <artifacts>/lxr-harness/build/bin/Lxr.Harness.Runner/release/Lxr.Harness.Runner.dll `
    hosts --repo-root <repo>
```

P3.1 adds an opt-in native scheduler adapter to `multi-thread-throughput`. It is disabled by
default and requires both `schedulerBenchmark=true` and
`P31_SCHEDULER_BENCHMARK_LIBRARY=<path>`. The adapter preserves the no-`PackageReference` rule and
is scheduler-substrate evidence, not a complete LXR collector arm.

P3.2 adds a harness-wide opt-in synthetic pause probe. It is disabled by default and requires
`P32_PAUSE_SCHEDULE_LIBRARY=<path>`. The worker parameters `pauseSchedulePolicy`,
`pauseScheduleCountPerKind`, and `pauseScheduleInjectedMicroseconds` select its fixed policy. Results
are written under a separate `pauseSchedule` object so synthetic scheduler durations cannot be
mistaken for `gc` pauses or complete-collector latency.

## Verifying it

```bash
bash scripts/verify-harness.sh              # audits the tree it ships in
bash scripts/verify-harness-control.sh      # proves the gate can fail
```

## Layout

| path | what |
|---|---|
| `src/Lxr.Harness.Core` | configuration model, identity and knob readback, statistics, result records and the schema-v2 conformance checker |
| `src/Lxr.Harness.Scenarios` | nine of the ten scenarios. **No ASP.NET reference** — this assembly has to load on CoreRun |
| `src/Lxr.Harness.Worker` | the process under test: one scenario, one configuration |
| `src/Lxr.Harness.Worker.AspNet` | the flagship scenario, separated so the ASP.NET framework reference cannot reach the shared assembly |
| `src/Lxr.Harness.Runner` | orchestrator: matrix, interleaving, timeouts, bounded dumps, aggregation, controls |
| `src/Lxr.Harness.Tests` | dependency-free assertion runner |
| `scripts/` | smoke entry point, testhost composition, and the two verification gates |

## Two properties worth preserving

**No `PackageReference`, anywhere.** The harness must restore and build offline, and must run
unchanged on CoreRun where there is no package resolution at all. The gate checks this.

**Invisible to the repository build.** `Directory.Build.props` and `Directory.Build.targets` here stop
MSBuild's walk-up before it reaches Arcade, `docs/` is excluded from every CI and PR trigger, and no
project glob in the repository reaches under `docs/`. All output goes to the gitignored
`artifacts/lxr-harness/`.

There is deliberately **no `global.json` here**: the harness builds with the repository's pinned SDK
so that its assemblies are in the same version band as a locally built runtime and can be loaded by
it. Adding one that rolled to a different SDK would break the CoreRun and testhost hosts.
