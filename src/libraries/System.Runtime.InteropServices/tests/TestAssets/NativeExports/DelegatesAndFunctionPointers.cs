// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace NativeExports
{
    public static unsafe class DelegatesAndFunctionPointers
    {
        [UnmanagedCallersOnly(EntryPoint = "invoke_callback_after_gc")]
        public static void InvokeCallbackAfterGCCollect(delegate* unmanaged<void> fptr)
        {
            // We are at the mercy of the GC to verify our delegate has been retain
            // across the native function call. This is a best effort validation.
            for (int i = 0; i < 5; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // If the corresponding Delegate was collected, the runtime will rudely abort.
            fptr();
        }

        [UnmanagedCallersOnly(EntryPoint = "invoke_managed_callback_after_gc")]
        public static void InvokeManagedCallbackAfterGCCollect(void* fptr)
        {
            // We are at the mercy of the GC to verify our delegate has been retain
            // across the native function call. This is a best effort validation.
            for (int i = 0; i < 5; ++i)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            // If the corresponding Delegate was collected, the runtime will rudely abort.
            ((delegate*<void>)fptr)();
        }

        [UnmanagedCallersOnly(EntryPoint = "invoke_callback_blittable_args")]
        public static int InvokeCallbackWithBlittableArgument(delegate* unmanaged<int, int, int> fptr, int a, int b)
        {
            return fptr(a, b);
        }

        [UnmanagedCallersOnly(EntryPoint = "invoke_managed_callback_blittable_args")]
        public static int InvokeManagedCallbackWithBlittableArgument(void* fptr, int a, int b)
        {
            return ((delegate*<int, int, int>)fptr)(a, b);
        }
    }
}
