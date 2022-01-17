// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi1Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111)]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000011)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_10000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_11111111)]
    public void GetMaskUpToLowestSetBit_nuint_64Bit(ulong value, ulong expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.GetMaskUpToLowestSetBit(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0b11111111_11111111_11111111_11111111)]
    [InlineData(0b00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000011)]
    [InlineData(0b11111111_11111111_11111111_11111100, 0b00000000_00000000_00000000_00000111)]
    public void GetMaskUpToLowestSetBit_nuint_32Bit(uint value, uint expectedResult)
    {
        nuint nativeValue = value;
        nuint expectedNativeResult = expectedResult;
        nuint actualResult = Bmi1.GetMaskUpToLowestSetBit(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
