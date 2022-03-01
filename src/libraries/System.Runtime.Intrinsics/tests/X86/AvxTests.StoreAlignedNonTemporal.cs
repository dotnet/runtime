// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class AvxTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    [InlineData(-4444, -3333, 2222, -1111)]
    public void StoreAlignedNonTemporal_nint_64Bit(long data1, long data2, long data3, long data4)
    {
        unsafe
        {
            nint nativeData1 = (nint)data1;
            nint nativeData2 = (nint)data2;
            nint nativeData3 = (nint)data3;
            nint nativeData4 = (nint)data4;

            Vector256<nint> source = Vector256.Create(data1, data2, data3, data4).AsNInt();
            Span<nint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nint) * 4), 32), 4);

            fixed (nint* ptr = &span[0])
            {
                Avx.StoreAlignedNonTemporal(ptr, source);
                Assert.Equal(nativeData1, span[0]);
                Assert.Equal(nativeData2, span[1]);
                Assert.Equal(nativeData3, span[2]);
                Assert.Equal(nativeData4, span[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    [InlineData(-4444, -3333, 2222, -1111)]
    public void StoreAlignedNonTemporal_nint_32Bit(int data1, int data2, int data3, int data4)
    {
        unsafe
        {
            nint nativeData1 = data1;
            nint nativeData2 = data2;
            nint nativeData3 = data3;
            nint nativeData4 = data4;

            Vector256<nint> source = Vector256.Create(data1, data2, data3, data4).AsNInt();
            Span<nint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nint) * 4), 32), 4);

            fixed (nint* ptr = &span[0])
            {
                Avx.StoreAlignedNonTemporal(ptr, source);
                Assert.Equal(nativeData1, span[0]);
                Assert.Equal(nativeData2, span[1]);
                Assert.Equal(nativeData3, span[2]);
                Assert.Equal(nativeData4, span[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    public void StoreAlignedNonTemporal_nuint_64Bit(ulong data1, ulong data2, ulong data3, ulong data4)
    {
        unsafe
        {
            nuint nativeData1 = (nuint)data1;
            nuint nativeData2 = (nuint)data2;
            nuint nativeData3 = (nuint)data3;
            nuint nativeData4 = (nuint)data4;

            Vector256<nuint> source = Vector256.Create(data1, data2, data3, data4).AsNUInt();
            Span<nuint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nuint) * 4), 32), 4);

            fixed (nuint* ptr = &span[0])
            {
                Avx.StoreAlignedNonTemporal(ptr, source);
                Assert.Equal(nativeData1, span[0]);
                Assert.Equal(nativeData2, span[1]);
                Assert.Equal(nativeData3, span[2]);
                Assert.Equal(nativeData4, span[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    public void StoreAlignedNonTemporal_nuint_32Bit(uint data1, uint data2, uint data3, uint data4)
    {
        unsafe
        {
            nuint nativeData1 = data1;
            nuint nativeData2 = data2;
            nuint nativeData3 = data3;
            nuint nativeData4 = data4;

            Vector256<nuint> source = Vector256.Create(data1, data2, data3, data4).AsNUInt();
            Span<nuint> span =
                new(NativeMemory.AlignedAlloc((nuint)(sizeof(nuint) * 4), 32), 4);

            fixed (nuint* ptr = &span[0])
            {
                Avx.StoreAlignedNonTemporal(ptr, source);
                Assert.Equal(nativeData1, span[0]);
                Assert.Equal(nativeData2, span[1]);
                Assert.Equal(nativeData3, span[2]);
                Assert.Equal(nativeData4, span[3]);
            }
        }
    }
}
