// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Userenv
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Userenv, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool CreateEnvironmentBlock(
            out char* environment,
            SafeTokenHandle token,
            [MarshalAs(UnmanagedType.Bool)] bool inherit);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Userenv)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool DestroyEnvironmentBlock(char* environment);
    }
}
