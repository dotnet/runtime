// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    public class DynamicExpressionVisitor : ExpressionVisitor
    {
        /// <summary>
        /// Visits the children of the <see cref="DynamicExpression" />.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected internal override Expression VisitDynamic(DynamicExpression node)
        {
            Expression[] a = ExpressionVisitorUtils.VisitArguments(this, node);
            if (a == null)
            {
                return node;
            }

            return node.Rewrite(a);
        }
    }
}
