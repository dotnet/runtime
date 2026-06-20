// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/129642
//
// On x64, a value produced by Vector128<T>.ToScalar() (an XMM -> GPR reinterpret) that is
// used both as an array index and in integer arithmetic was mis-compiled: the contained
// CreateScalar/CreateScalarUnsafe operand of ToScalar lives in a SIMD register, but codegen
// emitted a plain integer 'mov' (which cannot read an XMM register) instead of movd/movq.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public static class Runtime_129642
{
    private static readonly ulong[] s_table = CreateTable();

    private static ulong[] CreateTable()
    {
        var t = new ulong[64];
        for (int i = 0; i < t.Length; i++)
        {
            t[i] = 0x3ff0000000000000UL + (ulong)i;
        }
        return t;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static ulong IndexAndAdd(double x)
    {
        // n is produced by an XMM -> GPR reinterpret (movd/movq), used both as an array
        // index and in integer arithmetic.
        ulong n = Vector128.CreateScalarUnsafe(x).AsUInt64().ToScalar();
        return s_table[n % 64] + n;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static ulong IndexAndShift(double x)
    {
        ulong n = Vector128.CreateScalarUnsafe(x).AsUInt64().ToScalar();
        return s_table[n & 63] + (n << 46);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static uint IndexAndAddInt(float x)
    {
        uint n = Vector128.CreateScalarUnsafe(x).AsUInt32().ToScalar();
        return (uint)s_table.Length + (n & 63) + n;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        ulong n = BitConverter.DoubleToUInt64Bits(1.5);
        Assert.Equal(s_table[n % 64] + n, IndexAndAdd(1.5));
        Assert.Equal(s_table[n & 63] + (n << 46), IndexAndShift(1.5));

        uint m = BitConverter.SingleToUInt32Bits(1.5f);
        Assert.Equal((uint)s_table.Length + (m & 63) + m, IndexAndAddInt(1.5f));
    }
}
