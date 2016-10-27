// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedMultiplyNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedMultiplyNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte MultiplyNullableByte(byte a, byte b)
        {
            return (byte)(a * b);
        }

        public static char MultiplyNullableChar(char a, char b)
        {
            return (char)(a * b);
        }

        public static decimal MultiplyNullableDecimal(decimal a, decimal b)
        {
            return (decimal)(a * b);
        }

        public static double MultiplyNullableDouble(double a, double b)
        {
            return (double)(a * b);
        }

        public static float MultiplyNullableFloat(float a, float b)
        {
            return (float)(a * b);
        }

        public static int MultiplyNullableInt(int a, int b)
        {
            return (int)(a * b);
        }

        public static long MultiplyNullableLong(long a, long b)
        {
            return (long)(a * b);
        }

        public static sbyte MultiplyNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a * b);
        }

        public static short MultiplyNullableShort(short a, short b)
        {
            return (short)(a * b);
        }

        public static uint MultiplyNullableUInt(uint a, uint b)
        {
            return (uint)(a * b);
        }

        public static ulong MultiplyNullableULong(ulong a, ulong b)
        {
            return (ulong)(a * b);
        }

        public static ushort MultiplyNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a * b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyMultiplyNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal((byte?)(a * b), f());
        }

        private static void VerifyMultiplyNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)(a * b), f());
        }

        private static void VerifyMultiplyNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = null;
            try
            {
                expected = a * b;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyMultiplyNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal((sbyte?)(a * b), f());
        }

        private static void VerifyMultiplyNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal((short?)(a * b), f());
        }

        private static void VerifyMultiplyNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyMultiplyNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal((ushort?)(a * b), f());
        }

        private static void VerifyMultiplyNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a * b;
            Assert.Equal(expected, f());
        }

        #endregion
    }
}
