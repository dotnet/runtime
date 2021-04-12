// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;

namespace System.Linq.Expressions
{
    /// <summary>Represents an expression that has a constant value.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Constant" /> factory methods to create a <see cref="System.Linq.Expressions.ConstantExpression" />.
    /// The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> of a <see cref="System.Linq.Expressions.ConstantExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.Constant" />.</remarks>
    /// <example>The following code example shows how to create an expression that represents a constant value by using the <see cref="O:System.Linq.Expressions.Expression.Constant" /> method.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet4":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet4":::</example>
    [DebuggerTypeProxy(typeof(ConstantExpressionProxy))]
    public class ConstantExpression : Expression
    {
        internal ConstantExpression(object? value)
        {
            Value = value;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ConstantExpression.Type" /> that represents the static type of the expression.</value>
        public override Type Type
        {
            get
            {
                if (Value == null)
                {
                    return typeof(object);
                }

                return Value.GetType();
            }
        }

        /// <summary>Returns the node type of this Expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Constant;

        /// <summary>Gets the value of the constant expression.</summary>
        /// <value>An <see cref="object" /> equal to the value of the represented expression.</value>
        public object? Value { get; }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitConstant(this);
        }
    }

    internal sealed class TypedConstantExpression : ConstantExpression
    {
        internal TypedConstantExpression(object? value, Type type)
            : base(value)
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
        /// <summary>Creates a <see cref="System.Linq.Expressions.ConstantExpression" /> that has the <see cref="System.Linq.Expressions.ConstantExpression.Value" /> property set to the specified value.</summary>
        /// <param name="value">An <see cref="object" /> to set the <see cref="System.Linq.Expressions.ConstantExpression.Value" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ConstantExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Constant" /> and the <see cref="System.Linq.Expressions.ConstantExpression.Value" /> property set to the specified value.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.ConstantExpression" /> is equal to the type of <paramref name="value" />. If <paramref name="value" /> is <see langword="null" />, <see cref="O:System.Linq.Expressions.Expression.Type" /> is equal to <see cref="object" />.
        /// To represent <see langword="null" />, you can also use the <see cref="System.Linq.Expressions.Expression.Constant(object,System.Type)" /> method, with which you can explicitly specify the type.</remarks>
        /// <example>The following code example shows how to create an expression that represents a constant value.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet4":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet4":::</example>
        public static ConstantExpression Constant(object? value)
        {
            return new ConstantExpression(value);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.ConstantExpression" /> that has the <see cref="System.Linq.Expressions.ConstantExpression.Value" /> and <see cref="System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</summary>
        /// <param name="value">An <see cref="object" /> to set the <see cref="System.Linq.Expressions.ConstantExpression.Value" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Type" /> to set the <see cref="System.Linq.Expressions.Expression.Type" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.ConstantExpression" /> that has the <see cref="System.Linq.Expressions.Expression.NodeType" /> property equal to <see cref="System.Linq.Expressions.ExpressionType.Constant" /> and the <see cref="System.Linq.Expressions.ConstantExpression.Value" /> and <see cref="System.Linq.Expressions.Expression.Type" /> properties set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="type" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="value" /> is not <see langword="null" /> and <paramref name="type" /> is not assignable from the dynamic type of <paramref name="value" />.</exception>
        /// <remarks>This method can be useful for representing values of nullable types.</remarks>
        /// <example>The following code example shows how to create an expression that represents a constant of the nullable type and set its value to <see langword="null" />.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet22":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet22":::</example>
        public static ConstantExpression Constant(object? value, Type type)
        {
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type));
            if (value == null)
            {
                if (type == typeof(object))
                {
                    return new ConstantExpression(null);
                }

                if (!type.IsValueType || type.IsNullableType())
                {
                    return new TypedConstantExpression(null, type);
                }
            }
            else
            {
                Type valueType = value.GetType();
                if (type == valueType)
                {
                    return new ConstantExpression(value);
                }

                if (type.IsAssignableFrom(valueType))
                {
                    return new TypedConstantExpression(value, type);
                }
            }

            throw Error.ArgumentTypesMustMatch();
        }
    }
}
