// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This implementation uses SunOS portfs (event ports) to watch directories.
// portfs can detect when a directory's modification time changes via port_associate,
// but cannot tell us WHAT changed in the directory. This makes it different from
// Linux inotify or Windows ReadDirectoryChangesW.
//
// The FileSystemWatcher implementation must:
// 1. Use port_associate to watch for FILE_MODIFIED events on directories
// 2. When an event occurs, re-read the directory contents
// 3. Compare with cached state to determine what actually changed
//

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class PortFs
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PortCreate",
            SetLastError = true)]
        internal static partial SafeFileHandle PortCreate();

        // pFileObj must point to pinned memory with size >= sizeof(file_obj)
        // because the address is used as an identifier in the kernel.
        // dirPath string is used while making the association but is
        // never referenced again after PortAssociate returns.
        // mtime is the directory modification time before reading the directory
        // cookie is returned by PortGet to identify which directory changed
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PortAssociate",
            SetLastError = true, StringMarshalling = StringMarshalling.Utf8)]
        internal static unsafe partial int PortAssociate(SafeFileHandle fd, IntPtr pFileObj, string dirPath, Interop.Sys.TimeSpec* mtime, int evmask, nuint cookie);

        // Returns the cookie value from PortAssociate in the cookie parameter
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PortGet",
            SetLastError = true)]
        internal static unsafe partial int PortGet(SafeFileHandle fd, int* events, nuint* cookie, Interop.Sys.TimeSpec* tmo);

        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PortDissociate",
            SetLastError = true)]
        internal static partial int PortDissociate(SafeFileHandle fd, IntPtr pFileObj);

        // Send a synthetic event to wake up a blocked PortGet call
        // evflags can be any PortEvent value (e.g., FILE_NOFOLLOW for cancellation)
        // cookie is returned to PortGet to identify the event
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_PortSend",
            SetLastError = true)]
        internal static partial int PortSend(SafeFileHandle fd, int evflags, nuint cookie);

        [Flags]
        internal enum PortEvent
        {
            FILE_ACCESS      = 0x00000001,
            FILE_MODIFIED    = 0x00000002,
            FILE_ATTRIB      = 0x00000004,
            FILE_TRUNC       = 0x00100000,
            FILE_NOFOLLOW    = 0x10000000,
            FILE_EXCEPTION   = 0x20000000,

            // Exception events
            FILE_DELETE      = 0x00000010,
            FILE_RENAME_TO   = 0x00000020,
            FILE_RENAME_FROM = 0x00000040,
            // These are unused, and the duplicate value causes warnings.
            // UNMOUNTED     = 0x20000000,
            // MOUNTEDOVER   = 0x40000000,
        }

        // sizeof(file_obj) = 3*sizeof(timespec) + 3*sizeof(uintptr_t) + sizeof(char*)
        // On 64-bit: 3*16 + 3*8 + 8 = 80 bytes
        internal const int FileObjSize = 80;
    }
}
