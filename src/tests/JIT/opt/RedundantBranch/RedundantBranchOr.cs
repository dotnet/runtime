// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class RedundantBranchOr
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_00(int a, int b)
    {
        if ((a > 0) | (b > 0))
        {
            return 1;
        }
        // redundant
        else if (a > 0)
        {
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_01(int a, int b)
    {
        if ((a > 0) | (b > 0))
        {
            return 1;
        }
        // redundant
        else if (a <= 0)
        {
            return 3;
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_02(int a, int b)
    {
        if ((a > 0) | (b > 0))
        {
            return 1;
        }
        // redundant
        else if (b > 0)
        {
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_03(int a, int b)
    {
        if ((a > 0) | (b > 0))
        {
            return 1;
        }
        // redundant
        else if (b <= 0)
        {
            return 3;
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_04(int a, int b)
    {
        if ((a > 0) | (b > 0))
        {
            // redundant
            if ((a > 0) | (b > 0))
            {
                return 1;
            }
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_05(int a, int b)
    {
        if ((a == 0) | (b > 0))
        {
            return 1;
        }
        // redundant
        else if (a == 0)
        {
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Or_06(int a, int b)
    {
        if ((a == 0) | (b > 0))
        {
            return 1;
        }
        // redundant
        else if (b > 0)
        {
            return -1;
        }
        return 3;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        Func<int, int, int>[] funcs = {Or_00, Or_01, Or_02, Or_03, Or_04, Or_05, Or_06};
        int funcNum = 0;
        int cases = 0;
        int errors= 0;

        foreach (var f in funcs)
        {
            for (int a = -1; a <= 1; a++)
            {
                for (int b = -1; b <= 1; b++)
                {
                    cases++;
                    int result = f(a, b);

                    if (result < 0)
                    {
                        Console.WriteLine($"Or_0{funcNum}({a},{b}) = {result} wrong\n");
                        errors++;
                    }
                }
            }
            
            funcNum++;
        }

        Console.WriteLine($"{cases} tests, {errors} errors");
        return errors > 0 ? -1 : 100;
    }
}
