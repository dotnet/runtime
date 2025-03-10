// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

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

        /// <summary>
        /// Returns the OID as ASN1_OBJECT pointer.
        /// </summary>
        [LibraryImport(Libraries.CryptoNative, StringMarshalling = StringMarshalling.Utf8)]
        private static partial IntPtr CryptoNative_EvpPKeyGetEcGroupOid(SafeEvpPKeyHandle pkey);

        /// <summary>
        /// Returns the OID as a string or null if curve is not named.
        /// </summary>
        internal static string? EvpPKeyGetEcGroupName(SafeEvpPKeyHandle pkey)
        {
            IntPtr oid = CryptoNative_EvpPKeyGetEcGroupOid(pkey);

            if (oid == IntPtr.Zero)
            {
                return null;
            }

            return GetOidValue(oid);
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static partial int CryptoNative_EvpPKeyGetEcKeyParameters(
            SafeEvpPKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] bool includePrivate,
            out SafeBignumHandle qx_bn, out int x_cb,
            out SafeBignumHandle qy_bn, out int y_cb,
            out SafeBignumHandle d_bn, out int d_cb);

        internal static ECParameters EvpPKeyGetEcKeyParameters(
            SafeEvpPKeyHandle key,
            bool includePrivate)
        {
            SafeBignumHandle qx_bn, qy_bn, d_bn;
            int qx_cb, qy_cb, d_cb;
            ECParameters parameters = default;

            int rc = CryptoNative_EvpPKeyGetEcKeyParameters(
                key,
                includePrivate,
                out qx_bn, out qx_cb,
                out qy_bn, out qy_cb,
                out d_bn, out d_cb);

            using (qx_bn)
            using (qy_bn)
            using (d_bn)
            {
                if (rc == -1)
                {
                    throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                }
                else if (rc != 1)
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                // Match Windows semantics where qx, qy, and d have same length
                int expectedSize = GetEvpPKeySizeBytes(key);
                int cbKey = GetMax(qx_cb, qy_cb, d_cb);

                Debug.Assert(
                    cbKey <= expectedSize,
                    $"Expected output size was {expectedSize}, which a parameter exceeded. qx={qx_cb}, qy={qy_cb}, d={d_cb}");

                cbKey = GetMax(cbKey, expectedSize);

                parameters.Q = new ECPoint
                {
                    X = Crypto.ExtractBignum(qx_bn, cbKey),
                    Y = Crypto.ExtractBignum(qy_bn, cbKey)
                };
                parameters.D = d_cb == 0 ? null : Crypto.ExtractBignum(d_bn, cbKey);
            }

            return parameters;
        }

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial void CryptoNative_BufferFree(byte* ptr);

        [LibraryImport(Libraries.CryptoNative)]
        private static unsafe partial int CryptoNative_EvpPKeyGetEcCurveParameters(
            SafeEvpPKeyHandle key,
            [MarshalAs(UnmanagedType.Bool)] bool includePrivate,
            out ECCurve.ECCurveType curveType,
            out SafeBignumHandle qx, out int x_cb,
            out SafeBignumHandle qy, out int y_cb,
            out SafeBignumHandle d, out int d_cb,
            out SafeBignumHandle p, out int P_cb,
            out SafeBignumHandle a, out int A_cb,
            out SafeBignumHandle b, out int B_cb,
            out SafeBignumHandle gx, out int Gx_cb,
            out SafeBignumHandle gy, out int Gy_cb,
            out SafeBignumHandle order, out int order_cb,
            out SafeBignumHandle cofactor, out int cofactor_cb,
            out byte* seed, out int seed_cb);

        internal static unsafe ECParameters EvpPKeyGetEcCurveParameters(
            SafeEvpPKeyHandle key,
            bool includePrivate)
        {
            ECCurve.ECCurveType curveType;
            SafeBignumHandle qx_bn, qy_bn, d_bn, p_bn, a_bn, b_bn, gx_bn, gy_bn, order_bn, cofactor_bn;
            byte* seed = null;
            int qx_cb, qy_cb, p_cb, a_cb, b_cb, gx_cb, gy_cb, order_cb, cofactor_cb, seed_cb, d_cb;
            try
            {
                int rc = CryptoNative_EvpPKeyGetEcCurveParameters(
                    key,
                    includePrivate,
                    out curveType,
                    out qx_bn, out qx_cb,
                    out qy_bn, out qy_cb,
                    out d_bn, out d_cb,
                    out p_bn, out p_cb,
                    out a_bn, out a_cb,
                    out b_bn, out b_cb,
                    out gx_bn, out gx_cb,
                    out gy_bn, out gy_cb,
                    out order_bn, out order_cb,
                    out cofactor_bn, out cofactor_cb,
                    out seed, out seed_cb);

                using (qx_bn)
                using (qy_bn)
                using (d_bn)
                using (p_bn)
                using (a_bn)
                using (b_bn)
                using (gx_bn)
                using (gy_bn)
                using (order_bn)
                using (cofactor_bn)
                {
                    if (rc == -1)
                    {
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }
                    else if (rc != 1)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    int cbFieldLength;
                    int pFieldLength;
                    if (curveType == ECCurve.ECCurveType.Characteristic2)
                    {
                        // Match Windows semantics where a,b,gx,gy,qx,qy have same length
                        // Treat length of m separately as it is not tied to other fields for Char2 (Char2 not supported by Windows)
                        cbFieldLength = GetMax(new[] { a_cb, b_cb, gx_cb, gy_cb, qx_cb, qy_cb });
                        pFieldLength = p_cb;
                    }
                    else
                    {
                        // Match Windows semantics where p,a,b,gx,gy,qx,qy have same length
                        cbFieldLength = GetMax(new[] { p_cb, a_cb, b_cb, gx_cb, gy_cb, qx_cb, qy_cb });
                        pFieldLength = cbFieldLength;
                    }

                    // Match Windows semantics where order and d have same length
                    int cbSubgroupOrder = GetMax(order_cb, d_cb);

                    // Copy values to ECParameters
                    ECParameters parameters = default;
                    parameters.Q = new ECPoint
                    {
                        X = Crypto.ExtractBignum(qx_bn, cbFieldLength),
                        Y = Crypto.ExtractBignum(qy_bn, cbFieldLength)
                    };
                    parameters.D = d_cb == 0 ? null : Crypto.ExtractBignum(d_bn, cbSubgroupOrder);

                    var curve = parameters.Curve;
                    curve.CurveType = curveType;
                    curve.A = Crypto.ExtractBignum(a_bn, cbFieldLength)!;
                    curve.B = Crypto.ExtractBignum(b_bn, cbFieldLength)!;
                    curve.G = new ECPoint
                    {
                        X = Crypto.ExtractBignum(gx_bn, cbFieldLength),
                        Y = Crypto.ExtractBignum(gy_bn, cbFieldLength)
                    };
                    curve.Order = Crypto.ExtractBignum(order_bn, cbSubgroupOrder)!;

                    if (curveType == ECCurve.ECCurveType.Characteristic2)
                    {
                        curve.Polynomial = Crypto.ExtractBignum(p_bn, pFieldLength)!;
                    }
                    else
                    {
                        curve.Prime = Crypto.ExtractBignum(p_bn, pFieldLength)!;
                    }

                    // Optional parameters
                    curve.Cofactor = cofactor_cb == 0 ? null : Crypto.ExtractBignum(cofactor_bn, cofactor_cb);
                    Span<byte> seedSpan = new Span<byte>(seed, seed_cb);
                    curve.Seed = seedSpan.ToArray();

                    parameters.Curve = curve;
                    return parameters;
                }
            }
            finally
            {
                if (seed != null)
                    CryptoNative_BufferFree(seed);
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
