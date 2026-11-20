// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using Xunit;

public class AddressExposedLoads
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeSpan(ref ReadOnlySpan<byte> span) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearSpan(ref ReadOnlySpan<byte> span) => span = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeInt(ref int value) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int VolatileReads(int value)
    {
        // ARM64: {{ldr|ldar|ldapr}} w{{[0-9]+}},
        // ARM64: {{ldr|ldar|ldapr}} w{{[0-9]+}},
        ExposeInt(ref value);
        return Volatile.Read(ref value) + Volatile.Read(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GuardExposedSpan(ReadOnlySpan<byte> span, int count)
    {
        // ARM64-NOT: ThrowArgumentOutOfRangeException
        // X64-NOT: ThrowArgumentOutOfRangeException
        ExposeSpan(ref span);
        if (count < 0 || count > span.Length)
            return -1;
        return span.Slice(0, count).Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int SliceAfterWrite(ReadOnlySpan<byte> span, int count, bool throughCall)
    {
        ExposeSpan(ref span);
        if (count < 0 || count > span.Length)
            return -1;
        if (throughCall)
            ClearSpan(ref span);
        else
            span = default;
        return span.Slice(0, count).Length;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        Assert.Equal(6, VolatileReads(3));
        byte[] array = new byte[8];
        foreach (int count in new[] { int.MinValue, -1, 0, 1, 8, 9, int.MaxValue })
            Assert.Equal(count >= 0 && count <= 8 ? count : -1, GuardExposedSpan(array, count));
        foreach (bool throughCall in new[] { false, true })
        {
            Assert.Equal(0, SliceAfterWrite(array, 0, throughCall));
            Assert.Throws<ArgumentOutOfRangeException>(() => SliceAfterWrite(array, 1, throughCall));
        }
    }
}
