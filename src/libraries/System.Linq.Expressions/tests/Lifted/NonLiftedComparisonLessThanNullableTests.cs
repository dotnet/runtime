// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class NonLiftedComparisonLessThanNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonLessThanNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonLessThanNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyComparisonLessThanNullableByte(byte? a, byte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableChar(char? a, char? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableDouble(double? a, double? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableFloat(float? a, float? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableInt(int? a, int? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableLong(long? a, long? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableShort(short? a, short? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableUInt(uint? a, uint? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonLessThanNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.LessThan(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a < b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
