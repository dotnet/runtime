// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Linq.Expressions
{
    /// <summary>Represents a label, which can be put in any <see cref="System.Linq.Expressions.Expression" /> context. If it is jumped to, it will get the value provided by the corresponding <see cref="System.Linq.Expressions.GotoExpression" />. Otherwise, it receives the value in <see cref="System.Linq.Expressions.LabelExpression.DefaultValue" />. If the <see cref="System.Type" /> equals System.Void, no value should be provided.</summary>
    [DebuggerTypeProxy(typeof(LabelExpressionProxy))]
    public sealed class LabelExpression : Expression
    {
        internal LabelExpression(LabelTarget label, Expression? defaultValue)
        {
            Target = label;
            DefaultValue = defaultValue;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LabelExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => Target.Type;

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Label;

        /// <summary>The <see cref="System.Linq.Expressions.LabelTarget" /> which this label is associated with.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LabelTarget" /> which this label is associated with.</value>
        public LabelTarget Target { get; }

        /// <summary>The value of the <see cref="System.Linq.Expressions.LabelExpression" /> when the label is reached through regular control flow (for example, is not jumped to).</summary>
        /// <value>The Expression object representing the value of the <see cref="System.Linq.Expressions.LabelExpression" />.</value>
        public Expression? DefaultValue { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitLabel(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelExpression.Target" /> property of the result.</param>
        /// <param name="defaultValue">The <see cref="System.Linq.Expressions.LabelExpression.DefaultValue" /> property of the result</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public LabelExpression Update(LabelTarget target, Expression? defaultValue)
        {
            if (target == Target && defaultValue == DefaultValue)
            {
                return this;
            }
            return Expression.Label(target, defaultValue);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.LabelExpression" /> representing a label without a default value.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> which this <see cref="System.Linq.Expressions.LabelExpression" /> will be associated with.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LabelExpression" /> without a default value.</returns>
        public static LabelExpression Label(LabelTarget target)
        {
            return Label(target, defaultValue: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LabelExpression" /> representing a label with the given default value.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> which this <see cref="System.Linq.Expressions.LabelExpression" /> will be associated with.</param>
        /// <param name="defaultValue">The value of this <see cref="System.Linq.Expressions.LabelExpression" /> when the label is reached through regular control flow.</param>
        /// <returns>A <see cref="System.Linq.Expressions.LabelExpression" /> with the given default value.</returns>
        public static LabelExpression Label(LabelTarget target, Expression? defaultValue)
        {
            ValidateGoto(target, ref defaultValue, nameof(target), nameof(defaultValue), type: null);
            return new LabelExpression(target, defaultValue);
        }
    }
}
