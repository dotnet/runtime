// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct WinSize
        {
            internal ushort Row;
            internal ushort Col;
            internal ushort XPixel;
            internal ushort YPixel;
        };

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetWindowSize", SetLastError = true)]
        internal static partial int GetWindowSize(SafeFileHandle terminalHandle, out WinSize winSize);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetWindowSize", SetLastError = true)]
        internal static partial int SetWindowSize(in WinSize winSize);
    }
}
