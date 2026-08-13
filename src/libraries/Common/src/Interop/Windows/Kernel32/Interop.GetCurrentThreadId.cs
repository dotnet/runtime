// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        /// <safety>P/Invoke to the OS that returns the current thread's numeric id by value; it takes no arguments and reads or writes no caller-supplied memory.</safety>
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.Kernel32)]
        [SuppressGCTransition]
        public static safe partial int GetCurrentThreadId();
    }
}
