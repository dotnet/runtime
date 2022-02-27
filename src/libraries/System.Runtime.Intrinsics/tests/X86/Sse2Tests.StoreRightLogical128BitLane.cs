// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 0, 0)]
    [InlineData(7, 64, 1, 4611686018427387904, 0)]
    public void ShiftRightLogical128BitLane_nint_64Bit(long lower, long upper, byte count, long lowerExpected, long upperExpected)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        Vector128<nint> expected = Vector128.Create(lowerExpected, upperExpected).AsNInt();

        Vector128<nint> actual = Sse2.ShiftRightLogical128BitLane(vector, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 0, 0)]
    [InlineData(7, 64, 1, 4611686018427387904, 0)]
    public void ShiftRightLogical128BitLane_nint_32Bit(int lower, int upper, byte count, int lowerExpected, int upperExpected)
    {
        Vector128<nint> vector = Vector128.Create(lower, upper).AsNInt();
        Vector128<nint> expected = Vector128.Create(lowerExpected, upperExpected).AsNInt();

        Vector128<nint> actual = Sse2.ShiftRightLogical128BitLane(vector, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 0, 0)]
    [InlineData(7, 64, 1, 4611686018427387904, 0)]
    public void ShiftRightLogical128BitLane_nuint_64Bit(ulong lower, ulong upper, byte count, ulong lowerExpected, ulong upperExpected)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(lowerExpected, upperExpected).AsNUInt();

        Vector128<nuint> actual = Sse2.ShiftRightLogical128BitLane(vector, count);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(255, 255, 255, 0, 0)]
    [InlineData(240, 240, 240, 0, 0)]
    [InlineData(255, 0, 255, 0, 0)]
    [InlineData(7, 64, 1, 4611686018427387904, 0)]
    public void ShiftRightLogical128BitLane_nuint_32Bit(uint lower, uint upper, byte count, uint lowerExpected, uint upperExpected)
    {
        Vector128<nuint> vector = Vector128.Create(lower, upper).AsNUInt();
        Vector128<nuint> expected = Vector128.Create(lowerExpected, upperExpected).AsNUInt();

        Vector128<nuint> actual = Sse2.ShiftRightLogical128BitLane(vector, count);
        Assert.Equal(expected, actual);
    }
}
