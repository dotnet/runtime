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

    [StructLayout(LayoutKind.Explicit)] 
    struct X_explicit
    {
        [FieldOffset(0)]
        byte x;
        [FieldOffset(1)]
        BigArray a;
    }

    [StructLayout(LayoutKind.Explicit)] 
    struct X_non_blittable
    {
        [FieldOffset(0)]
        bool x;
        [FieldOffset(1)]
        BigArray a;
    }

    struct Y
    {
        BigArray a;
        byte y;
    }

    [StructLayout(LayoutKind.Explicit)]
    struct Y_explicit
    {
        [FieldOffset(0)]
        BigArray b;
        [FieldOffset(int.MaxValue)]
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
        if (Environment.Is64BitProcess)
        {
            // Explicit struct of big size triggers out of memory error instead of type load exception
            Assert.Throws<TypeLoadException>(() => sizeof(X_explicit));
            Assert.Throws<TypeLoadException>(() => sizeof(X_non_blittable));
            Assert.Throws<TypeLoadException>(() => sizeof(Y_explicit));
        }
    }
}
