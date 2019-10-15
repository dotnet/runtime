// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryUnboxTests
    {
        #region Test methods

        [Theory, ClassData(typeof(CompilationTypes))]
        public static void CheckUnaryUnboxTest(bool useInterpreter)
        {
            VerifyUnbox(42, typeof(int), false, useInterpreter);
            VerifyUnbox(42, typeof(int?), false, useInterpreter);
            VerifyUnbox(null, typeof(int?), false, useInterpreter);
            VerifyUnbox(null, typeof(int), true, useInterpreter);
        }

        [Fact]
        public static void ToStringTest()
        {
            UnaryExpression e = Expression.Unbox(Expression.Parameter(typeof(object), "x"), typeof(int));
            Assert.Equal("Unbox(x)", e.ToString());
        }

        #endregion

        #region Test verifiers

        private static void VerifyUnbox(object value, Type type, bool shouldThrow, bool useInterpreter)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(
                        Expression.Unbox(Expression.Constant(value, typeof(object)), type),
                        typeof(object)),
                    Enumerable.Empty<ParameterExpression>());

            Func<object> f = e.Compile(useInterpreter);

            if (shouldThrow)
            {
                Assert.Throws<NullReferenceException>(() => f());
            }
            else
            {
                Assert.Equal(value, f());
            }
        }


        #endregion
    }
}
