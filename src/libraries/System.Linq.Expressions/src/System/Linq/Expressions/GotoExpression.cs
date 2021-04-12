// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Specifies what kind of jump this <see cref="System.Linq.Expressions.GotoExpression" /> represents.</summary>
    public enum GotoExpressionKind
    {
        /// <summary>A <see cref="System.Linq.Expressions.GotoExpression" /> that represents a jump to some location.</summary>
        Goto,
        /// <summary>A <see cref="System.Linq.Expressions.GotoExpression" /> that represents a return statement.</summary>
        Return,
        /// <summary>A <see cref="System.Linq.Expressions.GotoExpression" /> that represents a break statement.</summary>
        Break,
        /// <summary>A <see cref="System.Linq.Expressions.GotoExpression" /> that represents a continue statement.</summary>
        Continue,
    }

    /// <summary>Represents an unconditional jump. This includes return statements, break and continue statements, and other jumps.</summary>
    /// <remarks></remarks>
    /// <example>The following example demonstrates how to create an expression that contains a <see cref="System.Linq.Expressions.GotoExpression" /> object by using the <see cref="O:System.Linq.Expressions.Expression.Goto" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet45":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet45":::</example>
    [DebuggerTypeProxy(typeof(GotoExpressionProxy))]
    public sealed class GotoExpression : Expression
    {
        internal GotoExpression(GotoExpressionKind kind, LabelTarget target, Expression? value, Type type)
        {
            Kind = kind;
            Value = value;
            Target = target;
            Type = type;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.GotoExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type { get; }

        /// <summary>Returns the node type of this <see cref="System.Linq.Expressions.Expression" />.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> that represents this expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Goto;

        /// <summary>The value passed to the target, or null if the target is of type System.Void.</summary>
        /// <value>The <see cref="System.Linq.Expressions.Expression" /> object representing the value passed to the target or null.</value>
        public Expression? Value { get; }

        /// <summary>The target label where this node jumps to.</summary>
        /// <value>The <see cref="System.Linq.Expressions.LabelTarget" /> object representing the target label for this node.</value>
        public LabelTarget Target { get; }

        /// <summary>The kind of the "go to" expression. Serves information purposes only.</summary>
        /// <value>The <see cref="System.Linq.Expressions.GotoExpressionKind" /> object representing the kind of the "go to" expression.</value>
        public GotoExpressionKind Kind { get; }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitGoto(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.GotoExpression.Target" /> property of the result.</param>
        /// <param name="value">The <see cref="System.Linq.Expressions.GotoExpression.Value" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public GotoExpression Update(LabelTarget target, Expression? value)
        {
            if (target == Target && value == Value)
            {
                return this;
            }
            return Expression.MakeGoto(Kind, target, value, Type);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a break statement.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and a null value to be passed to the target label upon jumping.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create an expression that contains a <see cref="System.Linq.Expressions.LoopExpression" /> object that uses the <see cref="O:System.Linq.Expressions.Expression.Break" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet44":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet44":::</example>
        public static GotoExpression Break(LabelTarget target)
        {
            return MakeGoto(GotoExpressionKind.Break, target, null, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a break statement. The value passed to the label upon jumping can be specified.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        public static GotoExpression Break(LabelTarget target, Expression? value)
        {
            return MakeGoto(GotoExpressionKind.Break, target, value, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a break statement with the specified type.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />.</returns>
        public static GotoExpression Break(LabelTarget target, Type type)
        {
            return MakeGoto(GotoExpressionKind.Break, target, null, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a break statement with the specified type. The value passed to the label upon jumping can be specified.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Break, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        public static GotoExpression Break(LabelTarget target, Expression? value, Type type)
        {
            return MakeGoto(GotoExpressionKind.Break, target, value, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a continue statement.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and a null value to be passed to the target label upon jumping.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create a loop expression that uses the <see cref="O:System.Linq.Expressions.Expression.Continue" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet46":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet46":::</example>
        public static GotoExpression Continue(LabelTarget target)
        {
            return MakeGoto(GotoExpressionKind.Continue, target, null, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a continue statement with the specified type.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and a null value to be passed to the target label upon jumping.</returns>
        public static GotoExpression Continue(LabelTarget target, Type type)
        {
            return MakeGoto(GotoExpressionKind.Continue, target, null, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a return statement.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Return, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and a null value to be passed to the target label upon jumping.</returns>
        public static GotoExpression Return(LabelTarget target)
        {
            return MakeGoto(GotoExpressionKind.Return, target, null, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a return statement with the specified type.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Return, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and a null value to be passed to the target label upon jumping.</returns>
        public static GotoExpression Return(LabelTarget target, Type type)
        {
            return MakeGoto(GotoExpressionKind.Return, target, null, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a return statement. The value passed to the label upon jumping can be specified.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create an expression that contains the <see cref="O:System.Linq.Expressions.Expression.Return" /> method.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet43":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet43":::</example>
        public static GotoExpression Return(LabelTarget target, Expression? value)
        {
            return MakeGoto(GotoExpressionKind.Return, target, value, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a return statement with the specified type. The value passed to the label upon jumping can be specified.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Continue, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        public static GotoExpression Return(LabelTarget target, Expression? value, Type type)
        {
            return MakeGoto(GotoExpressionKind.Return, target, value, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a "go to" statement.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to the specified value, and a null value to be passed to the target label upon jumping.</returns>
        /// <remarks></remarks>
        /// <example>The following example demonstrates how to create an expression that contains a <see cref="System.Linq.Expressions.GotoExpression" /> object.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet45":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet45":::</example>
        public static GotoExpression Goto(LabelTarget target)
        {
            return MakeGoto(GotoExpressionKind.Goto, target, null, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a "go to" statement with the specified type.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to the specified value, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and a null value to be passed to the target label upon jumping.</returns>
        public static GotoExpression Goto(LabelTarget target, Type type)
        {
            return MakeGoto(GotoExpressionKind.Goto, target, null, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a "go to" statement. The value passed to the label upon jumping can be specified.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        public static GotoExpression Goto(LabelTarget target, Expression? value)
        {
            return MakeGoto(GotoExpressionKind.Goto, target, value, typeof(void));
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a "go to" statement with the specified type. The value passed to the label upon jumping can be specified.</summary>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to Goto, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        public static GotoExpression Goto(LabelTarget target, Expression? value, Type type)
        {
            return MakeGoto(GotoExpressionKind.Goto, target, value, type);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.GotoExpression" /> representing a jump of the specified <see cref="System.Linq.Expressions.GotoExpressionKind" />. The value passed to the label upon jumping can also be specified.</summary>
        /// <param name="kind">The <see cref="System.Linq.Expressions.GotoExpressionKind" /> of the <see cref="System.Linq.Expressions.GotoExpression" />.</param>
        /// <param name="target">The <see cref="System.Linq.Expressions.LabelTarget" /> that the <see cref="System.Linq.Expressions.GotoExpression" /> will jump to.</param>
        /// <param name="value">The value that will be passed to the associated label upon jumping.</param>
        /// <param name="type">An <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.GotoExpression" /> with <see cref="System.Linq.Expressions.GotoExpression.Kind" /> equal to <paramref name="kind" />, the <see cref="System.Linq.Expressions.GotoExpression.Target" /> property set to <paramref name="target" />, the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <paramref name="type" />, and <paramref name="value" /> to be passed to the target label upon jumping.</returns>
        public static GotoExpression MakeGoto(GotoExpressionKind kind, LabelTarget target, Expression? value, Type type)
        {
            ValidateGoto(target, ref value, nameof(target), nameof(value), type);
            return new GotoExpression(kind, target, value, type);
        }

        private static void ValidateGoto(LabelTarget target, ref Expression? value, string targetParameter, string valueParameter, Type? type)
        {
            ContractUtils.RequiresNotNull(target, targetParameter);
            if (value == null)
            {
                if (target.Type != typeof(void)) throw Error.LabelMustBeVoidOrHaveExpression(nameof(target));

                if (type != null)
                {
                    TypeUtils.ValidateType(type, nameof(type));
                }
            }
            else
            {
                ValidateGotoType(target.Type, ref value, valueParameter);
            }
        }

        // Standard argument validation, taken from ValidateArgumentTypes
        private static void ValidateGotoType(Type expectedType, ref Expression value, string paramName)
        {
            ExpressionUtils.RequiresCanRead(value, paramName);
            if (expectedType != typeof(void))
            {
                if (!TypeUtils.AreReferenceAssignable(expectedType, value.Type))
                {
                    // C# auto-quotes return values, so we'll do that here
                    if (!TryQuote(expectedType, ref value))
                    {
                        throw Error.ExpressionTypeDoesNotMatchLabel(value.Type, expectedType);
                    }
                }
            }
        }
    }
}
