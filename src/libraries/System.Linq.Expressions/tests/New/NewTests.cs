// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NewTests
    {
        #region Test methods

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewCustomTest(bool useInterpreter)
        {
            Expression<Func<C>> e =
                Expression.Lambda<Func<C>>(
                    Expression.New(typeof(C)),
                    Enumerable.Empty<ParameterExpression>());
            Func<C> f = e.Compile(useInterpreter);

            Assert.Equal(new C(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewEnumTest(bool useInterpreter)
        {
            Expression<Func<E>> e =
                Expression.Lambda<Func<E>>(
                    Expression.New(typeof(E)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E> f = e.Compile(useInterpreter);

            Assert.Equal(new E(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableEnumTest(bool useInterpreter)
        {
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.New(typeof(E?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);

            Assert.Equal(new E?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableIntTest(bool useInterpreter)
        {
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.New(typeof(int?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);

            Assert.Equal(new int?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructTest(bool useInterpreter)
        {
            Expression<Func<S>> e =
                Expression.Lambda<Func<S>>(
                    Expression.New(typeof(S)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S> f = e.Compile(useInterpreter);

            Assert.Equal(new S(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructTest(bool useInterpreter)
        {
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.New(typeof(S?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile(useInterpreter);

            Assert.Equal(new S?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithStringTest(bool useInterpreter)
        {
            Expression<Func<Sc>> e =
                Expression.Lambda<Func<Sc>>(
                    Expression.New(typeof(Sc)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc> f = e.Compile(useInterpreter);

            Assert.Equal(new Sc(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithStringTest(bool useInterpreter)
        {
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(typeof(Sc?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);

            Assert.Equal(new Sc?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithStringAndFieldTest(bool useInterpreter)
        {
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.New(typeof(Scs)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);

            Assert.Equal(new Scs(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithStringAndFieldTest(bool useInterpreter)
        {
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.New(typeof(Scs?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile(useInterpreter);

            Assert.Equal(new Scs?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewStructWithTwoValuesTest(bool useInterpreter)
        {
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.New(typeof(Sp)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);

            Assert.Equal(new Sp(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableStructWithTwoValuesTest(bool useInterpreter)
        {
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.New(typeof(Sp?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile(useInterpreter);

            Assert.Equal(new Sp?(), f());
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckNewGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckNewGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckNewGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckNewNullableGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckNewNullableGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewNullableGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckNewNullableGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckNewGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts>> e =
                Expression.Lambda<Func<Ts>>(
                    Expression.New(typeof(Ts)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts> f = e.Compile(useInterpreter);

            Assert.Equal(new Ts(), f());
        }

        private static void CheckNewNullableGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.New(typeof(Ts?)),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile(useInterpreter);

            Assert.Equal(new Ts?(), f());
        }

        #endregion

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void PrivateDefaultConstructor(bool useInterpreter)
        {
            Assert.Equal("Test instance", TestPrivateDefaultConstructor.GetInstanceFunc(useInterpreter)().ToString());
        }

        class TestPrivateDefaultConstructor
        {
            private TestPrivateDefaultConstructor()
            {
            }

            public static Func<TestPrivateDefaultConstructor> GetInstanceFunc(bool useInterpreter)
            {
                var lambda = Expression.Lambda<Func<TestPrivateDefaultConstructor>>(Expression.New(typeof(TestPrivateDefaultConstructor)), new ParameterExpression[] { });
                return lambda.Compile(useInterpreter);
            }

            public override string ToString()
            {
                return "Test instance";
            }
        }

        [Fact]
        public static void CheckNewWithStaticCtor()
        {
            var cctor = typeof(StaticCtor).GetTypeInfo().DeclaredConstructors.Single(c => c.IsStatic);
            Assert.Throws<ArgumentException>("constructor", () => Expression.New(cctor));
        }

        [Theory]
        [ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithAbstractCtor(bool useInterpretation)
        {
            var ctor = typeof(AbstractCtor).GetTypeInfo().DeclaredConstructors.Single();
            var f = Expression.Lambda<Func<AbstractCtor>>(Expression.New(ctor));

            Assert.Throws<InvalidOperationException>(() => f.Compile(useInterpretation));
        }

        static class StaticCtor
        {
            static StaticCtor()
            {
            }
        }

        abstract class AbstractCtor
        {
            public AbstractCtor()
            {
            }
        }
    }
}
