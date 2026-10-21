// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P22;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(ImmixBlockBenchmarks).Assembly).Run(args);
}

public class ImmixBlockBenchmarks
{
    private const int Operations = 4096;
    private nint _library;
    private InitializeDelegate _initialize = null!;
    private ShutdownDelegate _shutdown = null!;
    private RunBatchDelegate _runBatch = null!;
    private EpochBatchDelegate _epochBatch = null!;

    [Params(1, 4, 16)]
    public int WorkerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P22_NATIVE_BENCHMARK")
            ?? throw new InvalidOperationException("P22_NATIVE_BENCHMARK is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = Load<InitializeDelegate>("P22_Initialize");
        _shutdown = Load<ShutdownDelegate>("P22_Shutdown");
        _runBatch = Load<RunBatchDelegate>("P22_RunBatch");
        _epochBatch = Load<EpochBatchDelegate>("P22_EpochBatch");
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
    public nuint MetadataMap() => Run(BenchmarkMode.MetadataMap);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint StateCas() => Run(BenchmarkMode.StateCas);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint PoolOnly() => Run(BenchmarkMode.PoolOnly);

    [Benchmark(OperationsPerInvoke = Operations, Baseline = true)]
    public nuint FreshAcquireRelease() => Run(BenchmarkMode.Fresh);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint FreshAcquireReleaseControl() => Run(BenchmarkMode.Fresh);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ReuseAcquireReturn() => Run(BenchmarkMode.Reuse);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint GcCopyAcquireReturn() => Run(BenchmarkMode.Copy);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ContendedAcquireRelease() => Run(BenchmarkMode.Contended);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ExtraCasSensitivity() => Run(BenchmarkMode.ExtraCas);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint InjectedOwnerDelay() => Run(BenchmarkMode.OwnerDelay);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint EpochChanges()
    {
        return EnsureSuccess(_epochBatch(Operations));
    }

    private nuint Run(BenchmarkMode mode)
    {
        return EnsureSuccess(_runBatch((uint)mode, Operations, (uint)WorkerCount));
    }

    private static nuint EnsureSuccess(nuint result)
    {
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
        MetadataMap,
        StateCas,
        PoolOnly,
        Fresh,
        Reuse,
        Copy,
        Contended,
        ExtraCas,
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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint EpochBatchDelegate(nuint operationCount);
}
