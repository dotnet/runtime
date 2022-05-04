// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse2Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(long.MinValue, long.MaxValue)]
    [InlineData(long.MaxValue, long.MinValue)]
    public void StoreAlignedNonTemporal_nint_64Bit(long lower, long upper)
    {
        Vector128<nint> sourceVector = Vector128.Create(lower, upper).AsNInt();

        unsafe
        {
            Span<nint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nint) * 2), 32), 4);

            fixed (nint* ptr = &span[0])
            {
                Sse2.StoreAlignedNonTemporal(ptr, sourceVector);
                Assert.Equal(lower, span[0]);
                Assert.Equal(upper, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(int.MinValue, int.MaxValue)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void StoreAlignedNonTemporal_nint_32Bit(int lower, int upper)
    {
        Vector128<nint> sourceVector = Vector128.Create(lower, upper).AsNInt();

        unsafe
        {
            Span<nint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nint) * 2), 32), 4);

            fixed (nint* ptr = &span[0])
            {
                Sse2.StoreAlignedNonTemporal(ptr, sourceVector);
                Assert.Equal(lower, span[0]);
                Assert.Equal(upper, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(ulong.MaxValue, 0)]
    public void StoreAlignedNonTemporal_nuint_64Bit(ulong lower, ulong upper)
    {
        Vector128<nuint> sourceVector = Vector128.Create(lower, upper).AsNUInt();

        unsafe
        {
            Span<nuint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nuint) * 2), 32), 4);

            fixed (nuint* ptr = &span[0])
            {
                Sse2.StoreAlignedNonTemporal(ptr, sourceVector);
                Assert.Equal(lower, span[0]);
                Assert.Equal(upper, span[1]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0)]
    [InlineData(12345678, 87654321)]
    [InlineData(uint.MaxValue, 0)]
    public void StoreAlignedNonTemporal_nuint_32Bit(uint lower, uint upper)
    {
        Vector128<nuint> sourceVector = Vector128.Create(lower, upper).AsNUInt();

        unsafe
        {
            Span<nuint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nuint) * 2), 32), 4);

            fixed (nuint* ptr = &span[0])
            {
                Sse2.StoreAlignedNonTemporal(ptr, sourceVector);
                Assert.Equal(lower, span[0]);
                Assert.Equal(upper, span[1]);
            }
        }
    }
}
