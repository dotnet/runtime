// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO
{
    /// <summary>
    /// Used for processing and filtering find results.
    /// </summary>
    internal unsafe ref struct RawFindData
    {
        internal RawFindData(Interop.NtDll.FILE_FULL_DIR_INFORMATION* info, string directory, string originalDirectory, string originalUserDirectory)
        {
            _info = info;
            Directory = directory;
            OriginalDirectory = originalDirectory;
            OriginalUserDirectory = originalUserDirectory;
        }

        internal unsafe Interop.NtDll.FILE_FULL_DIR_INFORMATION* _info;
        public string Directory { get; private set; }
        public string OriginalDirectory { get; private set; }
        public string OriginalUserDirectory { get; private set; }

        public ReadOnlySpan<char> FileName => _info->FileName;
        public FileAttributes Attributes => _info->FileAttributes;
        public long Length => _info->EndOfFile;

        public DateTime CreationTimeUtc => _info->CreationTime.ToDateTimeUtc();
        public DateTime LastAccessTimeUtc => _info->LastAccessTime.ToDateTimeUtc();
        public DateTime LastWriteTimeUtc => _info->LastWriteTime.ToDateTimeUtc();
    }
}
