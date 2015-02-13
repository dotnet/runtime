// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

using size_t  = System.IntPtr;
using ssize_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern unsafe ssize_t readlink(string path, byte[] buf, size_t bufsize);
    }
}
