// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class UnaryDecrementTests : IncrementDecrementTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementShortTest(bool useInterpreter)
        {
            short[] values = new short[] { 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementUShortTest(bool useInterpreter)
        {
            ushort[] values = new ushort[] { 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementUShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementIntTest(bool useInterpreter)
        {
            int[] values = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementInt(values[i], useInterpreter);
                VerifyDecrementIntMakeUnary(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementUIntTest(bool useInterpreter)
        {
            uint[] values = new uint[] { 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementUInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementLongTest(bool useInterpreter)
        {
            long[] values = new long[] { 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementLong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementULongTest(bool useInterpreter)
        {
            ulong[] values = new ulong[] { 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementULong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecrementFloatTest(bool useInterpreter)
        {
            float[] values = new float[] { 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementFloat(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecrementDoubleTest(bool useInterpreter)
        {
            double[] values = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementDouble(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.Decrement(Expression.Parameter(typeof(int), "x"));
            Assert.Equal("Decrement(x)", e.ToString());
        }

        [Theory, MemberData(nameof(NonArithmeticObjects), true)]
        public static void DecrementNonArithmetic(object value)
        {
            Expression ex = Expression.Constant(value);
            Assert.Throws<InvalidOperationException>(() => Expression.Decrement(ex));
        }

        [Theory, PerCompilationType(nameof(DecrementableValues), false)]
        public static void CustomOpDecrement(Decrementable operand, Decrementable expected, bool useInterpreter)
        {
            Func<Decrementable> func = Expression.Lambda<Func<Decrementable>>(
                Expression.Decrement(Expression.Constant(operand))).Compile(useInterpreter);
            Assert.Equal(expected.Value, func().Value);
        }

        [Theory, PerCompilationType(nameof(DoublyDecrementedDecrementableValues), false)]
        public static void UserDefinedOpDecrement(Decrementable operand, Decrementable expected, bool useInterpreter)
        {
            MethodInfo method = typeof(IncrementDecrementTests).GetMethod(nameof(DoublyDecrement));
            Func<Decrementable> func = Expression.Lambda<Func<Decrementable>>(
                Expression.Decrement(Expression.Constant(operand), method)).Compile(useInterpreter);
            Assert.Equal(expected.Value, func().Value);
        }

        [Theory, PerCompilationType(nameof(DoublyDecrementedInt32s), false)]
        public static void UserDefinedOpDecrementArithmeticType(int operand, int expected, bool useInterpreter)
        {
            MethodInfo method = typeof(IncrementDecrementTests).GetMethod(nameof(DoublyDecrementInt32));
            Func<int> func = Expression.Lambda<Func<int>>(
                Expression.Decrement(Expression.Constant(operand), method)).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Fact]
        public static void NullOperand()
        {
            AssertExtensions.Throws<ArgumentNullException>("expression", () => Expression.Decrement(null));
        }

        [Fact]
        public static void UnreadableOperand()
        {
            Expression operand = Expression.Property(null, typeof(Unreadable<int>), nameof(Unreadable<int>.WriteOnly));
            AssertExtensions.Throws<ArgumentException>("expression", () => Expression.Decrement(operand));
        }

        #endregion

        #region Test verifiers

        private static void VerifyDecrementShort(short value, bool useInterpreter)
        {
            Expression<Func<short>> e =
                Expression.Lambda<Func<short>>(
                    Expression.Decrement(Expression.Constant(value, typeof(short))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((short)(--value)), f());
        }

        private static void VerifyDecrementUShort(ushort value, bool useInterpreter)
        {
            Expression<Func<ushort>> e =
                Expression.Lambda<Func<ushort>>(
                    Expression.Decrement(Expression.Constant(value, typeof(ushort))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ushort)(--value)), f());
        }

        private static void VerifyDecrementInt(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.Decrement(Expression.Constant(value, typeof(int))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((int)(--value)), f());
        }

        private static void VerifyDecrementIntMakeUnary(int value, bool useInterpreter)
        {
            Expression<Func<int>> e =
                Expression.Lambda<Func<int>>(
                    Expression.MakeUnary(ExpressionType.Decrement, Expression.Constant(value), null),
                    Enumerable.Empty<ParameterExpression>());
            Func<int> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked(--value), f());
        }

        private static void VerifyDecrementUInt(uint value, bool useInterpreter)
        {
            Expression<Func<uint>> e =
                Expression.Lambda<Func<uint>>(
                    Expression.Decrement(Expression.Constant(value, typeof(uint))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((uint)(--value)), f());
        }

        private static void VerifyDecrementLong(long value, bool useInterpreter)
        {
            Expression<Func<long>> e =
                Expression.Lambda<Func<long>>(
                    Expression.Decrement(Expression.Constant(value, typeof(long))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((long)(--value)), f());
        }

        private static void VerifyDecrementULong(ulong value, bool useInterpreter)
        {
            Expression<Func<ulong>> e =
                Expression.Lambda<Func<ulong>>(
                    Expression.Decrement(Expression.Constant(value, typeof(ulong))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ulong)(--value)), f());
        }

        private static void VerifyDecrementFloat(float value, bool useInterpreter)
        {
            Expression<Func<float>> e =
                Expression.Lambda<Func<float>>(
                    Expression.Decrement(Expression.Constant(value, typeof(float))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float> f = e.Compile(useInterpreter);
            Assert.Equal((float)(--value), f());
        }

        private static void VerifyDecrementDouble(double value, bool useInterpreter)
        {
            Expression<Func<double>> e =
                Expression.Lambda<Func<double>>(
                    Expression.Decrement(Expression.Constant(value, typeof(double))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double> f = e.Compile(useInterpreter);
            Assert.Equal((double)(--value), f());
        }

        #endregion
    }
}
