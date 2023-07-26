// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//


using System;
using System.Runtime.CompilerServices;
using Xunit;


public class BringUpTest_LongArgsAndReturn
{
    const int Pass = 100;
    const int Fail = -1;

    // Returns max of two longs
    [MethodImplAttribute(MethodImplOptions.NoInlining)]
    public static long LongArgsAndReturn(long a, long b)
    {
       return a>b ? a : b;
    }


    [Fact]
    public static int TestEntryPoint()
    {
        long m = LongArgsAndReturn(10L, 20L);
        if (m != 20L) return Fail;
        return Pass;
    }
}
