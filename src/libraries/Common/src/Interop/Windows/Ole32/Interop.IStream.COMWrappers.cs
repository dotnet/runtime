// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

internal static partial class Interop
{
    internal static partial class Ole32
    {
        /// <summary>
        /// IStream interface. <see href="https://docs.microsoft.com/windows/desktop/api/objidl/nn-objidl-istream"/>
        /// </summary>
        /// <remarks>
        /// This interface explicitly doesn't use the built-in COM support, but instead is only used with ComWrappers.
        /// </remarks>
        internal interface IStream
        {
            // pcbRead is optional
            unsafe void Read(byte* pv, uint cb, uint* pcbRead);

            // pcbWritten is optional
            unsafe void Write(byte* pv, uint cb, uint* pcbWritten);

            // SeekOrigin matches the native values, plibNewPosition is optional
            unsafe void Seek(long dlibMove, SeekOrigin dwOrigin, ulong* plibNewPosition);

            void SetSize(ulong libNewSize);

            // pcbRead and pcbWritten are optional
            unsafe HRESULT CopyTo(
                IntPtr pstm,
                ulong cb,
                ulong* pcbRead,
                ulong* pcbWritten);

            void Commit(uint grfCommitFlags);

            void Revert();

            HRESULT LockRegion(
                ulong libOffset,
                ulong cb,
                uint dwLockType);

            HRESULT UnlockRegion(
                ulong libOffset,
                ulong cb,
                uint dwLockType);

            unsafe void Stat(
                STATSTG* pstatstg,
                STATFLAG grfStatFlag);

            unsafe HRESULT Clone(IntPtr* ppstm);
        }
    }
}
