// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

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
            internal long ATimeNsec;
            internal long MTime;
            internal long MTimeNsec;
            internal long CTime;
            internal long CTimeNsec;
            internal long BirthTime;
            internal long BirthTimeNsec;
            internal long Dev;
            internal long Ino;
            internal uint UserFlags;
        }

        internal static class FileTypes
        {
            internal const int S_IFMT = 0xF000;
            internal const int S_IFIFO = 0x1000;
            internal const int S_IFCHR = 0x2000;
            internal const int S_IFDIR = 0x4000;
            internal const int S_IFREG = 0x8000;
            internal const int S_IFLNK = 0xA000;
            internal const int S_IFSOCK = 0xC000;
        }

        [Flags]
        internal enum FileStatusFlags
        {
            None = 0,
            HasBirthTime = 1,
        }

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_FStat", SetLastError = true)]
        internal static extern int FStat(SafeFileHandle fd, out FileStatus output);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_Stat", SetLastError = true)]
        internal static extern int Stat(string path, out FileStatus output);

        [DllImport(Libraries.SystemNative, EntryPoint = "SystemNative_LStat", SetLastError = true)]
        internal static extern int LStat(string path, out FileStatus output);
    }
}
