// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        /// <summary>
        /// Returns the full path to the executable for the current process, resolving symbolic links.
        /// </summary>
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetProcessPath", SetLastError = true)]
        internal static extern string? GetProcessPath();
    }
}
