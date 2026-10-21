// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P15;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(ReferenceEnumerationBenchmarks).Assembly).Run(args);
}

[MemoryDiagnoser]
[InProcess]
public class ReferenceEnumerationBenchmarks
{
    private nint _library;
    private InitializeDelegate _initialize = null!;
    private ScanDelegate _scanCallbacks = null!;
    private ScanDelegate _scanRanges = null!;
    private ScanDelegate _scanRangeVisitor = null!;

    [Params(0, 1, 2)]
    public int Scenario { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P15_REFERENCE_BENCHMARK_LIBRARY")
            ?? throw new InvalidOperationException(
                "P15_REFERENCE_BENCHMARK_LIBRARY is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = GetDelegate<InitializeDelegate>("P15_Initialize");
        _scanCallbacks = GetDelegate<ScanDelegate>("P15_ScanCallbacks");
        _scanRanges = GetDelegate<ScanDelegate>("P15_ScanRanges");
        _scanRangeVisitor = GetDelegate<ScanDelegate>("P15_ScanRangeVisitor");

        int objectCount = _initialize(Scenario);
        if (objectCount != 2048)
        {
            throw new InvalidOperationException(
                $"Native benchmark initialized {objectCount} objects.");
        }

        nuint callbackChecksum = _scanCallbacks();
        nuint rangeChecksum = _scanRanges();
        nuint rangeVisitorChecksum = _scanRangeVisitor();
        if ((callbackChecksum != rangeChecksum) ||
            (callbackChecksum != rangeVisitorChecksum))
        {
            throw new InvalidOperationException(
                $"Scan checksums differ: {callbackChecksum}, {rangeChecksum}, " +
                $"{rangeVisitorChecksum}.");
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        NativeLibrary.Free(_library);
        _library = 0;
    }

    [Benchmark(Baseline = true)]
    public nuint PerSlotCallback() => _scanCallbacks();

    [Benchmark]
    public nuint ReferenceRanges() => _scanRanges();

    [Benchmark]
    public nuint ReferenceRangeVisitor() => _scanRangeVisitor();

    private T GetDelegate<T>(string name)
        where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(
            NativeLibrary.GetExport(_library, name));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate(int scenario);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint ScanDelegate();
}
