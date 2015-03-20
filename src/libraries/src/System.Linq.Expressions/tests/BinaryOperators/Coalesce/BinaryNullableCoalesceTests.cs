// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Binary
{
    public static unsafe class BinaryNullableCoalesceTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableBoolCoalesceTest()
        {
            bool?[] array1 = new bool?[] { null, true, false };
            bool?[] array2 = new bool?[] { null, true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableBoolCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableByteCoalesceTest()
        {
            byte?[] array1 = new byte?[] { null, 0, 1, byte.MaxValue };
            byte?[] array2 = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableByteCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableCharCoalesceTest()
        {
            char?[] array1 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            char?[] array2 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableCharCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDecimalCoalesceTest()
        {
            decimal?[] array1 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            decimal?[] array2 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableDecimalCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableDoubleCoalesceTest()
        {
            double?[] array1 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            double?[] array2 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableDoubleCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableEnumCoalesceTest()
        {
            E?[] array1 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            E?[] array2 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableEnumCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableEnumLongCoalesceTest()
        {
            El?[] array1 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            El?[] array2 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableEnumLongCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableFloatCoalesceTest()
        {
            float?[] array1 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            float?[] array2 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableFloatCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableIntCoalesceTest()
        {
            int?[] array1 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            int?[] array2 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableIntCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableLongCoalesceTest()
        {
            long?[] array1 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            long?[] array2 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableLongCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableStructCoalesceTest()
        {
            S?[] array1 = new S?[] { null, default(S), new S() };
            S?[] array2 = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableSByteCoalesceTest()
        {
            sbyte?[] array1 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            sbyte?[] array2 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableSByteCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableStructWithStringCoalesceTest()
        {
            Sc?[] array1 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            Sc?[] array2 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithStringCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableStructWithStringAndFieldCoalesceTest()
        {
            Scs?[] array1 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            Scs?[] array2 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithStringAndFieldCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableShortCoalesceTest()
        {
            short?[] array1 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            short?[] array2 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableShortCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableStructWithTwoValuesCoalesceTest()
        {
            Sp?[] array1 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            Sp?[] array2 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithTwoValuesCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableStructWithValueCoalesceTest()
        {
            Ss?[] array1 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            Ss?[] array2 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableStructWithValueCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUIntCoalesceTest()
        {
            uint?[] array1 = new uint?[] { null, 0, 1, uint.MaxValue };
            uint?[] array2 = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableUIntCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableULongCoalesceTest()
        {
            ulong?[] array1 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            ulong?[] array2 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableULongCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNullableUShortCoalesceTest()
        {
            ushort?[] array1 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            ushort?[] array2 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNullableUShortCoalesce(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckGenericEnumWithStructRestrictionCoalesceTest()
        {
            CheckGenericWithStructRestrictionCoalesceHelper<E>();
        }

        [Fact]
        public static void CheckGenericStructWithStructRestrictionCoalesceTest()
        {
            CheckGenericWithStructRestrictionCoalesceHelper<S>();
        }

        [Fact]
        public static void CheckGenericStructWithStringAndFieldWithStructRestrictionCoalesceTest()
        {
            CheckGenericWithStructRestrictionCoalesceHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionCoalesceHelper<Ts>() where Ts : struct
        {
            Ts?[] array1 = new Ts?[] { null, default(Ts), new Ts() };
            Ts?[] array2 = new Ts?[] { null, default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyGenericWithStructRestrictionCoalesce<Ts>(array1[i], array2[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBoolCoalesce(bool? a, bool? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile();

            // compute with expression tree
            bool? etResult = default(bool?);
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
            bool? csResult = default(bool?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableByteCoalesce(byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile();

            // compute with expression tree
            byte? etResult = default(byte?);
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
            byte? csResult = default(byte?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableCharCoalesce(char? a, char? b)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile();

            // compute with expression tree
            char? etResult = default(char?);
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
            char? csResult = default(char?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableDecimalCoalesce(decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            decimal? csResult = default(decimal?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableDoubleCoalesce(double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            double? csResult = default(double?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableEnumCoalesce(E? a, E? b)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(E?)),
                        Expression.Constant(b, typeof(E?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile();

            // compute with expression tree
            E? etResult = default(E?);
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
            E? csResult = default(E?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableEnumLongCoalesce(El? a, El? b)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(El?)),
                        Expression.Constant(b, typeof(El?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile();

            // compute with expression tree
            El? etResult = default(El?);
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
            El? csResult = default(El?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableFloatCoalesce(float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            float? csResult = default(float?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableIntCoalesce(int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            int? csResult = default(int?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableLongCoalesce(long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            long? csResult = default(long?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableStructCoalesce(S? a, S? b)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(S?)),
                        Expression.Constant(b, typeof(S?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile();

            // compute with expression tree
            S? etResult = default(S?);
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
            S? csResult = default(S?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableSByteCoalesce(sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile();

            // compute with expression tree
            sbyte? etResult = default(sbyte?);
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
            sbyte? csResult = default(sbyte?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableStructWithStringCoalesce(Sc? a, Sc? b)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sc?)),
                        Expression.Constant(b, typeof(Sc?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();

            // compute with expression tree
            Sc? etResult = default(Sc?);
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
            Sc? csResult = default(Sc?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableStructWithStringAndFieldCoalesce(Scs? a, Scs? b)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Scs?)),
                        Expression.Constant(b, typeof(Scs?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile();

            // compute with expression tree
            Scs? etResult = default(Scs?);
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
            Scs? csResult = default(Scs?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableShortCoalesce(short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            short? csResult = default(short?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableStructWithTwoValuesCoalesce(Sp? a, Sp? b)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Sp?)),
                        Expression.Constant(b, typeof(Sp?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile();

            // compute with expression tree
            Sp? etResult = default(Sp?);
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
            Sp? csResult = default(Sp?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableStructWithValueCoalesce(Ss? a, Ss? b)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ss?)),
                        Expression.Constant(b, typeof(Ss?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile();

            // compute with expression tree
            Ss? etResult = default(Ss?);
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
            Ss? csResult = default(Ss?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableUIntCoalesce(uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            uint? csResult = default(uint?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableULongCoalesce(ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            ulong? csResult = default(ulong?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyNullableUShortCoalesce(ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();

            // compute with expression tree
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

            // compute with real IL
            ushort? csResult = default(ushort?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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

        private static void VerifyGenericWithStructRestrictionCoalesce<Ts>(Ts? a, Ts? b) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.Coalesce(
                        Expression.Constant(a, typeof(Ts?)),
                        Expression.Constant(b, typeof(Ts?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile();

            // compute with expression tree
            Ts? etResult = default(Ts?);
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
            Ts? csResult = default(Ts?);
            Exception csException = null;
            try
            {
                csResult = a ?? b;
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
