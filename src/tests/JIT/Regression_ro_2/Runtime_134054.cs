// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_134054;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134054
{
    private struct S
    {
        public byte B;
        public byte C;
        public int D;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Use(ref S s) { }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static int Test(float f)
    {
        S s = default;
        s.C = 7;
        s.B = (byte)BitConverter.SingleToInt32Bits(f * 3.0f);
        Use(ref s);
        return s.B + (s.C << 8) + (s.D << 16);
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(1844, Test(1.1f));
    }
}
