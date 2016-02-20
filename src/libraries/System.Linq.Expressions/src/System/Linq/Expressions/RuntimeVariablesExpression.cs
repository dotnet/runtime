// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;

namespace System.Linq.Expressions
{
    /// <summary>
    /// An expression that provides runtime read/write access to variables.
    /// Needed to implement "eval" in some dynamic languages.
    /// Evaluates to an instance of <see cref="IList{IStrongBox}" /> when executed.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.RuntimeVariablesExpressionProxy))]
    public sealed class RuntimeVariablesExpression : Expression
    {
        private readonly ReadOnlyCollection<ParameterExpression> _variables;

        internal RuntimeVariablesExpression(ReadOnlyCollection<ParameterExpression> variables)
        {
            _variables = variables;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents.
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type
        {
            get { return typeof(IRuntimeVariables); }
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return ExpressionType.RuntimeVariables; }
        }

        /// <summary>
        /// The variables or parameters to which to provide runtime access.
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Variables
        {
            get { return _variables; }
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitRuntimeVariables(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="variables">The <see cref="Variables" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public RuntimeVariablesExpression Update(IEnumerable<ParameterExpression> variables)
        {
            if (variables == Variables)
            {
                return this;
            }
            return Expression.RuntimeVariables(variables);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates an instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" />.
        /// </summary>
        /// <param name="variables">An array of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> collection.</param>
        /// <returns>An instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.RuntimeVariables" /> and the <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property set to the specified value.</returns>
        public static RuntimeVariablesExpression RuntimeVariables(params ParameterExpression[] variables)
        {
            return RuntimeVariables((IEnumerable<ParameterExpression>)variables);
        }

        /// <summary>
        /// Creates an instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" />.
        /// </summary>
        /// <param name="variables">A collection of <see cref="T:System.Linq.Expressions.ParameterExpression" /> objects to use to populate the <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> collection.</param>
        /// <returns>An instance of <see cref="T:System.Linq.Expressions.RuntimeVariablesExpression" /> that has the <see cref="P:System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="F:System.Linq.Expressions.ExpressionType.RuntimeVariables" /> and the <see cref="P:System.Linq.Expressions.RuntimeVariablesExpression.Variables" /> property set to the specified value.</returns>
        public static RuntimeVariablesExpression RuntimeVariables(IEnumerable<ParameterExpression> variables)
        {
            ContractUtils.RequiresNotNull(variables, nameof(variables));

            var vars = variables.ToReadOnly();
            for (int i = 0; i < vars.Count; i++)
            {
                Expression v = vars[i];
                if (v == null)
                {
                    throw new ArgumentNullException($"{nameof(variables)}[{i}]");
                }
            }

            return new RuntimeVariablesExpression(vars);
        }
    }
}
