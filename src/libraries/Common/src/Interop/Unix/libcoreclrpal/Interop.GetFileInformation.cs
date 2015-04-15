// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libcoreclrpal
    {
        // Instead of calling stat(2) and friends directly, we call into libcoreclrpal which does so on our behalf
        // we do this because the ABI for this function is different across both OS and architectures and it
        // is easier to handle the differences in native code which builds against the platform headers.

        internal struct fileinfo
        {
            internal uint flags;
            internal int mode;
            internal int uid;
            internal int gid;
            internal long size;
            internal long atime;
            internal long mtime;
            internal long ctime;
            internal long btime;
        }

        internal static class FileTypes 
        { 
            internal const int S_IFMT  = 0xF000;
            internal const int S_IFIFO = 0x1000;
            internal const int S_IFDIR = 0x4000;
            internal const int S_IFREG = 0x8000;
            internal const int S_IFLNK = 0xA000;
        }

        [Flags]
        internal enum FileInformationFlags : uint
        {
            None = 0,
            HasBTime = 0x1,
        }

        [DllImport(Libraries.LibCoreClrPal, CharSet = CharSet.Ansi, SetLastError = true)]
        internal static extern unsafe int GetFileInformationFromPath(string path, out fileinfo buf);

        [DllImport(Libraries.LibCoreClrPal, SetLastError = true)]
        internal static extern unsafe int GetFileInformationFromFd(int fd, out fileinfo buf);
    }
}
