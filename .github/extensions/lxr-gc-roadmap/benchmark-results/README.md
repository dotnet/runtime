# Benchmark result files

Agents write one JSON file per benchmark checkpoint into this directory. The canvas loads every
`*.json` file, merges its `checkpoints` array, and orders checkpoints by date.

`schemaVersion` is **required** and must be `1` or `2`. `checkpoints` must be a top-level array: a
file that omits it, declares an unknown `schemaVersion`, is not valid JSON, or contains a checkpoint
without an `id`, a `YYYY-MM-DD` `date`, or a `results` array is **ignored, named in the canvas, and
does not prevent the other files from loading**.

```json
{
    "schemaVersion": 2,
    "checkpoints": [
        {
            "id": "p4-4-stw-sweep-removed",
            "date": "2026-08-07",
            "stepId": "P4.4",
            "notes": "Controlled A/B/C run details and interpretation.",
            "results": [
                {
                    "scenario": "webapi",
                    "collector": "lxr",
                    "operationsPerSecond": 250.0,
                    "pauseAverageMs": 5.0,
                    "pauseP99Ms": 12.0,
                    "pauseMaxMs": 20.0,
                    "workingSetMb": 250.0,
                    "committedMb": 220.0,
                    "notes": "Run count, duration, configuration, and failures."
                }
            ]
        }
    ]
}
```

Use the same checkpoint ID, date, step ID, workload inputs, runtime build, duration, warmup, and run
count for LXR, Workstation GC, and Server GC results. Use `null` for unavailable metrics.

**`null` means "not measured" and is never charted as zero.** A metric that is `null`, absent, or
otherwise not a finite number is omitted from the series rather than plotted at the origin. Emitting
`0` for a scenario that was never observed to pause publishes a collector with perfect zero pauses,
which is a fabricated result rather than a formatting choice.

## Schema version 2

v2 is **additive**: every v1 field keeps its name and meaning, so a v1 reader still parses a v2 file
and a v1 file still loads. v2 exists because v1 is pause-centric and has no field for
application-observed latency — which P0.2 establishes is the *acceptance* signal, with pause
distribution being characterization rather than a win condition. Table 5's supported G1-relative
target is a **99.99th percentile** latency ratio, so `latencyP9999Ms` is not optional.

| field | meaning |
|---|---|
| `latencyP50Ms`, `latencyP99Ms`, `latencyP999Ms`, `latencyP9999Ms`, `latencyMaxMs` | application-observed latency percentiles — the acceptance signal |
| `latencyMethod` | how latency was measured, e.g. `open-loop-intended-start`. **A record without it is not coordinated-omission-free** |
| `arrivalRatePerSecond`, `achievedRatePerSecond`, `overloaded` | offered load; an open-loop latency number is meaningless without it, and an overloaded run measures capacity rather than latency |
| `heapFactor`, `heapLimitMb` | the paper's results invert with heap generosity, so a throughput number without its heap factor is unusable |
| `requestedConfig`, `observedConfig`, `configEvidence`, `unverifiedKnobs` | what was asked for versus what the runtime reported, with the strength of the evidence recorded |
| `collectorConfirmed`, `valid`, `invalidReason` | a run whose collector cannot be confirmed is invalid and must be representable as such |
| `status`, `skipReason` | `ok`, `failed`, `timeout`, `crashed`, or `skipped`; a skip must be **declared**, never a silent absence |
| `runtimeBuildId`, `runtimeDescription`, `coreclrSha256` | ties a number to the commit that produced it |
| `ratioStatistic`, `ratioVsBaseline`, `ratioCiLow`, `ratioCiHigh`, `ciMethod` | ratio against the baseline arm with its confidence interval and the method that produced it |
| `invocations`, `seed`, `warmupSeconds`, `steadyStateSeconds` | reproducibility |
| `rawSamplesPath` | raw sample retention; a summary cannot be re-analysed |
| `inducedCollections` | explicit `GC.Collect()` count, so "no induced collections" is checkable rather than asserted |
| `machine` | cpu, physical and logical cores, ramGb, powerPlan, virtualized, backgroundLoadPercent, `noisy` |

Records with `valid: false` or a `status` other than `ok` are **listed in the canvas but excluded
from the charts**, so a failed or unconfirmed run stays visible without contaminating a trend line.

## `archive/`

The canvas reads only the top level of this directory, so anything moved into `archive/` disappears
from the charts while remaining on disk for provenance. `archive/baseline-2026-08-03.json` holds the
checkpoint from the abandoned `C:\github\runtimelab` prototype: its `lxr` rows measure a collector
that no longer exists and its baseline rows came from a different harness, so it must not be compared
against results produced by the current roadmap. Fresh baselines come from step P0.5.
