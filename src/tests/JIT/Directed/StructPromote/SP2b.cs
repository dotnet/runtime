// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP2b
{

    // Struct in reg (int, long)
    struct S
    {
        public int i0;
        public long l1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Foo(int i, S s)
    {
        return 100 * (long)s.i0 + 10 * s.l1 + (long)i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long M(long l0, int i1, int i2)
    {
        S s;
        s.i0 = i1;
        s.l1 = l0;
        return Foo(i2, s); // r0 <= r3; r2 <= r2; outarg[0/4] <= r0/r1;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long res = M(2, 3, 1);
        Console.WriteLine("M(2, 3, 1) is {0}.", res);
        if (res == 321)
            return 100;
        else
            return 99;
    }
}
