// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi1Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0, 0, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111100, 0, 2, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111100, 1, 0, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111100, 1, 2, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111, 0, 32, 0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111, 0, 31, 0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111110)]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00011100, 2, 3, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000111)]
    public void BitFieldExtract_byte_nuint_64Bit(long value, byte start, byte end, long expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.BitFieldExtract(nativeValue, start, end);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0, 0, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b00000000_00000000_00000000_00000000, 0, 2, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b11111111_11111111_11111111_11111111, 3, 4, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b11111111_11111111_11111111_11111111, 0, 32, 0b11111111_11111111_11111111_11111111)]
    [InlineData(0b11111111_11111111_11111111_11111111, 0, 31, 0b11111111_11111111_11111111_11111110)]
    [InlineData(0b00000000_00000000_00000000_00011100, 2, 3, 0b00000000_00000000_00000000_00000111)]
    public void BitFieldExtract_byte_nuint_32Bit(int value, byte start, byte end, int expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.BitFieldExtract(nativeValue, start, end);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    public void BitFieldExtract_ushort_nuint_64Bit(long value, ushort control, long expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.BitFieldExtract(nativeValue, control);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0, 0b00000000_00000000_00000000_00000000)]
    public void BitFieldExtract_ushort_nuint_32Bit(int value, ushort control, int expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.BitFieldExtract(nativeValue, control);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
