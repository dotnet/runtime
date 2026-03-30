# Git Bisect for Performance Regressions

This guide covers how to use `git bisect` to find the exact commit that
introduced a performance regression. It's a 3-phase process: validate the
regression, narrow the commit range, then bisect.

## Feasibility Check

Before investing time in bisection, assess whether the current environment can
support the investigation. Full bisection requires building dotnet/runtime at
multiple commits (each build takes 5–40 minutes) and running benchmarks, which
is resource-intensive.

| Factor | Feasible | Not feasible |
|--------|----------|--------------|
| **Disk space** | >50 GB free (multiple builds) | <20 GB free |
| **Build time budget** | Willing to wait 30–60+ min | Quick-turnaround expected |
| **OS/arch match** | Current environment matches the regression's OS/arch | Regression is Linux-only but running on Windows (or vice versa) |
| **SDK availability** | Can build dotnet/runtime at the relevant commits | Build infrastructure has changed too much between commits |
| **Benchmark complexity** | Simple, self-contained benchmark | Requires external services, databases, or specialized hardware |

### When full bisection is not feasible

Use a **lightweight analysis** path instead:

1. **Analyze `git log`** — Review commits in the regression range
   (`git log --oneline {good}..{bad}`) and identify changes to the affected code
   path. Look for algorithmic changes, removed optimizations, added validation,
   or new allocations.
2. **Check PR descriptions** — For each suspicious commit, read the associated
   PR description and review comments. Performance trade-offs are often discussed
   there.
3. **Narrow by code path** — Use `git log --oneline {good}..{bad} -- path/` to
   filter commits to the affected library or component.
4. **Report the narrowed range** — Include the list of candidate commits/PRs with
   an explanation of why each is suspicious. This gives maintainers a head start
   even without a definitive bisect result.

Note in the report that full bisection was not attempted and why.

## Identifying the Bisect Range

Determine the good and bad commits that bound the regression.

### Automated bot issues (`performanceautofiler`)

Issues from `performanceautofiler[bot]` follow a standard format:

- **Run Information** — Baseline commit, Compare commit, diff link, OS, arch,
  and configuration (e.g., `CompilationMode:tiered`, `RunKind:micro`).
- **Regression tables** — Each table shows benchmark name, Baseline time, Test
  time, and Test/Base ratio. A ratio >1.0 indicates a regression.
- **Repro commands** — Typically:
  ```
  git clone https://github.com/dotnet/performance.git
  python3 .\performance\scripts\benchmarks_ci.py -f net10.0 --filter 'SomeBenchmark*'
  ```
- **Graphs** — Time-series graphs showing when the regression appeared.

Key fields to extract:

- The **Baseline** and **Compare** commit SHAs — these define the bisect range.
- The **benchmark filter** — the `--filter` argument to reproduce the benchmark.
- The **Test/Base ratio** — how severe the regression is (>1.5× is significant).

### Customer reports

When a customer reports a regression (e.g., "X is slower on .NET 10 than
.NET 9"), there are no pre-defined commit SHAs. Determine the bisect range using
the cross-release approach below.

### Cross-release regressions

When a regression spans two .NET releases (e.g., .NET 9 → .NET 10), bisect on
the `main` branch between the commits from which the release branches were
snapped. Release branches in dotnet/runtime are
[snapped from main](../../../../docs/project/branching-guide.md).

Find the snap points with `git merge-base`:

```
git merge-base main release/9.0    # → good commit (last common ancestor)
git merge-base main release/10.0   # → bad commit
```

Use the resulting SHAs as the good/bad boundaries for bisection on `main`. This
avoids bisecting across release branches where cherry-picks and backports make
the history non-linear.

## Phase 1: Validate the Regression

Before bisecting, confirm the regression is reproducible. Create a standalone
BenchmarkDotNet project (see
[local benchmarking guide](local-benchmarking.md#creating-a-standalone-benchmark-project)),
build the runtime at the good and bad commits, and compare results.

If the regression is not reproducible locally, check for environment differences
(OS, arch, CPU model) and note this in your report. Consider using
[@EgorBot](egorbot-reference.md) to validate on dedicated hardware instead.

## Phase 2: Narrow the Commit Range

If the bisect range spans many commits, narrow it before running a full bisect:

1. **Check `git log --oneline {good}..{bad}`** — how many commits are in the
   range? If more than ~200, narrow first.
2. **Test midpoint commits manually** — pick a commit in the middle of the range,
   build, run the benchmark, and determine if it is good or bad. This halves the
   range in one step.
3. **For cross-release regressions** — use the `git merge-base` snap points. If
   the range between two release snap points is still large, test at intermediate
   release preview tags to narrow further.

## Phase 3: Git Bisect

Once you have a manageable commit range, use `git bisect` to binary-search for
the culprit.

### Bisect workflow

At each step:

1. **Rebuild the affected component** — use incremental builds where possible
   (see [incremental rebuilds](local-benchmarking.md#incremental-rebuilds)).
2. **Run the standalone benchmark** with the freshly-built CoreRun from the
   testhost folder (see
   [local benchmarking guide](local-benchmarking.md#building-dotnet-runtime-and-obtaining-corerun)
   for the exact path):
   ```
   cd PerfRepro
   dotnet run -c Release -f net{ver} -- \
       --filter '*' \
       --coreRun {runtime}/artifacts/bin/testhost/net{ver}-{os}-Release-{arch}/shared/Microsoft.NETCore.App/{ver}/CoreRun
   ```
3. **Determine good or bad** — compare the result against your threshold.

**Exit codes for `git bisect run`:**
- `0` — good (no regression at this commit)
- `1`–`124` — bad (regression present)
- `125` — skip (build failure or untestable commit)

The standalone benchmark project must be **outside the dotnet/runtime tree**
since `git bisect` checks out different commits which would overwrite in-tree
files. Place it in a stable location (e.g., `/tmp/bisect/`).

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

`git bisect` outputs the first bad commit. Run `git bisect reset` to return to
the original branch.

## Root Cause Analysis

Include the following in your report:

1. **The culprit commit or PR** — link to the specific commit SHA and its
   associated PR. Explain how the change relates to the regressing benchmark.
2. **Root cause analysis** — describe *why* the change caused the regression
   (e.g., an algorithm change, a removed optimization, additional validation
   overhead).
3. **If the root cause spans multiple PRs** — sometimes a regression results
   from the combined effect of several changes and `git bisect` lands on a
   commit that is only one contributing factor. In this case, report the
   narrowest commit range and list the PRs within that range that appear
   relevant to the affected code path.
