// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class AddExtendedOperand
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Unsigned(ulong wide, uint narrow)
    {
        // Keep the full-width source alive: a standalone zero extension would
        // need a separate register to avoid overwriting its upper bits.
        return ((ulong)(uint)wide + narrow) ^ wide;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong Reversed(ulong wide, uint narrow) => (narrow + (ulong)(uint)wide) ^ wide;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Signed(long wide, uint narrow) => ((long)(int)wide + narrow) ^ wide;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong LoadedUnsigned(ulong wide, ref uint narrow)
    {
        // Keep the load's zero extension and contain the extension of wide in ADD.
        // ARM64: ldr     w[[N:[0-9]+]], [x1]
        // ARM64-NEXT: add     x{{[0-9]+}}, x[[N]], w0, UXTW
        return ((ulong)(uint)wide + narrow) ^ wide;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long LoadedSigned(long wide, ref uint narrow)
    {
        // ARM64: ldr     w[[N:[0-9]+]], [x1]
        // ARM64-NEXT: add     x{{[0-9]+}}, x[[N]], w0, SXTW
        return ((long)(int)wide + narrow) ^ wide;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long SignedInt(int value, ref uint narrow)
    {
        // ARM64-NOT: sxtw
        // ARM64: add     {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW
        return (long)value + narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long ComputedInt(int value, ref uint narrow)
    {
        // ARM64-NOT: sxtw
        // ARM64: add     {{x[0-9]+}}, {{x[0-9]+}}, {{w[0-9]+}}, SXTW
        return (long)unchecked(value * 3) + narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong UnsignedInt(uint value, ref uint narrow)
    {
        // ARM64: ldr     w[[N:[0-9]+]], [x1]
        // ARM64-NEXT: add     x{{[0-9]+}}, x[[N]], w0, UXTW
        return (ulong)value + narrow;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ulong BothWide(ulong first, ulong second) => ((ulong)(uint)first + (uint)second) ^ first ^ second;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long Checked(long wide, uint narrow) => ((long)checked((int)wide) + narrow) ^ wide;

    [Fact]
    public static void TestEntryPoint()
    {
        ulong[] values = { 0, 1, uint.MaxValue, 0x1234567880000000, 0xFFFFFFFF00000001, ulong.MaxValue };
        uint[] counts = { 0, 1, int.MaxValue, uint.MaxValue };
        foreach (ulong wide in values)
        foreach (uint narrow in counts)
        {
            ulong low = wide & uint.MaxValue;
            long signedLow = low <= int.MaxValue ? (long)low : (long)low - 0x100000000L;
            Assert.Equal((low + narrow) ^ wide, Unsigned(wide, narrow));
            Assert.Equal((low + narrow) ^ wide, Reversed(wide, narrow));
            Assert.Equal((signedLow + narrow) ^ (long)wide, Signed((long)wide, narrow));
            uint fromMemory = narrow;
            Assert.Equal((low + narrow) ^ wide, LoadedUnsigned(wide, ref fromMemory));
            Assert.Equal((signedLow + narrow) ^ (long)wide, LoadedSigned((long)wide, ref fromMemory));
            Assert.Equal(signedLow + narrow, SignedInt((int)wide, ref fromMemory));
            Assert.Equal((long)unchecked((int)wide * 3) + narrow, ComputedInt((int)wide, ref fromMemory));
            Assert.Equal(low + narrow, UnsignedInt((uint)wide, ref fromMemory));
            Assert.Equal((low + narrow) ^ wide ^ (0x1234567800000000UL | narrow),
                BothWide(wide, 0x1234567800000000UL | narrow));
        }

        Assert.Throws<NullReferenceException>(() => LoadedUnsigned(0, ref Unsafe.NullRef<uint>()));
        Assert.Throws<NullReferenceException>(() => LoadedSigned(0, ref Unsafe.NullRef<uint>()));
        Assert.Equal(0L, Checked(-1, 0));
        Assert.Throws<OverflowException>(() => Checked(0x100000000, 0));
        Assert.Throws<OverflowException>(() => Checked(long.MinValue, 0));
    }
}
