// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;
using static System.RuntimeType;

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        private readonly CreateUninitializedCache? _allocator;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateUninitializedObject() => _allocator!.CreateUninitializedObject(_declaringType);

        private bool ShouldAllocate => _allocator is not null;

        private unsafe Delegate CreateInvokeDelegateForInterpreted()
        {
            Debug.Assert(MethodInvokerCommon.UseInterpretedPath);
            Debug.Assert(_strategy == InvokerStrategy.Ref4 || _strategy == InvokerStrategy.RefMany);

            return (InvokeFunc_RefArgs)InterpretedInvoke;
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr _, IntPtr* args)
        {
            return RuntimeMethodHandle.InvokeMethod(obj, (void**)args, _method.Signature, isConstructor: obj is null);
        }
    }
}
