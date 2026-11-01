// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class ByrefStoreForwarding
{
    [Fact]
    public static void ReloadsUseStoredValue()
    {
        int intValue = 5;
        Assert.Equal(8, IncrementInt(ref intValue));
        Assert.Equal(8, intValue);

        long longValue = long.MaxValue - 1;
        Assert.Equal(long.MinValue + 1, IncrementLong(ref longValue));
        Assert.Equal(long.MinValue + 1, longValue);

        float singleValue = 0.25f;
        Assert.Equal(3.25f, IncrementSingle(ref singleValue));
        Assert.Equal(3.25f, singleValue);

        double doubleValue = -0.5;
        Assert.Equal(2.5, IncrementDouble(ref doubleValue));
        Assert.Equal(2.5, doubleValue);

        int flags = -1;
        UpdateFlags(ref flags);
        Assert.Equal(~0x30, flags);
    }

    [Fact]
    public static void ReinterpretingReloadsReadMemory()
    {
        long longValue = 0x3FF0000000000000 - 3;
        Assert.Equal(1.0, IncrementLongReadDouble(ref longValue));

        double doubleValue = 1.0;
        Assert.Equal(BitConverter.DoubleToInt64Bits(4.0), IncrementDoubleReadLong(ref doubleValue));

        int intValue = 0x3F800000 - 3;
        Assert.Equal(1f, IncrementIntReadSingle(ref intValue));

        long pair = 0x55_00000000;
        long expected = pair;
        Unsafe.As<long, int>(ref expected) += 3;
        Assert.Equal(expected, IncrementIntReadLong(ref Unsafe.As<long, int>(ref pair)));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int IncrementInt(ref int value)
    {
        // ARM64: str w{{[0-9]+}}, [x0]
        // ARM64-NOT: ldr
        // X64: mov dword ptr [{{[a-z]+}}], eax
        // X64-NOT: ptr [
        value += 3;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long IncrementLong(ref long value)
    {
        // ARM64: str x{{[0-9]+}}, [x0]
        // ARM64-NOT: ldr
        // X64: mov qword ptr [{{[a-z]+}}], rax
        // X64-NOT: ptr [
        value += 3;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float IncrementSingle(ref float value)
    {
        // ARM64: str s{{[0-9]+}}, [x0]
        // ARM64-NOT: ldr
        // X64: {{v?}}movss dword ptr [{{[a-z]+}}], xmm0
        // X64-NOT: ptr [
        value += 3;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double IncrementDouble(ref double value)
    {
        // ARM64: str d{{[0-9]+}}, [x0]
        // ARM64-NOT: ldr
        // X64: {{v?}}movsd qword ptr [{{[a-z]+}}], xmm0
        // X64-NOT: ptr [
        value += 3;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void UpdateFlags(ref int value)
    {
        // ARM64: str w{{[0-9]+}}, [x0]
        // ARM64-NOT: ldr
        // X64: and dword ptr [{{[a-z]+}}], -17
        // X64-NEXT: and dword ptr [{{[a-z]+}}], -33
        // X64-NEXT: or dword ptr [{{[a-z]+}}], 64
        value &= ~0x10;
        value &= ~0x20;
        value |= 0x40;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static double IncrementLongReadDouble(ref long value)
    {
        // ARM64: str x{{[0-9]+}}, [x0]
        // ARM64: ldr d{{[0-9]+}}, [x0]
        // X64: add qword ptr [{{[a-z]+}}], 3
        // X64: {{v?}}movsd xmm0, qword ptr [{{[a-z]+}}]
        value += 3;
        return Unsafe.As<long, double>(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long IncrementDoubleReadLong(ref double value)
    {
        // ARM64: str d{{[0-9]+}}, [x0]
        // ARM64: ldr x{{[0-9]+}}, [x0]
        // X64: {{v?}}movsd qword ptr [{{[a-z]+}}], xmm0
        // X64: mov rax, qword ptr [{{[a-z]+}}]
        value += 3;
        return Unsafe.As<double, long>(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static float IncrementIntReadSingle(ref int value)
    {
        // ARM64: str w{{[0-9]+}}, [x0]
        // ARM64: ldr s{{[0-9]+}}, [x0]
        // X64: add dword ptr [{{[a-z]+}}], 3
        // X64: {{v?}}movss xmm0, dword ptr [{{[a-z]+}}]
        value += 3;
        return Unsafe.As<int, float>(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static long IncrementIntReadLong(ref int value)
    {
        // ARM64: str w{{[0-9]+}}, [x0]
        // ARM64: ldr x{{[0-9]+}}, [x0]
        // X64: add dword ptr [{{[a-z]+}}], 3
        // X64: mov rax, qword ptr [{{[a-z]+}}]
        value += 3;
        return Unsafe.As<int, long>(ref value);
    }
}
