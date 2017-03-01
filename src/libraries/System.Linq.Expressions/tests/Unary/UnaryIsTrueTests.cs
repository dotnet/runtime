// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryIsTrueTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryIsTrueBoolTest(bool useInterpreter)
        {
            bool[] values = new bool[] { false, true };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIsTrueBool(values[i], useInterpreter);
            }
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.IsTrue(Expression.Parameter(typeof(bool), "x"));
            Assert.Equal("IsTrue(x)", e.ToString());
        }

        #endregion

        #region Test verifiers

        private static void VerifyIsTrueBool(bool value, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.IsTrue(Expression.Constant(value, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile(useInterpreter);
            Assert.Equal((bool)(value == true), f());
        }

        #endregion

        private static IEnumerable<object[]> Truthinesses()
        {
            yield return new object[] {new Truthiness(true), true};
            yield return new object[] {new Truthiness(false), false};
        }

        [Theory, PerCompilationType(nameof(Truthinesses))]
        private static void VerifyMakeUnaryExplicitMethodIsTrueBool(Truthiness argument, bool expected, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.MakeUnary(
                        ExpressionType.IsTrue, Expression.Constant(argument), null, typeof(Truthiness).GetMethod("op_True")));
            Func<bool> f = e.Compile(useInterpreter);
            Assert.Equal(expected, f());
        }

        [Theory, PerCompilationType(nameof(Truthinesses))]
        private static void VerifyMakeUnaryDeduceMethodIsTrueBool(Truthiness argument, bool expected, bool useInterpreter)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.MakeUnary(
                        ExpressionType.IsTrue, Expression.Constant(argument), null, null));
            Func<bool> f = e.Compile(useInterpreter);
            Assert.Equal(expected, f());
        }
    }
}
