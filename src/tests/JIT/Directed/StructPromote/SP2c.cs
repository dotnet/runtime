// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP2c
{

    // Struct in reg (int, long)
    struct S
    {
        public int i0;
        public long l1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Foo(int i, long l, S s)
    {
        return l * 1000 + 100 * (long)s.i0 + 10 * s.l1 + (long)i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long M(long l0, long l1, int i1, int i2)
    {
        S s;
        s.i0 = i1;
        s.l1 = l1;
        return Foo(i2, l0, s); // r0 <= inarg[4]; r2/r3 <= r0/r1; outarg[0] <= inarg[0]; outarg[8/12] <= r2/r3
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long res = M(4, 2, 3, 1);
        Console.WriteLine("M(4, 2, 3, 1) is {0}.", res);
        if (res == 4321)
            return 100;
        else
            return 99;
    }
}
