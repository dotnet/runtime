// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class UnpromotedSpanLength
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ExposeSpan(ref ReadOnlySpan<byte> span) { }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ClearSpan(ref ReadOnlySpan<byte> span) => span = default;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int GuardExposedSpan(ReadOnlySpan<byte> span, int count)
    {
        // CHECK-NOT: ThrowArgumentOutOfRangeException
        ExposeSpan(ref span);
        if (count < 0 || count > span.Length)
            return -1;
        // Bounds check from Slice(0, count).
        if ((uint)count > (uint)span.Length)
            ThrowArgumentOutOfRangeException();
        return count;
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
        // Bounds check from Slice(0, count).
        if ((uint)count > (uint)span.Length)
            ThrowArgumentOutOfRangeException();
        return count;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowArgumentOutOfRangeException() => throw new ArgumentOutOfRangeException();

    [Fact]
    public static void TestEntryPoint()
    {
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
