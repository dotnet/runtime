// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.NtDll)]
        internal static unsafe partial uint NtCreateWaitCompletionPacket(
            out nint CompletionPacketHandle,
            DesiredAccess DesiredAccess,
            OBJECT_ATTRIBUTES* ObjectAttributes);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.NtDll)]
        internal static unsafe partial uint NtAssociateWaitCompletionPacket(
            nint CompletionPacketHandle,
            nint IoCompletionHandle,
            SafeWaitHandle TargetObjectHandle,
            void* KeyContext,
            void* ApcContext,
            uint IoStatus,
            nuint IoStatusInformation,
            BOOLEAN* AlreadySignaled);

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Libraries.NtDll)]
        internal static partial uint NtCancelWaitCompletionPacket(
            nint WaitCompletionPacketHandle,
            BOOLEAN RemoveSignaledPacket);
    }
}
