// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

namespace Test_skippage5_cs
{
public class Program
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct S
    {
        fixed byte x[65500];
    }

    class C
    {
        public S s;
    }

    [Fact]
    public static int TestEntryPoint() => Test(new C());

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Call(int r0, int r1, int r2, int r3, int r4, int r5, int r6, S s)
    {
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static int Test(C c)
    {
        Call(0, 1, 2, 3, 4, 5, 42, c.s);
        Console.WriteLine("TEST PASSED");
        return 100; // If we don't crash, we pass
    }
}
}
