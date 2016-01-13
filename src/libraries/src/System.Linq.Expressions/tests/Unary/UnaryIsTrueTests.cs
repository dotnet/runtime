// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Reflection;
using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class UnaryIsTrueTests
    {
        #region Test methods

        [Fact] //[WorkItem(3196, "https://github.com/dotnet/corefx/issues/3196")]
        public static void CheckUnaryIsTrueBoolTest()
        {
            bool[] values = new bool[] { false, true };
            for (int i = 0; i < values.Length; i++)
            {
                VerifyIsTrueBool(values[i]);
            }
        }

        #endregion

        #region Test verifiers

        private static void VerifyIsTrueBool(bool value)
        {
            Expression<Func<bool>> e =
                Expression.Lambda<Func<bool>>(
                    Expression.IsTrue(Expression.Constant(value, typeof(bool))),
                    Enumerable.Empty<ParameterExpression>());
            Func<bool> f = e.Compile();
            Assert.Equal((bool)(value == true), f());
        }


        #endregion
    }
}
