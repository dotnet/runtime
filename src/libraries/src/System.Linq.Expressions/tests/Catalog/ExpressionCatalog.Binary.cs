// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Assign()
        {
            foreach (var a in GetAssignments(Expression.Constant(42), Expression.Constant(43), (e, v) => Expression.Assign(e, v)))
            {
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Assign, a);
            }

            foreach (var a in GetAssignments(Expression.Constant(42), Expression.Constant(43), (e, v) => Expression.Block(Expression.Assign(e, v), e)))
            {
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Assign, a);
            }
        }

        private static IEnumerable<Expression> Coalesce()
        {
            var ex = Expression.Constant(new Exception("Oops!"));
            var er = Expression.Constant(new Exception("!spoO"));

            yield return Expression.Coalesce(Expression.Constant("bar", typeof(string)), Expression.Constant("foo", typeof(string)));
            yield return Expression.Coalesce(Expression.Constant(null, typeof(string)), Expression.Constant("foo", typeof(string)));
            yield return Expression.Coalesce(Expression.Constant("bar", typeof(string)), Expression.Throw(ex, typeof(string)));
            yield return Expression.Coalesce(Expression.Constant(null, typeof(string)), Expression.Throw(ex, typeof(string)));
            yield return Expression.Coalesce(Expression.Throw(ex, typeof(string)), Expression.Constant("foo", typeof(string)));
            yield return Expression.Coalesce(Expression.Throw(ex, typeof(string)), Expression.Throw(er, typeof(string)));

            yield return Expression.Coalesce(Expression.Constant(42, typeof(int?)), Expression.Constant(43, typeof(int?)));
            yield return Expression.Coalesce(Expression.Constant(null, typeof(int?)), Expression.Constant(43, typeof(int?)));
            yield return Expression.Coalesce(Expression.Constant(42, typeof(int?)), Expression.Throw(ex, typeof(int?)));
            yield return Expression.Coalesce(Expression.Constant(null, typeof(int?)), Expression.Throw(ex, typeof(int?)));
            yield return Expression.Coalesce(Expression.Throw(ex, typeof(int?)), Expression.Constant(43, typeof(int?)));
            yield return Expression.Coalesce(Expression.Throw(ex, typeof(int?)), Expression.Throw(er, typeof(int?)));

            // TODO: with conversion
        }
    }
}
