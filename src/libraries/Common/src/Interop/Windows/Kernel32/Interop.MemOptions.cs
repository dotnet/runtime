// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

internal static partial class Interop
{
    internal static partial class Kernel32
    {
        internal static partial class MemOptions
        {
            internal const int MEM_COMMIT = 0x1000;
            internal const int MEM_RESERVE = 0x2000;
            internal const int MEM_RELEASE = 0x8000;
            internal const int MEM_FREE = 0x10000;
        }

        internal const int INVALID_FILE_SIZE = -1;

        internal static partial class PageOptions
        {
            internal const int PAGE_READWRITE = 0x04;
            internal const int PAGE_READONLY = 0x02;
            internal const int PAGE_WRITECOPY = 0x08;
            internal const int PAGE_EXECUTE_READ = 0x20;
            internal const int PAGE_EXECUTE_READWRITE = 0x40;
        }

        internal static partial class FileMapOptions
        {
            internal const int FILE_MAP_COPY = 0x0001;
            internal const int FILE_MAP_WRITE = 0x0002;
            internal const int FILE_MAP_READ = 0x0004;
            internal const int FILE_MAP_EXECUTE = 0x0020;
        }

    }
}
