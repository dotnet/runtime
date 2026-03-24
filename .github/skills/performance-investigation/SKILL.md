---
name: performance-investigation
description: >
  Investigate performance regressions locally in dotnet/runtime. Use this skill
  when asked to investigate a performance regression, bisect to find a culprit
  commit, validate a regression with local builds, compare performance between
  commits using CoreRun, or benchmark private runtime builds with
  BenchmarkDotNet. Also use when asked about CoreRun, testhost, or local
  benchmarking against private builds. DO NOT USE FOR ad hoc PR benchmarking
  with @EgorBot or @MihuBot (use the performance-benchmark skill instead).
---

# Local Performance Investigation for dotnet/runtime

Investigate performance regressions locally by building the runtime at specific
commits, running BenchmarkDotNet with CoreRun, and using git bisect to find
culprit commits. This skill covers the full local investigation workflow from
validation to root-causing.

## When to Use This Skill

- Asked to **investigate a performance regression** (from an issue, bot report,
  or customer report)
- Asked to **compare performance** between commits, branches, or releases using
  local builds
- Asked to **bisect** to find the commit that introduced a regression
- Asked to **benchmark private runtime builds** using CoreRun
- Asked to **triage a performance issue** (use alongside the `issue-triage`
  skill for full triage)
- Given a `tenet-performance` or `tenet-performance-benchmarks` labeled issue
  that requires local investigation

> **Note:** For ad hoc PR benchmarking via @EgorBot or @MihuBot, use the
> `performance-benchmark` skill instead. This skill focuses on local builds,
> CoreRun, and git bisect.

## Investigation Workflow

The investigation follows three phases:

1. **Validate** — Confirm the regression is real and reproducible
2. **Narrow** — Reduce the commit range to a manageable size
3. **Bisect** — Binary-search for the culprit commit

For the full methodology, including feasibility checks, commit range
identification, and step-by-step bisection instructions, see the
[bisection guide](references/bisection-guide.md).

For details on building the runtime, using CoreRun, and running BenchmarkDotNet
against private builds, see the
[local benchmarking guide](references/local-benchmarking.md).

### Reporting Results

After completing the investigation, include in your report:

- Whether the regression was **confirmed** or **not reproduced**
- The **culprit commit/PR** (if bisection was performed)
- **Root cause analysis** — why the change caused the regression
- **Severity assessment** — Test/Base ratio, number of affected benchmarks,
  user impact

---

## Writing Good Benchmarks

These guidelines apply whether you're writing a benchmark for local validation
or for contribution to the dotnet/performance repo.

For comprehensive guidance, see the
[Microbenchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md).

### Key Principles

- **Move initialization to `[GlobalSetup]`** — separate setup from the measured
  code to avoid measuring allocation/initialization overhead
- **Return values** from benchmark methods to prevent dead code elimination
- **Avoid manual loops** — BenchmarkDotNet invokes the benchmark many times
  automatically; adding loops distorts measurements
- **No side effects** — benchmarks should be pure and produce consistent results
- **Focus on common cases** — benchmark hot paths and typical usage, not edge
  cases
- **Use consistent input data** — always use the same test data for reproducible
  comparisons

### Benchmark Class Requirements

- Must be `public`
- Must be a `class` (not struct)
- Must not be `sealed`
- Must not be `static`

### Example: Standalone Investigation Benchmark

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

[MemoryDiagnoser]
public class Bench
{
    private string _testString = default!;

    [Params(10, 100, 1000)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _testString = new string('a', Length);
    }

    [Benchmark]
    public int StringOperation()
    {
        return _testString.IndexOf('z');
    }
}
```

---

## External Resources

- [dotnet/performance repository](https://github.com/dotnet/performance) —
  central location for all .NET runtime benchmarks
- [Benchmarking workflow for dotnet/runtime](https://github.com/dotnet/performance/blob/master/docs/benchmarking-workflow-dotnet-runtime.md)
- [Profiling workflow for dotnet/runtime](https://github.com/dotnet/performance/blob/master/docs/profiling-workflow-dotnet-runtime.md)
- [Microbenchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md)
- [BenchmarkDotNet CLI arguments](https://benchmarkdotnet.org/articles/guides/console-args.html)
- [Performance guidelines](../../../../docs/project/performance-guidelines.md) —
  project-wide performance policy

## Related Skills

| Condition | Skill | When to use |
|-----------|-------|-------------|
| Need to benchmark a PR via @EgorBot | **performance-benchmark** | For ad hoc PR benchmarking on dedicated hardware |
| Triaging a performance regression issue | **issue-triage** | For the full triage workflow (assessment, recommendation, labels) |
| Fix PR linked to the regression | **code-review** | To review the fix for correctness and consistency |
| JIT regression test needed | **jit-regression-test** | To extract a JIT regression test from the issue |
