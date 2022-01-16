// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class LzcntTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 64)]
    [InlineData(0b11111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111, 0)]
    [InlineData(0b01111111_11111111_11111111_11111111_11111111_11111111_11111111_11111111, 1)]
    [InlineData(0b00010000_00000000_00000000_00000000_00000000_00000000_00000000_00000000, 3)]
    public void LeadingZeroCount_nuint_64Bit(ulong value, ulong expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Lzcnt.LeadingZeroCount(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0b00000000_00000000_00000000_00000000, 32)]
    [InlineData(0b11111111_11111111_11111111_11111111, 0)]
    [InlineData(0b01111111_11111111_11111111_11111111, 1)]
    [InlineData(0b00010000_00000000_00000000_00000000, 3)]
    public void LeadingZeroCount_nuint_32Bit(uint value, uint expectedResult)
    {
        nuint nativeValue = (nuint)value;
        nuint expectedNativeResult = (nuint)expectedResult;
        nuint actualResult = Lzcnt.LeadingZeroCount(nativeValue);

        Assert.Equal(expectedNativeResult, actualResult);
    }
}
