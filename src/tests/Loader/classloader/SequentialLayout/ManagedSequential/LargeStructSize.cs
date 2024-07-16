// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

[SkipOnMono("This test suite tests CoreCLR and Crossgen2/NativeAOT-specific layout rules.")]
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

    [StructLayout(LayoutKind.Sequential, Size = (((1<<27)-1)-6))] // FIELD_OFFSET_LAST_REAL_OFFSET is (((1<<27)-1)-6)
    struct BigArray1
    {
    }

    [StructLayout(LayoutKind.Sequential, Size = (((1<<27)-1)-6+1))]
    struct BigArray2
    {
    }

    [Fact]
    public static void TestLargeStructSize()
    {
        Assert.Equal((((1<<27)-1)-6), sizeof(BigArray1));
        Assert.Throws<TypeLoadException>(() => sizeof(BigArray2));
        Assert.Throws<TypeLoadException>(() => sizeof(X));
        Assert.Throws<TypeLoadException>(() => sizeof(Y));
    }
}
