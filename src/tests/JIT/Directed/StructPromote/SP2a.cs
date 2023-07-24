// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP2a
{

    // Struct in reg (long, int)
    struct S
    {
        public long l0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long Foo(int i, S s)
    {
        return 100 * (long)s.i1 + 10 * s.l0 + (long)i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long M(long l0, int i1, int i2)
    {
        S s;
        s.i1 = i1;
        s.l0 = l0;
        return Foo(i2, s);  // r0 <= r3; r2/r3 <= r0/r1; outarg[0] <= r3
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
