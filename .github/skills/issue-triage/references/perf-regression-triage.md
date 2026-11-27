# Performance Regression Triage

Guidance for investigating and triaging performance regressions in
dotnet/runtime. Referenced from the main [SKILL.md](../SKILL.md) during Step 5.

> **Note:** Build commands use the `build.cmd/sh` shorthand — run `build.cmd`
> on Windows or `./build.sh` on Linux/macOS. Other shell commands use
> Linux/macOS syntax (`cp -r`, forward-slash paths, `\` line continuation).
> On Windows, adapt accordingly: use `Copy-Item` or `xcopy`, backslash paths,
> and backtick (`` ` ``) line continuation.

A performance regression is a report that something got measurably slower (or
uses more memory/allocations) compared to a previous .NET version or a recent
commit. These reports come from several sources:

- **Automated bot issues** -- filed by `performanceautofiler[bot]` with
  baseline/compare commits, regression tables, and repro commands.
- **Customer reports** -- a user reports that an operation became slower after
  upgrading to a new .NET version, often with sample code or a reproduction
  project.
- **Cross-release regressions** -- a regression observed between two stable
  releases (e.g., .NET 9 → .NET 10) without a specific commit range.

The goals of this triage are to:

1. **Validate** that the regression is real and reproducible.
2. **Bisect** to the exact commit that introduced it.

## Feasibility Check

Before investing time in benchmarking and bisection, assess whether the current
environment can support the investigation. Full bisection requires building
dotnet/runtime at multiple commits (each build takes 5-40 minutes) and running
benchmarks, which is resource-intensive.

| Factor | Feasible | Not feasible |
|--------|----------|--------------|
| **Disk space** | >50 GB free (for multiple builds) | <20 GB free |
| **Build time budget** | User is willing to wait 30-60+ min | Quick-turnaround triage expected |
| **OS/arch match** | Current environment matches the regression's OS/arch | Regression is Linux-only but running on Windows (or vice versa) |
| **SDK availability** | Can build dotnet/runtime at the relevant commits | Build infrastructure has changed too much between commits |
| **Benchmark complexity** | Simple, self-contained benchmark | Requires external services, databases, or specialized hardware |

### When full bisection is not feasible

Use the **lightweight analysis** path instead:

1. **Analyze `git log`** -- Review commits in the regression range
   (`git log --oneline {good}..{bad}`) and identify changes to the affected
   code path. Look for algorithmic changes, removed optimizations, added
   validation, or new allocations.
2. **Check PR descriptions** -- For each suspicious commit, read the associated
   PR description and review comments. Performance trade-offs are often
   discussed there.
3. **Narrow by code path** -- Use `git log --oneline {good}..{bad} -- path/`
   to filter commits to the affected library or component.
4. **Report the narrowed range** -- Include the list of candidate commits/PRs
   in the triage report with an explanation of why each is suspicious. This
   gives maintainers a head start even without a definitive bisect result.

Note in the triage report that full bisection was not attempted and why
(e.g., "environment mismatch", "time constraint"), so maintainers know to
verify independently.

## Identifying the Bisect Range

Before benchmarking, determine the good and bad commits that bound the
regression.

### Automated bot issues (`performanceautofiler`)

Issues from `performanceautofiler[bot]` follow a standard format:

- **Run Information** -- Baseline commit, Compare commit, diff link, OS, arch,
  and configuration (e.g., `CompilationMode:tiered`, `RunKind:micro`).
- **Regression tables** -- Each table shows benchmark name, Baseline time,
  Test time, and Test/Base ratio. A ratio >1.0 indicates a regression.
- **Repro commands** -- Typically:
  ```
  git clone https://github.com/dotnet/performance.git
  python3 .\performance\scripts\benchmarks_ci.py -f net10.0 --filter 'SomeBenchmark*'
  ```
- **Graphs** -- Time-series graphs showing when the regression appeared.

Key fields to extract:

- The **Baseline** and **Compare** commit SHAs -- these define the bisect range.
- The **benchmark filter** -- the `--filter` argument to reproduce the benchmark.
- The **Test/Base ratio** -- how severe the regression is (>1.5× is significant).

### Customer reports

