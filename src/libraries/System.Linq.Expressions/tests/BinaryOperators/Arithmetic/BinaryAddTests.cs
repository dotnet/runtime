// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryAddTests
    {
        #region Test methods

        [Fact]
        public static void CheckByteAddTest()
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyByteAdd(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckSByteAddTest()
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifySByteAdd(array[i], array[j]);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortAddTest(bool useInterpreter)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUShortAdd(array[i], array[j], useInterpreter);
                    VerifyUShortAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortAddTest(bool useInterpreter)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyShortAdd(array[i], array[j], useInterpreter);
                    VerifyShortAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntAddTest(bool useInterpreter)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUIntAdd(array[i], array[j], useInterpreter);
                    VerifyUIntAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntAddTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyIntAdd(array[i], array[j], useInterpreter);
                    VerifyIntAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongAddTest(bool useInterpreter)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyULongAdd(array[i], array[j], useInterpreter);
                    VerifyULongAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongAddTest(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongAdd(array[i], array[j], useInterpreter);
                    VerifyLongAddOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatAddTest(bool useInterpreter)
        {
            float[] array = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyFloatAdd(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleAddTest(bool useInterpreter)
        {
            double[] array = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDoubleAdd(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalAddTest(bool useInterpreter)
        {
            decimal[] array = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDecimalAdd(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharAddTest(bool useInterpreter)
        {
            char[] array = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyCharAdd(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteAdd(byte a, byte b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte));
            Expression bExp = Expression.Constant(b, typeof(byte));
            Assert.Throws<InvalidOperationException>(() => Expression.Add(aExp, bExp));
        }

        private static void VerifySByteAdd(sbyte a, sbyte b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte));
            Expression bExp = Expression.Constant(b, typeof(sbyte));
            Assert.Throws<InvalidOperationException>(() => Expression.Add(aExp, bExp));
        }

        private static void VerifyUShortAdd(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());

            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)(a + b)), f());
        }

        private static void VerifyUShortAddOvf(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());

            Func<ushort> f = e.Compile(useInterpreter);

            int expected = a + b;
            if (expected < 0 || expected > ushort.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());

        }

        private static void VerifyShortAdd(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());

            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)(a + b)), f());
        }

        private static void VerifyShortAddOvf(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());

            Func<short> f = e.Compile(useInterpreter);

            int expected = a + b;
            if (expected < short.MinValue || expected > short.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyUIntAdd(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());

            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyUIntAddOvf(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());

            Func<uint> f = e.Compile(useInterpreter);

            long expected = a + (long)b;
            if (expected < 0 || expected > uint.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyIntAdd(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());

            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyIntAddOvf(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());

            Func<int> f = e.Compile(useInterpreter);

            long expected = a + (long)b;
            if (expected < int.MinValue || expected > int.MaxValue)
                Assert.Throws<OverflowException>(() => f());
            else
                Assert.Equal(expected, f());
        }

        private static void VerifyULongAdd(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());

            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyULongAddOvf(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());

            Func<ulong> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked(a + b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyLongAdd(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());

            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a + b), f());
        }

        private static void VerifyLongAddOvf(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.AddChecked(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());

            Func<long> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked(a + b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyFloatAdd(float a, float b, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());

            Func<float> f = e.Compile(useInterpreter);

            float expected = 0;
            try
            {
                expected = checked(a + b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyDoubleAdd(double a, double b, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());

            Func<double> f = e.Compile(useInterpreter);

            double expected = 0;
            try
            {
                expected = checked(a + b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyDecimalAdd(decimal a, decimal b, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Add(
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());

            Func<decimal> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = a + b;
            }
            catch(OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCharAdd(char a, char b)
        {
            Expression aExp = Expression.Constant(a, typeof(char));
            Expression bExp = Expression.Constant(b, typeof(char));
            Assert.Throws<InvalidOperationException>(() => Expression.Add(aExp, bExp));
        }

        #endregion

        [Fact]
        public static void CannotReduce()
        {
            Expression exp = Expression.Add(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceChecked()
        {
            Expression exp = Expression.AddChecked(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Add(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.Add(Expression.Constant(""), null));
        }

        [Fact]
        public static void CheckedThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.AddChecked(null, Expression.Constant("")));
        }

        [Fact]
        public static void CheckedThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.AddChecked(Expression.Constant(""), null));
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
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.Add(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Add(Expression.Constant(1), value));
        }

        [Fact]
        public static void CheckedThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.AddChecked(value, Expression.Constant(1)));
        }

        [Fact]
        public static void CheckedThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Add(Expression.Constant(1), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            BinaryExpression e1 = Expression.Add(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a + b)", e1.ToString());

            BinaryExpression e2 = Expression.AddChecked(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a + b)", e2.ToString());
        }
    }
}
