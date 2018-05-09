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
