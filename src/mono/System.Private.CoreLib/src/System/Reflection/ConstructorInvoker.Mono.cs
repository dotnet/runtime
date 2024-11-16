// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    public partial class ConstructorInvoker
    {
        private bool _shouldAllocate;
        private bool ShouldAllocate => _shouldAllocate;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072:UnrecognizedReflectionPattern", Justification = "Internal reflection implementation")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2077:UnrecognizedReflectionPattern", Justification = "Internal reflection implementation")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object CreateUninitializedObject() => RuntimeHelpers.GetUninitializedObject(_declaringType);

        private unsafe Delegate CreateInvokeDelegateForInterpreted()
        {
            Debug.Assert(MethodInvokerCommon.UseInterpretedPath);
            Debug.Assert(_strategy == InvokerStrategy.Ref4 || _strategy == InvokerStrategy.RefMany);
            return (InvokeFunc_RefArgs)InterpretedInvoke;
        }

        private unsafe object? InterpretedInvoke(object? obj, IntPtr _, IntPtr* args)
        {
            object? o = _method.InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}
