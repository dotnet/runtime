// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This project benchmarks the native ReferenceCountTable API exclusively
// through reference-count-benchmark-native.cpp / reference_count.h. It does
// not allocate real CoreCLR objects, does not run the CoreCLR GC, and does
// not exercise LXR collection or block reclamation: every "object start" is
// a synthetic, explicitly registered address inside a private native heap
// simulation.

using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace P23;

public static class Program
{
    // Callers MUST pass "--invocationCount 1 --unrollFactor 1" on the command
    // line (see run-reference-count-benchmark.ps1). This pins every iteration
    // to one measured native batch, so every [IterationSetup] method runs
    // immediately before its corresponding invocation. Several benchmarks
    // (increment-below-sticky, decrement-above-zero, the sticky
    // transition/attempt cases) rely on an untimed native preseed call that
    // must precede each timed batch exactly once; a larger unroll factor
    // would let BenchmarkDotNet amortize IterationSetup across multiple
    // invocations and corrupt that precondition. This is deliberately left
    // as a command-line option (rather than baked into an in-code Job) so
    // that the quick "--job Dry" validation mode can still override the
    // rest of the job's characteristics without producing a second,
    // duplicate job.
    public static void Main(string[] args) =>
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
}

/// <summary>
/// Width-dependent (log2 active reference-count bits: 1/2/3 -> 2/4/8-bit
/// widths) single-threaded benchmarks. No threads are created by these
/// benchmarks; the persistent native worker pool used by
/// <see cref="ReferenceCountContentionBenchmarks"/> is started once during
/// native initialization but is never touched here.
/// </summary>
public class ReferenceCountWidthBenchmarks
{
    private const int Operations = 4096;
    private const int StraddleOperations = 512;
    private const int ResetPoolOperations = 512;
    private const int ResetBlockOperations = 64;
    private const int ChurnOperations = 4000;

    private nint _library;
    private InitializeDelegate _initialize = null!;
    private ShutdownDelegate _shutdown = null!;
    private GetMaximumCountDelegate _getMaximumCount = null!;
    private PreseedDelegate _preseedWidthPool = null!;
    private BatchDelegate _load = null!;
    private BatchDelegate _increment = null!;
    private BatchDelegate _decrement = null!;
    private BatchDelegate _stickyIncrement = null!;
    private BatchDelegate _stickyDecrement = null!;
    private BatchDelegate _straddleZero = null!;
    private BatchDelegate _straddleOne = null!;
    private BatchDelegate _straddleMany = null!;
    private BatchDelegate _resetQuiescent4KiB = null!;
    private BatchDelegate _resetQuiescent64KiB = null!;
    private BatchDelegate _resetQuiescentBlock = null!;
    private BatchDelegate _allocationChurn = null!;
    private nuint _maximum;

