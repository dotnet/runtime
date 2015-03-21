// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class LiftedMultiplyNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckLiftedMultiplyNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyNullableUShort(values[i], values[j]);
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

        private static void VerifyMultiplyNullableByte(byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableByte")));
            Func<byte?> f = e.Compile();

            byte? result = default(byte);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            byte? expected = default(byte);
            Exception csEx = null;
            try
            {
                expected = (byte?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableChar(char? a, char? b)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableChar")));
            Func<char?> f = e.Compile();

            char? result = default(char);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            char? expected = default(char);
            Exception csEx = null;
            try
            {
                expected = (char?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableDecimal")));
            Func<decimal?> f = e.Compile();

            decimal? result = default(decimal);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            decimal? expected = default(decimal);
            Exception csEx = null;
            try
            {
                expected = (decimal?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableDouble(double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableDouble")));
            Func<double?> f = e.Compile();

            double? result = default(double);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            double? expected = default(double);
            Exception csEx = null;
            try
            {
                expected = (double?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableFloat(float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableFloat")));
            Func<float?> f = e.Compile();

            float? result = default(float);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            float? expected = default(float);
            Exception csEx = null;
            try
            {
                expected = (float?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableInt(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableInt")));
            Func<int?> f = e.Compile();

            int? result = default(int);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            int? expected = default(int);
            Exception csEx = null;
            try
            {
                expected = (int?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableLong(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableLong")));
            Func<long?> f = e.Compile();

            long? result = default(long);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            long? expected = default(long);
            Exception csEx = null;
            try
            {
                expected = (long?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableSByte")));
            Func<sbyte?> f = e.Compile();

            sbyte? result = default(sbyte);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            sbyte? expected = default(sbyte);
            Exception csEx = null;
            try
            {
                expected = (sbyte?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableShort(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableShort")));
            Func<short?> f = e.Compile();

            short? result = default(short);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            short? expected = default(short);
            Exception csEx = null;
            try
            {
                expected = (short?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableUInt(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableUInt")));
            Func<uint?> f = e.Compile();

            uint? result = default(uint);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            uint? expected = default(uint);
            Exception csEx = null;
            try
            {
                expected = (uint?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableULong")));
            Func<ulong?> f = e.Compile();

            ulong? result = default(ulong);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ulong? expected = default(ulong);
            Exception csEx = null;
            try
            {
                expected = (ulong?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        private static void VerifyMultiplyNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedMultiplyNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyNullableUShort")));
            Func<ushort?> f = e.Compile();

            ushort? result = default(ushort);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ushort? expected = default(ushort);
            Exception csEx = null;
            try
            {
                expected = (ushort?)(a * b);
            }
            catch (Exception ex)
            {
                csEx = ex;
            }

            if (fEx != null || csEx != null)
            {
                Assert.NotNull(fEx);
                Assert.NotNull(csEx);
                Assert.Equal(csEx.GetType(), fEx.GetType());
            }
            else
            {
                Assert.Equal(expected, result);
            }
        }

        #endregion
    }
}
