// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Access", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static partial int Access(string path, AccessMode mode);
    }
}
