// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 10, 0, 20, 20, 5, 10, -10, -20, 5, -10)]
    [InlineData(10, 0, -10, 0, 20, 20, 5, 10, -10, -20, -15, -10)]
    [InlineData(long.MaxValue, long.MinValue, long.MaxValue, long.MaxValue, long.MinValue, long.MaxValue, long.MinValue, long.MaxValue, -1, 1, -1, 0)]
    public void Subtract_nint_64Bit(long left1, long left2, long left3, long left4, long right1, long right2, long right3,
        long right4, long expected1, long expected2, long expected3, long expected4)
    {
        Vector256<nint> left = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> right = Vector256.Create(right1, right2, right3, right4).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx2.Subtract(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 10, 0, 20, 20, 5, 10, -10, -20, 5, -10)]
    [InlineData(10, 0, -10, 0, 20, 20, 5, 10, -10, -20, -15, -10)]
    [InlineData(int.MaxValue, int.MinValue, int.MaxValue, int.MaxValue, int.MinValue, int.MaxValue, int.MinValue, int.MaxValue, -1, 1, -1, 0)]
    public void Subtract_nint_32Bit(int left1, int left2, int left3, int left4, int right1, int right2, int right3,
        int right4, int expected1, int expected2, int expected3, int expected4)
    {
        Vector256<nint> left = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> right = Vector256.Create(right1, right2, right3, right4).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx2.Subtract(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 10, 0, 20, 20, 5, 10, 18446744073709551606, 18446744073709551596, 5, 18446744073709551606)]
    [InlineData(ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, ulong.MaxValue, 100, ulong.MaxValue, 1, 0, ulong.MaxValue - 100, 0, ulong.MaxValue - 1, ulong.MaxValue)]
    public void Subtract_nuint_64Bit(ulong left1, ulong left2, ulong left3, ulong left4, ulong right1, ulong right2, ulong right3,
        ulong right4, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        Vector256<nuint> left = Vector256.Create(left1, left2, left3, left4).AsNUInt();
        Vector256<nuint> right = Vector256.Create(right1, right2, right3, right4).AsNUInt();
        Vector256<nuint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        Vector256<nuint> actual = Avx2.Subtract(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(10, 0, 10, 0, 20, 20, 5, 10, 0, 0, 5, 0)]
    public void Subtract_nuint_32Bit(uint left1, uint left2, uint left3, uint left4, uint right1, uint right2, uint right3,
        uint right4, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        Vector256<nuint> left = Vector256.Create(left1, left2, left3, left4).AsNUInt();
        Vector256<nuint> right = Vector256.Create(right1, right2, right3, right4).AsNUInt();
        Vector256<nuint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        Vector256<nuint> actual = Avx2.Subtract(left, right);
        Assert.Equal(expected, actual);
    }
}
