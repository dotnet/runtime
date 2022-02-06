// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b10110100_11100000_11100000_00010010_11111100_01100010_01011011_110011, 0b10001000_00101011_01010000_10001101_00110001_11010001_11010111_1000100, 0b11000000_01101100_00100011_01110011_10000100_10110010_10011010_1100)]
    [InlineData(0b11101110_00010001_11011100_11011111_10111100_11101100_01101000_10111, 0b11100111_01101111_10100111_01100111_01011010_01001110_00001111_111010, 0b11010111_00111001_11110011_10111010_01101100_10111100_11101111_001)]
    public void MultiplyNoFlags_nuint_64Bit(ulong left, ulong right, ulong expectedResult)
    {
        nuint nativeLeft = (nuint)left;
        nuint nativeRight = (nuint)right;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi2.MultiplyNoFlags(nativeLeft, nativeRight);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b11000001_01101101_00111000_0001, 0b11110011_11000011_01111101_0100111, 0b10111000_00101110_01100001_000)]
    [InlineData(0b10100101_10000000_01010011_1100110, 0b11101000_11101111_00100101_010110, 0b10010110_10010110_11100110_11100)]
    public void MultiplyNoFlags_nuint_32Bit(uint left, uint right, uint expectedResult)
    {
        nuint nativeLeft = left;
        nuint nativeRight = right;
        nuint expectedNativeResult = expectedResult;
        nuint actualResult = Bmi2.MultiplyNoFlags(nativeLeft, nativeRight);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
