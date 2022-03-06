// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 170, 24, 321, 450, 123, 12, 128, 1, 65, 0, 65, 128, 170, 24)]
    public void BlendVariable_nint_64Bit(long left1, long left2, long left3, long left4, long right1, long right2, long right3, long right4, long mask1, long mask2, long mask3, long mask4, long expected1, long expected2, long expected3, long expected4)
    {
        Vector256<nint> left = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> right = Vector256.Create(right1, right2, right3, right4).AsNInt();
        Vector256<nint> mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx2.BlendVariable(left, right, mask);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 170, 24, 321, 450, 123, 12, 128, 1, 65, 0, 65, 128, 170, 24)]
    public void BlendVariable_nint_32Bit(int left1, int left2, int left3, int left4, int right1, int right2, int right3, int right4, int mask1, int mask2, int mask3, int mask4, int expected1, int expected2, int expected3, int expected4)
    {
        Vector256<nint> left = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> right = Vector256.Create(right1, right2, right3, right4).AsNInt();
        Vector256<nint> mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNInt();
        Vector256<nint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        Vector256<nint> actual = Avx2.BlendVariable(left, right, mask);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 170, 24, 321, 450, 123, 12, 128, 1, 65, 0, 65, 128, 170, 24)]
    public void BlendVariable_nuint_64Bit(ulong left1, ulong left2, ulong left3, ulong left4, ulong right1, ulong right2, ulong right3, ulong right4, ulong mask1, ulong mask2, ulong mask3, ulong mask4, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        Vector256<nuint> left = Vector256.Create(left1, left2, left3, left4).AsNUInt();
        Vector256<nuint> right = Vector256.Create(right1, right2, right3, right4).AsNUInt();
        Vector256<nuint> mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNUInt();
        Vector256<nuint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        Vector256<nuint> actual = Avx2.BlendVariable(left, right, mask);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 1, 1, 1, 1, 0, 0, 0, 0, 1, 0, 0, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 170, 24, 321, 450, 123, 12, 128, 1, 65, 0, 65, 128, 170, 24)]
    public void BlendVariable_nuint_32Bit(uint left1, uint left2, uint left3, uint left4, uint right1, uint right2, uint right3, uint right4, uint mask1, uint mask2, uint mask3, uint mask4, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        Vector256<nuint> left = Vector256.Create(left1, left2, left3, left4).AsNUInt();
        Vector256<nuint> right = Vector256.Create(right1, right2, right3, right4).AsNUInt();
        Vector256<nuint> mask = Vector256.Create(mask1, mask2, mask3, mask4).AsNUInt();
        Vector256<nuint> expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        Vector256<nuint> actual = Avx2.BlendVariable(left, right, mask);
        Assert.Equal(expected, actual);
    }
}
