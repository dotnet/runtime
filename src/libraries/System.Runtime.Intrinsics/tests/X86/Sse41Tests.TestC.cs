// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Intrinsics.X86;
using Xunit;

namespace System.Runtime.Intrinsics.Tests.X86;

public sealed partial class Sse41Tests
{
    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 1, 0, false)]
    [InlineData(1, 0, 1, 0, true)]
    [InlineData(123, 0, 123, 0, true)]
    [InlineData(123, 0, 321, 0, false)]
    public void TestC_nint_64Bit(long lowerLeft, long upperLeft, long lowerRight, long upperRight, bool expected)
    {
        Vector128<nint> leftVector = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> rightVector = Vector128.Create(lowerRight, upperRight).AsNInt();

        bool actual = Sse41.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 1, 0, false)]
    [InlineData(1, 0, 1, 0, true)]
    [InlineData(123, 0, 123, 0, true)]
    [InlineData(123, 0, 321, 0, false)]
    public void TestC_nint_32Bit(int lowerLeft, int upperLeft, int lowerRight, int upperRight, bool expected)
    {
        Vector128<nint> leftVector = Vector128.Create(lowerLeft, upperLeft).AsNInt();
        Vector128<nint> rightVector = Vector128.Create(lowerRight, upperRight).AsNInt();

        bool actual = Sse41.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run64BitTests))]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 1, 0, false)]
    [InlineData(1, 0, 1, 0, true)]
    [InlineData(123, 0, 123, 0, true)]
    [InlineData(123, 0, 321, 0, false)]
    public void TestC_nuint_64Bit(ulong lowerLeft, ulong upperLeft, ulong lowerRight, ulong upperRight, bool expected)
    {
        Vector128<nuint> leftVector = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> rightVector = Vector128.Create(lowerRight, upperRight).AsNUInt();

        bool actual = Sse41.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }

    [ConditionalTheory(nameof(Run32BitTests))]
    [InlineData(0, 0, 0, 0, true)]
    [InlineData(0, 0, 1, 0, false)]
    [InlineData(1, 0, 1, 0, true)]
    [InlineData(123, 0, 123, 0, true)]
    [InlineData(123, 0, 321, 0, false)]
    public void TestC_nuint_32Bit(uint lowerLeft, uint upperLeft, uint lowerRight, uint upperRight, bool expected)
    {
        Vector128<nuint> leftVector = Vector128.Create(lowerLeft, upperLeft).AsNUInt();
        Vector128<nuint> rightVector = Vector128.Create(lowerRight, upperRight).AsNUInt();

        bool actual = Sse41.TestC(leftVector, rightVector);
        Assert.Equal(expected, actual);
    }
}
