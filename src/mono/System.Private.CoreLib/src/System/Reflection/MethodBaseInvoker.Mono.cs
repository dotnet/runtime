// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using static System.Reflection.InvokerEmitUtil;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal partial class MethodBaseInvoker
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

            if (_method is RuntimeMethodInfo)
            {
                return (InvokeFunc_RefArgs)InterpretedInvoke_Method;
            }

            Debug.Assert(_method is RuntimeConstructorInfo);
            return (InvokeFunc_RefArgs)InterpretedInvoke_Constructor;
        }

        private unsafe object? InterpretedInvoke_Method(object? obj, IntPtr _, IntPtr* args)
        {
            object? o = ((RuntimeMethodInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return o;
        }

        internal unsafe object? InterpretedInvoke_Constructor(object? obj, IntPtr _, IntPtr* args)
        {
            object? o = ((RuntimeConstructorInfo)_method).InternalInvoke(obj, args, out Exception? exc);

            if (exc != null)
                throw exc;

            return obj == null ? o : null;
        }
    }
}
