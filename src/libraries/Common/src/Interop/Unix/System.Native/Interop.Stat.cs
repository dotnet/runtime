// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class System
    {
        internal struct FileStats
        {
            private FileStatsFlags Flags;
            internal int Mode;
            internal int Uid;
            internal int Gid;
            internal int Size;
            internal int AccessTime;
            internal int ModificationTime;
            internal int StatusChangeTime;
            internal int CreationTime;
        }

        internal static class FileTypes
        {
            internal const int S_IFMT = 0xF000;
            internal const int S_IFIFO = 0x1000;
            internal const int S_IFCHR = 0x2000;
            internal const int S_IFDIR = 0x4000;
            internal const int S_IFREG = 0x8000;
            internal const int S_IFLNK = 0xA000;
        }

        [Flags]
        internal enum FileStatsFlags
        {
            None = 0,
            HasCreationTime = 1,
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int FStat(int fileDescriptor, out FileStats output);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int Stat(string path, out FileStats output);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int LStat(string path, out FileStats output);
    }
}
