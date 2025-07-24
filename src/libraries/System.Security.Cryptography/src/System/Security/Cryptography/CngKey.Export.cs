// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography
{
    /// <summary>
    ///     Managed representation of an NCrypt key
    /// </summary>
    public sealed partial class CngKey : IDisposable
    {
        /// <summary>
        ///     Export the key out of the KSP
        /// </summary>
        public byte[] Export(CngKeyBlobFormat format)
        {
            ArgumentNullException.ThrowIfNull(format);

            int numBytesNeeded;
            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, format.Format, IntPtr.Zero, null, 0, out numBytesNeeded, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            byte[] buffer = new byte[numBytesNeeded];
            errorCode = Interop.NCrypt.NCryptExportKey(_keyHandle, IntPtr.Zero, format.Format, IntPtr.Zero, buffer, buffer.Length, out numBytesNeeded, 0);
            if (errorCode != ErrorCode.ERROR_SUCCESS)
                throw errorCode.ToCryptographicException();

            Array.Resize(ref buffer, numBytesNeeded);
            return buffer;
        }

        internal bool TryExportKeyBlob(
            string blobType,
            Span<byte> destination,
            out int bytesWritten)
        {
            return _keyHandle.TryExportKeyBlob(blobType, destination, out bytesWritten);
        }

        internal byte[] ExportPkcs8KeyBlob(
            ReadOnlySpan<char> password,
            int kdfCount)
        {
            bool ret = CngHelpers.ExportPkcs8KeyBlob(
                allocate: true,
                _keyHandle,
                password,
                kdfCount,
                Span<byte>.Empty,
                out _,
                out byte[]? allocated);

            Debug.Assert(ret);
            Debug.Assert(allocated != null); // since `allocate: true`
            return allocated;
        }

        internal bool TryExportPkcs8KeyBlob(
            ReadOnlySpan<char> password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten)
        {
            return CngHelpers.ExportPkcs8KeyBlob(
                false,
                _keyHandle,
                password,
                kdfCount,
                destination,
                out bytesWritten,
                out _);
        }
    }
}
