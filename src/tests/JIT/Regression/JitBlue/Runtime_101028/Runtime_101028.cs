// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_101028
{
    public static bool C1Run = false;
    public static bool C2Run = false;

    [Fact]
    public static int TestEntryPoint()
    {
        int x = 1234;
        Foo(ref x);
        if (!C1Run)
        {
            return 101;
        }

        if (!C2Run)
        {
            return 102;
        }

        return 100;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo(ref int x)
    {
        int value = x;
        int calc = value / (C1.Value | -1);
        int calc2 = value / (C2.Value | -1);
        Consume(calc + calc2);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Consume(int value)
    {
    }

    static class C1
    {
        public static int Value;
        static C1()
        {
            C1Run = true;
        }
    }

    static class C2
    {
        public static int Value;
        static C2()
        {
            C2Run = true;
        }
    }
}
