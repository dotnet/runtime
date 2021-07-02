// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        /// <summary>
        /// COM IStream interface. <see href="https://docs.microsoft.com/en-us/windows/desktop/api/objidl/nn-objidl-istream"/>
        /// </summary>
        /// <remarks>
        /// The definition in <see cref="System.Runtime.InteropServices.ComTypes"/> does not lend
        /// itself to efficiently accessing / implementing IStream.
        /// </remarks>
        [ComImport,
            Guid("0000000C-0000-0000-C000-000000000046"),
            InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IStream
        {
            // pcbRead is optional so it must be a pointer
            unsafe void Read(byte* pv, uint cb, uint* pcbRead);

            // pcbWritten is optional so it must be a pointer
            unsafe void Write(byte* pv, uint cb, uint* pcbWritten);

            // SeekOrgin matches the native values, plibNewPosition is optional
            unsafe void Seek(long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition);

            void SetSize(ulong libNewSize);

            // pcbRead and pcbWritten are optional
            unsafe void CopyTo(
                IStream pstm,
                ulong cb,
                ulong* pcbRead,
                ulong* pcbWritten);

            void Commit(uint grfCommitFlags);

            void Revert();

            // Using PreserveSig to allow explicitly returning the HRESULT for "not supported".

            [PreserveSig]
            HRESULT LockRegion(
                ulong libOffset,
                ulong cb,
                uint dwLockType);

            [PreserveSig]
            HRESULT UnlockRegion(
                ulong libOffset,
                ulong cb,
                uint dwLockType);

            unsafe void Stat(
                STATSTG* pstatstg,
                STATFLAG grfStatFlag);

            IStream Clone();
        }
    }
}
