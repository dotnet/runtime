// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;
using static System.RuntimeType;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
    {
        private readonly CreateUninitializedCache? _allocator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateUninitializedObject() => _allocator!.CreateUninitializedObject(_declaringType);

        private bool ShouldAllocate => _allocator is not null;

        private unsafe Delegate CreateInvokeDelegateForInterpreted()
        {
            Debug.Assert(MethodInvokerCommon.UseInterpretedPath);
            Debug.Assert(_strategy == InvokerStrategy.Ref4 || _strategy == InvokerStrategy.RefMany);

            if (_method is RuntimeMethodInfo)
            {
                return (InvokeFunc_RefArgs)InterpretedInvoke_Method;
            }

            if (_method is RuntimeConstructorInfo)
            {
                return (InvokeFunc_RefArgs)InterpretedInvoke_Constructor;
            }

            Debug.Assert(_method is DynamicMethod);
            return (InvokeFunc_RefArgs)InterpretedInvoke_DynamicMethod;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr _, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, ((RuntimeMethodInfo)_method).Signature, isConstructor: false);

        private unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr _, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, ((RuntimeConstructorInfo)_method).Signature, isConstructor: obj is null);

        private unsafe object? InterpretedInvoke_DynamicMethod(object? obj, IntPtr _, IntPtr* args) =>
            RuntimeMethodHandle.InvokeMethod(obj, (void**)args, ((DynamicMethod)_method).Signature, isConstructor: false);
    }
}
