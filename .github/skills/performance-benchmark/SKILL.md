---
name: performance-benchmark
description: Generate and run ad hoc performance benchmarks to validate code changes. Use this when asked to benchmark, profile, or validate the performance impact of a code change in dotnet/runtime.
---

# Ad Hoc Performance Benchmarking Locally (or with @EgorBot)

When you need to validate the performance impact of a code change, follow this process to write a BenchmarkDotNet benchmark and compare local baseline and changed builds.

## Step 1: Write the Benchmark

Create a BenchmarkDotNet benchmark that tests the specific operation being changed. Follow these guidelines:

### Benchmark Structure

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

public class Bench
{
    // Add setup/cleanup if needed
    [GlobalSetup]
    public void Setup()
    {
        // Initialize test data
    }

    [Benchmark]
    public void MyOperation()
    {
        // Test the operation
    }
}
```

### Best Practices

For comprehensive guidance, see the [Microbenchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md).

Key principles:

- **Move initialization to `[GlobalSetup]`**: Separate setup logic from the measured code to avoid measuring allocation/initialization overhead
- **Return values** from benchmark methods to prevent dead code elimination
- **Avoid loops**: BenchmarkDotNet invokes the benchmark many times automatically; adding manual loops distorts measurements
- **No side effects**: Benchmarks should be pure and produce consistent results
- **Focus on common cases**: Benchmark hot paths and typical usage, not edge cases or error paths
- **Use consistent input data**: Always use the same test data for reproducible comparisons
- **Avoid `[DisassemblyDiagnoser]`**: It causes crashes on Linux. Use `--envvars DOTNET_JitDisasm:MethodName` instead
- **Benchmark class requirements**: Must be `public`, not `sealed`, not `static`, and must be a `class` (not struct)

### Example: String Operation Benchmark

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

### Example: Collection Operation Benchmark

```csharp
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

[MemoryDiagnoser]
public class Bench
{
    private int[] _array = default!;
    private List<int> _list = default!;

    [Params(100, 1000, 10000)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _array = Enumerable.Range(0, Count).ToArray();
        _list = _array.ToList();
    }

    [Benchmark]
    public bool AnyArray() => _array.Any();

    [Benchmark]
    public bool AnyList() => _list.Any();

    [Benchmark]
    public int SumArray() => _array.Sum();

    [Benchmark]
    public int SumList() => _list.Sum();
}
```

## Step 2: Prepare Baseline and Changed Runtime Builds

At this point the change is typically already present in the working tree.

1. Save only the intended changes safely in a commit, patch, or separate worktree. Do not stash or revert unrelated changes.
2. Temporarily remove the changes and return the source to the baseline state.
3. Build Release runtime and testhost artifacts. For JIT, VM, and shared-framework library changes, run the repository build script for the current operating system with:

```text
./build.cmd|.sh clr+libs -rc Release -lc Release
```

The `libs` subset includes `libs.pretest`, which constructs and updates the testhost. The `libs.tests` subset is not needed for benchmarking.

4. Copy the generated testhost directory next to itself as `testhost_baseline`:

```text
artifacts/bin/testhost -> artifacts/bin/testhost_baseline
```

5. Restore the changes and run exactly the same Release build again. You can save time by just copying the changed bit to the artifacts/bin/testhost if you know exactly which component was changed.

The baseline remains in `artifacts/bin/testhost_baseline`, while the normal `artifacts/bin/testhost` directory now contains the changed runtime. Use the corresponding `CoreRun` executable under each directory.

Copying the directory preserves the baseline while leaving the normal testhost and other artifacts available for an incremental changed build. If the changed runtime was already built before restoring the baseline source, clean or explicitly rebuild the affected component to avoid capturing stale binaries.

For libraries outside the shared framework, build the library in Release and place the exact baseline or changed assembly, plus required dependencies, beside the corresponding `CoreRun`. Use the same layout for both testhosts.

## Step 3: Run the Benchmark Locally

Run the benchmark created in Step 1 against both hosts. The first `CoreRun` is the baseline:

```
dotnet run -c Release -- --filter "*" --coreRun "<baseline-corerun>" "<changed-corerun>"
```

Use a BenchmarkDotNet version compatible with the repository's current target framework. If it fails with `GetRuntimeVersion not implemented for NotRecognized`, update BenchmarkDotNet to a compatible preview or nightly version.

Optionally, you can pass additional environment variables to the benchmark process using `--envvars`. For example, to enable JIT disassembly for a specific method:

```
--envvars DOTNET_JitDisasm:MethodName
```

## @EgorBot Usage

[@EgorBot](https://github.com/EgorBo/EgorBot/blob/main/README.md) is a GitHub bot that runs BenchmarkDotNet snippets against `dotnet/runtime` PR changes and reports comparisons with the PR's base branch. It is only useful on GitHub for PRs in the `dotnet/runtime` repository.

Only use @EgorBot when the user explicitly asks for it. Prefer the local workflow above otherwise. The bot will notify you when results are ready, so do not wait for them.

Post a comment on the PR to trigger EgorBot with the benchmark. The general format is:

> 📝 **AI-generated content disclosure:** When posting benchmark comments to GitHub under a user's credentials — i.e., the account is **not** a dedicated "copilot" or "bot" account/app (e.g., `github-actions[bot]`, `copilot`) — you **MUST** include a concise, visible note (e.g. a `> [!NOTE]` alert) at the bottom of the content indicating the content was AI/Copilot-generated. Skip this if the user explicitly asks you to omit it.

@EgorBot [targets] [options] [BenchmarkDotNet args]

```cs
// Your benchmark code here
```
> **Note:** When using @EgorBot, follow these formatting rules:
> - The @EgorBot command must not be inside the code block.
> - Only the benchmark code should be inside the code block.
> - Do not place any additional text between the @EgorBot command line and the code block, as EgorBot will treat it as additional command arguments.

### Target Flags

- `-linux_amd`
- `-linux_intel`
- `-windows_amd`
- `-windows_intel`
- `-linux_arm64`
- `-osx_arm64` (baremetal, feel free to always include it)

The most common combination is `-linux_amd -osx_arm64`. Do not include more than 3 targets.

### Common Options

Use `-profiler` when absolutely necessary along with `-linux_arm64` and/or `-linux_amd` to include `perf` profiling and disassembly in the results.

### Example: Basic PR Benchmark

To benchmark the current PR changes against the base branch:

@EgorBot -linux_amd -osx_arm64

```cs
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

[MemoryDiagnoser]
public class Bench
{
    [Benchmark]
    public int MyOperation()
    {
        // Your benchmark code
        return 42;
    }
}
```

## Important Notes

- **Bot response time**: EgorBot uses polling and may take up to 30 seconds to respond
- **Supported repositories**: EgorBot monitors `dotnet/runtime` and `EgorBot/runtime-utils`
- **PR mode (default)**: When posting in a PR, EgorBot automatically compares the PR changes against the base branch
- **Results variability**: Results may vary between runs due to VM differences. Do not compare results across different architectures or cloud providers
- **Check the manual**: EgorBot replies include a link to the [manual](https://github.com/EgorBo/EgorBot?tab=readme-ov-file#github-usage) for advanced options

## Additional Resources

- [Microbenchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md) - Essential reading for writing effective benchmarks
- [BenchmarkDotNet CLI Arguments](https://github.com/dotnet/BenchmarkDotNet/blob/master/docs/articles/guides/console-args.md)
- [dotnet/performance benchmark suite](https://github.com/dotnet/performance)
- [EgorBot Manual](https://github.com/EgorBo/EgorBot?tab=readme-ov-file#github-usage)
