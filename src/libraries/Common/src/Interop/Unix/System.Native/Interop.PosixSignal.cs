// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_RegisterForPosixSignal")]
        [SuppressGCTransition]
        internal static extern unsafe bool RegisterForPosixSignal(PosixSignal signal, delegate* unmanaged<PosixSignal, int> handler);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_UnregisterForPosixSignal")]
        [SuppressGCTransition]
        internal static extern void UnregisterForPosixSignal(PosixSignal signal);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_HandlePosixSignal")]
        [SuppressGCTransition]
        internal static extern void HandlePosixSignal(PosixSignal signal);
    }
}
