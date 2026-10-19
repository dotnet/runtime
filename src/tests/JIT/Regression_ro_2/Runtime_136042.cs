// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_136042;

using System.Runtime.CompilerServices;
using Xunit;

// Accessing a static of a beforefieldinit class may run its static constructor, which can
// modify the heap. A static read after the access must observe that effect.
// Methods without AggressiveOptimization are precompiled when running with crossgen2.
public static class Runtime_136042
{
    static class Straight
    {
        public static int Counter;
        public class G { public static int Value = ++Counter; }
        public class G<T> { public static int Value = ++Counter; }
    }

    static class StraightOpt
    {
        public static int Counter;
        public class G { public static int Value = ++Counter; }
        public class G<T> { public static int Value = ++Counter; }
    }

    static class Loop
    {
        public static int Counter;
        public class G { public static int Value = ++Counter; }
        public class G<T> { public static int Value = ++Counter; }
    }

    static class LoopOpt
    {
        public static int Counter;
        public class G { public static int Value = ++Counter; }
        public class G<T> { public static int Value = ++Counter; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (int, int, int) StraightNonGeneric()
    {
        int before = Straight.Counter;
        int value = Straight.G.Value;
        return (before, value, Straight.Counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static (int, int, int) StraightGeneric<T>()
    {
        int before = Straight.Counter;
        int value = Straight.G<T>.Value;
        return (before, value, Straight.Counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, int, int) StraightOptNonGeneric()
    {
        int before = StraightOpt.Counter;
        int value = StraightOpt.G.Value;
        return (before, value, StraightOpt.Counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static (int, int, int) StraightOptGeneric<T>()
    {
        int before = StraightOpt.Counter;
        int value = StraightOpt.G<T>.Value;
        return (before, value, StraightOpt.Counter);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LoopNonGeneric(int n)
    {
        int seen = 0;
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            seen = seen * 10 + Loop.Counter;
            sum += Loop.G.Value;
        }
        return seen * 100 + sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int LoopGeneric<T>(int n)
    {
        int seen = 0;
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            seen = seen * 10 + Loop.Counter;
            sum += Loop.G<T>.Value;
        }
        return seen * 100 + sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int LoopOptNonGeneric(int n)
    {
        int seen = 0;
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            seen = seen * 10 + LoopOpt.Counter;
            sum += LoopOpt.G.Value;
        }
        return seen * 100 + sum;
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    static int LoopOptGeneric<T>(int n)
    {
        int seen = 0;
        int sum = 0;
        for (int i = 0; i < n; i++)
        {
            seen = seen * 10 + LoopOpt.Counter;
            sum += LoopOpt.G<T>.Value;
        }
        return seen * 100 + sum;
    }

    static void CheckStraight((int Before, int Value, int After) result, int expectedValue)
    {
        Assert.Equal(expectedValue, result.Value);
        Assert.Equal(expectedValue, result.After);
        Assert.True(result.Before == expectedValue - 1 || result.Before == expectedValue);
    }

    static void CheckLoop(int result, int expectedValue)
    {
        // Two iterations: the first may observe the counter before or after the static constructor ran,
        // the second must observe it after.
        int before = expectedValue - 1;
        int seenEarly = (before * 10 + expectedValue) * 100 + 2 * expectedValue;
        int seenLate = (expectedValue * 10 + expectedValue) * 100 + 2 * expectedValue;
        Assert.True(result == seenEarly || result == seenLate, $"Unexpected result {result}");
    }

    [Fact]
    public static void TestEntryPoint()
    {
        CheckStraight(StraightNonGeneric(), 1);
        CheckStraight(StraightGeneric<object>(), 2);
        CheckStraight(StraightOptNonGeneric(), 1);
        CheckStraight(StraightOptGeneric<object>(), 2);
        CheckLoop(LoopNonGeneric(2), 1);
        CheckLoop(LoopGeneric<object>(2), 2);
        CheckLoop(LoopOptNonGeneric(2), 1);
        CheckLoop(LoopOptGeneric<object>(2), 2);
    }
}
