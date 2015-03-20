// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static unsafe class UnaryBitwiseNotNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckUnaryBitwiseNotNullableBoolTest()
        {
            bool?[] values = new bool?[] { null, false, true };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableBool(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableLong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableSByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableShort(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableUInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableULong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotNullableUShort(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBitwiseNotNullableBool(bool? value)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.Not(Expression.Constant(value, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile();
            Assert.Equal((bool?)(!value), f());
        }

        private static void VerifyBitwiseNotNullableByte(byte? value)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.Not(Expression.Constant(value, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile();
            Assert.Equal((byte?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableInt(int? value)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Not(Expression.Constant(value, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();
            Assert.Equal((int?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableLong(long? value)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Not(Expression.Constant(value, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();
            Assert.Equal((long?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableSByte(sbyte? value)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.Not(Expression.Constant(value, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile();
            Assert.Equal((sbyte?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableShort(short? value)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Not(Expression.Constant(value, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();
            Assert.Equal((short?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableUInt(uint? value)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Not(Expression.Constant(value, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();
            Assert.Equal((uint?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableULong(ulong? value)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Not(Expression.Constant(value, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();
            Assert.Equal((ulong?)(~value), f());
        }

        private static void VerifyBitwiseNotNullableUShort(ushort? value)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Not(Expression.Constant(value, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();
            Assert.Equal((ushort?)(~value), f());
        }

        #endregion
    }
}
