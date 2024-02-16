// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static class CommFunctions
        {
            internal const int SETRTS = 3;       // Set RTS high
            internal const int CLRRTS = 4;       // Set RTS low
            internal const int SETDTR = 5;       // Set DTR high
            internal const int CLRDTR = 6;
        }

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool EscapeCommFunction(
            SafeFileHandle hFile,
            int dwFunc);
    }
}
