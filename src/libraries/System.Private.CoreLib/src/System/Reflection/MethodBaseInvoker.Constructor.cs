// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Reflection.MethodBase;

namespace System.Reflection
{
    internal sealed partial class MethodBaseInvoker
    {
        // The rarely used scenario of calling the constructor on an existing instance.
        internal unsafe object? InvokeConstructorWithoutAlloc(
            object? obj,
            BindingFlags invokeAttr,
            Binder? binder,
            object?[] parameters,
            CultureInfo? culture)
        {
            bool wrapInTargetInvocationException = (invokeAttr & BindingFlags.DoNotWrapExceptions) == 0;
            object? ret;
            int argCount = _argCount;

            scoped Span<bool> shouldCopyBack = stackalloc bool[argCount];
            IntPtr* pStorage = stackalloc IntPtr[2 * argCount];
            NativeMemory.Clear(pStorage, (nuint)(2 * argCount) * (nuint)sizeof(IntPtr));
            Span<object?> copyOfArgs = new(ref Unsafe.AsRef<object?>(pStorage), argCount);
            GCFrameRegistration regArgStorage = new((void**)pStorage, (uint)argCount, areByRefs: false);
            IntPtr* pByRefStorage = pStorage + argCount;
            GCFrameRegistration regByRefStorage = new((void**)pByRefStorage, (uint)argCount, areByRefs: true);

            try
            {
                GCFrameRegistration.RegisterForGCReporting(&regArgStorage);
                GCFrameRegistration.RegisterForGCReporting(&regByRefStorage);

                CheckArguments(parameters, copyOfArgs, shouldCopyBack, binder, culture, invokeAttr);

                for (int i = 0; i < argCount; i++)
                {
                    *(ByReference*)(pByRefStorage + i) = (_invokerArgFlags[i] & InvokerArgFlags.IsValueType) != 0 ?
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i).GetRawData()) :
                        ByReference.Create(ref Unsafe.AsRef<object>(pStorage + i));
                }

                try
                {
                    // Use the interpreted version to avoid having to generate a new method that doesn't allocate.
                    ret = InterpretedInvoke_Constructor(obj, pByRefStorage);
                }
                catch (Exception e) when (wrapInTargetInvocationException)
                {
                    throw new TargetInvocationException(e);
                }

                CopyBack(parameters, copyOfArgs, shouldCopyBack);
                return ret;
            }
            finally
            {
                GCFrameRegistration.UnregisterForGCReporting(&regByRefStorage);
                GCFrameRegistration.UnregisterForGCReporting(&regArgStorage);
            }
        }

        internal unsafe object? InvokeConstructorWithoutAlloc(object? obj, bool wrapInTargetInvocationException)
        {
            try
            {
                // Use the interpreted version to avoid having to generate a new method that doesn't allocate.
                return InterpretedInvoke_Constructor(obj, null);
            }
            catch (Exception e) when (wrapInTargetInvocationException)
            {
                throw new TargetInvocationException(e);
            }
        }
    }
}
