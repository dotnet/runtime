// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class AvxTests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    [InlineData(-4444, -3333, 2222, -1111)]
    public void LoadDquVector256_nint_64Bit(long data1, long data2, long data3, long data4)
    {
        nint nativeData1 = (nint)data1;
        nint nativeData2 = (nint)data2;
        nint nativeData3 = (nint)data3;
        nint nativeData4 = (nint)data4;

        Span<nint> span = stackalloc nint[4];
        span[0] = nativeData1;
        span[1] = nativeData2;
        span[2] = nativeData3;
        span[3] = nativeData4;

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector256<nint> actualVector = Avx.LoadDquVector256(ptr);
                Assert.Equal(nativeData1, actualVector[0]);
                Assert.Equal(nativeData2, actualVector[1]);
                Assert.Equal(nativeData3, actualVector[2]);
                Assert.Equal(nativeData4, actualVector[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    [InlineData(-4444, -3333, 2222, -1111)]
    public void LoadDquVector256_nint_32Bit(int data1, int data2, int data3, int data4)
    {
        nint nativeData1 = data1;
        nint nativeData2 = data2;
        nint nativeData3 = data3;
        nint nativeData4 = data4;

        Span<nint> span = stackalloc nint[4];
        span[0] = nativeData1;
        span[1] = nativeData2;
        span[2] = nativeData3;
        span[3] = nativeData4;

        unsafe
        {
            fixed (nint* ptr = &span[0])
            {
                Vector256<nint> actualVector = Avx.LoadDquVector256(ptr);
                Assert.Equal(nativeData1, actualVector[0]);
                Assert.Equal(nativeData2, actualVector[1]);
                Assert.Equal(nativeData3, actualVector[2]);
                Assert.Equal(nativeData4, actualVector[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    public void LoadDquVector256_nuint_64Bit(ulong data1, ulong data2, ulong data3, ulong data4)
    {
        nuint nativeData1 = (nuint)data1;
        nuint nativeData2 = (nuint)data2;
        nuint nativeData3 = (nuint)data3;
        nuint nativeData4 = (nuint)data4;

        Span<nuint> span = stackalloc nuint[4];
        span[0] = nativeData1;
        span[1] = nativeData2;
        span[2] = nativeData3;
        span[3] = nativeData4;

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector256<nuint> actualVector = Avx.LoadDquVector256(ptr);
                Assert.Equal(nativeData1, actualVector[0]);
                Assert.Equal(nativeData2, actualVector[1]);
                Assert.Equal(nativeData3, actualVector[2]);
                Assert.Equal(nativeData4, actualVector[3]);
            }
        }
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0)]
    [InlineData(1111, 2222, 3333, 4444)]
    public void LoadDquVector256_nuint_32Bit(uint data1, uint data2, uint data3, uint data4)
    {
        nuint nativeData1 = data1;
        nuint nativeData2 = data2;
        nuint nativeData3 = data3;
        nuint nativeData4 = data4;

        Span<nuint> span = stackalloc nuint[4];
        span[0] = nativeData1;
        span[1] = nativeData2;
        span[2] = nativeData3;
        span[3] = nativeData4;

        unsafe
        {
            fixed (nuint* ptr = &span[0])
            {
                Vector256<nuint> actualVector = Avx.LoadDquVector256(ptr);
                Assert.Equal(nativeData1, actualVector[0]);
                Assert.Equal(nativeData2, actualVector[1]);
                Assert.Equal(nativeData3, actualVector[2]);
                Assert.Equal(nativeData4, actualVector[3]);
            }
        }
    }
}
