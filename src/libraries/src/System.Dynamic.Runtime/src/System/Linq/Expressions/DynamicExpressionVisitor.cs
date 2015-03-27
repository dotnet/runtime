// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        protected internal virtual Expression VisitDynamic(DynamicExpression node)
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
