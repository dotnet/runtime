// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Sys
    {
        private static readonly int s_direntSize = GetDirentSize();

        internal enum NodeType : int
        {
            DT_UNKNOWN  =  0,
            DT_FIFO     =  1,
            DT_CHR      =  2,
            DT_DIR      =  4,
            DT_BLK      =  6,
            DT_REG      =  8,
            DT_LNK      = 10,
            DT_SOCK     = 12,
            DT_WHT      = 14
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct InternalDirectoryEntry
        {
            internal IntPtr     Name;
            internal int        NameLength;
            internal NodeType   InodeType;
        }

        internal struct DirectoryEntry
        {
            internal NodeType   InodeType;
            internal string     InodeName;
        }

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern Microsoft.Win32.SafeHandles.SafeDirectoryHandle OpenDir(string path);

        [DllImport(Libraries.SystemNative, SetLastError = false)]
        internal static extern int GetDirentSize();

        [DllImport(Libraries.SystemNative, SetLastError = false)]
        private static unsafe extern int ReadDirR(SafeDirectoryHandle dir, byte* buffer, int bufferSize, out InternalDirectoryEntry outputEntry);

        [DllImport(Libraries.SystemNative, SetLastError = true)]
        internal static extern int CloseDir(IntPtr dir);

        // The calling pattern for ReadDir is described in src/Native/System.Native/pal_readdir.cpp
        internal static int ReadDir(SafeDirectoryHandle dir, out DirectoryEntry outputEntry)
        {
            unsafe
            {
                // To reduce strcpys, alloc a buffer here and get the result from OS, then copy it over for the caller.
                byte* buffer = stackalloc byte[s_direntSize];
                InternalDirectoryEntry temp;
                int ret = ReadDirR(dir, buffer, s_direntSize, out temp);
                outputEntry = ret == 0 ?
                            new DirectoryEntry() { InodeName = GetDirectoryEntryName(temp), InodeType = temp.InodeType } : 
                            default(DirectoryEntry);

                return ret;
            }
        }

        private static unsafe string GetDirectoryEntryName(InternalDirectoryEntry dirEnt)
        {
            if (dirEnt.NameLength == -1)
                return Marshal.PtrToStringAnsi(dirEnt.Name);
            else
                return Marshal.PtrToStringAnsi(dirEnt.Name, dirEnt.NameLength);
        }
    }
}
