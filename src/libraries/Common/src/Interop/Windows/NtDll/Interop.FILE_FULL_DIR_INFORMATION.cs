// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class NtDll
    {
        /// <summary>
        /// <a href="https://msdn.microsoft.com/en-us/library/windows/hardware/ff540289.aspx">FILE_FULL_DIR_INFORMATION</a> structure.
        /// Used with GetFileInformationByHandleEx and FileIdBothDirectoryInfo/RestartInfo as well as NtQueryFileInformation.
        /// Equivalent to <a href="https://msdn.microsoft.com/en-us/library/windows/desktop/hh447298.aspx">FILE_FULL_DIR_INFO</a> structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct FILE_FULL_DIR_INFORMATION
        {
            /// <summary>
            /// Offset in bytes of the next entry, if any.
            /// </summary>
            public uint NextEntryOffset;

            /// <summary>
            /// Byte offset within the parent directory, undefined for NTFS.
            /// </summary>
            public uint FileIndex;
            public LongFileTime CreationTime;
            public LongFileTime LastAccessTime;
            public LongFileTime LastWriteTime;
            public LongFileTime ChangeTime;
            public long EndOfFile;
            public long AllocationSize;

            /// <summary>
            /// File attributes.
            /// </summary>
            /// <remarks>
            /// Note that MSDN documentation isn't correct for this- it can return
            /// any FILE_ATTRIBUTE that is currently set on the file, not just the
            /// ones documented.
            /// </remarks>
            public FileAttributes FileAttributes;

            /// <summary>
            /// The length of the file name in bytes (without null).
            /// </summary>
            public uint FileNameLength;

            /// <summary>
            /// The extended attribute size OR the reparse tag if a reparse point.
            /// </summary>
            public uint EaSize;

            private char _fileName;
            public unsafe ReadOnlySpan<char> FileName { get { fixed (char* c = &_fileName) { return new ReadOnlySpan<char>(c, (int)FileNameLength / sizeof(char)); } } }

            /// <summary>
            /// Gets the next info pointer or null if there are no more.
            /// </summary>
            public static unsafe FILE_FULL_DIR_INFORMATION* GetNextInfo(FILE_FULL_DIR_INFORMATION* info)
            {
                if (info == null)
                    return null;

                uint nextOffset = (*info).NextEntryOffset;
                if (nextOffset == 0)
                    return null;

                return (FILE_FULL_DIR_INFORMATION*)((byte*)info + nextOffset);
            }
        }
    }
}
