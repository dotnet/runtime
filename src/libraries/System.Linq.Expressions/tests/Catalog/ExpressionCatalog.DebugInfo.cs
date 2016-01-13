// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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