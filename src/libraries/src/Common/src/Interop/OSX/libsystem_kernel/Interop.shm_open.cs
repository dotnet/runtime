// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using mode_t  = System.Int32;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.LibSystemKernel, SetLastError = true)]
        internal static extern int shm_open(string name, OpenFlags flags, mode_t mode);

        [DllImport(Libraries.LibSystemKernel, SetLastError = true)]
        internal static extern int shm_unlink(string name);
    }
}
