// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryMultiplyTests
    {
        #region Test methods

        [Fact]
        public static void CheckByteMultiplyTest()
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyByteMultiply(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckSByteMultiplyTest()
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifySByteMultiply(array[i], array[j]);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortMultiplyTest(bool useInterpreter)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUShortMultiply(array[i], array[j], useInterpreter);
                    VerifyUShortMultiplyOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortMultiplyTest(bool useInterpreter)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyShortMultiply(array[i], array[j], useInterpreter);
                    VerifyShortMultiplyOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntMultiplyTest(bool useInterpreter)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUIntMultiply(array[i], array[j], useInterpreter);
                    VerifyUIntMultiplyOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntMultiplyTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyIntMultiply(array[i], array[j], useInterpreter);
                    VerifyIntMultiplyOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongMultiplyTest(bool useInterpreter)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyULongMultiply(array[i], array[j], useInterpreter);
                    VerifyULongMultiplyOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongMultiplyTest(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongMultiply(array[i], array[j], useInterpreter);
                    // we are not calling VerifyLongMultiplyOvf here
                    // because it currently fails on Linux
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongMultiplyTestOvf(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongMultiplyOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatMultiplyTest(bool useInterpreter)
        {
            float[] array = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyFloatMultiply(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleMultiplyTest(bool useInterpreter)
        {
            double[] array = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDoubleMultiply(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalMultiplyTest(bool useInterpreter)
        {
            decimal[] array = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDecimalMultiply(array[i], array[j], useInterpreter);
                }
            }
        }

        [Fact]
        public static void CheckCharMultiplyTest()
        {
            char[] array = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyCharMultiply(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteMultiply(byte a, byte b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte));
            Expression bExp = Expression.Constant(b, typeof(byte));
            Assert.Throws<InvalidOperationException>(() => Expression.Multiply(aExp, bExp));
        }

        private static void VerifySByteMultiply(sbyte a, sbyte b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte));
            Expression bExp = Expression.Constant(b, typeof(sbyte));
            Assert.Throws<InvalidOperationException>(() => Expression.Multiply(aExp, bExp));
        }

        private static void VerifyUShortMultiply(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)(a * b)), f());
        }

        private static void VerifyUShortMultiplyOvf(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = checked((ushort)(a * b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyShortMultiply(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)(a * b)), f());
        }

        private static void VerifyShortMultiplyOvf(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = checked((short)(a * b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());

        }

        private static void VerifyUIntMultiply(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a * b), f());
        }

        private static void VerifyUIntMultiplyOvf(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());

        }

        private static void VerifyIntMultiply(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a * b), f());
        }

        private static void VerifyIntMultiplyOvf(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());

        }

        private static void VerifyULongMultiply(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a * b), f());
        }

        private static void VerifyULongMultiplyOvf(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());

        }

        private static void VerifyLongMultiply(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a * b), f());
        }

        private static void VerifyLongMultiplyOvf(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.MultiplyChecked(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked(a * b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyFloatMultiply(float a, float b, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyDoubleMultiply(double a, double b, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(a * b, f());
        }

        private static void VerifyDecimalMultiply(decimal a, decimal b, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Multiply(
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = a * b;
            }
            catch(OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCharMultiply(char a, char b)
        {
            Expression aExp = Expression.Constant(a, typeof(char));
            Expression bExp = Expression.Constant(b, typeof(char));
            Assert.Throws<InvalidOperationException>(() => Expression.Multiply(aExp, bExp));
        }

        #endregion

        [Fact]
        public static void CannotReduce()
        {
            Expression exp = Expression.Multiply(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceChecked()
        {
            Expression exp = Expression.MultiplyChecked(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Multiply(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.Multiply(Expression.Constant(""), null));
        }

        [Fact]
        public static void CheckedThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.MultiplyChecked(null, Expression.Constant("")));
        }

        [Fact]
        public static void CheckedThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.MultiplyChecked(Expression.Constant(""), null));
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
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.Multiply(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Multiply(Expression.Constant(1), value));
        }

        [Fact]
        public static void CheckedThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.MultiplyChecked(value, Expression.Constant(1)));
        }

        [Fact]
        public static void CheckedThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.MultiplyChecked(Expression.Constant(1), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            BinaryExpression e1 = Expression.Multiply(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a * b)", e1.ToString());

            BinaryExpression e2 = Expression.MultiplyChecked(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a * b)", e2.ToString());
        }
    }
}
