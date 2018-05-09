// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: IStream interface definition.
**
**
=============================================================================*/

using System;

namespace System.Runtime.InteropServices.ComTypes
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]

    public struct STATSTG
    {
        public String pwcsName;
        public int type;
        public Int64 cbSize;
        public FILETIME mtime;
        public FILETIME ctime;
        public FILETIME atime;
        public int grfMode;
        public int grfLocksSupported;
        public Guid clsid;
        public int grfStateBits;
        public int reserved;
    }

    [Guid("0000000c-0000-0000-C000-000000000046")]
    [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
    [ComImport]
    public interface IStream
    {
        // ISequentialStream portion
        void Read([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out] Byte[] pv, int cb, IntPtr pcbRead);
        void Write([MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Byte[] pv, int cb, IntPtr pcbWritten);

        // IStream portion
        void Seek(Int64 dlibMove, int dwOrigin, IntPtr plibNewPosition);
        void SetSize(Int64 libNewSize);
        void CopyTo(IStream pstm, Int64 cb, IntPtr pcbRead, IntPtr pcbWritten);
        void Commit(int grfCommitFlags);
        void Revert();
        void LockRegion(Int64 libOffset, Int64 cb, int dwLockType);
        void UnlockRegion(Int64 libOffset, Int64 cb, int dwLockType);
        void Stat(out STATSTG pstatstg, int grfStatFlag);
        void Clone(out IStream ppstm);
    }
}
