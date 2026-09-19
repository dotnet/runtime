// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        private IntrinsicInvokeHelper.InvokeState _invokeState;

        internal unsafe MethodBaseInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
        }

        internal unsafe MethodBaseInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
        }

        internal unsafe MethodBaseInvoker(DynamicMethod method, Signature signature) : this(method, signature.Arguments)
        {
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
        }

        private unsafe object? InvokeWithSharedThunk(object? obj, IntPtr* args) =>
            IntrinsicInvokeHelper.Invoke(ref _invokeState, ref _strategy, ref _invokeFunc_RefArgs,
                _method, _argTypes, obj, args, backwardsCompat: true);

        internal unsafe object? InvokeDirectByRef(object? obj, IntPtr* args)
        {
            if ((_strategy & MethodBase.InvokerStrategy.StrategyDetermined_RefArgs) == 0)
            {
                MethodInvokerCommon.DetermineStrategy_RefArgs(ref _strategy, ref _invokeFunc_RefArgs, _method, backwardsCompat: true);
            }

            return _invokeFunc_RefArgs!(obj, args);
        }
    }
}
