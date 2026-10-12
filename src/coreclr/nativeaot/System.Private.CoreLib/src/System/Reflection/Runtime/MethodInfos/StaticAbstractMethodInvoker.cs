// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    // A static abstract interface method has no implementation to call: invoking it or binding a delegate to it throws
    // MemberAccessException, as on CoreCLR. Taking its address throws PlatformNotSupportedException, as for other
    // abstract methods.
    internal sealed class StaticAbstractMethodInvoker : MethodBaseInvoker
    {
        private readonly MethodInfo _method;

        public StaticAbstractMethodInvoker(MethodInfo method)
        {
            _method = method;
        }

        private MemberAccessException CreateException()
        {
            return new MemberAccessException(SR.Format(SR.Acc_CallStaticAbstract, _method.DeclaringType, _method.Name));
        }

        protected sealed override object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            throw CreateException();
        }

        protected sealed override object CreateInstance(object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            throw CreateException();
        }

        protected internal sealed override object CreateInstance(Span<object?> arguments)
        {
            throw CreateException();
        }

        protected internal sealed override object CreateInstanceWithFewArgs(Span<object?> arguments)
        {
            throw CreateException();
        }

        protected internal sealed override object? Invoke(object? thisObject, Span<object?> arguments)
        {
            // MethodInvoker.Invoke(object, Span<object>) leaves the argument count check to the invoker, unlike its
            // fixed-arity overloads, which check before calling InvokeDirectWithFewArgs. Check the count here first, as
            // DynamicInvokeInfo does for other methods, so a wrong count throws TargetParameterCountException.
            if (arguments.Length != _method.GetParametersAsSpan().Length)
                throw new TargetParameterCountException(SR.Arg_ParmCnt);

            throw CreateException();
        }

        protected internal sealed override object? InvokeDirectWithFewArgs(object? thisObject, Span<object?> arguments)
        {
            throw CreateException();
        }

        public sealed override Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen)
        {
            throw CreateException();
        }

        public sealed override IntPtr LdFtnResult
        {
            get
            {
                throw new PlatformNotSupportedException();
            }
        }
    }
}
