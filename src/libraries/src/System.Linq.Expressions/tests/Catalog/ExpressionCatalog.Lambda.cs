// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Tests.Expressions
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> Lambda()
        {
            for (var i = 0; i <= 16; i++)
            {
                var p = Enumerable.Range(0, i).Select(_ => Expression.Parameter(typeof(int))).ToArray();
                var c = p.Aggregate((Expression)Expression.Constant(0), (s, e) => Expression.Add(s, e));
                var f = Expression.Lambda(c, p);
                var a = Enumerable.Range(42, i).Select(j => Expression.Constant(j)).ToArray();
                var r = Expression.Invoke(f, a);
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Lambda, r);
            }

            for (var i = 0; i <= 16; i++)
            {
                var p = Enumerable.Range(0, i).Select(_ => Expression.Parameter(typeof(int))).ToArray();
                var c = p.Aggregate((Expression)Expression.Constant(0), (s, e) => Expression.Add(s, e));
                var f = p.Aggregate(c, (s, q) => Expression.Lambda(s, q));
                var a = Enumerable.Range(42, i).Select(j => Expression.Constant(j)).ToArray();
                var r = a.Aggregate(f, (g, b) => Expression.Invoke(g, b));
                yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.Lambda, r);
            }
        }
    }
}