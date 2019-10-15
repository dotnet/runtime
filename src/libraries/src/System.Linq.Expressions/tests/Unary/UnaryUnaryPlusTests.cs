// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryPlusTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusShortTest(bool useInterpreter)
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusUShortTest(bool useInterpreter)
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusUShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusIntTest(bool useInterpreter)
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusInt(values[i], useInterpreter);
                VerifyArithmeticMakeUnaryPlusInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusUIntTest(bool useInterpreter)
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusUInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusLongTest(bool useInterpreter)
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusLong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusULongTest(bool useInterpreter)
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusULong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusFloatTest(bool useInterpreter)
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusFloat(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusDoubleTest(bool useInterpreter)
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusDouble(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryArithmeticUnaryPlusDecimalTest(bool useInterpreter)
        {
            decimal[] values = new decimal[] { decimal.Zero, decimal.One, decimal.MinusOne, decimal.MinValue, decimal.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyArithmeticUnaryPlusDecimal(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.UnaryPlus(Expression.Parameter(typeof(int), "x"));
            Assert.Equal("+x", e.ToString());
        }

        #endregion

        #region Test verifiers

        private static void VerifyArithmeticUnaryPlusShort(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);
            Assert.Equal((short)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);
            Assert.Equal((ushort)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal((int)(+value), f());
        }

        private static void VerifyArithmeticMakeUnaryPlusInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.MakeUnary(ExpressionType.UnaryPlus, Expression.Constant(value), null),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal((int)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);
            Assert.Equal((uint)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusLong(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);
            Assert.Equal((long)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);
            Assert.Equal((ulong)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusFloat(float value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);
            Assert.Equal((float)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusDouble(double value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);
            Assert.Equal((double)(+value), f());
        }

        private static void VerifyArithmeticUnaryPlusDecimal(decimal value, bool useInterpreter)
        {
            Expression<Func<decimal>> e =
                Expression.Lambda<Func<decimal>>(
                    Expression.UnaryPlus(Expression.Constant(value, typeof(decimal))),
                    Enumerable.Empty<ParameterExpression>());
            Func<decimal> f = e.Compile(useInterpreter);
            Assert.Equal((decimal)(+value), f());
        }


        #endregion
    }
}
