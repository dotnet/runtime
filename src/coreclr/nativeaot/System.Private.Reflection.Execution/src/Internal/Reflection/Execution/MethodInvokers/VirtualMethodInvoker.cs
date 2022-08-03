// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using global::System;
using global::System.Threading;
using global::System.Reflection;
using global::System.Diagnostics;
using global::System.Collections.Generic;

using global::Internal.Runtime.Augments;
using global::Internal.Runtime.CompilerServices;
using global::Internal.Reflection.Execution;
using global::Internal.Reflection.Core.Execution;

namespace Internal.Reflection.Execution.MethodInvokers
{
    //
    // Implements Invoke() for virtual methods on interfaces.
    //
    internal sealed class VirtualMethodInvoker : MethodInvokerWithMethodInvokeInfo
    {
        public VirtualMethodInvoker(MethodInvokeInfo methodInvokeInfo, RuntimeTypeHandle declaringTypeHandle)
            : base(methodInvokeInfo)
        {
            _declaringTypeHandle = declaringTypeHandle;
        }

        public sealed override Delegate CreateDelegate(RuntimeTypeHandle delegateType, object target, bool isStatic, bool isVirtual, bool isOpen)
        {
            if (!isOpen)
            {
                // We're creating a delegate to a virtual override of this method, so resolve the virtual now.
                IntPtr resolvedVirtual = OpenMethodResolver.ResolveMethod(MethodInvokeInfo.VirtualResolveData, target);
                return RuntimeAugments.CreateDelegate(
                                delegateType,
                                resolvedVirtual,
                                target,
                                isStatic: false,
                                isOpen: isOpen);
            }
            else
            {
                // Create an open virtual method by providing the virtual resolver to the delegate type.
                return RuntimeAugments.CreateDelegate(
                    delegateType,
                    MethodInvokeInfo.VirtualResolveData,
                    target,
                    isStatic: false,
                    isOpen: isOpen);
            }
        }

        [DebuggerGuidedStepThroughAttribute]
        protected sealed override object? Invoke(object? thisObject, object?[]? arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            IntPtr resolvedVirtual = IntPtr.Zero;

            if (MethodInvokeInfo.IsSupportedSignature) // Workaround to match expected argument validation order
            {
                ValidateThis(thisObject, _declaringTypeHandle);

                resolvedVirtual = OpenMethodResolver.ResolveMethod(MethodInvokeInfo.VirtualResolveData, thisObject);
            }

            object? result = MethodInvokeInfo.Invoke(
                thisObject,
                resolvedVirtual,
                arguments,
                binderBundle,
                wrapInTargetInvocationException);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        internal IntPtr ResolveTarget(RuntimeTypeHandle type)
        {
            return OpenMethodResolver.ResolveMethod(MethodInvokeInfo.VirtualResolveData, type);
        }

        // On CoreCLR/Desktop, we do not attempt to resolve the target virtual method based on the type of the 'this' pointer.
        // For compatibility reasons, we'll do the same here.
        public sealed override IntPtr LdFtnResult
        {
            get
            {
                if (RuntimeAugments.IsInterface(_declaringTypeHandle))
                    throw new PlatformNotSupportedException();

                // Must be an abstract method
                if (MethodInvokeInfo.LdFtnResult == IntPtr.Zero && MethodInvokeInfo.VirtualResolveData != IntPtr.Zero)
                    throw new PlatformNotSupportedException();

                return MethodInvokeInfo.LdFtnResult;
            }
        }

        private RuntimeTypeHandle _declaringTypeHandle;
    }
}
