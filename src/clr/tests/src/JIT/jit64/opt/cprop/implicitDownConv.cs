// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

