// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Sys
    {
        // Even though csc will by default use a sequential layout, a CS0649 warning as error
        // is produced for un-assigned fields when no StructLayout is specified.
        //
        // Explicitly saying Sequential disables that warning/error for consumers which only
        // use Stat in debug builds.
        [StructLayout(LayoutKind.Sequential)]
        internal struct FileStatus
        {
            internal FileStatusFlags Flags;
            internal int Mode;
            internal uint Uid;
            internal uint Gid;
            internal long Size;
            internal long ATime;
            internal long MTime;
            internal long CTime;
            internal long BirthTime;
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
        internal enum FileStatusFlags
        {
            None = 0,
            HasBirthTime = 1,
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int FStat(int fd, out FileStatus output);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int Stat(string path, out FileStatus output);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int LStat(string path, out FileStatus output);
    }
}
