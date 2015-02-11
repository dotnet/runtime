// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using dev_t = System.Int64;
using ino_t = System.IntPtr;
using mode_t = System.Int32;
using nlink_t = System.IntPtr;
using uid_t = System.Int32;
using gid_t = System.Int32;
using off_t = System.Int64; // Assuming either 64-bit machine or _FILE_OFFSET_BITS == 64
using off64_t = System.Int64;
using blksize_t = System.IntPtr;
using blkcnt_t = System.IntPtr;
using blkcnt64_t = System.Int64;
using time_t = System.IntPtr;

internal static partial class Interop
{
    internal static partial class libc
    {
        // TODO: For now these stat functions are implemented as P/Invoke wrappers for the native OS
        // functionality, but the stat data structure has non-trivial variations from platform to
        // platform.  In the future, we should introduce a very thin native PAL layer
        // that papers overs the differences, and then change these interop definitions for
        // stat to use that thin native wrapper.

        internal static unsafe int stat(string path, out structStat buf)
        {
            int result;
            if (IntPtr.Size == 4)
            {
                stat_layout_32 buf32 = default(stat_layout_32);
                result = __xstat(_STAT_VER_LINUX_32, path, (byte*)&buf32);
                CopyStat32(ref buf32, out buf);
            }
            else
            {
                stat_layout_64 buf64 = default(stat_layout_64);
                result = __xstat(_STAT_VER_LINUX_64, path, (byte*)&buf64);
                CopyStat64(ref buf64, out buf);
            }
            return result;
        }

        internal static unsafe int fstat(int fd, out structStat buf)
        {
            int result;
            if (IntPtr.Size == 4)
            {
                stat_layout_32 buf32 = default(stat_layout_32);
                result = __fxstat(_STAT_VER_LINUX_32, fd, (byte*)&buf32);
                CopyStat32(ref buf32, out buf);
            }
            else
            {
                stat_layout_64 buf64 = default(stat_layout_64);
                result = __fxstat(_STAT_VER_LINUX_64, fd, (byte*)&buf64);
                CopyStat64(ref buf64, out buf);
            }
            return result;
        }

        internal struct structStat
        {
            internal dev_t st_dev;
            internal ino_t st_ino;
            internal mode_t st_mode;
            internal nlink_t st_nlink;
            internal uid_t st_uid;
            internal gid_t st_gid;
            internal dev_t st_rdev;
            internal off64_t st_size;
            internal blksize_t st_blksize;
            internal blkcnt64_t st_blocks;
            internal time_t st_atime;
            internal time_t st_atimensec;
            internal time_t st_mtime;
            internal time_t st_mtimensec;
            internal time_t st_ctime;
            internal time_t st_ctimensec;
        }

        internal static class FileTypes 
        { 
            internal const int S_IFMT = 0xF000;
            internal const int S_IFDIR = 0x4000;
            internal const int S_IFREG = 0x8000;
        } 

        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern unsafe int __xstat(int ver, string path, byte* buf);

        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern unsafe int __fxstat(int ver, int fd, byte* buf);

        private const int _STAT_VER_LINUX_32 = 3;
        private const int _STAT_VER_LINUX_64 = 1;

        // WARNING: While field size differences based on architecture could be handled by using
        // IntPtr as the field type, the layout of types here actually changes based on architecture,
        // e.g. the ordering of the st_mode and st_nlink fields.  As such, the stat type we expose
        // isn't the one we actually use for interop marshaling.

        private static void CopyStat32(ref stat_layout_32 src, out structStat dst)
        {
            dst.st_dev = src.st_dev;
            dst.st_ino = src.st_ino;
            dst.st_mode = src.st_mode;
            dst.st_nlink = src.st_nlink;
            dst.st_uid = src.st_uid;
            dst.st_gid = src.st_gid;
            dst.st_rdev = src.st_rdev;
            dst.st_size = src.st_size;
            dst.st_blksize = src.st_blksize;
            dst.st_blocks = src.st_blocks;
            dst.st_atime = src.st_atime;
            dst.st_atimensec = IntPtr.Zero;
            dst.st_mtime = src.st_mtime;
            dst.st_mtimensec = IntPtr.Zero;
            dst.st_ctime = src.st_ctime;
            dst.st_ctimensec = IntPtr.Zero;
        }

        private static void CopyStat64(ref stat_layout_64 src, out structStat dst)
        {
            dst.st_dev = src.st_dev;
            dst.st_ino = src.st_ino;
            dst.st_mode = src.st_mode;
            dst.st_nlink = src.st_nlink;
            dst.st_uid = src.st_uid;
            dst.st_gid = src.st_gid;
            dst.st_rdev = src.st_rdev;
            dst.st_size = src.st_size;
            dst.st_blksize = src.st_blksize;
            dst.st_blocks = (long)src.st_blocks;
            dst.st_atime = src.st_atime;
            dst.st_atimensec = src.st_atimensec;
            dst.st_mtime = src.st_mtime;
            dst.st_mtimensec = src.st_mtimensec;
            dst.st_ctime = src.st_ctime;
            dst.st_ctimensec = src.st_ctimensec;
        }

        private struct stat_layout_32
        {
            internal dev_t st_dev;
            private short __pad1;
            internal ino_t st_ino;
            internal mode_t st_mode;
            internal nlink_t st_nlink;
            internal uid_t st_uid;
            internal gid_t st_gid;
            internal dev_t st_rdev;
            private short __pad2;
            internal off64_t st_size;
            internal blksize_t st_blksize;
            internal blkcnt64_t st_blocks;
            private int __pad3;
            internal time_t st_atime;
            internal time_t st_atimensec;
            internal time_t st_mtime;
            internal time_t st_mtimensec;
            internal time_t st_ctime;
            internal time_t st_ctimensec;
            private long __unused4;
        }

        private struct stat_layout_64
        {
            internal dev_t st_dev;
            internal ino_t st_ino;
            internal nlink_t st_nlink;
            internal mode_t st_mode;
            internal uid_t st_uid;
            internal gid_t st_gid;
            private int __pad0;
            internal dev_t st_rdev;
            internal off_t st_size;
            internal blksize_t st_blksize;
            internal blkcnt_t st_blocks;
            internal time_t st_atime;
            internal time_t st_atimensec;
            internal time_t st_mtime;
            internal time_t st_mtimensec;
            internal time_t st_ctime;
            internal time_t st_ctimensec;
            private long __unused2;
            private long __unused3;
            private long __unused4;
        }
    }
}
