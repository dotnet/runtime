// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Tests.ExpressionCompiler;
using Xunit;

namespace Tests.ExpressionCompiler.New
{
    public static unsafe class NewWithTwoParametersTests
    {
        #region Test methods

        [Fact]
        public static void CheckNewWithTwoParametersStructWithValueTest()
        {
            int[] array1 = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            double[] array2 = new double[] { 0, 1, -1, double.MinValue, double.MaxValue, double.Epsilon, double.NegativeInfinity, double.PositiveInfinity, double.NaN };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNewWithTwoParametersStructWithValue(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNewWithTwoParametersCustom2Test()
        {
            int[] array1 = new int[] { 0, 1, -1, int.MinValue, int.MaxValue };
            string[] array2 = new string[] { null, "", "a", "foo" }; ;
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNewWithTwoParametersCustom2(array1[i], array2[j]);
                }
            }
        }

        [Fact]
        public static void CheckNewWithTwoParametersStructWithStringAndValueTest()
        {
            string[] array1 = new string[] { null, "", "a", "foo" };
            S[] array2 = new S[] { default(S), new S() };
            for (int i = 0; i < array1.Length; i++)
            {
                for (int j = 0; j < array2.Length; j++)
                {
                    VerifyNewWithTwoParametersStructWithStringAndValue(array1[i], array2[j]);
                }
            }
        }

        #endregion

        #region Verifier methods

        private static void VerifyNewWithTwoParametersStructWithValue(int a, double b)
        {
            ConstructorInfo constructor = typeof(Sp).GetConstructor(new Type[] { typeof(int), typeof(double) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(a, typeof(int)), Expression.Constant(b, typeof(double)) };
            Expression<Func<Sp>> e =
                Expression.Lambda<Func<Sp>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Sp> f = e.Compile();

            Assert.Equal(new Sp(a, b), f());
        }

        private static void VerifyNewWithTwoParametersCustom2(int a, string b)
        {
            ConstructorInfo constructor = typeof(D).GetConstructor(new Type[] { typeof(int), typeof(string) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(a, typeof(int)), Expression.Constant(b, typeof(string)) };
            Expression<Func<D>> e =
                Expression.Lambda<Func<D>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<D> f = e.Compile();

            Assert.Equal(new D(a, b), f());
        }

        private static void VerifyNewWithTwoParametersStructWithStringAndValue(string a, S b)
        {
            ConstructorInfo constructor = typeof(Scs).GetConstructor(new Type[] { typeof(string), typeof(S) });
            Expression[] exprArgs = new Expression[] { Expression.Constant(a, typeof(string)), Expression.Constant(b, typeof(S)) };
            Expression<Func<Scs>> e =
                Expression.Lambda<Func<Scs>>(
                    Expression.New(constructor, exprArgs),
                    Enumerable.Empty<ParameterExpression>());
            Func<Scs> f = e.Compile();

            Assert.Equal(new Scs(a, b), f());
        }

        #endregion
    }
}
