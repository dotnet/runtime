// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static unsafe partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PThreadMutex_Init", SetLastError = true)]
        internal static partial int PThreadMutex_Init(void* mutex);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PThreadMutex_Acquire", SetLastError = true)]
        internal static partial int PThreadMutex_Acquire(void* mutex, int timeoutMilliseconds);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PThreadMutex_Release", SetLastError = true)]
        internal static partial int PThreadMutex_Release(void* mutex);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PThreadMutex_Size")]
        [SuppressGCTransition]
        internal static partial int PThreadMutex_Size();
    }
}
