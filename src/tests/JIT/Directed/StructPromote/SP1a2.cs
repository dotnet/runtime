// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP1a2
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(int i, S s, int j)
    {
        return 1000 * j + 100 * s.i0 + 10 * s.i1 + i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2, int i3)
    {
        S s;
        s.i0 = i2;
        s.i1 = i1;
        return Foo(i0, s, i3);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(1, 2, 3, 4);
        Console.WriteLine("M(1, 2, 3, 4) is {0}.", res);
        if (res == 4321)
            return 100;
        else
            return 99;
    }
}
