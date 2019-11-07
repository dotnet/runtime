// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Linq.Expressions.Tests
{
    public static class IndexExpressionHelpers
    {
        internal static void AssertEqual(IndexExpression expected, IndexExpression actual)
        {
            Assert.Equal(expected.Object, actual.Object);
            Assert.Equal(expected.Indexer, actual.Indexer);
            Assert.Equal(expected.Arguments, actual.Arguments);
        }

        internal static void AssertInvokeCorrect<T>(T expected, IndexExpression expression)
        {
            Expression<Func<T>> lambda = Expression.Lambda<Func<T>>(expression);

            // Compile and evaluate with interpretation flag and without
            // in case there are bugs in the compiler/interpreter.
            Assert.Equal(expected, lambda.Compile(false)());
            Assert.Equal(expected, lambda.Compile(true)());
        }
    }
}
