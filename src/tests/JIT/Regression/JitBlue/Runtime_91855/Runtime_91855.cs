// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runitme_91855
{
    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Throws<DivideByZeroException>(() => Foo(null, 0));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector128<uint> Foo(C c, uint val)
    {
        return Vector128.Create<uint>(100u / val) & (c.V ^ Vector128<uint>.AllBitsSet);
    }

    private class C
    {
        public Vector128<uint> V;
    }
}
