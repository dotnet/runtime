// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class @libc
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

        [LibraryImport(Libraries.libc, EntryPoint = "setattrlist", StringMarshalling = StringMarshalling.Utf8, SetLastError = true)]
        internal static unsafe partial int setattrlist(string path, AttrList* attrList, void* attrBuf, nint attrBufSize, CULong options);

        internal const uint FSOPT_NOFOLLOW = 0x00000001;
        [LibraryImport(Libraries.libc, EntryPoint = "fsetattrlist", SetLastError = true)]
        private static unsafe partial int fsetattrlist(int fd, AttrList* attrList, void* attrBuf, nint attrBufSize, CULong options);

        internal static unsafe int fsetattrlist(SafeHandle handle, AttrList* attrList, void* attrBuf, nint attrBufSize, CULong options)
        {
            bool refAdded = false;
            try
            {
                handle.DangerousAddRef(ref refAdded);
                return fsetattrlist(handle.DangerousGetHandle().ToInt32(), attrList, attrBuf, attrBufSize, options);
            }
            finally
            {
                if (refAdded)
                    handle.DangerousRelease();
            }
        }
    }
}
