// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class OnesComplementNullableTests
    {
        #region Test methods

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableShortTest()
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableShort(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableUShortTest()
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableUShort(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableIntTest()
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableInt(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableUIntTest()
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableUInt(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableLongTest()
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableLong(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableULongTest()
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableULong(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableByteTest()
        {
            byte?[] values = new byte?[] { null, 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableByte(values[i]);
            }
        }

        [Fact] //[WorkItem(3737, "https://github.com/dotnet/corefx/issues/3737")]
        public static void CheckUnaryArithmeticOnesComplementNullableSByteTest()
        {
            sbyte?[] values = new sbyte?[] { null, 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticOnesComplementNullableSByte(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticOnesComplementNullableShort(short? value)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile();
            Assert.Equal((short?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableUShort(ushort? value)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile();
            Assert.Equal((ushort?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableInt(int? value)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();
            Assert.Equal((int?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableUInt(uint? value)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile();
            Assert.Equal((uint?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableLong(long? value)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile();
            Assert.Equal((long?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableULong(ulong? value)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile();
            Assert.Equal((ulong?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableByte(byte? value)
        {
            Expression<Func<byte?>> e =
                Expression.Lambda<Func<byte?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(byte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte?> f = e.Compile();
            Assert.Equal((byte?)(~value), f());
        }

        private static void VerifyArithmeticOnesComplementNullableSByte(sbyte? value)
        {
            Expression<Func<sbyte?>> e =
                Expression.Lambda<Func<sbyte?>>(
                    Expression.OnesComplement(Expression.Constant(value, typeof(sbyte?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte?> f = e.Compile();
            Assert.Equal((sbyte?)(~value), f());
        }

        #endregion
    }
}
