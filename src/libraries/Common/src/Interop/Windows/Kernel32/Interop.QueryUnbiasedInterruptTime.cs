// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // The actual native signature is:
        //      BOOL WINAPI QueryUnbiasedInterruptTime(
        //          _Out_ PULONGLONG UnbiasedTime
        //      );
        //
        // We take a ulong* (rather than a out ulong) to avoid the pinning overhead.
        // We don't set last error since we don't need the extended error info.

        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        internal static unsafe partial BOOL QueryUnbiasedInterruptTime(ulong* unbiasedTime);
    }
}
