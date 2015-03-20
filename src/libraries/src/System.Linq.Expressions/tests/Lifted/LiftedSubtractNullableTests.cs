// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class LiftedSubtractNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckLiftedSubtractNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedSubtractNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte SubtractNullableByte(byte a, byte b)
        {
            return (byte)(a - b);
        }

        public static char SubtractNullableChar(char a, char b)
        {
            return (char)(a - b);
        }

        public static decimal SubtractNullableDecimal(decimal a, decimal b)
        {
            return (decimal)(a - b);
        }

        public static double SubtractNullableDouble(double a, double b)
        {
            return (double)(a - b);
        }

        public static float SubtractNullableFloat(float a, float b)
        {
            return (float)(a - b);
        }

        public static int SubtractNullableInt(int a, int b)
        {
            return (int)(a - b);
        }

        public static long SubtractNullableLong(long a, long b)
        {
            return (long)(a - b);
        }

        public static sbyte SubtractNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)(a - b);
        }

        public static short SubtractNullableShort(short a, short b)
        {
            return (short)(a - b);
        }

        public static uint SubtractNullableUInt(uint a, uint b)
        {
            return (uint)(a - b);
        }

        public static ulong SubtractNullableULong(ulong a, ulong b)
        {
            return (ulong)(a - b);
        }

        public static ushort SubtractNullableUShort(ushort a, ushort b)
        {
            return (ushort)(a - b);
        }

        #endregion

        #region Test verifiers

        private static void VerifySubtractNullableByte(byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableByte")));
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
                expected = (byte?)(a - b);
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

        private static void VerifySubtractNullableChar(char? a, char? b)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableChar")));
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
                expected = (char?)(a - b);
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

        private static void VerifySubtractNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableDecimal")));
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
                expected = (decimal?)(a - b);
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

        private static void VerifySubtractNullableDouble(double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableDouble")));
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
                expected = (double?)(a - b);
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

        private static void VerifySubtractNullableFloat(float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableFloat")));
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
                expected = (float?)(a - b);
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

        private static void VerifySubtractNullableInt(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableInt")));
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
                expected = (int?)(a - b);
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

        private static void VerifySubtractNullableLong(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableLong")));
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
                expected = (long?)(a - b);
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

        private static void VerifySubtractNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableSByte")));
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
                expected = (sbyte?)(a - b);
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

        private static void VerifySubtractNullableShort(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableShort")));
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
                expected = (short?)(a - b);
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

        private static void VerifySubtractNullableUInt(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableUInt")));
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
                expected = (uint?)(a - b);
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

        private static void VerifySubtractNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableULong")));
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
                expected = (ulong?)(a - b);
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

        private static void VerifySubtractNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedSubtractNullableTests).GetTypeInfo().GetDeclaredMethod("SubtractNullableUShort")));
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
                expected = (ushort?)(a - b);
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
