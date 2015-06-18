// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Cast
{
    public static unsafe class IsNullableTests
    {
        #region Test methods

        [Fact]
        public static void CheckNullableEnumIsEnumTypeTest()
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumIsEnumType(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableEnumIsObjectTest()
        {
            E?[] array = new E?[] { null, (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableEnumIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntIsObjectTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableIntIsValueTypeTest()
        {
            int?[] array = new int?[] { null, 0, 1, -1, int.MinValue, int.MaxValue };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableIntIsValueType(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructIsIEquatableOfStructTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructIsIEquatableOfStruct(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructIsObjectTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructIsObject(array[i]);
            }
        }

        [Fact]
        public static void CheckNullableStructIsValueTypeTest()
        {
            S?[] array = new S?[] { null, default(S), new S() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyNullableStructIsValueType(array[i]);
            }
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsEnum()
        {
            CheckGenericWithStructRestrictionIsObjectHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStruct()
        {
            CheckGenericWithStructRestrictionIsObjectHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastObjectAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionIsObjectHelper<Scs>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsEnum()
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<E>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStruct()
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<S>();
        }

        [Fact]
        public static void ConvertGenericWithStructRestrictionCastValueTypeAsStructWithStringAndField()
        {
            CheckGenericWithStructRestrictionIsValueTypeHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckGenericWithStructRestrictionIsObjectHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionIsObject<Ts>(array[i]);
            }
        }

        private static void CheckGenericWithStructRestrictionIsValueTypeHelper<Ts>() where Ts : struct
        {
            Ts[] array = new Ts[] { default(Ts), new Ts() };
            for (int i = 0; i < array.Length; i++)
            {
                VerifyGenericWithStructRestrictionIsValueType<Ts>(array[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyNullableEnumIsEnumType(E? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(E?)), typeof(Enum)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is Enum;
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

        private static void VerifyNullableEnumIsObject(E? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(E?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyNullableIntIsObject(int? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(int?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyNullableIntIsValueType(int? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(int?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is ValueType;
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

        private static void VerifyNullableStructIsIEquatableOfStruct(S? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S?)), typeof(IEquatable<S>)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is IEquatable<S>;
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

        private static void VerifyNullableStructIsObject(S? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S?)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyNullableStructIsValueType(S? value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(S?)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is ValueType;
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

        private static void VerifyGenericWithStructRestrictionIsObject<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Ts)), typeof(object)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is object;
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

        private static void VerifyGenericWithStructRestrictionIsValueType<Ts>(Ts value) where Ts : struct
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.TypeIs(Expression.Constant(value, typeof(Ts)), typeof(ValueType)),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();

            // compute the value with the expression tree
            bool etResult = default(bool);
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
            bool csResult = default(bool);
            Exception csException = null;
            try
            {
                csResult = value is ValueType;
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
