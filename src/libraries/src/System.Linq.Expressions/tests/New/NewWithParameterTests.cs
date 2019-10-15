// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NewWithParameterTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterEnumTest(bool useInterpreter)
        {
            foreach (E value in new E[] { (E)0, E.A, E.B, (E)int.MaxValue, (E)int.MinValue })
            {
                VerifyWithParameterEnum(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterIntTest(bool useInterpreter)
        {
            foreach (int value in new int[] { 0, 1, -1, int.MinValue, int.MaxValue })
            {
                VerifyWithParameterInt(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterStructTest(bool useInterpreter)
        {
            foreach (S value in new S[] { default(S), new S() })
            {
                VerifyWithParameterStruct(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterStructWithStringTest(bool useInterpreter)
        {
            foreach (Sc value in new Sc[] { default(Sc), new Sc(), new Sc(null) })
            {
                VerifyWithParameterStructWithString(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterStructWithStringAndFieldTest(bool useInterpreter)
        {
            foreach (Scs value in new Scs[] { default(Scs), new Scs(), new Scs(null, new S()) })
            {
                VerifyWithParameterStructWithStringAndField(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterStructWithTwoValuesTest(bool useInterpreter)
        {
            foreach (Sp value in new Sp[] { default(Sp), new Sp(), new Sp(5, 5.0) })
            {
                VerifyWithParameterStructWithTwoValues(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterStringTest(bool useInterpreter)
        {
            foreach (Sc value in new Sc[] { default(Sc), new Sc(), new Sc(null) })
            {
                VerifyWithParameterString(value, useInterpreter);
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterGenericWithStructRestrictionWithEnumTest(bool useInterpreter)
        {
            CheckNewWithParameterGenericWithStructRestrictionHelper<E>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterGenericWithStructRestrictionWithStructTest(bool useInterpreter)
        {
            CheckNewWithParameterGenericWithStructRestrictionHelper<S>(useInterpreter);
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithParameterGenericWithStructRestrictionWithStructWithStringAndFieldTest(bool useInterpreter)
        {
            CheckNewWithParameterGenericWithStructRestrictionHelper<Scs>(useInterpreter);
        }

        #endregion

        #region Generic helpers

        private static void CheckNewWithParameterGenericWithStructRestrictionHelper<Ts>(bool useInterpreter) where Ts : struct
        {
            foreach (Ts value in new Ts[] { default(Ts), new Ts() })
            {
                VerifyWithParameterGenericWithStructRestriction<Ts>(value, useInterpreter);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyWithParameterEnum(E value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(E?).GetConstructor(new Type[] { typeof(E) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(E)) };
            Expression<Func<E?>> e =
                Expression.Lambda<Func<E?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<E?> f = e.Compile(useInterpreter);
            Assert.Equal(new E?(value), f());
        }

        private static void VerifyWithParameterInt(int value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(int?).GetConstructor(new Type[] { typeof(int) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(int)) };
            Expression<Func<int?>> e =
                Expression.Lambda<Func<int?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<int?> f = e.Compile(useInterpreter);
            Assert.Equal(new int?(value), f());
        }

        private static void VerifyWithParameterStruct(S value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(S?).GetConstructor(new Type[] { typeof(S) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(S)) };
            Expression<Func<S?>> e =
                Expression.Lambda<Func<S?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<S?> f = e.Compile(useInterpreter);
            Assert.Equal(new S?(value), f());
        }

        private static void VerifyWithParameterStructWithString(Sc value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(Sc?).GetConstructor(new Type[] { typeof(Sc) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Sc)) };
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);
            Assert.Equal(new Sc?(value), f());
        }

        private static void VerifyWithParameterStructWithStringAndField(Scs value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(Scs?).GetConstructor(new Type[] { typeof(Scs) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Scs)) };
            Expression<Func<Scs?>> e =
                Expression.Lambda<Func<Scs?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs?> f = e.Compile(useInterpreter);
            Assert.Equal(new Scs?(value), f());
        }

        private static void VerifyWithParameterString(Sc value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(Sc?).GetConstructor(new Type[] { typeof(Sc) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Sc)) };
            Expression<Func<Sc?>> e =
                Expression.Lambda<Func<Sc?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sc?> f = e.Compile(useInterpreter);
            Assert.Equal(new Sc?(value), f());
        }

        private static void VerifyWithParameterStructWithTwoValues(Sp value, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(Sp?).GetConstructor(new Type[] { typeof(Sp) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Sp)) };
            Expression<Func<Sp?>> e =
                Expression.Lambda<Func<Sp?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp?> f = e.Compile(useInterpreter);
            Assert.Equal(new Sp?(value), f());
        }

        private static void VerifyWithParameterGenericWithStructRestriction<Ts>(Ts value, bool useInterpreter) where Ts : struct
        {
            ConstructorInfo constructor = typeof(Ts?).GetConstructor(new Type[] { typeof(Ts) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(value, typeof(Ts)) };
            Expression<Func<Ts?>> e =
                Expression.Lambda<Func<Ts?>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Ts?> f = e.Compile(useInterpreter);
            Assert.Equal(new Ts?(value), f());
        }

        #endregion
    }
}
