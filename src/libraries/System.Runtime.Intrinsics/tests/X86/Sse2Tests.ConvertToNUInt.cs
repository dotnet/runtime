// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    //
    // Sse2.ConvertToNUInt is currently failing
    // -> PlatformNotSupportedException
    // ¯\_(ツ)_/¯
    //
    /*
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, 0)]
    [InlineData(123, 123, 0)]
    public void ConvertToNUInt_nuint_64Bit(ulong lower, ulong upper, ulong expected)
    {
        Vector128<nuint> value = Vector128.Create(lower, upper).AsNUInt();

        nuint actual = Sse2.ConvertToNUInt(value);
        Assert.Equal((nuint)expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0)]
    [InlineData(-123, 4294967173, 0)]
    [InlineData(123, 123, 0)]
    public void ConvertToNUInt_nuint_32Bit(ulong lower, ulong upper, ulong expected)
    {
        Vector128<nuint> value = Vector128.Create(lower, upper).AsNUInt();

        nuint actual = Sse2.ConvertToNUInt(value);
        Assert.Equal((nuint)expected, actual);
    }
    */
}
