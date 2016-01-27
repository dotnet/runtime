// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.CompilerServices;

internal class Program
{
    private static int s_result = 100;
    private static int Main()
    {
        Test(1L << 32);
        return s_result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Test(long i)
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

