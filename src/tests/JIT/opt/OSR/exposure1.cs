// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

// Runtime 83738: need to ensure that 's' in 'Foo'
// is marked as address exposed during OSR compiles.

public class Exposure1
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Bar()
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static bool Foo(int n)
    {
        S s = new S { F = 1234 };
        ref int foo = ref s.F;

        for (int i = 0; i < n; i++)
        {
            Bar();
        }

        int abc = s.F * 3 + 4;
        foo = 25;
        int def = s.F * 3 + 4;

        int eabc = 1234 * 3 + 4;
        int edef = 25 * 3 + 4;
        Console.WriteLine("abc = {0} (expected {1}), def = {2} (expected {3})", abc, eabc, def, edef);
        return (abc == eabc && def == edef);
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return Foo(50000) ? 100 : -1;
    }

    public struct S
    {
        public int F;
    }
}
