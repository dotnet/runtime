// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class PopcntTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111, 64)]
    [InlineData(0b00000111_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 3)]
    [InlineData(0b11100000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 3)]
    [InlineData(0b10101010_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 4)]
    [InlineData(0b11111111_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 8)]
    public void PopCount_nuint_64Bit(ulong value, ulong expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Popcnt.PopCount(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0)]
    [InlineData(0b11111111_11111111_11111111_11111111, 32)]
    [InlineData(0b10100000_00000000_00000000_00000000, 2)]
    [InlineData(0b01110000_00000000_00000000_00000000, 3)]
    [InlineData(0b11110000_00000000_00000000_00000000, 4)]
    [InlineData(0b11100000_00010000_00000100_00000000, 5)]
    public void PopCount_nuint_32Bit(uint value, uint expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Popcnt.PopCount(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
