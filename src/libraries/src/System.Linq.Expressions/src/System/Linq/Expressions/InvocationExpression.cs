// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>
    /// Represents an expression that applies a delegate or lambda expression to a list of argument expressions.
    /// </summary>
    [DebuggerTypeProxy(typeof(Expression.InvocationExpressionProxy))]
    public sealed class InvocationExpression : Expression, IArgumentProvider
    {
        private IList<Expression> _arguments;
        private readonly Expression _lambda;
        private readonly Type _returnType;

        internal InvocationExpression(Expression lambda, IList<Expression> arguments, Type returnType)
        {
            _lambda = lambda;
            _arguments = arguments;
            _returnType = returnType;
        }

        /// <summary>
        /// Gets the static type of the expression that this <see cref="Expression" /> represents.
        /// </summary>
        /// <returns>The <see cref="Type"/> that represents the static type of the expression.</returns>
        public sealed override Type Type
        {
            get { return _returnType; }
        }

        /// <summary>
        /// Returns the node type of this Expression. Extension nodes should return
        /// ExpressionType.Extension when overriding this method.
        /// </summary>
        /// <returns>The <see cref="ExpressionType"/> of the expression.</returns>
        public sealed override ExpressionType NodeType
        {
            get { return ExpressionType.Invoke; }
        }

        /// <summary>
        /// Gets the delegate or lambda expression to be applied.
        /// </summary>
        public Expression Expression
        {
            get { return _lambda; }
        }

        /// <summary>
        /// Gets the arguments that the delegate or lambda expression is applied to.
        /// </summary>
        public ReadOnlyCollection<Expression> Arguments
        {
            get { return ReturnReadOnly(ref _arguments); }
        }

        /// <summary>
        /// Creates a new expression that is like this one, but using the
        /// supplied children. If all of the children are the same, it will
        /// return this expression.
        /// </summary>
        /// <param name="expression">The <see cref="Expression" /> property of the result.</param>
        /// <param name="arguments">The <see cref="Arguments" /> property of the result.</param>
        /// <returns>This expression if no children changed, or an expression with the updated children.</returns>
        public InvocationExpression Update(Expression expression, IEnumerable<Expression> arguments)
        {
            if (expression == Expression && arguments == Arguments)
            {
                return this;
            }

            return Expression.Invoke(expression, arguments);
        }

        public Expression GetArgument(int index)
        {
            return _arguments[index];
        }

        public int ArgumentCount
        {
            get
            {
                return _arguments.Count;
            }
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitInvocation(this);
        }

        internal InvocationExpression Rewrite(Expression lambda, Expression[] arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == _arguments.Count);

            return Expression.Invoke(lambda, arguments ?? _arguments);
        }

        internal LambdaExpression LambdaOperand
        {
            get
            {
                return (_lambda.NodeType == ExpressionType.Quote)
                    ? (LambdaExpression)((UnaryExpression)_lambda).Operand
                    : (_lambda as LambdaExpression);
            }
        }
    }

    public partial class Expression
    {
        ///<summary>
        ///Creates an <see cref="T:System.Linq.Expressions.InvocationExpression" /> that 
        ///applies a delegate or lambda expression to a list of argument expressions.
        ///</summary>
        ///<returns>
        ///An <see cref="T:System.Linq.Expressions.InvocationExpression" /> that 
        ///applies the specified delegate or lambda expression to the provided arguments.
        ///</returns>
        ///<param name="expression">
        ///An <see cref="T:System.Linq.Expressions.Expression" /> that represents the delegate
        ///or lambda expression to be applied.
        ///</param>
        ///<param name="arguments">
        ///An array of <see cref="T:System.Linq.Expressions.Expression" /> objects
        ///that represent the arguments that the delegate or lambda expression is applied to.
        ///</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="expression" /> is null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="expression" />.Type does not represent a delegate type or an <see cref="T:System.Linq.Expressions.Expression`1" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression" />.</exception>
        ///<exception cref="T:System.InvalidOperationException">
        ///<paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the delegate represented by <paramref name="expression" />.</exception>
        public static InvocationExpression Invoke(Expression expression, params Expression[] arguments)
        {
            return Invoke(expression, (IEnumerable<Expression>)arguments);
        }

        ///<summary>
        ///Creates an <see cref="T:System.Linq.Expressions.InvocationExpression" /> that 
        ///applies a delegate or lambda expression to a list of argument expressions.
        ///</summary>
        ///<returns>
        ///An <see cref="T:System.Linq.Expressions.InvocationExpression" /> that 
        ///applies the specified delegate or lambda expression to the provided arguments.
        ///</returns>
        ///<param name="expression">
        ///An <see cref="T:System.Linq.Expressions.Expression" /> that represents the delegate
        ///or lambda expression to be applied.
        ///</param>
        ///<param name="arguments">
        ///An <see cref="T:System.Collections.Generic.IEnumerable`1" /> of <see cref="T:System.Linq.Expressions.Expression" /> objects
        ///that represent the arguments that the delegate or lambda expression is applied to.
        ///</param>
        ///<exception cref="T:System.ArgumentNullException">
        ///<paramref name="expression" /> is null.</exception>
        ///<exception cref="T:System.ArgumentException">
        ///<paramref name="expression" />.Type does not represent a delegate type or an <see cref="T:System.Linq.Expressions.Expression`1" />.-or-The <see cref="P:System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression" />.</exception>
        ///<exception cref="T:System.InvalidOperationException">
        ///<paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the delegate represented by <paramref name="expression" />.</exception>
        public static InvocationExpression Invoke(Expression expression, IEnumerable<Expression> arguments)
        {
            RequiresCanRead(expression, "expression");

            var args = arguments.ToReadOnly();
            var mi = GetInvokeMethod(expression);
            ValidateArgumentTypes(mi, ExpressionType.Invoke, ref args);
            return new InvocationExpression(expression, args, mi.ReturnType);
        }

        /// <summary>
        /// Gets the delegate's Invoke method; used by InvocationExpression.
        /// </summary>
        /// <param name="expression">The expression to be invoked.</param>
        internal static MethodInfo GetInvokeMethod(Expression expression)
        {
            Type delegateType = expression.Type;
            if (!expression.Type.IsSubclassOf(typeof(MulticastDelegate)))
            {
                Type exprType = TypeUtils.FindGenericType(typeof(Expression<>), expression.Type);
                if (exprType == null)
                {
                    throw Error.ExpressionTypeNotInvocable(expression.Type);
                }
                delegateType = exprType.GetGenericArguments()[0];
            }

            return delegateType.GetMethod("Invoke");
        }
    }
}
