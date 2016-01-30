// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryArithmeticNegateNullableOneOffTests
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
