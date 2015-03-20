// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.New
{
    public static unsafe class NewTests
    {
        #region Test methods

        [Fact]
        public static void CheckNewCustomTest()
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.New(typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile();

            Assert.Equal(new C(), f());
        }

        [Fact]
        public static void CheckNewEnumTest()
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.New(typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile();

            Assert.Equal(new E(), f());
        }

        [Fact]
        public static void CheckNewNullableEnumTest()
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.New(typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile();

            Assert.Equal(new E?(), f());
        }

        [Fact]
        public static void CheckNewNullableIntTest()
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.New(typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();

            Assert.Equal(new int?(), f());
        }

        [Fact]
        public static void CheckNewStructTest()
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.New(typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile();

            Assert.Equal(new S(), f());
        }

        [Fact]
        public static void CheckNewNullableStructTest()
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.New(typeof(S?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile();

            Assert.Equal(new S?(), f());
        }

        [Fact]
        public static void CheckNewStructWithStringTest()
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.New(typeof(Sc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile();

            Assert.Equal(new Sc(), f());
        }

        [Fact]
        public static void CheckNewNullableStructWithStringTest()
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(typeof(Sc?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();

            Assert.Equal(new Sc?(), f());
        }

        [Fact]
        public static void CheckNewStructWithStringAndFieldTest()
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.New(typeof(Scs)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile();

            Assert.Equal(new Scs(), f());
        }

        [Fact]
        public static void CheckNewNullableStructWithStringAndFieldTest()
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.New(typeof(Scs?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile();

            Assert.Equal(new Scs?(), f());
        }

        [Fact]
        public static void CheckNewStructWithTwoValuesTest()
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.New(typeof(Sp)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile();

            Assert.Equal(new Sp(), f());
        }

        [Fact]
        public static void CheckNewNullableStructWithTwoValuesTest()
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.New(typeof(Sp?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile();

            Assert.Equal(new Sp?(), f());
        }

        [Fact]
        public static void CheckNewGenericWithStructRestrictionWithEnumTest()
        {
            CheckNewGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckNewGenericWithStructRestrictionWithStructTest()
        {
            CheckNewGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckNewGenericWithStructRestrictionWithStructWithStringAndFieldTest()
        {
            CheckNewGenericWithStructRestrictionHelper<Scs>();
        }

        [Fact]
        public static void CheckNewNullableGenericWithStructRestrictionWithEnumTest()
        {
            CheckNewNullableGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckNewNullableGenericWithStructRestrictionWithStructTest()
        {
            CheckNewNullableGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckNewNullableGenericWithStructRestrictionWithStructWithStringAndFieldTest()
        {
            CheckNewNullableGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckNewGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.New(typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile();

            Assert.Equal(new Ts(), f());
        }

        private static void CheckNewNullableGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.New(typeof(Ts?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile();

            Assert.Equal(new Ts?(), f());
        }

        #endregion
    }
}
