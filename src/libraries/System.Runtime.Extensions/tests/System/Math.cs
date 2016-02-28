// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Tests
{
    public static class MathTests
    {
        [Fact]
        public static void Cos()
        {
            Assert.Equal(0.54030230586814, Math.Cos(1.0), 10);
            Assert.Equal(1.0, Math.Cos(0.0));
            Assert.Equal(0.54030230586814, Math.Cos(-1.0), 10);
            Assert.Equal(double.NaN, Math.Cos(double.NaN));
            Assert.Equal(double.NaN, Math.Cos(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Cos(double.NegativeInfinity));
        }

        [Fact]
        public static void Sin()
        {
            Assert.Equal(0.841470984807897, Math.Sin(1.0), 10);
            Assert.Equal(0.0, Math.Sin(0.0));
            Assert.Equal(-0.841470984807897, Math.Sin(-1.0), 10);
            Assert.Equal(double.NaN, Math.Sin(double.NaN));
            Assert.Equal(double.NaN, Math.Sin(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Sin(double.NegativeInfinity));
        }

        [Fact]
        public static void Tan()
        {
            Assert.Equal(1.5574077246549, Math.Tan(1.0), 10);
            Assert.Equal(0.0, Math.Tan(0.0));
            Assert.Equal(-1.5574077246549, Math.Tan(-1.0), 10);
            Assert.Equal(double.NaN, Math.Tan(double.NaN));
            Assert.Equal(double.NaN, Math.Tan(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Tan(double.NegativeInfinity));
        }

        [Fact]
        public static void Cosh()
        {
            Assert.Equal(1.54308063481524, Math.Cosh(1.0), 10);
            Assert.Equal(1.0, Math.Cosh(0.0));
            Assert.Equal(1.54308063481524, Math.Cosh(-1.0), 10);
            Assert.Equal(double.NaN, Math.Cosh(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Cosh(double.PositiveInfinity));
            Assert.Equal(double.PositiveInfinity, Math.Cosh(double.NegativeInfinity));
        }

        [Fact]
        public static void Sinh()
        {
            Assert.Equal(1.1752011936438, Math.Sinh(1.0), 10);
            Assert.Equal(0.0, Math.Sinh(0.0));
            Assert.Equal(-1.1752011936438, Math.Sinh(-1.0), 10);
            Assert.Equal(double.NaN, Math.Sinh(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Sinh(double.PositiveInfinity));
            Assert.Equal(double.NegativeInfinity, Math.Sinh(double.NegativeInfinity));
        }

        [Fact]
        public static void Tanh()
        {
            Assert.Equal(0.761594155955765, Math.Tanh(1.0), 10);
            Assert.Equal(0.0, Math.Tanh(0.0));
            Assert.Equal(-0.761594155955765, Math.Tanh(-1.0), 10);
            Assert.Equal(double.NaN, Math.Tanh(double.NaN));
            Assert.Equal(1.0, Math.Tanh(double.PositiveInfinity));
            Assert.Equal(-1.0, Math.Tanh(double.NegativeInfinity));
        }

        [Fact]
        public static void Acos()
        {
            Assert.Equal(0.0, Math.Acos(1.0));
            Assert.Equal(1.5707963267949, Math.Acos(0.0), 10);
            Assert.Equal(3.14159265358979, Math.Acos(-1.0), 10);
            Assert.Equal(double.NaN, Math.Acos(double.NaN));
            Assert.Equal(double.NaN, Math.Acos(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Acos(double.NegativeInfinity));
        }

        [Fact]
        public static void Asin()
        {
            Assert.Equal(1.5707963267949, Math.Asin(1.0), 10);
            Assert.Equal(0.0, Math.Asin(0.0));
            Assert.Equal(-1.5707963267949, Math.Asin(-1.0), 10);
            Assert.Equal(double.NaN, Math.Asin(double.NaN));
            Assert.Equal(double.NaN, Math.Asin(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Asin(double.NegativeInfinity));
        }

        [Fact]
        public static void Atan()
        {
            Assert.Equal(0.785398163397448, Math.Atan(1.0), 10);
            Assert.Equal(0.0, Math.Atan(0.0));
            Assert.Equal(-0.785398163397448, Math.Atan(-1.0), 10);
            Assert.Equal(double.NaN, Math.Atan(double.NaN));
            Assert.Equal(1.5707963267949, Math.Atan(double.PositiveInfinity), 4);
            Assert.Equal(-1.5707963267949, Math.Atan(double.NegativeInfinity), 4);
        }

        [Fact]
        public static void Atan2()
        {
            Assert.Equal(0.0, Math.Atan2(0.0, 0.0));
            Assert.Equal(1.5707963267949, Math.Atan2(1.0, 0.0), 10);
            Assert.Equal(0.588002603547568, Math.Atan2(2.0, 3.0), 10);
            Assert.Equal(0.0, Math.Atan2(0.0, 3.0));
            Assert.Equal(-0.588002603547568, Math.Atan2(-2.0, 3.0), 10);
            Assert.Equal(double.NaN, Math.Atan2(double.NaN, 1.0));
            Assert.Equal(double.NaN, Math.Atan2(1.0, double.NaN));
            Assert.Equal(1.5707963267949, Math.Atan2(double.PositiveInfinity, 1.0), 10);
            Assert.Equal(-1.5707963267949, Math.Atan2(double.NegativeInfinity, 1.0), 10);
            Assert.Equal(0.0, Math.Atan2(1.0, double.PositiveInfinity));
            Assert.Equal(3.14159265358979, Math.Atan2(1.0, double.NegativeInfinity), 10);
        }

        [Fact]
        public static void Ceiling_Decimal()
        {
            Assert.Equal(2.0m, Math.Ceiling(1.1m));
            Assert.Equal(2.0m, Math.Ceiling(1.9m));
            Assert.Equal(-1.0m, Math.Ceiling(-1.1m));
        }

        [Fact]
        public static void Ceiling_Double()
        {
            Assert.Equal(2.0, Math.Ceiling(1.1));
            Assert.Equal(2.0, Math.Ceiling(1.9));
            Assert.Equal(-1.0, Math.Ceiling(-1.1));
            Assert.Equal(double.NaN, Math.Ceiling(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Ceiling(double.PositiveInfinity));
            Assert.Equal(double.NegativeInfinity, Math.Ceiling(double.NegativeInfinity));
        }

        [Fact]
        public static void Floor_Decimal()
        {
            Assert.Equal(1.0m, Math.Floor(1.1m));
            Assert.Equal(1.0m, Math.Floor(1.9m));
            Assert.Equal(-2.0m, Math.Floor(-1.1m));
        }

        [Fact]
        public static void Floor_Double()
        {
            Assert.Equal(1.0, Math.Floor(1.1));
            Assert.Equal(1.0, Math.Floor(1.9));
            Assert.Equal(-2.0, Math.Floor(-1.1));
            Assert.Equal(double.NaN, Math.Floor(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Floor(double.PositiveInfinity));
            Assert.Equal(double.NegativeInfinity, Math.Floor(double.NegativeInfinity));
        }

        [Fact]
        public static void Round_Decimal()
        {
            Assert.Equal(0.0m, Math.Round(0.0m));
            Assert.Equal(1.0m, Math.Round(1.4m));
            Assert.Equal(2.0m, Math.Round(1.5m));
            Assert.Equal(2e16m, Math.Round(2e16m));
            Assert.Equal(0.0m, Math.Round(-0.0m));
            Assert.Equal(-1.0m, Math.Round(-1.4m));
            Assert.Equal(-2.0m, Math.Round(-1.5m));
            Assert.Equal(-2e16m, Math.Round(-2e16m));
        }

        [Fact]
        public static void Round_Decimal_Digits()
        {
            Assert.Equal(3.422m, Math.Round(3.42156m, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(-3.422m, Math.Round(-3.42156m, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(Decimal.Zero, Math.Round(Decimal.Zero, 3, MidpointRounding.AwayFromZero));
        }

        [Fact]
        public static void Round_Double()
        {
            Assert.Equal(0.0, Math.Round(0.0));
            Assert.Equal(1.0, Math.Round(1.4));
            Assert.Equal(2.0, Math.Round(1.5));
            Assert.Equal(2e16, Math.Round(2e16));
            Assert.Equal(0.0, Math.Round(-0.0));
            Assert.Equal(-1.0, Math.Round(-1.4));
            Assert.Equal(-2.0, Math.Round(-1.5));
            Assert.Equal(-2e16, Math.Round(-2e16));
        }

        [Fact]
        public static void Round_Double_Digits()
        {
            Assert.Equal(3.422, Math.Round(3.42156, 3, MidpointRounding.AwayFromZero), 10);
            Assert.Equal(-3.422, Math.Round(-3.42156, 3, MidpointRounding.AwayFromZero), 10);
            Assert.Equal(0.0, Math.Round(0.0, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(double.NaN, Math.Round(double.NaN, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(double.PositiveInfinity, Math.Round(double.PositiveInfinity, 3, MidpointRounding.AwayFromZero));
            Assert.Equal(double.NegativeInfinity, Math.Round(double.NegativeInfinity, 3, MidpointRounding.AwayFromZero));
        }

        [Fact]
        public static void Sqrt()
        {
            Assert.Equal(1.73205080756888, Math.Sqrt(3.0), 10);
            Assert.Equal(0.0, Math.Sqrt(0.0));
            Assert.Equal(double.NaN, Math.Sqrt(-3.0));
            Assert.Equal(double.NaN, Math.Sqrt(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Sqrt(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Sqrt(double.NegativeInfinity));
        }

        [Fact]
        public static void Log()
        {
            Assert.Equal(1.09861228866811, Math.Log(3.0), 10);
            Assert.Equal(double.NegativeInfinity, Math.Log(0.0));
            Assert.Equal(double.NaN, Math.Log(-3.0));
            Assert.Equal(double.NaN, Math.Log(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Log(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Log(double.NegativeInfinity));
        }

        [Fact]
        public static void LogWithBase()
        {
            Assert.Equal(1.0, Math.Log(3.0, 3.0));
            Assert.Equal(2.40217350273, Math.Log(14, 3.0), 10);
            Assert.Equal(double.NegativeInfinity, Math.Log(0.0, 3.0));
            Assert.Equal(double.NaN, Math.Log(-3.0, 3.0));
            Assert.Equal(double.NaN, Math.Log(double.NaN, 3.0));
            Assert.Equal(double.PositiveInfinity, Math.Log(double.PositiveInfinity, 3.0));
            Assert.Equal(double.NaN, Math.Log(double.NegativeInfinity, 3.0));
        }

        [Fact]
        public static void Log10()
        {
            Assert.Equal(0.477121254719662, Math.Log10(3.0), 10);
            Assert.Equal(double.NegativeInfinity, Math.Log10(0.0));
            Assert.Equal(double.NaN, Math.Log10(-3.0));
            Assert.Equal(double.NaN, Math.Log10(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Log10(double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Log10(double.NegativeInfinity));
        }

        [Fact]
        public static void Pow()
        {
            Assert.Equal(1.0, Math.Pow(0.0, 0.0));
            Assert.Equal(1.0, Math.Pow(1.0, 0.0));
            Assert.Equal(8.0, Math.Pow(2.0, 3.0));
            Assert.Equal(0.0, Math.Pow(0.0, 3.0));
            Assert.Equal(-8.0, Math.Pow(-2.0, 3.0));
            Assert.Equal(double.NaN, Math.Pow(double.NaN, 1.0));
            Assert.Equal(double.NaN, Math.Pow(1.0, double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Pow(double.PositiveInfinity, 1.0));
            Assert.Equal(double.NegativeInfinity, Math.Pow(double.NegativeInfinity, 1.0));
            Assert.Equal(1.0, Math.Pow(1.0, double.PositiveInfinity));
            Assert.Equal(1.0, Math.Pow(1.0, double.NegativeInfinity));
            Assert.Equal(double.NaN, Math.Pow(-1.0, double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Pow(-1.0, double.NegativeInfinity));
            Assert.Equal(double.PositiveInfinity, Math.Pow(1.1, double.PositiveInfinity));
            Assert.Equal(0.0, Math.Pow(1.1, double.NegativeInfinity));
        }

        [Fact]
        public static void Abs_Decimal()
        {
            Assert.Equal(3.0m, Math.Abs(3.0m));
            Assert.Equal(0.0m, Math.Abs(0.0m));
            Assert.Equal(0.0m, Math.Abs(-0.0m));
            Assert.Equal(3.0m, Math.Abs(-3.0m));
            Assert.Equal(Decimal.MaxValue, Math.Abs(Decimal.MinValue));
        }

        [Fact]
        public static void Abs_Double()
        {
            Assert.Equal(3.0, Math.Abs(3.0));
            Assert.Equal(0.0, Math.Abs(0.0));
            Assert.Equal(3.0, Math.Abs(-3.0));
            Assert.Equal(double.NaN, Math.Abs(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Abs(double.PositiveInfinity));
            Assert.Equal(double.PositiveInfinity, Math.Abs(double.NegativeInfinity));
        }

        [Fact]
        public static void Abs_Short()
        {
            Assert.Equal((short)3, Math.Abs((short)3));
            Assert.Equal((short)0, Math.Abs((short)0));
            Assert.Equal((short)3, Math.Abs((short)(-3)));
            Assert.Throws<OverflowException>(() => Math.Abs(short.MinValue));
        }

        [Fact]
        public static void Abs_Int()
        {
            Assert.Equal(3, Math.Abs(3));
            Assert.Equal(0, Math.Abs(0));
            Assert.Equal(3, Math.Abs(-3));
            Assert.Throws<OverflowException>(() => Math.Abs(int.MinValue));
        }

        [Fact]
        public static void Abs_Long()
        {
            Assert.Equal(3L, Math.Abs(3L));
            Assert.Equal(0L, Math.Abs(0L));
            Assert.Equal(3L, Math.Abs(-3L));
            Assert.Throws<OverflowException>(() => Math.Abs(long.MinValue));
        }

        [Fact]
        public static void Abs_SByte()
        {
            Assert.Equal((sbyte)3, Math.Abs((sbyte)3));
            Assert.Equal((sbyte)0, Math.Abs((sbyte)0));
            Assert.Equal((sbyte)3, Math.Abs((sbyte)(-3)));
            Assert.Throws<OverflowException>(() => Math.Abs(sbyte.MinValue));
        }

        [Fact]
        public static void Abs_Float()
        {
            Assert.Equal(3.0, Math.Abs(3.0f));
            Assert.Equal(0.0, Math.Abs(0.0f));
            Assert.Equal(3.0, Math.Abs(-3.0f));
            Assert.Equal(float.NaN, Math.Abs(float.NaN));
            Assert.Equal(float.PositiveInfinity, Math.Abs(float.PositiveInfinity));
            Assert.Equal(float.PositiveInfinity, Math.Abs(float.NegativeInfinity));
        }

        [Fact]
        public static void Exp()
        {
            Assert.Equal(20.0855369231877, Math.Exp(3.0), 10);
            Assert.Equal(1.0, Math.Exp(0.0));
            Assert.Equal(0.0497870683678639, Math.Exp(-3.0), 10);
            Assert.Equal(double.NaN, Math.Exp(double.NaN));
            Assert.Equal(double.PositiveInfinity, Math.Exp(double.PositiveInfinity));
            Assert.Equal(0.0, Math.Exp(double.NegativeInfinity));
        }

        [Fact]
        public static void IEEERemainder()
        {
            Assert.Equal(-1.0, Math.IEEERemainder(3, 2));
            Assert.Equal(0.0, Math.IEEERemainder(4, 2));
            Assert.Equal(1.0, Math.IEEERemainder(10, 3));
            Assert.Equal(-1.0, Math.IEEERemainder(11, 3));
            Assert.Equal(-2.0, Math.IEEERemainder(28, 5));
            Assert.Equal(1.8, Math.IEEERemainder(17.8, 4), 10);
            Assert.Equal(1.4, Math.IEEERemainder(17.8, 4.1), 10);
            Assert.Equal(0.0999999999999979, Math.IEEERemainder(-16.3, 4.1), 10);
            Assert.Equal(1.4, Math.IEEERemainder(17.8, -4.1), 10);
            Assert.Equal(-1.4, Math.IEEERemainder(-17.8, -4.1), 10);
        }

        [Fact]
        public static void Min_Byte()
        {
            Assert.Equal((byte)2, Math.Min((byte)3, (byte)2));
            Assert.Equal(byte.MinValue, Math.Min(byte.MinValue, byte.MaxValue));
        }

        [Fact]
        public static void Min_Decimal()
        {
            Assert.Equal(-2.0m, Math.Min(3.0m, -2.0m));
            Assert.Equal(decimal.MinValue, Math.Min(decimal.MinValue, decimal.MaxValue));
        }

        [Fact]
        public static void Min_Double()
        {
            Assert.Equal(-2.0, Math.Min(3.0, -2.0));
            Assert.Equal(double.MinValue, Math.Min(double.MinValue, double.MaxValue));
            Assert.Equal(double.NegativeInfinity, Math.Min(double.NegativeInfinity, double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Min(double.NegativeInfinity, double.NaN));
            Assert.Equal(double.NaN, Math.Min(double.NaN, double.NaN));
        }

        [Fact]
        public static void Min_Short()
        {
            Assert.Equal((short)(-2), Math.Min((short)3, (short)(-2)));
            Assert.Equal(short.MinValue, Math.Min(short.MinValue, short.MaxValue));
        }

        [Fact]
        public static void Min_Int()
        {
            Assert.Equal(-2, Math.Min(3, -2));
            Assert.Equal(int.MinValue, Math.Min(int.MinValue, int.MaxValue));
        }

        [Fact]
        public static void Min_Long()
        {
            Assert.Equal(-2L, Math.Min(3L, -2L));
            Assert.Equal(long.MinValue, Math.Min(long.MinValue, long.MaxValue));
        }

        [Fact]
        public static void Min_SByte()
        {
            Assert.Equal((sbyte)(-2), Math.Min((sbyte)3, (sbyte)(-2)));
            Assert.Equal(sbyte.MinValue, Math.Min(sbyte.MinValue, sbyte.MaxValue));
        }

        [Fact]
        public static void Min_Float()
        {
            Assert.Equal(-2.0f, Math.Min(3.0f, -2.0f));
            Assert.Equal(float.MinValue, Math.Min(float.MinValue, float.MaxValue));
            Assert.Equal(float.NegativeInfinity, Math.Min(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, Math.Min(float.NegativeInfinity, float.NaN));
            Assert.Equal(float.NaN, Math.Min(float.NaN, float.NaN));
        }

        [Fact]
        public static void Min_UShort()
        {
            Assert.Equal((ushort)2, Math.Min((ushort)3, (ushort)2));
            Assert.Equal(ushort.MinValue, Math.Min(ushort.MinValue, ushort.MaxValue));
        }

        [Fact]
        public static void Min_UInt()
        {
            Assert.Equal((uint)2, Math.Min((uint)3, (uint)2));
            Assert.Equal(uint.MinValue, Math.Min(uint.MinValue, uint.MaxValue));
        }

        [Fact]
        public static void Min_ULong()
        {
            Assert.Equal((ulong)2, Math.Min((ulong)3, (ulong)2));
            Assert.Equal(ulong.MinValue, Math.Min(ulong.MinValue, ulong.MaxValue));
        }

        [Fact]
        public static void Max_Byte()
        {
            Assert.Equal((byte)3, Math.Max((byte)2, (byte)3));
            Assert.Equal(byte.MaxValue, Math.Max(byte.MinValue, byte.MaxValue));
        }

        [Fact]
        public static void Max_Decimal()
        {
            Assert.Equal(3.0m, Math.Max(-2.0m, 3.0m));
            Assert.Equal(decimal.MaxValue, Math.Max(decimal.MinValue, decimal.MaxValue));
        }

        [Fact]
        public static void Max_Double()
        {
            Assert.Equal(3.0, Math.Max(3.0, -2.0));
            Assert.Equal(double.MaxValue, Math.Max(double.MinValue, double.MaxValue));
            Assert.Equal(double.PositiveInfinity, Math.Max(double.NegativeInfinity, double.PositiveInfinity));
            Assert.Equal(double.NaN, Math.Max(double.PositiveInfinity, double.NaN));
            Assert.Equal(double.NaN, Math.Max(double.NaN, double.NaN));
        }

        [Fact]
        public static void Max_Short()
        {
            Assert.Equal((short)3, Math.Max((short)(-2), (short)3));
            Assert.Equal(short.MaxValue, Math.Max(short.MinValue, short.MaxValue));
        }

        [Fact]
        public static void Max_Int()
        {
            Assert.Equal(3, Math.Max(-2, 3));
            Assert.Equal(int.MaxValue, Math.Max(int.MinValue, int.MaxValue));
        }

        [Fact]
        public static void Max_Long()
        {
            Assert.Equal(3L, Math.Max(-2L, 3L));
            Assert.Equal(long.MaxValue, Math.Max(long.MinValue, long.MaxValue));
        }

        [Fact]
        public static void Max_SByte()
        {
            Assert.Equal((sbyte)3, Math.Max((sbyte)(-2), (sbyte)3));
            Assert.Equal(sbyte.MaxValue, Math.Max(sbyte.MinValue, sbyte.MaxValue));
        }

        [Fact]
        public static void Max_Float()
        {
            Assert.Equal(3.0f, Math.Max(3.0f, -2.0f));
            Assert.Equal(float.MaxValue, Math.Max(float.MinValue, float.MaxValue));
            Assert.Equal(float.PositiveInfinity, Math.Max(float.NegativeInfinity, float.PositiveInfinity));
            Assert.Equal(float.NaN, Math.Max(float.PositiveInfinity, float.NaN));
            Assert.Equal(float.NaN, Math.Max(float.NaN, float.NaN));
        }

        [Fact]
        public static void Max_UShort()
        {
            Assert.Equal((ushort)3, Math.Max((ushort)2, (ushort)3));
            Assert.Equal(ushort.MaxValue, Math.Max(ushort.MinValue, ushort.MaxValue));
        }

        [Fact]
        public static void Max_UInt()
        {
            Assert.Equal((uint)3, Math.Max((uint)2, (uint)3));
            Assert.Equal(uint.MaxValue, Math.Max(uint.MinValue, uint.MaxValue));
        }

        [Fact]
        public static void Max_ULong()
        {
            Assert.Equal((ulong)3, Math.Max((ulong)2, (ulong)3));
            Assert.Equal(ulong.MaxValue, Math.Max(ulong.MinValue, ulong.MaxValue));
        }

        [Fact]
        public static void Sign_Decimal()
        {
            Assert.Equal(0, Math.Sign(0.0m));
            Assert.Equal(0, Math.Sign(-0.0m));
            Assert.Equal(-1, Math.Sign(-3.14m));
            Assert.Equal(1, Math.Sign(3.14m));
        }

        [Fact]
        public static void Sign_Double()
        {
            Assert.Equal(0, Math.Sign(0.0));
            Assert.Equal(0, Math.Sign(-0.0));
            Assert.Equal(-1, Math.Sign(-3.14));
            Assert.Equal(1, Math.Sign(3.14));
            Assert.Equal(-1, Math.Sign(double.NegativeInfinity));
            Assert.Equal(1, Math.Sign(double.PositiveInfinity));
            Assert.Throws<ArithmeticException>(() => Math.Sign(double.NaN));
        }

        [Fact]
        public static void Sign_Short()
        {
            Assert.Equal(0, Math.Sign((short)0));
            Assert.Equal(-1, Math.Sign((short)(-3)));
            Assert.Equal(1, Math.Sign((short)3));
        }

        [Fact]
        public static void Sign_Int()
        {
            Assert.Equal(0, Math.Sign(0));
            Assert.Equal(-1, Math.Sign(-3));
            Assert.Equal(1, Math.Sign(3));
        }

        [Fact]
        public static void Sign_Long()
        {
            Assert.Equal(0, Math.Sign(0));
            Assert.Equal(-1, Math.Sign(-3));
            Assert.Equal(1, Math.Sign(3));
        }

        [Fact]
        public static void Sign_SByte()
        {
            Assert.Equal(0, Math.Sign((sbyte)0));
            Assert.Equal(-1, Math.Sign((sbyte)(-3)));
            Assert.Equal(1, Math.Sign((sbyte)3));
        }

        [Fact]
        public static void Sign_Float()
        {
            Assert.Equal(0, Math.Sign(0.0f));
            Assert.Equal(0, Math.Sign(-0.0f));
            Assert.Equal(-1, Math.Sign(-3.14f));
            Assert.Equal(1, Math.Sign(3.14f));
            Assert.Equal(-1, Math.Sign(float.NegativeInfinity));
            Assert.Equal(1, Math.Sign(float.PositiveInfinity));
            Assert.Throws<ArithmeticException>(() => Math.Sign(float.NaN));
        }

        [Fact]
        public static void Truncate_Decimal()
        {
            Assert.Equal(0.0m, Math.Truncate(0.12345m));
            Assert.Equal(3.0m, Math.Truncate(3.14159m));
            Assert.Equal(-3.0m, Math.Truncate(-3.14159m));
        }

        [Fact]
        public static void Truncate_Double()
        {
            Assert.Equal(0.0, Math.Truncate(0.12345));
            Assert.Equal(3.0, Math.Truncate(3.14159));
            Assert.Equal(-3.0, Math.Truncate(-3.14159));
        }
    }
}
