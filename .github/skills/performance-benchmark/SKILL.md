---
name: performance-benchmark
description: Generate and run ad hoc performance benchmarks to validate code changes. Use this when asked to benchmark, profile, or validate the performance impact of a code change in dotnet/runtime.
---

# Ad Hoc Performance Benchmarking with @EgorBot

When you need to validate the performance impact of a code change, follow this process to write a BenchmarkDotNet benchmark and trigger @EgorBot to run it.
The bot will notify you when results are ready, so don't wait for them.

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

## Step 2: Mention @EgorBot in a comment/PR description

Post a comment on the PR to trigger EgorBot with your benchmark. The general format is:

@EgorBot [targets] [options] [BenchmarkDotNet args]

```cs
// Your benchmark code here
```
> **Note:** The @EgorBot command must not be inside the code block. Only the benchmark code should be inside the code block.

### Target Flags

- `-linux_amd`
- `-linux_intel`
- `-windows_amd`
- `-windows_intel`
- `-linux_arm64`
- `-osx_arm64` (baremetal, feel free to always include it)

The most common combination is `-linux_amd -osx_arm64`. Do not include more than 4 targets.

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
- [EgorBot Manual](https://github.com/EgorBo/EgorBot?tab=readme-ov-file#github-usage)
