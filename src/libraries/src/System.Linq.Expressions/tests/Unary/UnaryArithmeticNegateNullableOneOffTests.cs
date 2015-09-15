// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq.Expressions;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static unsafe class UnaryArithmeticNegateNullableOneOffTests
    {
        [Fact] //[WorkItem(3197, "https://github.com/dotnet/corefx/issues/3197")]
        public static void UnaryArithmeticNegateNullableStackBalance()
        {
            var e = Expression.Lambda<Func<decimal?>>(
                Expression.Negate(
                    Expression.Negate(
                        Expression.Constant(1.0m, typeof(decimal?))
                    )
                )
            );

            var f = e.Compile();

            Assert.True(f() == 1.0m);
        }
    }
}