    [Params(1, 2, 3)]
    public byte LogReferenceCountBits { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P23_NATIVE_BENCHMARK")
            ?? throw new InvalidOperationException("P23_NATIVE_BENCHMARK is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = Load<InitializeDelegate>("P23_Initialize");
        _shutdown = Load<ShutdownDelegate>("P23_Shutdown");
        _getMaximumCount = Load<GetMaximumCountDelegate>("P23_GetMaximumCount");
        _preseedWidthPool = Load<PreseedDelegate>("P23_PreseedWidthPoolBatch");
        _load = Load<BatchDelegate>("P23_LoadBatch");
        _increment = Load<BatchDelegate>("P23_IncrementBatch");
        _decrement = Load<BatchDelegate>("P23_DecrementBatch");
        _stickyIncrement = Load<BatchDelegate>("P23_StickyIncrementBatch");
        _stickyDecrement = Load<BatchDelegate>("P23_StickyDecrementBatch");
        _straddleZero = Load<BatchDelegate>("P23_StraddleZeroBatch");
        _straddleOne = Load<BatchDelegate>("P23_StraddleOneBatch");
        _straddleMany = Load<BatchDelegate>("P23_StraddleManyBatch");
        _resetQuiescent4KiB = Load<BatchDelegate>("P23_QuiescentReset4KiBBatch");
        _resetQuiescent64KiB = Load<BatchDelegate>("P23_QuiescentReset64KiBBatch");
        _resetQuiescentBlock = Load<BatchDelegate>("P23_QuiescentResetBlockBatch");
        _allocationChurn = Load<BatchDelegate>("P23_AllocationChurnBatch");

        int result = _initialize(LogReferenceCountBits);
        if (result != 0)
        {
            throw new InvalidOperationException($"Native initialization failed: {result}.");
        }
        _maximum = EnsureSuccess(_getMaximumCount());
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _shutdown();
        NativeLibrary.Free(_library);
    }

    [Benchmark(OperationsPerInvoke = Operations, Baseline = true)]
    public nuint Load() => EnsureSuccess(_load((nuint)Operations));

    [IterationSetup(Target = nameof(IncrementBelowSticky))]
    public void SetupIncrementBelowSticky() => Preseed(0);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint IncrementBelowSticky() => EnsureSuccess(_increment((nuint)Operations));

    [IterationSetup(Target = nameof(DecrementAboveZero))]
    public void SetupDecrementAboveZero() => Preseed(2);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint DecrementAboveZero() => EnsureSuccess(_decrement((nuint)Operations));

    [IterationSetup(Target = nameof(StickyTransition))]
    public void SetupStickyTransition() => Preseed(_maximum - 1);

    // Every slot starts at max-1: the single timed increment per slot is the
    // documented max-1 -> max sticky transition. The untimed preseed (which
    // itself performs a reset + re-initialize + compare-exchange per slot) is
    // the "paired batch" required to reach that precondition without timing
    // it. The native batch also rejects any non-Updated status.
    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint StickyTransition() => EnsureSuccess(_increment((nuint)Operations));

    [IterationSetup(Target = nameof(StickyIncrementAttempt))]
    public void SetupStickyIncrementAttempt() => Preseed(_maximum);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint StickyIncrementAttempt() => EnsureSuccess(_stickyIncrement((nuint)Operations));

    [IterationSetup(Target = nameof(StickyDecrementAttempt))]
    public void SetupStickyDecrementAttempt() => Preseed(_maximum);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint StickyDecrementAttempt() => EnsureSuccess(_stickyDecrement((nuint)Operations));

    [Benchmark(OperationsPerInvoke = StraddleOperations)]
    public nuint StraddleZeroContinuations() => EnsureSuccess(_straddleZero((nuint)StraddleOperations));

    [Benchmark(OperationsPerInvoke = StraddleOperations)]
    public nuint StraddleOneContinuation() => EnsureSuccess(_straddleOne((nuint)StraddleOperations));

    [Benchmark(OperationsPerInvoke = StraddleOperations)]
    public nuint StraddleManyContinuations() => EnsureSuccess(_straddleMany((nuint)StraddleOperations));

    [Benchmark(OperationsPerInvoke = ResetPoolOperations)]
    public nuint QuiescentReset4KiB() => EnsureSuccess(_resetQuiescent4KiB((nuint)ResetPoolOperations));

    [Benchmark(OperationsPerInvoke = ResetPoolOperations)]
    public nuint QuiescentReset64KiB() => EnsureSuccess(_resetQuiescent64KiB((nuint)ResetPoolOperations));

    [Benchmark(OperationsPerInvoke = ResetBlockOperations)]
    public nuint QuiescentResetBlock32KiB() => EnsureSuccess(_resetQuiescentBlock((nuint)ResetBlockOperations));

    // Returns the exact number of Increment calls issued (always 80% of
    // ChurnOperations), not a mixed checksum, so the count can be verified
    // precisely by callers.
    [Benchmark(OperationsPerInvoke = ChurnOperations)]
    public nuint AllocationChurn() => EnsureSuccess(_allocationChurn((nuint)ChurnOperations));

    private void Preseed(nuint value) => EnsureSuccess(_preseedWidthPool(value));

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

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate(byte logReferenceCountBits);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint GetMaximumCountDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint PreseedDelegate(nuint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint BatchDelegate(nuint operationCount);
}

/// <summary>
/// Concurrency benchmarks that dispatch to a persistent native worker pool
/// (created once per native initialization, capped at 16 workers, and never
/// recreated per measured invocation). The reference-count width is fixed at
/// its widest (8-bit) setting so that saturation of the shared/adjacent
/// slots under high worker counts does not short-circuit the compare-exchange
/// path being measured.
/// </summary>
public class ReferenceCountContentionBenchmarks
{
    private const int Operations = 1_048_576;
    private const byte FixedLogReferenceCountBits = 3;

    private nint _library;
    private InitializeDelegate _initialize = null!;
    private ShutdownDelegate _shutdown = null!;
    private RunContentionBatchDelegate _runBatch = null!;

    [Params(1, 4, 16)]
    public int WorkerCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        string libraryPath =
            Environment.GetEnvironmentVariable("P23_NATIVE_BENCHMARK")
            ?? throw new InvalidOperationException("P23_NATIVE_BENCHMARK is required.");
        _library = NativeLibrary.Load(libraryPath);
        _initialize = Load<InitializeDelegate>("P23_Initialize");
        _shutdown = Load<ShutdownDelegate>("P23_Shutdown");
        _runBatch = Load<RunContentionBatchDelegate>("P23_RunContentionBatch");
        int result = _initialize(FixedLogReferenceCountBits);
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
    public nuint SameSlotContention() => Run(ContentionMode.SameSlot);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint SameSlotContentionControl() => Run(ContentionMode.SameSlot);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint AdjacentWordContention() => Run(ContentionMode.AdjacentWord);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint SeparateLineContention() => Run(ContentionMode.SeparateLine);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint ExtraCasContention() => Run(ContentionMode.ExtraCas);

    [Benchmark(OperationsPerInvoke = Operations)]
    public nuint DelayContention() => Run(ContentionMode.Delay);

    private nuint Run(ContentionMode mode)
    {
        return EnsureSuccess(_runBatch((uint)mode, (nuint)Operations, (uint)WorkerCount));
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

    private enum ContentionMode : uint
    {
        SameSlot,
        AdjacentWord,
        SeparateLine,
        ExtraCas,
        Delay,
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int InitializeDelegate(byte logReferenceCountBits);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShutdownDelegate();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate nuint RunContentionBatchDelegate(
        uint mode,
        nuint operationCount,
        uint workerCount);
}
