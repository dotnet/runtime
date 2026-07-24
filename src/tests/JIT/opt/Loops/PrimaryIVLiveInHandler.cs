// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression tests for the induction variable optimization that replaces a
// primary IV that is live in/out of a handler (and therefore cannot be
// enregistered) with a fresh enregisterable local inside the loop. The
// transformation must preserve the value of the IV outside the loop on every
// path, including exceptional ones.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class PrimaryIVLiveInHandler
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void SideEffect() { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Identity(int x) => x;

    private static int[] MakeData(int n)
    {
        int[] data = new int[n];
        for (int i = 0; i < n; i++)
        {
            data[i] = i * 2;
        }

        return data;
    }

    // The loop counter is reused as the return value local and is live across a
    // finally that does not use it. This is the pattern from the original
    // regression: the IV is live in/out of the handler and the optimization
    // should be able to replace it inside the loop while keeping the after-loop
    // value correct.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumLiveAcrossFinally(int[] data)
    {
        int j;
        int sum = 0;
        try
        {
            for (j = 0; j < data.Length; j++)
            {
                sum += data[j];
            }

            j = sum;
        }
        finally
        {
            SideEffect();
        }

        return j;
    }

    // Same shape but the IV's final value (not overwritten) is what is observed
    // after the finally, exercising the store-back into the original local in
    // the loop exit.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int CountLiveAcrossFinally(int[] data, out int sum)
    {
        int j;
        sum = 0;
        try
        {
            for (j = 0; j < data.Length; j++)
            {
                sum += data[j];
            }
        }
        finally
        {
            SideEffect();
        }

        return j;
    }

    // The loop may throw. On the exceptional path the finally runs (without
    // using the IV) and the exception propagates, so the after-loop value is
    // never observed. On the normal path it must be correct.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumWithThrowingLoop(int[] data, int throwAt)
    {
        int j;
        int sum = 0;
        try
        {
            for (j = 0; j < data.Length; j++)
            {
                if (j == throwAt)
                {
                    throw new InvalidOperationException();
                }

                sum += data[j];
            }

            j = sum;
        }
        finally
        {
            SideEffect();
        }

        return j;
    }

    // The IV is live into a catch as a pass-through value used after the
    // try/catch. The optimization must not replace it (or must keep the value
    // correct) since a catch resumes normal execution.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SumLiveAcrossCatch(int[] data, int throwAt)
    {
        int j = 0;
        int sum = 0;
        try
        {
            for (j = 0; j < data.Length; j++)
            {
                if (j == throwAt)
                {
                    throw new InvalidOperationException();
                }

                sum += data[j];
            }

            j = sum;
        }
        catch (InvalidOperationException)
        {
            // Falls through to use j below.
        }

        return j;
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public static void SumLiveAcrossFinally_ReturnsSum(int n)
    {
        int[] data = MakeData(n);
        int expected = 0;
        for (int i = 0; i < n; i++)
        {
            expected += data[i];
        }

        Assert.Equal(expected, SumLiveAcrossFinally(data));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(1000)]
    public static void CountLiveAcrossFinally_ReturnsCountAndSum(int n)
    {
        int[] data = MakeData(n);
        int expectedSum = 0;
        for (int i = 0; i < n; i++)
        {
            expectedSum += data[i];
        }

        int count = CountLiveAcrossFinally(data, out int sum);
        Assert.Equal(n, count);
        Assert.Equal(expectedSum, sum);
    }

    [Fact]
    public static void SumWithThrowingLoop_NormalPath_ReturnsSum()
    {
        int[] data = MakeData(1000);
        int expected = 0;
        for (int i = 0; i < data.Length; i++)
        {
            expected += data[i];
        }

        // throwAt is out of range, so the loop never throws.
        Assert.Equal(expected, SumWithThrowingLoop(data, -1));
    }

    [Fact]
    public static void SumWithThrowingLoop_ExceptionalPath_Throws()
    {
        int[] data = MakeData(1000);
        Assert.Throws<InvalidOperationException>(() => SumWithThrowingLoop(data, 500));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(500)]
    public static void SumLiveAcrossCatch_ReturnsExpected(int throwAt)
    {
        int[] data = MakeData(1000);

        int expected;
        if ((uint)throwAt < (uint)data.Length)
        {
            // The catch is taken with j == throwAt.
            expected = throwAt;
        }
        else
        {
            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i];
            }

            expected = sum;
        }

        Assert.Equal(expected, SumLiveAcrossCatch(data, throwAt));
    }
}
