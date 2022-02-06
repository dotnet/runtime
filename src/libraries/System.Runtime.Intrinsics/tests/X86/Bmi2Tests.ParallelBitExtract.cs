// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b11010111_10110111_01101001_10110001_10010001_00110000_00010110_00110, 0b11110001_11010111_10110001_11000110_01001010_10001001_11011100_0001010, 0b11111010_11010011_01000100_000101)]
    [InlineData(0b10110110_11110100_10000101_00101010_10011101_10000011_11010110_0100100, 0b11110110_01000011_00001101_10100011_11000010_01010001_01000100_0010011, 0b10111110_00110110_10000111_000)]
    public void ParallelBitExtract_nuint_64Bit(ulong value, ulong mask, ulong expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint nativeMask = (nuint)mask;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi2.ParallelBitExtract(nativeValue, nativeMask);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b11111111_11010110_01010111_0011111, 0b10101110_01110110_11011101_0010111, 0b11111101_11011011_1111)]
    [InlineData(0b10100011_01011011_10111001_101100, 0b10111100_11001011_00101110_011001, 0b11000011_11110001_0)]
    public void ParallelBitExtract_nuint_32Bit(uint value, uint mask, uint expectedResult)
    {
        nuint nativeValue = value;
        nuint nativeMask = mask;
        nuint expectedNativeResult = expectedResult;
        nuint actualResult = Bmi2.ParallelBitExtract(nativeValue, nativeMask);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
