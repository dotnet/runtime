// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P16;

public static class Program
{
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(LockBenchmarks).Assembly).Run(args);
}

public class LockBenchmarks
{
    private readonly object _gate = new();

    [Benchmark]
    public void Uncontended()
    {
        lock (_gate)
        {
        }
    }

    [Benchmark]
    public void Recursive()
    {
        lock (_gate)
        {
            lock (_gate)
            {
            }
        }
    }
}

public class ContendedLockBenchmarks
{
    private readonly object _gate = new();
    private readonly AutoResetEvent _request = new(false);
    private readonly AutoResetEvent _acquired = new(false);
    private readonly AutoResetEvent _done = new(false);
    private Thread? _worker;
    private volatile bool _stopping;

    [GlobalSetup]
    public void Setup()
    {
        _worker = new Thread(
            () =>
            {
                while (true)
                {
                    _request.WaitOne();
                    if (_stopping)
                    {
                        return;
                    }

                    lock (_gate)
                    {
                        _acquired.Set();
                        Thread.SpinWait(2_000);
                    }
                    _done.Set();
                }
            })
        {
            IsBackground = true,
        };
        _worker.Start();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _stopping = true;
        _request.Set();
        _worker!.Join();
    }

    [Benchmark]
    public void Contended()
    {
        _request.Set();
        _acquired.WaitOne();
        lock (_gate)
        {
        }
        _done.WaitOne();
    }
}

public class HashBenchmarks
{
    private readonly object _hashed = new();

    [GlobalSetup]
    public void Setup() => RuntimeHelpers.GetHashCode(_hashed);

    [Benchmark]
    public int First() => RuntimeHelpers.GetHashCode(new object());

    [Benchmark]
    public int Cached() => RuntimeHelpers.GetHashCode(_hashed);

    [Benchmark]
    public int LockThenHash()
    {
        object value = new();
        lock (value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }

    [Benchmark]
    public int HashThenLock()
    {
        object value = new();
        int hash = RuntimeHelpers.GetHashCode(value);
        lock (value)
        {
            return hash;
        }
    }
}

public class StateLockHashBenchmarks
{
    private object _gate = null!;
    private GCHandle _handle;
    private nint _library;
    private LoadDelegate _load = null!;
    private CompareExchangeDelegate _compareExchange = null!;
    private SetDelegate _set = null!;

    [Params(0u, 2u, 3u)]
    public uint State { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P16_NATIVE_HOOK_LIBRARY")
            ?? throw new InvalidOperationException("P16_NATIVE_HOOK_LIBRARY is required.");
        _library = NativeLibrary.Load(libraryPath);
        _load = Marshal.GetDelegateForFunctionPointer<LoadDelegate>(
            NativeLibrary.GetExport(_library, "GC_ObjectHeaderBitsTest_Load"));
        _compareExchange = Marshal.GetDelegateForFunctionPointer<CompareExchangeDelegate>(
            NativeLibrary.GetExport(_library, "GC_ObjectHeaderBitsTest_CompareExchange"));
        _set = Marshal.GetDelegateForFunctionPointer<SetDelegate>(
            NativeLibrary.GetExport(_library, "GC_ObjectHeaderBitsTest_Set"));
        _gate = new object();
        _handle = GCHandle.Alloc(_gate);
        _set(GCHandle.ToIntPtr(_handle), State);
        RuntimeHelpers.GetHashCode(_gate);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _set(GCHandle.ToIntPtr(_handle), 0);
        _handle.Free();
        NativeLibrary.Free(_library);
    }

    [Benchmark]
    public void UncontendedLock()
    {
        lock (_gate)
        {
        }
    }

    [Benchmark]
    public int CachedHash() => RuntimeHelpers.GetHashCode(_gate);

    [Benchmark]
    public uint StateLoad() => _load(GCHandle.ToIntPtr(_handle));

    [Benchmark]
    public uint StateRoundTrip()
    {
        uint alternate = State == 2 ? 3u : 2u;
        uint observed = _compareExchange(GCHandle.ToIntPtr(_handle), alternate, State);
        _compareExchange(GCHandle.ToIntPtr(_handle), State, alternate);
        return observed;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint LoadDelegate(nint handle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint CompareExchangeDelegate(nint handle, uint value, uint comparand);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate uint SetDelegate(nint handle, uint value);
}
