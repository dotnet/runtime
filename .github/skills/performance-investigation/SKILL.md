---
name: performance-investigation
description: >
  Investigate performance regressions and validate performance impact of code
  changes in dotnet/runtime. Use this skill whenever asked to benchmark a PR,
  investigate a performance regression, validate performance impact, run
  benchmarks, generate JIT diffs, compare performance between commits, triage
  a performance issue, or check whether a change improves or regresses
  performance. Also use when asked about @EgorBot, @MihuBot, BenchmarkDotNet,
  CoreRun, or dotnet/performance. Covers ad hoc PR benchmarking, deep
  regression investigation with git bisect, and JIT diff analysis.
---

# Performance Investigation for dotnet/runtime

Investigate performance regressions and validate the performance impact of code
changes. This skill covers three workflows, from quick PR validation to deep
regression root-causing.

## When to Use This Skill

- Asked to **benchmark** a PR or validate performance impact of a change
- Asked to **investigate a performance regression** (from an issue, bot report,
  or customer report)
- Asked to **generate JIT diffs** or analyze codegen impact
- Asked to **compare performance** between commits, branches, or releases
- Asked to **triage a performance issue** (use alongside the `issue-triage`
  skill for full triage)
- Given a `tenet-performance` or `tenet-performance-benchmarks` labeled issue
- Asked how to use `@EgorBot`, `@MihuBot`, BenchmarkDotNet, or CoreRun

## Choose Your Workflow

| Context | Workflow | What it does |
|---------|----------|-------------|
| PR is open and you want to measure its impact | [Workflow 1: PR Benchmark Validation](#workflow-1-pr-benchmark-validation) | Write a benchmark, invoke a bot, get results |
| A regression has been reported (issue or bot alert) | [Workflow 2: Regression Investigation](#workflow-2-regression-investigation) | Validate, bisect, root-cause |
| Change affects JIT codegen and you want to see diffs | [Workflow 3: JIT Diff Analysis](#workflow-3-jit-diff-analysis) | Generate JIT diffs via MihuBot |

If you're triaging a performance regression issue, use Workflow 2 for the
investigation methodology, then return to the `issue-triage` skill for
triage-specific assessment and recommendation.

---

## Workflow 1: PR Benchmark Validation

Use this when a PR is open and you want to measure its performance impact.

### Step 1: Write a BenchmarkDotNet Benchmark

Create a benchmark that targets the specific operation being changed. See
[Writing Good Benchmarks](#writing-good-benchmarks) below for best practices.

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

[MemoryDiagnoser]
public class Bench
{
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data
    }

    [Benchmark]
    public int MyOperation()
    {
        // Test the operation — return a value to prevent dead code elimination
        return 42;
    }
}
```

### Step 2: Choose a Bot and Invoke It

**Use @EgorBot** when you need to run custom benchmark code (written in Step 1):

Post a comment on the PR:

````
@EgorBot -amd -arm

```cs
// Your benchmark code here
```
````

EgorBot builds dotnet/runtime for the PR and base branch, runs the benchmark on
dedicated hardware, and posts BDN results back as a comment.

See [EgorBot reference](references/egorbot-reference.md) for the full target
list, options, and examples.

**Use @MihuBot** when you want to run existing benchmarks from the
[dotnet/performance](https://github.com/dotnet/performance) repo:

```
@MihuBot benchmark <filter>
```

This is useful when established benchmarks already cover the affected code path
and you don't need to write custom code.

See [MihuBot reference](references/mihubot-reference.md) for the full command
syntax and options.

### Step 3: Interpret Results

EgorBot and MihuBot post results as PR comments. Look for:

- **Ratio column** — values >1.0 indicate the PR is slower, <1.0 indicate it's
  faster
- **Statistical significance** — if a `--statisticalTest` column is present,
  look for `Faster`, `Slower`, or `Same` annotations
- **Memory/allocation changes** — check `Allocated` column if
  `[MemoryDiagnoser]` is enabled

---

## Workflow 2: Regression Investigation

Use this when a performance regression has been reported — whether from
`performanceautofiler[bot]`, a customer report, or a cross-release comparison.

### Overview

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

### Quick Path: Use Bots Instead of Local Bisection

If the regression range is narrow (a few commits) or the environment doesn't
support local builds, you can use bots to validate specific commits without
building locally:

```
@EgorBot -amd -commits {good-sha},{bad-sha}
```

Or with @MihuBot for existing benchmarks:

```
@MihuBot benchmark <filter> https://github.com/dotnet/runtime/compare/{good-sha}...{bad-sha}
```

This won't perform a full bisect, but it can confirm whether the regression
exists and help narrow the range.

### Reporting Results

After completing the investigation, include in your report:

- Whether the regression was **confirmed** or **not reproduced**
- The **culprit commit/PR** (if bisection was performed)
- **Root cause analysis** — why the change caused the regression
- **Severity assessment** — Test/Base ratio, number of affected benchmarks,
  user impact

---

## Workflow 3: JIT Diff Analysis

Use this when a change affects JIT code generation and you want to see how it
changes the emitted machine code across the entire BCL.

### Invoke MihuBot for JIT Diffs

Post a comment on the PR:

```
@MihuBot
```

MihuBot generates comprehensive JIT diffs showing codegen regressions and
improvements. For ARM64-specific diffs or tier-0 analysis:

```
@MihuBot -arm -tier0
```

See [MihuBot reference](references/mihubot-reference.md) for the full set of JIT
diff options and usage guidance.

### Interpreting JIT Diffs

MihuBot reports include:

- **Code size changes** — total bytes added/removed across all methods
- **Per-method diffs** — individual methods that changed, with before/after
  assembly
- **Regressions vs improvements** — clearly separated sections

A small increase in code size across many methods may indicate a JIT change with
broad impact. A large increase in a few methods may indicate a targeted
optimization that trades code size for speed (or a regression).

---

## Writing Good Benchmarks

These guidelines apply whether you're writing a benchmark for EgorBot, for
local validation, or for contribution to the dotnet/performance repo.

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

### Avoid `[DisassemblyDiagnoser]`

It causes crashes on Linux. To get disassembly, use the `--envvars` option
instead:

```
@EgorBot -amd --envvars DOTNET_JitDisasm:MethodName
```

### Example: Comparing Two Implementations

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
| Triaging a performance regression issue | **issue-triage** | For the full triage workflow (assessment, recommendation, labels) |
| Fix PR linked to the regression | **code-review** | To review the fix for correctness and consistency |
| JIT regression test needed | **jit-regression-test** | To extract a JIT regression test from the issue |
