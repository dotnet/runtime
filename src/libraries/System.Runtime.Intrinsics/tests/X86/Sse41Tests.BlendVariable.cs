// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 1, 0)]
    [InlineData(1, 0, 1, 1, 0, 0, 1, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 321, 450, 128, 1, 65, 128)]
    public void BlendVariable_nint_64Bit(long lowerLeft, long upperLeft, long lowerRight, long upperRight, long lowerMask, long upperMask, long expectedLower, long expectedUpper)
    {
        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> mask = Vector128.Create(lowerMask, upperMask).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.BlendVariable(left, right, mask);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 1, 0)]
    [InlineData(1, 0, 1, 1, 0, 0, 1, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 321, 450, 128, 1, 65, 128)]
    public void BlendVariable_nint_32Bit(int lowerLeft, int upperLeft, int lowerRight, int upperRight, int lowerMask, int upperMask, int expectedLower, int expectedUpper)
    {
        Vector128<nint> left = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> right = Vector128.Create(lowerRight, upperRight).AsNInt();
        Vector128<nint> mask = Vector128.Create(lowerMask, upperMask).AsNInt();
        Vector128<nint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNInt();

        Vector128<nint> actualVector = Sse41.BlendVariable(left, right, mask);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 1, 0)]
    [InlineData(1, 0, 1, 1, 0, 0, 1, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 321, 450, 128, 1, 65, 128)]
    public void BlendVariable_nuint_64Bit(ulong lowerLeft, ulong upperLeft, ulong lowerRight, ulong upperRight, ulong lowerMask, ulong upperMask, ulong expectedLower, ulong expectedUpper)
    {
        Vector128<nuint> left = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> right = Vector128.Create(lowerRight, upperRight).AsNUInt();
        Vector128<nuint> mask = Vector128.Create(lowerMask, upperMask).AsNUInt();
        Vector128<nuint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> actualVector = Sse41.BlendVariable(left, right, mask);
        Assert.Equal(expectedVector, actualVector);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(1, 0, 0, 0, 0, 0, 1, 0)]
    [InlineData(1, 0, 1, 1, 0, 0, 1, 0)]
    [InlineData(1, 1, 1, 1, 1, 1, 1, 1)]
    [InlineData(123, 128, 321, 450, 128, 1, 65, 128)]
    public void BlendVariable_nuint_32Bit(uint lowerLeft, uint upperLeft, uint lowerRight, uint upperRight, uint lowerMask, uint upperMask, uint expectedLower, uint expectedUpper)
    {
        Vector128<nuint> left = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> right = Vector128.Create(lowerRight, upperRight).AsNUInt();
        Vector128<nuint> mask = Vector128.Create(lowerMask, upperMask).AsNUInt();
        Vector128<nuint> expectedVector = Vector128.Create(expectedLower, expectedUpper).AsNUInt();

        Vector128<nuint> actualVector = Sse41.BlendVariable(left, right, mask);
        Assert.Equal(expectedVector, actualVector);
    }
}
