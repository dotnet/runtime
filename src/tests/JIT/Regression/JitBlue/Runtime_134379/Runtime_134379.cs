// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// https://github.com/dotnet/runtime/issues/134379: loop hoisting moved a possibly-faulting load above a store to a
// local that the exception handler reads. Such a local is observable after an exception even though it is not
// address exposed, so the store is a barrier for hoisting faulting loads. Each shape stores the local first,
// then dereferences a null object; the handler must see the stored value.
public class Runtime_134379
{
    private class Data
    {
        public int N;
        public double K;
    }

    private static int s_observedByFinally;

    [Fact]
    public static void TestEntryPoint()
    {
        Data d = new Data { N = 3, K = 0.5 };

        // A store to a local that the catch handler reads precedes the dereference of `d`; the handler must see
        // the store even though the loop then throws on `d`. `d.N` stays in the loop (integer hoisting budget)
        // while the `d.K` load it proves would be hoisted if the store were not a barrier.
        Assert.Equal(1638.0, SumObservedByHandler(d, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        Assert.Equal(1.0, SumObservedByHandler(null, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));

        // The same, observed through a finally block.
        Assert.Equal(1638.0, SumObservedByFinally(d, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        Assert.Equal(4, s_observedByFinally);
        s_observedByFinally = -2;
        Assert.Throws<NullReferenceException>(() => SumObservedByFinally(null, 4, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28));
        Assert.Equal(1, s_observedByFinally);

        // The pre-existing shape of the same bug, without register pressure: an ordinary faulting load hoisted
        // above the store to the handler-visible local. Fails on JITs without the handler-visible-store barrier.
        Assert.Equal(12, SumObservedByHandlerPlain(new Data { N = 3 }, 4));
        Assert.Equal(1, SumObservedByHandlerPlain(null, 4));
    }

    // `observed` is not address exposed, but it is live into the handler, so its store must stay ahead of any
    // hoisted copy of `d.K`. The 28 integer arguments keep `d.N` in the loop on every target (see Runtime_134352).
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumObservedByHandler(Data d, int n,
        int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
        int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
    {
        int observed = -1;
        int intSum = 0;
        double doubleSum = 0;
        try
        {
            int i = 0;
            do
            {
                observed = i + 1;
                intSum += d.N;
                doubleSum += d.K;
                intSum += (i ^ a0) + (i ^ a1) + (i ^ a2) + (i ^ a3) + (i ^ a4) + (i ^ a5) + (i ^ a6) + (i ^ a7)
                    + (i ^ a8) + (i ^ a9) + (i ^ a10) + (i ^ a11) + (i ^ a12) + (i ^ a13) + (i ^ a14) + (i ^ a15)
                    + (i ^ a16) + (i ^ a17) + (i ^ a18) + (i ^ a19) + (i ^ a20) + (i ^ a21) + (i ^ a22) + (i ^ a23)
                    + (i ^ a24) + (i ^ a25) + (i ^ a26) + (i ^ a27);
                i++;
            }
            while (i < n);
        }
        catch (NullReferenceException)
        {
            return observed;
        }
        return doubleSum + intSum;
    }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static double SumObservedByFinally(Data d, int n,
        int a0, int a1, int a2, int a3, int a4, int a5, int a6, int a7, int a8, int a9, int a10, int a11, int a12, int a13,
        int a14, int a15, int a16, int a17, int a18, int a19, int a20, int a21, int a22, int a23, int a24, int a25, int a26, int a27)
    {
        int observed = -1;
        int intSum = 0;
        double doubleSum = 0;
        try
        {
            int i = 0;
            do
            {
                observed = i + 1;
                intSum += d.N;
                doubleSum += d.K;
                intSum += (i ^ a0) + (i ^ a1) + (i ^ a2) + (i ^ a3) + (i ^ a4) + (i ^ a5) + (i ^ a6) + (i ^ a7)
                    + (i ^ a8) + (i ^ a9) + (i ^ a10) + (i ^ a11) + (i ^ a12) + (i ^ a13) + (i ^ a14) + (i ^ a15)
                    + (i ^ a16) + (i ^ a17) + (i ^ a18) + (i ^ a19) + (i ^ a20) + (i ^ a21) + (i ^ a22) + (i ^ a23)
                    + (i ^ a24) + (i ^ a25) + (i ^ a26) + (i ^ a27);
                i++;
            }
            while (i < n);
        }
        finally
        {
            s_observedByFinally = observed;
        }
        return doubleSum + intSum;
    }
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int SumObservedByHandlerPlain(Data d, int n)
    {
        int observed = -1;
        int sum = 0;
        try
        {
            int i = 0;
            do
            {
                observed = i + 1;
                sum += d.N;
                i++;
            }
            while (i < n);
        }
        catch (NullReferenceException)
        {
            return observed;
        }
        return sum;
    }
}
