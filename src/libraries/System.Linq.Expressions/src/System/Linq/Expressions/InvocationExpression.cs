// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Reflection;

namespace System.Linq.Expressions
{
    /// <summary>Represents an expression that applies a delegate or lambda expression to a list of argument expressions.</summary>
    /// <remarks>Use the <see cref="O:System.Linq.Expressions.Expression.Invoke" /> factory methods to create an <see cref="System.Linq.Expressions.InvocationExpression" />.
    /// The <see cref="O:System.Linq.Expressions.Expression.NodeType" /> of an <see cref="System.Linq.Expressions.InvocationExpression" /> is <see cref="System.Linq.Expressions.ExpressionType.Invoke" />.</remarks>
    /// <example>The following example creates an <see cref="System.Linq.Expressions.InvocationExpression" /> that represents invoking a lambda expression with specified arguments.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet6":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet6":::</example>
    [DebuggerTypeProxy(typeof(InvocationExpressionProxy))]
    public class InvocationExpression : Expression, IArgumentProvider
    {
        internal InvocationExpression(Expression expression, Type returnType)
        {
            Expression = expression;
            Type = returnType;
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.InvocationExpression.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.InvocationExpression.Type" /> that represents the static type of the expression.</value>
        public sealed override Type Type { get; }

        /// <summary>Returns the node type of this expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Invoke;

        /// <summary>Gets the delegate or lambda expression to be applied.</summary>
        /// <value>An <see cref="System.Linq.Expressions.Expression" /> that represents the delegate to be applied.</value>
        public Expression Expression { get; }

        /// <summary>Gets the arguments that the delegate or lambda expression is applied to.</summary>
        /// <value>A <see cref="System.Collections.ObjectModel.ReadOnlyCollection{T}" /> of <see cref="System.Linq.Expressions.Expression" /> objects which represent the arguments that the delegate is applied to.</value>
        public ReadOnlyCollection<Expression> Arguments => GetOrMakeArguments();

        /// <summary>Creates a new expression that is like this one, but using the supplied children. If all of the children are the same, it will return this expression.</summary>
        /// <param name="expression">The <see cref="System.Linq.Expressions.InvocationExpression.Expression" /> property of the result.</param>
        /// <param name="arguments">The <see cref="System.Linq.Expressions.InvocationExpression.Arguments" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public InvocationExpression Update(Expression expression, IEnumerable<Expression>? arguments)
        {
            if (expression == Expression && arguments != null)
            {
                if (ExpressionUtils.SameElements(ref arguments, Arguments))
                {
                    return this;
                }
            }

            return Invoke(expression, arguments);
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>
        /// Gets the argument expression with the specified <paramref name="index"/>.
        /// </summary>
        /// <param name="index">The index of the argument expression to get.</param>
        /// <returns>The expression representing the argument at the specified <paramref name="index"/>.</returns>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual Expression GetArgument(int index)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>
        /// Gets the number of argument expressions of the node.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        public virtual int ArgumentCount
        {
            get
            {
                throw ContractUtils.Unreachable;
            }
        }

        /// <summary>Dispatches to the specific visit method for this node type.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            return visitor.VisitInvocation(this);
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            throw ContractUtils.Unreachable;
        }

        internal LambdaExpression? LambdaOperand
        {
            get
            {
                return (Expression.NodeType == ExpressionType.Quote)
                    ? (LambdaExpression)((UnaryExpression)Expression).Operand
                    : (Expression as LambdaExpression);
            }
        }
    }

    #region Specialized Subclasses

    internal sealed class InvocationExpressionN : InvocationExpression
    {
        private IReadOnlyList<Expression> _arguments;

        public InvocationExpressionN(Expression lambda, IReadOnlyList<Expression> arguments, Type returnType)
            : base(lambda, returnType)
        {
            _arguments = arguments;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(ref _arguments);
        }

        public override Expression GetArgument(int index) => _arguments[index];

        public override int ArgumentCount => _arguments.Count;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == _arguments.Count);

