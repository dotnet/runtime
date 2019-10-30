// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;

public class P
{
    public static int Main()
    {
        bool pass = true;

        pass &= (F1(27) == 196418);
        pass &= F3(375);

        if (pass)
        {
            Console.WriteLine("PASS");
            return 100;
        }

        Console.WriteLine("FAIL");
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int F1(int k)
    {
        if (k < 3) return 1;
        return F1(k - 1) + F1(k - 2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool F2(int k)
    {
        return (k == 0) || !F3(k - 1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool F3(int k)
    {
        return (k == 1) || !F2(k - 1);
    }
}
