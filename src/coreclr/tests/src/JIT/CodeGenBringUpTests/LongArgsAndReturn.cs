// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//


using System;
using System.Runtime.CompilerServices;


public class BringUpTest
{
    const int Pass = 100;
    const int Fail = -1;

    // Returns max of two longs
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long LongArgsAndReturn(long a, long b)
    {
       return a>b ? a : b;
    }


    public static int Main()
    {
        long m = LongArgsAndReturn(10L, 20L);
        if (m != 20L) return Fail;
        return Pass;
    }
}
