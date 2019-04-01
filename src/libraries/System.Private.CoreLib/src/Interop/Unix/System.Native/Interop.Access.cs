// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
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

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Access", SetLastError = true)]
        internal static extern int Access(string path, AccessMode mode);
    }
}
