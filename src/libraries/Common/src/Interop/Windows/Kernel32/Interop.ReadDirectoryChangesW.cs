// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        [LibraryImport(Libraries.Kernel32, EntryPoint = "ReadDirectoryChangesW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool ReadDirectoryChangesW(
            SafeFileHandle hDirectory,
            byte[] lpBuffer,
            uint nBufferLength,
            [MarshalAs(UnmanagedType.Bool)] bool bWatchSubtree,
            uint dwNotifyFilter,
            uint* lpBytesReturned,
            NativeOverlapped* lpOverlapped,
            void* lpCompletionRoutine);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal readonly struct FILE_NOTIFY_INFORMATION
        {
            internal readonly uint NextEntryOffset;
            internal readonly FileAction Action;

            // The size of FileName portion of the record, in bytes. The value does not include the terminating null character.
            internal readonly uint FileNameLength;

            // A variable-length field that contains the file name. This field is part of Windows SDK definition of this structure.
            // It is intentionally omitted in the managed definition given how it is used.
            // internal readonly fixed char FileName[1];
        }

        internal enum FileAction : uint
        {
            FILE_ACTION_ADDED = 0x00000001,
            FILE_ACTION_REMOVED = 0x00000002,
            FILE_ACTION_MODIFIED = 0x00000003,
            FILE_ACTION_RENAMED_OLD_NAME = 0x00000004,
            FILE_ACTION_RENAMED_NEW_NAME = 0x00000005
        }
    }
}
