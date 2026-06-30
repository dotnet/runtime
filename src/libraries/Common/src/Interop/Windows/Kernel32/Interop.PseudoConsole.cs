// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct PseudoConsoleCoord
        {
            internal short X;
            internal short Y;
        }

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial int CreatePseudoConsole(PseudoConsoleCoord size, SafeHandle hInput, SafeHandle hOutput, uint dwFlags, out IntPtr phPC);

        [LibraryImport(Libraries.Kernel32, SetLastError = true)]
        internal static partial int ResizePseudoConsole(SafeHandle hPC, PseudoConsoleCoord size);

        [LibraryImport(Libraries.Kernel32)]
        internal static partial void ClosePseudoConsole(IntPtr hPC);

        internal const int PROC_THREAD_ATTRIBUTE_PSEUDOCONSOLE = 0x00020016;
    }
}
