// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

public class Runtime_133415
{
    private const MethodImplOptions TestOptions = MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization;

    [ConditionalFact(typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.IsMultithreadingSupported))]
    public static void TestEntryPoint()
    {
        Run(CopyLoop, 1554, 6);
        Run(InitLoop, 168, 6);
        Run(PartialWriteLoop, 489, 6);
        Run(p => ExceptionBeforeOverwrite(p, true), 75, 1);
        Run(p => ExceptionBeforeOverwrite(p, false), 225, 1);
        Run(ZeroLoop, 0, 6);
        Run(p => OverlappingRemainder(p, 6), 567, 6);
        Run(p => EmptyInit(p, 6), 1512, 6);
        Run(p => DefaultOverlap(p, 6), 0, 6);
    }

    // Dead-capture elimination is an optimization. Keep the semantic cases above
    // enabled under stress and MinOpts, but check object lifetime only with the
    // optimizing x64 JIT, where this copy is physically promoted.
    public static bool IsOptimizingX64Jit =>
        TestLibrary.PlatformDetection.IsMultithreadingSupported &&
        TestLibrary.PlatformDetection.IsCoreCLR &&
        RuntimeInformation.ProcessArchitecture == Architecture.X64 &&
        RuntimeFeature.IsDynamicCodeCompiled &&
        !TestLibrary.CoreClrConfigurationDetection.IsCoreClrInterpreter &&
        !TestLibrary.CoreClrConfigurationDetection.IsAnyJitStress &&
        !TestLibrary.CoreClrConfigurationDetection.IsDebugRuntime;

    [ConditionalFact(typeof(Runtime_133415), nameof(IsOptimizingX64Jit))]
    public static void DeadStructIsNotCaptured()
    {
        Run(CopyLivenessLoop, 1554, 6, checkDeadCapture: true);
    }

    private static void Run(Func<Pump, Task<long>> body, long expected, int suspensions, bool checkDeadCapture = false)
    {
        Pump pump = new Pump();
        Task<long> task = body(pump);
        Assert.False(task.IsCompleted);
        int resumed = 0;
        while (!task.IsCompleted)
        {
            Action continuation = null;
            Assert.True(SpinWait.SpinUntil(() => task.IsCompleted || pump.TryTake(out continuation), TimeSpan.FromSeconds(30)));
            if (continuation is null)
                break;
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            if (checkDeadCapture && pump.PreviousSnapshot is not null)
            {
                Assert.False(pump.PreviousSnapshot.IsAlive);
            }
            resumed++;
            continuation();
        }
        Assert.Equal(expected, task.GetAwaiter().GetResult());
        Assert.Equal(suspensions, resumed);
    }

    // The explicit non-GC overlap prevents regular struct promotion. Physical
    // promotion can still replace A and B, leaving the GC reference in the parent.
    [StructLayout(LayoutKind.Explicit)]
    private struct Value
    {
        [FieldOffset(0)] public Box Reference;
        [FieldOffset(8)] public long A;
        [FieldOffset(16)] public long B;
        [FieldOffset(12)] public int Overlapping;
    }

    private sealed class Box
    {
        public int Id;
        public Box(int id) => Id = id;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Value Create(int seed) => new Value { Reference = new Box(17 * seed), A = 3 * seed, B = 5 * seed };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Consume(Value value) => (value.Reference?.Id ?? 0) + value.A + value.B;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ConsumeByRef(ref Value value) => Consume(value);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Observe(long a, long b) => a + b;

    [MethodImpl(TestOptions)]
    private static async Task<long> CopyLoop(Pump pump)
    {
        long sum = 0;
        for (int i = 1; i <= 6; i++)
        {
            await pump.Gate;
            Value source = Create(i);
            source.A += source.B;
            sum += Observe(source.A, source.B);
            // This whole definition kills the previous iteration's snapshot,
            // even if promotion decomposes the copy into individual field stores.
            Value snapshot = source;
            source.B += i;
            sum += ConsumeByRef(ref snapshot) + Consume(source);
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void TrackSnapshot(Pump pump, Box box)
    {
        pump.PreviousSnapshot = new WeakReference(box);
    }

    [MethodImpl(TestOptions)]
    private static async Task<long> CopyLivenessLoop(Pump pump)
    {
        long sum = 0;
        for (int i = 1; i <= 6; i++)
        {
            await pump.Gate;
            Value source = Create(i);
            source.A += source.B;
            sum += Observe(source.A, source.B);
            Value snapshot = source;
            source.B += i;
            sum += ConsumeByRef(ref snapshot) + Consume(source);
            TrackSnapshot(pump, snapshot.Reference);
            // At the next suspension, neither source nor snapshot is needed:
            // both are overwritten before their next use. Capturing the old
            // parent struct would keep this Box alive unnecessarily.
        }
        return sum;
    }

    [SkipLocalsInit]
    [MethodImpl(TestOptions)]
    private static async Task<long> InitLoop(Pump pump)
    {
        long sum = 0;
        for (int i = 1; i <= 6; i++)
        {
            Value value = Create(i);
            Observe(value.A, value.B);
            value = default;
            value.A = 3 * i;
            value.B = 5 * i;
            await pump.Gate;
            Assert.Null(value.Reference);
            sum += Consume(value);
        }
        return sum;
    }

    [MethodImpl(TestOptions)]
    private static async Task<long> PartialWriteLoop(Pump pump)
    {
        Value value = Create(3);
        long sum = 0;
        for (int i = 1; i <= 6; i++)
        {
            await pump.Gate;
            // This partial definition must not kill Reference or B.
            value.A = 12 + i;
            sum += Consume(value);
        }
        return sum;
    }

    private sealed class ExpectedException : Exception
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Value CreateOrThrow(bool shouldThrow)
    {
        if (shouldThrow)
            throw new ExpectedException();
        return Create(9);
    }

    [MethodImpl(TestOptions)]
    private static async Task<long> ExceptionBeforeOverwrite(Pump pump, bool shouldThrow)
    {
        Value value = Create(3);
        try
        {
            await pump.Gate;
            // The right-hand side must finish before the old value is overwritten.
            value = CreateOrThrow(shouldThrow);
        }
        catch (ExpectedException)
        {
            return Consume(value);
        }
        return Consume(value);
    }

    [MethodImpl(TestOptions)]
    private static async Task<long> ZeroLoop(Pump pump)
    {
        Value value = default;
        long sum = 0;
        for (int i = 0; i < 6; i++)
        {
            sum += Observe(value.A, value.B) + Consume(value);
            await pump.Gate;
            sum += Observe(value.A, value.B) + Consume(value);
        }
        return sum;
    }

    [MethodImpl(TestOptions)]
    private static async Task<long> OverlappingRemainder(Pump pump, int count)
    {
        long sum = 0;
        for (int i = 1; i <= count; i++)
        {
            await pump.Gate;
            Quad source = CreateQuad(i);
            source.Hot += i;
            sum += Observe(source.Hot);
            Quad snapshot = source;
            source.Hot += 7;
            sum += ConsumeQuadByRef(ref snapshot) + ConsumeQuad(source);
        }
        return sum;
    }

    // Eight primitive fields exceed ordinary promotion's field count. Their
    // direct accesses encourage complete physical promotion. The initial whole
    // default may then produce an empty parent FIELD_LIST, with later writebacks.
    [MethodImpl(TestOptions)]
    private static async Task<long> EmptyInit(Pump pump, int count)
    {
        long sum = 0;
        for (int i = 1; i <= count; i++)
        {
            await pump.Gate;
            Eight value = default;
            value.F0 = i;
            value.F1 = 2 * i;
            value.F2 = 3 * i;
            value.F3 = 4 * i;
            value.F4 = 5 * i;
            value.F5 = 6 * i;
            value.F6 = 7 * i;
            value.F7 = 8 * i;
            sum += ObservePair(value.F0, value.F1) + ObservePair(value.F2, value.F3) +
                   ObservePair(value.F4, value.F5) + ObservePair(value.F6, value.F7);
            sum += ConsumeEight(value);
        }
        return sum;
    }

    // Never nonzero and never address-exposed. If Hot promotes, defaulting the
    // parent remainder uses an overlapping zero SIMD16 value, exercising the
    // recursive all-zero FIELD_LIST recognition in async default analysis.
    [MethodImpl(TestOptions)]
    private static async Task<long> DefaultOverlap(Pump pump, int count)
    {
        Quad value = default;
        long sum = 0;
        for (int i = 0; i < count; i++)
        {
            sum += Observe(value.Hot) + Observe(value.Hot);
            sum += ConsumeQuad(value);
            await pump.Gate;
            sum += Observe(value.Hot) + Observe(value.Hot);
            sum += ConsumeQuad(value);
        }
        return sum;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct Quad
    {
        [FieldOffset(0)] public int A;
        [FieldOffset(4)] public int Hot;
        [FieldOffset(8)] public int C;
        [FieldOffset(12)] public int D;
        [FieldOffset(2)] public int Overlap;
    }

    private struct Eight
    {
        public int F0, F1, F2, F3, F4, F5, F6, F7;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Quad CreateQuad(int i) => new Quad { A = i, Hot = 2 * i, C = 3 * i, D = 4 * i };
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Observe(int value) => value;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ObservePair(int a, int b) => a + b;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeQuad(Quad value) => value.A + value.Hot + value.C + value.D;
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeQuadByRef(ref Quad value) => ConsumeQuad(value);
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ConsumeEight(Eight value) => value.F0 + value.F1 + value.F2 + value.F3 + value.F4 + value.F5 + value.F6 + value.F7;

    private sealed class Pump
    {
        private readonly Queue<Action> _pending = new Queue<Action>();
        internal WeakReference PreviousSnapshot;
        internal GateAwaiter Gate => new GateAwaiter(this);

        internal void Enqueue(Action continuation)
        {
            lock (_pending)
                _pending.Enqueue(continuation);
        }

        internal bool TryTake(out Action continuation)
        {
            lock (_pending)
            {
                if (_pending.Count == 0)
                {
                    continuation = null;
                    return false;
                }
                continuation = _pending.Dequeue();
                return true;
            }
        }
    }

    private readonly struct GateAwaiter : INotifyCompletion
    {
        private readonly Pump _pump;
        internal GateAwaiter(Pump pump) => _pump = pump;
        public GateAwaiter GetAwaiter() => this;
        public bool IsCompleted => false;
        public void OnCompleted(Action continuation) => _pump.Enqueue(continuation);
        public void GetResult()
        {
        }
    }
}
