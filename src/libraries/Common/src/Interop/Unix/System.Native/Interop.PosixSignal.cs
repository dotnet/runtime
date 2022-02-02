// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_SetPosixSignalHandler")]
        [SuppressGCTransition]
        internal static unsafe partial void SetPosixSignalHandler(delegate* unmanaged<int, PosixSignal, int> handler);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_EnablePosixSignalHandling", SetLastError = true)]
        internal static partial bool EnablePosixSignalHandling(int signal);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_DisablePosixSignalHandling")]
        internal static partial void DisablePosixSignalHandling(int signal);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_HandleNonCanceledPosixSignal")]
        internal static partial void HandleNonCanceledPosixSignal(int signal);

        [GeneratedDllImport(Libraries.SystemNative, EntryPoint = "SystemNative_GetPlatformSignalNumber")]
        [SuppressGCTransition]
        internal static partial int GetPlatformSignalNumber(PosixSignal signal);
    }
}
