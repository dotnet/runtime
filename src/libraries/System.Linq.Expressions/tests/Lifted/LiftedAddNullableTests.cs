// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LiftedAddNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableByteTest(bool useInterpreter)
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableCharTest(bool useInterpreter)
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableChar(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableDecimalTest(bool useInterpreter)
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableSByteTest(bool useInterpreter)
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableSByte(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLiftedAddNullableNumberTest(bool useInterpreter)
        {
            Number?[] values = new Number?[] { null, new Number(0), new Number(1), Number.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyAddNullableNumber(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte AddNullableByte(byte a, byte b)
        {
            return (byte)(a + b);
        }

        public static char AddNullableChar(char a, char b)
        {
            return (char)(a + b);
        }

        public static decimal AddNullableDecimal(decimal a, decimal b)
        {
            return (decimal)(a + b);
        }

        public static double AddNullableDouble(double a, double b)
        {
            return (double)(a + b);
        }

        public static float AddNullableFloat(float a, float b)
        {
            return (float)(a + b);
        }

        public static int AddNullableInt(int a, int b)
        {
            return (int)(a + b);
        }

        public static long AddNullableLong(long a, long b)
        {
            return (long)(a + b);
        }

        public static sbyte AddNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a + b);
        }

        public static short AddNullableShort(short a, short b)
        {
            return (short)(a + b);
        }

        public static uint AddNullableUInt(uint a, uint b)
        {
            return (uint)(a + b);
        }

        public static ulong AddNullableULong(ulong a, ulong b)
        {
            return (ulong)(a + b);
        }

        public static ushort AddNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a + b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyAddNullableByte(byte? a, byte? b, bool useInterpreter)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableByte")));
            Func<byte?> f = e.Compile(useInterpreter);

            Assert.Equal((byte?)(a + b), f());
        }

        private static void VerifyAddNullableChar(char? a, char? b, bool useInterpreter)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableChar")));
            Func<char?> f = e.Compile(useInterpreter);

            Assert.Equal((char?)(a + b), f());
        }

        private static void VerifyAddNullableDecimal(decimal? a, decimal? b, bool useInterpreter)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableDecimal")));
            Func<decimal?> f = e.Compile(useInterpreter);

            decimal? expected = null;
            try
            {
                expected = a + b;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyAddNullableDouble(double? a, double? b, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableDouble")));
            Func<double?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddNullableFloat(float? a, float? b, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableFloat")));
            Func<float?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddNullableInt(int? a, int? b, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableInt")));
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddNullableLong(long? a, long? b, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableLong")));
            Func<long?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddNullableSByte(sbyte? a, sbyte? b, bool useInterpreter)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableSByte")));
            Func<sbyte?> f = e.Compile(useInterpreter);

            Assert.Equal((sbyte?)(a + b), f());
        }

        private static void VerifyAddNullableShort(short? a, short? b, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableShort")));
            Func<short?> f = e.Compile(useInterpreter);

            Assert.Equal((short?)(a + b), f());
        }

        private static void VerifyAddNullableUInt(uint? a, uint? b, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableUInt")));
            Func<uint?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddNullableULong(ulong? a, ulong? b, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableULong")));
            Func<ulong?> f = e.Compile(useInterpreter);

            Assert.Equal(a + b, f());
        }

        private static void VerifyAddNullableUShort(ushort? a, ushort? b, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedAddNullableTests).GetTypeInfo().GetDeclaredMethod("AddNullableUShort")));
            Func<ushort?> f = e.Compile(useInterpreter);

            Assert.Equal((ushort?)(a + b), f());
        }

        private static void VerifyAddNullableNumber(Number? a, Number? b, bool useInterpreter)
        {
            Expression<Func<Number?>> e =
                Expression.Lambda<Func<Number?>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(Number?)),
                        Expression.Constant(b, typeof(Number?))));
            Assert.Equal(typeof(Number?), e.Body.Type);
            Func<Number?> f = e.Compile(useInterpreter);

            Number? expected = a + b;
            Assert.Equal(expected, f());
        }

        #endregion
    }
}
