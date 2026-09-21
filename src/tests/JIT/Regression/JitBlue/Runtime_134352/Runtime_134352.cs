// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Loop hoisting of indirections that local assertion prop proved non-faulting.
//
// Since local assertion prop also runs in postorder during morph, the loads of `d.V` in `Sum` and `d.K` in
// `SumScalar` reach the optimizer flagged GTF_IND_NONFAULTING | GTF_ORDER_SIDEEFF (the loop test's load of
// `d.N` establishes `d != null`). Hoisting must still move them out of the loop when that is legal, and must
// still not move them above a side effect that precedes them in the loop body, since with `d == null` they
// would then fault before that side effect.
public class Runtime_134352
{
    private class Data
    {
        public double[] V;
        public int N;
        public double K;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Data d = new Data { V = new double[] { 1, 2, 3, 4 }, N = 4, K = 0.5 };

        Assert.Equal(5.0, Sum(d));
        Assert.Equal(5.0, SumScalar(d));
        Assert.Throws<NullReferenceException>(() => Sum(null));
        Assert.Throws<NullReferenceException>(() => SumScalar(null));

        int[] log = new int[3];
        Assert.Equal(3 * (4 + 1.0), SumLoadsFirst(d, log));
        Assert.Equal(new int[] { 1, 2, 3 }, log);

        log = new int[3];
        Assert.Throws<NullReferenceException>(() => SumLoadsFirst(null, log));
        // The loads precede the store, so with a null `d` nothing may have been logged,
        // whether or not the loads were hoisted.
        Assert.Equal(new int[] { 0, 0, 0 }, log);

        log = new int[3];
        Assert.Equal(3 * (4 + 1.0), SumStoreFirst(d, log));
        Assert.Equal(new int[] { 1, 2, 3 }, log);

        log = new int[3];
        Assert.Throws<NullReferenceException>(() => SumStoreFirst(null, log));
        // The store precedes the loads, so the first iteration's store must be visible even though the
        // loads fault: the loads may not be hoisted above it.
        Assert.Equal(new int[] { 1, 0, 0 }, log);
    }

    // `d.V` is loop invariant and is the first field that the loop body evaluates: hoistable.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double Sum(Data d)
    {
        // The invariant array field load should be immediately before loop alignment in the preheader.
        // X64:      mov [[ARRAY:[a-z0-9]+]], gword ptr
        // X64-NEXT: align

        double sum = 0;
        for (int j = 0; j < d.N; j++)
        {
            sum += d.V[j] * d.K;
        }
        return sum;
    }

    // Same for an invariant scalar field.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumScalar(Data d)
    {
        // X64:      vmovsd [[SCALAR:xmm[0-9]+]], qword ptr
        // X64-NEXT: align

        double sum = 0;
        for (int j = 0; j < d.N; j++)
        {
            sum += d.K * (j + 1);
        }
        return sum;
    }

    // Same, with a side effect after the loads.
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

    // Here `d.N` (possibly faulting) establishes `d != null` for `d.V` inside the loop body, after a store.
    // Neither load may be hoisted above the store.
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
}
