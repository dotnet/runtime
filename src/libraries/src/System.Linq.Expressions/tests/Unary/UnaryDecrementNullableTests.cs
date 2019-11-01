// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public class UnaryDecrementNullableTests : IncrementDecrementTests
    {
        public struct DecrementableWhenNullable
        {
            public DecrementableWhenNullable(int value)
            {
                Value = value;
            }

            public int Value { get; }

            public static DecrementableWhenNullable? operator --(DecrementableWhenNullable? operand)
            {
                if (operand.HasValue)
                {
                    int dec = unchecked(operand.GetValueOrDefault().Value - 1);
                    if (dec == 0)
                    {
                        return null;
                    }

                    return new DecrementableWhenNullable(dec);
                }

                return new DecrementableWhenNullable(-1);
            }
        }

        public static IEnumerable<object[]> DecrementableWhenNullableValues()
        {
            yield return new object[] { new DecrementableWhenNullable(0), new DecrementableWhenNullable(-1) };
            yield return new object[] { new DecrementableWhenNullable(1), null };
            yield return new object[] { new DecrementableWhenNullable(int.MaxValue), new DecrementableWhenNullable(int.MaxValue - 1) };
            yield return new object[] { new DecrementableWhenNullable(int.MinValue), new DecrementableWhenNullable(int.MaxValue) };
            yield return new object[] { null, new DecrementableWhenNullable(-1) };
        }

        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementNullableShortTest(bool useInterpreter)
        {
            short?[] values = new short?[] { null, 0, 1, -1, short.MinValue, short.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementNullableUShortTest(bool useInterpreter)
        {
            ushort?[] values = new ushort?[] { null, 0, 1, ushort.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableUShort(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementNullableIntTest(bool useInterpreter)
        {
            int?[] values = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementNullableUIntTest(bool useInterpreter)
        {
            uint?[] values = new uint?[] { null, 0, 1, uint.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableUInt(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementNullableLongTest(bool useInterpreter)
        {
            long?[] values = new long?[] { null, 0, 1, -1, long.MinValue, long.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableLong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryDecrementNullableULongTest(bool useInterpreter)
        {
            ulong?[] values = new ulong?[] { null, 0, 1, ulong.MaxValue };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableULong(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecrementFloatTest(bool useInterpreter)
        {
            float?[] values = new float?[] { null, 0, 1, -1, float.MinValue, float.MaxValue, float.Epsilon, float.NegativeInfinity, float.PositiveInfinity, float.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableFloat(values[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckDecrementDoubleTest(bool useInterpreter)
        {
            double?[] values = new double?[] { null, 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyDecrementNullableDouble(values[i], useInterpreter);
            }
        }

        [Theory, MemberData(nameof(NonArithmeticObjects), false)]
        public static void DecrementNonArithmetic(object value)
        {
            Expression ex = Expression.Constant(value, typeof(Nullable<>).MakeGenericType(value.GetType()));
            Assert.Throws<InvalidOperationException>(() => Expression.Decrement(ex));
        }

        [Theory, PerCompilationType(nameof(DecrementableValues), true)]
        public static void CustomOpDecrement(Decrementable? operand, Decrementable? expected, bool useInterpreter)
        {
            Func<Decrementable?> func = Expression.Lambda<Func<Decrementable?>>(
                Expression.Decrement(Expression.Constant(operand, typeof(Decrementable?)))).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(DecrementableWhenNullableValues))]
        public static void NonLiftedNullableOpDecrement(
            DecrementableWhenNullable? operand, DecrementableWhenNullable? expected, bool useInterpreter)
        {
            Func<DecrementableWhenNullable?> func = Expression.Lambda<Func<DecrementableWhenNullable?>>(
                Expression.Decrement(Expression.Constant(operand, typeof(DecrementableWhenNullable?)))).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(DoublyDecrementedDecrementableValues), true)]
        public static void UserDefinedOpDecrement(Decrementable? operand, Decrementable? expected, bool useInterpreter)
        {
            MethodInfo method = typeof(IncrementDecrementTests).GetMethod(nameof(DoublyDecrement));
            Func<Decrementable?> func = Expression.Lambda<Func<Decrementable?>>(
                Expression.Decrement(Expression.Constant(operand, typeof(Decrementable?)), method)).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        [Theory, PerCompilationType(nameof(DoublyDecrementedInt32s), true)]
        public static void UserDefinedOpDecrementArithmeticType(int? operand, int? expected, bool useInterpreter)
        {
            MethodInfo method = typeof(IncrementDecrementTests).GetMethod(nameof(DoublyDecrementInt32));
            Func<int?> func = Expression.Lambda<Func<int?>>(
                Expression.Decrement(Expression.Constant(operand, typeof(int?)), method)).Compile(useInterpreter);
            Assert.Equal(expected, func());
        }

        #endregion

        #region Test verifiers

        private static void VerifyDecrementNullableShort(short? value, bool useInterpreter)
        {
            Expression<Func<short?>> e =
                Expression.Lambda<Func<short?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(short?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<short?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((short?)(--value)), f());
        }

        private static void VerifyDecrementNullableUShort(ushort? value, bool useInterpreter)
        {
            Expression<Func<ushort?>> e =
                Expression.Lambda<Func<ushort?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(ushort?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ushort?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ushort?)(--value)), f());
        }

        private static void VerifyDecrementNullableInt(int? value, bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(int?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((int?)(--value)), f());
        }

        private static void VerifyDecrementNullableUInt(uint? value, bool useInterpreter)
        {
            Expression<Func<uint?>> e =
                Expression.Lambda<Func<uint?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(uint?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<uint?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((uint?)(--value)), f());
        }

        private static void VerifyDecrementNullableLong(long? value, bool useInterpreter)
        {
            Expression<Func<long?>> e =
                Expression.Lambda<Func<long?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(long?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<long?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((long?)(--value)), f());
        }

        private static void VerifyDecrementNullableULong(ulong? value, bool useInterpreter)
        {
            Expression<Func<ulong?>> e =
                Expression.Lambda<Func<ulong?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(ulong?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<ulong?> f = e.Compile(useInterpreter);
            Assert.Equal(unchecked((ulong?)(--value)), f());
        }

        private static void VerifyDecrementNullableFloat(float? value, bool useInterpreter)
        {
            Expression<Func<float?>> e =
                Expression.Lambda<Func<float?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(float?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<float?> f = e.Compile(useInterpreter);
            Assert.Equal((float?)(--value), f());
        }

        private static void VerifyDecrementNullableDouble(double? value, bool useInterpreter)
        {
            Expression<Func<double?>> e =
                Expression.Lambda<Func<double?>>(
                    Expression.Decrement(Expression.Constant(value, typeof(double?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<double?> f = e.Compile(useInterpreter);
            Assert.Equal((double?)(--value), f());
        }

        #endregion
    }
}
