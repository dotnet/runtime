// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        private IntrinsicInvokeHelper.InvokeState _invokeState;

        internal unsafe ConstructorInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _invokeFunc_RefArgs = InvokeWithSharedThunk;
        }

        private unsafe object? InvokeWithSharedThunk(object? obj, IntPtr* args) =>
            IntrinsicInvokeHelper.Invoke(ref _invokeState, ref _strategy, ref _invokeFunc_RefArgs,
                _method, _argTypes, obj, args, backwardsCompat: false);
    }
}
