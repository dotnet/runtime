# Local Benchmarking with Private Runtime Builds

This guide covers how to benchmark dotnet/runtime changes locally using
BenchmarkDotNet and privately-built runtime binaries (CoreRun). This approach
lets you measure performance without installing a custom SDK — BenchmarkDotNet
loads the locally-built runtime directly.

> **Note:** Build commands use the `build.cmd/sh` shorthand — run `build.cmd`
> on Windows or `./build.sh` on Linux/macOS. Other shell commands use
> Linux/macOS syntax. On Windows, adapt accordingly (use `Copy-Item` or `xcopy`,
> backslash paths, backtick line continuation).

## Building dotnet/runtime and Obtaining CoreRun

Build the runtime at the commit you want to test:

```
build.cmd/sh clr+libs -c release
```

The key artifact is the **testhost** folder containing **CoreRun** at:

```
artifacts/bin/testhost/net{version}-{os}-Release-{arch}/shared/Microsoft.NETCore.App/{version}/
```

CoreRun is a lightweight host that loads the locally-built runtime and
libraries. BenchmarkDotNet uses it via the `--coreRun` argument to benchmark
private builds without installing them as SDKs.

## Creating a Standalone Benchmark Project

For regression validation and bisection, use a standalone BenchmarkDotNet
project rather than the full [dotnet/performance](https://github.com/dotnet/performance)
repo. Standalone projects are faster to build, easier to iterate on, and more
reliable across different runtime commits.

### From an automated bot issue

Copy the relevant benchmark class from the `dotnet/performance` repo:

1. Clone `dotnet/performance` and locate the benchmark class referenced in the
   issue's `--filter` argument.
2. Create a new console project:
   ```
   mkdir PerfRepro && cd PerfRepro
   dotnet new console
   dotnet add package BenchmarkDotNet
   ```
3. Copy the benchmark class (and any helper types) into the project. Adjust
   namespaces and usings as needed.
4. Add a `Program.cs` entry point:
   ```csharp
   BenchmarkDotNet.Running.BenchmarkSwitcher
       .FromAssembly(typeof(Program).Assembly)
       .Run(args);
   ```

### From a customer report

Write a minimal BenchmarkDotNet benchmark that exercises the reported code path:

1. Create a new console project with `BenchmarkDotNet` as above.
2. Write a `[Benchmark]` method that calls the API or runs the workload the
   customer identified as slow.
3. If the customer provided sample code, adapt it into a proper BDN benchmark
   with `[GlobalSetup]` for initialization and `[Benchmark]` for the hot path.

## Comparing Good and Bad Commits

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

Run the standalone benchmark with both CoreRuns. BenchmarkDotNet compares them
side-by-side when given multiple `--coreRun` paths (the first is treated as the
baseline):

```
cd PerfRepro
dotnet run -c Release -f net{ver} -- \
    --filter '*' \
    --coreRun /tmp/corerun-good/.../CoreRun \
              /tmp/corerun-bad/.../CoreRun
```

To add a statistical significance column, append `--statisticalTest 5%`. This
performs a Mann–Whitney U test and marks results as `Faster`, `Slower`, or
`Same`.

## Interpreting Results

| Outcome | Meaning | Next step |
|---------|---------|-----------|
| `Slower` with ratio >1.10 | Regression confirmed | Proceed to bisection |
| `Slower` with ratio 1.05–1.10 | Small regression — likely real but needs confirmation | Re-run with `--iterationCount 30`. If it persists, treat as confirmed. |
| `Same` or within noise | Not reproduced locally | Check environment differences (OS, arch, CPU). Note in the report. |
| `Slower` but ratio <1.05 | Marginal — may be noise | Re-run with `--iterationCount 30`. If still marginal, note as inconclusive. |

## Using ResultsComparer

For a thorough comparison of saved BDN result files, use the
[ResultsComparer](https://github.com/dotnet/performance/tree/main/src/tools/ResultsComparer)
tool:

```
dotnet run --project performance/src/tools/ResultsComparer \
    --base /path/to/baseline-results \
    --diff /path/to/compare-results \
    --threshold 5%
```

## Incremental Rebuilds

Full rebuilds are slow. Minimize per-step build time by rebuilding only the
affected component:

| Component changed | Fast rebuild command |
|-------------------|---------------------|
| A single library (e.g., System.Text.Json) | `cd src/libraries/System.Text.Json/src && dotnet build -c Release --no-restore` |
| CoreLib | `build.cmd/sh clr.corelib -c Release` |
| CoreCLR (JIT, GC, runtime) | `build.cmd/sh clr -c Release` |
| All libraries | `build.cmd/sh libs -c Release` |

After an incremental library rebuild, the updated DLL is placed in the testhost
folder automatically. CoreRun picks up the new version on the next benchmark
run.

**Caveat:** If a rebuild crosses a commit that changes the build infrastructure
(e.g., SDK version bump in `global.json`), the incremental build may fail. In a
`git bisect` context, use exit code `125` (skip) to handle this gracefully.
