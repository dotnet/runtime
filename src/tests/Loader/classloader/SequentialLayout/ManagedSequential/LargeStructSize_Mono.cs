// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[SkipOnCoreClr("This test suite tests Mono-specific layout rules.")]
public unsafe class LargeStructSize
{
    struct X
    {
        byte x;
        BigArray1 a;
    }

    struct Y
    {
        BigArray1 a;
        byte y;
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue - 16)]
    struct BigArray1
    {
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue - 15)]
    struct BigArray2
    {
    }

    [Fact]
    public static void TestLargeStructSize()
    {
        Assert.Equal(int.MaxValue - 16, sizeof(BigArray1));
        Assert.Throws<TypeLoadException>(() => sizeof(BigArray2));
        Assert.Throws<TypeLoadException>(() => sizeof(X));
        Assert.Throws<TypeLoadException>(() => sizeof(Y));
    }
}
