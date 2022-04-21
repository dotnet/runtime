// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Cryptography;
using Microsoft.Win32.SafeHandles;

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
            // Sanity check the current bounds
            Span<byte> empty = default;

            ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                _keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                ref MemoryMarshal.GetReference(empty),
                empty.Length,
                out int written,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            if (written > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            errorCode = Interop.NCrypt.NCryptExportKey(
                _keyHandle,
                IntPtr.Zero,
                blobType,
                IntPtr.Zero,
                ref MemoryMarshal.GetReference(destination),
                destination.Length,
                out written,
                0);

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                throw errorCode.ToCryptographicException();
            }

            bytesWritten = written;
            return true;
        }

        internal byte[] ExportPkcs8KeyBlob(
            ReadOnlySpan<char> password,
            int kdfCount)
        {
            bool ret = ExportPkcs8KeyBlob(
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
            return ExportPkcs8KeyBlob(
                false,
                _keyHandle,
                password,
                kdfCount,
                destination,
                out bytesWritten,
                out _);
        }

        // The Windows APIs for OID strings are ASCII-only
        private static readonly byte[] s_pkcs12TripleDesOidBytes =
            System.Text.Encoding.ASCII.GetBytes("1.2.840.113549.1.12.1.3\0");

        internal static unsafe bool ExportPkcs8KeyBlob(
            bool allocate,
            SafeNCryptKeyHandle keyHandle,
            ReadOnlySpan<char> password,
            int kdfCount,
            Span<byte> destination,
            out int bytesWritten,
            out byte[]? allocated)
        {
            using (SafeUnicodeStringHandle stringHandle = new SafeUnicodeStringHandle(password))
            {
                fixed (byte* oidPtr = s_pkcs12TripleDesOidBytes)
                {
                    Interop.NCrypt.NCryptBuffer* buffers = stackalloc Interop.NCrypt.NCryptBuffer[3];

                    Interop.NCrypt.PBE_PARAMS pbeParams = default;
                    Span<byte> salt = new Span<byte>(pbeParams.rgbSalt, Interop.NCrypt.PBE_PARAMS.RgbSaltSize);
                    RandomNumberGenerator.Fill(salt);
                    pbeParams.Params.cbSalt = salt.Length;
                    pbeParams.Params.iIterations = kdfCount;

                    buffers[0] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsSecret,
                        cbBuffer = checked(2 * (password.Length + 1)),
                        pvBuffer = stringHandle.DangerousGetHandle(),
                    };

                    if (buffers[0].pvBuffer == IntPtr.Zero)
                    {
                        buffers[0].cbBuffer = 0;
                    }

                    buffers[1] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsAlgOid,
                        cbBuffer = s_pkcs12TripleDesOidBytes.Length,
                        pvBuffer = (IntPtr)oidPtr,
                    };

                    buffers[2] = new Interop.NCrypt.NCryptBuffer
                    {
                        BufferType = Interop.NCrypt.BufferType.PkcsAlgParam,
                        cbBuffer = sizeof(Interop.NCrypt.PBE_PARAMS),
                        pvBuffer = (IntPtr)(&pbeParams),
                    };

                    Interop.NCrypt.NCryptBufferDesc desc = new Interop.NCrypt.NCryptBufferDesc
                    {
                        cBuffers = 3,
                        pBuffers = (IntPtr)buffers,
                        ulVersion = 0,
                    };

                    Span<byte> empty = default;

                    ErrorCode errorCode = Interop.NCrypt.NCryptExportKey(
                        keyHandle,
                        IntPtr.Zero,
                        Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                        ref desc,
                        ref MemoryMarshal.GetReference(empty),
                        0,
                        out int numBytesNeeded,
                        0);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }

                    allocated = null;

                    if (allocate)
                    {
                        allocated = new byte[numBytesNeeded];
                        destination = allocated;
                    }
                    else if (numBytesNeeded > destination.Length)
                    {
                        bytesWritten = 0;
                        return false;
                    }

                    errorCode = Interop.NCrypt.NCryptExportKey(
                        keyHandle,
                        IntPtr.Zero,
                        Interop.NCrypt.NCRYPT_PKCS8_PRIVATE_KEY_BLOB,
                        ref desc,
                        ref MemoryMarshal.GetReference(destination),
                        destination.Length,
                        out numBytesNeeded,
                        0);

                    if (errorCode != ErrorCode.ERROR_SUCCESS)
                    {
                        throw errorCode.ToCryptographicException();
                    }

                    if (allocate && numBytesNeeded != destination.Length)
                    {
                        byte[] trimmed = new byte[numBytesNeeded];
                        destination.Slice(0, numBytesNeeded).CopyTo(trimmed);
                        Array.Clear(allocated!, 0, numBytesNeeded);
                        allocated = trimmed;
                    }

                    bytesWritten = numBytesNeeded;
                    return true;
                }
            }
        }
    }
}
