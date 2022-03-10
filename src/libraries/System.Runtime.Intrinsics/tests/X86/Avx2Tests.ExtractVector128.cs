// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 2, 3, 4, 1, 3, 4)]
    [InlineData(1, 2, 3, 4, 2, 1, 2)]
    [InlineData(long.MaxValue, long.MinValue, long.MaxValue, long.MinValue, 1, long.MaxValue, long.MinValue)]
    public void ExtractVector128_nint_64Bit(long value1, long value2, long value3, long value4, byte index, long expectedLeft, long expectedRight)
    {
        Vector256<nint> value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        Vector128<nint> result = Avx2.ExtractVector128(value, index);

        Assert.Equal(expectedLeft, result[0]);
        Assert.Equal(expectedRight, result[1]);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 2, 3, 4, 1, 3, 4)]
    [InlineData(1, 2, 3, 4, 2, 1, 2)]
    [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MinValue, 1, int.MaxValue, int.MinValue)]
    public void ExtractVector128_nint_32Bit(int value1, int value2, int value3, int value4, byte index, int expectedLeft, int expectedRight)
    {
        Vector256<nint> value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        Vector128<nint> result = Avx2.ExtractVector128(value, index);

        Assert.Equal(expectedLeft, result[0]);
        Assert.Equal(expectedRight, result[1]);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 2, 3, 4, 1, 3, 4)]
    [InlineData(1, 2, 3, 4, 2, 1, 2)]
    [InlineData(ulong.MaxValue, ulong.MinValue, ulong.MaxValue, ulong.MinValue, 1, ulong.MaxValue, ulong.MinValue)]
    public void ExtractVector128_nuint_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, byte index, ulong expectedLeft, ulong expectedRight)
    {
        Vector256<nuint> value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        Vector128<nuint> result = Avx2.ExtractVector128(value, index);

        Assert.Equal(expectedLeft, result[0]);
        Assert.Equal(expectedRight, result[1]);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 2, 3, 4, 1, 3, 4)]
    [InlineData(1, 2, 3, 4, 2, 1, 2)]
    [InlineData(uint.MaxValue, uint.MinValue, uint.MaxValue, uint.MinValue, 1, uint.MaxValue, uint.MinValue)]
    public void ExtractVector128_nuint_32Bit(uint value1, uint value2, uint value3, uint value4, byte index, uint expectedLeft, uint expectedRight)
    {
        Vector256<nuint> value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        Vector128<nuint> result = Avx2.ExtractVector128(value, index);

        Assert.Equal(expectedLeft, result[0]);
        Assert.Equal(expectedRight, result[1]);
    }
}
