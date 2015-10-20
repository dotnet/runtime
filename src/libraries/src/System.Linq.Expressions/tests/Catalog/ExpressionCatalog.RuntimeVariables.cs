// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> RuntimeVariables()
        {
            var p0 = Expression.Parameter(typeof(int));
            var p1 = Expression.Parameter(typeof(int));
            var p2 = Expression.Parameter(typeof(int));
            var rv = Expression.Parameter(typeof(IRuntimeVariables));

            var getInfo = ((MethodCallExpression)((Expression<Func<IRuntimeVariables, string>>)(rvp => GetInfo(rvp))).Body).Method;

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.RuntimeVariables,
                Expression.Block(
                    new[] { p0, p1, p2, rv },
                    Expression.Assign(p0, Expression.Constant(1)),
                    Expression.Assign(p1, Expression.Constant(2)),
                    Expression.Assign(p2, Expression.Constant(3)),
                    Expression.Assign(rv, Expression.RuntimeVariables(p0, p2)),
                    Expression.Call(getInfo, rv)
                )
            );
        }

        private static string GetInfo(IRuntimeVariables vars)
        {
            return "{ Count = " + vars.Count + ", Values = { " + string.Join(", ", Enumerable.Range(0, vars.Count).Select(i => vars[i])) + " } }";
        }
    }
}