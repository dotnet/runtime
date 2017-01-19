// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class AsNullableTests
    {
        [Fact]
        public static void NotLiftedEvenOnNullableOperand()
        {
            Assert.False(Expression.TypeAs(Expression.Constant(E.A, typeof(E?)), typeof(E?)).IsLifted);
            Assert.False(Expression.TypeAs(Expression.Constant(E.A, typeof(E?)), typeof(Enum)).IsLifted);
        }

        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumAsEnumTypeTest(bool useInterpreter)
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumAsEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckEnumAsNullableEnumTypeTest(bool useInterpreter)
        {
            E[] array = { 0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyEnumAsNullableEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumAsNullableEnumTypeTest(bool useInterpreter)
        {
            E?[] array = { null, 0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumAsNullableEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckLongAsNullableEnumTypeTest(bool useInterpreter)
        {
            long[] array = { 0, 1, long.MinValue, long.MaxValue };
            foreach (long value in array)
            {
                VerifyLongAsNullableEnumType(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableLongAsNullableEnumTypeTest(bool useInterpreter)
        {
            long?[] array = { null, 0, 1, long.MinValue, long.MaxValue };
            foreach (long? value in array)
            {
                VerifyNullableLongAsNullableEnumType(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumAsObjectTest(bool useInterpreter)
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntAsObjectTest(bool useInterpreter)
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntAsValueTypeTest(bool useInterpreter)
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntAsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructAsIEquatableOfStructTest(bool useInterpreter)
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructAsIEquatableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructAsObjectTest(bool useInterpreter)
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructAsObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructAsValueTypeTest(bool useInterpreter)
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructAsValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionAsObjectHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsObject<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionAsValueTypeHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsValueType<Ts>(array[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableEnumAsEnumType(E? value, bool useInterpreter)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E?)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        private static void VerifyEnumAsNullableEnumType(E value, bool useInterpreter)
        {
            Expression<Func<E?>> e = Expression.Lambda<Func<E?>>(
                Expression.TypeAs(Expression.Constant(value), typeof(E?)));
            Func<E?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumAsNullableEnumType(E? value, bool useInterpreter)
        {
            Expression<Func<E?>> e = Expression.Lambda<Func<E?>>(
                Expression.TypeAs(Expression.Constant(value, typeof(E?)), typeof(E?)));
            Func<E?> f = e.Compile(useInterpreter);
            Assert.Equal(value, f());
        }

        private static void VerifyLongAsNullableEnumType(long value, bool useInterpreter)
        {
            Expression<Func<E?>> e = Expression.Lambda<Func<E?>>(
                Expression.TypeAs(Expression.Constant(value), typeof(E?)));
            Func<E?> f = e.Compile(useInterpreter);
            Assert.False(f().HasValue);
        }

        private static void VerifyNullableLongAsNullableEnumType(long? value, bool useInterpreter)
        {
            Expression<Func<E?>> e = Expression.Lambda<Func<E?>>(
                Expression.TypeAs(Expression.Constant(value, typeof(long?)), typeof(E?)));
            Func<E?> f = e.Compile(useInterpreter);
            Assert.False(f().HasValue);
        }

        private static void VerifyNullableEnumAsObject(E? value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyNullableIntAsObject(int? value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyNullableIntAsValueType(int? value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        private static void VerifyNullableStructAsIEquatableOfStruct(S? value, bool useInterpreter)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S?)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value as IEquatable<S>, f());
        }

        private static void VerifyNullableStructAsObject(S? value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyNullableStructAsValueType(S? value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        private static void VerifyGenericWithStructRestrictionAsObject<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value as object, f());
        }

        private static void VerifyGenericWithStructRestrictionAsValueType<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value as ValueType, f());
        }

        #endregion
    }
}
