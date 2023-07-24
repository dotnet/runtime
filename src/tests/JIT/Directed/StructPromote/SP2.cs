// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

#define USE_STRUCT
using System.Runtime.CompilerServices;
using System;
using Xunit;

public class SP2
{

#if USE_STRUCT
    // Struct in reg (int, long)
    struct S
    {
        public int i0;
        public long l1;
    }
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
#if USE_STRUCT
    static long Foo(S s)
    {
        return 10 * (long)s.i0 + s.l1;
    }
#else
    static long Foo(int i0, long l1) {
        return 10*(long)i0 + l1;
    }
#endif

    [MethodImpl(MethodImplOptions.NoInlining)]
    static long M(long l0, int i1)
    {
#if USE_STRUCT
        S s;
        s.i0 = i1;
        s.l1 = l0;
        return Foo(s);   // r0 <= r2; r2/r3 <= r0/r1
#else
        return Foo(i1, l0);
#endif
    }

    [Fact]
    public static int TestEntryPoint()
    {
        long res = M(1, 2);
        Console.WriteLine("M(1, 2) is {0}.", res);
        if (res == 21)
            return 100;
        else
            return 99;
    }
}
