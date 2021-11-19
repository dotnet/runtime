// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // The actual native signature is:
        //      BOOL WINAPI QueryPerformanceCounter(
        //          _Out_ LARGE_INTEGER* lpPerformanceCount
        //      );
        //
        // We take a long* (rather than a out long) to avoid the pinning overhead.
        // We don't set last error since we don't need the extended error info.

        [GeneratedDllImport(Libraries.Kernel32, ExactSpelling = true)]
        [SuppressGCTransition]
        internal static unsafe partial BOOL QueryPerformanceCounter(long* lpPerformanceCount);
    }
}
