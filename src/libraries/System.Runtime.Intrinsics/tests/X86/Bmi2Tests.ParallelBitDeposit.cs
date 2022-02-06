// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b11011011_00101001_01101000_11100101_00101100_11010001_01011010_1001010, 0b10100111_11111011_00011100_00111100_11011110_01011000_11000101_1100100, 0b10001001_01100000_01100000_10000010_01011000_01000010_00000101_00000)]
    [InlineData(0b10101101_00111101_01000011_01010111_11110101_00100001_00101000_1010101, 0b10010100_01010100_11101011_10001111_01100000_00110101_11010000_0110010, 0b10010000_01000100_00100000_00001001_00100000_00000001_01000000_0100010)]
    public void ParallelBitDeposit_nuint_64Bit(ulong value, ulong mask, ulong expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint nativeMask = (nuint)mask;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi2.ParallelBitDeposit(nativeValue, nativeMask);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b11101000_11000110_11111111_0110, 0b10111101_01010111_00100000_0100001, 0b10011101_01010110_00100000_0100000)]
    [InlineData(0b11110111_00001010_01100111_01011, 0b11101000_01101101_11110100_1100001, 0b10100000_00101000_11100100_0100001)]
    public void ParallelBitDeposit_nuint_32Bit(uint value, uint mask, uint expectedResult)
    {
        nuint nativeValue = value;
        nuint nativeMask = mask;
        nuint expectedNativeResult = expectedResult;
        nuint actualResult = Bmi2.ParallelBitDeposit(nativeValue, nativeMask);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
