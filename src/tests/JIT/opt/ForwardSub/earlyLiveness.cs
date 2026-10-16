// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class EarlyLiveness_ForwardSub
{
    [Fact]
    public static int TestEntryPoint()
    {
        int result = 100;
        int test1 = Test1();
        if (test1 != 0)
        {
            Console.WriteLine("Test1 returned {0}", test1);
            result = -1;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Test1()
    {
        S1 s1 = new();
        S1 s2 = s1;
        return Foo(s1) + Foo(s2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Foo(S1 s)
    {
        int result = s.A;
        s.A = 1234;
        Consume(s);
        return result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Consume<T>(T value) { }

    private struct S1
    {
        public int A, B, C, D, E;
    }
}
