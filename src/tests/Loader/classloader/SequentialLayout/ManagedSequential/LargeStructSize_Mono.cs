// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public unsafe class LargeStructSize
{
    struct X_64
    {
        byte x;
        BigArray_64_1 a;
    }

    [StructLayout(LayoutKind.Explicit)] 
    struct X_explicit_64
    {
        [FieldOffset(0)]
        bool x;
        [FieldOffset(1)]
        BigArray_64_1 a;
    }

    struct Y_64
    {
        BigArray_64_1 a;
        byte y;
    }

    struct X_32
    {
        byte x;
        BigArray_32_1 a;
    }

    [StructLayout(LayoutKind.Explicit)] 
    struct X_explicit_32
    {
        [FieldOffset(0)]
        bool x;
        [FieldOffset(1)]
        BigArray_32_1 a;
    }

    struct Y_32
    {
        BigArray_32_1 a;
        byte y;
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue - 16)]
    struct BigArray_64_1
    {
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue - 16 - 1)]
    struct BigArray_64_2
    {
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue - 8)]
    struct BigArray_32_1
    {
    }

    [StructLayout(LayoutKind.Sequential, Size = int.MaxValue - 8 - 1)]
    struct BigArray_32_2
    {
    }

    [Fact]
    public static void TestLargeStructSize()
    {
        if (Environment.Is64BitProcess)
        {
            Assert.Equal(int.MaxValue - (IntPtr.Size * 2), sizeof(BigArray_64_1));
            Assert.Throws<TypeLoadException>(() => sizeof(BigArray_64_2));
            Assert.Throws<TypeLoadException>(() => sizeof(X_64));
            Assert.Throws<TypeLoadException>(() => sizeof(X_explicit_64));
            Assert.Throws<TypeLoadException>(() => sizeof(Y_64));
        }
        else
        {
            Assert.Equal(int.MaxValue - (IntPtr.Size * 2), sizeof(BigArray_32_1));
            Assert.Throws<TypeLoadException>(() => sizeof(BigArray_32_2));
            Assert.Throws<TypeLoadException>(() => sizeof(X_32));
            Assert.Throws<TypeLoadException>(() => sizeof(X_explicit_32));
            Assert.Throws<TypeLoadException>(() => sizeof(Y_32));
        }
    }
}
