// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern IntPtr opendir(string name); // opendir/readdir/closedir defined in terms of IntPtr so it may be used in iterators (which don't allow unsafe code)
    }
}
