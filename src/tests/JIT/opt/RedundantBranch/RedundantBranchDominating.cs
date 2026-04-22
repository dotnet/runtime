// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Runtime 126554
// These cases specifically exercise redundant dominating branch elimination:
// the innermost test implies one or more dominating tests and all of them share
// the same untaken exit.
public class RedundantBranchDominating
{
    private static int s_effects;
    private static readonly int[] s_values = { -10, -3, -2, -1, 0, 1, 2, 3, 10, 499, 500 };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int SideEffect(int value)
    {
        s_effects++;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dom_00(int count)
    {
        if ((SideEffect(count) > 0) && (count < 500))
        {
            if (count == 10)
            {
                return 1;
            }

            return 3;
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dom_01(int count)
    {
        if (SideEffect(count) > 0)
        {
            if (count > 1)
            {
                if (count > 2)
                {
                    return 1;
                }
            }
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dom_02(int count)
    {
        if (SideEffect(count) > 0)
        {
            return 3;
        }

        if (count < -1)
        {
            if (count < -2)
            {
                return 1;
            }
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dom_03(int count)
    {
        if (count > 0)
        {
            if (SideEffect(count) > 1)
            {
                if (count > 2)
                {
                    return 1;
                }
            }
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Dom_04(int count)
    {
        if ((count > 1) || (count > 0))
        {
            if (count > 0)
            {
                return 1;
            }

            return 2;
        }

        return 3;
    }

    private static void RunTest(string name, Func<int, int> func, int[] expectedResults, int[] expectedEffects)
    {
        s_effects = 0;

        for (int i = 0; i < s_values.Length; i++)
        {
            int value = s_values[i];
            int before = s_effects;
            int result = func(value);
            int effects = s_effects - before;

            Assert.True(result == expectedResults[i], $"{name}({value}) = {result}, expected {expectedResults[i]}");
            Assert.True(effects == expectedEffects[i], $"{name}({value}) effects={effects}, expected {expectedEffects[i]}");
        }
    }

    [Fact]
    public static void TestDom00() =>
        RunTest(nameof(Dom_00), Dom_00, new[] { 3, 3, 3, 3, 3, 3, 3, 3, 1, 3, 3 }, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });

    [Fact]
    public static void TestDom01() =>
        RunTest(nameof(Dom_01), Dom_01, new[] { 3, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1 }, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });

    [Fact]
    public static void TestDom02() =>
        RunTest(nameof(Dom_02), Dom_02, new[] { 1, 1, 3, 3, 3, 3, 3, 3, 3, 3, 3 }, new[] { 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 });

    [Fact]
    public static void TestDom03() =>
        RunTest(nameof(Dom_03), Dom_03, new[] { 3, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1 }, new[] { 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1 });

    [Fact]
    public static void TestDom04() =>
        RunTest(nameof(Dom_04), Dom_04, new[] { 3, 3, 3, 3, 3, 1, 1, 1, 1, 1, 1 }, new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 });
}
