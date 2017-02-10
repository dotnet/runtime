// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic.Utils;
using System.Linq.Expressions.Compiler;
using System.Reflection;
using System.Runtime.CompilerServices;
using DelegateHelpers = System.Linq.Expressions.Compiler.DelegateHelpers;

namespace System.Linq.Expressions
{
    /// <summary>
    /// The base type for all nodes in Expression Trees.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling")]
    public abstract partial class Expression
    {
        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments) =>
            DynamicExpression.Dynamic(binder, returnType, arguments);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0) =>
            DynamicExpression.Dynamic(binder, returnType, arg0);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1) =>
            DynamicExpression.Dynamic(binder, returnType, arg0, arg1);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2) =>
            DynamicExpression.Dynamic(binder, returnType, arg0, arg1, arg2);

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, Expression arg0, Expression arg1, Expression arg2, Expression arg3) =>
            DynamicExpression.Dynamic(binder, returnType, arg0, arg1, arg2, arg3);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="returnType">The result type of the dynamic expression.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.Binder">Binder</see> and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        /// <remarks>
        /// The <see cref="DynamicExpression.DelegateType">DelegateType</see> property of the
        /// result will be inferred from the types of the arguments and the specified return type.
        /// </remarks>
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments) =>
            DynamicExpression.Dynamic(binder, returnType, arguments);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression> arguments) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arguments);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and one argument.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and two arguments.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0, arg1);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" /> and three arguments.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arg0">The first argument to the dynamic operation.</param>
        /// <param name="arg1">The second argument to the dynamic operation.</param>
        /// <param name="arg2">The third argument to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0, arg1, arg2);

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0, Expression arg1, Expression arg2, Expression arg3) =>
            DynamicExpression.MakeDynamic(delegateType, binder, arg0, arg1, arg2, arg3);

        /// <summary>
        /// Creates a <see cref="DynamicExpression" /> that represents a dynamic operation bound by the provided <see cref="CallSiteBinder" />.
        /// </summary>
        /// <param name="delegateType">The type of the delegate used by the <see cref="CallSite" />.</param>
        /// <param name="binder">The runtime binder for the dynamic operation.</param>
        /// <param name="arguments">The arguments to the dynamic operation.</param>
        /// <returns>
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
        /// <see cref="ExpressionType.Dynamic">Dynamic</see> and has the
        /// <see cref="DynamicExpression.DelegateType">DelegateType</see>,
        /// <see cref="DynamicExpression.Binder">Binder</see>, and
        /// <see cref="DynamicExpression.Arguments">Arguments</see> set to the specified values.
        /// </returns>
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[] arguments) =>
            MakeDynamic(delegateType, binder, (IEnumerable<Expression>)arguments);
    }
}
