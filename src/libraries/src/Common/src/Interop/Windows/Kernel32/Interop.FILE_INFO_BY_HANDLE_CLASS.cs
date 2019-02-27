// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        // https://msdn.microsoft.com/en-us/library/windows/desktop/aa364228.aspx
        internal enum FILE_INFO_BY_HANDLE_CLASS : uint
        {
            // Up to FileRemoteProtocolInfo available in Windows 7

            /// <summary>
            /// Returns basic information (timestamps and attributes).
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileBasicInformation.
            /// </remarks>
            FileBasicInfo,

            /// <summary>
            /// Returns file size, link count, pending delete status, and if it is a directory.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileStandardInformation.
            /// </remarks>
            FileStandardInfo,

            /// <summary>
            /// Gets the file name.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileNameInformation.
            /// </remarks>
            FileNameInfo,

            /// <summary>
            /// Renames a file. Allows renaming a file without having to specify a full path, if you have
            /// a handle to the directory the file resides in.
            /// </summary>
            /// <remarks>
            /// Only valid for SetFileInformationByHandle. Thunks to NtSetInformationFile and FileRenameInformation.
            /// MoveFileEx is effectively the same API.
            /// </remarks>
            FileRenameInfo,

            /// <summary>
            /// Allows marking a file handle for deletion. Handle must have been opened with Delete access.
            /// You cannot change the state of a handle opened with DeleteOnClose.
            /// </summary>
            /// <remarks>
            /// Only valid for SetFileInformationByHandle. Thunks to NtSetInformationFile and FileDispositionInformation.
            /// DeleteFile is effectively the same API.
            /// </remarks>
            FileDispositionInfo,

            /// <summary>
            /// Allows setting the allocated size of the file.
            /// </summary>
            /// <remarks>
            /// Only valid for SetFileInformationByHandle. Thunks to NtSetInformationFile.
            /// SetEndOfFile sets this after setting the logical end of file to the current position via FileEndOfFileInfo.
            /// </remarks>
            FileAllocationInfo,

            /// <summary>
            /// Allows setting the end of file.
            /// </summary>
            /// <remarks>
            /// Only valid for SetFileInformationByHandle. Thunks to NtSetInformationFile.
            /// SetEndOfFile calls this to set the logical end of file to whatever the current position is.
            /// </remarks>
            FileEndOfFileInfo,

            /// <summary>
            /// Gets stream information for the file.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileStreamInformation.
            /// </remarks>
            FileStreamInfo,

            /// <summary>
            /// Gets compression information for the file.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileCompressionInformation.
            /// </remarks>
            FileCompressionInfo,

            /// <summary>
            /// Gets the file attributes and reparse tag.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileAttributeTagInformation.
            /// </remarks>
            FileAttributeTagInfo,

            /// <summary>
            /// Starts a query for for file information in a directory.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryDirectoryFile and FileIdBothDirectoryInformation with RestartScan
            /// set to false.
            /// </remarks>
            FileIdBothDirectoryInfo,

            /// <summary>
            /// Resumes a query for file information in a directory.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryDirectoryFile and FileIdBothDirectoryInformation with RestartScan
            /// set to true.
            /// </remarks>
            FileIdBothDirectoryRestartInfo,

            /// <summary>
            /// Allows setting the priority hint for a file.
            /// </summary>
            /// <remarks>
            /// Only valid for SetFileInformationByHandle. Thunks to NtSetInformationFile.
            /// </remarks>
            FileIoPriorityHintInfo,

            /// <summary>
            /// Gets the file remote protocol information.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryInformationFile and FileRemoteProtocolInformation.
            /// </remarks>
            FileRemoteProtocolInfo,

            /// <summary>
            /// Starts a query for for file information in a directory. Uses FILE_FULL_DIR_INFO.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryDirectoryFile and FileFullDirectoryInformation with RestartScan
            /// set to false. Windows 8 and up.
            /// </remarks>
            FileFullDirectoryInfo,

            /// <summary>
            /// Resumes a query for file information in a directory. Uses FILE_FULL_DIR_INFO.
            /// </summary>
            /// <remarks>
            /// Thunks to NtQueryDirectoryFile and FileFullDirectoryInformation with RestartScan
            /// set to true. Windows 8 and up.
            /// </remarks>
            FileFullDirectoryRestartInfo
        }
    }
}
