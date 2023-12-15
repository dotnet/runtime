// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
    public partial struct GCHandle
    {
        internal static IntPtr InternalAlloc(object? value, GCHandleType type)
        {
            IntPtr handle = _InternalAlloc(value, type);
            if (handle == 0)
                handle = InternalAllocWithGCTransition(value, type);
            return handle;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr _InternalAlloc(object? value, GCHandleType type);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static IntPtr InternalAllocWithGCTransition(object? value, GCHandleType type)
            => _InternalAllocWithGCTransition(ObjectHandleOnStack.Create(ref value), type);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCHandle_InternalAllocWithGCTransition")]
        private static partial IntPtr _InternalAllocWithGCTransition(ObjectHandleOnStack value, GCHandleType type);

        internal static void InternalFree(IntPtr handle)
        {
            if (!_InternalFree(handle))
                InternalFreeWithGCTransition(handle);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern bool _InternalFree(IntPtr handle);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void InternalFreeWithGCTransition(IntPtr dependentHandle)
            => _InternalFreeWithGCTransition(dependentHandle);

        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "GCHandle_InternalFreeWithGCTransition")]
        private static partial void _InternalFreeWithGCTransition(IntPtr dependentHandle);

#if DEBUG
        // The runtime performs additional checks in debug builds
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalGet(IntPtr handle);
#else
#pragma warning disable 8500 // address of managed types
        internal static unsafe object? InternalGet(IntPtr handle) => *(object*)handle;
#pragma warning restore 8500
#endif

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern void InternalSet(IntPtr handle, object? value);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? InternalCompareExchange(IntPtr handle, object? value, object? oldValue);
    }
}
