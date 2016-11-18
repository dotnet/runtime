// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Linq.Expressions
{
    public abstract partial class ExpressionVisitor
    {
        /// <summary>
        /// Visits the children of the <see cref="DynamicExpression" />.
        /// </summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>The modified expression, if it or any subexpression was modified;
        /// otherwise, returns the original expression.</returns>
        protected internal virtual Expression VisitDynamic(DynamicExpression node)
        {
            Expression[] a = VisitArguments((IArgumentProvider)node);
            if (a == null)
            {
                return node;
            }

            return node.Rewrite(a);
        }
    }
}