When a customer reports a regression (e.g., "X is slower on .NET 10 than
.NET 9"), there are no pre-defined commit SHAs. You need to determine the
bisect range yourself -- see [Cross-release regressions](#cross-release-regressions)
below.

Also identify the **scenario to benchmark** from the customer's report -- the
specific API call, code pattern, or workload that regressed.

### Cross-release regressions

When a regression spans two .NET releases (e.g., .NET 9 → .NET 10), bisect
on the `main` branch between the commits from which the release branches were
snapped. Release branches in dotnet/runtime are
[snapped from main](../../../../docs/project/branching-guide.md).

Find the snap points with `git merge-base`:

```
git merge-base main release/9.0    # → good commit (last common ancestor)
git merge-base main release/10.0   # → bad commit
```

Use the resulting SHAs as the good/bad boundaries for bisection on `main`.
This avoids bisecting across release branches where cherry-picks and backports
make the history non-linear.

## Phase 1: Create a Standalone Benchmark

Before investing time in bisection, create a standalone BenchmarkDotNet
project that reproduces the regressing scenario. This project will be used
for both validation (Phase 1) and bisection (Phase 3).

### Why a standalone project?

The full [dotnet/performance](https://github.com/dotnet/performance) repo
has many dependencies and can be fragile across different runtime commits.
A standalone project with only the impacted benchmark is faster to build,
easier to iterate on, and more reliable during `git bisect`.

### Creating the benchmark project

**From an automated bot issue** -- copy the relevant benchmark class and its
dependencies from the `dotnet/performance` repo into a new standalone project:

1. Clone `dotnet/performance` and locate the benchmark class referenced in the
   issue's `--filter` argument.
2. Create a new console project and add a reference to
   `BenchmarkDotNet` (NuGet):
   ```
   mkdir PerfRepro && cd PerfRepro
   dotnet new console
   dotnet add package BenchmarkDotNet
   ```
3. Copy the benchmark class (and any helper types it depends on) into the
   project. Adjust namespaces and usings as needed.
4. Add a `Program.cs` entry point:
   ```csharp
   BenchmarkDotNet.Running.BenchmarkSwitcher
       .FromAssembly(typeof(Program).Assembly)
       .Run(args);
   ```

**From a customer report** -- write a minimal BenchmarkDotNet benchmark that
exercises the reported code path:

1. Create a new console project with `BenchmarkDotNet` as above.
2. Write a `[Benchmark]` method that calls the API or runs the workload the
   customer identified as slow.
3. If the customer provided sample code, adapt it into a proper BDN benchmark
   with `[GlobalSetup]` for initialization and `[Benchmark]` for the hot path.

### Building dotnet/runtime and obtaining CoreRun

Build dotnet/runtime at the commit you want to test:

```
build.cmd/sh clr+libs -c release
```

The key artifact is the **testhost** folder containing **CoreRun** at:

```
artifacts/bin/testhost/net{version}-{os}-Release-{arch}/shared/Microsoft.NETCore.App/{version}/
```

BenchmarkDotNet uses CoreRun to load the locally-built runtime and libraries,
meaning you can benchmark private builds without installing them as SDKs.

### Validating the regression

Build dotnet/runtime at both the good and bad commits, saving each testhost
folder:

```
git checkout {bad-sha}
build.cmd/sh clr+libs -c release
cp -r artifacts/bin/testhost/net{ver}-{os}-Release-{arch} /tmp/corerun-bad

git checkout {good-sha}
build.cmd/sh clr+libs -c release
cp -r artifacts/bin/testhost/net{ver}-{os}-Release-{arch} /tmp/corerun-good
```

Run the standalone benchmark with both CoreRuns. BenchmarkDotNet compares
them side-by-side when given multiple `--coreRun` paths (the first is treated
as the baseline):

```
cd PerfRepro
dotnet run -c Release -f net{ver} -- \
    --filter '*' \
    --coreRun /tmp/corerun-good/.../CoreRun \
              /tmp/corerun-bad/.../CoreRun
```

To add a statistical significance column, append `--statisticalTest 5%`.
This performs a Mann–Whitney U test and marks results as `Faster`, `Slower`,
or `Same`.

### Interpret the results

| Outcome | Meaning | Next step |
|---------|---------|-----------|
| `Slower` with ratio >1.10 | Regression confirmed | Proceed to Phase 2 |
| `Slower` with ratio between 1.05 and 1.10 | Small regression -- likely real but needs confirmation | Re-run with more iterations (`--iterationCount 30`). If it persists, treat as confirmed and proceed to Phase 2. |
| `Same` or within noise | Not reproduced locally | Check environment differences (OS, arch, CPU). Note in the report. |
| `Slower` but ratio <1.05 | Marginal -- may be noise | Re-run with more iterations (`--iterationCount 30`). If still marginal, note as inconclusive. |

For a thorough comparison of saved BDN result files, use the
[ResultsComparer](https://github.com/dotnet/performance/tree/main/src/tools/ResultsComparer)
tool:

```
dotnet run --project performance/src/tools/ResultsComparer \
    --base /path/to/baseline-results \
    --diff /path/to/compare-results \
    --threshold 5%
```

## Phase 2: Narrow the Commit Range

If the bisect range spans many commits, narrow it before running a full
bisect:

1. **Check `git log --oneline {good}..{bad}`** -- how many commits are in the
   range? If it is more than ~200, try to narrow it first.
2. **Test midpoint commits manually** -- pick a commit in the middle of the
   range, build, run the benchmark, and determine if it is good or bad.
   This halves the range in one step.
3. **For cross-release regressions** -- use the `git merge-base` snap points
   described above. If the range between two release snap points is still
   large, test at intermediate release preview tags to narrow further.

## Phase 3: Git Bisect

Once you have a manageable commit range (good commit and bad commit), use
`git bisect` to binary-search for the culprit.

### Bisect workflow

At each step of the bisect, you need to:

1. **Rebuild the affected component** -- use incremental builds where possible
   (see [Incremental Rebuilds](#incremental-rebuilds-during-bisect) below).
2. **Run the standalone benchmark** with the freshly-built CoreRun:
   ```
   cd PerfRepro
   dotnet run -c Release -f net{ver} -- \
       --filter '*' \
       --coreRun {runtime}/artifacts/bin/testhost/.../CoreRun
   ```
3. **Determine good or bad** -- compare the result against your threshold.

**Exit codes for `git bisect run`:**
- `0` -- good (no regression at this commit)
- `1`–`124` -- bad (regression present)
- `125` -- skip (build failure or untestable commit)

The standalone benchmark project must be **outside the dotnet/runtime tree**
since `git bisect` checks out different commits, which would overwrite
in-tree files. Place it in a stable location (e.g., `/tmp/bisect/`).

### Run the bisect

```
cd /path/to/runtime
git bisect start {bad-sha} {good-sha}
git bisect run /path/to/bisect-script.sh
```

**Time estimate:** Each bisect step requires a rebuild + benchmark run.
For ~1000 commits (log₂(1000) ≈ 10 steps) with a 5-minute rebuild, expect
roughly 50 minutes for the full bisect.

### After bisect completes

`git bisect` will output the first bad commit. Run `git bisect reset` to
return to the original branch.

### Root cause analysis and triage report

Include the following in the triage report:

1. **The culprit commit or PR** -- link to the specific commit SHA and its
   associated PR. Explain how the change relates to the regressing benchmark.
2. **Root cause analysis** -- describe *why* the change caused the regression
   (e.g., an algorithm change, a removed optimization, additional validation
   overhead).
3. **If the root cause spans multiple PRs** -- sometimes a regression results
   from the combined effect of several changes and `git bisect` lands on a
   commit that is only one contributing factor. In this case, report the
   narrowest commit range that introduced the regression and list the PRs or
   commits within that range that appear relevant to the affected code path.

## Incremental Rebuilds During Bisect

Full rebuilds are slow. Minimize per-step build time:

| Component changed | Fast rebuild command |
|-------------------|---------------------|
| A single library (e.g., System.Text.Json) | `cd src/libraries/System.Text.Json/src && dotnet build -c Release --no-restore` |
| CoreLib | `build.cmd/sh clr.corelib -c Release` |
| CoreCLR (JIT, GC, runtime) | `build.cmd/sh clr -c Release` |
| All libraries | `build.cmd/sh libs -c Release` |

After an incremental library rebuild, the updated DLL is placed in the
testhost folder automatically. CoreRun will pick up the new version on the
next benchmark run.

**Caveat:** If bisect crosses a commit that changes the build infrastructure
(e.g., SDK version bump in `global.json`), the incremental build may fail.
Use exit code `125` (skip) to handle this gracefully.

## Performance-Specific Assessment

When assessing a performance regression in Step 6, consider:

- **Severity** -- What is the Test/Base ratio? >2× is severe; 1.1–1.2× may be
  noise or acceptable.
- **Breadth** -- How many benchmarks regressed? A single narrow benchmark vs.
  hundreds of benchmarks suggests different root causes.
- **Affected component** -- Is it JIT (codegen), GC, a specific library, or
  cross-cutting?
- **User impact** -- Does the regressing benchmark represent a real-world hot
  path, or is it a synthetic microbenchmark?
- **Trade-off** -- Was the regression an intentional trade-off for correctness,
  security, or another dimension? Check the PR description of the culprit
  commit.

## Performance-Specific Recommendation Criteria

### KEEP

- Regression is confirmed with statistical significance
- Regression is not an intentional trade-off documented in the culprit PR
- Bisect identified a specific culprit commit (include it in the report)

### CLOSE

- Regression is not reproducible locally and the automated data appears noisy
- Regression was an intentional, documented trade-off
- Regression has already been fixed in a subsequent commit

### NEEDS INFO

- Regression is marginal (Test/Base <1.05) and could be noise -- request a
  re-run on the performance infrastructure
- Environment-specific regression that cannot be reproduced locally -- note the
  environment mismatch
