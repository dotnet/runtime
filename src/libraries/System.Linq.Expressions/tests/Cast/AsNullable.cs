// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Cast
{
    public static unsafe class AsNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableEnumAsEnumTypeTest()
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumAsEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableEnumAsObjectTest()
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntAsObjectTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntAsValueTypeTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntAsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructAsIEquatableOfStructTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructAsIEquatableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructAsObjectTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructAsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructAsValueTypeTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructAsValueType(array[i]);
            }
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum()
        {
            CheckGenericWithStructRestrictionAsObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct()
        {
            CheckGenericWithStructRestrictionAsObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionAsObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum()
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct()
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionAsValueTypeHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionAsObjectHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsObject<Ts>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionAsValueTypeHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionAsValueType<Ts>(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableEnumAsEnumType(E? value)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E?)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Enum> f = e.Compile();

            // compute the value with the expression tree
            Enum etResult = default(Enum);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            Enum csResult = default(Enum);
            Exception csException = null;
            try
            {
                csResult = value as Enum;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableEnumAsObject(E? value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(E?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = value as object;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableIntAsObject(int? value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = value as object;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableIntAsValueType(int? value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(int?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = value as ValueType;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableStructAsIEquatableOfStruct(S? value)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S?)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<IEquatable<S>> f = e.Compile();

            // compute the value with the expression tree
            IEquatable<S> etResult = default(IEquatable<S>);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            IEquatable<S> csResult = default(IEquatable<S>);
            Exception csException = null;
            try
            {
                csResult = value as IEquatable<S>;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableStructAsObject(S? value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = value as object;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyNullableStructAsValueType(S? value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(S?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = value as ValueType;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithStructRestrictionAsObject<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<object> f = e.Compile();

            // compute the value with the expression tree
            object etResult = default(object);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            object csResult = default(object);
            Exception csException = null;
            try
            {
                csResult = value as object;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        private static void VerifyGenericWithStructRestrictionAsValueType<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.TypeAs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<ValueType> f = e.Compile();

            // compute the value with the expression tree
            ValueType etResult = default(ValueType);
            Exception etException = null;
            try
            {
                etResult = f();
            }
            catch (Exception ex)
            {
                etException = ex;
            }

            // compute the value with regular IL
            ValueType csResult = default(ValueType);
            Exception csException = null;
            try
            {
                csResult = value as ValueType;
            }
            catch (Exception ex)
            {
                csException = ex;
            }

            // either both should have failed the same way or they should both produce the same result
            if (etException != null || csException != null)
            {
                Assert.NotNull(etException);
                Assert.NotNull(csException);
                Assert.Equal(csException.GetType(), etException.GetType());
            }
            else
            {
                Assert.Equal(csResult, etResult);
            }
        }

        #endregion
    }
}
