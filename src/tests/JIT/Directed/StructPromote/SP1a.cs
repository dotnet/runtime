// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP1a
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(int i, S s)
    {
        return 100 * s.i0 + 10 * s.i1 + i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2)
    {
        S s;
        s.i0 = i2;
        s.i1 = i1;
        return Foo(i0, s); // r0 <= r0; r1 <= r2; r2 <= r1
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(1, 2, 3);
        Console.WriteLine("M(1, 2, 3) is {0}.", res);
        if (res == 321)
            return 100;
        else
            return 99;
    }
}
