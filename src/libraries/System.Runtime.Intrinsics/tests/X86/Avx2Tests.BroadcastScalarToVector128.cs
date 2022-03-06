// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Avx2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void BroadcastScalarToVector128_nint_64Bit(long scalarValue)
    {
        Vector128<nint> value = Vector128.CreateScalar(scalarValue).AsNInt();

        Vector128<nint> result = Avx2.BroadcastScalarToVector128(value);
        Assert.Equal(scalarValue, result[0]);
        Assert.Equal(scalarValue, result[1]);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void BroadcastScalarToVector128_nint_32Bit(int scalarValue)
    {
        Vector128<nint> value = Vector128.CreateScalar(scalarValue).AsNInt();

        Vector128<nint> result = Avx2.BroadcastScalarToVector128(value);
        Assert.Equal(scalarValue, result[0]);
        Assert.Equal(scalarValue, result[1]);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    public void BroadcastScalarToVector128_nuint_64Bit(ulong scalarValue)
    {
        Vector128<nuint> value = Vector128.CreateScalar(scalarValue).AsNUInt();

        Vector128<nuint> result = Avx2.BroadcastScalarToVector128(value);
        Assert.Equal(scalarValue, result[0]);
        Assert.Equal(scalarValue, result[1]);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    public void BroadcastScalarToVector128_nuint_32Bit(uint scalarValue)
    {
        Vector128<nuint> value = Vector128.CreateScalar(scalarValue).AsNUInt();

        Vector128<nuint> result = Avx2.BroadcastScalarToVector128(value);
        Assert.Equal(scalarValue, result[0]);
        Assert.Equal(scalarValue, result[1]);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void BroadcastScalarToVector128_nint_Pointer_64Bit(long scalarValue)
    {
        Span<nint> source = stackalloc nint[2];
        source[0] = (nint)scalarValue;
        source[1] = 0;

        unsafe
        {
            fixed (nint* ptr = &source[0])
            {
                Vector128<nint> result = Avx2.BroadcastScalarToVector128(ptr);
                Assert.Equal(scalarValue, result[0]);
                Assert.Equal(scalarValue, result[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void BroadcastScalarToVector128_nint_Pointer_32Bit(int scalarValue)
    {
        Span<nint> source = stackalloc nint[2];
        source[0] = scalarValue;
        source[1] = 0;

        unsafe
        {
            fixed (nint* ptr = &source[0])
            {
                Vector128<nint> result = Avx2.BroadcastScalarToVector128(ptr);
                Assert.Equal(scalarValue, result[0]);
                Assert.Equal(scalarValue, result[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0)]
    [InlineData(ulong.MinValue)]
    [InlineData(ulong.MaxValue)]
    public void BroadcastScalarToVector128_nuint_Pointer_64Bit(ulong scalarValue)
    {
        Span<nuint> source = stackalloc nuint[2];
        source[0] = (nuint)scalarValue;
        source[1] = 0;

        unsafe
        {
            fixed (nuint* ptr = &source[0])
            {
                Vector128<nuint> result = Avx2.BroadcastScalarToVector128(ptr);
                Assert.Equal(scalarValue, result[0]);
                Assert.Equal(scalarValue, result[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0)]
    [InlineData(uint.MinValue)]
    [InlineData(uint.MaxValue)]
    public void BroadcastScalarToVector128_nuint_Pointer_32Bit(uint scalarValue)
    {
        Span<nuint> source = stackalloc nuint[2];
        source[0] = scalarValue;
        source[1] = 0;

        unsafe
        {
            fixed (nuint* ptr = &source[0])
            {
                Vector128<nuint> result = Avx2.BroadcastScalarToVector128(ptr);
                Assert.Equal(scalarValue, result[0]);
                Assert.Equal(scalarValue, result[1]);
            }
        }
    }
}
