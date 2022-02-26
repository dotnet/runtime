// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Ssse3Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(1, 1, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(0, 0, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_1111, 256, 256)]
    [InlineData(1, 0, 0, 1, 0b0000_1111, 256, 0)]
    [InlineData(1, 0, 0, 1, 0b0000_1110, 65536, 0)]
    [InlineData(1, 1, 1, 1, 0b0101_1111, 0, 0)]
    [InlineData(1, 1, 1, 1, 0b0000_1011, 1099511627776, 1099511627776)]
    public void CompareGreaterThan_nint_64Bit(long lowerLeft, long upperLeft, long lowerRight, long upperRight, byte mask, long expectedLower, long expectedUpper)
    {
        Vector128<nint> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> actualResult = Ssse3.AlignRight(left, right, mask);

        Assert.Equal(expectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(1, 1, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(0, 0, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_1111, 256, 256)]
    [InlineData(1, 0, 0, 1, 0b0000_1111, 256, 0)]
    [InlineData(1, 0, 0, 1, 0b0000_1110, 65536, 0)]
    [InlineData(1, 1, 1, 1, 0b0101_1111, 0, 0)]
    [InlineData(1, 1, 1, 1, 0b0000_1011, 1099511627776, 1099511627776)]
    public void CompareGreaterThan_nint_32Bit(int lowerLeft, int upperLeft, int lowerRight, int upperRight, byte mask, int expectedLower, int expectedUpper)
    {
        Vector128<nint> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> actualResult = Ssse3.AlignRight(left, right, mask);

        Assert.Equal(expectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(1, 1, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(0, 0, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_1111, 256, 256)]
    [InlineData(1, 0, 0, 1, 0b0000_1111, 256, 0)]
    [InlineData(1, 0, 0, 1, 0b0000_1110, 65536, 0)]
    [InlineData(1, 1, 1, 1, 0b0101_1111, 0, 0)]
    [InlineData(1, 1, 1, 1, 0b0000_1011, 1099511627776, 1099511627776)]
    public void CompareGreaterThan_nuint_64Bit(ulong lowerLeft, ulong upperLeft, ulong lowerRight, ulong upperRight, byte mask, ulong expectedLower, ulong expectedUpper)
    {
        Vector128<nuint> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> left = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> right = Vector128.Create(lowerRight, upperRight).AsNUInt();
        Vector128<nuint> actualResult = Ssse3.AlignRight(left, right, mask);

        Assert.Equal(expectedResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(1, 1, 0, 0, 0b0000_0000, 0, 0)]
    [InlineData(0, 0, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_0000, 1, 1)]
    [InlineData(1, 1, 1, 1, 0b0000_1111, 256, 256)]
    [InlineData(1, 0, 0, 1, 0b0000_1111, 256, 0)]
    [InlineData(1, 0, 0, 1, 0b0000_1110, 65536, 0)]
    [InlineData(1, 1, 1, 1, 0b0101_1111, 0, 0)]
    [InlineData(1, 1, 1, 1, 0b0000_1011, 1099511627776, 1099511627776)]
    public void CompareGreaterThan_nuint_32Bit(uint lowerLeft, uint upperLeft, uint lowerRight, uint upperRight, byte mask, uint expectedLower, uint expectedUpper)
    {
        Vector128<nuint> expectedResult = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> left = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> right = Vector128.Create(lowerRight, upperRight).AsNUInt();
        Vector128<nuint> actualResult = Ssse3.AlignRight(left, right, mask);

        Assert.Equal(expectedResult, actualResult);
    }
}
