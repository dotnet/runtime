// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class CastNullableTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumCastEnumTypeTest(bool useInterpreter)
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumCastEnumType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableEnumCastObjectTest(bool useInterpreter)
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntCastObjectTest(bool useInterpreter)
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableIntCastValueTypeTest(bool useInterpreter)
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntCastValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructCastIEquatableOfStructTest(bool useInterpreter)
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructCastIEquatableOfStruct(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructCastObjectTest(bool useInterpreter)
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructCastObject(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNullableStructCastValueTypeTest(bool useInterpreter)
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructCastValueType(array[i], useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastObjectHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastObjectHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastObjectHelper<Scs>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField(bool useInterpreter)
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionCastObjectHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastObject<Ts>(array[i], useInterpreter);
            }
        }

        private static void CheckGenericWithStructRestrictionCastValueTypeHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastValueType<Ts>(array[i], useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableEnumCastEnumType(E? value, bool useInterpreter)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableEnumCastObject(E? value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntCastObject(int? value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableIntCastValueType(int? value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructCastIEquatableOfStruct(S? value, bool useInterpreter)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S?)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<S>> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructCastObject(S? value, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(S?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyNullableStructCastValueType(S? value, bool useInterpreter)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(S?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithStructRestrictionCastObject<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        private static void VerifyGenericWithStructRestrictionCastValueType<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile(useInterpreter);

            Assert.Equal(value, f());
        }

        #endregion
    }
}
