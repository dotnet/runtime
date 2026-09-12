// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_133678
{
    [Fact]
    public static unsafe void TestEntryPoint()
    {
        Assert.Throws<NullReferenceException>(() => Test(ref Unsafe.AsRef<byte>((void*)1)));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static byte Read(ref byte r) => r;

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    private static void Test(ref byte p)
    {
        if (Unsafe.IsNullRef(ref p))
        {
            return;
        }

        ref byte q = ref Unsafe.Add(ref p, -1);
        // Two uses keep q in an IL local, exercising VN-based offset peeling.
        Read(ref q);
        Read(ref q);
    }
}
