// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0)]
    [InlineData(0, 20, 1, 123, 1, 11, 1, 123, 0, -1, 0, 0)]
    public void CompareGreaterThan_nint_64Bit(long left1, long left2, long left3, long left4, long right1, long right2, long right3, long right4, long expected1, long expected2, long expected3, long expected4)
    {
        Vector256<nint> left = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> right = Vector256.Create(right1, right2, right3, right4).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx2.CompareGreaterThan(left, right);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(0, 1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0)]
    [InlineData(0, 20, 1, 123, 1, 11, 1, 123, 0, -1, 0, 0)]
    public void CompareGreaterThan_nint_32Bit(int left1, int left2, int left3, int left4, int right1, int right2, int right3, int right4, int expected1, int expected2, int expected3, int expected4)
    {
        Vector256<nint> left = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> right = Vector256.Create(right1, right2, right3, right4).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx2.CompareGreaterThan(left, right);
        Assert.Equal(expected, actual);
    }
}
