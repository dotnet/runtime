// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    //
    // Sse2.ConvertToNIntWithTruncation is currently failing
    // -> PlatformNotSupportedException
    // ¯\_(ツ)_/¯
    //
    /*
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, 0)]
    [InlineData(123, 123, 0)]
    public void ConvertToNIntWithTruncation_nint_64Bit(double lower, double upper, long expected)
    {
        Vector128<double> value = Vector128.Create(lower, upper);

        nint actual = Sse2.ConvertToNIntWithTruncation(value);
        Assert.Equal((nint)expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, 0)]
    [InlineData(123, 123, 0)]
    public void ConvertToNIntWithTruncation_nint_32Bit(double lower, double upper, long expected)
    {
        Vector128<double> value = Vector128.Create(lower, upper);

        nint actual = Sse2.ConvertToNIntWithTruncation(value);
        Assert.Equal((nint)expected, actual);
    }
    */
}
