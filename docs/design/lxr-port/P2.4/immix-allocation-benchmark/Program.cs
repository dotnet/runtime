// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P24.Benchmarks;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(
            typeof(ImmixAllocationBenchmarks).Assembly).Run(args);
}

public class ImmixAllocationBenchmarks
{
    private const int Operations = 32;
    private nint _library;
    private InitializeDelegate _initialize = null!;
    private ShutdownDelegate _shutdown = null!;
    private RunBatchDelegate _runBatch = null!;

    [Params(1, 4, 16)]
    public int WorkerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P24_NATIVE_BENCHMARK")
            ?? throw new InvalidOperationException(
                "P24_NATIVE_BENCHMARK is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = Load<InitializeDelegate>("P24_Initialize");
        _shutdown = Load<ShutdownDelegate>("P24_Shutdown");
        _runBatch = Load<RunBatchDelegate>("P24_RunBatch");
        int result = _initialize();
        if (result != 0)
        {
            throw new InvalidOperationException(
                $"Native initialization failed: {result}.");
        }

    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _shutdown();
        NativeLibrary.Free(_library);
    }

    [Benchmark(OperationsPerInvoke = Operations, Baseline = true)]
    public nuint Fresh() => Run(BenchmarkMode.Fresh);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint FreshControl() => Run(BenchmarkMode.Fresh);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint Recycled() => Run(BenchmarkMode.Recycled);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint RecycledControl() => Run(BenchmarkMode.Recycled);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ConcurrentRecycled() =>
        Run(BenchmarkMode.ConcurrentRecycled);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint HoleScan() => Run(BenchmarkMode.HoleScan);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint LongLivedCache() => Run(BenchmarkMode.LongLivedCache);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ExtraScanSensitivity() => Run(BenchmarkMode.ExtraScan);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint InjectedOwnerDelay() => Run(BenchmarkMode.OwnerDelay);

    private nuint Run(BenchmarkMode mode)
    {
        nuint result = _runBatch(
            (uint)mode,
            Operations,
            (uint)WorkerCount);
        if (result == nuint.MaxValue)
        {
            throw new InvalidOperationException(
                $"Native benchmark failed in mode {mode}.");
        }

        return result;
    }

    private T Load<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(
            NativeLibrary.GetExport(_library, name));

    private enum BenchmarkMode : uint
    {
        Fresh,
        Recycled,
        ConcurrentRecycled,
        HoleScan,
        LongLivedCache,
        ExtraScan,
        OwnerDelay,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint RunBatchDelegate(
        uint mode,
        nuint operationCount,
        uint workerCount);
}

public class DefaultAllocationBenchmarks
{
    [Benchmark]
    public byte[] Allocate() => new byte[64];
}
