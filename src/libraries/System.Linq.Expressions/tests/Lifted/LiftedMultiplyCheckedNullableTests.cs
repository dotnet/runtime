// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class LiftedMultiplyCheckedNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedMultiplyCheckedNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyMultiplyCheckedNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Helpers

        public static byte MultiplyCheckedNullableByte(byte a, byte b)
        {
            return (byte)checked(a * b);
        }

        public static char MultiplyCheckedNullableChar(char a, char b)
        {
            return (char)checked(a * b);
        }

        public static decimal MultiplyCheckedNullableDecimal(decimal a, decimal b)
        {
            return (decimal)checked(a * b);
        }

        public static double MultiplyCheckedNullableDouble(double a, double b)
        {
            return (double)checked(a * b);
        }

        public static float MultiplyCheckedNullableFloat(float a, float b)
        {
            return (float)checked(a * b);
        }

        public static int MultiplyCheckedNullableInt(int a, int b)
        {
            return (int)checked(a * b);
        }

        public static long MultiplyCheckedNullableLong(long a, long b)
        {
            return (long)checked(a * b);
        }

        public static sbyte MultiplyCheckedNullableSByte(sbyte a, sbyte b)
        {
            return (sbyte)checked(a * b);
        }

        public static short MultiplyCheckedNullableShort(short a, short b)
        {
            return (short)checked(a * b);
        }

        public static uint MultiplyCheckedNullableUInt(uint a, uint b)
        {
            return (uint)checked(a * b);
        }

        public static ulong MultiplyCheckedNullableULong(ulong a, ulong b)
        {
            return (ulong)checked(a * b);
        }

        public static ushort MultiplyCheckedNullableUShort(ushort a, ushort b)
        {
            return (ushort)checked(a * b);
        }

        #endregion

        #region Test verifiers

        private static void VerifyMultiplyCheckedNullableByte(byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableByte")));
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
                expected = (byte?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableChar(char? a, char? b)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableChar")));
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
                expected = (char?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableDecimal")));
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
                expected = (decimal?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableDouble(double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableDouble")));
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
                expected = (double?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableFloat(float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableFloat")));
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
                expected = (float?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableInt(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableInt")));
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
                expected = (int?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableLong(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableLong")));
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
                expected = (long?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableSByte")));
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
                expected = (sbyte?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableShort(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableShort")));
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
                expected = (short?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableUInt(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableUInt")));
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
                expected = (uint?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableULong")));
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
                expected = (ulong?)checked(a * b);
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

        private static void VerifyMultiplyCheckedNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        typeof(LiftedMultiplyCheckedNullableTests).GetTypeInfo().GetDeclaredMethod("MultiplyCheckedNullableUShort")));
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
                expected = (ushort?)checked(a * b);
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
