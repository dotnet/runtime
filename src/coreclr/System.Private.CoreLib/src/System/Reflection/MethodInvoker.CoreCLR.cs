// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection.Emit;

namespace System.Reflection
{
    public partial class MethodInvoker
    {
        private readonly Signature? _signature;

        private unsafe MethodInvoker(RuntimeMethodInfo method) : this(method, method.Signature.Arguments)
        {
            _signature = method.Signature;
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
            _invocationFlags = method.ComputeAndUpdateInvocationFlags();
        }

        private unsafe MethodInvoker(DynamicMethod method) : this(method, method.Signature.Arguments)
        {
            _signature = method.Signature;
            _invokeFunc_RefArgs = InterpretedInvoke_Method;
            // No _invocationFlags for DynamicMethod.
        }

        private unsafe MethodInvoker(RuntimeConstructorInfo constructor) : this(constructor, constructor.Signature.Arguments)
        {
            _signature = constructor.Signature;
            _invokeFunc_RefArgs = InterpretedInvoke_Constructor;
            _invocationFlags = constructor.ComputeAndUpdateInvocationFlags();
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature!, isConstructor: false);

        private unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _signature!, isConstructor: obj is null);
    }
}
