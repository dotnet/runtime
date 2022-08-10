// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Threading;
using global::System.Reflection;
using System.Runtime.InteropServices;
using global::System.Diagnostics;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;
using global::Internal.Reflection.Execution;
using global::Internal.Reflection.Core.Execution;
using global::Internal.Runtime.CompilerServices;

namespace Internal.Reflection.Execution.MethodInvokers
{
    //
    // Implements Invoke() for non-virtual instance methods.
    //
    internal sealed class InstanceMethodInvoker : MethodInvokerWithMethodInvokeInfo
    {
        public InstanceMethodInvoker(MethodInvokeInfo methodInvokeInfo, RuntimeTypeHandle declaringTypeHandle)
            : base(methodInvokeInfo)
        {
            _declaringTypeHandle = declaringTypeHandle;
        }

        [DebuggerGuidedStepThroughAttribute]
        protected sealed override object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            if (MethodInvokeInfo.IsSupportedSignature) // Workaround to match expected argument validation order
            {
                ValidateThis(thisObject, _declaringTypeHandle);
            }

            object? result = MethodInvokeInfo.Invoke(
                thisObject,
                MethodInvokeInfo.LdFtnResult,
                arguments,
                binderBundle,
                wrapInTargetInvocationException);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        public sealed override Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen)
        {
            if (isOpen)
            {
                MethodInfo methodInfo = (MethodInfo)MethodInvokeInfo.Method;

                short resolveType = OpenMethodResolver.OpenNonVirtualResolve;

                if (methodInfo.DeclaringType.IsValueType && !methodInfo.IsStatic)
                {
                    // Open instance method for valuetype
                    resolveType = OpenMethodResolver.OpenNonVirtualResolveLookthruUnboxing;
                }

                return RuntimeAugments.CreateDelegate(
                    delegateType,
                    new OpenMethodResolver(_declaringTypeHandle, MethodInvokeInfo.LdFtnResult, default(GCHandle), 0, resolveType).ToIntPtr(),
                    target,
                    isStatic: isStatic,
                    isOpen: isOpen);
            }
            else
            {
                return base.CreateDelegate(delegateType, target, isStatic, isVirtual, isOpen);
            }
        }

        public sealed override IntPtr LdFtnResult => MethodInvokeInfo.LdFtnResult;

        private RuntimeTypeHandle _declaringTypeHandle;
    }
}
