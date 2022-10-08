// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSignalForBreak")]
        [SuppressGCTransition]
        internal static partial int GetSignalForBreak();

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetSignalForBreak")]
        internal static partial int SetSignalForBreak(int signalForBreak, [MarshalAs(UnmanagedType.Bool)] bool distinguishNewLines);
    }
}
