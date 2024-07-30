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
        BigArray a;
    }

    struct Y
    {
        BigArray a;
        byte y;
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue)]
    struct BigArray
    {
    }

    [Fact]
    public static void TestLargeStructSize()
    {
        Assert.Equal(int.MaxValue, sizeof(BigArray));
        Assert.Throws<TypeLoadException>(() => sizeof(X));
        Assert.Throws<TypeLoadException>(() => sizeof(Y));
    }
}
