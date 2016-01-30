// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Linq.Expressions.Tests
{
    partial class ExpressionCatalog
    {
        private static IEnumerable<KeyValuePair<ExpressionType, Expression>> DebugInfo()
        {
            var doc = Expression.SymbolDocument("foo.cs");

            yield return new KeyValuePair<ExpressionType, Expression>(ExpressionType.DebugInfo,
                Expression.Block(
                    Expression.DebugInfo(doc, 1, 2, 3, 4),
                    Expression.Constant(41),
                    Expression.DebugInfo(doc, 2, 3, 4, 5),
                    Expression.Constant(42)
                )
            );
        }
    }
}
