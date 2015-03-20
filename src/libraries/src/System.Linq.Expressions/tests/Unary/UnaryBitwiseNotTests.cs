// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static unsafe class UnaryBitwiseNotTests
    {
        #region Test methods

        [Fact]
        public static void CheckUnaryBitwiseNotBoolTest()
        {
            bool[] values = new bool[] { false, true };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotBool(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotByteTest()
        {
            byte[] values = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotIntTest()
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotLongTest()
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotLong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotSByteTest()
        {
            sbyte[] values = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotSByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotShortTest()
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotShort(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotUIntTest()
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotUInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotULongTest()
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotULong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryBitwiseNotUShortTest()
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyBitwiseNotUShort(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyBitwiseNotBool(bool value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.Not(Expression.Constant(value, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.Equal((bool)(!value), f());
        }

        private static void VerifyBitwiseNotByte(byte value)
        {
            Expression<Func<byte>> e =
                Expression.Lambda<Func<byte>>(
                    Expression.Not(Expression.Constant(value, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<byte> f = e.Compile();
            Assert.Equal((byte)(~value), f());
        }

        private static void VerifyBitwiseNotInt(int value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Not(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal((int)(~value), f());
        }

        private static void VerifyBitwiseNotLong(long value)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Not(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();
            Assert.Equal((long)(~value), f());
        }

        private static void VerifyBitwiseNotSByte(sbyte value)
        {
            Expression<Func<sbyte>> e =
                Expression.Lambda<Func<sbyte>>(
                    Expression.Not(Expression.Constant(value, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<sbyte> f = e.Compile();
            Assert.Equal((sbyte)(~value), f());
        }

        private static void VerifyBitwiseNotShort(short value)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Not(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();
            Assert.Equal((short)(~value), f());
        }

        private static void VerifyBitwiseNotUInt(uint value)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Not(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();
            Assert.Equal((uint)(~value), f());
        }

        private static void VerifyBitwiseNotULong(ulong value)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Not(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();
            Assert.Equal((ulong)(~value), f());
        }

        private static void VerifyBitwiseNotUShort(ushort value)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Not(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();
            Assert.Equal((ushort)(~value), f());
        }

        #endregion
    }
}
