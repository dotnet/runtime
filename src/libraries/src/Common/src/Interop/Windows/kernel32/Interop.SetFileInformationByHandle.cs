// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, SetLastError = true)]
        internal static extern bool SetFileInformationByHandle(SafeFileHandle hFile, FILE_INFO_BY_HANDLE_CLASS FileInformationClass, ref FILE_BASIC_INFO lpFileInformation, uint dwBufferSize);

        // Default values indicate "no change".  Use defaults so that we don't force callsites to be aware of the default values
        internal unsafe static bool SetFileTime(
            SafeFileHandle hFile,
            long creationTime = -1,
            long lastAccessTime = -1,
            long lastWriteTime = -1,
            long changeTime = -1,
            uint fileAttributes = 0)
        {
            FILE_BASIC_INFO basicInfo = new FILE_BASIC_INFO()
            {
                CreationTime = creationTime,
                LastAccessTime = lastAccessTime,
                LastWriteTime = lastWriteTime,
                ChangeTime = changeTime,
                FileAttributes = fileAttributes
            };

            return SetFileInformationByHandle(hFile, FILE_INFO_BY_HANDLE_CLASS.FileBasicInfo, ref basicInfo, (uint)Marshal.SizeOf<FILE_BASIC_INFO>());
        }

        internal struct FILE_BASIC_INFO
        {
            internal long CreationTime;
            internal long LastAccessTime;
            internal long LastWriteTime;
            internal long ChangeTime;
            internal uint FileAttributes;
        }

        internal enum FILE_INFO_BY_HANDLE_CLASS : uint
        {
            FileBasicInfo = 0x0u,
            FileStandardInfo = 0x1u,
            FileNameInfo = 0x2u,
            FileRenameInfo = 0x3u,
            FileDispositionInfo = 0x4u,
            FileAllocationInfo = 0x5u,
            FileEndOfFileInfo = 0x6u,
            FileStreamInfo = 0x7u,
            FileCompressionInfo = 0x8u,
            FileAttributeTagInfo = 0x9u,
            FileIdBothDirectoryInfo = 0xAu,
            FileIdBothDirectoryRestartInfo = 0xBu,
            FileIoPriorityHintInfo = 0xCu,
            FileRemoteProtocolInfo = 0xDu,
            FileFullDirectoryInfo = 0xEu,
            FileFullDirectoryRestartInfo = 0xFu,
            FileStorageInfo = 0x10u,
            FileAlignmentInfo = 0x11u,
            FileIdInfo = 0x12u,
            FileIdExtdDirectoryInfo = 0x13u,
            FileIdExtdDirectoryRestartInfo = 0x14u,
            MaximumFileInfoByHandleClass = 0x15u,
        }
    }
}
