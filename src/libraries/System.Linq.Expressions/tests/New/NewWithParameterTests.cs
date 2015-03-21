// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Tests.ExpressionCompiler;
using Xunit;

namespace Tests.ExpressionCompiler.New
{
    public static unsafe class NewWithParameterTests
    {
        #region Test methods

        [Fact]
        public static void CheckNewWithParameterEnumTest()
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyWithParameterEnum(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterIntTest()
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyWithParameterInt(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterStructTest()
        {
            foreach (S value in new S[] { default(S), new S() })
            {
                VerifyWithParameterStruct(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterStructWithStringTest()
        {
            foreach (Sc value in new Sc[] { default(Sc), new Sc(), new Sc(null) })
            {
                VerifyWithParameterStructWithString(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterStructWithStringAndFieldTest()
        {
            foreach (Scs value in new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyWithParameterStructWithStringAndField(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterStructWithTwoValuesTest()
        {
            foreach (Sp value in new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyWithParameterStructWithTwoValues(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterStringTest()
        {
            foreach (Sc value in new Sc[] { default(Sc), new Sc(), new Sc(null) })
            {
                VerifyWithParameterString(value);
            }
        }

        [Fact]
        public static void CheckNewWithParameterGenericWithStructRestrictionWithEnumTest()
        {
            CheckNewWithParameterGenericWithStructRestrictionHelper<E>();
        }

        [Fact]
        public static void CheckNewWithParameterGenericWithStructRestrictionWithStructTest()
        {
            CheckNewWithParameterGenericWithStructRestrictionHelper<S>();
        }

        [Fact]
        public static void CheckNewWithParameterGenericWithStructRestrictionWithStructWithStringAndFieldTest()
        {
            CheckNewWithParameterGenericWithStructRestrictionHelper<Scs>();
        }

        #endregion

        #region Generic helpers

        private static void CheckNewWithParameterGenericWithStructRestrictionHelper<Ts>() where Ts : struct
        {
            foreach (Ts value in new Ts[] { default(Ts), new Ts() })
            {
                VerifyWithParameterGenericWithStructRestriction<Ts>(value);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyWithParameterEnum(E value)
        {
            ConstructorInfo constructor = typeof(E?).GetConstructor(new Type[] { typeof(E) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(E)) };
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile();
            Assert.Equal(new E?(value), f());
        }

        private static void VerifyWithParameterInt(int value)
        {
            ConstructorInfo constructor = typeof(int?).GetConstructor(new Type[] { typeof(int) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(int)) };
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile();
            Assert.Equal(new int?(value), f());
        }

        private static void VerifyWithParameterStruct(S value)
        {
            ConstructorInfo constructor = typeof(S?).GetConstructor(new Type[] { typeof(S) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(S)) };
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile();
            Assert.Equal(new S?(value), f());
        }

        private static void VerifyWithParameterStructWithString(Sc value)
        {
            ConstructorInfo constructor = typeof(Sc?).GetConstructor(new Type[] { typeof(Sc) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Sc)) };
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();
            Assert.Equal(new Sc?(value), f());
        }

        private static void VerifyWithParameterStructWithStringAndField(Scs value)
        {
            ConstructorInfo constructor = typeof(Scs?).GetConstructor(new Type[] { typeof(Scs) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Scs)) };
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile();
            Assert.Equal(new Scs?(value), f());
        }

        private static void VerifyWithParameterString(Sc value)
        {
            ConstructorInfo constructor = typeof(Sc?).GetConstructor(new Type[] { typeof(Sc) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Sc)) };
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile();
            Assert.Equal(new Sc?(value), f());
        }

        private static void VerifyWithParameterStructWithTwoValues(Sp value)
        {
            ConstructorInfo constructor = typeof(Sp?).GetConstructor(new Type[] { typeof(Sp) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Sp)) };
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile();
            Assert.Equal(new Sp?(value), f());
        }

        private static void VerifyWithParameterGenericWithStructRestriction<Ts>(Ts value) where Ts : struct
        {
            ConstructorInfo constructor = typeof(Ts?).GetConstructor(new Type[] { typeof(Ts) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Ts)) };
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile();
            Assert.Equal(new Ts?(value), f());
        }

        #endregion
    }
}
