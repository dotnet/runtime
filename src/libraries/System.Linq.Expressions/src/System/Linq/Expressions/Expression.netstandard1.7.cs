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
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, IEnumerable<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(arguments, nameof(arguments));
            ContractUtils.RequiresNotNull(returnType, nameof(returnType));

            var args = arguments.ToReadOnly();
            ContractUtils.RequiresNotEmpty(args, nameof(args));
            return MakeDynamic(binder, returnType, args);
        }

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

            Type delegateType = info.DelegateType;
            if (delegateType == null)
            {
                delegateType = info.MakeDelegateType(returnType, arg0);
            }

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
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

            Type delegateType = info.DelegateType;
            if (delegateType == null)
            {
                delegateType = info.MakeDelegateType(returnType, arg0, arg1);
            }

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
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

            Type delegateType = info.DelegateType;
            if (delegateType == null)
            {
                delegateType = info.MakeDelegateType(returnType, arg0, arg1, arg2);
            }

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
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

            Type delegateType = info.DelegateType;
            if (delegateType == null)
            {
                delegateType = info.MakeDelegateType(returnType, arg0, arg1, arg2, arg3);
            }

            return DynamicExpression.Make(returnType, delegateType, binder, arg0, arg1, arg2, arg3);
        }

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
        public static DynamicExpression Dynamic(CallSiteBinder binder, Type returnType, params Expression[] arguments)
        {
            return Dynamic(binder, returnType, (IEnumerable<Expression>)arguments);
        }

        private static DynamicExpression MakeDynamic(CallSiteBinder binder, Type returnType, ReadOnlyCollection<Expression> args)
        {
            ContractUtils.RequiresNotNull(binder, nameof(binder));

            for (int i = 0; i < args.Count; i++)
            {
                Expression arg = args[i];

                ValidateDynamicArgument(arg, nameof(arg));
            }

            Type delegateType = DelegateHelpers.MakeCallSiteDelegate(args, returnType);

            // Since we made a delegate with argument types that exactly match,
            // we can skip delegate and argument validation

            switch (args.Count)
            {
                case 1: return DynamicExpression.Make(returnType, delegateType, binder, args[0]);
                case 2: return DynamicExpression.Make(returnType, delegateType, binder, args[0], args[1]);
                case 3: return DynamicExpression.Make(returnType, delegateType, binder, args[0], args[1], args[2]);
                case 4: return DynamicExpression.Make(returnType, delegateType, binder, args[0], args[1], args[2], args[3]);
                default: return DynamicExpression.Make(returnType, delegateType, binder, args);
            }
        }

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
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, IEnumerable<Expression> arguments)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);

            var args = arguments.ToReadOnly();
            ValidateArgumentTypes(method, ExpressionType.Dynamic, ref args, nameof(delegateType));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, args);
        }

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
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, Expression arg0)
        {
            ContractUtils.RequiresNotNull(delegateType, nameof(delegateType));
            ContractUtils.RequiresNotNull(binder, nameof(binder));
            if (!delegateType.IsSubclassOf(typeof(MulticastDelegate))) throw Error.TypeMustBeDerivedFromSystemDelegate();

            var method = GetValidMethodForDynamic(delegateType);
            var parameters = method.GetParametersCached();

            ValidateArgumentCount(method, ExpressionType.Dynamic, 2, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
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

            ValidateArgumentCount(method, ExpressionType.Dynamic, 3, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg1, parameters[2], nameof(delegateType), nameof(arg1));

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
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

            ValidateArgumentCount(method, ExpressionType.Dynamic, 4, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg1, parameters[2], nameof(delegateType), nameof(arg1));
            ValidateDynamicArgument(arg2, nameof(arg2));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg2, parameters[3], nameof(delegateType), nameof(arg2));

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
        /// A <see cref="DynamicExpression" /> that has <see cref="NodeType" /> equal to
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

            ValidateArgumentCount(method, ExpressionType.Dynamic, 5, parameters);
            ValidateDynamicArgument(arg0, nameof(arg0));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg0, parameters[1], nameof(delegateType), nameof(arg0));
            ValidateDynamicArgument(arg1, nameof(arg1));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg1, parameters[2], nameof(delegateType), nameof(arg1));
            ValidateDynamicArgument(arg2, nameof(arg2));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg2, parameters[3], nameof(delegateType), nameof(arg2));
            ValidateDynamicArgument(arg3, nameof(arg3));
            ValidateOneArgument(method, ExpressionType.Dynamic, arg3, parameters[4], nameof(delegateType), nameof(arg3));

            return DynamicExpression.Make(method.GetReturnType(), delegateType, binder, arg0, arg1, arg2, arg3);
        }

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
        public static DynamicExpression MakeDynamic(Type delegateType, CallSiteBinder binder, params Expression[] arguments)
        {
            return MakeDynamic(delegateType, binder, (IEnumerable<Expression>)arguments);
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
            TypeUtils.ValidateType(type, nameof(type));
            if (type == typeof(void)) throw Error.ArgumentTypeCannotBeVoid();
        }

        private static MethodInfo GetValidMethodForDynamic(Type delegateType)
        {
            var method = delegateType.GetMethod("Invoke");
            var pi = method.GetParametersCached();
            if (pi.Length == 0 || pi[0].ParameterType != typeof(CallSite)) throw Error.FirstArgumentMustBeCallSite();
            return method;
        }
    }
}
