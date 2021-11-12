// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct AttrList
        {
            public ushort bitmapCount;
            public ushort reserved;
            public uint commonAttr;
            public uint volAttr;
            public uint dirAttr;
            public uint fileAttr;
            public uint forkAttr;

            public const ushort ATTR_BIT_MAP_COUNT = 5;
            public const uint ATTR_CMN_CRTIME = 0x00000200;
        }

        [DllImport(Libraries.libc, EntryPoint = "setattrlist", SetLastError = true)]
        internal static unsafe extern int setattrlist(string path, AttrList* attrList, void* attrBuf, nint attrBufSize, CULong options);
    }
}
