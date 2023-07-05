// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP1
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(S s)
    {
        return s.i0 * 10 + s.i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1)
    {
        S s;
        s.i0 = i1;
        s.i1 = i0;
        return Foo(s);  // r0 <= r1, r1 <= r0
    }

    [Fact]
    public static int TestEntryPoint()
    {
        int res = M(1, 2);
        Console.WriteLine("M(1, 2) is {0}.", res);
        if (res == 21)
            return 100;
        else
            return 99;
    }
}
