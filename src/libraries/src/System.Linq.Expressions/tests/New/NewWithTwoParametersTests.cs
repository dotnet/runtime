// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class NewWithTwoParametersTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithTwoParametersStructWithValueTest(bool useInterpreter)
        {
            int[] array1 = { 0, 1, -1, int.MinValue, int.MaxValue };
            double[] array2 = { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNewWithTwoParametersStructWithValue(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithTwoParametersCustom2Test(bool useInterpreter)
        {
            int[] array1 = { 0, 1, -1, int.MinValue, int.MaxValue };
            string[] array2 = { null, "", "a", "foo" }; ;
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNewWithTwoParametersCustom2(array1[i], array2[j], useInterpreter);
                }
            }
        }

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckNewWithTwoParametersStructWithStringAndValueTest(bool useInterpreter)
        {
            string[] array1 = { null, "", "a", "foo" };
            S[] array2 = { default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNewWithTwoParametersStructWithStringAndValue(array1[i], array2[j], useInterpreter);
                }
            }
        }

        #endregion

        #region Verifier methods

        private static void VerifyNewWithTwoParametersStructWithValue(int a, double b, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(Sp).GetConstructor(new[] { typeof(int), typeof(double) });
            Expression[] exprArgs = { Expression.Constant(a, typeof(int)), Expression.Constant(b, typeof(double)) };
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile(useInterpreter);

            Assert.Equal(new Sp(a, b), f());
        }

        private static void VerifyNewWithTwoParametersCustom2(int a, string b, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(D).GetConstructor(new[] { typeof(int), typeof(string) });
            Expression[] exprArgs = { Expression.Constant(a, typeof(int)), Expression.Constant(b, typeof(string)) };
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile(useInterpreter);

            Assert.Equal(new D(a, b), f());
        }

        private static void VerifyNewWithTwoParametersStructWithStringAndValue(string a, S b, bool useInterpreter)
        {
            ConstructorInfo constructor = typeof(Scs).GetConstructor(new[] { typeof(string), typeof(S) });
            Expression[] exprArgs = { Expression.Constant(a, typeof(string)), Expression.Constant(b, typeof(S)) };
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile(useInterpreter);

            Assert.Equal(new Scs(a, b), f());
        }

        #endregion

        [Fact]
        public static void UpdateSameReturnsSame()
        {
            ConstructorInfo constructor = typeof(Sp).GetConstructor(new[] { typeof(int), typeof(double) });
            Expression[] exprArgs = { Expression.Constant(23), Expression.Constant(40.0) };
            NewExpression newExp = Expression.New(constructor, exprArgs);
            Assert.Same(newExp, newExp.Update(exprArgs));

            constructor = typeof(TestClass).GetConstructor(new[] { typeof(string), typeof(int) });
            MemberInfo[] members = { typeof(TestClass).GetField(nameof(TestClass.S)), typeof(TestClass).GetProperty(nameof(TestClass.Val)) };
            exprArgs = new Expression[] { Expression.Constant("x"), Expression.Constant(23) };

            newExp = Expression.New(constructor, exprArgs, members);
            Assert.Same(newExp, newExp.Update(exprArgs));
        }

        [Fact]
        public static void UpdateDifferentReturnsDifferent()
        {
            ConstructorInfo constructor = typeof(Sp).GetConstructor(new[] { typeof(int), typeof(double) });
            NewExpression newExp = Expression.New(constructor, Expression.Constant(23), Expression.Constant(40.0));
            Assert.NotSame(newExp, newExp.Update(new[] {Expression.Constant(23), Expression.Constant(40.0)}));

            constructor = typeof(TestClass).GetConstructor(new[] { typeof(string), typeof(int) });
            MemberInfo[] members = { typeof(TestClass).GetField(nameof(TestClass.S)), typeof(TestClass).GetProperty(nameof(TestClass.Val)) };

            newExp = Expression.New(constructor, new[] { Expression.Constant("x"), Expression.Constant(23) }, members);
            Assert.NotSame(newExp, newExp.Update(new[] { Expression.Constant("x"), Expression.Constant(23) }));
        }

        [Fact]
        public static void UpdateDoesntRepeatEnumeration()
        {
            ConstructorInfo constructor = typeof(Sp).GetConstructor(new[] { typeof(int), typeof(double) });
            NewExpression newExp = Expression.New(constructor, Expression.Constant(23), Expression.Constant(40.0));
            Assert.NotSame(newExp, newExp.Update(new[] { Expression.Constant(23), Expression.Constant(40.0) }));

            constructor = typeof(TestClass).GetConstructor(new[] { typeof(string), typeof(int) });
            MemberInfo[] members = { typeof(TestClass).GetField(nameof(TestClass.S)), typeof(TestClass).GetProperty(nameof(TestClass.Val)) };

            newExp = Expression.New(constructor, new[] { Expression.Constant("x"), Expression.Constant(23) }, members);
            Assert.NotSame(newExp, newExp.Update(new RunOnceEnumerable<Expression>(new[] { Expression.Constant("x"), Expression.Constant(23) })));
        }

        private class TestClass
        {
            public TestClass(string s, int val)
            {
                S = s;
                Val = val;
            }

            public string S;

            public int Val { get; set; }
        }
    }
}
