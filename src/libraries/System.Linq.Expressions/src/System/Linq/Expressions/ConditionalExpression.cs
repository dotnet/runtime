// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Dynamic.Utils;
using System.Diagnostics;

using AstUtils = System.Linq.Expressions.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents an expression that has a conditional operator.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Condition" /> factory method to create a <see cref="System.Linq.Expressions.ConditionalExpression" />.
    /// The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> of a <see cref="System.Linq.Expressions.ConditionalExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.Conditional" />.</remarks>
    /// <example>The following code example shows how to create an expression that represents a conditional statement. If the first argument evaluates to <see langword="true" />, the second argument is executed; otherwise, the third argument is executed.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet3":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet3":::</example>
    [DebuggerTypeProxy(typeof(ConditionalExpressionProxy))]
    public class ConditionalExpression : Expression
    {
        internal ConditionalExpression(Expression test, Expression ifTrue)
        {
            Test = test;
            IfTrue = ifTrue;
        }

        internal static ConditionalExpression Make(Expression test, Expression ifTrue, Expression ifFalse, Type type)
        {
            if (ifTrue.Type != type || ifFalse.Type != type)
            {
                return new FullConditionalExpressionWithType(test, ifTrue, ifFalse, type);
            }
            if (ifFalse is DefaultExpression && ifFalse.Type == typeof(void))
            {
                return new ConditionalExpression(test, ifTrue);
            }
            else
            {
                return new FullConditionalExpression(test, ifTrue, ifFalse);
            }
        }

        /// <summary>Returns the node type of this expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Conditional;

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ConditionalExpression.Type" /> that represents the static type of the expression.</value>
        public override Type Type => IfTrue.Type;

        /// <summary>Gets the test of the conditional operation.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the test of the conditional operation.</value>
        public Expression Test { get; }

        /// <summary>Gets the expression to execute if the test evaluates to <see langword="true" />.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the expression to execute if the test is <see langword="true" />.</value>
        public Expression IfTrue { get; }

        /// <summary>Gets the expression to execute if the test evaluates to <see langword="false" />.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the expression to execute if the test is <see langword="false" />.</value>
        public Expression IfFalse => GetFalse();

        internal virtual Expression GetFalse()
        {
            // Using a singleton here to ensure a stable object identity for IfFalse, which Update relies on.
            return AstUtils.Empty;
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitConditional(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="test">The <see cref="System.Linq.Expressions.ConditionalExpression.Test" /> property of the result.</param>
        /// <param name="ifTrue">The <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" /> property of the result.</param>
        /// <param name="ifFalse">The <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public ConditionalExpression Update(Expression test, Expression ifTrue, Expression ifFalse)
        {
            if (test == Test && ifTrue == IfTrue && ifFalse == IfFalse)
            {
                return this;
            }
            return Expression.Condition(test, ifTrue, ifFalse, Type);
        }
    }

    internal class FullConditionalExpression : ConditionalExpression
    {
        private readonly Expression _false;

        internal FullConditionalExpression(Expression test, Expression ifTrue, Expression ifFalse)
            : base(test, ifTrue)
        {
            _false = ifFalse;
        }

        internal override Expression GetFalse() => _false;
    }

    internal sealed class FullConditionalExpressionWithType : FullConditionalExpression
    {
        internal FullConditionalExpressionWithType(Expression test, Expression ifTrue, Expression ifFalse, Type type)
            : base(test, ifTrue, ifFalse)
        {
            Type = type;
        }

        public sealed override Type Type { get; }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.ConditionalExpression" /> that represents a conditional statement.</summary>
        /// <param name="test">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.</param>
        /// <param name="ifTrue">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.</param>
        /// <param name="ifFalse">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ConditionalExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Conditional" /> and the <see cref="System.Linq.Expressions.ConditionalExpression.Test" />, <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" />, and <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="test" /> or <paramref name="ifTrue" /> or <paramref name="ifFalse" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="test" />.Type is not <see cref="bool" />.
        /// -or-
        /// <paramref name="ifTrue" />.Type is not equal to <paramref name="ifFalse" />.Type.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ConditionalExpression" /> is equal to the <see cref="O:System.Linq.Expressions.Expression.Type" /> property of <paramref name="ifTrue" />.</remarks>
        /// <example>The following code example shows how to create an expression that represents a conditional statement. If the first argument evaluates to <see langword="true" />, the second argument is executed; otherwise, the third argument is executed.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet3":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet3":::</example>
        /// <altmember cref="System.Linq.Expressions.Expression.IfThen(System.Linq.Expressions.Expression,System.Linq.Expressions.Expression)"/>
        /// <altmember cref="System.Linq.Expressions.Expression.IfThenElse(System.Linq.Expressions.Expression,System.Linq.Expressions.Expression,System.Linq.Expressions.Expression)"/>
        public static ConditionalExpression Condition(Expression test, Expression ifTrue, Expression ifFalse)
        {
            ExpressionUtils.RequiresCanRead(test, nameof(test));
            ExpressionUtils.RequiresCanRead(ifTrue, nameof(ifTrue));
            ExpressionUtils.RequiresCanRead(ifFalse, nameof(ifFalse));

            if (test.Type != typeof(bool))
            {
                throw Error.ArgumentMustBeBoolean(nameof(test));
            }
            if (!TypeUtils.AreEquivalent(ifTrue.Type, ifFalse.Type))
            {
                throw Error.ArgumentTypesMustMatch();
            }

            return ConditionalExpression.Make(test, ifTrue, ifFalse, ifTrue.Type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ConditionalExpression" /> that represents a conditional statement.</summary>
        /// <param name="test">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.</param>
        /// <param name="ifTrue">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.</param>
        /// <param name="ifFalse">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Linq.Expressions.Expression.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ConditionalExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Conditional" /> and the <see cref="System.Linq.Expressions.ConditionalExpression.Test" />, <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" />, and <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values.</returns>
        /// <remarks>This method allows explicitly unifying the result type of the conditional expression in cases where the types of <paramref name="ifTrue" /> and <paramref name="ifFalse" /> expressions are not equal. Types of both <paramref name="ifTrue" /> and <paramref name="ifFalse" /> must be implicitly reference assignable to the result type. The <paramref name="type" /> is allowed to be <see cref="void" />.</remarks>
        public static ConditionalExpression Condition(Expression test, Expression ifTrue, Expression ifFalse, Type type)
        {
            ExpressionUtils.RequiresCanRead(test, nameof(test));
            ExpressionUtils.RequiresCanRead(ifTrue, nameof(ifTrue));
            ExpressionUtils.RequiresCanRead(ifFalse, nameof(ifFalse));
            ContractUtils.RequiresNotNull(type, nameof(type));

            if (test.Type != typeof(bool))
            {
                throw Error.ArgumentMustBeBoolean(nameof(test));
            }

            if (type != typeof(void))
            {
                if (!TypeUtils.AreReferenceAssignable(type, ifTrue.Type) ||
                    !TypeUtils.AreReferenceAssignable(type, ifFalse.Type))
                {
                    throw Error.ArgumentTypesMustMatch();
                }
            }

            return ConditionalExpression.Make(test, ifTrue, ifFalse, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ConditionalExpression" /> that represents a conditional block with an <see langword="if" /> statement.</summary>
        /// <param name="test">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.</param>
        /// <param name="ifTrue">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ConditionalExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Conditional" /> and the <see cref="System.Linq.Expressions.ConditionalExpression.Test" />, <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" />, properties set to the specified values. The <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> property is set to default expression and the type of the resulting <see cref="System.Linq.Expressions.ConditionalExpression" /> returned by this method is <see cref="void" />.</returns>
        /// <remarks></remarks>
        /// <example>The following code example shows how to create an expression that represents a conditional block.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet32":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet32":::</example>
        public static ConditionalExpression IfThen(Expression test, Expression ifTrue)
        {
            return Condition(test, ifTrue, Expression.Empty(), typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ConditionalExpression" /> that represents a conditional block with <see langword="if" /> and <see langword="else" /> statements.</summary>
        /// <param name="test">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.Test" /> property equal to.</param>
        /// <param name="ifTrue">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" /> property equal to.</param>
        /// <param name="ifFalse">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ConditionalExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Conditional" /> and the <see cref="System.Linq.Expressions.ConditionalExpression.Test" />, <see cref="System.Linq.Expressions.ConditionalExpression.IfTrue" />, and <see cref="System.Linq.Expressions.ConditionalExpression.IfFalse" /> properties set to the specified values. The type of the resulting <see cref="System.Linq.Expressions.ConditionalExpression" /> returned by this method is <see cref="void" />.</returns>
        /// <remarks></remarks>
        /// <example>The following code example shows how to create an expression that represents a conditional block.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet33":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet33":::</example>
        public static ConditionalExpression IfThenElse(Expression test, Expression ifTrue, Expression ifFalse)
        {
            return Condition(test, ifTrue, ifFalse, typeof(void));
        }
    }
}
