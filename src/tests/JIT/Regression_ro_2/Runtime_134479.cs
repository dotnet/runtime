// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_134479
{
    [Theory]
    [InlineData(1, 8, 8u)]
    [InlineData(-1, -8, 65528u)]
    [InlineData(32767, -8, 65528u)]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Widening128(int value, int expectedSigned, uint expectedUnsigned)
    {
        Vector128<short> signed = Vector128.Create((short)value);
        Vector128<ushort> unsigned = signed.AsUInt16();
        Assert.Equal(Vector128.Create(expectedSigned), Vector128.Create((int)Vector128.Dot(signed, Vector128<short>.One)));
        Assert.Equal(Vector128.Create(expectedUnsigned), Vector128.Create((uint)Vector128.Dot(unsigned, Vector128<ushort>.One)));
    }

    [Theory]
    [InlineData(1, 8, 8u)]
    [InlineData(-1, -8, 248u)]
    [InlineData(127, -8, 248u)]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Widening64(int value, int expectedSigned, uint expectedUnsigned)
    {
        Vector64<sbyte> signed = Vector64.Create((sbyte)value);
        Vector64<byte> unsigned = signed.AsByte();
        Assert.Equal(Vector64.Create(expectedSigned), Vector64.Create((int)Vector64.Dot(signed, Vector64<sbyte>.One)));
        Assert.Equal(Vector64.Create(expectedUnsigned), Vector64.Create((uint)Vector64.Dot(unsigned, Vector64<byte>.One)));
    }

    [Theory]
    [InlineData(1, 4, 4u)]
    [InlineData(-1, -4, 4294967292u)]
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CompatibleBroadcasts(int value, int expectedSigned, uint expectedUnsigned)
    {
        Vector128<int> vector = Vector128.Create(value);
        Assert.Equal(Vector128.Create(expectedSigned), Vector128.Create(Vector128.Dot(vector, Vector128<int>.One)));
        Assert.Equal(Vector128.Create(expectedUnsigned), Vector128.Create(unchecked((uint)Vector128.Dot(vector, Vector128<int>.One))));
    }
}
