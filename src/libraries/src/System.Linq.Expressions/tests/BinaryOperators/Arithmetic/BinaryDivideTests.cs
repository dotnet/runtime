// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryDivideTests
    {
        #region Test methods

        [Fact]
        public static void CheckByteDivideTest()
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyByteDivide(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckSByteDivideTest()
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifySByteDivide(array[i], array[j]);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUShortDivideTest(bool useInterpreter)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUShortDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckShortDivideTest(bool useInterpreter)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyShortDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckUIntDivideTest(bool useInterpreter)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUIntDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckIntDivideTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyIntDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckULongDivideTest(bool useInterpreter)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyULongDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckLongDivideTest(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatDivideTest(bool useInterpreter)
        {
            float[] array = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyFloatDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleDivideTest(bool useInterpreter)
        {
            double[] array = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDoubleDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalDivideTest(bool useInterpreter)
        {
            decimal[] array = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDecimalDivide(array[i], array[j], useInterpreter);
                }
            }
        }

        [Fact]
        public static void CheckCharDivideTest()
        {
            char[] array = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyCharDivide(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteDivide(byte a, byte b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte));
            Expression bExp = Expression.Constant(b, typeof(byte));
            Assert.Throws<InvalidOperationException>(() => Expression.Divide(aExp, bExp));
        }

        private static void VerifySByteDivide(sbyte a, sbyte b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte));
            Expression bExp = Expression.Constant(b, typeof(sbyte));
            Assert.Throws<InvalidOperationException>(() => Expression.Divide(aExp, bExp));
        }

        private static void VerifyUShortDivide(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal((ushort)(a / b), f());
        }

        private static void VerifyShortDivide(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(unchecked((short)(a / b)), f());
        }

        private static void VerifyUIntDivide(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyIntDivide(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (b == -1 && a == int.MinValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyULongDivide(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyLongDivide(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else if (b == -1 && a == long.MinValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyFloatDivide(float a, float b, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(a / b, f());
        }

        private static void VerifyDoubleDivide(double a, double b, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(a / b, f());
        }

        private static void VerifyDecimalDivide(decimal a, decimal b, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Divide(
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            if (b == 0)
                Assert.Throws<DivideByZeroException>(() => f());
            else
                Assert.Equal(a / b, f());
        }

        private static void VerifyCharDivide(char a, char b)
        {
            Expression aExp = Expression.Constant(a, typeof(char));
            Expression bExp = Expression.Constant(b, typeof(char));
            Assert.Throws<InvalidOperationException>(() => Expression.Divide(aExp, bExp));
        }

        #endregion

        [Fact]
        public static void CannotReduce()
        {
            Expression exp = Expression.Divide(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Divide(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.Divide(Expression.Constant(""), null));
        }

        private static class Unreadable<T>
        {
            public static T WriteOnly
            {
                set { }
            }
        }

        [Fact]
        public static void ThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.Divide(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Divide(Expression.Constant(1), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            BinaryExpression e = Expression.Divide(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a / b)", e.ToString());
        }
    }
}
