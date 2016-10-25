// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents a label, which can be placed in any <see cref="Expression"/> context. If
    /// it is jumped to, it will get the value provided by the corresponding
    /// <see cref="GotoExpression"/>. Otherwise, it gets the value in <see cref="DefaultValue"/>. If the
    /// <see cref="Type"/> equals System.Void, no value should be provided.
    /// </summary>
    [DebuggerTypeProxy(typeof(LabelExpressionProxy))]
    public sealed class LabelExpression : Expression
    {
        internal LabelExpression(LabelTarget label, Expression defaultValue)
        {
            Target = label;
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression"/> represents. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="System.Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type => Target.Type;

        /// <summary>
        /// Returns the node type of this <see cref="Expression"/>. (Inherited from <see cref="Expression"/>.)
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> that represents this expression.</returns>
        public sealed override ExpressionType NodeType => ExpressionType.Label;

        /// <summary>
        /// The <see cref="LabelTarget"/> which this label is associated with.
        /// </summary>
        public LabelTarget Target { get; }

        /// <summary>
        /// The value of the <see cref="LabelExpression"/> when the label is reached through
        /// normal control flow (e.g. is not jumped to).
        /// </summary>
        public Expression DefaultValue { get; }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitLabel(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="target">The <see cref="Target"/> property of the result.</param>
        /// <param name="defaultValue">The <see cref="DefaultValue"/> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public LabelExpression Update(LabelTarget target, Expression defaultValue)
        {
            if (target == Target && defaultValue == DefaultValue)
            {
                return this;
            }
            return Expression.Label(target, defaultValue);
        }
    }

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="LabelExpression"/> representing a label with no default value.
        /// </summary>
        /// <param name="target">The <see cref="LabelTarget"/> which this <see cref="LabelExpression"/> will be associated with.</param>
        /// <returns>A <see cref="LabelExpression"/> with no default value.</returns>
        public static LabelExpression Label(LabelTarget target)
        {
            return Label(target, null);
        }

        /// <summary>
        /// Creates a <see cref="LabelExpression"/> representing a label with the given default value.
        /// </summary>
        /// <param name="target">The <see cref="LabelTarget"/> which this <see cref="LabelExpression"/> will be associated with.</param>
        /// <param name="defaultValue">The value of this <see cref="LabelExpression"/> when the label is reached through normal control flow.</param>
        /// <returns>A <see cref="LabelExpression"/> with the given default value.</returns>
        public static LabelExpression Label(LabelTarget target, Expression defaultValue)
        {
            ValidateGoto(target, ref defaultValue, nameof(target), nameof(defaultValue), null);
            return new LabelExpression(target, defaultValue);
        }
    }
}
