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

    [Fact]
    public static int TestEntryPoint()
    {
        Func<int, int>[] funcs = { Dom_00, Dom_01, Dom_02 };
        int[] values = { -10, -3, -2, -1, 0, 1, 2, 3, 10, 499, 500 };
        int[] expectedDom00 = { 3, 3, 3, 3, 3, 3, 3, 3, 1, 3, 3 };
        int[] expectedDom01 = { 3, 3, 3, 3, 3, 3, 3, 1, 1, 1, 1 };
        int[] expectedDom02 = { 1, 1, 3, 3, 3, 3, 3, 3, 3, 3, 3 };
        int[][] expected = { expectedDom00, expectedDom01, expectedDom02 };

        int cases = 0;
        int errors = 0;

        s_effects = 0;

        for (int funcNum = 0; funcNum < funcs.Length; funcNum++)
        {
            for (int valueNum = 0; valueNum < values.Length; valueNum++)
            {
                int before = s_effects;
                int result = funcs[funcNum](values[valueNum]);

                cases++;

                if ((result != expected[funcNum][valueNum]) || (s_effects != (before + 1)))
                {
                    Console.WriteLine($"Dom_0{funcNum}({values[valueNum]}) = {result}, effects={s_effects - before}");
                    errors++;
                }
            }
        }

        Console.WriteLine($"{cases} tests, {errors} errors");
        return errors > 0 ? -1 : 100;
    }
}
