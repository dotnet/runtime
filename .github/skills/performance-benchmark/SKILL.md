---
name: performance-benchmark
description: Generate and run ad hoc performance benchmarks to validate code changes. Use this when asked to benchmark, profile, or validate the performance impact of a code change in dotnet/runtime.
---

# Ad Hoc Performance Benchmarking

When you need to validate the performance impact of a code change, follow this process to write a BenchmarkDotNet benchmark and trigger EgorBot to run it.

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

## Step 2: Post the EgorBot Comment

Post a comment on the PR to trigger EgorBot with your benchmark. The general format is:

```
@EgorBot [target flags] [options] [BenchmarkDotNet args]

```cs
// Your benchmark code here
```
```

### Target Flags (Required - Choose at Least One)

| Flag | Architecture | Description |
|------|--------------|-------------|
| `-x64` or `-amd` | x64 | Linux Azure Genoa (AMD EPYC) - default x64 target |
| `-arm` | ARM64 | Linux Azure Cobalt100 (Neoverse-N2) |
| `-intel` | x64 | Azure Cascade Lake (more flaky due to JCC Erratum and loop alignment sensitivity) |
| `-windows_x64` | x64 | Windows x64 (when Windows-specific testing is needed) |

**Choosing targets:**

- **Default for most changes**: Use `-x64` for quick verification of non-architecture/non-OS specific changes
- **Default when ARM might differ**: Use `-x64 -arm` if there's any suspicion the change might behave differently on ARM
- **Windows-specific changes**: Use `-windows_x64` when Windows behavior needs testing
- **Noisy results suspected**: Use `-arm -intel -amd` to get results from multiple x64 CPUs (note: `-intel` targets are more flaky)

### Common Options

| Option | Description |
|--------|-------------|
| `-profiler` | Collect flamegraph/hot assembly using perf record |
| `--envvars KEY:VALUE` | Set environment variables (e.g., `DOTNET_JitDisasm:MethodName`) |
| `-commit <hash>` | Run against a specific commit |
| `-commit <hash1> vs <hash2>` | Compare two commits |
| `-commit <hash> vs previous` | Compare commit with its parent |

### Example: Basic PR Benchmark

To benchmark the current PR changes against the base branch:

```
@EgorBot -x64 -arm

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
```

### Example: Benchmark with Profiling and Disassembly

```
@EgorBot -x64 -profiler --envvars DOTNET_JitDisasm:SumArray

```cs
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

public class Bench
{
    private int[] _data = Enumerable.Range(0, 1000).ToArray();

    [Benchmark]
    public int SumArray() => _data.Sum();
}
```
```

### Example: Compare Two Commits

```
@EgorBot -amd -commit abc1234 vs def5678

```cs
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Bench).Assembly).Run(args);

public class Bench
{
    [Benchmark]
    public void TestMethod()
    {
        // Benchmark code
    }
}
```
```

### Example: Run Existing dotnet/performance Benchmarks

To run benchmarks from the dotnet/performance repository (no code snippet needed):

```
@EgorBot -arm -intel --filter `*TryGetValueFalse<String, String>*`
```

**Note**: Surround filter expressions with backticks to avoid issues with special characters.

## Important Notes

- **Bot response time**: EgorBot uses polling and may take up to 30 seconds to respond
- **Supported repositories**: EgorBot monitors `dotnet/runtime` and `EgorBot/runtime-utils`
- **PR mode (default)**: When posting in a PR, EgorBot automatically compares the PR changes against the base branch
- **Results variability**: Results may vary between runs due to VM differences. Do not compare results across different architectures or cloud providers
- **Check the manual**: EgorBot replies include a link to the [manual](https://github.com/EgorBot/runtime-utils) for advanced options

## Additional Resources

- [Microbenchmark Design Guidelines](https://github.com/dotnet/performance/blob/main/docs/microbenchmark-design-guidelines.md) - Essential reading for writing effective benchmarks
- [BenchmarkDotNet CLI Arguments](https://github.com/dotnet/BenchmarkDotNet/blob/master/docs/articles/guides/console-args.md)
- [EgorBot Manual](https://github.com/EgorBot/runtime-utils)
- [BenchmarkDotNet Filter Simulator](http://egorbot.westus2.cloudapp.azure.com:5042/microbenchmarks)
