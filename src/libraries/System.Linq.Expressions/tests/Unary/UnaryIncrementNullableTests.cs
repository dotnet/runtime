// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class UnaryIncrementNullableTests : IncrementDecrementTests
    {
        public struct IncrementableWhenNullable
        {
            public IncrementableWhenNullable(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public static IncrementableWhenNullable? operator ++(IncrementableWhenNullable? operand)
            {
                if (operand.HasValue)
                {
                    int dec = unchecked(operand.GetValueOrDefault().Value + 1);
                    if (dec == 0)
                    {
                        return null;
                    }

                    return new IncrementableWhenNullable(dec);
                }

                return new IncrementableWhenNullable(1);
            }
        }

        private static IEnumerable<object[]> IncrementableWhenNullableValues()
        {
            yield return new object[] { new IncrementableWhenNullable(0), new IncrementableWhenNullable(1) };
            yield return new object[] { new IncrementableWhenNullable(-1), null };
            yield return new object[] { new IncrementableWhenNullable(int.MinValue), new IncrementableWhenNullable(int.MinValue + 1) };
            yield return new object[] { new IncrementableWhenNullable(int.MaxValue), new IncrementableWhenNullable(int.MinValue) };
            yield return new object[] { null, new IncrementableWhenNullable(1) };
        }

        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryIncrementNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryIncrementNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableUShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryIncrementNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryIncrementNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableUInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryIncrementNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableLong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryIncrementNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableULong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIncrementFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableFloat(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckIncrementDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIncrementNullableDouble(values[i], useInterpreter);
            }
        }

        #endregion

        [Theory, MemberData(nameof(NonArithmeticObjects), false)]
        public static void DecrementNonArithmetic(object value)
        {
            Expression ex = Expression.Constant(value, typeof(Nullable<>).MakeGenericType(value.GetType()));
            Assert.Throws<InvalidOperationException>(() => Expression.Decrement(ex));
        }

        [Theory, PerCompilationType(nameof(IncrementableValues), true)]
        public static void CustomOpIncrement(Incrementable? operand, Incrementable? expected, bool useInterpreter)
        {
            Func<Incrementable?> func = Expression.Lambda<Func<Incrementable?>>(
                Expression.Increment(Expression.Constant(operand, typeof(Incrementable?)))).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(IncrementableWhenNullableValues))]
        public static void NonLiftedNullableOpIncrement(
            IncrementableWhenNullable? operand, IncrementableWhenNullable? expected, bool useInterpreter)
        {
            Func<IncrementableWhenNullable?> func = Expression.Lambda<Func<IncrementableWhenNullable?>>(
                Expression.Increment(Expression.Constant(operand, typeof(IncrementableWhenNullable?)))).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(DoublyIncrementedIncrementableValues), true)]
        public static void UserDefinedOpIncrement(Incrementable? operand, Incrementable? expected, bool useInterpreter)
        {
            MethodInfo method = typeof(IncrementDecrementTests).GetMethod(nameof(DoublyIncrement));
            Func<Incrementable?> func = Expression.Lambda<Func<Incrementable?>>(
                Expression.Increment(Expression.Constant(operand, typeof(Incrementable?)), method)).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(DoublyIncrementedInt32s), true)]
        public static void UserDefinedOpIncrementArithmeticType(int? operand, int? expected, bool useInterpreter)
        {
            MethodInfo method = typeof(IncrementDecrementTests).GetMethod(nameof(DoublyIncrementInt32));
            Func<int?> func = Expression.Lambda<Func<int?>>(
                Expression.Increment(Expression.Constant(operand, typeof(int?)), method)).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        #region Test verifiers

        private static void VerifyIncrementNullableShort(short? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Increment(Expression.Constant(value, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((short?)(++value)), f());
        }

        private static void VerifyIncrementNullableUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Increment(Expression.Constant(value, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ushort?)(++value)), f());
        }

        private static void VerifyIncrementNullableInt(int? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Increment(Expression.Constant(value, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((int?)(++value)), f());
        }

        private static void VerifyIncrementNullableUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Increment(Expression.Constant(value, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((uint?)(++value)), f());
        }

        private static void VerifyIncrementNullableLong(long? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Increment(Expression.Constant(value, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((long?)(++value)), f());
        }

        private static void VerifyIncrementNullableULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Increment(Expression.Constant(value, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ulong?)(++value)), f());
        }

        private static void VerifyIncrementNullableFloat(float? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Increment(Expression.Constant(value, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);
            Assert.Equal((float?)(++value), f());
        }

        private static void VerifyIncrementNullableDouble(double? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Increment(Expression.Constant(value, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);
            Assert.Equal((double?)(++value), f());
        }

        #endregion
    }
}
