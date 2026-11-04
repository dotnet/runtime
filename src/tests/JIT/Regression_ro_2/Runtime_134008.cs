// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Tail duplication in fgUpdateFlowGraph used to endlessly rotate the unconditional
// branch out of the "L = 7" block around the T1 -> T2 -> T3 -> T1 cycle of conditional
// blocks, so the JIT never finished compiling Test.

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134008
{
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static int Test(int b, int c)
    {
        int L = b + c;
        if (b == 1) goto T1;
        if (b == 2) goto T2;
        if (b == 3) goto T3;
        if (c == 1) goto T1;
        if (c == 2) goto T2;
        if (c == 3) goto T3;
        L = 7;
        goto T1;

    T1:
        if (L == 1) return 11;
        goto T2;
    T2:
        if (L == 2) return 22;
        goto T3;
    T3:
        if (L == 3) return 33;
        goto T1;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(11, Test(1, 0));
        Assert.Equal(22, Test(2, 0));
        Assert.Equal(33, Test(3, 0));
    }
}
