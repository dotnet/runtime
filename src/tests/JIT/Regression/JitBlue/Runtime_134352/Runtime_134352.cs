// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

// https://github.com/dotnet/runtime/issues/134352: loop hoisting of field loads that local assertion prop proved
// non-faulting.
//
// Local assertion prop runs during morph, so an invariant field load inside a loop body is commonly marked
// GTF_IND_NONFAULTING | GTF_ORDER_SIDEEFF because an earlier dereference of the same object proved it cannot
// fault. Hoisting such a load turns the copy in the preheader into an ordinary possibly-faulting load, which
// is only allowed where that cannot change behavior. These cases run each loop shape with a null object,
// where hoisting a load past a store, or away from an explicit null check, would be observable.
public class Runtime_134352
{
    private class Data
    {
        public double[] V;
        public int N;
        public double K;
    }

    // A field at an offset beyond the null page: a load from it cannot act as a null check (it would read
    // unmapped memory instead of the null page), so morph keeps an explicit null check in front of it and
    // the load must never be hoisted away from that check.
    [StructLayout(LayoutKind.Explicit)]
    private sealed class Big
    {
        [FieldOffset(0x20000)] public double X;
    }

    private static int s_sink;

    [Fact]
    public static void TestEntryPoint()
    {
        Data d = new Data { V = new double[] { 1, 2, 3, 4 }, N = 4, K = 0.5 };

        Assert.Equal(5.0, Sum(d));
        Assert.Throws<NullReferenceException>(() => Sum(null));

        int[] log = new int[3];
        Assert.Equal(3 * (4 + 1.0), SumLoadsFirst(d, log));
        Assert.Equal(new int[] { 1, 2, 3 }, log);

        log = new int[3];
        Assert.Throws<NullReferenceException>(() => SumLoadsFirst(null, log));
        Assert.Equal(new int[] { 0, 0, 0 }, log);

        log = new int[3];
        Assert.Equal(3 * (4 + 1.0), SumStoreFirst(d, log));
        Assert.Equal(new int[] { 1, 2, 3 }, log);

        // The store precedes the loads, so the first iteration's store must be visible even though the
        // loads fault: the loads may not be hoisted above it.
        log = new int[3];
        Assert.Throws<NullReferenceException>(() => SumStoreFirst(null, log));
        Assert.Equal(new int[] { 1, 0, 0 }, log);

        // With d == null the first iteration throws at d.N, before the store: nothing may have been stored.
        d.N = 4;
        log = new int[3];
        Assert.Equal(1.5, SumGuarded(d, log));
        Assert.Equal(new int[] { 1, 2, 3 }, log);
        Assert.Equal(2, d.N);

        log = new int[3];
        Assert.Throws<NullReferenceException>(() => SumGuarded(null, log));
        Assert.Equal(new int[] { 0, 0, 0 }, log);

        // The store precedes the first dereference of `d`, so it must be visible when d.N throws.
        d.N = 4;
        log = new int[3];
        Assert.Equal(1.5, SumNotGuarded(d, log));
        Assert.Equal(new int[] { 1, 2, 3 }, log);

        log = new int[3];
        Assert.Throws<NullReferenceException>(() => SumNotGuarded(null, log));
        Assert.Equal(new int[] { 1, 0, 0 }, log);

        // An invariant bounds check that is never hoisted precedes the dereference of `d`: hoisting `d.K`
        // would report NullReferenceException where the loop throws IndexOutOfRangeException first.
        d.N = 3;
        Assert.Equal(1666.0, SumMixedExceptions(d, new[] { 7 }, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        Assert.Throws<IndexOutOfRangeException>(() => SumMixedExceptions(null, new int[0], 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));

        // Nested loops: the copy hoisted by the inner loop lands in the outer loop's header, where an unhoisted
        // invariant bounds check precedes it; the outer loop must not hoist that copy above the bounds check.
        d.N = 3;
        Assert.Equal(2471.0, SumMixedExceptionsNested(d, new[] { 7 }, 2, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        Assert.Throws<IndexOutOfRangeException>(() => SumMixedExceptionsNested(null, new int[0], 2, 3, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));

        // Zero trips: the loop body never runs, so a null object must not be dereferenced by a hoisted copy either.
        Assert.Equal(0.0, Sum(new Data { V = new double[0], N = 0, K = 0.5 }));
        Assert.Equal(0.0, SumLoadsFirst(null, new int[0]));
        Assert.Equal(0.0, SumGuarded(null, new int[0]));

        // Large offset: must throw NullReferenceException, not crash with an access violation.
        Assert.Equal(1630.0, SumBig(new Big { X = 1.5 }, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        Assert.Throws<NullReferenceException>(() => SumBig(null, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
    }

    // `d.V` and `d.K` are invariant and the first thing the loop body evaluates: hoistable.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double Sum(Data d)
    {
        double sum = 0;
        for (int j = 0; j < d.N; j++)
        {
            sum += d.V[j] * d.K;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumLoadsFirst(Data d, int[] log)
    {
        double sum = 0;
        for (int j = 0; j < log.Length; j++)
        {
            sum += d.N;
            sum += d.V[0];
            log[j] = j + 1;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumStoreFirst(Data d, int[] log)
    {
        double sum = 0;
        for (int j = 0; j < log.Length; j++)
        {
            log[j] = j + 1;
            sum += d.N;
            sum += d.V[0];
        }
        return sum;
    }

    // d.K is proven non-faulting by the d.N load that precedes it, but follows a store to `log`.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumGuarded(Data d, int[] log)
    {
        double sum = 0;
        for (int j = 0; j < log.Length; j++)
        {
            s_sink = d.N;
            log[j] = j + 1;
            sum += d.K;
            d.N = j;
        }
        return sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumNotGuarded(Data d, int[] log)
    {
        double sum = 0;
        for (int j = 0; j < log.Length; j++)
        {
            log[j] = j + 1;
            s_sink = d.N;
            sum += d.K;
            d.N = j;
        }
        return sum;
    }

    // The 28 integer arguments use up the integer hoisting budget on every target (the largest is 28 registers,
    // x64 with APX), so `array[0]` (an invariant bounds check) and `d.N` stay in the loop while `d.K` alone
    // fits the floating-point budget. If `d.N` were hoisted, the loop would throw NullReferenceException before
    // `array[0]` regardless of this change. The loops with many arguments are do/while loops so that the body
    // is the loop header regardless of its size (a for loop this large is not inverted).
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumMixedExceptions(Data d, int[] array, int n,
        int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
        int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
    {
        int intSum = 0;
        double doubleSum = 0;
        int i = 0;
        do
        {
            intSum += array[0];
            intSum += d.N;
            doubleSum += d.K;
            intSum += (i ^ a0) + (i ^ a1) + (i ^ a2) + (i ^ a3) + (i ^ a4) + (i ^ a5) + (i ^ a6) + (i ^ a7)
                + (i ^ a8) + (i ^ a9) + (i ^ a10) + (i ^ a11) + (i ^ a12) + (i ^ a13) + (i ^ a14) + (i ^ a15)
                + (i ^ a16) + (i ^ a17) + (i ^ a18) + (i ^ a19) + (i ^ a20) + (i ^ a21) + (i ^ a22) + (i ^ a23)
                + (i ^ a24) + (i ^ a25) + (i ^ a26) + (i ^ a27);
            i++;
        }
        while (i < n);
        return doubleSum + intSum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumMixedExceptionsNested(Data d, int[] array, int outer, int inner,
        int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
        int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
    {
        int intSum = 0;
        double doubleSum = 0;
        int i = 0;
        do
        {
            intSum += array[0];
            int j = 0;
            do
            {
                intSum += d.N;
                doubleSum += d.K;
                intSum += (j ^ a0) + (j ^ a1) + (j ^ a2) + (j ^ a3) + (j ^ a4) + (j ^ a5) + (j ^ a6) + (j ^ a7)
                    + (j ^ a8) + (j ^ a9) + (j ^ a10) + (j ^ a11) + (j ^ a12) + (j ^ a13) + (j ^ a14) + (j ^ a15)
                    + (j ^ a16) + (j ^ a17) + (j ^ a18) + (j ^ a19) + (j ^ a20) + (j ^ a21) + (j ^ a22) + (j ^ a23)
                    + (j ^ a24) + (j ^ a25) + (j ^ a26) + (j ^ a27);
                j++;
            }
            while (j < inner);
            i++;
        }
        while (i < outer);
        return doubleSum + intSum;
    }

    // The integer arguments use up the integer hoisting budget so that the explicit null check for `value.X` is
    // judged unprofitable to hoist; the load itself must then not be hoisted either.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumBig(Big value, int n,
        int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
        int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
    {
        double sum = 0;
        int i = 0;
        do
        {
            sum += value.X;
            sum += (i ^ a0) + (i ^ a1) + (i ^ a2) + (i ^ a3) + (i ^ a4) + (i ^ a5) + (i ^ a6) + (i ^ a7)
                + (i ^ a8) + (i ^ a9) + (i ^ a10) + (i ^ a11) + (i ^ a12) + (i ^ a13) + (i ^ a14) + (i ^ a15)
                + (i ^ a16) + (i ^ a17) + (i ^ a18) + (i ^ a19) + (i ^ a20) + (i ^ a21) + (i ^ a22) + (i ^ a23)
                + (i ^ a24) + (i ^ a25) + (i ^ a26) + (i ^ a27);
            i++;
        }
        while (i < n);
        return sum;
    }
}
