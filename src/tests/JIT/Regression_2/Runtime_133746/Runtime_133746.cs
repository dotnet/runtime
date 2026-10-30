// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Xunit;

public class Runtime_133746
{
    [Fact]
    [SkipLocalsInit]
    public static void TestEntryPoint()
    {
        Vector2 pair = new Vector2(1.0f, -2.0f);
        Unsafe.As<Vector2, sbyte>(ref pair) = -1;
        ValidatePair(in pair);

        Validate(Vector64.CreateScalar((sbyte)-1), (sbyte)-1);
        Validate(Vector64.CreateScalar(sbyte.MinValue), sbyte.MinValue);
        Validate(Vector64.CreateScalar(sbyte.MaxValue), sbyte.MaxValue);
        Validate(Vector64.CreateScalar((short)-1), (short)-1);
        Validate(Vector64.CreateScalar(short.MinValue), short.MinValue);
        Validate(Vector64.CreateScalar(short.MaxValue), short.MaxValue);
        Validate(Vector64.CreateScalar(-1), -1);
        Validate(Vector64.CreateScalar(int.MinValue), int.MinValue);
        Validate(Vector64.CreateScalar(int.MaxValue), int.MaxValue);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ValidatePair(in Vector2 value)
    {
        int expected = BitConverter.IsLittleEndian ? 0x3F8000FF : unchecked((int)0xFF800000);
        Assert.Equal(expected, BitConverter.SingleToInt32Bits(value.X));
        Assert.Equal(-2.0f, value.Y);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Validate<T>(in Vector64<T> value, T expected) where T : struct
    {
        Assert.Equal(expected, value.GetElement(0));

        for (int i = 1; i < Vector64<T>.Count; i++)
        {
            Assert.Equal(default(T), value.GetElement(i));
        }
    }
}
