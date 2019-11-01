// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryArithmeticNegateTests
    {
        #region Test methods

        [Fact]
        public static void CheckUnaryArithmeticNegateByteTest()
        {
            byte[] values = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateByte(values[i]);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateCharTest()
        {
            char[] values = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateChar(values[i]);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryArithmeticNegateDecimalTest(bool useInterpreter)
        {
            decimal[] values = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateDecimal(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryArithmeticNegateDoubleTest(bool useInterpreter)
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateDouble(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryArithmeticNegateFloatTest(bool useInterpreter)
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateFloat(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryArithmeticNegateIntTest(bool useInterpreter)
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryArithmeticNegateLongTest(bool useInterpreter)
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateLong(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void CheckUnaryArithmeticNegateSByteTest()
        {
            sbyte[] values = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateSByte(values[i]);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryArithmeticNegateShortTest(bool useInterpreter)
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticNegateShort(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.Negate(Expression.Parameter(typeof(int), "x"));
            Assert.Equal("-x", e.ToString());
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticNegateByte(byte value)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Negate(Expression.Constant(value, typeof(byte))));
        }

        private static void VerifyArithmeticNegateChar(char value)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Negate(Expression.Constant(value, typeof(char))));
        }

        private static void VerifyArithmeticNegateDecimal(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Negate(Expression.Constant(value, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);
            Assert.Equal((decimal)(-value), f());
        }

        private static void VerifyArithmeticNegateDouble(double value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Negate(Expression.Constant(value, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);
            Assert.Equal((double)(-value), f());
        }

        private static void VerifyArithmeticNegateFloat(float value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Negate(Expression.Constant(value, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);
            Assert.Equal((float)(-value), f());
        }

        private static void VerifyArithmeticNegateInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Negate(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((int)(-value)), f());
        }

        private static void VerifyArithmeticNegateLong(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Negate(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((long)(-value)), f());
        }

        private static void VerifyArithmeticNegateSByte(sbyte value)
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Negate(Expression.Constant(value, typeof(sbyte))));
        }

        private static void VerifyArithmeticNegateShort(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Negate(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((short)(-value)), f());
        }

        #endregion
    }
}
