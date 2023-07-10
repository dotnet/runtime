// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP1d
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(int i, int j, int k, int k2, int k3, S s)
    {
        return 1000000 * s.i0 + 100000 * s.i1 + 10000 * i + 1000 * j + 100 * k + 10 * k2 + k3;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2, int i3, int i4, int i5, int i6)
    {
        S s;
        s.i0 = i3;
        s.i1 = i2;
        return Foo(i1, i0, i4, i5, i6, s); // r0 <= r1; r1 <= r0; r2 <= inarg[0]; r3 <= inarg[4]; 
        // outarg[0] <= inarg[8]; outarg[4] <= r3; outarg[8] <= r2
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(4, 5, 6, 7, 3, 2, 1);
        Console.WriteLine("M(4, 5, 6, 7, 3, 2, 1) is {0}.", res);
        if (res == 7654321)
            return 100;
        else
            return 99;
    }
}
