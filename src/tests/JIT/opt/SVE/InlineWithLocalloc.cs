// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.Arm;
using Xunit;

public class InlineWithLocalloc
{
    [Fact]
    public static void TestEntryPoint()
    {
        if (Sve.IsSupported)
        {
            Assert.Equal(42, WithStackBuffer(100));
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static int WithStackBuffer(int length)
    {
        Span<int> buffer = stackalloc int[length];
        buffer.Fill(21);
        return DoubleWithVector(buffer[length - 1]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int DoubleWithVector(int value)
    {
        Vector<int> vector = new Vector<int>(value);
        return (vector + vector)[0];
    }
}
