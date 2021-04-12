// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic.Utils;
using System.Linq.Expressions.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;

using DelegateHelpers = System.Linq.Expressions.Compiler.DelegateHelpers;

namespace System.Linq.Expressions
{
    /// <summary>Represents a dynamic operation.</summary>
    public class DynamicExpression : Expression, IDynamicExpression
    {
        internal DynamicExpression(Type delegateType, CallSiteBinder binder)
        {
            Debug.Assert(delegateType.GetInvokeMethod().GetReturnType() == typeof(object) || GetType() != typeof(DynamicExpression));
            DelegateType = delegateType;
            Binder = binder;
        }

        /// <summary>
        /// Gets a value that indicates whether the expression tree node can be reduced.
        /// </summary>
        public override bool CanReduce => true;

        /// <summary>
        /// Reduces the dynamic expression node to a simpler expression.
        /// </summary>
        /// <returns>The reduced expression.</returns>
        [DynamicDependency("Target", typeof(CallSite<>))]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "The field will be preserved by the DynamicDependency")]
        public override Expression Reduce()
        {
            var site = Expression.Constant(CallSite.Create(DelegateType, Binder));
            return Expression.Invoke(
                        Expression.Field(
                            site,
                            "Target"),
                        this.Arguments.AddFirst(site));
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, ReadOnlyCollection<Expression> arguments)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpressionN(delegateType, binder, arguments);
            }
            else
            {
                return new TypedDynamicExpressionN(returnType, delegateType, binder, arguments);
            }
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression1(delegateType, binder, arg0);
            }
            else
            {
                return new TypedDynamicExpression1(returnType, delegateType, binder, arg0);
            }
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression2(delegateType, binder, arg0, arg1);
            }
            else
            {
                return new TypedDynamicExpression2(returnType, delegateType, binder, arg0, arg1);
            }
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression3(delegateType, binder, arg0, arg1, arg2);
            }
            else
            {
                return new TypedDynamicExpression3(returnType, delegateType, binder, arg0, arg1, arg2);
            }
        }

        internal static DynamicExpression Make(Type returnType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            if (returnType == typeof(object))
            {
                return new DynamicExpression4(delegateType, binder, arg0, arg1, arg2, arg3);
            }
            else
            {
                return new TypedDynamicExpression4(returnType, delegateType, binder, arg0, arg1, arg2, arg3);
            }
        }

        /// <summary>Gets the static type of the expression that this <see cref="System.Linq.Expressions.Expression" /> represents.</summary>
        /// <value>The <see cref="System.Linq.Expressions.DynamicExpression.Type" /> that represents the static type of the expression.</value>
        public override Type Type => typeof(object);

        /// <summary>Returns the node type of this expression. Extension nodes should return <see cref="System.Linq.Expressions.ExpressionType.Extension" /> when overriding this method.</summary>
        /// <value>The <see cref="System.Linq.Expressions.ExpressionType" /> of the expression.</value>
        public sealed override ExpressionType NodeType => ExpressionType.Dynamic;

        /// <summary>Gets the <see cref="System.Runtime.CompilerServices.CallSiteBinder" />, which determines the run-time behavior of the dynamic site.</summary>
        /// <value>The <see cref="System.Runtime.CompilerServices.CallSiteBinder" />, which determines the run-time behavior of the dynamic site.</value>
        public CallSiteBinder Binder { get; }

        /// <summary>Gets the type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</summary>
        /// <value>The <see cref="System.Type" /> object representing the type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</value>
        public Type DelegateType { get; }

        /// <summary>Gets the arguments to the dynamic operation.</summary>
        /// <value>The read-only collections containing the arguments to the dynamic operation.</value>
        public ReadOnlyCollection<Expression> Arguments => GetOrMakeArguments();

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>Dispatches to the specific visit method for this node type. For example, <see cref="System.Linq.Expressions.MethodCallExpression" /> calls the <see cref="System.Linq.Expressions.ExpressionVisitor.VisitMethodCall(System.Linq.Expressions.MethodCallExpression)" />.</summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        /// <remarks>This default implementation for <see cref="System.Linq.Expressions.ExpressionType.Extension" /> nodes calls <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />. Override this method to call into a more specific method on a derived visitor class of the <see cref="System.Linq.Expressions.ExpressionVisitor" /> class. However, it should still support unknown visitors by calling <see cref="O:System.Linq.Expressions.ExpressionVisitor.VisitExtension" />.</remarks>
        protected internal override Expression Accept(ExpressionVisitor visitor)
        {
            if (visitor is DynamicExpressionVisitor dynVisitor)
            {
                return dynVisitor.VisitDynamic(this);
            }

            return base.Accept(visitor);
        }

        /// <summary>
        /// Makes a copy of this node replacing the args with the provided values.  The
        /// number of the args needs to match the number of the current block.
        ///
        /// This helper is provided to allow re-writing of nodes to not depend on the specific optimized
        /// subclass of DynamicExpression which is being used.
        /// </summary>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual DynamicExpression Rewrite(Expression[] args)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>Compares the value sent to the parameter, <paramref name="arguments" />, to the <see langword="Arguments" /> property of the current instance of <see langword="DynamicExpression" />. If the values of the parameter and the property are equal, the current instance is returned. If they are not equal, a new <see langword="DynamicExpression" /> instance is returned that is identical to the current instance except that the <see langword="Arguments" /> property is set to the value of parameter <paramref name="arguments" />.</summary>
        /// <param name="arguments">The <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> property of the result.</param>
        /// <returns>This expression if no children are changed or an expression with the updated children.</returns>
        public DynamicExpression Update(IEnumerable<Expression>? arguments)
        {
            ICollection<Expression>? args;
            if (arguments == null)
            {
                args = null;
            }
            else
            {
                args = arguments as ICollection<Expression>;
                if (args == null)
                {
                    arguments = args = arguments.ToReadOnly();
                }
            }

            if (SameArguments(args))
            {
                return this;
            }

            return ExpressionExtension.MakeDynamic(DelegateType, Binder, arguments);
        }

        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        internal virtual bool SameArguments(ICollection<Expression>? arguments)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>Returns the argument at index, throwing if index is out of bounds. You should not use this member. It is only public due to assembly refactoring, and it is used internally for performance optimizations.</summary>
        /// <param name="index">The index of the argument.</param>
        /// <returns>Returns <see cref="System.Linq.Expressions.Expression" />.</returns>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        Expression IArgumentProvider.GetArgument(int index)
        {
            throw ContractUtils.Unreachable;
        }

        /// <summary>Returns the number of arguments to the expression tree node. You should not use this member. It is only public due to assembly refactoring, and it is used internally for performance optimizations.</summary>
        /// <value>Returns <see cref="int" />.</value>
        /// <remarks>This member is an explicit interface member implementation. It can be used only when the <see cref="System.Linq.Expressions.DynamicExpression" /> instance is cast to an <see cref="System.Linq.Expressions.IArgumentProvider" /> interface.</remarks>
        [ExcludeFromCodeCoverage(Justification = "Unreachable")]
        int IArgumentProvider.ArgumentCount
        {
            get { throw ContractUtils.Unreachable; }
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result is inferred from the types of the arguments and the specified return type.</remarks>
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
        {
            return ExpressionExtension.Dynamic(binder, returnType, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />,  and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result is inferred from the types of the arguments and the specified return type.</remarks>
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
        {
            return ExpressionExtension.Dynamic(binder, returnType, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />,  and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result is inferred from the types of the arguments and the specified return type.</remarks>
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0)
        {
            return ExpressionExtension.Dynamic(binder, returnType, arg0);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result is inferred from the types of the arguments and the specified return type.</remarks>
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1)
        {
            return ExpressionExtension.Dynamic(binder, returnType, arg0, arg1);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result is inferred from the types of the arguments and the specified return type.</remarks>
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2)
        {
            return ExpressionExtension.Dynamic(binder, returnType, arg0, arg1, arg2);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <param name="arg3">The fourth argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.Binder" /> and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        /// <remarks>The <see cref="O:System.Linq.Expressions.DynamicExpression.DelegateType" /> property of the result is inferred from the types of the arguments and the specified return type.</remarks>
        public static new DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            return ExpressionExtension.Dynamic(binder, returnType, arg0, arg1, arg2, arg3);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression>? arguments)
        {
            return ExpressionExtension.MakeDynamic(delegateType, binder, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" />.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[]? arguments)
        {
            return ExpressionExtension.MakeDynamic(delegateType, binder, arguments);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and one argument.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0)
        {
            return ExpressionExtension.MakeDynamic(delegateType, binder, arg0);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and two arguments.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
        {
            return ExpressionExtension.MakeDynamic(delegateType, binder, arg0, arg1);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and three arguments.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
        {
            return ExpressionExtension.MakeDynamic(delegateType, binder, arg0, arg1, arg2);
        }

        /// <summary>Creates a <see cref="System.Linq.Expressions.DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="System.Runtime.CompilerServices.CallSiteBinder" /> and four arguments.</summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="System.Runtime.CompilerServices.CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <param name="arg3">The fourth argument to the dynamic operation.</param>
        /// <returns>A <see cref="System.Linq.Expressions.DynamicExpression" /> that has <see cref="System.Linq.Expressions.DynamicExpression.NodeType" /> equal to <see cref="System.Linq.Expressions.ExpressionType.Dynamic" />, and has the <see cref="System.Linq.Expressions.DynamicExpression.DelegateType" />, <see cref="System.Linq.Expressions.DynamicExpression.Binder" />, and <see cref="System.Linq.Expressions.DynamicExpression.Arguments" /> set to the specified values.</returns>
        public static new DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            return ExpressionExtension.MakeDynamic(delegateType, binder, arg0, arg1, arg2, arg3);
        }

        /// <summary>Rewrite this node replacing the dynamic expression's arguments with the provided values. The number of <paramref name="args" /> needs to match the number of the current expression. You should not use this type. It is only public due to assembly refactoring, and it is used internally for performance optimizations. This helper method allows re-writing of nodes to be independent of the specific implementation class deriving from DynamicExpression that is being used at the call site.</summary>
        /// <param name="args">The arguments.</param>
        /// <returns>Returns <see cref="System.Linq.Expressions.Expression" />, the rewritten expression.</returns>
        Expression IDynamicExpression.Rewrite(Expression[] args) => Rewrite(args);

        /// <summary>Optionally creates the CallSite and returns the CallSite for the DynamicExpression's polymorphic inline cache. You should not use this member. It is only public due to assembly refactoring, and it is used internally for performance optimizations.</summary>
        /// <returns>Returns <see cref="object" />.</returns>
        object IDynamicExpression.CreateCallSite()
        {
            return CallSite.Create(this.DelegateType, this.Binder);
        }
    }

    #region Specialized Subclasses

    internal class DynamicExpressionN : DynamicExpression, IArgumentProvider
    {
        private IReadOnlyList<Expression> _arguments;       // storage for the original IList or read-only collection.  See IArgumentProvider for more info.

        internal DynamicExpressionN(Type delegateType, CallSiteBinder binder, IReadOnlyList<Expression> arguments)
            : base(delegateType, binder)
        {
            _arguments = arguments;
        }

        Expression IArgumentProvider.GetArgument(int index) => _arguments[index];

        internal override bool SameArguments(ICollection<Expression>? arguments) =>
            ExpressionUtils.SameElements(arguments, _arguments);

        int IArgumentProvider.ArgumentCount => _arguments.Count;

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(ref _arguments);
        }

        internal override DynamicExpression Rewrite(Expression[] args)
        {
            Debug.Assert(args.Length == ((IArgumentProvider)this).ArgumentCount);

            return ExpressionExtension.MakeDynamic(DelegateType, Binder, args);
        }
    }

    internal sealed class TypedDynamicExpressionN : DynamicExpressionN
    {
        internal TypedDynamicExpressionN(Type returnType, Type delegateType, CallSiteBinder binder, IReadOnlyList<Expression> arguments)
            : base(delegateType, binder, arguments)
        {
            Debug.Assert(delegateType.GetInvokeMethod().GetReturnType() == returnType);
            Type = returnType;
        }

        public sealed override Type Type { get; }
    }

    internal class DynamicExpression1 : DynamicExpression, IArgumentProvider
    {
        private object _arg0;               // storage for the 1st argument or a read-only collection.  See IArgumentProvider for more info.

        internal DynamicExpression1(Type delegateType, CallSiteBinder binder, Expression arg0)
            : base(delegateType, binder)
        {
            _arg0 = arg0;
        }

        Expression IArgumentProvider.GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        int IArgumentProvider.ArgumentCount => 1;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 1)
            {
                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    return en.Current == ExpressionUtils.ReturnObject<Expression>(_arg0);
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override DynamicExpression Rewrite(Expression[] args)
        {
            Debug.Assert(args.Length == 1);

            return ExpressionExtension.MakeDynamic(DelegateType, Binder, args[0]);
        }
    }

    internal sealed class TypedDynamicExpression1 : DynamicExpression1
    {
        internal TypedDynamicExpression1(Type retType, Type delegateType, CallSiteBinder binder, Expression arg0)
            : base(delegateType, binder, arg0)
        {
            Type = retType;
        }

        public sealed override Type Type { get; }
    }

    internal class DynamicExpression2 : DynamicExpression, IArgumentProvider
    {
        private object _arg0;                   // storage for the 1st argument or a read-only collection.  See IArgumentProvider for more info.
        private readonly Expression _arg1;      // storage for the 2nd argument

        internal DynamicExpression2(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
            : base(delegateType, binder)
        {
            _arg0 = arg0;
            _arg1 = arg1;
        }

        Expression IArgumentProvider.GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        int IArgumentProvider.ArgumentCount => 2;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 2)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        return en.Current == _arg1;
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override DynamicExpression Rewrite(Expression[] args)
        {
            Debug.Assert(args.Length == 2);

            return ExpressionExtension.MakeDynamic(DelegateType, Binder, args[0], args[1]);
        }
    }

    internal sealed class TypedDynamicExpression2 : DynamicExpression2
    {
        internal TypedDynamicExpression2(Type retType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
            : base(delegateType, binder, arg0, arg1)
        {
            Type = retType;
        }

        public sealed override Type Type { get; }
    }

    internal class DynamicExpression3 : DynamicExpression, IArgumentProvider
    {
        private object _arg0;                       // storage for the 1st argument or a read-only collection.  See IArgumentProvider for more info.
        private readonly Expression _arg1, _arg2;   // storage for the 2nd & 3rd arguments

        internal DynamicExpression3(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
            : base(delegateType, binder)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
        }

        Expression IArgumentProvider.GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        int IArgumentProvider.ArgumentCount => 3;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 3)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            return en.Current == _arg2;
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override DynamicExpression Rewrite(Expression[] args)
        {
            Debug.Assert(args.Length == 3);

            return ExpressionExtension.MakeDynamic(DelegateType, Binder, args[0], args[1], args[2]);
        }
    }

    internal sealed class TypedDynamicExpression3 : DynamicExpression3
    {
        internal TypedDynamicExpression3(Type retType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
            : base(delegateType, binder, arg0, arg1, arg2)
        {
            Type = retType;
        }

        public sealed override Type Type { get; }
    }

    internal class DynamicExpression4 : DynamicExpression, IArgumentProvider
    {
        private object _arg0;                               // storage for the 1st argument or a read-only collection.  See IArgumentProvider for more info.
        private readonly Expression _arg1, _arg2, _arg3;    // storage for the 2nd - 4th arguments

        internal DynamicExpression4(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
            : base(delegateType, binder)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }

        Expression IArgumentProvider.GetArgument(int index) =>
            index switch
            {
                0 => ExpressionUtils.ReturnObject<Expression>(_arg0),
                1 => _arg1,
                2 => _arg2,
                3 => _arg3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };

        int IArgumentProvider.ArgumentCount => 4;

        internal override bool SameArguments(ICollection<Expression>? arguments)
        {
            if (arguments != null && arguments.Count == 4)
            {
                if (_arg0 is ReadOnlyCollection<Expression> alreadyCollection)
                {
                    return ExpressionUtils.SameElements(arguments, alreadyCollection);
                }

                using (IEnumerator<Expression> en = arguments.GetEnumerator())
                {
                    en.MoveNext();
                    if (en.Current == _arg0)
                    {
                        en.MoveNext();
                        if (en.Current == _arg1)
                        {
                            en.MoveNext();
                            if (en.Current == _arg2)
                            {
                                en.MoveNext();
                                return en.Current == _arg3;
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal override ReadOnlyCollection<Expression> GetOrMakeArguments()
        {
            return ExpressionUtils.ReturnReadOnly(this, ref _arg0);
        }

        internal override DynamicExpression Rewrite(Expression[] args)
        {
            Debug.Assert(args.Length == 4);

            return ExpressionExtension.MakeDynamic(DelegateType, Binder, args[0], args[1], args[2], args[3]);
        }
    }

    internal sealed class TypedDynamicExpression4 : DynamicExpression4
    {
        internal TypedDynamicExpression4(Type retType, Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
            : base(delegateType, binder, arg0, arg1, arg2, arg3)
        {
            Type = retType;
        }

        public sealed override Type Type { get; }
    }

    #endregion

    internal static class ExpressionExtension
    {
        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[]? arguments)
        {
            return MakeDynamic(delegateType, binder, (IEnumerable<Expression>?)arguments);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression>? arguments)
        {
            IReadOnlyList<Expression> argumentList = arguments as IReadOnlyList<Expression> ?? arguments.ToReadOnly();
            switch (argumentList.Count)
            {
                case 1:
                    return MakeDynamic(delegateType, binder, argumentList[0]);
                case 2:
                    return MakeDynamic(delegateType, binder, argumentList[0], argumentList[1]);
                case 3:
                    return MakeDynamic(delegateType, binder, argumentList[0], argumentList[1], argumentList[2]);
                case 4:
                    return MakeDynamic(delegateType, binder, argumentList[0], argumentList[1], argumentList[2], argumentList[3]);
            }

            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);

            var args = arguments.ToReadOnly(); // Ensure is TrueReadOnlyCollection when count > 4. Returns fast if it already is.
            ExpressionUtils.ValidateArgumentTypes(method, ExpressionType.Dynamic, ref args, nameof(delegateType));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, args);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and one argument.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);
            var parameters = method.GetParametersCached();

            ExpressionUtils.ValidateArgumentCount(method, ExpressionType.Dynamic, 2, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, arg0);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and two arguments.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);
            var parameters = method.GetParametersCached();

            ExpressionUtils.ValidateArgumentCount(method, ExpressionType.Dynamic, 3, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg1, parameters[2], nameof(delegateType), nameof(arg1));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, arg0, arg1);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and three arguments.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);
            var parameters = method.GetParametersCached();

            ExpressionUtils.ValidateArgumentCount(method, ExpressionType.Dynamic, 4, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg1, parameters[2], nameof(delegateType), nameof(arg1));
            ValidateDynamicArgument(arg2, nameof(arg2));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg2, parameters[3], nameof(delegateType), nameof(arg2));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, arg0, arg1, arg2);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and four arguments.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <param name="arg3">The fourth argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);
            var parameters = method.GetParametersCached();

            ExpressionUtils.ValidateArgumentCount(method, ExpressionType.Dynamic, 5, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg1, parameters[2], nameof(delegateType), nameof(arg1));
            ValidateDynamicArgument(arg2, nameof(arg2));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg2, parameters[3], nameof(delegateType), nameof(arg2));
            ValidateDynamicArgument(arg3, nameof(arg3));
            ExpressionUtils.ValidateOneArgument(method, ExpressionType.Dynamic, arg3, parameters[4], nameof(delegateType), nameof(arg3));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, arg0, arg1, arg2, arg3);
        }

        private static MethodInfo GetValidMethodForDynamic(Type delegateType)
        {
            var method = delegateType.GetInvokeMethod();
            var pi = method.GetParametersCached();
            if (pi.Length == 0 || pi[0].ParameterType != typeof(CallSite)) throw Error.FirstArgumentMustBeCallSite();
            return method;
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
        {
            return Dynamic(binder, returnType, (IEnumerable<Expression>)arguments);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ValidateDynamicArgument(arg0, nameof(arg0));

            DelegateHelpers.TypeInfo info = DelegateHelpers.GetNextTypeInfo(
                returnType,
                DelegateHelpers.GetNextTypeInfo(
                    arg0.Type,
                    DelegateHelpers.NextTypeInfo(typeof(CallSite))
                )
            );

            Type delegateType = info.DelegateType ?? info.MakeDelegateType(returnType, arg0);

            return DynamicExpression.Make(returnType, delegateType, binder, arg0);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));

            DelegateHelpers.TypeInfo info = DelegateHelpers.GetNextTypeInfo(
                returnType,
                DelegateHelpers.GetNextTypeInfo(
                    arg1.Type,
                    DelegateHelpers.GetNextTypeInfo(
                        arg0.Type,
                        DelegateHelpers.NextTypeInfo(typeof(CallSite))
                    )
                )
            );

            Type delegateType = info.DelegateType ?? info.MakeDelegateType(returnType, arg0, arg1);

            return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ValidateDynamicArgument(arg2, nameof(arg2));

            DelegateHelpers.TypeInfo info = DelegateHelpers.GetNextTypeInfo(
                returnType,
                DelegateHelpers.GetNextTypeInfo(
                    arg2.Type,
                    DelegateHelpers.GetNextTypeInfo(
                        arg1.Type,
                        DelegateHelpers.GetNextTypeInfo(
                            arg0.Type,
                            DelegateHelpers.NextTypeInfo(typeof(CallSite))
                        )
                    )
                )
            );

            Type delegateType = info.DelegateType ?? info.MakeDelegateType(returnType, arg0, arg1, arg2);

            return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1, arg2);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <param name="arg3">The fourth argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ValidateDynamicArgument(arg2, nameof(arg2));
            ValidateDynamicArgument(arg3, nameof(arg3));

            DelegateHelpers.TypeInfo info = DelegateHelpers.GetNextTypeInfo(
                returnType,
                DelegateHelpers.GetNextTypeInfo(
                    arg3.Type,
                    DelegateHelpers.GetNextTypeInfo(
                        arg2.Type,
                        DelegateHelpers.GetNextTypeInfo(
                            arg1.Type,
                            DelegateHelpers.GetNextTypeInfo(
                                arg0.Type,
                                DelegateHelpers.NextTypeInfo(typeof(CallSite))
                            )
                        )
                    )
                )
            );

            Type delegateType = info.DelegateType ?? info.MakeDelegateType(returnType, arg0, arg1, arg2, arg3);

            return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1, arg2, arg3);
        }

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="DynamicExpression.NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(arguments, nameof(arguments));
            ContractUtils.RequiresNotNull(returnType, nameof(returnType));

            var args = arguments.ToReadOnly();
            ContractUtils.RequiresNotEmpty(args, nameof(arguments));
            return MakeDynamic(binder, returnType, args);
        }

        private static DynamicExpression MakeDynamic(CallSiteBinder binder, Type returnType, ReadOnlyCollection<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));

            int n = arguments.Count;

            for (int i = 0; i < n; i++)
            {
                Expression arg = arguments[i];

                ValidateDynamicArgument(arg, nameof(arguments), i);
            }

            Type delegateType = DelegateHelpers.MakeCallSiteDelegate(arguments, returnType);

            // Since we made a delegate with argument types that exactly match,
            // we can skip delegate and argument validation

            return n switch
            {
                1 => DynamicExpression.Make(returnType, delegateType, binder, arguments[0]),
                2 => DynamicExpression.Make(returnType, delegateType, binder, arguments[0], arguments[1]),
                3 => DynamicExpression.Make(returnType, delegateType, binder, arguments[0], arguments[1], arguments[2]),
                4 => DynamicExpression.Make(returnType, delegateType, binder, arguments[0], arguments[1], arguments[2], arguments[3]),
                _ => DynamicExpression.Make(returnType, delegateType, binder, arguments),
            };
        }

        private static void ValidateDynamicArgument(Expression arg, string paramName)
        {
            ValidateDynamicArgument(arg, paramName, -1);
        }

        private static void ValidateDynamicArgument(Expression arg, string paramName, int index)
        {
            ExpressionUtils.RequiresCanRead(arg, paramName, index);
            var type = arg.Type;
            ContractUtils.RequiresNotNull(type, nameof(type));
            TypeUtils.ValidateType(type, nameof(type), allowByRef: true, allowPointer: true);
            if (type == typeof(void)) throw Error.ArgumentTypeCannotBeVoid();
        }
    }
}
