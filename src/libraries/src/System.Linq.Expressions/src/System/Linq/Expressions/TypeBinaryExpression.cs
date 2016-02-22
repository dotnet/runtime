// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents an operation between an expression and a type. 
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.TypeBinaryExpressionProxy))]
    public sealed class TypeBinaryExpression : Expression
    {
        private readonly Expression _expression;
        private readonly Type _typeOperand;
        private readonly ExpressionType _nodeKind;

        internal TypeBinaryExpression(Expression expression, Type typeOperand, ExpressionType nodeKind)
        {
            _expression = expression;
            _typeOperand = typeOperand;
            _nodeKind = nodeKind;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents.
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type
        {
            get { return typeof(bool); }
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return _nodeKind; }
        }

        /// <summary>
        /// Gets the expression operand of a type test operation.
        /// </summary>
        public Expression Expression
        {
            get { return _expression; }
        }

        /// <summary>
        /// Gets the type operand of a type test operation.
        /// </summary>
        public Type TypeOperand
        {
            get { return _typeOperand; }
        }

        #region Reduce TypeEqual

        internal Expression ReduceTypeEqual()
        {
            Type cType = Expression.Type;

            if (cType.GetTypeInfo().IsValueType)
            {
                if (cType.IsNullableType())
                {
                    // If the expression type is a a nullable type, it will match if
                    // the value is not null and the type operand
                    // either matches or is its type argument (T to its T?).
                    if (cType.GetNonNullableType() != _typeOperand.GetNonNullableType())
                    {
                        return Expression.Block(Expression, Expression.Constant(false));
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
                    return Expression.Block(Expression, Expression.Constant(cType == _typeOperand.GetNonNullableType()));
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
                new[] { parameter },
                Expression.Assign(parameter, Expression),
                ByValParameterTypeEqual(parameter)
            );
        }

        // Helper that is used when re-eval of LHS is safe.
        private Expression ByValParameterTypeEqual(ParameterExpression value)
        {
            Expression getType = Expression.Call(value, typeof(object).GetMethod("GetType"));

            // In remoting scenarios, obj.GetType() can return an interface.
            // But JIT32's optimized "obj.GetType() == typeof(ISomething)" codegen,
            // causing it to always return false.
            // We workaround this optimization by generating different, less optimal IL
            // if TypeOperand is an interface.
            if (_typeOperand.GetTypeInfo().IsInterface)
            {
                var temp = Expression.Parameter(typeof(Type));
                getType = Expression.Block(new[] { temp }, Expression.Assign(temp, getType), temp);
            }

            // We use reference equality when comparing to null for correctness
            // (don't invoke a user defined operator), and reference equality
            // on types for performance (so the JIT can optimize the IL).
            return Expression.AndAlso(
                Expression.ReferenceNotEqual(value, Expression.Constant(null)),
                Expression.ReferenceEqual(
                    getType,
                    Expression.Constant(_typeOperand.GetNonNullableType(), typeof(Type))
                )
            );
        }

        private Expression ReduceConstantTypeEqual()
        {
            ConstantExpression ce = Expression as ConstantExpression;
            //TypeEqual(null, T) always returns false.
            if (ce.Value == null)
            {
                return Expression.Constant(false);
            }
            else
            {
                return Expression.Constant(_typeOperand.GetNonNullableType() == ce.Value.GetType());
            }
        }

        #endregion

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitTypeBinary(this);
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="expression">The <see cref="Expression" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
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

    public partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="TypeBinaryExpression"/>.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="Expression"/> property equal to.</param>
        /// <param name="type">A <see cref="Type"/> to set the <see cref="TypeBinaryExpression.TypeOperand"/> property equal to.</param>
        /// <returns>A <see cref="TypeBinaryExpression"/> for which the <see cref="NodeType"/> property is equal to <see cref="TypeIs"/> and for which the <see cref="Expression"/> and <see cref="TypeBinaryExpression.TypeOperand"/> properties are set to the specified values.</returns>
        public static TypeBinaryExpression TypeIs(Expression expression, Type type)
        {
            RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type.IsByRef) throw Error.TypeMustNotBeByRef();

            return new TypeBinaryExpression(expression, type, ExpressionType.TypeIs);
        }

        /// <summary>
        /// Creates a <see cref="TypeBinaryExpression"/> that compares run-time type identity.
        /// </summary>
        /// <param name="expression">An <see cref="Expression"/> to set the <see cref="Expression"/> property equal to.</param>
        /// <param name="type">A <see cref="Type"/> to set the <see cref="TypeBinaryExpression.TypeOperand"/> property equal to.</param>
        /// <returns>A <see cref="TypeBinaryExpression"/> for which the <see cref="NodeType"/> property is equal to <see cref="TypeEqual"/> and for which the <see cref="Expression"/> and <see cref="TypeBinaryExpression.TypeOperand"/> properties are set to the specified values.</returns>
        public static TypeBinaryExpression TypeEqual(Expression expression, Type type)
        {
            RequiresCanRead(expression, nameof(expression));
            ContractUtils.RequiresNotNull(type, nameof(type));
            if (type.IsByRef) throw Error.TypeMustNotBeByRef();

            return new TypeBinaryExpression(expression, type, ExpressionType.TypeEqual);
        }
    }
}
