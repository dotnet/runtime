// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private IntrinsicInvokeHelper.InvokeState _invokeState;

        private unsafe MethodInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
        }

        private unsafe MethodInvoker(DynamicMethod method) : this(method, method.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
            // No _invocationFlags for DynamicMethod.
        }

        private unsafe MethodInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _needsByRefStrategy = true;
        }

        private unsafe object? InvokeWithSharedThunk(object? obj, IntPtr* args) =>
            IntrinsicInvokeHelper.Invoke(ref _invokeState, ref _strategy, ref _invokeFunc_RefArgs,
                _method, _argTypes, obj, args, backwardsCompat: false);
    }
}
