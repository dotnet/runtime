// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System.Runtime.CompilerServices;
using System;

class SP1b
{

    // Struct in reg (2 ints)
    struct S
    {
        public int i0;
        public int i1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Foo(int i, int j, S s)
    {
        return 1000 * s.i0 + 100 * s.i1 + 10 * i + j;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int M(int i0, int i1, int i2, int i3)
    {
        S s;
        s.i0 = i3;
        s.i1 = i2;
        return Foo(i1, i0, s);  // r0 <= r1; r1 <= r0; r2 <= r3; r3 <= r2
    }

    public static int Main(String[] args)
    {
        int res = M(1, 2, 3, 4);
        Console.WriteLine("M(1, 2, 3, 4) is {0}.", res);
        if (res == 4321)
            return 100;
        else
            return 99;
    }
}
