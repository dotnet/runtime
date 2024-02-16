// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_InitializeTerminalAndSignalHandling", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool InitializeTerminalAndSignalHandling();

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetKeypadXmit", StringMarshalling = StringMarshalling.Utf8)]
        internal static partial void SetKeypadXmit(SafeFileHandle terminalHandle, string terminfoString);
    }
}
