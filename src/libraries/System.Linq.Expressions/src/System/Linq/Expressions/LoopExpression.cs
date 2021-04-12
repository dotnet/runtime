// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents an infinite loop. It can be exited with "break".</summary>
    /// <remarks></remarks>
    /// <example>The following example demonstrates how to create a block expression that contains a <see cref="System.Linq.Expressions.LoopExpression" /> object by using the <see cref="O:System.Linq.Expressions.Expression.Loop" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet44":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet44":::</example>
    [DebuggerTypeProxy(typeof(LoopExpressionProxy))]
    public sealed class LoopExpression : Expression
    {
        internal LoopExpression(Expression body, LabelTarget? @break, LabelTarget? @continue)
        {
            Body = body;
            BreakLabel = @break;
            ContinueLabel = @continue;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LoopExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => BreakLabel == null ? typeof(void) : BreakLabel.Type;

        /// <summary>Returns the node type of this expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Loop;

        /// <summary>Gets the <see cref="System.Linq.Expressions.Expression" /> that is the body of the loop.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> that is the body of the loop.</value>
        public Expression Body { get; }

        /// <summary>Gets the <see cref="System.Linq.Expressions.LabelTarget" /> that is used by the loop body as a break statement target.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LabelTarget" /> that is used by the loop body as a break statement target.</value>
        public LabelTarget? BreakLabel { get; }

        /// <summary>Gets the <see cref="System.Linq.Expressions.LabelTarget" /> that is used by the loop body as a continue statement target.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LabelTarget" /> that is used by the loop body as a continue statement target.</value>
        public LabelTarget? ContinueLabel { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitLoop(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="breakLabel">The <see cref="System.Linq.Expressions.LoopExpression.BreakLabel" /> property of the result.</param>
        /// <param name="continueLabel">The <see cref="System.Linq.Expressions.LoopExpression.ContinueLabel" /> property of the result.</param>
        /// <param name="body">The <see cref="System.Linq.Expressions.LoopExpression.Body" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public LoopExpression Update(LabelTarget? breakLabel, LabelTarget? continueLabel, Expression body)
        {
            if (breakLabel == BreakLabel && continueLabel == ContinueLabel && body == Body)
            {
                return this;
            }
            return Expression.Loop(body, breakLabel, continueLabel);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.LoopExpression" /> with the given body.</summary>
        /// <param name="body">The body of the loop.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.LoopExpression" />.</returns>
        public static LoopExpression Loop(Expression body)
        {
            return Loop(body, @break: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LoopExpression" /> with the given body and break target.</summary>
        /// <param name="body">The body of the loop.</param>
        /// <param name="break">The break target used by the loop body.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.LoopExpression" />.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create a block expression that contains a <see cref="System.Linq.Expressions.LoopExpression" /> object.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet44":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet44":::</example>
        public static LoopExpression Loop(Expression body, LabelTarget? @break)
        {
            return Loop(body, @break, @continue: null);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.LoopExpression" /> with the given body.</summary>
        /// <param name="body">The body of the loop.</param>
        /// <param name="break">The break target used by the loop body.</param>
        /// <param name="continue">The continue target used by the loop body.</param>
        /// <returns>The created <see cref="System.Linq.Expressions.LoopExpression" />.</returns>
        public static LoopExpression Loop(Expression body, LabelTarget? @break, LabelTarget? @continue)
        {
            ExpressionUtils.RequiresCanRead(body, nameof(body));
            if (@continue != null && @continue.Type != typeof(void)) throw Error.LabelTypeMustBeVoid(nameof(@continue));
            return new LoopExpression(body, @break, @continue);
        }
    }
}
