// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse3Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(123, 123, 321, 321, 123)]
    [InlineData(100, 200, -23, -23, 200)]
    public void ConvertScalarToVector128Double_nint_64Bit(double lowerUpper, double upperUpper, long value, double expectedLower, double expectedUpper)
    {
        Vector128<double> upper = Vector128.Create(lowerUpper, upperUpper);
        Vector128<double> expected = Vector128.Create(expectedLower, expectedUpper);

        Vector128<double> actual = Sse2.ConvertScalarToVector128Double(upper, (nint)value);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(123, 123, 321, 321, 123)]
    [InlineData(100, 200, -23, -23, 200)]
    public void ConvertScalarToVector128Double_nint_32Bit(double lowerUpper, double upperUpper, int value, double expectedLower, double expectedUpper)
    {
        Vector128<double> upper = Vector128.Create(lowerUpper, upperUpper);
        Vector128<double> expected = Vector128.Create(expectedLower, expectedUpper);

        Vector128<double> actual = Sse2.ConvertScalarToVector128Double(upper, (nint)value);
        Assert.Equal(expected, actual);
    }
}
