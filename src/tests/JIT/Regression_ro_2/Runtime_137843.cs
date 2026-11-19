// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

namespace Runtime_137843;

public class Runtime_137843
{
    private ref struct Inner
    {
        public byte A;
    }

    private ref struct Outer
    {
        public byte X;
        public Inner I;
        public long L;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Outer o = Test(5);
        Assert.Equal(26, o.X + o.I.A + o.L);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Outer Test(byte b)
    {
        Outer o = default;
        o.X = b;
        o.I.A = (byte)(b + 1);
        o.L = b * 3;
        return o;
    }
}
