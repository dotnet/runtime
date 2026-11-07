// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Runtime_134052;

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_134052
{
    private struct Big32
    {
        public long A, B, C, D;
    }

    private struct HfaD4
    {
        public double A, B, C, D;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Big32 GetBig32() => new Big32 { A = 1, B = 2, C = 3, D = 4 };

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static HfaD4 Test() => Unsafe.BitCast<Big32, HfaD4>(GetBig32());

    [Fact]
    public static void TestEntryPoint()
    {
        HfaD4 result = Test();
        Assert.Equal(1, BitConverter.DoubleToInt64Bits(result.A));
        Assert.Equal(2, BitConverter.DoubleToInt64Bits(result.B));
        Assert.Equal(3, BitConverter.DoubleToInt64Bits(result.C));
        Assert.Equal(4, BitConverter.DoubleToInt64Bits(result.D));
    }
}
