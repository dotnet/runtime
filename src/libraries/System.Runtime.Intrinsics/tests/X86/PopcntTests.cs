// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed class PopcntTests
{
    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))]
    [InlineData(0b00000000, 0)]
    [InlineData(0b11111111, 8)]
    [InlineData(0b00000111, 3)]
    [InlineData(0b11100000, 3)]
    [InlineData(0b10101010, 4)]
    public void PopCount_nuint_64Bit(long value, long expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Popcnt.PopCount(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(typeof(PlatformDetection), nameof(PlatformDetection.Is32BitProcess))]
    [InlineData(0b0000, 0)]
    [InlineData(0b1111, 4)]
    [InlineData(0b0111, 3)]
    [InlineData(0b1110, 3)]
    [InlineData(0b1010, 2)]
    public void PopCount_nuint_32Bit(int value, int expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Popcnt.PopCount(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