            return Expression.Invoke(lambda, arguments ?? _arguments);
        }
    }

    internal sealed class InvocationExpression0 : InvocationExpression
    {
        public InvocationExpression0(Expression lambda, Type returnType)
            : base(lambda, returnType)
        {
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return EmptyReadOnlyCollection<Expression>.Instance;
        }

        public override Expression GetArgument(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        public override int ArgumentCount => 0;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == 0);

            return Expression.Invoke(lambda);
        }
    }

    internal sealed class InvocationExpression1 : InvocationExpression
    {
        private object _arg0;       // storage for the 1st argument or a read-only collection.  See IArgumentProvider

        public InvocationExpression1(Expression lambda, Type returnType, Expression arg0)
            : base(lambda, returnType)
        {
            _arg0 = arg0;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        public override Expression GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override int ArgumentCount => 1;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == 1);

            if (arguments != null)
            {
                return Expression.Invoke(lambda, arguments[0]);
            }
            return Expression.Invoke(lambda, ExpressionUtils.ReturnObject<Expression>(_arg0));
        }
    }

    internal sealed class InvocationExpression2 : InvocationExpression
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1;  // storage for the 2nd argument

        public InvocationExpression2(Expression lambda, Type returnType, Expression arg0, Expression arg1)
            : base(lambda, returnType)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        public override Expression GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override int ArgumentCount => 2;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == 2);

            if (arguments != null)
            {
                return Expression.Invoke(lambda, arguments[0], arguments[1]);
            }
            return Expression.Invoke(lambda, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1);
        }
    }

    internal sealed class InvocationExpression3 : InvocationExpression
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1;  // storage for the 2nd argument
        private readonly Expression _arg2;  // storage for the 3rd argument

        public InvocationExpression3(Expression lambda, Type returnType, Expression arg0, Expression arg1, Expression arg2)
            : base(lambda, returnType)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        public override Expression GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override int ArgumentCount => 3;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == 3);

            if (arguments != null)
            {
                return Expression.Invoke(lambda, arguments[0], arguments[1], arguments[2]);
            }
            return Expression.Invoke(lambda, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2);
        }
    }

    internal sealed class InvocationExpression4 : InvocationExpression
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1;  // storage for the 2nd argument
        private readonly Expression _arg2;  // storage for the 3rd argument
        private readonly Expression _arg3;  // storage for the 4th argument

        public InvocationExpression4(Expression lambda, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
            : base(lambda, returnType)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        public override Expression GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                3 => _arg3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override int ArgumentCount => 4;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == 4);

            if (arguments != null)
            {
                return Expression.Invoke(lambda, arguments[0], arguments[1], arguments[2], arguments[3]);
            }
            return Expression.Invoke(lambda, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3);
        }
    }

    internal sealed class InvocationExpression5 : InvocationExpression
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider
        private readonly Expression _arg1;  // storage for the 2nd argument
        private readonly Expression _arg2;  // storage for the 3rd argument
        private readonly Expression _arg3;  // storage for the 4th argument
        private readonly Expression _arg4;  // storage for the 5th argument

        public InvocationExpression5(Expression lambda, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
            : base(lambda, returnType)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
            _arg4 = arg4;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        public override Expression GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                3 => _arg3,
                4 => _arg4,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        public override int ArgumentCount => 5;

        internal override InvocationExpression Rewrite(Expression lambda, Expression[]? arguments)
        {
            Debug.Assert(lambda != null);
            Debug.Assert(arguments == null || arguments.Length == 5);

            if (arguments != null)
            {
                return Expression.Invoke(lambda, arguments[0], arguments[1], arguments[2], arguments[3], arguments[4]);
            }
            return Expression.Invoke(lambda, ExpressionUtils.ReturnObject<Expression>(_arg0), _arg1, _arg2, _arg3, _arg4);
        }
    }

    #endregion

    /// <summary>Provides the base class from which the classes that represent expression tree nodes are derived. It also contains <see langword="static" /> (<see langword="Shared" /> in Visual Basic) factory methods to create the various node types. This is an <see langword="abstract" /> class.</summary>
    /// <remarks></remarks>
    /// <example>The following code example shows how to create a block expression. The block expression consists of two <see cref="System.Linq.Expressions.MethodCallExpression" /> objects and one <see cref="System.Linq.Expressions.ConstantExpression" /> object.
    /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/cs/program.cs" id="Snippet13":::
    /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.linq.expressions.expressiondev10/vb/module1.vb" id="Snippet13":::</example>
    public partial class Expression
    {
        /// <summary>
        /// Creates an <see cref="InvocationExpression"/> that
        /// applies a delegate or lambda expression with no arguments.
        /// </summary>
        /// <returns>
        /// An <see cref="InvocationExpression"/> that
        /// applies the specified delegate or lambda expression.
        /// </returns>
        /// <param name="expression">
        /// An <see cref="Expression"/> that represents the delegate
        /// or lambda expression to be applied.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="expression"/>.Type does not represent a delegate type or an <see cref="Expression{TDelegate}"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The number of arguments does not contain match the number of parameters for the delegate represented by <paramref name="expression"/>.</exception>
        internal static InvocationExpression Invoke(Expression expression)
        {
            // COMPAT: This method is marked as non-public to avoid a gap between a 0-ary and 2-ary overload (see remark for the unary case below).

            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            MethodInfo method = GetInvokeMethod(expression);

            ParameterInfo[] pis = GetParametersForValidation(method, ExpressionType.Invoke);

            ValidateArgumentCount(method, ExpressionType.Invoke, 0, pis);

            return new InvocationExpression0(expression, method.ReturnType);
        }

        /// <summary>
        /// Creates an <see cref="InvocationExpression"/> that
        /// applies a delegate or lambda expression to one argument expression.
        /// </summary>
        /// <returns>
        /// An <see cref="InvocationExpression"/> that
        /// applies the specified delegate or lambda expression to the provided arguments.
        /// </returns>
        /// <param name="expression">
        /// An <see cref="Expression"/> that represents the delegate
        /// or lambda expression to be applied.
        /// </param>
        /// <param name="arg0">
        /// The <see cref="Expression"/> that represents the first argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="expression"/>.Type does not represent a delegate type or an <see cref="Expression{TDelegate}"/>.-or-The <see cref="Expression.Type"/> property of an argument expression is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The number of arguments does not contain match the number of parameters for the delegate represented by <paramref name="expression"/>.</exception>
        internal static InvocationExpression Invoke(Expression expression, Expression arg0)
        {
            // COMPAT: This method is marked as non-public to ensure compile-time compatibility for Expression.Invoke(e, null).

            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            MethodInfo method = GetInvokeMethod(expression);

            ParameterInfo[] pis = GetParametersForValidation(method, ExpressionType.Invoke);

            ValidateArgumentCount(method, ExpressionType.Invoke, 1, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Invoke, arg0, pis[0], nameof(expression), nameof(arg0));

            return new InvocationExpression1(expression, method.ReturnType, arg0);
        }

        /// <summary>
        /// Creates an <see cref="InvocationExpression"/> that
        /// applies a delegate or lambda expression to two argument expressions.
        /// </summary>
        /// <returns>
        /// An <see cref="InvocationExpression"/> that
        /// applies the specified delegate or lambda expression to the provided arguments.
        /// </returns>
        /// <param name="expression">
        /// An <see cref="Expression"/> that represents the delegate
        /// or lambda expression to be applied.
        /// </param>
        /// <param name="arg0">
        /// The <see cref="Expression"/> that represents the first argument.
        /// </param>
        /// <param name="arg1">
        /// The <see cref="Expression"/> that represents the second argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="expression"/>.Type does not represent a delegate type or an <see cref="Expression{TDelegate}"/>.-or-The <see cref="Expression.Type"/> property of an argument expression is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The number of arguments does not contain match the number of parameters for the delegate represented by <paramref name="expression"/>.</exception>
        internal static InvocationExpression Invoke(Expression expression, Expression arg0, Expression arg1)
        {
            // NB: This method is marked as non-public to avoid public API additions at this point.
            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            MethodInfo method = GetInvokeMethod(expression);

            ParameterInfo[] pis = GetParametersForValidation(method, ExpressionType.Invoke);

            ValidateArgumentCount(method, ExpressionType.Invoke, 2, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Invoke, arg0, pis[0], nameof(expression), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Invoke, arg1, pis[1], nameof(expression), nameof(arg1));

            return new InvocationExpression2(expression, method.ReturnType, arg0, arg1);
        }

        /// <summary>
        /// Creates an <see cref="InvocationExpression"/> that
        /// applies a delegate or lambda expression to three argument expressions.
        /// </summary>
        /// <returns>
        /// An <see cref="InvocationExpression"/> that
        /// applies the specified delegate or lambda expression to the provided arguments.
        /// </returns>
        /// <param name="expression">
        /// An <see cref="Expression"/> that represents the delegate
        /// or lambda expression to be applied.
        /// </param>
        /// <param name="arg0">
        /// The <see cref="Expression"/> that represents the first argument.
        /// </param>
        /// <param name="arg1">
        /// The <see cref="Expression"/> that represents the second argument.
        /// </param>
        /// <param name="arg2">
        /// The <see cref="Expression"/> that represents the third argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="expression"/>.Type does not represent a delegate type or an <see cref="Expression{TDelegate}"/>.-or-The <see cref="Expression.Type"/> property of an argument expression is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The number of arguments does not contain match the number of parameters for the delegate represented by <paramref name="expression"/>.</exception>
        internal static InvocationExpression Invoke(Expression expression, Expression arg0, Expression arg1, Expression arg2)
        {
            // NB: This method is marked as non-public to avoid public API additions at this point.

            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            MethodInfo method = GetInvokeMethod(expression);

            ParameterInfo[] pis = GetParametersForValidation(method, ExpressionType.Invoke);

            ValidateArgumentCount(method, ExpressionType.Invoke, 3, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Invoke, arg0, pis[0], nameof(expression), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Invoke, arg1, pis[1], nameof(expression), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Invoke, arg2, pis[2], nameof(expression), nameof(arg2));

            return new InvocationExpression3(expression, method.ReturnType, arg0, arg1, arg2);
        }

        /// <summary>
        /// Creates an <see cref="InvocationExpression"/> that
        /// applies a delegate or lambda expression to four argument expressions.
        /// </summary>
        /// <returns>
        /// An <see cref="InvocationExpression"/> that
        /// applies the specified delegate or lambda expression to the provided arguments.
        /// </returns>
        /// <param name="expression">
        /// An <see cref="Expression"/> that represents the delegate
        /// or lambda expression to be applied.
        /// </param>
        /// <param name="arg0">
        /// The <see cref="Expression"/> that represents the first argument.
        /// </param>
        /// <param name="arg1">
        /// The <see cref="Expression"/> that represents the second argument.
        /// </param>
        /// <param name="arg2">
        /// The <see cref="Expression"/> that represents the third argument.
        /// </param>
        /// <param name="arg3">
        /// The <see cref="Expression"/> that represents the fourth argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="expression"/>.Type does not represent a delegate type or an <see cref="Expression{TDelegate}"/>.-or-The <see cref="Expression.Type"/> property of an argument expression is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The number of arguments does not contain match the number of parameters for the delegate represented by <paramref name="expression"/>.</exception>
        internal static InvocationExpression Invoke(Expression expression, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            // NB: This method is marked as non-public to avoid public API additions at this point.

            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            MethodInfo method = GetInvokeMethod(expression);

            ParameterInfo[] pis = GetParametersForValidation(method, ExpressionType.Invoke);

            ValidateArgumentCount(method, ExpressionType.Invoke, 4, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Invoke, arg0, pis[0], nameof(expression), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Invoke, arg1, pis[1], nameof(expression), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Invoke, arg2, pis[2], nameof(expression), nameof(arg2));
            arg3 = ValidateOneArgument(method, ExpressionType.Invoke, arg3, pis[3], nameof(expression), nameof(arg3));

            return new InvocationExpression4(expression, method.ReturnType, arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// Creates an <see cref="InvocationExpression"/> that
        /// applies a delegate or lambda expression to five argument expressions.
        /// </summary>
        /// <returns>
        /// An <see cref="InvocationExpression"/> that
        /// applies the specified delegate or lambda expression to the provided arguments.
        /// </returns>
        /// <param name="expression">
        /// An <see cref="Expression"/> that represents the delegate
        /// or lambda expression to be applied.
        /// </param>
        /// <param name="arg0">
        /// The <see cref="Expression"/> that represents the first argument.
        /// </param>
        /// <param name="arg1">
        /// The <see cref="Expression"/> that represents the second argument.
        /// </param>
        /// <param name="arg2">
        /// The <see cref="Expression"/> that represents the third argument.
        /// </param>
        /// <param name="arg3">
        /// The <see cref="Expression"/> that represents the fourth argument.
        /// </param>
        /// <param name="arg4">
        /// The <see cref="Expression"/> that represents the fifth argument.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="expression"/>.Type does not represent a delegate type or an <see cref="Expression{TDelegate}"/>.-or-The <see cref="Expression.Type"/> property of an argument expression is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression"/>.</exception>
        /// <exception cref="InvalidOperationException">
        /// The number of arguments does not contain match the number of parameters for the delegate represented by <paramref name="expression"/>.</exception>
        internal static InvocationExpression Invoke(Expression expression, Expression arg0, Expression arg1, Expression arg2, Expression arg3, Expression arg4)
        {
            // NB: This method is marked as non-public to avoid public API additions at this point.

            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            MethodInfo method = GetInvokeMethod(expression);

            ParameterInfo[] pis = GetParametersForValidation(method, ExpressionType.Invoke);

            ValidateArgumentCount(method, ExpressionType.Invoke, 5, pis);

            arg0 = ValidateOneArgument(method, ExpressionType.Invoke, arg0, pis[0], nameof(expression), nameof(arg0));
            arg1 = ValidateOneArgument(method, ExpressionType.Invoke, arg1, pis[1], nameof(expression), nameof(arg1));
            arg2 = ValidateOneArgument(method, ExpressionType.Invoke, arg2, pis[2], nameof(expression), nameof(arg2));
            arg3 = ValidateOneArgument(method, ExpressionType.Invoke, arg3, pis[3], nameof(expression), nameof(arg3));
            arg4 = ValidateOneArgument(method, ExpressionType.Invoke, arg4, pis[4], nameof(expression), nameof(arg4));

            return new InvocationExpression5(expression, method.ReturnType, arg0, arg1, arg2, arg3, arg4);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.InvocationExpression" /> that applies a delegate or lambda expression to a list of argument expressions.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> that represents the delegate or lambda expression to be applied.</param>
        /// <param name="arguments">An array of <see cref="System.Linq.Expressions.Expression" /> objects that represent the arguments that the delegate or lambda expression is applied to.</param>
        /// <returns>An <see cref="System.Linq.Expressions.InvocationExpression" /> that applies the specified delegate or lambda expression to the provided arguments.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="expression" />.Type does not represent a delegate type or an <see cref="System.Linq.Expressions.Expression{T}" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the delegate represented by <paramref name="expression" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.InvocationExpression" /> represents the return type of the delegate that is represented by <paramref name="expression" />.Type.
        /// The <see cref="O:System.Linq.Expressions.InvocationExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.InvocationExpression" /> is empty if <paramref name="arguments" /> is <see langword="null" />. Otherwise, it contains the same elements as <paramref name="arguments" /> except that some of these <see cref="System.Linq.Expressions.Expression" /> objects may be *quoted*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding parameter of the delegate represented by `expression` is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format></remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.Invoke(System.Linq.Expressions.Expression,System.Linq.Expressions.Expression[])" /> method to create an <see cref="System.Linq.Expressions.InvocationExpression" /> that represents the invocation of a lambda expression with specified arguments.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet6":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet6":::</example>
        public static InvocationExpression Invoke(Expression expression, params Expression[]? arguments)
        {
            return Invoke(expression, (IEnumerable<Expression>?)arguments);
        }

        /// <summary>Creates an <see cref="System.Linq.Expressions.InvocationExpression" /> that applies a delegate or lambda expression to a list of argument expressions.</summary>
        /// <param name="expression">An <see cref="System.Linq.Expressions.Expression" /> that represents the delegate or lambda expression to be applied to.</param>
        /// <param name="arguments">An <see cref="System.Collections.Generic.IEnumerable{T}" /> that contains <see cref="System.Linq.Expressions.Expression" /> objects that represent the arguments that the delegate or lambda expression is applied to.</param>
        /// <returns>An <see cref="System.Linq.Expressions.InvocationExpression" /> that applies the specified delegate or lambda expression to the provided arguments.</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="expression" /> is <see langword="null" />.</exception>
        /// <exception cref="System.ArgumentException"><paramref name="expression" />.Type does not represent a delegate type or an <see cref="System.Linq.Expressions.Expression{T}" />.
        /// -or-
        /// The <see cref="System.Linq.Expressions.Expression.Type" /> property of an element of <paramref name="arguments" /> is not assignable to the type of the corresponding parameter of the delegate represented by <paramref name="expression" />.</exception>
        /// <exception cref="System.InvalidOperationException"><paramref name="arguments" /> does not contain the same number of elements as the list of parameters for the delegate represented by <paramref name="expression" />.</exception>
        /// <remarks>The <see cref="O:System.Linq.Expressions.Expression.Type" /> property of the resulting <see cref="System.Linq.Expressions.InvocationExpression" /> represents the return type of the delegate that is represented by <paramref name="expression" />.Type.
        /// The <see cref="O:System.Linq.Expressions.InvocationExpression.Arguments" /> property of the resulting <see cref="System.Linq.Expressions.InvocationExpression" /> is empty if <paramref name="arguments" /> is <see langword="null" />. Otherwise, it contains the same elements as <paramref name="arguments" /> except that some of these <see cref="System.Linq.Expressions.Expression" /> objects may be *quoted*.
        /// <format type="text/markdown"><![CDATA[
        /// > [!NOTE]
        /// >  An element will be quoted only if the corresponding parameter of the delegate represented by `expression` is of type <xref:System.Linq.Expressions.Expression>. Quoting means the element is wrapped in a <xref:System.Linq.Expressions.ExpressionType.Quote> node. The resulting node is a <xref:System.Linq.Expressions.UnaryExpression> whose <xref:System.Linq.Expressions.UnaryExpression.Operand%2A> property is the element of `arguments`.
        /// ]]></format></remarks>
        /// <example>The following example demonstrates how to use the <see cref="System.Linq.Expressions.Expression.Invoke(System.Linq.Expressions.Expression,System.Linq.Expressions.Expression[])" /> method to create an <see cref="System.Linq.Expressions.InvocationExpression" /> that represents the invocation of a lambda expression with specified arguments.
        /// :::code language="csharp" source="~/samples/snippets/csharp/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/CS/Expression.cs" id="Snippet6":::
        /// :::code language="vb" source="~/samples/snippets/visualbasic/VS_Snippets_CLR_System/system.Linq.Expressions.Expression/VB/Expression.vb" id="Snippet6":::</example>
        public static InvocationExpression Invoke(Expression expression, IEnumerable<Expression>? arguments)
        {
            IReadOnlyList<Expression> argumentList = arguments as IReadOnlyList<Expression> ?? arguments.ToReadOnly();

            switch (argumentList.Count)
            {
                case 0:
                    return Invoke(expression);
                case 1:
                    return Invoke(expression, argumentList[0]);
                case 2:
                    return Invoke(expression, argumentList[0], argumentList[1]);
                case 3:
                    return Invoke(expression, argumentList[0], argumentList[1], argumentList[2]);
                case 4:
                    return Invoke(expression, argumentList[0], argumentList[1], argumentList[2], argumentList[3]);
                case 5:
                    return Invoke(expression, argumentList[0], argumentList[1], argumentList[2], argumentList[3], argumentList[4]);
            }

            ExpressionUtils.RequiresCanRead(expression, nameof(expression));

            ReadOnlyCollection<Expression> args = argumentList.ToReadOnly(); // Ensure is TrueReadOnlyCollection when count > 5. Returns fast if it already is.
            MethodInfo mi = GetInvokeMethod(expression);
            ValidateArgumentTypes(mi, ExpressionType.Invoke, ref args, nameof(expression));
            return new InvocationExpressionN(expression, args, mi.ReturnType);
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
                Type? exprType = TypeUtils.FindGenericType(typeof(Expression<>), expression.Type);
                if (exprType == null)
                {
                    throw Error.ExpressionTypeNotInvocable(expression.Type, nameof(expression));
                }
                delegateType = exprType.GetGenericArguments()[0];
            }

            return delegateType.GetInvokeMethod();
        }
    }
}
