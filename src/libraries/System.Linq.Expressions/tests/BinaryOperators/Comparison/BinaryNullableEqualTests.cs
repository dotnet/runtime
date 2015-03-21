// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryNullableEqualTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableBoolEqualTest()
        {
            bool?[] array = new bool?[] { null, true, false };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableBoolEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableByteEqualTest()
        {
            byte?[] array = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableByteEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableCharEqualTest()
        {
            char?[] array = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableCharEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDecimalEqualTest()
        {
            decimal?[] array = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDecimalEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDoubleEqualTest()
        {
            double?[] array = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableDoubleEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableFloatEqualTest()
        {
            float?[] array = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableFloatEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableIntEqualTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableIntEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableLongEqualTest()
        {
            long?[] array = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableLongEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableSByteEqualTest()
        {
            sbyte?[] array = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableSByteEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableShortEqualTest()
        {
            short?[] array = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableShortEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUIntEqualTest()
        {
            uint?[] array = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUIntEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableULongEqualTest()
        {
            ulong?[] array = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableULongEqual(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUShortEqualTest()
        {
            ushort?[] array = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyNullableUShortEqual(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBoolEqual(bool? a, bool? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableByteEqual(byte? a, byte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableCharEqual(char? a, char? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableDecimalEqual(decimal? a, decimal? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableDoubleEqual(double? a, double? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableFloatEqual(float? a, float? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableIntEqual(int? a, int? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableLongEqual(long? a, long? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableSByteEqual(sbyte? a, sbyte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableShortEqual(short? a, short? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableUIntEqual(uint? a, uint? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableULongEqual(ulong? a, ulong? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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

        private static void VerifyNullableUShortEqual(ushort? a, ushort? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute with expression tree
            bool etResult = default(bool);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute with real IL
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = (bool)(a == b);
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
