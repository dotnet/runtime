// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// FlowGraphNaturalLoop::FindConstInit/HasZeroTripTest walk BasicBlock::GetUniquePred backwards
// from a loop's preheader looking for a constant store to the induction variable (or a zero-trip
// guard). That walk did not stop at EH handler/filter entry blocks, even though such a block's
// single explicit flow-graph predecessor does not capture every way control can really reach it:
// a second-pass "finally" funclet can run on the way into a catch (after an exception filter, or
// when an unclonable finally is reached exceptionally) without any explicit flow edge. As a
// result the loop could be unrolled using a constant that was only valid before that finally ran.
public class Runtime_137683
{
    private static int s_sum;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void MayThrow(int k)
    {
        if (k != 0)
        {
            throw new Exception();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Yes(int x) => x == 0;

    // The loop's "constant init" appears to come from the filter (which sets i = 0 to decide
    // whether to handle the exception), but by the time the catch body runs, the intervening
    // finally (which runs during the unwind, after the filter already ran) has overwritten it.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void RunWithFilterAndFinally(int k)
    {
        int i = 100;
        try
        {
            try
            {
                MayThrow(k);
            }
            finally
            {
                i = 10;
            }
        }
        catch when (Yes(i = 0))
        {
            do
            {
                s_sum += i;
                i++;
            } while (i < 3);
        }
    }

    [Fact]
    public static void TestFilterAndFinally()
    {
        s_sum = 0;
        RunWithFilterAndFinally(1);
        Assert.Equal(10, s_sum);
    }

    private static int s_side;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Touch(int n) => s_side += n;

    // A finally containing a dense switch cannot be cloned by fgCloneFinally, so it keeps a
    // single explicit predecessor: the normal-path BBJ_CALLFINALLY. Walking back from that
    // predecessor finds the constant assignment made on the normal path (i = 0), but that
    // constant is not valid when the finally is instead reached exceptionally, where i retains
    // its earlier value.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void RunWithUnclonableFinally(int k)
    {
        int i = 10;
        try
        {
            MayThrow(k);
            i = 0;
        }
        finally
        {
            do
            {
                s_sum += i;
                i++;
            } while (i < 3);

            switch (k)
            {
                case 0: Touch(0); break;
                case 1: Touch(1); break;
                case 2: Touch(2); break;
                case 3: Touch(3); break;
                case 4: Touch(4); break;
                case 5: Touch(5); break;
                case 6: Touch(6); break;
                case 7: Touch(7); break;
                default: Touch(8); break;
            }
        }
    }

    [Fact]
    public static void TestUnclonableFinallyNormalPath()
    {
        s_sum = 0;
        s_side = 0;
        RunWithUnclonableFinally(0);
        Assert.Equal(3, s_sum);
        Assert.Equal(0, s_side);
    }

    [Fact]
    public static void TestUnclonableFinallyExceptionPath()
    {
        s_sum = 0;
        s_side = 0;
        Assert.Throws<Exception>(() => RunWithUnclonableFinally(1));
        Assert.Equal(10, s_sum);
        Assert.Equal(1, s_side);
    }
}
