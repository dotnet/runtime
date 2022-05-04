// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse42Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, -1, 0)]
    [InlineData(0, 1, 0, 0, 0, -1)]
    [InlineData(1, 1, 1, 1, 0, 0)]
    [InlineData(123, 0, 0, 0, -1, 0)]
    [InlineData(0, 321, 0, 0, 0, -1)]
    [InlineData(123, 123, 123, 123, 0, 0)]
    public void CompareGreaterThan_nint_64Bit(long lowerLeft, long upperLeft, long lowerRight, long upperRight, long expectedLower, long expectedUpper)
    {
        Vector128<nint> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> actualResult = Sse42.CompareGreaterThan(left, right);

        Assert.Equal(expectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, -1, 0)]
    [InlineData(0, 1, 0, 0, 0, -1)]
    [InlineData(1, 1, 1, 1, 0, 0)]
    [InlineData(123, 0, 0, 0, -1, 0)]
    [InlineData(0, 321, 0, 0, 0, -1)]
    [InlineData(123, 123, 123, 123, 0, 0)]
    public void CompareGreaterThan_nint_32Bit(int lowerLeft, int upperLeft, int lowerRight, int upperRight, int expectedLower, int expectedUpper)
    {
        Vector128<nint> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> actualResult = Sse42.CompareGreaterThan(left, right);

        Assert.Equal(expectedResult, actualResult);
    }
}

