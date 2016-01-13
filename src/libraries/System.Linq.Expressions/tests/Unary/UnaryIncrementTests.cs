// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryIncrementTests
    {
        #region Test methods

        [Fact]
        public static void CheckUnaryIncrementShortTest()
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementShort(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryIncrementUShortTest()
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementUShort(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryIncrementIntTest()
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryIncrementUIntTest()
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementUInt(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryIncrementLongTest()
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementLong(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryIncrementULongTest()
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementULong(values[i]);
            }
        }

        [Fact]
        public static void CheckIncrementFloatTest()
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementFloat(values[i]);
            }
        }

        [Fact]
        public static void CheckIncrementDoubleTest()
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementDouble(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyIncrementShort(short value)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Increment(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile();
            Assert.Equal((short)(++value), f());
        }

        private static void VerifyIncrementUShort(ushort value)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Increment(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile();
            Assert.Equal((ushort)(++value), f());
        }

        private static void VerifyIncrementInt(int value)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Increment(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile();
            Assert.Equal((int)(++value), f());
        }

        private static void VerifyIncrementUInt(uint value)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Increment(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile();
            Assert.Equal((uint)(++value), f());
        }

        private static void VerifyIncrementLong(long value)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Increment(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile();
            Assert.Equal((long)(++value), f());
        }

        private static void VerifyIncrementULong(ulong value)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Increment(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile();
            Assert.Equal((ulong)(++value), f());
        }

        private static void VerifyIncrementFloat(float value)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Increment(Expression.Constant(value, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile();
            Assert.Equal((float)(++value), f());
        }

        private static void VerifyIncrementDouble(double value)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Increment(Expression.Constant(value, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile();
            Assert.Equal((double)(++value), f());
        }

        #endregion
    }
}
