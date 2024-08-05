// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;
using System.Runtime.CompilerServices;

public class Runtime_91056
{
    [Fact]
    public static void TestEntryPoint()
    {
        S s = default;
        if (False())
        {
            s.A = 1234;
        }

        Foo(0, 0, s, s);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool False() => false;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Foo(int a, int b, S s1, S s2)
    {
    }

    public struct S
    {
        public int A;
    }
}