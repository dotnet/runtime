// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetSignalForBreak")]
        [SuppressGCTransition]
        internal static extern int GetSignalForBreak();

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetSignalForBreak")]
        internal static extern int SetSignalForBreak(int signalForBreak);
    }
}
