// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents the default value of a type or an empty expression.
    /// </summary>
    [DebuggerTypeProxy(typeof(DefaultExpressionProxy))]
    public sealed class DefaultExpression : Expression
    {
        internal DefaultExpression(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents.
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type { get; }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Default;

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
        /// Creates an empty expression that has <see cref="Void"/> type.
        /// </summary>
        /// <returns>
        /// A <see cref="DefaultExpression"/> that has the <see cref="NodeType"/> property equal to
        /// <see cref="ExpressionType.Default"/> and the <see cref="Type"/> property set to <see cref="Void"/>.
        /// </returns>
        public static DefaultExpression Empty()
        {
            return new DefaultExpression(typeof(void)); // Create new object each time for different identity
        }

        /// <summary>
        /// Creates a <see cref="DefaultExpression"/> that has the <see cref="Type"/> property set to the specified type.
        /// </summary>
        /// <param name="type">A <see cref="Type"/> to set the <see cref="Type"/> property equal to.</param>
        /// <returns>
        /// A <see cref="DefaultExpression"/> that has the <see cref="NodeType"/> property equal to
        /// <see cref="ExpressionType.Default"/> and the <see cref="Type"/> property set to the specified type.
        /// </returns>
        public static DefaultExpression Default(Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (type.IsByRef)
            {
                throw Error.TypeMustNotBeByRef(nameof(type));
            }

            if (type.IsPointer)
            {
                throw Error.TypeMustNotBePointer(nameof(type));
            }

            return new DefaultExpression(type);
        }
    }
}
