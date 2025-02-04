// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_112053
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<NullReferenceException>(() => Foo(null, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Foo(C c, int x)
    {
        c.G = Test(1 / x);
    }

    static RetBuf Test(int x)
    {
        return new RetBuf();
    }

    class C
    {
        public RetBuf G;
    }

    struct RetBuf
    {
        public nint A, B, C, D, E, F, G, H;
    }
}
