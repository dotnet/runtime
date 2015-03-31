// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, EntryPoint = "readdir$INODE64", SetLastError = true)]
        internal static extern IntPtr readdir(IntPtr dirp);

        internal static unsafe DType GetDirEntType(IntPtr dirEnt)
        {
            return ((dirent*)dirEnt)->d_type;
        }

        internal static unsafe string GetDirEntName(IntPtr dirEnt)
        {
            dirent* curEntryPtr = (dirent*)dirEnt;
            return Marshal.PtrToStringAnsi((IntPtr)curEntryPtr->d_name, curEntryPtr->d_namelen);
        }

        internal enum DType : byte
        {
            DT_UNKNOWN = 0,
            DT_FIFO = 1,
            DT_CHR = 2,
            DT_DIR = 4,
            DT_BLK = 6,
            DT_REG = 8,
            DT_LNK = 10,
            DT_SOCK = 12,
            DT_WHT = 14
        }

        private const int __DARWIN_MAXPATHLEN = 1024;

        #pragma warning disable 0649 // fields are assigned by P/Invoke call 
        private unsafe struct dirent 
        { 
            internal ulong d_ino; 
            internal ulong d_off; 
            internal ushort d_reclen;
            internal ushort d_namelen;
            internal DType d_type; 
            internal fixed byte d_name[__DARWIN_MAXPATHLEN]; 
        }
        #pragma warning restore 0649
    }
}
