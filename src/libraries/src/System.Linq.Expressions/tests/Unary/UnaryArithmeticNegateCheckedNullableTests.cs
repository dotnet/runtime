// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static class UnaryArithmeticNegateCheckedNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableChar(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableDecimal(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableDouble(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableFloat(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableLong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableSByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCheckedNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateCheckedNullableShort(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticNegateCheckedNullableByte(byte? value)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.NegateChecked(Expression.Constant(value, typeof(byte?))));
        }

        private static void VerifyArithmeticNegateCheckedNullableChar(char? value)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.NegateChecked(Expression.Constant(value, typeof(char?))));
        }

        private static void VerifyArithmeticNegateCheckedNullableDecimal(decimal? value)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.NegateChecked(Expression.Constant(value, typeof(decimal?))),
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
                csResult = checked((decimal?)(-value));
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

        private static void VerifyArithmeticNegateCheckedNullableDouble(double? value)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.NegateChecked(Expression.Constant(value, typeof(double?))),
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
                csResult = checked((double?)(-value));
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

        private static void VerifyArithmeticNegateCheckedNullableFloat(float? value)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.NegateChecked(Expression.Constant(value, typeof(float?))),
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
                csResult = checked((float?)(-value));
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

        private static void VerifyArithmeticNegateCheckedNullableInt(int? value)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.NegateChecked(Expression.Constant(value, typeof(int?))),
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
                csResult = checked((int?)(-value));
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

        private static void VerifyArithmeticNegateCheckedNullableLong(long? value)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.NegateChecked(Expression.Constant(value, typeof(long?))),
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
                csResult = checked((long?)(-value));
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

        private static void VerifyArithmeticNegateCheckedNullableSByte(sbyte? value)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.NegateChecked(Expression.Constant(value, typeof(sbyte?))));
        }

        private static void VerifyArithmeticNegateCheckedNullableShort(short? value)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.NegateChecked(Expression.Constant(value, typeof(short?))),
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
                csResult = checked((short?)(-value));
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

        #endregion
    }
}
