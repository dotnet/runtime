// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern int access(string path, AccessModes amode);

        [Flags]
        internal enum AccessModes 
        {
            F_OK = 0,
            X_OK = 1,
            W_OK = 2,
            R_OK = 4
        }
    }
}
