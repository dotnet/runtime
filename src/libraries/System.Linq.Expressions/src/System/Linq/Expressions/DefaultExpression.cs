// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents the default value of a type or an empty expression.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create an expression that represents a default value for a given type by using the <see cref="O:System.Linq.Expressions.Expression.Default" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet6":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet6":::</example>
    [DebuggerTypeProxy(typeof(DefaultExpressionProxy))]
    public sealed class DefaultExpression : Expression
    {
        internal DefaultExpression(Type type)
        {
            Type = type;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.DefaultExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type { get; }

        /// <summary>Returns the node type of this expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Default;

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitDefault(this);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates an empty expression that has <see cref="void" /> type.</summary>
        /// <returns>A <see cref="System.Linq.Expressions.DefaultExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Default" /> and the <see cref="System.Linq.Expressions.Expression.Type" /> property set to <see cref="void" />.</returns>
        /// <remarks>An empty expression can be used where an expression is expected but no action is desired. For example, you can use an empty expression as the last expression in a block expression. In this case, the block expression's return value is void.</remarks>
        /// <example>The following code example shows how to create an empty expression and add it to a block expression.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet31":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet31":::</example>
        public static DefaultExpression Empty()
        {
            return new DefaultExpression(typeof(void)); // Create new object each time for different identity
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DefaultExpression" /> that has the <see cref="System.Linq.Expressions.Expression.Type" /> property set to the specified type.</summary>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DefaultExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Default" /> and the <see cref="System.Linq.Expressions.Expression.Type" /> property set to the specified type.</returns>
        /// <remarks></remarks>
        /// <example>The following code example shows how to create an expression that represents a default value for a given type.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet6":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet6":::</example>
        public static DefaultExpression Default(Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            return new DefaultExpression(type);
        }
    }
}
