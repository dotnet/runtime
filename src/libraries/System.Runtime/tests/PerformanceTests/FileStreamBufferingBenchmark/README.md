# FileStream Buffering Optimization Benchmark

This benchmark validates the performance improvements from disabling FileStream internal buffering when wrapped by StreamReader/StreamWriter.

## Background

When a FileStream is wrapped by StreamReader or StreamWriter, having both layers of buffering active causes:
- Additional memory allocations for `BufferedFileStreamStrategy`
- GC pressure from finalizers
- Lock contention on every ReadAsync/WriteAsync
- Reduced throughput

By setting `bufferSize: 1` on FileStream, we skip the BufferedFileStreamStrategy wrapper and let StreamReader/StreamWriter handle all buffering.

## Running the Benchmark

From the repository root:

```bash
cd src/libraries/System.Runtime/tests/PerformanceTests/FileStreamBufferingBenchmark
dotnet run -c Release
```

## What's Being Tested

The benchmark tests these public APIs that benefit from the optimization:

1. **File.WriteAllLinesAsync** - Writing lines to a file
2. **File.AppendAllLinesAsync** - Appending lines to a file
3. **File.ReadAllTextAsync** - Reading entire file content
4. **File.ReadAllLinesAsync** - Reading all lines from a file
5. **StreamReader(path)** - Path-taking StreamReader constructor
6. **StreamWriter(path)** - Path-taking StreamWriter constructor
7. **FileInfo.OpenText()** - Opening file for reading
8. **FileInfo.CreateText()** - Creating file for writing
9. **FileInfo.AppendText()** - Appending to file

## Expected Results

With the optimization (bufferSize: 1), you should see:
- **Lower memory allocations** (fewer bytes allocated, fewer Gen0/Gen1/Gen2 collections)
- **Faster execution times** (especially for larger files)
- **Better throughput** for both read and write operations

The improvements are most noticeable when:
- Working with files larger than the buffer size (4KB)
- Performing many small I/O operations
- Running under memory pressure

## Baseline Comparison

To compare before/after, you would need to:
1. Checkout the commit before the changes
2. Run the benchmark and save results
3. Checkout the commit with changes
4. Run the benchmark again
5. Use BenchmarkDotNet's comparison tools to analyze the difference

Example:
```bash
# Before changes
git checkout <commit-before-changes>
dotnet run -c Release --exporters json --artifacts ./before

# After changes  
git checkout <commit-with-changes>
dotnet run -c Release --exporters json --artifacts ./after

# Compare results manually or use BenchmarkDotNet comparison features
```
