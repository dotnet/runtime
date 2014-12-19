// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Runtime.InteropServices;

using mode_t = System.Int32;
using off64_t = System.Int64;
using off_t = System.IntPtr;
using size_t = System.IntPtr;
using time_t = System.IntPtr;
using ino_t = System.IntPtr;

internal static partial class Interop
{
    private const string LIBC = "libc";

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int unlink(string pathname);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern off64_t ftruncate64(int fd, off64_t length);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int posix_fadvise(int fd, off_t offset, off_t len, int advice);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int flock(int fd, int operation);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int access(string path, int amode);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int rename(string oldpath, string newpath);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int remove(string pathname);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int mkdir(string pathname, mode_t mode);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern IntPtr opendir(string name); // opendir/readdir/closedir defined in terms of IntPtr so it may be used in iterators (which don't allow unsafe code)

    [DllImport(LIBC, SetLastError = true)]
    internal static extern IntPtr readdir(IntPtr dirp);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int closedir(IntPtr dirp);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int chdir(string path);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern unsafe byte* getcwd(byte* buf, size_t bufSize);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int pathconf(string path, int name);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int utime(string path, ref utimbuf times);

    [DllImport(LIBC, SetLastError = true)]
    internal static extern int chmod(string path, mode_t mode);

    [DllImport(LIBC)]
    internal static extern int fnmatch(string pattern, string str, int flags);

    [DllImport(LIBC)]
    internal static extern int geteuid();

    [DllImport(LIBC)]
    internal static extern int getegid();

    internal struct utimbuf
    {
        internal time_t actime;
        internal time_t modtime;
    }

#pragma warning disable 0649 // fields are assigned by P/Invoke call
    internal unsafe struct dirent
    {
        internal ino_t d_ino;
        internal off_t d_off;
        internal short d_reclen;
        internal byte d_type;
        internal fixed byte d_name[256];
    }
#pragma warning restore 0649

    internal enum LockOperations
    {
        LOCK_SH = 1,
        LOCK_EX = 2,
        LOCK_UN = 8,
        LOCK_NB = 4,
    }

    internal enum Permissions
    {
        Mask = S_IRWXU | S_IRWXG | S_IRWXO,

        S_IRWXU = S_IRUSR | S_IWUSR | S_IXUSR,
        S_IRUSR = 0x100,
        S_IWUSR = 0x80,
        S_IXUSR = 0x40,

        S_IRWXG = S_IRGRP | S_IWGRP | S_IXGRP,
        S_IRGRP = 0x20,
        S_IWGRP = 0x10,
        S_IXGRP = 0x8,

        S_IRWXO = S_IROTH | S_IWOTH | S_IXOTH,
        S_IROTH = 0x4,
        S_IWOTH = 0x2,
        S_IXOTH = 0x1,
    }

    internal enum Advice
    {
        POSIX_FADV_NORMAL = 0,
        POSIX_FADV_RANDOM = 1,
        POSIX_FADV_SEQUENTIAL = 2,
        POSIX_FADV_WILLNEED = 3,
        POSIX_FADV_DONTNEED = 4,
        POSIX_FADV_NOREUSE = 5
    }

    internal enum FileTypes
    {
        S_IFMT = 0xF000,
        S_IFDIR = 0x4000,
        S_IFREG = 0x8000
    }

    internal enum PathConfNames
    {
        NAME_MAX = 3,
        PATH_MAX = 4,
    }

    internal enum AccessModes
    {
        F_OK = 0
    }

    internal static int DEFAULT_MAX_PATH = 4096;
    internal static int DEFAULT_MAX_NAME = 255;
}
