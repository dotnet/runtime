// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using BCRYPT_ECCKEY_BLOB = Interop.BCrypt.BCRYPT_ECCKEY_BLOB;
using ErrorCode = Interop.NCrypt.ErrorCode;
using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

namespace System.Security.Cryptography
{
    internal static partial class ECCng
    {
        internal delegate TResult EncodeBlobFunc<TResult>(byte[] blob);

        // When possible, operate on the blob inside the callback since the array will be pinned during its execution.
        internal static T EncodeEccKeyBlob<T>(KeyBlobMagicNumber magic, byte[] x, byte[] y, byte[]? d, Func<byte[], T> encodeCallback, bool clearBlob = true)
        {
            bool includePrivateParameters = d is not null;
            byte[] blob;
            unsafe
            {
                // We need to build a key blob structured as follows:
                //     BCRYPT_ECCKEY_BLOB   header
                //     byte[cbKey]          Q.X
                //     byte[cbKey]          Q.Y
                //     -- Only if "includePrivateParameters" is true --
                //     byte[cbKey]          D

                Debug.Assert(x.Length == y.Length);

                int blobSize;

                checked
                {
                    blobSize = sizeof(BCRYPT_ECCKEY_BLOB) +
                        x.Length +
                        y.Length;
                    if (includePrivateParameters)
                    {
                        blobSize += d!.Length;
                    }
                }

                blob = new byte[blobSize];
                fixed (byte* pBlob = &blob[0])
                {
                    try
                    {
                        // Build the header
                        BCRYPT_ECCKEY_BLOB* pBcryptBlob = (BCRYPT_ECCKEY_BLOB*)pBlob;
                        pBcryptBlob->Magic = magic;
                        pBcryptBlob->cbKey = x.Length;

                        // Emit the blob
                        int offset = sizeof(BCRYPT_ECCKEY_BLOB);
                        Interop.BCrypt.Emit(blob, ref offset, x);
                        Interop.BCrypt.Emit(blob, ref offset, y);

                        if (includePrivateParameters)
                        {
                            Debug.Assert(x.Length == d?.Length);
                            Interop.BCrypt.Emit(blob, ref offset, d!);
                        }

                        // We better have computed the right allocation size above!
                        Debug.Assert(offset == blobSize);
                        return encodeCallback(blob);
                    }
                    finally
                    {
                        if (clearBlob)
                        {
                            CryptographicOperations.ZeroMemory(blob);
                        }
                    }
                }
            }
        }

        internal delegate T DecodeBlobFunc<T>(KeyBlobMagicNumber magic, byte[] x, byte[] y, byte[]? d);

        // When possible, operate on the private key inside the callback since the array will be pinned during its execution.
        internal static T DecodeEccKeyBlob<T>(ReadOnlySpan<byte> ecBlob, DecodeBlobFunc<T> decodeCallback, bool clearPrivateKey = true)
        {
            // We now have a buffer laid out as follows:
            //     BCRYPT_ECCKEY_BLOB   header
            //     byte[cbKey]          Q.X
            //     byte[cbKey]          Q.Y
            //     -- Private only --
            //     byte[cbKey]          D

            KeyBlobMagicNumber magic = (KeyBlobMagicNumber)MemoryMarshal.Cast<byte, int>(ecBlob)[0];

            unsafe
            {
                // Fail-fast if a rogue provider gave us a blob that isn't even the size of the blob header.
                if (ecBlob.Length < sizeof(BCRYPT_ECCKEY_BLOB))
                    throw ErrorCode.E_FAIL.ToCryptographicException();

                fixed (byte* pEcBlob = &ecBlob[0])
                {
                    BCRYPT_ECCKEY_BLOB* pBcryptBlob = (BCRYPT_ECCKEY_BLOB*)pEcBlob;

                    int offset = sizeof(BCRYPT_ECCKEY_BLOB);

                    byte[] x = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey);
                    byte[] y = Interop.BCrypt.Consume(ecBlob, ref offset, pBcryptBlob->cbKey);

                    if (offset < ecBlob.Length)
                    {
                        byte[] d = new byte[pBcryptBlob->cbKey];

                        fixed (byte* pinnedD = d)
                        {
                            try
                            {
                                Interop.BCrypt.Consume(ecBlob, ref offset, d.Length, d);

                                Debug.Assert(offset == ecBlob.Length);

                                return decodeCallback(magic, x, y, d);
                            }
                            finally
                            {
                                if (clearPrivateKey)
                                {
                                    CryptographicOperations.ZeroMemory(d);
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.Assert(offset == ecBlob.Length);

                        return decodeCallback(magic, x, y, null);
                    }
                }
            }
        }

        internal static SafeNCryptKeyHandle ImportKeyBlob(
            string blobType,
            ReadOnlySpan<byte> keyBlob,
            string curveName,
            SafeNCryptProviderHandle provider)
        {
            ErrorCode errorCode;
            SafeNCryptKeyHandle keyHandle;

            using (SafeUnicodeStringHandle safeCurveName = new SafeUnicodeStringHandle(curveName))
            {
                unsafe
                {
                    Interop.BCrypt.BCryptBufferDesc desc = default;
                    Interop.BCrypt.BCryptBuffer buff = default;

                    buff.cbBuffer = (curveName.Length + 1) * 2; // Add 1 for null terminator
                    buff.BufferType = Interop.BCrypt.CngBufferDescriptors.NCRYPTBUFFER_ECC_CURVE_NAME;
                    buff.pvBuffer = safeCurveName.DangerousGetHandle();

                    desc.cBuffers = 1;
                    desc.pBuffers = (IntPtr)(&buff);
                    desc.ulVersion = Interop.BCrypt.BCRYPTBUFFER_VERSION;

                    errorCode = Interop.NCrypt.NCryptImportKey(
                        provider,
                        IntPtr.Zero,
                        blobType,
                        (IntPtr)(&desc),
                        out keyHandle,
                        ref MemoryMarshal.GetReference(keyBlob),
                        keyBlob.Length,
                        0);
                }
            }

            if (errorCode != ErrorCode.ERROR_SUCCESS)
            {
                Exception e = errorCode.ToCryptographicException();
                keyHandle.Dispose();
                if (errorCode == ErrorCode.NTE_INVALID_PARAMETER)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curveName), e);
                }
                throw e;
            }

            return keyHandle;
        }
    }
}
