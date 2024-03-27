// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Runtime.General;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;

namespace Internal.Reflection.Core.Execution
{
    //
    // This class polymorphically implements the MethodBase.Invoke() api and its close cousins. MethodBaseInvokers are designed to be built once and cached
    // for maximum Invoke() throughput.
    //
    public abstract class MethodBaseInvoker
    {
        protected MethodBaseInvoker() { }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object thisObject, object?[] arguments, Binder? binder, BindingFlags invokeAttr, CultureInfo? cultureInfo)
        {
            BinderBundle binderBundle = binder.ToBinderBundle(invokeAttr, cultureInfo);
            bool wrapInTargetInvocationException = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
            object? result = Invoke(thisObject, arguments, binderBundle, wrapInTargetInvocationException);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        public object CreateInstance(object?[] arguments, Binder? binder, BindingFlags invokeAttr, CultureInfo? cultureInfo)
        {
            BinderBundle binderBundle = binder.ToBinderBundle(invokeAttr, cultureInfo);
            bool wrapInTargetInvocationException = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
            object result = CreateInstance(arguments, binderBundle, wrapInTargetInvocationException);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        protected abstract object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException);
        protected abstract object CreateInstance(object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException);
        protected internal abstract object CreateInstance(Span<object?> arguments);
        protected internal abstract object CreateInstanceWithFewArgs(Span<object?> arguments);
        public abstract Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen);
        protected internal abstract object? Invoke(object? thisObject, Span<object?> arguments);
        protected internal abstract object? InvokeDirectWithFewArgs(object? thisObject, Span<object?> arguments);

        // This property is used to retrieve the target method pointer. It is used by the RuntimeMethodHandle.GetFunctionPointer API
        public abstract IntPtr LdFtnResult { get; }

        protected static void ValidateThis(object thisObject, RuntimeTypeHandle declaringTypeHandle)
        {
            if (thisObject == null)
                throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);

            if (!RuntimeAugments.IsAssignable(thisObject, declaringTypeHandle))
                throw new TargetException(SR.Format(SR.RFLCT_Targ_ITargMismatch_WithType, declaringTypeHandle.GetRuntimeTypeInfoForRuntimeTypeHandle(), thisObject.GetType()));
        }
    }
}
