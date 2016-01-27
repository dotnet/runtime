// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Deletes the specified empty directory.
        /// </summary>
        /// <param name="path">The path of the directory to delete</param>
        /// <returns>
        /// Returns 0 on success; otherwise, returns -1
        /// </returns>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_RmDir", SetLastError = true)]
        internal static extern int RmDir(string path);
    }
}
