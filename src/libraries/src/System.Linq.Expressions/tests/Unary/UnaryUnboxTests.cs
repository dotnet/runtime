// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryUnboxTests
    {
        #region Test methods

        [Fact] //[WorkItem(4021, "https://github.com/dotnet/corefx/issues/4021")]
        public static void CheckUnaryUnboxTest()
        {
            VerifyUnbox(42, typeof(int), false);
            VerifyUnbox(42, typeof(int?), false);
            VerifyUnbox(null, typeof(int?), false);
            VerifyUnbox(null, typeof(int), true);
        }

        #endregion

        #region Test verifiers

        private static void VerifyUnbox(object value, Type type, bool shouldThrow)
        {
            Expression<Func<object>> e =
                Expression.Lambda<Func<object>>(
                    Expression.Convert(
                        Expression.Unbox(Expression.Constant(value, typeof(object)), type),
                        typeof(object)),
                    Enumerable.Empty<ParameterExpression>());

            Func<object> f = e.Compile();

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
