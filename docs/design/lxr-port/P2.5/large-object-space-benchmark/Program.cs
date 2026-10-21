// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P25;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(LargeObjectSpaceBenchmarks).Assembly).Run(args);
}

public class LargeObjectSpaceBenchmarks
{
    private const int Operations = 64;
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
            Environment.GetEnvironmentVariable("P25_NATIVE_BENCHMARK")
            ?? throw new InvalidOperationException("P25_NATIVE_BENCHMARK is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = Load<InitializeDelegate>("P25_Initialize");
        _shutdown = Load<ShutdownDelegate>("P25_Shutdown");
        _runBatch = Load<RunBatchDelegate>("P25_RunBatch");
        int result = _initialize();
        if (result != 0)
        {
            throw new InvalidOperationException($"Native initialization failed: {result}.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _shutdown();
        NativeLibrary.Free(_library);
    }

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint Geometry() => Run(BenchmarkMode.Geometry);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint GeometryControl() => Run(BenchmarkMode.Geometry);

    [Benchmark(OperationsPerInvoke = Operations, Baseline = true)]
    public nuint FreshAllocateRelease() => Run(BenchmarkMode.Fresh);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint RecycledAllocateRelease() => Run(BenchmarkMode.Recycled);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ReuseCounterAllocation() => Run(BenchmarkMode.ReuseCounter);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ExtraReuseUpdates() => Run(BenchmarkMode.ExtraReuseUpdates);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint InjectedOwnerDelay() => Run(BenchmarkMode.InjectedOwnerDelay);

    private nuint Run(BenchmarkMode mode)
    {
        nuint result = _runBatch((uint)mode, Operations, (uint)WorkerCount);
        if (result == nuint.MaxValue)
        {
            throw new InvalidOperationException("Native benchmark operation failed.");
        }
        return result;
    }

    private T Load<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));

    private enum BenchmarkMode : uint
    {
        Geometry,
        Fresh,
        Recycled,
        ReuseCounter,
        ExtraReuseUpdates,
        InjectedOwnerDelay,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint RunBatchDelegate(uint mode, nuint operations, uint workerCount);
}
