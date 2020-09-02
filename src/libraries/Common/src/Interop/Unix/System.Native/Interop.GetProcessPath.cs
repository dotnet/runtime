// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Takes a path containing relative subpaths or links and returns the absolute path.
        /// This function works on both files and folders and returns a null-terminated string.
        /// </summary>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetProcessPath", SetLastError = true)]
        internal static extern string? GetProcessPath();
    }
}
