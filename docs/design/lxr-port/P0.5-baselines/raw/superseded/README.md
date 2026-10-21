# Superseded first measurement matrix

The primary matrix was measured twice. This directory holds what survives of the first run, which is
not the run P0.5 publishes. Its run directories lived under `artifacts/`, which is gitignored and is
destroyed with the worktree, so these two CSVs are the only remaining record.

It is kept because it is the evidence for two findings, and because a finding whose evidence has been
deleted is an assertion.

## `first-matrix-cells.csv`

Every published cell of the first run: 120 rows, both phases, as the runner aggregated them. The
`lifecycle-semantics.wks.h1.3` throughput cell is the F19 crash — `OutOfMemoryException` in
`ConditionalWeakTable.Container.Resize` at a 56 MiB heap, in a scenario calibration had recorded as
viable at 4 MiB. Viability is not monotone in heap size for that scenario, which is a property
bisection cannot represent.

Note that `dispatchLagP99Ms` is present on only 12 of these 120 rows. That is the defect, not a gap
in the extract: the aggregator computed the field only for scenarios whose catalogue entry declares
Latency as their primary metric. See below.

## `first-matrix-dispatch-lag.csv`

The same cells, but with the dispatch lag and the longest observed pause recomputed from the 600
per-invocation reports rather than read from the aggregate, because the aggregate does not contain
them. 66 cells reported a dispatch lag; the file has one row each.

This is the F16 evidence, and the numbers say something narrower than they first appear to:

| | cells |
|---|---|
| reported a dispatch lag | 66 |
| lag above the 1 ms bound | 31 |
| of those, published valid | 19 |
| rejected by the shipped rule (lag no pause explains) | 12 |
| kept by the shipped rule (lag a pause explains) | 19 |
| verdicts that differ between the old gate and the shipped rule | **0** |

The old gate ran behind `cell.Primary is PrimaryMetric.Latency`, a property of the scenario catalogue
entry rather than of the run. Nine of the ten scenarios run open-loop while declaring Throughput, so
the check was evaluated on 12 of the 66 cells that had something to check. Nineteen cells with a lag
over the bound were published valid without the gate ever looking at them.

Those nineteen were valid. A collector pause accounts for every one of them — `lifecycle-semantics`
at h6.0 published a 90.25 ms latency p99 against a 90.79 ms dispatch lag and a **129.78 ms** longest
pause, which is an in-process dispatcher being suspended by the collector it is measuring, and is a
real property of the run. So the first matrix did not publish wrong verdicts here. It published
accidental ones: the right answer on all 66 cells, reached on 54 of them by not asking the question.
The shipped rule derives the same verdicts from the lag and the pause, which is why it still holds
when the two diverge.

The twelve rejected cells are all `aspnet-request-load`, all at a lag of 11.08-11.30 ms. That is the
timer-granularity defect: `Thread.Sleep` rounds up to the host's 15.625 ms tick, so at 1,000 op/s the
dispatcher woke a tick late. Both that and the 1,000 op/s rate itself - which nothing had chosen; it
was the runner's global default standing in for a rate the scenario could not derive (F18) - are
fixed in the run P0.5 publishes.

## Reproducing

Neither CSV can be regenerated: the run directories are gone. They were extracted from
`artifacts/lxr-harness/runs/p05.s1.testhost.{throughput,latency}/` with the report-reading loop
recorded in section 6 of `P0.5-baselines.md`. The extract was itself wrong on the first attempt - it
read `dispatchLagP99Ms` from `results.json`, found it on 12 of 120 rows, and reported "0 cells
published valid over the bound", which would have contradicted the finding it was gathering evidence
for. It was caught because it printed the counts it compared rather than a verdict.
