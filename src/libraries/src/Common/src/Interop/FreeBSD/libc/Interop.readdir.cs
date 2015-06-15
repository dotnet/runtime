// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

using ino_t = System.IntPtr;
using off_t = System.Int64; // Assuming either 64-bit machine or _FILE_OFFSET_BITS == 64

internal static partial class Interop
{
    internal static partial class libc
    {
        [DllImport(Libraries.Libc, SetLastError = true)]
        internal static extern IntPtr readdir(SafeDirHandle dirp);

        internal static unsafe DType GetDirEntType(IntPtr dirEnt)
        {
            return ((dirent*)dirEnt)->d_type;
        }

        internal static unsafe string GetDirEntName(IntPtr dirEnt)
        {
            return Marshal.PtrToStringAnsi((IntPtr)((dirent*)dirEnt)->d_name);
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

        #pragma warning disable 0649 // fields are assigned by P/Invoke call 
        private unsafe struct dirent 
        { 
            internal UInt32 d_fileno;
            internal UInt16 d_reclen;
            internal DType d_type;
            internal byte d_namlen;
            internal fixed byte d_name[256];
        } 
        #pragma warning restore 0649 
    }
}
