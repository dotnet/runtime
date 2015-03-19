// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Ternary
{
    public static unsafe class TernaryNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckTernaryNullableBoolTest()
        {
            bool[] array1 = new bool[] { false, true };
            bool?[] array2 = new bool?[] { null, true, false };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableBool(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableByteTest()
        {
            bool[] array1 = new bool[] { false, true };
            byte?[] array2 = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableByte(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableCharTest()
        {
            bool[] array1 = new bool[] { false, true };
            char?[] array2 = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableChar(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableDecimalTest()
        {
            bool[] array1 = new bool[] { false, true };
            decimal?[] array2 = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableDecimal(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableDoubleTest()
        {
            bool[] array1 = new bool[] { false, true };
            double?[] array2 = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableDouble(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableEnumTest()
        {
            bool[] array1 = new bool[] { false, true };
            E?[] array2 = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableEnum(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableEnumLongTest()
        {
            bool[] array1 = new bool[] { false, true };
            El?[] array2 = new El?[] { null, (El)0, El.A, El.B, (El)long.MaxValue, (El)long.MinValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableEnumLong(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableFloatTest()
        {
            bool[] array1 = new bool[] { false, true };
            float?[] array2 = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableFloat(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableIntTest()
        {
            bool[] array1 = new bool[] { false, true };
            int?[] array2 = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableInt(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableLongTest()
        {
            bool[] array1 = new bool[] { false, true };
            long?[] array2 = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableLong(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableStructTest()
        {
            bool[] array1 = new bool[] { false, true };
            S?[] array2 = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableStruct(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableSByteTest()
        {
            bool[] array1 = new bool[] { false, true };
            sbyte?[] array2 = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableSByte(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableStructWithStringTest()
        {
            bool[] array1 = new bool[] { false, true };
            Sc?[] array2 = new Sc?[] { null, default(Sc), new Sc(), new Sc(null) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableStructWithString(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableStructWithStringAndFieldTest()
        {
            bool[] array1 = new bool[] { false, true };
            Scs?[] array2 = new Scs?[] { null, default(Scs), new Scs(), new Scs(null, new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableStructWithStringAndField(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableShortTest()
        {
            bool[] array1 = new bool[] { false, true };
            short?[] array2 = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableShort(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableStructWithTwoValuesTest()
        {
            bool[] array1 = new bool[] { false, true };
            Sp?[] array2 = new Sp?[] { null, default(Sp), new Sp(), new Sp(5, 5.0) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableStructWithTwoValues(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableStructWithValueTest()
        {
            bool[] array1 = new bool[] { false, true };
            Ss?[] array2 = new Ss?[] { null, default(Ss), new Ss(), new Ss(new S()) };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableStructWithValue(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableUIntTest()
        {
            bool[] array1 = new bool[] { false, true };
            uint?[] array2 = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableUInt(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableULongTest()
        {
            bool[] array1 = new bool[] { false, true };
            ulong?[] array2 = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableULong(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableUShortTest()
        {
            bool[] array1 = new bool[] { false, true };
            ushort?[] array2 = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableUShort(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        [Fact]
        public static void CheckTernaryNullableGenericWithStructRestrictionWithEnumTest()
        {
            CheckTernaryNullableGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckTernaryNullableGenericWithStructRestrictionWithStructTest()
        {
            CheckTernaryNullableGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckTernaryNullableGenericWithStructRestrictionWithStructWithStringAndFieldTest()
        {
            CheckTernaryNullableGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckTernaryNullableGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            bool[] array1 = new bool[] { false, true };
            Ts?[] array2 = new Ts?[] { default(Ts), new Ts() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    for (int k = 0; k < array2.Length; k++)
                    {
                        VerifyNullableGenericWithStructRestriction<Ts>(array1[i], array2[j], array2[k]);
                    }
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableBool(bool condition, bool? a, bool? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile();

            bool? result = default(bool?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            bool? expected = default(bool?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableByte(bool condition, byte? a, byte? b)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile();

            byte? result = default(byte?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            byte? expected = default(byte?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableChar(bool condition, char? a, char? b)
        {
            Expression<Func<char?>> e =
                Expression.Lambda<Func<char?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<char?> f = e.Compile();

            char? result = default(char?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            char? expected = default(char?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableDecimal(bool condition, decimal? a, decimal? b)
        {
            Expression<Func<decimal?>> e =
                Expression.Lambda<Func<decimal?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal?> f = e.Compile();

            decimal? result = default(decimal?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            decimal? expected = default(decimal?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableDouble(bool condition, double? a, double? b)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile();

            double? result = default(double?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            double? expected = default(double?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableEnum(bool condition, E? a, E? b)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(E?)),
                        Expression.Constant(b, typeof(E?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile();

            E? result = default(E?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            E? expected = default(E?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableEnumLong(bool condition, El? a, El? b)
        {
            Expression<Func<El?>> e =
                Expression.Lambda<Func<El?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(El?)),
                        Expression.Constant(b, typeof(El?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<El?> f = e.Compile();

            El? result = default(El?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            El? expected = default(El?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableFloat(bool condition, float? a, float? b)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile();

            float? result = default(float?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            float? expected = default(float?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableInt(bool condition, int? a, int? b)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();

            int? result = default(int?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            int? expected = default(int?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableLong(bool condition, long? a, long? b)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();

            long? result = default(long?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            long? expected = default(long?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableStruct(bool condition, S? a, S? b)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(S?)),
                        Expression.Constant(b, typeof(S?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile();

            S? result = default(S?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            S? expected = default(S?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableSByte(bool condition, sbyte? a, sbyte? b)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile();

            sbyte? result = default(sbyte?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            sbyte? expected = default(sbyte?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableStructWithString(bool condition, Sc? a, Sc? b)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sc?)),
                        Expression.Constant(b, typeof(Sc?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();

            Sc? result = default(Sc?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sc? expected = default(Sc?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableStructWithStringAndField(bool condition, Scs? a, Scs? b)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Scs?)),
                        Expression.Constant(b, typeof(Scs?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile();

            Scs? result = default(Scs?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Scs? expected = default(Scs?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableShort(bool condition, short? a, short? b)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();

            short? result = default(short?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            short? expected = default(short?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableStructWithTwoValues(bool condition, Sp? a, Sp? b)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Sp?)),
                        Expression.Constant(b, typeof(Sp?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile();

            Sp? result = default(Sp?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Sp? expected = default(Sp?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableStructWithValue(bool condition, Ss? a, Ss? b)
        {
            Expression<Func<Ss?>> e =
                Expression.Lambda<Func<Ss?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ss?)),
                        Expression.Constant(b, typeof(Ss?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ss?> f = e.Compile();

            Ss? result = default(Ss?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ss? expected = default(Ss?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableUInt(bool condition, uint? a, uint? b)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();

            uint? result = default(uint?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            uint? expected = default(uint?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableULong(bool condition, ulong? a, ulong? b)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();

            ulong? result = default(ulong?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ulong? expected = default(ulong?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableUShort(bool condition, ushort? a, ushort? b)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();

            ushort? result = default(ushort?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            ushort? expected = default(ushort?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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

        private static void VerifyNullableGenericWithStructRestriction<Ts>(bool condition, Ts? a, Ts? b) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.Condition(
                        Expression.Constant(condition, typeof(bool)),
                        Expression.Constant(a, typeof(Ts?)),
                        Expression.Constant(b, typeof(Ts?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile();

            Ts? result = default(Ts?);
            Exception fEx = null;
            try
            {
                result = f();
            }
            catch (Exception ex)
            {
                fEx = ex;
            }

            Ts? expected = default(Ts?);
            Exception csEx = null;
            try
            {
                expected = condition ? a : b;
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
