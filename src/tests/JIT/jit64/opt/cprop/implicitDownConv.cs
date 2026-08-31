// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Program
{
    private static int s_result = 100;
    [Fact]
    public static int TestEntryPoint()
    {
        Test(1L << 32);
        return s_result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void Test(long i)
    {
        if (i == 0)
            return;
        int j = (int)i;
        if (j != 0)
        {
            Console.WriteLine("j != 0");
            s_result = 101;
        }
        Console.WriteLine("j == " + j);
    }
}

