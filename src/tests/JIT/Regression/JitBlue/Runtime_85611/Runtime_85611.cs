// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_85611
{
    private static int s_result;

    [Fact]
    public static int TestEntryPoint()
    {
        S s = new();
        s.A = GetA();
        // We need to be careful not to reorder the two accesses of 's' in the call to 'Foo.
        // The second is marked as a last use and we make use of that to omit a copy;
        // if we reorder them then s.A will read the mutated value inside Bar.
        Foo(s.A, Bar(s));
        if (s_result != 100)
        {
            Console.WriteLine("FAIL: Result is {0}", s_result);
        }

        return s_result;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GetA() => 100;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(int field, int arg)
    {
        s_result = field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int Bar(S s)
    {
        Use(ref s);
        s.A = 101;
        return 42;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(ref S s)
    {
    }

    private struct S
    {
        public int A, B, C, D, E, F, G, H;
    }
}
