// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyCreate")]
        internal static partial SafeEvpPKeyHandle EvpPkeyCreate();

        [LibraryImport(Libraries.CryptoNative)]
        private static partial SafeEvpPKeyHandle CryptoNative_EvpPKeyDuplicate(
            SafeEvpPKeyHandle currentKey,
            EvpAlgorithmId algorithmId);

        internal static SafeEvpPKeyHandle EvpPKeyDuplicate(
            SafeEvpPKeyHandle currentKey,
            EvpAlgorithmId algorithmId)
        {
            SafeEvpPKeyHandle pkey = CryptoNative_EvpPKeyDuplicate(
                currentKey,
                algorithmId);

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyDestroy")]
        internal static partial void EvpPkeyDestroy(IntPtr pkey);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeySize")]
        internal static partial int EvpPKeySize(SafeEvpPKeyHandle pkey);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_UpRefEvpPkey")]
        internal static partial int UpRefEvpPkey(SafeEvpPKeyHandle handle);

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial SafeEvpPKeyHandle CryptoNative_DecodeSubjectPublicKeyInfo(
            byte* buf,
            int len,
            int algId);

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial SafeEvpPKeyHandle CryptoNative_DecodePkcs8PrivateKey(
            byte* buf,
            int len,
            int algId);

        internal static unsafe SafeEvpPKeyHandle DecodeSubjectPublicKeyInfo(
            ReadOnlySpan<byte> source,
            EvpAlgorithmId algorithmId)
        {
            SafeEvpPKeyHandle handle;

            fixed (byte* sourcePtr = source)
            {
                handle = CryptoNative_DecodeSubjectPublicKeyInfo(
                    sourcePtr,
                    source.Length,
                    (int)algorithmId);
            }

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        internal static unsafe SafeEvpPKeyHandle DecodePkcs8PrivateKey(
            ReadOnlySpan<byte> source,
            EvpAlgorithmId algorithmId)
        {
            SafeEvpPKeyHandle handle;

            fixed (byte* sourcePtr = source)
            {
                handle = CryptoNative_DecodePkcs8PrivateKey(
                    sourcePtr,
                    source.Length,
                    (int)algorithmId);
            }

            if (handle.IsInvalid)
            {
                handle.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return handle;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_GetPkcs8PrivateKeySize(IntPtr pkey, out int p8size);

        private static int GetPkcs8PrivateKeySize(IntPtr pkey)
        {
            const int Success = 1;
            const int Error = -1;
            const int MissingPrivateKey = -2;

            int ret = CryptoNative_GetPkcs8PrivateKeySize(pkey, out int p8size);

            switch (ret)
            {
                case Success:
                    return p8size;
                case Error:
                    throw CreateOpenSslCryptographicException();
                case MissingPrivateKey:
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                default:
                    Debug.Fail($"Unexpected return '{ret}' value from {nameof(CryptoNative_GetPkcs8PrivateKeySize)}.");
                    throw new CryptographicException();
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial int CryptoNative_EncodePkcs8PrivateKey(IntPtr pkey, byte* buf);

        internal static ArraySegment<byte> RentEncodePkcs8PrivateKey(SafeEvpPKeyHandle pkey)
        {
            bool addedRef = false;

            try
            {
                pkey.DangerousAddRef(ref addedRef);
                IntPtr handle = pkey.DangerousGetHandle();

                int size = GetPkcs8PrivateKeySize(handle);
                byte[] rented = CryptoPool.Rent(size);
                int written;

                unsafe
                {
                    fixed (byte* buf = rented)
                    {
                        written = CryptoNative_EncodePkcs8PrivateKey(handle, buf);
                    }
                }

                Debug.Assert(written == size);
                return new ArraySegment<byte>(rented, 0, written);
            }
            finally
            {
                if (addedRef)
                {
                    pkey.DangerousRelease();
                }
            }
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_GetSubjectPublicKeyInfoSize(IntPtr pkey);

        private static int GetSubjectPublicKeyInfoSize(IntPtr pkey)
        {
            int ret = CryptoNative_GetSubjectPublicKeyInfoSize(pkey);

            if (ret < 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            return ret;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial int CryptoNative_EncodeSubjectPublicKeyInfo(IntPtr pkey, byte* buf);

        internal static ArraySegment<byte> RentEncodeSubjectPublicKeyInfo(SafeEvpPKeyHandle pkey)
        {
            bool addedRef = false;

            try
            {
                pkey.DangerousAddRef(ref addedRef);
                IntPtr handle = pkey.DangerousGetHandle();

                int size = GetSubjectPublicKeyInfoSize(handle);
                byte[] rented = CryptoPool.Rent(size);
                int written;

                unsafe
                {
                    fixed (byte* buf = rented)
                    {
                        written = CryptoNative_EncodeSubjectPublicKeyInfo(handle, buf);
                    }
                }

                Debug.Assert(written == size);
                return new ArraySegment<byte>(rented, 0, written);
            }
            finally
            {
                if (addedRef)
                {
                    pkey.DangerousRelease();
                }
            }
        }

        internal enum EvpAlgorithmId
        {
            Unknown = 0,
            RSA = 6,
            DSA = 116,
            ECC = 408,
        }
    }
}
