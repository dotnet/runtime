// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents the default value of a type or an empty expression.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.DefaultExpressionProxy))]
    public sealed class DefaultExpression : Expression
    {
        private readonly Type _type;

        internal DefaultExpression(Type type)
        {
            _type = type;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents.
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type
        {
            get { return _type; }
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return ExpressionType.Default; }
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitDefault(this);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates an empty expression that has <see cref="System.Void"/> type.
        /// </summary>
        /// <returns>
        /// A <see cref="DefaultExpression"/> that has the <see cref="P:Expression.NodeType"/> property equal to 
        /// <see cref="F:ExpressionType.Default"/> and the <see cref="P:Expression.Type"/> property set to <see cref="System.Void"/>.
        /// </returns>
        public static DefaultExpression Empty()
        {
            return new DefaultExpression(typeof(void)); // Create new object each time for different identity 
        }

        /// <summary>
        /// Creates a <see cref="DefaultExpression"/> that has the <see cref="P:Expression.Type"/> property set to the specified type.
        /// </summary>
        /// <param name="type">A <see cref="System.Type"/> to set the <see cref="P:Expression.Type"/> property equal to.</param>
        /// <returns>
        /// A <see cref="DefaultExpression"/> that has the <see cref="P:Expression.NodeType"/> property equal to 
        /// <see cref="F:ExpressionType.Default"/> and the <see cref="P:Expression.Type"/> property set to the specified type.
        /// </returns>
        public static DefaultExpression Default(Type type)
        {
            return new DefaultExpression(type);
        }
    }
}
