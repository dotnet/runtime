// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class AvxTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 20, 30, 40, 0, 0, 0, 0, 0, 30, 40)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 0, 2222, 4444, 1111, 1111)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 1, 1111, 1111, 2222, 4444)]
    [InlineData(-1111, 1111, 1111, -1111, -2222, 4444, 2, -2222, 4444, 1111, -1111)]
    public void InsertVector128_nint_64Bit(long value1, long value2, long value3, long value4, long dataLeft, long dataRight, byte index, long expected1, long expected2, long expected3, long expected4)
    {
        Vector256<nint> value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        Vector128<nint> data = Vector128.Create(dataLeft, dataRight).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx.InsertVector128(value, data, index);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 20, 30, 40, 0, 0, 0, 0, 0, 30, 40)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 0, 2222, 4444, 1111, 1111)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 1, 1111, 1111, 2222, 4444)]
    public void InsertVector128_nint_32Bit(int value1, int value2, int value3, int value4, int dataLeft, int dataRight, byte index, int expected1, int expected2, int expected3, int expected4)
    {
        Vector256<nint> value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        Vector128<nint> data = Vector128.Create(dataLeft, dataRight).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx.InsertVector128(value, data, index);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 20, 30, 40, 0, 0, 0, 0, 0, 30, 40)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 0, 2222, 4444, 1111, 1111)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 1, 1111, 1111, 2222, 4444)]
    public void InsertVector128_nuint_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, ulong dataLeft, ulong dataRight, byte index, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        Vector256<nuint> value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        Vector128<nuint> data = Vector128.Create(dataLeft, dataRight).AsNUInt();
        Vector256<nuint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        Vector256<nuint> actual = Avx.InsertVector128(value, data, index);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 20, 30, 40, 0, 0, 0, 0, 0, 30, 40)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 0, 2222, 4444, 1111, 1111)]
    [InlineData(1111, 1111, 1111, 1111, 2222, 4444, 1, 1111, 1111, 2222, 4444)]
    public void InsertVector128_nuint_32Bit(uint value1, uint value2, uint value3, uint value4, uint dataLeft, uint dataRight, byte index, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        Vector256<nuint> value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        Vector128<nuint> data = Vector128.Create(dataLeft, dataRight).AsNUInt();
        Vector256<nuint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        Vector256<nuint> actual = Avx.InsertVector128(value, data, index);
        Assert.Equal(expected, actual);
    }
}
