// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, -1, -1)]
    [InlineData(0, 1, 0, 1, -1, -1)]
    [InlineData(0, 1, 0, 0, -1, 0)]
    [InlineData(0, 123, 0, 123, -1, -1)]
    public void CompareEqual_nint_64Bit(long leftLower, long leftUpper, long rightLower, long rightUpper, long expectedLower, long expectedUpper)
    {
        Vector128<nint> leftVector = Vector128.Create(leftLower, leftUpper).AsNInt();
        Vector128<nint> rightVector = Vector128.Create(rightLower, rightUpper).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.CompareEqual(leftVector, rightVector);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, -1, -1)]
    [InlineData(0, 1, 0, 1, -1, -1)]
    [InlineData(0, 1, 0, 0, -1, 0)]
    [InlineData(0, 123, 0, 123, -1, -1)]
    public void CompareEqual_nint_32Bit(int leftLower, int leftUpper, int rightLower, int rightUpper, int expectedLower, int expectedUpper)
    {
        Vector128<nint> leftVector = Vector128.Create(leftLower, leftUpper).AsNInt();
        Vector128<nint> rightVector = Vector128.Create(rightLower, rightUpper).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.CompareEqual(leftVector, rightVector);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 18446744073709551615, 18446744073709551615)]
    [InlineData(0, 1, 0, 1, 18446744073709551615, 18446744073709551615)]
    [InlineData(0, 1, 0, 0, 18446744073709551615, 0)]
    [InlineData(0, 123, 0, 123, 18446744073709551615, 18446744073709551615)]
    public void CompareEqual_nuint_64Bit(ulong leftLower, ulong leftUpper, ulong rightLower, ulong rightUpper, ulong expectedLower, ulong expectedUpper)
    {
        Vector128<nuint> leftVector = Vector128.Create(leftLower, leftUpper).AsNUInt();
        Vector128<nuint> rightVector = Vector128.Create(rightLower, rightUpper).AsNUInt();
        Vector128<nuint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> actualVector = Sse41.CompareEqual(leftVector, rightVector);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 18446744073709551615, 18446744073709551615)]
    [InlineData(0, 1, 0, 1, 18446744073709551615, 18446744073709551615)]
    [InlineData(0, 1, 0, 0, 18446744073709551615, 0)]
    [InlineData(0, 123, 0, 123, 18446744073709551615, 18446744073709551615)]
    public void CompareEqual_nuint_32Bit(uint leftLower, uint leftUpper, uint rightLower, uint rightUpper, uint expectedLower, uint expectedUpper)
    {
        Vector128<nuint> leftVector = Vector128.Create(leftLower, leftUpper).AsNUInt();
        Vector128<nuint> rightVector = Vector128.Create(rightLower, rightUpper).AsNUInt();
        Vector128<nuint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> actualVector = Sse41.CompareEqual(leftVector, rightVector);
        Assert.Equal(expectedVector, actualVector);
    }
}
