// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Run:  dotnet run -c Release

using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

// Uncontended: single-threaded WaitAsync + Release. Primary target of the CAS fast path.
[MemoryDiagnoser]
public class Uncontended
{
    private SemaphoreSlim _sem = null!;

    [GlobalSetup]
    public void Setup() => _sem = new SemaphoreSlim(1, 1);

    [Benchmark]
    public async Task WaitAsync_Release()
    {
        await _sem.WaitAsync();
        _sem.Release();
    }
}

// Uncontended with higher initial count. Fast path fires every acquire; CAS never retries.
[MemoryDiagnoser]
public class UncontendedHighCount
{
    private SemaphoreSlim _sem = null!;

    [Params(2, 8)]
    public int InitialCount { get; set; }

    [GlobalSetup]
    public void Setup() => _sem = new SemaphoreSlim(InitialCount, InitialCount);

    [Benchmark]
    public async Task WaitAsync_Release()
    {
        await _sem.WaitAsync();
        _sem.Release();
    }
}

// Contended: N concurrent tasks racing for one permit.
// Verifies no regression under contention (fast path loses, slow path wins).
[MemoryDiagnoser]
public class Contended
{
    private SemaphoreSlim _sem = null!;

    [Params(2, 8)]
    public int Concurrency { get; set; }

    [GlobalSetup]
    public void Setup() => _sem = new SemaphoreSlim(1, 1);

    [Benchmark]
    public Task Contended_WaitAsync()
    {
        Task[] tasks = new Task[Concurrency];
        for (int i = 0; i < Concurrency; i++)
            tasks[i] = RunAsync(_sem);
        return Task.WhenAll(tasks);

        static async Task RunAsync(SemaphoreSlim s)
        {
            await s.WaitAsync();
            s.Release();
        }
    }
}

// Async-mutex throughput: 1000 iterations per invocation. Most common real-world pattern.
[MemoryDiagnoser]
public class AsyncMutex
{
    private const int Iterations = 1_000;
    private SemaphoreSlim _sem = null!;

    [GlobalSetup]
    public void Setup() => _sem = new SemaphoreSlim(1, 1);

    [Benchmark]
    public async Task SustainedThroughput()
    {
        for (int i = 0; i < Iterations; i++)
        {
            await _sem.WaitAsync();
            _sem.Release();
        }
    }
}

public class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromTypes(new[] {
            typeof(Uncontended),
            typeof(UncontendedHighCount),
            typeof(Contended),
            typeof(AsyncMutex),
        }).Run(args);
}
