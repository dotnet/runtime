// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

internal partial class Interop
{
    internal partial class Kernel32
    {
        internal partial class IOReparseOptions
        {
            internal const uint IO_REPARSE_TAG_FILE_PLACEHOLDER = 0x80000015;
            internal const uint IO_REPARSE_TAG_MOUNT_POINT = 0xA0000003;
        }

        internal partial class FileOperations
        {
            internal const int OPEN_EXISTING = 3;
            internal const int COPY_FILE_FAIL_IF_EXISTS = 0x00000001;

            internal const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
            internal const int FILE_FLAG_FIRST_PIPE_INSTANCE = 0x00080000;
            internal const int FILE_FLAG_OVERLAPPED = 0x40000000;

            internal const int FILE_LIST_DIRECTORY = 0x0001;
        }
        
    }
}
