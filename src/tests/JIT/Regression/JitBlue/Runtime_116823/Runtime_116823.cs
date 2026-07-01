// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Regression test for https://github.com/dotnet/runtime/issues/116823
//
// Unchecked conversions from float/double to a small integral type
// (sbyte, byte, short, ushort, char) must saturate when the source value
// is outside the destination type's range, consistent with the saturating
// float/double -> int conversions introduced in .NET 9.

namespace Runtime_116823;

using System.Runtime.CompilerServices;
using Xunit;

public class Runtime_116823
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static short DoubleToShort(double v) => (short)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ushort DoubleToUShort(double v) => (ushort)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static sbyte DoubleToSByte(double v) => (sbyte)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte DoubleToByte(double v) => (byte)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static char DoubleToChar(double v) => (char)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static short FloatToShort(float v) => (short)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static ushort FloatToUShort(float v) => (ushort)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static sbyte FloatToSByte(float v) => (sbyte)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static byte FloatToByte(float v) => (byte)v;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static char FloatToChar(float v) => (char)v;

    [Fact]
    // Mono has not yet been updated to saturate float/double -> small integral conversions;
    // tracked by https://github.com/dotnet/runtime/issues/116823.
    [SkipOnMono("https://github.com/dotnet/runtime/issues/116823", TestPlatforms.Any)]
    public static void TestEntryPoint()
    {
        // ---------------- double -> short ----------------
        // Just-out-of-range positive: 32768.000000000007 must saturate to short.MaxValue (32767).
        Assert.Equal((short)32767, DoubleToShort(32768.000000000007));
        Assert.Equal((short)32767, DoubleToShort(32768.0));
        Assert.Equal((short)32767, DoubleToShort(1e30));
        Assert.Equal((short)32767, DoubleToShort(double.PositiveInfinity));
        // Just-out-of-range negative.
        Assert.Equal((short)-32768, DoubleToShort(-32769.0));
        Assert.Equal((short)-32768, DoubleToShort(-1e30));
        Assert.Equal((short)-32768, DoubleToShort(double.NegativeInfinity));
        // NaN -> 0.
        Assert.Equal((short)0, DoubleToShort(double.NaN));
        // In-range values are unaffected.
        Assert.Equal((short)32767, DoubleToShort(32767.0));
        Assert.Equal((short)-32768, DoubleToShort(-32768.0));
        Assert.Equal((short)0, DoubleToShort(0.0));
        Assert.Equal((short)123, DoubleToShort(123.9));
        Assert.Equal((short)-123, DoubleToShort(-123.9));

        // ---------------- double -> ushort ----------------
        Assert.Equal((ushort)65535, DoubleToUShort(65536.0));
        Assert.Equal((ushort)65535, DoubleToUShort(1e30));
        Assert.Equal((ushort)65535, DoubleToUShort(double.PositiveInfinity));
        Assert.Equal((ushort)0, DoubleToUShort(-1.0));
        Assert.Equal((ushort)0, DoubleToUShort(-1e30));
        Assert.Equal((ushort)0, DoubleToUShort(double.NegativeInfinity));
        Assert.Equal((ushort)0, DoubleToUShort(double.NaN));
        Assert.Equal((ushort)65535, DoubleToUShort(65535.0));
        Assert.Equal((ushort)0, DoubleToUShort(0.0));

        // ---------------- double -> sbyte ----------------
        Assert.Equal((sbyte)127, DoubleToSByte(128.0));
        Assert.Equal((sbyte)127, DoubleToSByte(double.PositiveInfinity));
        Assert.Equal((sbyte)-128, DoubleToSByte(-129.0));
        Assert.Equal((sbyte)-128, DoubleToSByte(double.NegativeInfinity));
        Assert.Equal((sbyte)0, DoubleToSByte(double.NaN));
        Assert.Equal((sbyte)127, DoubleToSByte(127.0));
        Assert.Equal((sbyte)-128, DoubleToSByte(-128.0));

        // ---------------- double -> byte ----------------
        Assert.Equal((byte)255, DoubleToByte(256.0));
        Assert.Equal((byte)255, DoubleToByte(double.PositiveInfinity));
        Assert.Equal((byte)0, DoubleToByte(-1.0));
        Assert.Equal((byte)0, DoubleToByte(double.NegativeInfinity));
        Assert.Equal((byte)0, DoubleToByte(double.NaN));
        Assert.Equal((byte)255, DoubleToByte(255.0));
        Assert.Equal((byte)0, DoubleToByte(0.0));

        // ---------------- double -> char ----------------
        Assert.Equal((char)65535, DoubleToChar(65536.0));
        Assert.Equal((char)65535, DoubleToChar(double.PositiveInfinity));
        Assert.Equal((char)0, DoubleToChar(-1.0));
        Assert.Equal((char)0, DoubleToChar(double.NegativeInfinity));
        Assert.Equal((char)0, DoubleToChar(double.NaN));

        // ---------------- float -> short ----------------
        Assert.Equal((short)32767, FloatToShort(40000f));
        Assert.Equal((short)32767, FloatToShort(float.PositiveInfinity));
        Assert.Equal((short)-32768, FloatToShort(-40000f));
        Assert.Equal((short)-32768, FloatToShort(float.NegativeInfinity));
        Assert.Equal((short)0, FloatToShort(float.NaN));
        Assert.Equal((short)123, FloatToShort(123.9f));

        // ---------------- float -> ushort ----------------
        Assert.Equal((ushort)65535, FloatToUShort(70000f));
        Assert.Equal((ushort)65535, FloatToUShort(float.PositiveInfinity));
        Assert.Equal((ushort)0, FloatToUShort(-1f));
        Assert.Equal((ushort)0, FloatToUShort(float.NegativeInfinity));
        Assert.Equal((ushort)0, FloatToUShort(float.NaN));

        // ---------------- float -> sbyte ----------------
        Assert.Equal((sbyte)127, FloatToSByte(200f));
        Assert.Equal((sbyte)127, FloatToSByte(float.PositiveInfinity));
        Assert.Equal((sbyte)-128, FloatToSByte(-200f));
        Assert.Equal((sbyte)-128, FloatToSByte(float.NegativeInfinity));
        Assert.Equal((sbyte)0, FloatToSByte(float.NaN));

        // ---------------- float -> byte ----------------
        Assert.Equal((byte)255, FloatToByte(300f));
        Assert.Equal((byte)255, FloatToByte(float.PositiveInfinity));
        Assert.Equal((byte)0, FloatToByte(-1f));
        Assert.Equal((byte)0, FloatToByte(float.NegativeInfinity));
        Assert.Equal((byte)0, FloatToByte(float.NaN));

        // ---------------- float -> char ----------------
        Assert.Equal((char)65535, FloatToChar(70000f));
        Assert.Equal((char)65535, FloatToChar(float.PositiveInfinity));
        Assert.Equal((char)0, FloatToChar(-1f));
        Assert.Equal((char)0, FloatToChar(float.NegativeInfinity));
        Assert.Equal((char)0, FloatToChar(float.NaN));
    }
}
