// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void LoadScalarVector128_nint_64Bit(long value)
    {
        unsafe
        {
            nint* ptr = (nint*)&value;
            Vector128<nint> actualVector = Sse2.LoadScalarVector128(ptr);
            Assert.Equal(value, actualVector[0]);
            Assert.Equal(0, actualVector[1]);
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void LoadScalarVector128_nint_32Bit(int value)
    {
        unsafe
        {
            nint* ptr = (nint*)&value;
            Vector128<nint> actualVector = Sse2.LoadScalarVector128(ptr);
            Assert.Equal(value, actualVector[0]);
            Assert.Equal(0, actualVector[1]);
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(ulong.MaxValue)]
    public void LoadScalarVector128_nuint_64Bit(ulong value)
    {
        unsafe
        {
            nuint* ptr = (nuint*)&value;
            Vector128<nuint> actualVector = Sse2.LoadScalarVector128(ptr);
            Assert.Equal(value, actualVector[0]);
            Assert.Equal((nuint)0, actualVector[1]);
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(uint.MaxValue)]
    public void LoadScalarVector128_nuint_32Bit(uint value)
    {
        unsafe
        {
            nuint* ptr = (nuint*)&value;
            Vector128<nuint> actualVector = Sse2.LoadScalarVector128(ptr);
            Assert.Equal(value, actualVector[0]);
            Assert.Equal((nuint)0, actualVector[1]);
        }
    }
}
