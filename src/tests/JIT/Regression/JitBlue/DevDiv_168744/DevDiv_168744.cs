// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Test(ulong i)
    {
        bool res;

        // When folding boolean conditionals (optOptimizeBools) RyuJIT failed to
        // correctly identify when the result of the compare is against 0/1 value.
        if (((i & 0x8000000000000000) == 0x8000000000000000)
            && ((i & 0x0100000000000000) == 0x0100000000000000))
        {
            res = true;
        }
        else
        {
            res = false;
        }

        return res;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        bool res = Program.Test(0x8100000000000000);

        if (res == true)
        {
            Console.WriteLine("Pass");
            return 100;
        }

        Console.WriteLine("Fail");
        return 101;
    }
}
