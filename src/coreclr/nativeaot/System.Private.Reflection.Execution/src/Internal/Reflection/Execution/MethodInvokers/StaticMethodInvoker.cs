// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Threading;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;
using global::Internal.Reflection.Execution;
using global::Internal.Reflection.Core.Execution;

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

        [DebuggerGuidedStepThroughAttribute]
        protected sealed override object Invoke(object thisObject, object[] arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            object result = RuntimeAugments.CallDynamicInvokeMethod(
                thisObject,
                MethodInvokeInfo.LdFtnResult,
                MethodInvokeInfo.DynamicInvokeMethod,
                MethodInvokeInfo.DynamicInvokeGenericDictionary,
                MethodInvokeInfo.MethodInfo,
                arguments,
                binderBundle,
                wrapInTargetInvocationException: wrapInTargetInvocationException,
                methodToCallIsThisCall: false);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public sealed override IntPtr LdFtnResult => MethodInvokeInfo.LdFtnResult;
    }
}
