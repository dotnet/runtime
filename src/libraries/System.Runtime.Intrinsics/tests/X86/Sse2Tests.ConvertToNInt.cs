// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, -123)]
    [InlineData(123, 123, 123)]
    public void ConvertToNInt_nint_double_64Bit(double lower, double upper, long expected)
    {
        Vector128<double> value = Vector128.Create(lower, upper);

        nint actual = Sse2.ConvertToNInt(value);
        Assert.Equal((nint)expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, -123)]
    [InlineData(123, 123, 123)]
    public void ConvertToNInt_nint_double_32Bit(double lower, double upper, long expected)
    {
        Vector128<double> value = Vector128.Create(lower, upper);

        nint actual = Sse2.ConvertToNInt(value);
        Assert.Equal((nint)expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, -123)]
    [InlineData(123, 123, 123)]
    [InlineData(123, 0, 123)]
    public void ConvertToNInt_nint_long_64Bit(long lower, long upper, long expected)
    {
        Vector128<nint> value = Vector128.Create(lower, upper).AsNInt();

        nint actual = Sse2.ConvertToNInt(value);
        Assert.Equal((nint)expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, -123)]
    [InlineData(123, 123, 123)]
    [InlineData(123, 123, 0)]
    public void ConvertToNInt_nint_long_32Bit(long lower, long upper, long expected)
    {
        Vector128<nint> value = Vector128.Create(lower, upper).AsNInt();

        nint actual = Sse2.ConvertToNInt(value);
        Assert.Equal((nint)expected, actual);
    }
}
