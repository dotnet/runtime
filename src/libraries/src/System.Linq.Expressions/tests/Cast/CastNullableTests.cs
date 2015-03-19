// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Cast
{
    public static unsafe class CastNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableEnumCastEnumTypeTest()
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumCastEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableEnumCastObjectTest()
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntCastObjectTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntCastValueTypeTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntCastValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructCastIEquatableOfStructTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructCastIEquatableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructCastObjectTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructCastObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructCastValueTypeTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructCastValueType(array[i]);
            }
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum()
        {
            CheckGenericWithStructRestrictionCastObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct()
        {
            CheckGenericWithStructRestrictionCastObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionCastObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum()
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct()
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionCastValueTypeHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionCastObjectHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastObject<Ts>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionCastValueTypeHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionCastValueType<Ts>(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableEnumCastEnumType(E? value)
        {
            Expression<Func<Enum>> e =
                Expression.Lambda<Func<Enum>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(Enum)),
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
                csResult = (Enum)value;
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

        private static void VerifyNullableEnumCastObject(E? value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(E?)), typeof(object)),
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
                csResult = (object)value;
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

        private static void VerifyNullableIntCastObject(int? value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(object)),
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
                csResult = (object)value;
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

        private static void VerifyNullableIntCastValueType(int? value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(int?)), typeof(ValueType)),
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
                csResult = (ValueType)value;
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

        private static void VerifyNullableStructCastIEquatableOfStruct(S? value)
        {
            Expression<Func<IEquatable<S>>> e =
                Expression.Lambda<Func<IEquatable<S>>>(
                    Expression.Convert(Expression.Constant(value, typeof(S?)), typeof(IEquatable<S>)),
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
                csResult = (IEquatable<S>)value;
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

        private static void VerifyNullableStructCastObject(S? value)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(S?)), typeof(object)),
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
                csResult = (object)value;
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

        private static void VerifyNullableStructCastValueType(S? value)
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(S?)), typeof(ValueType)),
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
                csResult = (ValueType)value;
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

        private static void VerifyGenericWithStructRestrictionCastObject<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(object)),
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
                csResult = (object)value;
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

        private static void VerifyGenericWithStructRestrictionCastValueType<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<ValueType>> e =
                Expression.Lambda<Func<ValueType>>(
                    Expression.Convert(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
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
                csResult = (ValueType)value;
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
