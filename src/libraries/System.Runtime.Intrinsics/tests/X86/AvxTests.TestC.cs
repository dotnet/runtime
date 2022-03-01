// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class AvxTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, true)]
    [InlineData(0, 0, 0, 0, 0, 0, 1, 0, false)]
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 123, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 321, 0, 0, 0, false)]
    public void TestC_nint_64Bit(long left1, long left2, long left3, long left4, long right1, long right2, long right3, long right4, bool expected)
    {
        Vector256<nint> leftVector = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> rightVector = Vector256.Create(right1, right2, right3, right4).AsNInt();

        bool actual = Avx.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, true)]
    [InlineData(0, 0, 0, 0, 0, 0, 1, 0, false)]
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 123, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 321, 0, 0, 0, false)]
    public void TestC_nint_32Bit(int left1, int left2, int left3, int left4, int right1, int right2, int right3, int right4, bool expected)
    {
        Vector256<nint> leftVector = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> rightVector = Vector256.Create(right1, right2, right3, right4).AsNInt();

        bool actual = Avx.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, true)]
    [InlineData(0, 0, 0, 0, 0, 0, 1, 0, false)]
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 123, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 321, 0, 0, 0, false)]
    public void TestC_nuint_64Bit(ulong left1, ulong left2, ulong left3, ulong left4, ulong right1, ulong right2, ulong right3, ulong right4, bool expected)
    {
        Vector256<nint> leftVector = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> rightVector = Vector256.Create(right1, right2, right3, right4).AsNInt();

        bool actual = Avx.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, true)]
    [InlineData(0, 0, 0, 0, 0, 0, 1, 0, false)]
    [InlineData(1, 0, 0, 0, 1, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 123, 0, 0, 0, true)]
    [InlineData(123, 0, 0, 0, 321, 0, 0, 0, false)]
    public void TestC_nuint_32Bit(uint left1, uint left2, uint left3, uint left4, uint right1, uint right2, uint right3, uint right4, bool expected)
    {
        Vector256<nint> leftVector = Vector256.Create(left1, left2, left3, left4).AsNInt();
        Vector256<nint> rightVector = Vector256.Create(right1, right2, right3, right4).AsNInt();

        bool actual = Avx.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }
}
