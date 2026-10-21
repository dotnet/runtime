// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Runtime.InteropServices;

namespace P14;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(VariableAllocationBenchmarks).Assembly).Run(args);
}

[MemoryDiagnoser]
public class VariableAllocationBenchmarks
{
    [Params(0, 16, 64, 256, 1024, 8192, 85000)]
    public int Length { get; set; }

    [GlobalSetup]
    public void VerifyCountOnlyCallback() => AllocationNotificationCounter.VerifyCountOnlyCallback();

    [Benchmark]
    public byte[] ByteArray() => new byte[Length];

    [Benchmark]
    public object[] ObjectArray() => new object[Length];

    [Benchmark]
    public string String() => new('x', Length);
}

[MemoryDiagnoser]
public class FixedAllocationBenchmarks
{
    private int _value;

    [GlobalSetup]
    public void VerifyCountOnlyCallback() => AllocationNotificationCounter.VerifyCountOnlyCallback();

    [Benchmark]
    public object Object() => new object();

    [Benchmark]
    public object Box() => _value++;

    [Benchmark]
    public FinalizableObject Finalizable() => new();

    public sealed class FinalizableObject
    {
        public static int Finalized;

        ~FinalizableObject()
        {
            Interlocked.Increment(ref Finalized);
        }
    }
}

internal static class AllocationNotificationCounter
{
    private const int ProbeObjectCount = 4096;
    private static object[]? s_probeRoots;

    public static void VerifyCountOnlyCallback()
    {
        if (Environment.GetEnvironmentVariable("P14_EXPECT_COUNT_ONLY") != "1")
        {
            return;
        }

        string libraryPath =
            Environment.GetEnvironmentVariable("P14_NATIVE_HOOK_LIBRARY")
            ?? throw new InvalidOperationException("P14_NATIVE_HOOK_LIBRARY is required.");
        nint library = NativeLibrary.Load(libraryPath);
        try
        {
            ResetDelegate reset = GetDelegate<ResetDelegate>(
                library,
                "GC_AllocationNotificationTest_Reset");
            GetCountDelegate getCountOnly = GetDelegate<GetCountDelegate>(
                library,
                "GC_AllocationNotificationTest_GetCountOnly");

            reset(0, 0);
            object[] roots = new object[ProbeObjectCount];
            for (int index = 0; index < roots.Length; index++)
            {
                roots[index] = new object();
            }
            s_probeRoots = roots;

            long count = getCountOnly();
            if (count < ProbeObjectCount)
            {
                throw new InvalidOperationException(
                    $"Count-only allocation notification callback delivered {count}; " +
                    $"expected at least {ProbeObjectCount}.");
            }

            Console.WriteLine($"P14_COUNT_ONLY_DELTA={count};MINIMUM={ProbeObjectCount}");
        }
        finally
        {
            s_probeRoots = null;
            NativeLibrary.Free(library);
        }
    }

    private static T GetDelegate<T>(nint library, string name)
        where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ResetDelegate(nint methodTableFilter, nuint alignedSizeFilter);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate long GetCountDelegate();
}
