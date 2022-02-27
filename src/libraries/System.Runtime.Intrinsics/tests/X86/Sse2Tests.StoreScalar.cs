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
    public void StoreScalar_nint_64Bit(long value)
    {
        Vector128<nint> sourceVector = Vector128.CreateScalar(value).AsNInt();
        Span<nint> span = stackalloc nint[2];

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Sse2.StoreScalar(ptr, sourceVector);
                Assert.Equal(value, span[0]);
                Assert.Equal(0, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void StoreScalar_nint_32Bit(int value)
    {
        Vector128<nint> sourceVector = Vector128.CreateScalar(value).AsNInt();
        Span<nint> span = stackalloc nint[2];

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Sse2.StoreScalar(ptr, sourceVector);
                Assert.Equal(value, span[0]);
                Assert.Equal(0, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(ulong.MaxValue)]
    public void StoreScalar_nuint_64Bit(ulong value)
    {
        Vector128<nuint> sourceVector = Vector128.CreateScalar(value).AsNUInt();
        Span<nuint> span = stackalloc nuint[2];

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Sse2.StoreScalar(ptr, sourceVector);
                Assert.Equal(value, span[0]);
                Assert.Equal((nuint)0, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(uint.MaxValue)]
    public void StoreScalar_nuint_32Bit(uint value)
    {
        Vector128<nuint> sourceVector = Vector128.CreateScalar(value).AsNUInt();
        Span<nuint> span = stackalloc nuint[2];

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Sse2.StoreScalar(ptr, sourceVector);
                Assert.Equal(value, span[0]);
                Assert.Equal((nuint)0, span[1]);
            }
        }
    }
}
