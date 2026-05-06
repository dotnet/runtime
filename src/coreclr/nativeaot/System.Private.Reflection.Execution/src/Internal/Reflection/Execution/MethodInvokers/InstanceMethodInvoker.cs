// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using global::Internal.Runtime.Augments;
using global::Internal.Runtime.CompilerServices;
using global::System;
using global::System.Diagnostics;
using global::System.Reflection;

namespace Internal.Reflection.Execution.MethodInvokers
{
    //
    // Implements Invoke() for non-virtual instance methods.
    //
    internal sealed unsafe class InstanceMethodInvoker : MethodInvokerWithMethodInvokeInfo
    {
        public InstanceMethodInvoker(MethodInvokeInfo methodInvokeInfo, RuntimeTypeHandle declaringTypeHandle)
            : base(methodInvokeInfo)
        {
            _declaringTypeHandle = declaringTypeHandle;

            if (methodInvokeInfo.Method.IsConstructor && !methodInvokeInfo.Method.IsStatic)
            {
                if (RuntimeAugments.IsByRefLike(declaringTypeHandle))
                {
                    _allocatorMethod = &ThrowTargetException;
                }
                else
                {
                    _allocatorMethod = (delegate*<nint, object>)RuntimeAugments.GetAllocateObjectHelperForType(declaringTypeHandle);
                }
            }
        }

        private static object ThrowTargetException(IntPtr _)
        {
            throw new TargetException();
        }

        [DebuggerGuidedStepThrough]
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
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        protected sealed override object? Invoke(object? thisObject, Span<object?> arguments)
        {
            if (MethodInvokeInfo.IsSupportedSignature) // Workaround to match expected argument validation order
            {
                ValidateThis(thisObject, _declaringTypeHandle);
            }

            object? result = MethodInvokeInfo.Invoke(
                thisObject,
                MethodInvokeInfo.LdFtnResult,
                arguments);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThrough]
        protected sealed override object? InvokeDirectWithFewArgs(object? thisObject, Span<object?> arguments)
        {
            if (MethodInvokeInfo.IsSupportedSignature) // Workaround to match expected argument validation order
            {
                ValidateThis(thisObject, _declaringTypeHandle);
            }

            object? result = MethodInvokeInfo.InvokeDirectWithFewArgs(
                thisObject,
                MethodInvokeInfo.LdFtnResult,
                arguments);
            DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return result;
        }

        [DebuggerGuidedStepThroughAttribute]
        protected sealed override object CreateInstance(object[] arguments, BinderBundle binderBundle, bool wrapInTargetInvocationException)
        {
            object thisObject = RawCalliHelper.Call<object>(_allocatorMethod, _declaringTypeHandle.Value);
            MethodInvokeInfo.Invoke(
                thisObject,
                MethodInvokeInfo.LdFtnResult,
                arguments,
                binderBundle,
                wrapInTargetInvocationException);
            System.Diagnostics.DebugAnnotations.PreviousCallContainsDebuggerStepInCode();
            return thisObject;
        }

        protected sealed override object CreateInstance(Span<object?> arguments)
        {
            object thisObject = RawCalliHelper.Call(_allocatorMethod, _declaringTypeHandle.Value);
            Invoke(thisObject, arguments);
            return thisObject;
        }

        protected sealed override object CreateInstanceWithFewArgs(Span<object?> arguments)
        {
            object thisObject = RawCalliHelper.Call(_allocatorMethod, _declaringTypeHandle.Value);
            InvokeDirectWithFewArgs(thisObject, arguments);
            return thisObject;
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
        private delegate*<nint, object> _allocatorMethod;

        private static class RawCalliHelper
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T Call<T>(delegate*<IntPtr, T> pfn, IntPtr arg)
            => pfn(arg);
        }
    }
}
