// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P21;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(SideMetadataBenchmarks).Assembly).Run(args);
}

public class SideMetadataBenchmarks
{
    private const int Operations = 4096;
    private nint _library;
    private InitializeDelegate _initialize = null!;
    private ShutdownDelegate _shutdown = null!;
    private BatchDelegate _mapLoad = null!;
    private BatchDelegate _precomputedLoad = null!;
    private BatchDelegate _bitStore = null!;
    private BatchDelegate _referenceCount = null!;
    private BatchDelegate _byteUpdate = null!;
    private BatchDelegate _bulkRead = null!;
    private BatchDelegate _sparseBulkRead = null!;
    private BatchDelegate _reset4KiB = null!;
    private BatchDelegate _reset64KiB = null!;
    private BatchDelegate _reset1MiB = null!;
    private BatchDelegate _extraCas = null!;
    private FirstCommitDelegate _firstCommit = null!;
    private BatchDelegate _falseSharingSameWord = null!;
    private BatchDelegate _falseSharingSeparateLine = null!;

    [Params(1, 2, 3)]
    public byte LogReferenceCountBits { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P21_NATIVE_BENCHMARK")
            ?? throw new InvalidOperationException("P21_NATIVE_BENCHMARK is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = Load<InitializeDelegate>("P21_Initialize");
        _shutdown = Load<ShutdownDelegate>("P21_Shutdown");
        _mapLoad = Load<BatchDelegate>("P21_MapLoadBatch");
        _precomputedLoad = Load<BatchDelegate>("P21_PrecomputedLoadBatch");
        _bitStore = Load<BatchDelegate>("P21_BitStoreBatch");
        _referenceCount = Load<BatchDelegate>("P21_ReferenceCountBatch");
        _byteUpdate = Load<BatchDelegate>("P21_ByteUpdateBatch");
        _bulkRead = Load<BatchDelegate>("P21_BulkReadBatch");
        _sparseBulkRead = Load<BatchDelegate>("P21_SparseBulkReadBatch");
        _reset4KiB = Load<BatchDelegate>("P21_Reset4KiBBatch");
        _reset64KiB = Load<BatchDelegate>("P21_Reset64KiBBatch");
        _reset1MiB = Load<BatchDelegate>("P21_Reset1MiBBatch");
        _extraCas = Load<BatchDelegate>("P21_ExtraCasSensitivityBatch");
        _firstCommit = Load<FirstCommitDelegate>("P21_ReserveAndFirstCommit");
        BatchDelegate falseSharing = Load<BatchDelegate>("P21_FalseSharingBatch");
        _falseSharingSameWord = _ => falseSharing(8);
        _falseSharingSeparateLine = _ => falseSharing(4096);
        int result = _initialize(LogReferenceCountBits);
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

    [Benchmark(OperationsPerInvoke = Operations, Baseline = true)]
    public nuint MapAndLoad() => _mapLoad(Operations);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint MapAndLoadControl() => _mapLoad(Operations);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint PrecomputedLoad() => _precomputedLoad(Operations);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint BitStore() => _bitStore(Operations);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ReferenceCountUpdate() => _referenceCount(Operations);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ByteUpdate() => _byteUpdate(Operations);

    [Benchmark(OperationsPerInvoke = 8)]
    public nuint BulkRead64KiB() => _bulkRead(8);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint SparseBulkRead64B() => _sparseBulkRead(Operations);

    [Benchmark(OperationsPerInvoke = 32)]
    public nuint Reset4KiB() => _reset4KiB(32);

    [Benchmark(OperationsPerInvoke = 32)]
    public nuint Reset64KiB() => _reset64KiB(32);

    [Benchmark(OperationsPerInvoke = 8)]
    public nuint Reset1MiB() => _reset1MiB(8);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ExtraCasSensitivity() => _extraCas(Operations);

    [Benchmark]
    public nuint ReserveAndFirstCommit() => _firstCommit(LogReferenceCountBits);

    [Benchmark(OperationsPerInvoke = 20000)]
    public nuint FalseSharingSameWord() => _falseSharingSameWord(1);

    [Benchmark(OperationsPerInvoke = 20000)]
    public nuint FalseSharingSeparateLine() => _falseSharingSeparateLine(1);

    private T Load<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(_library, name));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate(byte logReferenceCountBits);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint BatchDelegate(nuint operationCount);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint FirstCommitDelegate(byte logReferenceCountBits);
}
