// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class LambdaModuloTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloDecimalTest(bool useInterpreter)
        {
            decimal[] values = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloDecimal(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloDoubleTest(bool useInterpreter)
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloDouble(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloFloatTest(bool useInterpreter)
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloFloat(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloIntTest(bool useInterpreter)
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloLongTest(bool useInterpreter)
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloLong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloShortTest(bool useInterpreter)
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloShort(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloUIntTest(bool useInterpreter)
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloUInt(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloULongTest(bool useInterpreter)
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloULong(values[i], values[j], useInterpreter);
                }
            }
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void LambdaModuloUShortTest(bool useInterpreter)
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                for (int j = 0; j < values.Length; j++)
                {
                    VerifyModuloUShort(values[i], values[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Test verifiers

        private enum ResultType
        {
            Success,
            DivideByZero,
            Overflow
        }

        #region Verify decimal

        private static void VerifyModuloDecimal(decimal a, decimal b, bool useInterpreter)
        {
            bool divideByZero;
            decimal expected;
            if (b == 0)
            {
                divideByZero = true;
                expected = 0;
            }
            else
            {
                divideByZero = false;
                expected = a % b;
            }

            ParameterExpression p0 = Expression.Parameter(typeof(decimal), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(decimal), "p1");

            // verify with parameters supplied
            Expression<Func<decimal>> e1 =
                Expression.Lambda<Func<decimal>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<decimal, decimal, decimal>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(decimal)),
                    Expression.Constant(b, typeof(decimal))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f1 = e1.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f1());
            }
            else
            {
                Assert.Equal(expected, f1());
            }

            // verify with values passed to make parameters
            Expression<Func<decimal, decimal, Func<decimal>>> e2 =
                Expression.Lambda<Func<decimal, decimal, Func<decimal>>>(
                    Expression.Lambda<Func<decimal>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<decimal, decimal, Func<decimal>> f2 = e2.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f2(a, b)());
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
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal, decimal, decimal> f3 = e3.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f3(a, b));
            }
            else
            {
                Assert.Equal(expected, f3(a, b));
            }

            // verify as a function generator
            Expression<Func<Func<decimal, decimal, decimal>>> e4 =
                Expression.Lambda<Func<Func<decimal, decimal, decimal>>>(
                    Expression.Lambda<Func<decimal, decimal, decimal>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<decimal, decimal, decimal>> f4 = e4.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f4()(a, b));
            }
            else
            {
                Assert.Equal(expected, f4()(a, b));
            }

            // verify with currying
            Expression<Func<decimal, Func<decimal, decimal>>> e5 =
                Expression.Lambda<Func<decimal, Func<decimal, decimal>>>(
                    Expression.Lambda<Func<decimal, decimal>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<decimal, Func<decimal, decimal>> f5 = e5.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f5(a)(b));
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
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(decimal)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal, decimal> f6 = e6.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f6(b));
            }
            else
            {
                Assert.Equal(expected, f6(b));
            }
        }

        #endregion


        #region Verify double

        private static void VerifyModuloDouble(double a, double b, bool useInterpreter)
        {
            double expected = a % b;

            ParameterExpression p0 = Expression.Parameter(typeof(double), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(double), "p1");

            // verify with parameters supplied
            Expression<Func<double>> e1 =
                Expression.Lambda<Func<double>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<double, double, double>>(
                            Expression.Modulo(p0, p1),
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
                        Expression.Modulo(p0, p1),
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
                                Expression.Modulo(p0, p1),
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
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<double, double, double>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<double, Func<double, double>>> e5 =
                Expression.Lambda<Func<double, Func<double, double>>>(
                    Expression.Lambda<Func<double, double>>(
                        Expression.Modulo(p0, p1),
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
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(double)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<double, double> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify float

        private static void VerifyModuloFloat(float a, float b, bool useInterpreter)
        {
            float expected = a % b;

            ParameterExpression p0 = Expression.Parameter(typeof(float), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(float), "p1");

            // verify with parameters supplied
            Expression<Func<float>> e1 =
                Expression.Lambda<Func<float>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<float, float, float>>(
                            Expression.Modulo(p0, p1),
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
                        Expression.Modulo(p0, p1),
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
                                Expression.Modulo(p0, p1),
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
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<float, float, float>> f4 = e4.Compile(useInterpreter);

            Assert.Equal(expected, f4()(a, b));

            // verify with currying
            Expression<Func<float, Func<float, float>>> e5 =
                Expression.Lambda<Func<float, Func<float, float>>>(
                    Expression.Lambda<Func<float, float>>(
                        Expression.Modulo(p0, p1),
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
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(float)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<float, float> f6 = e6.Compile(useInterpreter)();

            Assert.Equal(expected, f6(b));
        }

        #endregion


        #region Verify int

        private static void VerifyModuloInt(int a, int b, bool useInterpreter)
        {
            ResultType outcome;
            int expected = 0;
            if (b == 0)
            {
                outcome = ResultType.DivideByZero;
            }
            else if (a == int.MinValue && b == -1)
            {
                outcome = ResultType.Overflow;
            }
            else
            {
                expected = a % b;
                outcome = ResultType.Success;
            }

            ParameterExpression p0 = Expression.Parameter(typeof(int), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(int), "p1");

            // verify with parameters supplied
            Expression<Func<int>> e1 =
                Expression.Lambda<Func<int>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<int, int, int>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(int)),
                    Expression.Constant(b, typeof(int))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f1 = e1.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f1());
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f1());
                    break;
                default:
                    Assert.Equal(expected, f1());
                    break;
            }

            // verify with values passed to make parameters
            Expression<Func<int, int, Func<int>>> e2 =
                Expression.Lambda<Func<int, int, Func<int>>>(
                    Expression.Lambda<Func<int>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<int, int, Func<int>> f2 = e2.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f2(a, b)());
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f2(a, b)());
                    break;
                default:
                    Assert.Equal(expected, f2(a, b)());
                    break;
            }

            // verify with values directly passed
            Expression<Func<Func<int, int, int>>> e3 =
                Expression.Lambda<Func<Func<int, int, int>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<int, int, int>>>(
                            Expression.Lambda<Func<int, int, int>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<int, int, int> f3 = e3.Compile(useInterpreter)();

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f3(a, b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f3(a, b));
                    break;
                default:
                    Assert.Equal(expected, f3(a, b));
                    break;
            }

            // verify as a function generator
            Expression<Func<Func<int, int, int>>> e4 =
                Expression.Lambda<Func<Func<int, int, int>>>(
                    Expression.Lambda<Func<int, int, int>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<int, int, int>> f4 = e4.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f4()(a, b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f4()(a, b));
                    break;
                default:
                    Assert.Equal(expected, f4()(a, b));
                    break;
            }

            // verify with currying
            Expression<Func<int, Func<int, int>>> e5 =
                Expression.Lambda<Func<int, Func<int, int>>>(
                    Expression.Lambda<Func<int, int>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<int, Func<int, int>> f5 = e5.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f5(a)(b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f5(a)(b));
                    break;
                default:
                    Assert.Equal(expected, f5(a)(b));
                    break;
            }

            // verify with one parameter
            Expression<Func<Func<int, int>>> e6 =
                Expression.Lambda<Func<Func<int, int>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<int, Func<int, int>>>(
                            Expression.Lambda<Func<int, int>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(int)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<int, int> f6 = e6.Compile(useInterpreter)();

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f6(b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f6(b));
                    break;
                default:
                    Assert.Equal(expected, f6(b));
                    break;
            }
        }

        #endregion


        #region Verify long

        private static void VerifyModuloLong(long a, long b, bool useInterpreter)
        {
            ResultType outcome;
            long expected = 0;
            if (b == 0)
            {
                outcome = ResultType.DivideByZero;
            }
            else if (a == long.MinValue && b == -1)
            {
                outcome = ResultType.Overflow;
            }
            else
            {
                expected = a % b;
                outcome = ResultType.Success;
            }

            ParameterExpression p0 = Expression.Parameter(typeof(long), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(long), "p1");

            // verify with parameters supplied
            Expression<Func<long>> e1 =
                Expression.Lambda<Func<long>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<long, long, long>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(long)),
                    Expression.Constant(b, typeof(long))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f1 = e1.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f1());
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f1());
                    break;
                default:
                    Assert.Equal(expected, f1());
                    break;
            }

            // verify with values passed to make parameters
            Expression<Func<long, long, Func<long>>> e2 =
                Expression.Lambda<Func<long, long, Func<long>>>(
                    Expression.Lambda<Func<long>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<long, long, Func<long>> f2 = e2.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f2(a, b)());
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f2(a, b)());
                    break;
                default:
                    Assert.Equal(expected, f2(a, b)());
                    break;
            }

            // verify with values directly passed
            Expression<Func<Func<long, long, long>>> e3 =
                Expression.Lambda<Func<Func<long, long, long>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<long, long, long>>>(
                            Expression.Lambda<Func<long, long, long>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<long, long, long> f3 = e3.Compile(useInterpreter)();

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f3(a, b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f3(a, b));
                    break;
                default:
                    Assert.Equal(expected, f3(a, b));
                    break;
            }

            // verify as a function generator
            Expression<Func<Func<long, long, long>>> e4 =
                Expression.Lambda<Func<Func<long, long, long>>>(
                    Expression.Lambda<Func<long, long, long>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<long, long, long>> f4 = e4.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f4()(a, b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f4()(a, b));
                    break;
                default:
                    Assert.Equal(expected, f4()(a, b));
                    break;
            }

            // verify with currying
            Expression<Func<long, Func<long, long>>> e5 =
                Expression.Lambda<Func<long, Func<long, long>>>(
                    Expression.Lambda<Func<long, long>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<long, Func<long, long>> f5 = e5.Compile(useInterpreter);

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f5(a)(b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f5(a)(b));
                    break;
                default:
                    Assert.Equal(expected, f5(a)(b));
                    break;
            }

            // verify with one parameter
            Expression<Func<Func<long, long>>> e6 =
                Expression.Lambda<Func<Func<long, long>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<long, Func<long, long>>>(
                            Expression.Lambda<Func<long, long>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(long)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<long, long> f6 = e6.Compile(useInterpreter)();

            switch (outcome)
            {
                case ResultType.DivideByZero:
                    Assert.Throws<DivideByZeroException>(() => f6(b));
                    break;
                case ResultType.Overflow:
                    Assert.Throws<OverflowException>(() => f6(b));
                    break;
                default:
                    Assert.Equal(expected, f6(b));
                    break;
            }
        }

        #endregion


        #region Verify short

        private static void VerifyModuloShort(short a, short b, bool useInterpreter)
        {
            bool divideByZero;
            short expected;
            if (b == 0)
            {
                divideByZero = true;
                expected = 0;
            }
            else
            {
                divideByZero = false;
                expected = (short)(a % b);
            }

            ParameterExpression p0 = Expression.Parameter(typeof(short), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(short), "p1");

            // verify with parameters supplied
            Expression<Func<short>> e1 =
                Expression.Lambda<Func<short>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<short, short, short>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(short)),
                    Expression.Constant(b, typeof(short))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f1 = e1.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f1());
            }
            else
            {
                Assert.Equal(expected, f1());
            }

            // verify with values passed to make parameters
            Expression<Func<short, short, Func<short>>> e2 =
                Expression.Lambda<Func<short, short, Func<short>>>(
                    Expression.Lambda<Func<short>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<short, short, Func<short>> f2 = e2.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f2(a, b)());
            }
            else
            {
                Assert.Equal(expected, f2(a, b)());
            }

            // verify with values directly passed
            Expression<Func<Func<short, short, short>>> e3 =
                Expression.Lambda<Func<Func<short, short, short>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<short, short, short>>>(
                            Expression.Lambda<Func<short, short, short>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<short, short, short> f3 = e3.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f3(a, b));
            }
            else
            {
                Assert.Equal(expected, f3(a, b));
            }

            // verify as a function generator
            Expression<Func<Func<short, short, short>>> e4 =
                Expression.Lambda<Func<Func<short, short, short>>>(
                    Expression.Lambda<Func<short, short, short>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<short, short, short>> f4 = e4.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f4()(a, b));
            }
            else
            {
                Assert.Equal(expected, f4()(a, b));
            }

            // verify with currying
            Expression<Func<short, Func<short, short>>> e5 =
                Expression.Lambda<Func<short, Func<short, short>>>(
                    Expression.Lambda<Func<short, short>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<short, Func<short, short>> f5 = e5.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f5(a)(b));
            }
            else
            {
                Assert.Equal(expected, f5(a)(b));
            }

            // verify with one parameter
            Expression<Func<Func<short, short>>> e6 =
                Expression.Lambda<Func<Func<short, short>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<short, Func<short, short>>>(
                            Expression.Lambda<Func<short, short>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(short)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<short, short> f6 = e6.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f6(b));
            }
            else
            {
                Assert.Equal(expected, f6(b));
            }
        }

        #endregion


        #region Verify uint

        private static void VerifyModuloUInt(uint a, uint b, bool useInterpreter)
        {
            bool divideByZero;
            uint expected;
            if (b == 0)
            {
                divideByZero = true;
                expected = 0;
            }
            else
            {
                divideByZero = false;
                expected = a % b;
            }

            ParameterExpression p0 = Expression.Parameter(typeof(uint), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(uint), "p1");

            // verify with parameters supplied
            Expression<Func<uint>> e1 =
                Expression.Lambda<Func<uint>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<uint, uint, uint>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(uint)),
                    Expression.Constant(b, typeof(uint))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f1 = e1.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f1());
            }
            else
            {
                Assert.Equal(expected, f1());
            }

            // verify with values passed to make parameters
            Expression<Func<uint, uint, Func<uint>>> e2 =
                Expression.Lambda<Func<uint, uint, Func<uint>>>(
                    Expression.Lambda<Func<uint>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<uint, uint, Func<uint>> f2 = e2.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f2(a, b)());
            }
            else
            {
                Assert.Equal(expected, f2(a, b)());
            }

            // verify with values directly passed
            Expression<Func<Func<uint, uint, uint>>> e3 =
                Expression.Lambda<Func<Func<uint, uint, uint>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<uint, uint, uint>>>(
                            Expression.Lambda<Func<uint, uint, uint>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint, uint, uint> f3 = e3.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f3(a, b));
            }
            else
            {
                Assert.Equal(expected, f3(a, b));
            }

            // verify as a function generator
            Expression<Func<Func<uint, uint, uint>>> e4 =
                Expression.Lambda<Func<Func<uint, uint, uint>>>(
                    Expression.Lambda<Func<uint, uint, uint>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<uint, uint, uint>> f4 = e4.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f4()(a, b));
            }
            else
            {
                Assert.Equal(expected, f4()(a, b));
            }

            // verify with currying
            Expression<Func<uint, Func<uint, uint>>> e5 =
                Expression.Lambda<Func<uint, Func<uint, uint>>>(
                    Expression.Lambda<Func<uint, uint>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<uint, Func<uint, uint>> f5 = e5.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f5(a)(b));
            }
            else
            {
                Assert.Equal(expected, f5(a)(b));
            }

            // verify with one parameter
            Expression<Func<Func<uint, uint>>> e6 =
                Expression.Lambda<Func<Func<uint, uint>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<uint, Func<uint, uint>>>(
                            Expression.Lambda<Func<uint, uint>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(uint)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint, uint> f6 = e6.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f6(b));
            }
            else
            {
                Assert.Equal(expected, f6(b));
            }
        }

        #endregion


        #region Verify ulong

        private static void VerifyModuloULong(ulong a, ulong b, bool useInterpreter)
        {
            bool divideByZero;
            ulong expected;
            if (b == 0)
            {
                divideByZero = true;
                expected = 0;
            }
            else
            {
                divideByZero = false;
                expected = a % b;
            }

            ParameterExpression p0 = Expression.Parameter(typeof(ulong), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(ulong), "p1");

            // verify with parameters supplied
            Expression<Func<ulong>> e1 =
                Expression.Lambda<Func<ulong>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ulong, ulong, ulong>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(ulong)),
                    Expression.Constant(b, typeof(ulong))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f1 = e1.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f1());
            }
            else
            {
                Assert.Equal(expected, f1());
            }

            // verify with values passed to make parameters
            Expression<Func<ulong, ulong, Func<ulong>>> e2 =
                Expression.Lambda<Func<ulong, ulong, Func<ulong>>>(
                    Expression.Lambda<Func<ulong>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<ulong, ulong, Func<ulong>> f2 = e2.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f2(a, b)());
            }
            else
            {
                Assert.Equal(expected, f2(a, b)());
            }

            // verify with values directly passed
            Expression<Func<Func<ulong, ulong, ulong>>> e3 =
                Expression.Lambda<Func<Func<ulong, ulong, ulong>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ulong, ulong, ulong>>>(
                            Expression.Lambda<Func<ulong, ulong, ulong>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong, ulong, ulong> f3 = e3.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f3(a, b));
            }
            else
            {
                Assert.Equal(expected, f3(a, b));
            }

            // verify as a function generator
            Expression<Func<Func<ulong, ulong, ulong>>> e4 =
                Expression.Lambda<Func<Func<ulong, ulong, ulong>>>(
                    Expression.Lambda<Func<ulong, ulong, ulong>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ulong, ulong, ulong>> f4 = e4.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f4()(a, b));
            }
            else
            {
                Assert.Equal(expected, f4()(a, b));
            }

            // verify with currying
            Expression<Func<ulong, Func<ulong, ulong>>> e5 =
                Expression.Lambda<Func<ulong, Func<ulong, ulong>>>(
                    Expression.Lambda<Func<ulong, ulong>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<ulong, Func<ulong, ulong>> f5 = e5.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f5(a)(b));
            }
            else
            {
                Assert.Equal(expected, f5(a)(b));
            }

            // verify with one parameter
            Expression<Func<Func<ulong, ulong>>> e6 =
                Expression.Lambda<Func<Func<ulong, ulong>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ulong, Func<ulong, ulong>>>(
                            Expression.Lambda<Func<ulong, ulong>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(ulong)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong, ulong> f6 = e6.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f6(b));
            }
            else
            {
                Assert.Equal(expected, f6(b));
            }
        }

        #endregion


        #region Verify ushort

        private static void VerifyModuloUShort(ushort a, ushort b, bool useInterpreter)
        {
            bool divideByZero;
            ushort expected;

            if (b == 0)
            {
                divideByZero = true;
                expected = 0;
            }
            else
            {
                divideByZero = false;
                expected = (ushort)(a % b);
            }

            ParameterExpression p0 = Expression.Parameter(typeof(ushort), "p0");
            ParameterExpression p1 = Expression.Parameter(typeof(ushort), "p1");

            // verify with parameters supplied
            Expression<Func<ushort>> e1 =
                Expression.Lambda<Func<ushort>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ushort, ushort, ushort>>(
                            Expression.Modulo(p0, p1),
                            new ParameterExpression[] { p0, p1 }),
                        new Expression[]
                {
                    Expression.Constant(a, typeof(ushort)),
                    Expression.Constant(b, typeof(ushort))
                }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f1 = e1.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f1());
            }
            else
            {
                Assert.Equal(expected, f1());
            }

            // verify with values passed to make parameters
            Expression<Func<ushort, ushort, Func<ushort>>> e2 =
                Expression.Lambda<Func<ushort, ushort, Func<ushort>>>(
                    Expression.Lambda<Func<ushort>>(
                        Expression.Modulo(p0, p1),
                        Enumerable.Empty<ParameterExpression>()),
                    new ParameterExpression[] { p0, p1 });
            Func<ushort, ushort, Func<ushort>> f2 = e2.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f2(a, b)());
            }
            else
            {
                Assert.Equal(expected, f2(a, b)());
            }

            // verify with values directly passed
            Expression<Func<Func<ushort, ushort, ushort>>> e3 =
                Expression.Lambda<Func<Func<ushort, ushort, ushort>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<Func<ushort, ushort, ushort>>>(
                            Expression.Lambda<Func<ushort, ushort, ushort>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p0, p1 }),
                            Enumerable.Empty<ParameterExpression>()),
                        Enumerable.Empty<Expression>()),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort, ushort, ushort> f3 = e3.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f3(a, b));
            }
            else
            {
                Assert.Equal(expected, f3(a, b));
            }

            // verify as a function generator
            Expression<Func<Func<ushort, ushort, ushort>>> e4 =
                Expression.Lambda<Func<Func<ushort, ushort, ushort>>>(
                    Expression.Lambda<Func<ushort, ushort, ushort>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p0, p1 }),
                    Enumerable.Empty<ParameterExpression>());
            Func<Func<ushort, ushort, ushort>> f4 = e4.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f4()(a, b));
            }
            else
            {
                Assert.Equal(expected, f4()(a, b));
            }

            // verify with currying
            Expression<Func<ushort, Func<ushort, ushort>>> e5 =
                Expression.Lambda<Func<ushort, Func<ushort, ushort>>>(
                    Expression.Lambda<Func<ushort, ushort>>(
                        Expression.Modulo(p0, p1),
                        new ParameterExpression[] { p1 }),
                    new ParameterExpression[] { p0 });
            Func<ushort, Func<ushort, ushort>> f5 = e5.Compile(useInterpreter);

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f5(a)(b));
            }
            else
            {
                Assert.Equal(expected, f5(a)(b));
            }

            // verify with one parameter
            Expression<Func<Func<ushort, ushort>>> e6 =
                Expression.Lambda<Func<Func<ushort, ushort>>>(
                    Expression.Invoke(
                        Expression.Lambda<Func<ushort, Func<ushort, ushort>>>(
                            Expression.Lambda<Func<ushort, ushort>>(
                                Expression.Modulo(p0, p1),
                                new ParameterExpression[] { p1 }),
                            new ParameterExpression[] { p0 }),
                        new Expression[] { Expression.Constant(a, typeof(ushort)) }),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort, ushort> f6 = e6.Compile(useInterpreter)();

            if (divideByZero)
            {
                Assert.Throws<DivideByZeroException>(() => f6(b));
            }
            else
            {
                Assert.Equal(expected, f6(b));
            }
        }

        #endregion


        #endregion
    }
}
