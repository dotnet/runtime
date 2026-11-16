// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_134066;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134066
{
    public struct S
    {
        public long A;
        public object B;
    }

    public class C
    {
        public S S;
    }

    private static int s_x;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume(long a, long b, long c, long d, long e, long f, long g, long h, S s)
    {
        if (s.A != 42 || (string)s.B != "hello")
        {
            throw new Exception("bad");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(C c)
    {
        // Call-free loop makes the method fully interruptible.
        while (s_x != 0)
        {
            s_x--;
        }
        Consume(1, 2, 3, 4, 5, 6, 7, 8, c.S);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        C c = new C();
        c.S.A = 42;
        c.S.B = "hello";
        for (int i = 0; i < 100; i++)
        {
            Test(c);
        }
    }
}
