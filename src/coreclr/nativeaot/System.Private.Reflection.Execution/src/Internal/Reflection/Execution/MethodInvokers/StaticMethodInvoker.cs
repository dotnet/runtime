// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Diagnostics;
using global::System.Reflection;

namespace Internal.Reflection.Execution.MethodInvokers
{
    //
    // Implements Invoke() for static methods.
    //
    internal sealed class StaticMethodInvoker : MethodInvokerWithMethodInvokeInfo
    {
        public StaticMethodInvoker(MethodInvokeInfo methodInvokeInfo)
            : base(methodInvokeInfo)
        {
        }

        [DebuggerGuidedStepThrough]
        protected sealed override object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            object? result = MethodInvokeInfo.Invoke(
                null, // this pointer is ignored for static methods
                MethodInvokeInfo.LdFtnResult,
                arguments,
                binderBundle,
                wrapInTargetInvocationException);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        protected sealed override object? Invoke(object? thisObject, Span<object?> arguments)
        {
            object? result = MethodInvokeInfo.Invoke(
                null, // this pointer is ignored for static methods
                MethodInvokeInfo.LdFtnResult,
                arguments);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        protected sealed override object? InvokeDirectWithFewArgs(object? thisObject, Span<object?> arguments)
        {
            object? result = MethodInvokeInfo.InvokeDirectWithFewArgs(
                null, // this pointer is ignored for static methods
                MethodInvokeInfo.LdFtnResult,
                arguments);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }


        protected sealed override object CreateInstance(object[] arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            throw NotImplemented.ByDesign;
        }

        protected sealed override object CreateInstance(Span<object?> arguments)
        {
            throw NotImplemented.ByDesign;
        }

        protected sealed override object CreateInstanceWithFewArgs(Span<object?> arguments)
        {
            throw NotImplemented.ByDesign;
        }

        public sealed override IntPtr LdFtnResult => MethodInvokeInfo.LdFtnResult;
    }
}
