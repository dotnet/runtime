// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LambdaSubtractTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractDecimalTest(bool useInterpreter)
        {
            decimal[] values = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractDoubleTest(bool useInterpreter)
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractFloatTest(bool useInterpreter)
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractIntTest(bool useInterpreter)
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractLongTest(bool useInterpreter)
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractShortTest(bool useInterpreter)
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractUIntTest(bool useInterpreter)
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractULongTest(bool useInterpreter)
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaSubtractUShortTest(bool useInterpreter)
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifySubtractUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        #region Verify decimal

        private static void VerifySubtractDecimal(decimal a, decimal b, bool useInterpreter)
        {
            decimal expected = 0;
            bool overflowed = false;
            try
            {
                expected = a - b;
            }
            catch (OverflowException)
            {
                overflowed = true;
            }

            ParameterExpression p0 = Expression.Parameter(typeof(decimal), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(decimal), "p1");

            // verify with parameters supplied
            Expression<Func<decimal>> e1 =
                Expression.Lambda<Func<decimal>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<decimal, decimal, decimal>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(decimal)),
                    Expression.Constant(b, typeof(decimal))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f1 = e1.Compile(useInterpreter);

            if (overflowed)
            {
                Assert.Throws<OverflowException>(() => f1());
            }
            else
            {
                Assert.Equal(expected, f1());
            }

            // verify with values passed to make parameters
            Expression<Func<decimal, decimal, Func<decimal>>> e2 =
                Expression.Lambda<Func<decimal, decimal, Func<decimal>>>(
                    Expression.Lambda<Func<decimal>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<decimal, decimal, Func<decimal>> f2 = e2.Compile(useInterpreter);

            if (overflowed)
            {
                Assert.Throws<OverflowException>(() => f2(a, b)());
            }
            else
            {
                Assert.Equal(expected, f2(a, b)());
            }

            // verify with values directly passed
            Expression<Func<Func<decimal, decimal, decimal>>> e3 =
                Expression.Lambda<Func<Func<decimal, decimal, decimal>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<decimal, decimal, decimal>>>(
                            Expression.Lambda<Func<decimal, decimal, decimal>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal, decimal, decimal> f3 = e3.Compile(useInterpreter)();

            if (overflowed)
            {
                Assert.Throws<OverflowException>(() => f3(a, b));
            }
            else
            {
                Assert.Equal(expected, f3(a, b));
            }

            // verify as a function generator
            Expression<Func<Func<decimal, decimal, decimal>>> e4 =
                Expression.Lambda<Func<Func<decimal, decimal, decimal>>>(
                    Expression.Lambda<Func<decimal, decimal, decimal>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<decimal, decimal, decimal>> f4 = e4.Compile(useInterpreter);

            if (overflowed)
            {
                Assert.Throws<OverflowException>(() => f4()(a, b));
            }
            else
            {
                Assert.Equal(expected, f4()(a, b));
            }

            // verify with currying
            Expression<Func<decimal, Func<decimal, decimal>>> e5 =
                Expression.Lambda<Func<decimal, Func<decimal, decimal>>>(
                    Expression.Lambda<Func<decimal, decimal>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<decimal, Func<decimal, decimal>> f5 = e5.Compile(useInterpreter);

            if (overflowed)
            {
                Assert.Throws<OverflowException>(() => f5(a)(b));
            }
            else
            {
                Assert.Equal(expected, f5(a)(b));
            }

            // verify with one parameter
            Expression<Func<Func<decimal, decimal>>> e6 =
                Expression.Lambda<Func<Func<decimal, decimal>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<decimal, Func<decimal, decimal>>>(
                            Expression.Lambda<Func<decimal, decimal>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(decimal)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal, decimal> f6 = e6.Compile(useInterpreter)();

            if (overflowed)
            {
                Assert.Throws<OverflowException>(() => f6(b));
            }
            else
            {
                Assert.Equal(expected, f6(b));
            }
        }

        #endregion


        #region Verify double

        private static void VerifySubtractDouble(double a, double b, bool useInterpreter)
        {
            double expected = a - b;

            ParameterExpression p0 = Expression.Parameter(typeof(double), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(double), "p1");

            // verify with parameters supplied
            Expression<Func<double>> e1 =
                Expression.Lambda<Func<double>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<double, double, double>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(double)),
                    Expression.Constant(b, typeof(double))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<double, double, Func<double>>> e2 =
                Expression.Lambda<Func<double, double, Func<double>>>(
                    Expression.Lambda<Func<double>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<double, double, Func<double>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<double, double, double>>> e3 =
                Expression.Lambda<Func<Func<double, double, double>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<double, double, double>>>(
                            Expression.Lambda<Func<double, double, double>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<double, double, double> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<double, double, double>>> e4 =
                Expression.Lambda<Func<Func<double, double, double>>>(
                    Expression.Lambda<Func<double, double, double>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<double, double, double>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<double, Func<double, double>>> e5 =
                Expression.Lambda<Func<double, Func<double, double>>>(
                    Expression.Lambda<Func<double, double>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<double, Func<double, double>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<double, double>>> e6 =
                Expression.Lambda<Func<Func<double, double>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<double, Func<double, double>>>(
                            Expression.Lambda<Func<double, double>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(double)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<double, double> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify float

        private static void VerifySubtractFloat(float a, float b, bool useInterpreter)
        {
            float expected = a - b;

            ParameterExpression p0 = Expression.Parameter(typeof(float), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(float), "p1");

            // verify with parameters supplied
            Expression<Func<float>> e1 =
                Expression.Lambda<Func<float>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<float, float, float>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(float)),
                    Expression.Constant(b, typeof(float))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<float, float, Func<float>>> e2 =
                Expression.Lambda<Func<float, float, Func<float>>>(
                    Expression.Lambda<Func<float>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<float, float, Func<float>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<float, float, float>>> e3 =
                Expression.Lambda<Func<Func<float, float, float>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<float, float, float>>>(
                            Expression.Lambda<Func<float, float, float>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<float, float, float> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<float, float, float>>> e4 =
                Expression.Lambda<Func<Func<float, float, float>>>(
                    Expression.Lambda<Func<float, float, float>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<float, float, float>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<float, Func<float, float>>> e5 =
                Expression.Lambda<Func<float, Func<float, float>>>(
                    Expression.Lambda<Func<float, float>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<float, Func<float, float>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<float, float>>> e6 =
                Expression.Lambda<Func<Func<float, float>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<float, Func<float, float>>>(
                            Expression.Lambda<Func<float, float>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(float)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<float, float> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify int

        private static void VerifySubtractInt(int a, int b, bool useInterpreter)
        {
            int expected = unchecked(a - b);

            ParameterExpression p0 = Expression.Parameter(typeof(int), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(int), "p1");

            // verify with parameters supplied
            Expression<Func<int>> e1 =
                Expression.Lambda<Func<int>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<int, int, int>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(int)),
                    Expression.Constant(b, typeof(int))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<int, int, Func<int>>> e2 =
                Expression.Lambda<Func<int, int, Func<int>>>(
                    Expression.Lambda<Func<int>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<int, int, Func<int>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<int, int, int>>> e3 =
                Expression.Lambda<Func<Func<int, int, int>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<int, int, int>>>(
                            Expression.Lambda<Func<int, int, int>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<int, int, int> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<int, int, int>>> e4 =
                Expression.Lambda<Func<Func<int, int, int>>>(
                    Expression.Lambda<Func<int, int, int>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<int, int, int>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<int, Func<int, int>>> e5 =
                Expression.Lambda<Func<int, Func<int, int>>>(
                    Expression.Lambda<Func<int, int>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<int, Func<int, int>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<int, int>>> e6 =
                Expression.Lambda<Func<Func<int, int>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<int, Func<int, int>>>(
                            Expression.Lambda<Func<int, int>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(int)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<int, int> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify long

        private static void VerifySubtractLong(long a, long b, bool useInterpreter)
        {
            long expected = unchecked(a - b);

            ParameterExpression p0 = Expression.Parameter(typeof(long), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(long), "p1");

            // verify with parameters supplied
            Expression<Func<long>> e1 =
                Expression.Lambda<Func<long>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<long, long, long>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(long)),
                    Expression.Constant(b, typeof(long))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<long, long, Func<long>>> e2 =
                Expression.Lambda<Func<long, long, Func<long>>>(
                    Expression.Lambda<Func<long>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<long, long, Func<long>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<long, long, long>>> e3 =
                Expression.Lambda<Func<Func<long, long, long>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<long, long, long>>>(
                            Expression.Lambda<Func<long, long, long>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<long, long, long> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<long, long, long>>> e4 =
                Expression.Lambda<Func<Func<long, long, long>>>(
                    Expression.Lambda<Func<long, long, long>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<long, long, long>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<long, Func<long, long>>> e5 =
                Expression.Lambda<Func<long, Func<long, long>>>(
                    Expression.Lambda<Func<long, long>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<long, Func<long, long>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<long, long>>> e6 =
                Expression.Lambda<Func<Func<long, long>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<long, Func<long, long>>>(
                            Expression.Lambda<Func<long, long>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(long)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<long, long> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify short

        private static void VerifySubtractShort(short a, short b, bool useInterpreter)
        {
            short expected = unchecked((short)(a - b));

            ParameterExpression p0 = Expression.Parameter(typeof(short), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(short), "p1");

            // verify with parameters supplied
            Expression<Func<short>> e1 =
                Expression.Lambda<Func<short>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<short, short, short>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(short)),
                    Expression.Constant(b, typeof(short))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<short, short, Func<short>>> e2 =
                Expression.Lambda<Func<short, short, Func<short>>>(
                    Expression.Lambda<Func<short>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<short, short, Func<short>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<short, short, short>>> e3 =
                Expression.Lambda<Func<Func<short, short, short>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<short, short, short>>>(
                            Expression.Lambda<Func<short, short, short>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<short, short, short> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<short, short, short>>> e4 =
                Expression.Lambda<Func<Func<short, short, short>>>(
                    Expression.Lambda<Func<short, short, short>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<short, short, short>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<short, Func<short, short>>> e5 =
                Expression.Lambda<Func<short, Func<short, short>>>(
                    Expression.Lambda<Func<short, short>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<short, Func<short, short>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<short, short>>> e6 =
                Expression.Lambda<Func<Func<short, short>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<short, Func<short, short>>>(
                            Expression.Lambda<Func<short, short>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(short)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<short, short> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify uint

        private static void VerifySubtractUInt(uint a, uint b, bool useInterpreter)
        {
            uint expected = unchecked(a - b);

            ParameterExpression p0 = Expression.Parameter(typeof(uint), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(uint), "p1");

            // verify with parameters supplied
            Expression<Func<uint>> e1 =
                Expression.Lambda<Func<uint>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<uint, uint, uint>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(uint)),
                    Expression.Constant(b, typeof(uint))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<uint, uint, Func<uint>>> e2 =
                Expression.Lambda<Func<uint, uint, Func<uint>>>(
                    Expression.Lambda<Func<uint>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<uint, uint, Func<uint>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<uint, uint, uint>>> e3 =
                Expression.Lambda<Func<Func<uint, uint, uint>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<uint, uint, uint>>>(
                            Expression.Lambda<Func<uint, uint, uint>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint, uint, uint> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<uint, uint, uint>>> e4 =
                Expression.Lambda<Func<Func<uint, uint, uint>>>(
                    Expression.Lambda<Func<uint, uint, uint>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<uint, uint, uint>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<uint, Func<uint, uint>>> e5 =
                Expression.Lambda<Func<uint, Func<uint, uint>>>(
                    Expression.Lambda<Func<uint, uint>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<uint, Func<uint, uint>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<uint, uint>>> e6 =
                Expression.Lambda<Func<Func<uint, uint>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<uint, Func<uint, uint>>>(
                            Expression.Lambda<Func<uint, uint>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(uint)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint, uint> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify ulong

        private static void VerifySubtractULong(ulong a, ulong b, bool useInterpreter)
        {
            ulong expected = unchecked(a - b);

            ParameterExpression p0 = Expression.Parameter(typeof(ulong), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(ulong), "p1");

            // verify with parameters supplied
            Expression<Func<ulong>> e1 =
                Expression.Lambda<Func<ulong>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ulong, ulong, ulong>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(ulong)),
                    Expression.Constant(b, typeof(ulong))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<ulong, ulong, Func<ulong>>> e2 =
                Expression.Lambda<Func<ulong, ulong, Func<ulong>>>(
                    Expression.Lambda<Func<ulong>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<ulong, ulong, Func<ulong>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<ulong, ulong, ulong>>> e3 =
                Expression.Lambda<Func<Func<ulong, ulong, ulong>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ulong, ulong, ulong>>>(
                            Expression.Lambda<Func<ulong, ulong, ulong>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong, ulong, ulong> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<ulong, ulong, ulong>>> e4 =
                Expression.Lambda<Func<Func<ulong, ulong, ulong>>>(
                    Expression.Lambda<Func<ulong, ulong, ulong>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ulong, ulong, ulong>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<ulong, Func<ulong, ulong>>> e5 =
                Expression.Lambda<Func<ulong, Func<ulong, ulong>>>(
                    Expression.Lambda<Func<ulong, ulong>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<ulong, Func<ulong, ulong>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<ulong, ulong>>> e6 =
                Expression.Lambda<Func<Func<ulong, ulong>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ulong, Func<ulong, ulong>>>(
                            Expression.Lambda<Func<ulong, ulong>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(ulong)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong, ulong> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify ushort

        private static void VerifySubtractUShort(ushort a, ushort b, bool useInterpreter)
        {
            ushort expected = unchecked((ushort)(a - b));

            ParameterExpression p0 = Expression.Parameter(typeof(ushort), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(ushort), "p1");

            // verify with parameters supplied
            Expression<Func<ushort>> e1 =
                Expression.Lambda<Func<ushort>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ushort, ushort, ushort>>(
                            Expression.Subtract(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(ushort)),
                    Expression.Constant(b, typeof(ushort))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f1 = e1.Compile(useInterpreter);

            Assert.Equal(expected, f1());

            // verify with values passed to make parameters
            Expression<Func<ushort, ushort, Func<ushort>>> e2 =
                Expression.Lambda<Func<ushort, ushort, Func<ushort>>>(
                    Expression.Lambda<Func<ushort>>(
                        Expression.Subtract(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<ushort, ushort, Func<ushort>> f2 = e2.Compile(useInterpreter);

            Assert.Equal(expected, f2(a, b)());

            // verify with values directly passed
            Expression<Func<Func<ushort, ushort, ushort>>> e3 =
                Expression.Lambda<Func<Func<ushort, ushort, ushort>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ushort, ushort, ushort>>>(
                            Expression.Lambda<Func<ushort, ushort, ushort>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort, ushort, ushort> f3 = e3.Compile(useInterpreter)();

            Assert.Equal(expected, f3(a, b));

            // verify as a function generator
            Expression<Func<Func<ushort, ushort, ushort>>> e4 =
                Expression.Lambda<Func<Func<ushort, ushort, ushort>>>(
                    Expression.Lambda<Func<ushort, ushort, ushort>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ushort, ushort, ushort>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<ushort, Func<ushort, ushort>>> e5 =
                Expression.Lambda<Func<ushort, Func<ushort, ushort>>>(
                    Expression.Lambda<Func<ushort, ushort>>(
                        Expression.Subtract(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<ushort, Func<ushort, ushort>> f5 = e5.Compile(useInterpreter);

            Assert.Equal(expected, f5(a)(b));

            // verify with one parameter
            Expression<Func<Func<ushort, ushort>>> e6 =
                Expression.Lambda<Func<Func<ushort, ushort>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ushort, Func<ushort, ushort>>>(
                            Expression.Lambda<Func<ushort, ushort>>(
                                Expression.Subtract(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(ushort)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort, ushort> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #endregion
    }
}
