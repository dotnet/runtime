// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryNullablePowerTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableBytePowerTest(bool useInterpreter)
        {
            byte?[] array = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableBytePower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableSBytePowerTest(bool useInterpreter)
        {
            sbyte?[] array = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSBytePower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUShortPowerTest(bool useInterpreter)
        {
            ushort?[] array = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableShortPowerTest(bool useInterpreter)
        {
            short?[] array = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableUIntPowerTest(bool useInterpreter)
        {
            uint?[] array = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntPowerTest(bool useInterpreter)
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableULongPowerTest(bool useInterpreter)
        {
            ulong?[] array = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongPowerTest(bool useInterpreter)
        {
            long?[] array = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableFloatPowerTest(bool useInterpreter)
        {
            float?[] array = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDoublePowerTest(bool useInterpreter)
        {
            double?[] array = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoublePower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableDecimalPowerTest(bool useInterpreter)
        {
            decimal?[] array = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalPower(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableCharPowerTest(bool useInterpreter)
        {
            char?[] array = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharPower(array[i], array[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBytePower(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerByte")
                    ));
            Func<byte?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerByte(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableSBytePower(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerSByte")
                    ));
            Func<sbyte?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerSByte(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableUShortPower(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerUShort")
                    ));
            Func<ushort?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerUShort(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableShortPower(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerShort")
                    ));
            Func<short?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerShort(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableUIntPower(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerUInt")
                    ));
            Func<uint?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerUInt(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableIntPower(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerInt")
                    ));
            Func<int?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerInt(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableULongPower(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerULong")
                    ));
            Func<ulong?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerULong(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableLongPower(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerLong")
                    ));
            Func<long?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerLong(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableFloatPower(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerFloat")
                    ));
            Func<float?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerFloat(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableDoublePower(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerDouble")
                    ));
            Func<double?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerDouble(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        private static void VerifyNullableDecimalPower(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerDecimal")
                    ));
            Func<decimal?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
            {
                decimal expected = 0;
                try
                {
                    expected = PowerDecimal(a.GetValueOrDefault(), b.GetValueOrDefault());
                }
                catch (OverflowException)
                {
                    Assert.Throws<OverflowException>(() => f());
                    return;
                }

                Assert.Equal(expected, f());
            }
            else
                Assert.Null(f());
        }

        private static void VerifyNullableCharPower(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Power(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(BinaryNullablePowerTests).GetTypeInfo().GetDeclaredMethod("PowerChar")
                    ));
            Func<char?> f = e.Compile(useInterpreter);

            if (a.HasValue & b.HasValue)
                Assert.Equal(PowerChar(a.GetValueOrDefault(), b.GetValueOrDefault()), f());
            else
                Assert.Null(f());
        }

        #endregion

        #region Helper methods

        public static byte PowerByte(byte a, byte b)
        {
            return unchecked((byte)Math.Pow(a, b));
        }

        public static sbyte PowerSByte(sbyte a, sbyte b)
        {
            return unchecked((sbyte)Math.Pow(a, b));
        }

        public static ushort PowerUShort(ushort a, ushort b)
        {
            return unchecked((ushort)Math.Pow(a, b));
        }

        public static short PowerShort(short a, short b)
        {
            return unchecked((short)Math.Pow(a, b));
        }

        public static uint PowerUInt(uint a, uint b)
        {
            return unchecked((uint)Math.Pow(a, b));
        }

        public static int PowerInt(int a, int b)
        {
            return unchecked((int)Math.Pow(a, b));
        }

        public static ulong PowerULong(ulong a, ulong b)
        {
            return unchecked((ulong)Math.Pow(a, b));
        }

        public static long PowerLong(long a, long b)
        {
            return unchecked((long)Math.Pow(a, b));
        }

        public static float PowerFloat(float a, float b)
        {
            return (float)Math.Pow(a, b);
        }

        public static double PowerDouble(double a, double b)
        {
            return Math.Pow(a, b);
        }

        public static decimal PowerDecimal(decimal a, decimal b)
        {
            return (decimal)Math.Pow((double)a, (double)b);
        }

        public static char PowerChar(char a, char b)
        {
            return unchecked((char)Math.Pow(a, b));
        }

        #endregion
    }
}
