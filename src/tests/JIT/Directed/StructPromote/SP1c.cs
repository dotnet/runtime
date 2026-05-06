// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP1c
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(int i, int j, int k, S s)
    {
        return 10000 * s.i0 + 1000 * s.i1 + 100 * i + 10 * j + k;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2, int i3, int i4)
    {
        S s;
        s.i0 = i3;
        s.i1 = i2;
        return Foo(i1, i0, i4, s);  // r0 <= r1; r1 <= r0; r2 <= inarg[0]; r3 <= r3; outarg[0] <= r2
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(2, 3, 4, 5, 1);
        Console.WriteLine("M(2, 3, 4, 5, 1) is {0}.", res);
        if (res == 54321)
            return 100;
        else
            return 99;
    }
}
