// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryArithmeticNegateNullableOneOffTests
    {
        [Theory, ClassData(typeof(CompilationTypes))]
        public static void UnaryArithmeticNegateNullableStackBalance(bool useInterpreter)
        {
            Expression<Func<decimal?>> e = Expression.Lambda<Func<decimal?>>(
                Expression.Negate(
                    Expression.Negate(
                        Expression.Constant(1.0m, typeof(decimal?))
                    )
                )
            );

            Func<decimal?> f = e.Compile(useInterpreter);

            Assert.True(f() == 1.0m);
        }
    }
}
