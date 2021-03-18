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
            [MarshalAs(UnmanagedType.U2)] public ushort bitmapCount;
            [MarshalAs(UnmanagedType.U2)] public ushort reserved;
            [MarshalAs(UnmanagedType.U4)] public uint commonAttr;
            [MarshalAs(UnmanagedType.U4)] public uint volAttr;
            [MarshalAs(UnmanagedType.U4)] public uint dirAttr;
            [MarshalAs(UnmanagedType.U4)] public uint fileAttr;
            [MarshalAs(UnmanagedType.U4)] public uint forkAttr;

            public const ushort ATTR_BIT_MAP_COUNT = 5;
            public const uint ATTR_CMN_CRTIME = 0x00000200;
            public const uint ATTR_CMN_MODTIME = 0x00000400;
            public const uint ATTR_CMN_ACCTIME = 0x00001000;
        }

        [DllImport(Libraries.libc, EntryPoint = "setattrlist", SetLastError = true)]
        internal static unsafe extern int setattrlist(string path, AttrList* attrList, void* attrBuf, nint attrBufSize, uint options);

        internal const uint FSOPT_NOFOLLOW = 0x00000001;
    }
}
