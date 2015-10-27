// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static class UnaryDecrementTests
    {
        #region Test methods

        [Fact]
        public static void CheckUnaryDecrementShortTest()
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementShort(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryDecrementUShortTest()
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementUShort(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryDecrementIntTest()
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryDecrementUIntTest()
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementUInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryDecrementLongTest()
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementLong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryDecrementULongTest()
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementULong(values[i]);
            }
        }

        [Fact]
        public static void CheckDecrementFloatTest()
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementFloat(values[i]);
            }
        }

        [Fact]
        public static void CheckDecrementDoubleTest()
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementDouble(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyDecrementShort(short value)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Decrement(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();
            Assert.Equal((short)(--value), f());
        }

        private static void VerifyDecrementUShort(ushort value)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Decrement(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();
            Assert.Equal((ushort)(--value), f());
        }

        private static void VerifyDecrementInt(int value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Decrement(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal((int)(--value), f());
        }

        private static void VerifyDecrementUInt(uint value)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Decrement(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();
            Assert.Equal((uint)(--value), f());
        }

        private static void VerifyDecrementLong(long value)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Decrement(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();
            Assert.Equal((long)(--value), f());
        }

        private static void VerifyDecrementULong(ulong value)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Decrement(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();
            Assert.Equal((ulong)(--value), f());
        }

        private static void VerifyDecrementFloat(float value)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Decrement(Expression.Constant(value, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile();
            Assert.Equal((float)(--value), f());
        }

        private static void VerifyDecrementDouble(double value)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Decrement(Expression.Constant(value, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile();
            Assert.Equal((double)(--value), f());
        }

        #endregion
    }
}
