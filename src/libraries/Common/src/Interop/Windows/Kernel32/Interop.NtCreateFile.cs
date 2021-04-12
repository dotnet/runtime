// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        // https://docs.microsoft.com/en-us/windows/desktop/api/winternl/nf-winternl-ntcreatefile
        // https://docs.microsoft.com/en-us/windows-hardware/drivers/ddi/content/ntifs/nf-ntifs-ntcreatefile
        [DllImport(Libraries.NtDll, CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static unsafe extern uint NtCreateFile(
            out IntPtr fileHandle,
            int desiredAccess,
            ref OBJECT_ATTRIBUTES objectAttributes,
            out IO_STATUS_BLOCK ioStatusBlock,
            long* allocationSize,
            uint fileAttributes,
            uint shareAccess,
            uint createDisposition,
            uint createOptions,
            void* extendedAttributesBuffer,
            uint extendedAttributesLength);

        internal static unsafe uint NtCreateFile(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long allocationSize, out IntPtr fileHandle)
        {
            string prefixedAbsolutePath = PathInternal.IsExtended(path)
                ? path
                : @"\??\" + Path.GetFullPath(path); // TODO: we might consider getting rid of this managed allocation

            fixed (char* filePath = prefixedAbsolutePath)
            {
                UNICODE_STRING unicodeString = new UNICODE_STRING(filePath, prefixedAbsolutePath.Length);
                OBJECT_ATTRIBUTES objectAttributes = new OBJECT_ATTRIBUTES(&unicodeString, GetObjectAttributes(share));

                return NtCreateFile(
                    fileHandle: out fileHandle,
                    desiredAccess: GetDesiredAccess(access, mode, options),
                    objectAttributes: ref objectAttributes,
                    ioStatusBlock: out _,
                    allocationSize: &allocationSize,
                    fileAttributes: GetFileAttributes(options),
                    shareAccess: GetShareAccess(share),
                    createDisposition: GetCreateDisposition(mode),
                    createOptions: GetCreateOptions(options),
                    extendedAttributesBuffer: default,
                    extendedAttributesLength: default);
            }
        }

        private static uint GetObjectAttributes(FileShare share)
        {
            uint result = 0;// 0x00000040; // Lookups for this object should be case insensitive. [OBJ_CASE_INSENSITIVE]

            if ((share & FileShare.Inheritable) != 0 )
            {
                result |= 0x00000002;
            }

            return result;
        }

        private static int GetDesiredAccess(FileAccess access, FileMode fileMode, FileOptions options)
        {
            int result = 0;

            if ((access & FileAccess.Read) != 0)
            {
                result |= GenericOperations.GENERIC_READ;
            }
            if ((access & FileAccess.Write) != 0)
            {
                result |= GenericOperations.GENERIC_WRITE;
            }
            if (fileMode == FileMode.Append)
            {
                result |= 0x0004; // FILE_APPEND_DATA
            }
            if ((options & FileOptions.Asynchronous) == 0)
            {
                result |= 0x00100000; // SYNCHRONIZE, requried by FILE_SYNCHRONOUS_IO_NONALERT
            }

            return result;
        }

        private static uint GetFileAttributes(FileOptions options)
        {
            uint result = 0;

            if ((options & FileOptions.Encrypted) != 0)
            {
                result |= 0x00004000; // FILE_ATTRIBUTE_ENCRYPTED
            }

            return result;
        }

        // FileShare.Inheritable is handled in GetObjectAttributes
        private static uint GetShareAccess(FileShare share)
        {
            uint result = 0;

            if ((share & FileShare.Read) != 0)
            {
                result |= 1; // FILE_SHARE_READ
            }
            if ((share & FileShare.Write) != 0)
            {
                result |= 2; // FILE_SHARE_WRITE
            }
            if ((share & FileShare.Delete) != 0)
            {
                result |= 4; // FILE_SHARE_DELETE
            }

            // https://docs.microsoft.com/en-us/windows/win32/api/winternl/nf-winternl-ntcreatefile
            // "If the original caller of NtCreateFile does not specify FILE_SHARE_READ, FILE_SHARE_WRITE, or FILE_SHARE_DELETE,
            // no other open operations can be performed on the file; that is, the original caller is given exclusive access to the file."
            // which is how we get FileShare.None working

            return result;
        }

        private static uint GetCreateDisposition(FileMode mode)
        {
            switch (mode)
            {
                case FileMode.CreateNew:
                    return 2; // FILE_CREATE
                case FileMode.Create:
                    return 0; // FILE_SUPERSEDE
                case FileMode.OpenOrCreate:
                case FileMode.Append: // has extra handling in GetDesiredAccess
                    return 3; // FILE_OPEN_IF
                case FileMode.Truncate:
                    return 4; // FILE_OVERWRITE
                default:
                    Debug.Assert(mode == FileMode.Open); // the enum value is validated in FileStream ctor
                    return 1; // FILE_OPEN
            }
        }

        // FileOptions.Encryptend is handled in GetFileAttributes
        private static uint GetCreateOptions(FileOptions options)
        {
            // Every directory is just a directory FILE.
            // FileStream does not allow for opening directories on purpose.
            // FILE_NON_DIRECTORY_FILE is used to ensure that
            uint result = 0x00000040; // FILE_NON_DIRECTORY_FILE

            if ((options & FileOptions.WriteThrough) != 0)
            {
                result |= 0x00000002; // FILE_WRITE_THROUGH
            }
            if ((options & FileOptions.RandomAccess) != 0)
            {
                result |= 0x00000800; // FILE_RANDOM_ACCESS
            }
            if ((options & FileOptions.SequentialScan) != 0)
            {
                result |= 0x00000004; // FILE_SEQUENTIAL_ONLY
            }
            if ((options & FileOptions.DeleteOnClose) != 0)
            {
                result |= 0x00001000; // FILE_DELETE_ON_CLOSE
            }
            if ((options & FileOptions.Asynchronous) == 0)
            {
                // it's async by default, so we need to disable it when async was not requested
                result |= 0x00000020; // FILE_SYNCHRONOUS_IO_NONALERT, has extra handling in GetDesiredAccess
            }
            if (((int)options & 0x20000000) != 0) // NoBuffering
            {
                result |= 0x00000008; // FILE_NO_INTERMEDIATE_BUFFERING
            }

            return result;
        }
    }
}
