// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace Tests.ExpressionCompiler.Unary
{
    public static class UnaryIsTrueNullableTests
    {
        #region Test methods

        [Fact] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryIsTrueNullableBoolTest()
        {
            bool?[] values = new bool?[] { null, false, true };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIsTrueNullableBool(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyIsTrueNullableBool(bool? value)
        {
            Expression<Func<bool?>> e =
                Expression.Lambda<Func<bool?>>(
                    Expression.IsTrue(Expression.Constant(value, typeof(bool?))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool?> f = e.Compile();
            Assert.Equal((bool?)(value == default(bool?) ? default(bool?) : value == true), f());
        }


        #endregion
    }
}
