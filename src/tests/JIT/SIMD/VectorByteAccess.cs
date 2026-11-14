// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using Xunit;

namespace SIMDTests;

public class VectorByteAccess
{
    [Fact]
    public static void LocalByteAccess()
    {
        Assert.Equal(42, ReadFirstByte());
        Vector<byte> value = WriteFirstByte();
        Assert.Equal(7, value[0]);
        for (int i = 1; i < Vector<byte>.Count; i++)
        {
            Assert.Equal(42, value[i]);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte ReadFirstByte()
    {
        Vector<byte> value = CreateVector();
        return Unsafe.As<Vector<byte>, byte>(ref value);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<byte> WriteFirstByte()
    {
        Vector<byte> value = CreateVector();
        Unsafe.As<Vector<byte>, byte>(ref value) = 7;
        return value;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static Vector<byte> CreateVector() => new Vector<byte>(42);
}
