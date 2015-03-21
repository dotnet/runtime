// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class NonLiftedComparisonGreaterThanNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyComparisonGreaterThanNullableByte(byte? a, byte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableChar(char? a, char? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableDouble(double? a, double? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableFloat(float? a, float? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableInt(int? a, int? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableLong(long? a, long? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableShort(short? a, short? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableUInt(uint? a, uint? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThan(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a > b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
