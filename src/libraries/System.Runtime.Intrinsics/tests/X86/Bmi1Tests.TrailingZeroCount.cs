// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi1Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 64)]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010, 1)]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000110, 1)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_10000000, 7)]
    public void TrailingZeroCount_nuint_64Bit(long value, long expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.ResetLowestSetBit(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 32)]
    [InlineData(0b00000000_00000000_00000000_00000010, 1)]
    [InlineData(0b00000000_00000000_00000000_00000110, 1)]
    [InlineData(0b11111111_11111111_11111111_11111100, 2)]
    public void TrailingZeroCount_nuint_32Bit(int value, int expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.ResetLowestSetBit(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
