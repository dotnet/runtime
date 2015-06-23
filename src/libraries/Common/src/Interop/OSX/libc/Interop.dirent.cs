// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static unsafe string GetDirEntName(IntPtr dirEnt)
        {
            dirent* curEntryPtr = (dirent*)dirEnt;
            return Marshal.PtrToStringAnsi((IntPtr)curEntryPtr->d_name, curEntryPtr->d_namelen);
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
