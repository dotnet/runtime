// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        private readonly Signature? _signature;

        internal unsafe MethodBaseInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _signature = method.Signature;
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        internal unsafe MethodBaseInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _signature = constructor.Signature;
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
            _invokeFunc_RefArgs = InterpretedInvoke_Constructor;
        }

        internal unsafe MethodBaseInvoker(DynamicMethod method, Signature signature) : this(method, signature.Arguments)
        {
            _signature = signature;
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
        }

        private unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature!, isConstructor: obj is null);

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature!, isConstructor: false);
    }
}
