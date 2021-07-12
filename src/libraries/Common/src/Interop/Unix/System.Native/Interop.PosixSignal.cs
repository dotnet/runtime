// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetPosixSignalHandler")]
        [SuppressGCTransition]
        internal static extern unsafe void SetPosixSignalHandler(delegate* unmanaged<int, PosixSignal, int> handler);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnablePosixSignalHandling", SetLastError = true)]
        internal static extern bool EnablePosixSignalHandling(int signal);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_DisablePosixSignalHandling")]
        internal static extern void DisablePosixSignalHandling(int signal);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_HandleNonCanceledPosixSignal")]
        internal static extern bool HandleNonCanceledPosixSignal(int signal, int handlersDisposed);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPlatformSignalNumber")]
        [SuppressGCTransition]
        internal static extern int GetPlatformSignalNumber(PosixSignal signal);
    }
}
