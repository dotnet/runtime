// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using rlim_t = System.Int64;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int getrlimit(int resource, out rlimit rlim);

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int setrlimit(int resource, ref rlimit rlim);

        internal struct rlimit
        {
            internal rlim_t rlim_cur;
            internal rlim_t rlim_max;
        };
    }
}
