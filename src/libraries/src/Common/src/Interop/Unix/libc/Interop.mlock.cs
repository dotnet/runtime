// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using size_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int mlock(IntPtr addr, size_t len);

        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int munlock(IntPtr addr, size_t len);
    }
}
