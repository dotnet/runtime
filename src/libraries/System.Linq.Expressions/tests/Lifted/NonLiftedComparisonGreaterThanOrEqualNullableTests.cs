// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class NonLiftedComparisonGreaterThanOrEqualNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckNonLiftedComparisonGreaterThanOrEqualNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonGreaterThanOrEqualNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyComparisonGreaterThanOrEqualNullableByte(byte? a, byte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableChar(char? a, char? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableDouble(double? a, double? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableFloat(float? a, float? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableInt(int? a, int? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableLong(long? a, long? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableShort(short? a, short? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableUInt(uint? a, uint? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        private static void VerifyComparisonGreaterThanOrEqualNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        false,
                        null));
            Func<bool> f = e.Compile();

            bool expected = a >= b;
            bool result = f();
            Assert.Equal(expected, result);
        }

        #endregion
    }
}
