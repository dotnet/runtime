// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class NonLiftedComparisonLessThanOrEqualNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanOrEqualNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanOrEqualNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyComparisonLessThanOrEqualNullableByte(byte? a, byte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableChar(char? a, char? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableDouble(double? a, double? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableFloat(float? a, float? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableInt(int? a, int? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableLong(long? a, long? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableShort(short? a, short? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableUInt(uint? a, uint? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanOrEqualNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThanOrEqual(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a <= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
