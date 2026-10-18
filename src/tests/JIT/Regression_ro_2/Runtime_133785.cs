// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133785
{
    // On x64, the element offset is 16 + Index * 3 == int.MaxValue - 3.
    private const int Index = 715827876;

    private struct S3
    {
        public byte A, B, C;
    }

    [Fact]
    public static void TestEntryPoint()
    {
        S3[] array = new S3[1];
        S3 value = new S3 { A = 1, B = 2, C = 3 };

        Assert.Throws<IndexOutOfRangeException>(() => StoreInit(array));
        Assert.Throws<IndexOutOfRangeException>(() => StoreCopy(array, value));
        Assert.Throws<IndexOutOfRangeException>(() => LoadCopy(array, ref value));
    }

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreInit(S3[] array) => array[Index] = default;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void StoreCopy(S3[] array, S3 value) => array[Index] = value;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void LoadCopy(S3[] array, ref S3 value) => value = array[Index];
}
