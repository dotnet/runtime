// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using System.Reflection.Runtime.General;
using System.Runtime.CompilerServices;

using Internal.Runtime.Augments;

namespace Internal.Reflection.Core.Execution
{
    //
    // This class polymorphically implements the MethodBase.Invoke() api and its close cousins. MethodInvokers are designed to be built once and cached
    // for maximum Invoke() throughput.
    //
    [ReflectionBlocked]
    public abstract class MethodInvoker
    {
        protected MethodInvoker() { }

        [DebuggerGuidedStepThrough]
        public object? Invoke(object thisObject, object?[] arguments, Binder? binder, BindingFlags invokeAttr, CultureInfo? cultureInfo)
        {
            BinderBundle binderBundle = binder.ToBinderBundle(invokeAttr, cultureInfo);
            bool wrapInTargetInvocationException = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
            object? result = Invoke(thisObject, arguments, binderBundle, wrapInTargetInvocationException);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }
        protected abstract object? Invoke(object? thisObject, object?[] arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException);
        public abstract Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen);

        // This property is used to retrieve the target method pointer. It is used by the RuntimeMethodHandle.GetFunctionPointer API
        public abstract IntPtr LdFtnResult { get; }

        protected static void ValidateThis(object thisObject, RuntimeTypeHandle declaringTypeHandle)
        {
            if (thisObject == null)
                throw new TargetException(SR.RFLCT_Targ_StatMethReqTarg);

            if (RuntimeAugments.IsAssignable(thisObject, declaringTypeHandle))
                return;

            if (RuntimeAugments.IsInterface(declaringTypeHandle))
            {
                if (RuntimeAugments.IsInstanceOfInterface(thisObject, declaringTypeHandle))
                    return;
            }

            throw new TargetException(SR.RFLCT_Targ_ITargMismatch);
        }
    }
}
