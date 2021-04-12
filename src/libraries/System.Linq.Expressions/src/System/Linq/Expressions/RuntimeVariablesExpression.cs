// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>An expression that provides runtime read/write permission for variables.</summary>
    /// <remarks>This type is necessary for implementing "eval" in dynamic languages. It evaluates to an instance of <see cref="System.Collections.Generic.IList{T}" /> at run time.</remarks>
    [DebuggerTypeProxy(typeof(RuntimeVariablesExpressionProxy))]
    public sealed class RuntimeVariablesExpression : Expression
    {
        internal RuntimeVariablesExpression(ReadOnlyCollection<ParameterExpression> variables)
        {
            Variables = variables;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.RuntimeVariablesExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => typeof(IRuntimeVariables);

        /// <summary>Returns the node type of this Expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.RuntimeVariables;

        /// <summary>The variables or parameters to which to provide runtime access.</summary>
        /// <value>The read-only collection containing parameters that will be provided the runtime access.</value>
        public ReadOnlyCollection<ParameterExpression> Variables { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitRuntimeVariables(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="variables">The <see cref="System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public RuntimeVariablesExpression Update(IEnumerable<ParameterExpression> variables)
        {
            if (variables != null)
            {
                if (ExpressionUtils.SameElements(ref variables!, Variables))
                {
                    return this;
                }
            }

            return RuntimeVariables(variables!);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates an instance of <see cref="System.Linq.Expressions.RuntimeVariablesExpression" />.</summary>
        /// <param name="variables">An array of <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> collection.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.RuntimeVariablesExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RuntimeVariables" /> and the <see cref="System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property set to the specified value.</returns>
        public static RuntimeVariablesExpression RuntimeVariables(params ParameterExpression[] variables)
        {
            return RuntimeVariables((IEnumerable<ParameterExpression>)variables);
        }

        /// <summary>Creates an instance of <see cref="System.Linq.Expressions.RuntimeVariablesExpression" />.</summary>
        /// <param name="variables">A collection of <see cref="System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> collection.</param>
        /// <returns>An instance of <see cref="System.Linq.Expressions.RuntimeVariablesExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.RuntimeVariables" /> and the <see cref="System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property set to the specified value.</returns>
        public static RuntimeVariablesExpression RuntimeVariables(IEnumerable<ParameterExpression> variables)
        {
            ContractUtils.RequiresNotNull(variables, nameof(variables));

            ReadOnlyCollection<ParameterExpression> vars = variables.ToReadOnly();
            for (int i = 0; i < vars.Count; i++)
            {
                ContractUtils.RequiresNotNull(vars[i], nameof(variables), i);
            }

            return new RuntimeVariablesExpression(vars);
        }
    }
}
