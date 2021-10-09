// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    /// <summary>
    /// Invokes the object. If it falls back, just produce an error.
    /// </summary>
    internal sealed class ComInvokeAction : InvokeBinder
    {
        [RequiresUnreferencedCode(Binder.TrimmerWarning)]
        internal ComInvokeAction(CallInfo callInfo)
            : base(callInfo)
        {
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. Constructors are marked as such.")]
        public override DynamicMetaObject FallbackInvoke(DynamicMetaObject target, DynamicMetaObject[] args, DynamicMetaObject errorSuggestion)
        {
            if (ComBinder.TryBindInvoke(this, target, args, out DynamicMetaObject res))
            {
                return res;
            }

            return errorSuggestion ?? new DynamicMetaObject(
                Expression.Throw(
                    Expression.New(
                        typeof(NotSupportedException).GetConstructor(new[] { typeof(string) }),
                        Expression.Constant(SR.COMCannotPerformCall)
                    ),
                    typeof(object)
                ),
                target.Restrictions.Merge(BindingRestrictions.Combine(args))
            );
        }
    }

    /// <summary>
    /// Splats the arguments to another nested dynamic site, which does the
    /// real invocation of the IDynamicMetaObjectProvider.
    /// </summary>
    internal sealed class SplatInvokeBinder : CallSiteBinder
    {
        private static readonly SplatInvokeBinder s_instance = new SplatInvokeBinder();

        internal static SplatInvokeBinder Instance
        {
            [RequiresUnreferencedCode(Binder.TrimmerWarning)]
            get => s_instance;
        }

        private SplatInvokeBinder() { }

        // Just splat the args and dispatch through a nested site
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
            Justification = "This whole class is unsafe. The only entry-point to this class is through Instance property which is marked as unsafe.")]
        public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
        {
            Debug.Assert(args.Length == 2);

            int count = ((object[])args[1]).Length;
            ParameterExpression array = parameters[1];

            var nestedArgs = new ReadOnlyCollectionBuilder<Expression>(count + 1);
            var delegateArgs = new Type[count + 3]; // args + target + returnType + CallSite
            nestedArgs.Add(parameters[0]);
            delegateArgs[0] = typeof(CallSite);
            delegateArgs[1] = typeof(object);
            for (int i = 0; i < count; i++)
            {
                nestedArgs.Add(Expression.ArrayAccess(array, Expression.Constant(i)));
                delegateArgs[i + 2] = typeof(object).MakeByRefType();
            }
            delegateArgs[delegateArgs.Length - 1] = typeof(object);

            return Expression.IfThen(
                Expression.Equal(Expression.ArrayLength(array), Expression.Constant(count)),
                Expression.Return(
                    returnLabel,
                    Expression.MakeDynamic(
                        Expression.GetDelegateType(delegateArgs),
                        new ComInvokeAction(new CallInfo(count)),
                        nestedArgs
                    )
                )
            );
        }
    }
}
