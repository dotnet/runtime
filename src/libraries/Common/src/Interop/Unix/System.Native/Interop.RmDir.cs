// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_RmDir", CharSet = CharSet.Ansi, SetLastError = true)]
        internal static partial int RmDir(string path);
    }
}
