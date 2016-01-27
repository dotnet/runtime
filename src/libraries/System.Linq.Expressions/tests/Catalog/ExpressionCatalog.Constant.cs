// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<Expression> Constant()
        {
            yield return Expression.Constant(null, typeof(object));
            yield return Expression.Constant(null, typeof(string));
            yield return Expression.Constant(null, typeof(IDisposable));
            yield return Expression.Constant(null, typeof(int?));

            yield return Expression.Constant(42, typeof(object));
            yield return Expression.Constant(42, typeof(ValueType));
            yield return Expression.Constant(42, typeof(int));
            yield return Expression.Constant(42, typeof(int?));

            yield return Expression.Constant("", typeof(string));
            yield return Expression.Constant("bar", typeof(string));
            yield return Expression.Constant("foo", typeof(IComparable));
        }
    }
}
