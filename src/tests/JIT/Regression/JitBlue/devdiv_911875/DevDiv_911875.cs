// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Xunit;

public class Repro
{
    public float x;
    public float y;

    [Fact]
    public static int TestEntryPoint()
    {
        byte[] buf = new byte[8];
        WriteFloat(buf, 0, 123.0F);
        WriteFloat(buf, 4, 456.0F);
        Repro pt = new Repro();
        Test(pt, buf);

        if (Convert.ToInt32(pt.x) != 123 || Convert.ToInt32(pt.y) != 456)
        {
            Console.WriteLine("FAIL!");
            Console.WriteLine("Unexpected values for pt.x and pt.y.");
            Console.WriteLine(pt.x);
            Console.WriteLine(pt.y);
            return 101;
        }

        Console.WriteLine("PASS!");
        return 100;
    }

    internal static void Test(object obj, byte[] buf)
    {
        ((Repro)obj).x = ReadFloat(buf, 0);
        ((Repro)obj).y = ReadFloat(buf, 4);
    }

    public static int ReadInt(byte[] buf, int offset)
    {
        return (buf[offset + 0] << 24)
             + (buf[offset + 1] << 16)
             + (buf[offset + 2] << 8)
             + (buf[offset + 3] << 0);
    }

    public static float ReadFloat(byte[] buf, int offset)
    {
        return IntBitsToFloat(ReadInt(buf, offset));
    }

    internal static void WriteInt(byte[] buf, int offset, int val)
    {
        buf[offset + 3] = (byte)(val);
        buf[offset + 2] = (byte)(val >> 8);
        buf[offset + 1] = (byte)(val >> 16);
        buf[offset] = (byte)(val >> 24);
    }

    internal static void WriteFloat(byte[] buf, int offset, float value)
    {
        WriteInt(buf, offset, FloatToRawIntBits(value));
    }

    public static float IntBitsToFloat(int value)
    {
        FloatConverter converter = new FloatConverter();
        return FloatConverter.ToFloat(value, ref converter);
    }

    public static int FloatToRawIntBits(float f)
    {
        FloatConverter converter = new FloatConverter();
        return FloatConverter.ToInt(f, ref converter);
    }
}

[StructLayout(LayoutKind.Explicit)]
public struct FloatConverter
{
    [FieldOffset(0)]
    private float _f;
    [FieldOffset(0)]
    private int _i;

    public static int ToInt(float value, ref FloatConverter converter)
    {
        converter._f = value;
        return converter._i;
    }

    public static float ToFloat(int value, ref FloatConverter converter)
    {
        converter._i = value;
        return converter._f;
    }
}

