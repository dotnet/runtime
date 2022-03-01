// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class AvxTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(20, 30, 40, 50, 0, 20, 30)]
    [InlineData(20, 30, 40, 50, 1, 40, 50)]
    [InlineData(20, 30, 40, 50, 2, 20, 30)]
    [InlineData(-20, 30, 40, 50, 2, -20, 30)]
    public void ExtractVector128_nint_64Bit(long value1, long value2, long value3, long value4, byte index, long expectedLeft, long expectedRight)
    {
        Vector256<nint> value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        Vector128<nint> expected = Vector128.Create(expectedLeft, expectedRight).AsNInt();

        Vector128<nint> actual = Avx.ExtractVector128(value, index);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(20, 30, 40, 50, 0, 20, 30)]
    [InlineData(20, 30, 40, 50, 1, 40, 50)]
    [InlineData(20, 30, 40, 50, 2, 20, 30)]
    [InlineData(-20, 30, 40, 50, 2, -20, 30)]
    public void ExtractVector128_nint_32Bit(int value1, int value2, int value3, int value4, byte index, int expectedLeft, int expectedRight)
    {
        Vector256<nint> value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        Vector128<nint> expected = Vector128.Create(expectedLeft, expectedRight).AsNInt();

        Vector128<nint> actual = Avx.ExtractVector128(value, index);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(20, 30, 40, 50, 0, 20, 30)]
    [InlineData(20, 30, 40, 50, 1, 40, 50)]
    [InlineData(20, 30, 40, 50, 2, 20, 30)]
    public void ExtractVector128_nuint_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, byte index, ulong expectedLeft, ulong expectedRight)
    {
        Vector256<nuint> value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(expectedLeft, expectedRight).AsNUInt();

        Vector128<nuint> actual = Avx.ExtractVector128(value, index);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(20, 30, 40, 50, 0, 20, 30)]
    [InlineData(20, 30, 40, 50, 1, 40, 50)]
    [InlineData(20, 30, 40, 50, 2, 20, 30)]
    public void ExtractVector128_nuint_32Bit(uint value1, uint value2, uint value3, uint value4, byte index, uint expectedLeft, uint expectedRight)
    {
        Vector256<nuint> value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(expectedLeft, expectedRight).AsNUInt();

        Vector128<nuint> actual = Avx.ExtractVector128(value, index);
        Assert.Equal(expected, actual);
    }
}
