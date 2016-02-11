// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.CompilerServices.ConditionalWeakTable<,>))]

namespace System.Linq.Expressions
{
    public partial class DynamicExpressionVisitor
    {
        // Member is excluded in the base so it is automatically excluded for this type without any way to override that behavior
        protected virtual Expression VisitDynamic(DynamicExpression node) { return default(Expression); }
    }
}
