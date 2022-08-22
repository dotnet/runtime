// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;

class RedundantBranchAnd
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_00(int a, int b)
    {
        if ((a > 0) & (b > 0))
        {
            // redundant
            if (a > 0)
            {
                return 1;
            }
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_01(int a, int b)
    {
        if ((a > 0) & (b > 0))
        {
            // redundant
            if (a <= 0)
            {
                return -1;
            }
            return 1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_02(int a, int b)
    {
        if ((a > 0) & (b > 0))
        {
            // redundant
            if (b > 0)
            {
                return 1;
            }

            return -1;
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_03(int a, int b)
    {
        if ((a > 0) & (b > 0))
        {
            // redundant
            if (b <= 0)
            {
                return -1;
            }

            return 1;
        }

        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_04(int a, int b)
    {
        if ((a > 0) & (b > 0))
        {
            // redundant
            if ((a > 0) & (b > 0))
            {
                return 1;
            }
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_05(int a, int b)
    {
        if ((a == 0) & (b > 0))
        {
            // redundant
            if (a == 0)
            {
                return 1;
            }
            return -1;
        }
        return 3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int And_06(int a, int b)
    {
        if ((a == 0) & (b > 0))
        {
            // redundant
            if (b > 0)
            {
                return 1;
            }
            return -1;
        }
        return 3;
    }


    public static int Main()
    {
        Func<int, int, int>[] funcs = {And_00, And_01, And_02, And_03, And_04, And_05, And_06};
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
                        Console.WriteLine($"And_0{funcNum}({a},{b}) = {result} wrong\n");
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
