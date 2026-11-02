// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class CalleeSavePairs
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Barrier(bool throwException)
    {
        if (throwException)
        {
            throw new InvalidOperationException();
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long NineIntegers(long[] values, bool throwException)
    {
        // ARM64: stp x19, x20, [sp, #0x10]
        // ARM64-NEXT: stp x21, x22, [sp, #0x20]
        // ARM64-NEXT: stp x23, x24, [sp, #0x30]
        // ARM64-NEXT: stp x25, x26, [sp, #0x40]
        // ARM64-NEXT: stp x27, x28, [sp, #0x50]
        // ARM64: ldp x27, x28, [sp, #0x50]
        // ARM64-NEXT: ldp x25, x26, [sp, #0x40]
        // ARM64-NEXT: ldp x23, x24, [sp, #0x30]
        // ARM64-NEXT: ldp x21, x22, [sp, #0x20]
        // ARM64-NEXT: ldp x19, x20, [sp, #0x10]
        long a = values[0], b = values[1], c = values[2], d = values[3];
        long e = values[4], f = values[5], g = values[6], h = values[7], i = values[8];
        Barrier(throwException);
        return a + b + c + d + e + f + g + h + i;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double ThreeFloatsEightIntegers(long[] values, double[] floats, bool throwException)
    {
        // ARM64: stp d8, d9, [sp, #0x10]
        // ARM64-NEXT: stp d10, d11, [sp, #0x20]
        // ARM64-NEXT: stp x19, x20, [sp, #0x30]
        // ARM64-NEXT: stp x21, x22, [sp, #0x40]
        // ARM64-NEXT: stp x23, x24, [sp, #0x50]
        // ARM64-NEXT: stp x25, x26, [sp, #0x60]
        // ARM64: ldp x25, x26, [sp, #0x60]
        // ARM64-NEXT: ldp x23, x24, [sp, #0x50]
        // ARM64-NEXT: ldp x21, x22, [sp, #0x40]
        // ARM64-NEXT: ldp x19, x20, [sp, #0x30]
        // ARM64-NEXT: ldp d10, d11, [sp, #0x20]
        // ARM64-NEXT: ldp d8, d9, [sp, #0x10]
        long a = values[0], b = values[1], c = values[2], d = values[3];
        long e = values[4], f = values[5], g = values[6], h = values[7];
        double p = floats[0], q = floats[1], r = floats[2];
        Barrier(throwException);
        return a + b + c + d + e + f + g + h + p + q + r;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double ThreeFloatsNineIntegers(long[] values, double[] floats, bool throwException)
    {
        // ARM64: stp d8, d9, [sp, #0x10]
        // ARM64-NEXT: stp d10, d11, [sp, #0x20]
        // ARM64-NEXT: stp x19, x20, [sp, #0x30]
        // ARM64-NEXT: stp x21, x22, [sp, #0x40]
        // ARM64-NEXT: stp x23, x24, [sp, #0x50]
        // ARM64-NEXT: stp x25, x26, [sp, #0x60]
        // ARM64-NEXT: stp x27, x28, [sp, #0x70]
        // ARM64: ldp x27, x28, [sp, #0x70]
        // ARM64-NEXT: ldp x25, x26, [sp, #0x60]
        // ARM64-NEXT: ldp x23, x24, [sp, #0x50]
        // ARM64-NEXT: ldp x21, x22, [sp, #0x40]
        // ARM64-NEXT: ldp x19, x20, [sp, #0x30]
        // ARM64-NEXT: ldp d10, d11, [sp, #0x20]
        // ARM64-NEXT: ldp d8, d9, [sp, #0x10]
        long a = values[0], b = values[1], c = values[2], d = values[3];
        long e = values[4], f = values[5], g = values[6], h = values[7], i = values[8];
        double p = floats[0], q = floats[1], r = floats[2];
        Barrier(throwException);
        return a + b + c + d + e + f + g + h + i + p + q + r;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        long[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9];
        double[] floats = [1, 2, 3];

        Assert.Equal(45L, NineIntegers(values, false));
        Assert.Equal(42.0, ThreeFloatsEightIntegers(values, floats, false));
        Assert.Equal(51.0, ThreeFloatsNineIntegers(values, floats, false));

        Assert.Throws<InvalidOperationException>(() => NineIntegers(values, true));
        Assert.Throws<InvalidOperationException>(() => ThreeFloatsEightIntegers(values, floats, true));
        Assert.Throws<InvalidOperationException>(() => ThreeFloatsNineIntegers(values, floats, true));
    }
}
