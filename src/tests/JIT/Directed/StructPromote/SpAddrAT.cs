// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SpAddrAT
{

    // This one makes sure that we don't (independently) promote a struct local that is address-taken.

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(S s0, S s1)
    {
        return s0.i0 + s0.i1 + s1.i0 + s1.i1;
    }

    static int Bar(ref S s0)
    {
        return s0.i0 + s0.i1;
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
        int x = Bar(ref s0);
        return Foo(s0, s1) + x;  // r0 <= &s0[0]; r1 <= &s0[4]; r2 <= r2; r3 <= r3
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(1, 2, 3, 4);
        Console.WriteLine("M(1, 2, 3, 4) is {0}.", res);
        if (res == 13)
            return 100;
        else
            return 99;
    }
}
