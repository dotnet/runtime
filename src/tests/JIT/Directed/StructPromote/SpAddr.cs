// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SpAddr
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(S s0, S s1)
    {
        // Console.WriteLine("s0 = [{0}, {1}], s1 = [{2}, {3}]", s0.i0, s0.i1, s1.i0, s1.i1);
        return s0.i0 + s0.i1 + s1.i0 + s1.i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2, int i3)
    {
        S s0;
        s0.i0 = i1;
        s0.i1 = i0;
        S s1;
        s1.i0 = i2;
        s1.i1 = i3;
        return Foo(s0, s1); // r0 <= r1; r1 <= r0; r2 <= r3; r3 <= r2
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(1, 2, 3, 4);
        Console.WriteLine("M(1, 2, 3, 4) is {0}.", res);
        if (res == 10)
            return 100;
        else
            return 99;
    }
}
