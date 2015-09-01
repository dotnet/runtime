// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryNullableSubtractTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableByteSubtractTest()
        {
            byte?[] array = new byte?[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableByteSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableSByteSubtractTest()
        {
            sbyte?[] array = new sbyte?[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSByteSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUShortSubtractTest()
        {
            ushort?[] array = new ushort?[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableShortSubtractTest()
        {
            short?[] array = new short?[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUIntSubtractTest()
        {
            uint?[] array = new uint?[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableIntSubtractTest()
        {
            int?[] array = new int?[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableULongSubtractTest()
        {
            ulong?[] array = new ulong?[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableLongSubtractTest()
        {
            long?[] array = new long?[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableFloatSubtractTest()
        {
            float?[] array = new float?[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDoubleSubtractTest()
        {
            double?[] array = new double?[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoubleSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDecimalSubtractTest()
        {
            decimal?[] array = new decimal?[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableCharSubtractTest()
        {
            char?[] array = new char?[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharSubtract(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableByteSubtract(byte? a, byte? b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte?));
            Expression bExp = Expression.Constant(b, typeof(byte?));
            Assert.Throws<InvalidOperationException>(() => Expression.Subtract(aExp, bExp));
        }

        private static void VerifyNullableSByteSubtract(sbyte? a, sbyte? b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte?));
            Expression bExp = Expression.Constant(b, typeof(sbyte?));
            Assert.Throws<InvalidOperationException>(() => Expression.Subtract(aExp, bExp));
        }

        private static void VerifyNullableUShortSubtract(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();

            // add with expression tree
            ushort? etResult = default(ushort?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            ushort? csResult = default(ushort?);
            Exception csException = null;
            try
            {
                csResult = (ushort?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableShortSubtract(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();

            // add with expression tree
            short? etResult = default(short?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            short? csResult = default(short?);
            Exception csException = null;
            try
            {
                csResult = (short?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableUIntSubtract(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();

            // add with expression tree
            uint? etResult = default(uint?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            uint? csResult = default(uint?);
            Exception csException = null;
            try
            {
                csResult = (uint?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableIntSubtract(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();

            // add with expression tree
            int? etResult = default(int?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            int? csResult = default(int?);
            Exception csException = null;
            try
            {
                csResult = (int?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableULongSubtract(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();

            // add with expression tree
            ulong? etResult = default(ulong?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            ulong? csResult = default(ulong?);
            Exception csException = null;
            try
            {
                csResult = (ulong?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableLongSubtract(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();

            // add with expression tree
            long? etResult = default(long?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            long? csResult = default(long?);
            Exception csException = null;
            try
            {
                csResult = (long?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableFloatSubtract(float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile();

            // add with expression tree
            float? etResult = default(float?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            float? csResult = default(float?);
            Exception csException = null;
            try
            {
                csResult = (float?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableDoubleSubtract(double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile();

            // add with expression tree
            double? etResult = default(double?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            double? csResult = default(double?);
            Exception csException = null;
            try
            {
                csResult = (double?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableDecimalSubtract(decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile();

            // add with expression tree
            decimal? etResult = default(decimal?);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // add with real IL
            decimal? csResult = default(decimal?);
            Exception csException = null;
            try
            {
                csResult = (decimal?)(a - b);
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableCharSubtract(char? a, char? b)
        {
            Expression aExp = Expression.Constant(a, typeof(char?));
            Expression bExp = Expression.Constant(b, typeof(char?));
            Assert.Throws<InvalidOperationException>(() => Expression.Subtract(aExp, bExp));
        }

        #endregion
    }
}
