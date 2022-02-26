// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(1, 1, 1, 0, 1, 0)]
    [InlineData(10, 10, 10, 50, 100, 500)]
    [InlineData(75524, 123, 23, 321, 1737052, 39483)]
    public void Multiply_nint_64Bit(long leftLower, long leftUpper, long rightLower, long rightUpper, long expectedLower, long expectedUpper)
    {
        Vector128<nint> leftVector = Vector128.Create(leftLower, leftUpper).AsNInt();
        Vector128<nint> rightVector = Vector128.Create(rightLower, rightUpper).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.Multiply(leftVector, rightVector);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(1, 1, 1, 0, 1, 0)]
    [InlineData(10, 10, 10, 50, 100, 500)]
    [InlineData(75524, 123, 23, 321, 1737052, 39483)]
    public void Multiply_nint_32Bit(int leftLower, int leftUpper, int rightLower, int rightUpper, int expectedLower, int expectedUpper)
    {
        Vector128<nint> leftVector = Vector128.Create(leftLower, leftUpper).AsNInt();
        Vector128<nint> rightVector = Vector128.Create(rightLower, rightUpper).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.Multiply(leftVector, rightVector);
        Assert.Equal(expectedVector, actualVector);
    }
}
