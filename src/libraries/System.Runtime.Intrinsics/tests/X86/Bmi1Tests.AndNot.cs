// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Bmi1Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000)]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000010)]
    public void AndNot_nuint_64Bit(ulong left, ulong right, ulong expectedResult)
    {
        nuint nativeLeft = (nuint)left;
        nuint nativeRight = (nuint)right;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.AndNot(nativeLeft, nativeRight);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000000)]
    [InlineData(0b00000000_00000000_00000000_00000000, 0b00000000_00000000_00000000_00000010, 0b00000000_00000000_00000000_00000010)]
    public void AndNot_nuint_32Bit(uint left, uint right, uint expectedResult)
    {
        nuint nativeLeft = (nuint)left;
        nuint nativeRight = (nuint)right;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Bmi1.AndNot(nativeLeft, nativeRight);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
