// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal const ulong RLIM_INFINITY = ulong.MaxValue;

        internal enum RlimitResources : int
        {
            RLIMIT_CPU      = 0,        // CPU limit in seconds
            RLIMIT_FSIZE    = 1,        // Largest file that can be created, in bytes
            RLIMIT_DATA     = 2,        // Maximum size of data segment, in bytes
            RLIMIT_STACK    = 3,        // Maximum size of stack segment, in bytes
            RLIMIT_CORE     = 4,        // Largest core file that can be created, in bytes
            RLIMIT_AS       = 5,        // Address space limit
            RLIMIT_RSS      = 6,        // Largest resident set size, in bytes
            RLIMIT_MEMLOCK  = 7,        // Locked-in-memory address space
            RLIMIT_NPROC    = 8,        // Number of processes
            RLIMIT_NOFILE   = 9,        // Number of open files
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RLimit
        {
            internal ulong CurrentLimit;
            internal ulong MaximumLimit;
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetRLimit", SetLastError = true)]
        internal static extern int GetRLimit(RlimitResources resourceType, out RLimit limits);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetRLimit", SetLastError = true)]
        internal static extern int SetRLimit(RlimitResources resourceType, ref RLimit limits);
    }
}
