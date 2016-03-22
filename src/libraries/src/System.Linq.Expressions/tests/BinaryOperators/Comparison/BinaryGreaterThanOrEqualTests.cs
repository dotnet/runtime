// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class BinaryGreaterThanOrEqualTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckByteGreaterThanOrEqualTest(bool useInterpreter)
        {
            byte[] array = new byte[] { 0, 1, byte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyByteGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckCharGreaterThanOrEqualTest(bool useInterpreter)
        {
            char[] array = new char[] { '\0', '\b', 'A', '\uffff' };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyCharGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecimalGreaterThanOrEqualTest(bool useInterpreter)
        {
            decimal[] array = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDecimalGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDoubleGreaterThanOrEqualTest(bool useInterpreter)
        {
            double[] array = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyDoubleGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckFloatGreaterThanOrEqualTest(bool useInterpreter)
        {
            float[] array = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyFloatGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIntGreaterThanOrEqualTest(bool useInterpreter)
        {
            int[] array = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyIntGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongGreaterThanOrEqualTest(bool useInterpreter)
        {
            long[] array = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyLongGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckSByteGreaterThanOrEqualTest(bool useInterpreter)
        {
            sbyte[] array = new sbyte[] { 0, 1, -1, sbyte.MinValue, sbyte.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifySByteGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckShortGreaterThanOrEqualTest(bool useInterpreter)
        {
            short[] array = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyShortGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUIntGreaterThanOrEqualTest(bool useInterpreter)
        {
            uint[] array = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUIntGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckULongGreaterThanOrEqualTest(bool useInterpreter)
        {
            ulong[] array = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyULongGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUShortGreaterThanOrEqualTest(bool useInterpreter)
        {
            ushort[] array = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                for (int j = 0; j < array.Length; j++)
                {
                    VerifyUShortGreaterThanOrEqual(array[i], array[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyByteGreaterThanOrEqual(byte a, byte b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(byte)),
                        Expression.Constant(b, typeof(byte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyCharGreaterThanOrEqual(char a, char b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(char)),
                        Expression.Constant(b, typeof(char))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyDecimalGreaterThanOrEqual(decimal a, decimal b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(decimal)),
                        Expression.Constant(b, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyDoubleGreaterThanOrEqual(double a, double b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(double)),
                        Expression.Constant(b, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyFloatGreaterThanOrEqual(float a, float b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(float)),
                        Expression.Constant(b, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyIntGreaterThanOrEqual(int a, int b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(int)),
                        Expression.Constant(b, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyLongGreaterThanOrEqual(long a, long b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(long)),
                        Expression.Constant(b, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifySByteGreaterThanOrEqual(sbyte a, sbyte b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(sbyte)),
                        Expression.Constant(b, typeof(sbyte))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyShortGreaterThanOrEqual(short a, short b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(short)),
                        Expression.Constant(b, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyUIntGreaterThanOrEqual(uint a, uint b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(uint)),
                        Expression.Constant(b, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyULongGreaterThanOrEqual(ulong a, ulong b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(ulong)),
                        Expression.Constant(b, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        private static void VerifyUShortGreaterThanOrEqual(ushort a, ushort b, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.GreaterThanOrEqual(
                        Expression.Constant(a, typeof(ushort)),
                        Expression.Constant(b, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);

            Assert.Equal(a >= b, f());
        }

        #endregion

        [Fact]
        public static void CannotReduce()
        {
            Expression exp = Expression.GreaterThanOrEqual(Expression.Constant(0), Expression.Constant(0));
            Assert.False(exp.CanReduce);
            Assert.Same(exp, exp.Reduce());
            Assert.Throws<ArgumentException>(null, () => exp.ReduceAndCheck());
        }

        [Fact]
        public static void ThrowsOnLeftNull()
        {
            Assert.Throws<ArgumentNullException>("left", () => Expression.GreaterThanOrEqual(null, Expression.Constant(0)));
        }

        [Fact]
        public static void ThrowsOnRightNull()
        {
            Assert.Throws<ArgumentNullException>("right", () => Expression.GreaterThanOrEqual(Expression.Constant(0), null));
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
            Assert.Throws<ArgumentException>("left", () => Expression.GreaterThanOrEqual(value, Expression.Constant(1)));
        }

        [Fact]
        public static void ThrowsOnRightUnreadable()
        {
            Expression value = Expression.Property(null, typeof(Unreadable<int>), "WriteOnly");
            Assert.Throws<ArgumentException>("right", () => Expression.GreaterThanOrEqual(Expression.Constant(1), value));
        }
    }
}
