// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinarySubtractTests
    {
        #region Test methods

        [Fact]
        public static void CheckByteSubtractTest()
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyByteSubtract(array[i], array[j]);
                }
            }
        }

        [Fact]
        public static void CheckSByteSubtractTest()
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifySByteSubtract(array[i], array[j]);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortSubtractTest(bool useInterpreter)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUShortSubtract(array[i], array[j], useInterpreter);
                    VerifyUShortSubtractOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortSubtractTest(bool useInterpreter)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyShortSubtract(array[i], array[j], useInterpreter);
                    VerifyShortSubtractOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntSubtractTest(bool useInterpreter)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUIntSubtract(array[i], array[j], useInterpreter);
                    VerifyUIntSubtractOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntSubtractTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyIntSubtract(array[i], array[j], useInterpreter);
                    VerifyIntSubtractOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongSubtractTest(bool useInterpreter)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyULongSubtract(array[i], array[j], useInterpreter);
                    VerifyULongSubtractOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongSubtractTest(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongSubtract(array[i], array[j], useInterpreter);
                    VerifyLongSubtractOvf(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatSubtractTest(bool useInterpreter)
        {
            float[] array = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyFloatSubtract(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleSubtractTest(bool useInterpreter)
        {
            double[] array = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDoubleSubtract(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalSubtractTest(bool useInterpreter)
        {
            decimal[] array = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDecimalSubtract(array[i], array[j], useInterpreter);
                }
            }
        }

        [Fact]
        public static void CheckCharSubtractTest()
        {
            char[] array = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyCharSubtract(array[i], array[j]);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteSubtract(byte a, byte b)
        {
            Expression aExp = Expression.Constant(a, typeof(byte));
            Expression bExp = Expression.Constant(b, typeof(byte));
            Assert.Throws<InvalidOperationException>(() => Expression.Subtract(aExp, bExp));
        }

        private static void VerifySByteSubtract(sbyte a, sbyte b)
        {
            Expression aExp = Expression.Constant(a, typeof(sbyte));
            Expression bExp = Expression.Constant(b, typeof(sbyte));
            Assert.Throws<InvalidOperationException>(() => Expression.Subtract(aExp, bExp));
        }

        private static void VerifyUShortSubtract(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((ushort)(a - b)), f());
        }

        private static void VerifyUShortSubtractOvf(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);

            ushort expected = 0;
            try
            {
                expected = checked((ushort)(a - b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyShortSubtract(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked((short)(a - b)), f());
        }

        private static void VerifyShortSubtractOvf(short a, short b, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);

            short expected = 0;
            try
            {
                expected = checked((short)(a - b));
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyUIntSubtract(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a - b), f());
        }

        private static void VerifyUIntSubtractOvf(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);

            uint expected = 0;
            try
            {
                expected = checked(a - b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());

        }

        private static void VerifyIntSubtract(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a - b), f());
        }

        private static void VerifyIntSubtractOvf(int a, int b, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);

            int expected = 0;
            try
            {
                expected = checked(a - b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyULongSubtract(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a - b), f());
        }

        private static void VerifyULongSubtractOvf(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);

            ulong expected = 0;
            try
            {
                expected = checked(a - b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());

        }

        private static void VerifyLongSubtract(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            Assert.Equal(unchecked(a - b), f());
        }

        private static void VerifyLongSubtractOvf(long a, long b, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.SubtractChecked(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);

            long expected = 0;
            try
            {
                expected = checked(a - b);
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyFloatSubtract(float a, float b, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);

            Assert.Equal(a - b, f());
        }

        private static void VerifyDoubleSubtract(double a, double b, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);

            Assert.Equal(a - b, f());
        }

        private static void VerifyDecimalSubtract(decimal a, decimal b, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.Subtract(
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);

            decimal expected = 0;
            try
            {
                expected = a - b;
            }
            catch (OverflowException)
            {
                Assert.Throws<OverflowException>(() => f());
                return;
            }

            Assert.Equal(expected, f());
        }

        private static void VerifyCharSubtract(char a, char b)
        {
            Expression aExp = Expression.Constant(a, typeof(char));
            Expression bExp = Expression.Constant(b, typeof(char));
            Assert.Throws<InvalidOperationException>(() => Expression.Subtract(aExp, bExp));
        }

        #endregion

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Subtract_MultipleOverloads_CorrectlyResolvesOperator1(bool useInterpreter)
        {
            BinaryExpression subtract = Expression.Subtract(Expression.Constant(new DateTime(100)), Expression.Constant(new DateTime(10)));
            Func<TimeSpan> lambda = Expression.Lambda<Func<TimeSpan>>(subtract).Compile(useInterpreter);
            Assert.Equal(new TimeSpan(90), lambda());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void Subtract_MultipleOverloads_CorrectlyResolvesOperator2(bool useInterpreter)
        {
            BinaryExpression subtract = Expression.Subtract(Expression.Constant(new DateTime(100)), Expression.Constant(new TimeSpan(10)));
            Func<DateTime> lambda = Expression.Lambda<Func<DateTime>>(subtract).Compile(useInterpreter);
            Assert.Equal(new DateTime(90), lambda());
        }

        [Fact]
        public static void Subtract_NoSuchOperatorDeclaredOnType_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => Expression.Add(Expression.Constant(new SubClass(0)), Expression.Constant(new SubClass(1))));
        }

        public class BaseClass
        {
            public BaseClass(int value) { Value = value; }
            public int Value { get; }

            public static BaseClass operator -(BaseClass i1, BaseClass i2) => new BaseClass(i1.Value - i2.Value);
        }

        public class SubClass : BaseClass
        {
            public SubClass(int value) : base(value) { }
        }

        [Fact]
        public static void CannotReduce()
        {
            Expression exp = Expression.Subtract(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void CannotReduceChecked()
        {
            Expression exp = Expression.SubtractChecked(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            AssertExtensions.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.Subtract(null, Expression.Constant("")));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.Subtract(Expression.Constant(""), null));
        }

        [Fact]
        public static void CheckedThrowsOnLeftNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("left", () => Expression.SubtractChecked(null, Expression.Constant("")));
        }

        [Fact]
        public static void CheckedThrowsOnRightNull()
        {
            AssertExtensions.Throws<ArgumentNullException>("right", () => Expression.SubtractChecked(Expression.Constant(""), null));
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
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.Subtract(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.Subtract(Expression.Constant(1), value));
        }

        [Fact]
        public static void CheckedThrowsOnLeftUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("left", () => Expression.SubtractChecked(value, Expression.Constant(1)));
        }

        [Fact]
        public static void CheckedThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            AssertExtensions.Throws<ArgumentException>("right", () => Expression.SubtractChecked(Expression.Constant(1), value));
        }

        [Fact]
        public static void ToStringTest()
        {
            BinaryExpression e1 = Expression.Subtract(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a - b)", e1.ToString());

            BinaryExpression e2 = Expression.SubtractChecked(Expression.Parameter(typeof(int), "a"), Expression.Parameter(typeof(int), "b"));
            Assert.Equal("(a - b)", e2.ToString());
        }
    }
}
