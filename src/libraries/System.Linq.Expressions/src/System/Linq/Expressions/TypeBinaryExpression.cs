// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Dynamic.Utils;
using System.Runtime.CompilerServices;
using static System.Linq.Expressions.CachedReflectionInfo;

namespace System.Linq.Expressions
{
    /// <summary>Represents an operation between an expression and a type.</summary>
    /// <remarks>A type test is an example of an operation between an expression and a type.
    /// Use the <see cref="O:System.Linq.Expressions.Expression.TypeIs" /> factory method to create a <see cref="System.Linq.Expressions.TypeBinaryExpression" />.
    /// The value of the <see cref="O:System.Linq.Expressions.Expression.NodeType" /> property of a <see cref="System.Linq.Expressions.TypeBinaryExpression" /> object is <see cref="System.Linq.Expressions.ExpressionType.TypeIs" />.</remarks>
    /// <example>The following example creates a <see cref="System.Linq.Expressions.TypeBinaryExpression" /> object that represents a type test of a string value against the <see cref="int" /> type.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet12":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet12":::</example>
    [DebuggerTypeProxy(typeof(TypeBinaryExpressionProxy))]
    public sealed class TypeBinaryExpression : Expression
    {
        internal TypeBinaryExpression(Expression expression, Type typeOperand, ExpressionType nodeType)
        {
            Expression = expression;
            TypeOperand = typeOperand;
            NodeType = nodeType;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.TypeBinaryExpression.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.TypeBinaryExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type => typeof(bool);

        /// <summary>Returns the node type of this Expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType { get; }

        /// <summary>Gets the expression operand of a type test operation.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the expression operand of a type test operation.</value>
        public Expression Expression { get; }

        /// <summary>Gets the type operand of a type test operation.</summary>
        /// <value>A <see cref="System.Type" /> that represents the type operand of a type test operation.</value>
        public Type TypeOperand { get; }

        #region Reduce TypeEqual

        internal Expression ReduceTypeEqual()
        {
            Type cType = Expression.Type;

            if (cType.IsValueType || TypeOperand.IsPointer)
            {
                if (cType.IsNullableType())
                {
                    // If the expression type is a nullable type, it will match if
                    // the value is not null and the type operand
                    // either matches or is its type argument (T to its T?).
                    if (cType.GetNonNullableType() != TypeOperand.GetNonNullableType())
                    {
                        return Expression.Block(Expression, Utils.Constant(value: false));
                    }
                    else
                    {
                        return Expression.NotEqual(Expression, Expression.Constant(null, Expression.Type));
                    }
                }
                else
                {
                    // For other value types (including Void), we can
                    // determine the result now
                    return Expression.Block(Expression, Utils.Constant(cType == TypeOperand.GetNonNullableType()));
                }
            }

            Debug.Assert(TypeUtils.AreReferenceAssignable(typeof(object), Expression.Type), "Expecting reference types only after this point.");

            // Can check the value right now for constants.
            if (Expression.NodeType == ExpressionType.Constant)
            {
                return ReduceConstantTypeEqual();
            }

            // expression is a ByVal parameter. Can safely reevaluate.
            var parameter = Expression as ParameterExpression;
            if (parameter != null && !parameter.IsByRef)
            {
                return ByValParameterTypeEqual(parameter);
            }

            // Create a temp so we only evaluate the left side once
            parameter = Expression.Parameter(typeof(object));

            return Expression.Block(
                new TrueReadOnlyCollection<ParameterExpression>(parameter),
                new TrueReadOnlyCollection<Expression>(
                    Expression.Assign(parameter, Expression),
                    ByValParameterTypeEqual(parameter)
                )
            );
        }

        // Helper that is used when re-eval of LHS is safe.
        private Expression ByValParameterTypeEqual(ParameterExpression value)
        {
            Expression getType = Expression.Call(value, Object_GetType);

            // In remoting scenarios, obj.GetType() can return an interface.
            // But JIT32's optimized "obj.GetType() == typeof(ISomething)" codegen,
            // causing it to always return false.
            // We workaround this optimization by generating different, less optimal IL
            // if TypeOperand is an interface.
            if (TypeOperand.IsInterface)
            {
                ParameterExpression temp = Expression.Parameter(typeof(Type));
                getType = Expression.Block(
                    new TrueReadOnlyCollection<ParameterExpression>(temp),
                    new TrueReadOnlyCollection<Expression>(
                        Expression.Assign(temp, getType),
                        temp
                    )
                );
            }

            // We use reference equality when comparing to null for correctness
            // (don't invoke a user defined operator), and reference equality
            // on types for performance (so the JIT can optimize the IL).
            return Expression.AndAlso(
                Expression.ReferenceNotEqual(value, Utils.Null),
                Expression.ReferenceEqual(
                    getType,
                    Expression.Constant(TypeOperand.GetNonNullableType(), typeof(Type))
                )
            );
        }

        private Expression ReduceConstantTypeEqual()
        {
            ConstantExpression? ce = Expression as ConstantExpression;
            //TypeEqual(null, T) always returns false.
            if (ce!.Value == null)
            {
                return Utils.Constant(value: false);
            }
            else
            {
                return Utils.Constant(TypeOperand.GetNonNullableType() == ce.Value.GetType());
            }
        }

        #endregion
        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitTypeBinary(this);
        }

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="expression">The <see cref="System.Linq.Expressions.TypeBinaryExpression.Expression" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public TypeBinaryExpression Update(Expression expression)
        {
            if (expression == Expression)
            {
                return this;
            }
            if (NodeType == ExpressionType.TypeIs)
            {
                return Expression.TypeIs(expression, TypeOperand);
            }
            return Expression.TypeEqual(expression, TypeOperand);
        }
    }

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>Creates a <see cref="System.Linq.Expressions.TypeBinaryExpression" />.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.TypeBinaryExpression.Expression" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Linq.Expressions.Expression.Type" /> to set the <see cref="System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.TypeBinaryExpression" /> for which the <see cref="System.Linq.Expressions.Expression.NodeType" /> property is equal to <see cref="System.Linq.Expressions.ExpressionType.TypeIs" /> and for which the <see cref="System.Linq.Expressions.TypeBinaryExpression.Expression" /> and <see cref="System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> properties are set to the specified values.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> or <paramref name="type" /> is <see langword="null" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.UnaryExpression" /> represents <see cref="bool" />.</remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.TypeIs(System.Linq.Expressions.Expression,System.Type)" /> method to create a <see cref="System.Linq.Expressions.TypeBinaryExpression" /> that represents a type test of a string value against the <see cref="int" /> type.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet12":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet12":::</example>
        public static TypeBinaryExpression TypeIs(Expression expression, Type type)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type.IsByRef) throw Error.TypeMustNotBeByRef(nameof(type));

            return new TypeBinaryExpression(expression, type, ExpressionType.TypeIs);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.TypeBinaryExpression" /> that compares run-time type identity.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> to set the <see cref="System.Linq.Expressions.Expression" /> property equal to.</param>
        /// <param name="type">A <see cref="System.Linq.Expressions.Expression.Type" /> to set the <see cref="System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> property equal to.</param>
        /// <returns>A <see cref="System.Linq.Expressions.TypeBinaryExpression" /> for which the <see cref="System.Linq.Expressions.Expression.NodeType" /> property is equal to <see cref="System.Linq.Expressions.Expression.TypeEqual(System.Linq.Expressions.Expression,System.Type)" /> and for which the <see cref="System.Linq.Expressions.Expression" /> and <see cref="System.Linq.Expressions.TypeBinaryExpression.TypeOperand" /> properties are set to the specified values.</returns>
        public static TypeBinaryExpression TypeEqual(Expression expression, Type type)
        {
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type.IsByRef) throw Error.TypeMustNotBeByRef(nameof(type));

            return new TypeBinaryExpression(expression, type, ExpressionType.TypeEqual);
        }
    }
}
