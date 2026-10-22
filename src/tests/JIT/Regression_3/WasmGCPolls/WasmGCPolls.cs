// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Xunit;

public class WasmGCPolls
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(10, 45)]
    [InlineData(1000, 499500)]
    public static void TestCycles(int iterations, int expected)
    {
        Assert.Equal(expected, SimpleCycle(iterations));
        Assert.Equal(expected, ReferenceCycle(iterations));
        Assert.Equal(expected, InteriorByrefCycle(iterations));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 3)]
    [InlineData(10, 45)]
    [InlineData(1000, 499500)]
    public static void TestTailCallCycle(int iterations, int expected)
    {
        Assert.Equal(expected, WasmGCPollsTailCalls.First(iterations, 0));
        Assert.Equal(expected, WasmGCPollsTailCalls.SingleBlockTailCall(iterations, 0));
        Assert.Equal(expected, WasmGCPollsTailCalls.SharedSuccessor(iterations, false));
        Assert.Equal(expected, WasmGCPollsTailCalls.SharedSuccessor(iterations, true));
    }

    [Theory]
    [InlineData(0, false, 0)]
    [InlineData(0, true, 0)]
    [InlineData(1, false, 1)]
    [InlineData(1, true, 2)]
    [InlineData(2, false, 3)]
    [InlineData(2, true, 3)]
    [InlineData(3, false, 4)]
    [InlineData(3, true, 5)]
    [InlineData(1000, false, 1500)]
    [InlineData(1000, true, 1500)]
    public static void TestIrreducibleCycle(int iterations, bool enterSecond, int expected)
    {
        Assert.Equal(expected, IrreducibleCycle(iterations, enterSecond));
    }

    // No calls or allocations in the cycle: the compiler must insert a GC poll.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SimpleCycle(int iterations)
    {
        int sum = 0;
        for (int i = 0; i < iterations; i++)
        {
            sum += i;
        }

        return sum;
    }

    private sealed class Box
    {
        public int Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Box CreateBox()
    {
        return new Box();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int ReferenceCycle(int iterations)
    {
        Box box = CreateBox();
        for (int i = 0; i < iterations; i++)
        {
            box.Value += i;
        }

        return box.Value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int InteriorByrefCycle(int iterations)
    {
        ref int value = ref CreateBox().Value;
        for (int i = 0; i < iterations; i++)
        {
            value += i;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IrreducibleCycle(int iterations, bool enterSecond)
    {
        int sum = 0;
        if (iterations <= 0)
        {
            return sum;
        }

        if (enterSecond)
        {
            goto Second;
        }

    First:
        sum++;
        if (--iterations <= 0)
        {
            return sum;
        }

    Second:
        sum += 2;
        if (--iterations > 0)
        {
            goto First;
        }

        return sum;
    }
}
