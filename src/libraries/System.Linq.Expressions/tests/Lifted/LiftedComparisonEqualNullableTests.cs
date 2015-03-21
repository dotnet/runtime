// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Lifted
{
    public static unsafe class LiftedComparisonEqualNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckLiftedComparisonEqualNullableBoolTest()
        {
            bool?[] values = new bool?[] { null, true, false };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableBool(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableCharTest()
        {
            char?[] values = new char?[] { null, '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableChar(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableDecimalTest()
        {
            decimal?[] values = new decimal?[] { null, decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableDecimal(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableDoubleTest()
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableDouble(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableFloatTest()
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableFloat(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableLong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableSByte(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableShort(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableUInt(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableULong(values[i], values[j]);
                }
            }
        }

        [Fact]
        public static void CheckLiftedComparisonEqualNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyComparisonEqualNullableUShort(values[i], values[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyComparisonEqualNullableBool(bool? a, bool? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(bool?)),
                        Expression.Constant(b, typeof(bool?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableByte(byte? a, byte? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(byte?)),
                        Expression.Constant(b, typeof(byte?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableChar(char? a, char? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(char?)),
                        Expression.Constant(b, typeof(char?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableDecimal(decimal? a, decimal? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(decimal?)),
                        Expression.Constant(b, typeof(decimal?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableDouble(double? a, double? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(double?)),
                        Expression.Constant(b, typeof(double?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableFloat(float? a, float? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(float?)),
                        Expression.Constant(b, typeof(float?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableInt(int? a, int? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(int?)),
                        Expression.Constant(b, typeof(int?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableLong(long? a, long? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(long?)),
                        Expression.Constant(b, typeof(long?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableSByte(sbyte? a, sbyte? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(sbyte?)),
                        Expression.Constant(b, typeof(sbyte?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableShort(short? a, short? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(short?)),
                        Expression.Constant(b, typeof(short?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableUInt(uint? a, uint? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(uint?)),
                        Expression.Constant(b, typeof(uint?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableULong(ulong? a, ulong? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(ulong?)),
                        Expression.Constant(b, typeof(ulong?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        private static void VerifyComparisonEqualNullableUShort(ushort? a, ushort? b)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Equal(
                        Expression.Constant(a, typeof(ushort?)),
                        Expression.Constant(b, typeof(ushort?)),
                        true,
                        null));
            Func<bool?> f = e.Compile();

            bool? expected = a == b;
            bool? result = f();
            Assert.Equal(a == null || b == null ? null : expected, result);
        }

        #endregion
    }
}
