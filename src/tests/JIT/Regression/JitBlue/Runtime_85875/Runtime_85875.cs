// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public unsafe class Runtime_85874
{
    [Fact]
    public static void Optimize128()
    {
        const double value = 5.0;
        double* ptr = (double*)NativeMemory.AlignedAlloc(16, 16);
        *ptr = value;
        Move128(ptr);
        Assert.Equals(value, *ptr);
    }

    [Fact]
    public static void Optimize256()
    {
        const double value = 5.0;
        double* ptr = (double*)NativeMemory.AlignedAlloc(32, 32);
        *ptr = value;
        Move256(ptr);
        Assert.Equals(value, *ptr);
    }

    [Fact]
    public static void Optimize512()
    {
        const double value = 5.0;
        double* ptr = (double*)NativeMemory.AlignedAlloc(64, 64);
        *ptr = value;
        Move512(ptr);
        Assert.Equals(value, *ptr);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Move128(double* p)
    {
        Vector128.Store(Vector128.LoadAligned((byte*)p).AsDouble(), p);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Move256(double* p)
    {
        Vector256.Store(Vector256.LoadAligned((byte*)p).AsDouble(), p);
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Move512(double* p)
    {
        Vector512.Store(Vector512.LoadAligned((byte*)p).AsDouble(), p);
    }
}
