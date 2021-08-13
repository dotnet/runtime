// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

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

        [DllImport(Libraries.Kernel32, SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool EscapeCommFunction(
            SafeFileHandle hFile,
            int dwFunc);
    }
}
