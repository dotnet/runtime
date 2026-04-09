// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_Init", SetLastError = true)]
        internal static partial int LowLevelCrossProcessMutex_Init(void* mutex);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_Acquire", SetLastError = true)]
        internal static partial int LowLevelCrossProcessMutex_Acquire(void* mutex, int timeoutMilliseconds);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_Release", SetLastError = true)]
        internal static partial int LowLevelCrossProcessMutex_Release(void* mutex);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_Destroy", SetLastError = true)]
        internal static partial int LowLevelCrossProcessMutex_Destroy(void* mutex);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_Size")]
        [SuppressGCTransition]
        internal static partial int LowLevelCrossProcessMutex_Size();

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_GetOwnerProcessAndThreadId", SetLastError = true)]
        [SuppressGCTransition]
        internal static partial void LowLevelCrossProcessMutex_GetOwnerProcessAndThreadId(void* mutex, out uint processId, out uint threadId);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId", SetLastError = true)]
        [SuppressGCTransition]
        internal static partial void LowLevelCrossProcessMutex_SetOwnerProcessAndThreadId(void* mutex, uint processId, uint threadId);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_IsAbandoned", SetLastError = true)]
        [SuppressGCTransition]
        [return: MarshalAs(UnmanagedType.U1)]
        internal static partial bool LowLevelCrossProcessMutex_IsAbandoned(void* mutex);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_LowLevelCrossProcessMutex_SetAbandoned", SetLastError = true)]
        [SuppressGCTransition]
        internal static partial void LowLevelCrossProcessMutex_SetAbandoned(void* mutex, [MarshalAs(UnmanagedType.U1)] bool abandoned);
    }
}
