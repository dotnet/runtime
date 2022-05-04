// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 255, 0, 255, 255)]
    [InlineData(-255, 0, -255, -255, 0, -255, 0, -255, -255)]
    [InlineData(7, 64, 1, 0, 7, 16384, 0, 0, 0)]
    public void ShiftRightLogical128BitLane_nint_byte_64Bit(long value1, long value2, long value3, long value4, byte count, long expected1, long expected2, long expected3, long expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        var actual = Avx2.ShiftRightLogical128BitLane(value, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 255, 0, 255, 255)]
    [InlineData(-255, 0, -255, -255, 0, -255, 0, -255, -255)]
    [InlineData(7, 64, 1, 0, 7, 896, 8192, 128, 0)]
    public void ShiftRightLogical128BitLane_nint_byte_32Bit(int value1, int value2, int value3, int value4, byte count, int expected1, int expected2, int expected3, int expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        var actual = Avx2.ShiftRightLogical128BitLane(value, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 255, 0, 255, 255)]
    [InlineData(7, 64, 1, 0, 7, 16384, 0, 0, 0)]
    public void ShiftRightLogical128BitLane_nuint_byte_64Bit(ulong value1, ulong value2, ulong value3, ulong value4, byte count, ulong expected1, ulong expected2, ulong expected3, ulong expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        var actual = Avx2.ShiftRightLogical128BitLane(value, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 255, 255, 0, 0, 0, 0)]
    [InlineData(240, 240, 240, 240, 240, 0, 0, 0, 0)]
    [InlineData(255, 0, 255, 255, 0, 255, 0, 255, 255)]
    [InlineData(7, 64, 1, 0, 7, 16384, 0, 0, 0)]
    public void ShiftRightLogical128BitLane_nuint_byte_32Bit(uint value1, uint value2, uint value3, uint value4, byte count, uint expected1, uint expected2, uint expected3, uint expected4)
    {
        var value = Vector256.Create(value1, value2, value3, value4).AsNInt();
        var expected = Vector256.Create(expected1, expected2, expected3, expected4).AsNInt();

        var actual = Avx2.ShiftRightLogical128BitLane(value, count);
        Assert.Equal(expected, actual);
    }
}
