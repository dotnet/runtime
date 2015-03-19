// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class LiftedComparisonNotEqualNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableBoolTest()
        {
            bool?[] values = new bool?[] { null, true, false };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableBool(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonNotEqualNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonNotEqualNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyComparisonNotEqualNullableBool(bool? a, bool? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableByte(byte? a, byte? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableChar(char? a, char? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableDouble(double? a, double? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableFloat(float? a, float? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableInt(int? a, int? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableLong(long? a, long? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableShort(short? a, short? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableUInt(uint? a, uint? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonNotEqualNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.NotEqual(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a != b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        #endregion
    }
}
