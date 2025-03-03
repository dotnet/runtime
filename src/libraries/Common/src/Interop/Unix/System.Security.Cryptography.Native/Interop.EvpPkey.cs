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

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPkeyDestroy")]
        internal static partial void EvpPkeyDestroy(IntPtr pkey, IntPtr extraHandle);

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyBits")]
        internal static partial int EvpPKeyBits(SafeEvpPKeyHandle pkey);

        internal static int GetEvpPKeySizeBytes(SafeEvpPKeyHandle pkey)
        {
            // EVP_PKEY_size returns the maximum suitable size for the output buffers for almost all operations that can be done with the key.
            // For most of the OpenSSL 'default' provider keys it will return the same size as this method,
            // but other providers such as 'tpm2' it may return larger size.
            // Instead we will round up EVP_PKEY_bits result.
            int keySizeBits = Interop.Crypto.EvpPKeyBits(pkey);

            if (keySizeBits <= 0)
            {
                Debug.Fail($"EVP_PKEY_bits returned non-positive value: {keySizeBits}");
                throw new CryptographicException();
            }

            return (keySizeBits + 7) / 8;
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_UpRefEvpPkey")]
        private static partial int UpRefEvpPkey(SafeEvpPKeyHandle handle, IntPtr extraHandle);

        internal static int UpRefEvpPkey(SafeEvpPKeyHandle handle)
        {
            return UpRefEvpPkey(handle, handle.ExtraHandle);
        }

        [LibraryImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EvpPKeyType")]
        internal static partial EvpAlgorithmId EvpPKeyType(SafeEvpPKeyHandle handle);

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

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_LoadPrivateKeyFromEngine(
            string engineName,
            string keyName,
            [MarshalAs(UnmanagedType.Bool)] out bool haveEngine);

        internal static SafeEvpPKeyHandle LoadPrivateKeyFromEngine(
            string engineName,
            string keyName)
        {
            Debug.Assert(engineName is not null);
            Debug.Assert(keyName is not null);

            SafeEvpPKeyHandle pkey = CryptoNative_LoadPrivateKeyFromEngine(engineName, keyName, out bool haveEngine);

            if (!haveEngine)
            {
                pkey.Dispose();
                throw new CryptographicException(SR.Cryptography_EnginesNotSupported);
            }

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial SafeEvpPKeyHandle CryptoNative_LoadPublicKeyFromEngine(
            string engineName,
            string keyName,
            [MarshalAs(UnmanagedType.Bool)] out bool haveEngine);

        internal static SafeEvpPKeyHandle LoadPublicKeyFromEngine(
            string engineName,
            string keyName)
        {
            Debug.Assert(engineName is not null);
            Debug.Assert(keyName is not null);

            SafeEvpPKeyHandle pkey = CryptoNative_LoadPublicKeyFromEngine(engineName, keyName, out bool haveEngine);

            if (!haveEngine)
            {
                pkey.Dispose();
                throw new CryptographicException(SR.Cryptography_EnginesNotSupported);
            }

            if (pkey.IsInvalid)
            {
                pkey.Dispose();
                throw CreateOpenSslCryptographicException();
            }

            return pkey;
        }

        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr CryptoNative_LoadKeyFromProvider(
            string providerName,
            string keyUri,
            ref IntPtr extraHandle,
            [MarshalAs(UnmanagedType.Bool)] out bool haveProvider);

        internal static SafeEvpPKeyHandle LoadKeyFromProvider(
            string providerName,
            string keyUri)
        {
            IntPtr extraHandle = IntPtr.Zero;
            IntPtr evpPKeyHandle = IntPtr.Zero;

            try
            {
                evpPKeyHandle = CryptoNative_LoadKeyFromProvider(providerName, keyUri, ref extraHandle, out bool haveProvider);

                if (!haveProvider)
                {
                    Debug.Assert(evpPKeyHandle == IntPtr.Zero && extraHandle == IntPtr.Zero, "both handles should be null if provider is not supported");
                    throw new PlatformNotSupportedException(SR.PlatformNotSupported_CryptographyOpenSSLProvidersNotSupported);
                }

                if (evpPKeyHandle == IntPtr.Zero || extraHandle == IntPtr.Zero)
                {
                    Debug.Assert(evpPKeyHandle == IntPtr.Zero, "extraHandle should not be null if evpPKeyHandle is not null");
                    throw CreateOpenSslCryptographicException();
                }

                return new SafeEvpPKeyHandle(evpPKeyHandle, extraHandle: extraHandle);
            }
            catch
            {
                if (evpPKeyHandle != IntPtr.Zero || extraHandle != IntPtr.Zero)
                {
                    EvpPkeyDestroy(evpPKeyHandle, extraHandle);
                }

                throw;
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
