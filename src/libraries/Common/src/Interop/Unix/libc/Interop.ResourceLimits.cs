// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        // Make the rlimit resource values a strongly-typed enum for easier use
        internal enum RLIMIT_Resources : int
        {
            RLIMIT_CPU          = 0,
            RLIMIT_FSIZE        = 1,
            RLIMIT_DATA         = 2,
            RLIMIT_STACK        = 3,
            RLIMIT_CORE         = 4,
            RLIMIT_AS           = 5,
            RLIMIT_RSS          = RLIMIT_AS,
            RLIMIT_MEMLOCK      = 6,
            RLIMIT_NPROC        = 7,
            RLIMIT_NOFILE       = 8,
            RLIM_NLIMITS        = 9,
            _RLIMIT_POSIX_FLAG  = 0x1000,
        }

        // from sys\resource.h
        [StructLayout(LayoutKind.Sequential)]
        internal struct rlimit
        {
            internal ulong rlim_cur;
            internal ulong rlim_max;
        }

        /// <summary>
        /// Gets the resource limits for the current process
        /// </summary>
        /// <param name="resource">The type of resource limit to query for</param>
        /// <param name="info">The limit values</param>
        /// <returns>Returns 0 on success; on failure, -1 is returned and errno is set to the OS-specific error code</returns>
        [DllImport(Interop.Libraries.Libc)]
        private static extern int getrlimit(
            RLIMIT_Resources    resource, 
            ref rlimit          info);

        /// <summary>
        /// Sets the resource limits for the current process
        /// </summary>
        /// <param name="resource">The type of resource to limit</param>
        /// <param name="info">The new maximum values for the limit</param>
        /// <returns>Returns 0 on success; otherwise, returns -1 and sets errno to the platform-specific error code</returns>
        [DllImport(Interop.Libraries.Libc)]
        internal static extern int setrlimit(
            RLIMIT_Resources    resource,
            ref rlimit          info);

        /// <summary>
        /// Gets the resource limits for the current process
        /// </summary>
        /// <param name="resource">The type of resource limit to query for</param>
        /// <returns>Returns the rlimit values for the specific resource</returns>
        internal static rlimit getrlimit(RLIMIT_Resources resource)
        {
            rlimit info = new rlimit();
            int result = getrlimit(resource, ref info);
            if (result < 0)
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), SR.ResourceLimitQueryFailure);
            }

            return info;
        }

    }
}