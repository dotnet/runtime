// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public partial class Avx2Tests
{
    /*
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 0)]
    [InlineData(-255, 0, -255, -255, 0, 0)]
    [InlineData(7, 64, 1, 0, 14, 128)]
    [InlineData(7, 64, -1, 0, 0, 0)]
    public void ShiftLeftLogicalVariable_128_nint_64Bit(long lower, long upper, long lowerCount, long upperCount, long lowerExpected, long upperExpected)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        Vector128<nint> count = Vector128.Create(lowerCount, upperCount).AsNInt();
        Vector128<nint> expected = Vector128.Create(lowerExpected, upperExpected).AsNInt();

        Vector128<nint> actual = Avx2.ShiftLeftLogicalVariable(vector, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 0)]
    [InlineData(-255, 0, -255, -255, 0, 0)]
    [InlineData(7, 64, 1, 0, 14, 128)]
    [InlineData(7, 64, -1, 0, 0, 0)]
    public void ShiftLeftLogicalVariable_128_nint_32Bit(int lower, int upper, int lowerCount, int upperCount, int lowerExpected, int upperExpected)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        Vector128<nint> count = Vector128.Create(lowerCount, upperCount).AsNInt();
        Vector128<nint> expected = Vector128.Create(lowerExpected, upperExpected).AsNInt();

        Vector128<nint> actual = Avx2.ShiftLeftLogicalVariable(vector, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 0)]
    [InlineData(7, 64, 1, 0, 14, 128)]
    public void ShiftLeftLogicalVariable_128_nuint_64Bit(ulong lower, ulong upper, ulong lowerCount, ulong upperCount, ulong lowerExpected, ulong upperExpected)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        Vector128<nuint> count = Vector128.Create(lowerCount, upperCount).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(lowerExpected, upperExpected).AsNUInt();

        Vector128<nuint> actual = Avx2.ShiftLeftLogicalVariable(vector, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 0)]
    [InlineData(7, 64, 1, 0, 14, 128)]
    public void ShiftLeftLogicalVariable_128_nuint_32Bit(uint lower, uint upper, uint lowerCount, uint upperCount, uint lowerExpected, uint upperExpected)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        Vector128<nuint> count = Vector128.Create(lowerCount, upperCount).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(lowerExpected, upperExpected).AsNUInt();

        Vector128<nuint> actual = Avx2.ShiftLeftLogicalVariable(vector, count);
        Assert.Equal(expected, actual);
    }
    */

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 255, 255, 255, 0, 0, 0, 0, 255)]
    [InlineData(-255, 0, -255, -255, -255, -255, -255, 0, 0, 0, 0, -255)]
    [InlineData(7, 64, 1, 0, 7, 64, 7, 64, 896, 0, 128, 0)]
    public void ShiftLeftLogicalVariable_256_nint_64Bit(long value1, long value2, long value3, long value4, long count1, long count2, long count3,
        long count4, long expected1, long expected2, long expected3, long expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var count = Vector256.Create(count1, count2, count3, count4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        var actual = Avx2.ShiftLeftLogicalVariable(value, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 255, 255, 255, 0, 0, 0, 0, 255)]
    [InlineData(-255, 0, -255, -255, -255, -255, -255, 0, 0, 0, 0, -255)]
    [InlineData(7, 64, 1, 0, 7, 64, 7, 64, 896, 0, 128, 0)]
    public void ShiftLeftLogicalVariable_256_nint_32Bit(int value1, int value2, int value3, int value4, int count1, int count2, int count3,
        int count4, int expected1, int expected2, int expected3, int expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var count = Vector256.Create(count1, count2, count3, count4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        var actual = Avx2.ShiftLeftLogicalVariable(value, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 255, 255, 255, 0, 0, 0, 0, 255)]
    [InlineData(7, 64, 1, 0, 7, 64, 7, 64, 896, 0, 128, 0)]
    public void ShiftLeftLogicalVariable_256_nuint_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, ulong count1, ulong count2, ulong count3,
        ulong count4, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        var count = Vector256.Create(count1, count2, count3, count4).AsNUInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        var actual = Avx2.ShiftLeftLogicalVariable(value, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 255, 255, 255, 0, 0, 0, 0, 255)]
    [InlineData(7, 64, 1, 0, 7, 64, 7, 64, 896, 0, 128, 0)]
    public void ShiftLeftLogicalVariable_256_nuint_32Bit(uint value1, uint value2, uint value3, uint value4, uint count1, uint count2, uint count3,
        uint count4, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNUInt();
        var count = Vector256.Create(count1, count2, count3, count4).AsNUInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNUInt();

        var actual = Avx2.ShiftLeftLogicalVariable(value, count);
        Assert.Equal(expected, actual);
    }
}
