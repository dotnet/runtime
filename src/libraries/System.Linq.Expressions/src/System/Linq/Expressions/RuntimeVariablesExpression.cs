// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
    /// Evaluates to an instance of <see cref="IList{IStrongBox}"/> when executed.
    /// </summary>
    [DebuggerTypeProxy(typeof(RuntimeVariablesExpressionProxy))]
    public sealed class RuntimeVariablesExpression : Expression
    {
        internal RuntimeVariablesExpression(ReadOnlyCollection<ParameterExpression> variables)
        {
            Variables = variables;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type => typeof(IRuntimeVariables);

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.RuntimeVariables;

        /// <summary>
        /// The variables or parameters to which to provide runtime access.
        /// </summary>
        public ReadOnlyCollection<ParameterExpression> Variables { get; }

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
        /// <param name="variables">The <see cref="Variables"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
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

    public partial class Expression
    {
        /// <summary>
        /// Creates an instance of <see cref="RuntimeVariablesExpression"/>.
        /// </summary>
        /// <param name="variables">An array of <see cref="ParameterExpression"/> objects to use to populate the <see cref="RuntimeVariablesExpression.Variables"/> collection.</param>
        /// <returns>An instance of <see cref="RuntimeVariablesExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.RuntimeVariables"/> and the <see cref="RuntimeVariablesExpression.Variables"/> property set to the specified value.</returns>
        public static RuntimeVariablesExpression RuntimeVariables(params ParameterExpression[] variables)
        {
            return RuntimeVariables((IEnumerable<ParameterExpression>)variables);
        }

        /// <summary>
        /// Creates an instance of <see cref="RuntimeVariablesExpression"/>.
        /// </summary>
        /// <param name="variables">A collection of <see cref="ParameterExpression"/> objects to use to populate the <see cref="RuntimeVariablesExpression.Variables"/> collection.</param>
        /// <returns>An instance of <see cref="RuntimeVariablesExpression"/> that has the <see cref="NodeType"/> property equal to <see cref="ExpressionType.RuntimeVariables"/> and the <see cref="RuntimeVariablesExpression.Variables"/> property set to the specified value.</returns>
        public static RuntimeVariablesExpression RuntimeVariables(IEnumerable<ParameterExpression> variables)
        {
            ArgumentNullException.ThrowIfNull(variables);

            ReadOnlyCollection<ParameterExpression> vars = variables.ToReadOnly();
            for (int i = 0; i < vars.Count; i++)
            {
                ContractUtils.RequiresNotNull(vars[i], nameof(variables), i);
            }

            return new RuntimeVariablesExpression(vars);
        }
    }
}
