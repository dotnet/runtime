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
        internal static extern IntPtr readdir(IntPtr dirp);

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
    }
}
