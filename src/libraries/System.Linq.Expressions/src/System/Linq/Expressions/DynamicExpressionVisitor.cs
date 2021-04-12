// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents a visitor or rewriter for dynamic expression trees.</summary>
    /// <remarks>This class is designed to be inherited to create more specialized classes whose functionality requires traversing, examining, or copying a dynamic expression tree.</remarks>
    public class DynamicExpressionVisitor : ExpressionVisitor
    {
        /// <summary>Visits the children of the <see cref="System.Linq.Expressions.DynamicExpression" />.</summary>
        /// <param name="node">The expression to visit.</param>
        /// <returns>Returns <see cref="System.Linq.Expressions.Expression" />, the modified expression, if it or any subexpression is modified; otherwise, returns the original expression.</returns>
        protected internal override Expression VisitDynamic(DynamicExpression node)
        {
            Expression[]? a = ExpressionVisitorUtils.VisitArguments(this, node);
            if (a == null)
            {
                return node;
            }

            return node.Rewrite(a);
        }
    }
}
