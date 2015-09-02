// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        internal enum AccessMode : int
        {
            F_OK = 0,   /* Check for existence */
            X_OK = 1,   /* Check for execute */
            W_OK = 2,   /* Check for write */
            R_OK = 4,   /* Check for read */
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int Access(string path, AccessMode mode);
    }
}
