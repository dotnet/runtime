// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaGenerateKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DsaGenerateKey(out SafeDsaHandle dsa, int bits);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaSizeSignature")]
        private static extern int DsaSizeSignature(SafeDsaHandle dsa);

        /// <summary>
        /// Return the maximum size of the DER-encoded key in bytes.
        /// </summary>
        internal static int DsaEncodedSignatureSize(SafeDsaHandle dsa)
        {
            int size = DsaSizeSignature(dsa);
            return size;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaSignatureFieldSize")]
        private static extern int AndroidCryptoNative_DsaSignatureFieldSize(SafeDsaHandle dsa);

        /// <summary>
        /// Return the size of the 'r' or 's' signature fields in bytes.
        /// </summary>
        internal static int DsaSignatureFieldSize(SafeDsaHandle dsa)
        {
            // Add another byte for the leading zero byte.
            int size = AndroidCryptoNative_DsaSignatureFieldSize(dsa);
            Debug.Assert(size * 2 < DsaEncodedSignatureSize(dsa));
            return size;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaSizeP")]
        private static extern int DsaSizeP(SafeDsaHandle dsa);

        /// <summary>
        /// Return the size of the key in bytes.
        /// </summary>
        internal static int DsaKeySize(SafeDsaHandle dsa)
        {
            int keySize = DsaSizeP(dsa);

            // Assume an even multiple of 8 bytes \ 64 bits
            keySize = (keySize + 7) / 8 * 8;
            return keySize;
        }

        internal static bool DsaSign(SafeDsaHandle dsa, ReadOnlySpan<byte> hash, Span<byte> refSignature, out int outSignatureLength) =>
            DsaSign(dsa, ref MemoryMarshal.GetReference(hash), hash.Length, ref MemoryMarshal.GetReference(refSignature), out outSignatureLength);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DsaSign(SafeDsaHandle dsa, ref byte hash, int hashLength, ref byte refSignature, out int outSignatureLength);

        internal static bool DsaVerify(SafeDsaHandle dsa, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
        {
            int ret = DsaVerify(
                dsa,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(signature),
                signature.Length);

            if (ret == -1)
            {
                throw new CryptographicException();
            }

            return ret == 1;
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaVerify")]
        private static extern int DsaVerify(SafeDsaHandle dsa, ref byte hash, int hashLength, ref byte signature, int signatureLength);

        private struct AndroidDSAParameters : IDisposable
        {
            public SafeBignumHandle? p_bn;
            public SafeBignumHandle? q_bn;
            public SafeBignumHandle? g_bn;
            public SafeBignumHandle? y_bn;
            public SafeBignumHandle? x_bn;
            public int p_cb;
            public int q_cb;
            public int g_cb;
            public int y_cb;
            public int x_cb;

            public void Dispose()
            {
                p_bn?.Dispose();
                q_bn?.Dispose();
                g_bn?.Dispose();
                y_bn?.Dispose();
                x_bn?.Dispose();
            }
        };

        internal static DSAParameters ExportDsaParameters(SafeDsaHandle key, bool includePrivateParameters)
        {
            Debug.Assert(
                key != null && !key.IsInvalid,
                "Callers should check the key is invalid and throw an exception with a message");

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException();
            }

            AndroidDSAParameters parameters = default;

            if (!GetDsaParameters(key, out parameters))
            {
                parameters.Dispose();
                throw new CryptographicException();
            }

            using (parameters)
            {
                // Match Windows semantics where p, g and y have same length
                int pgy_cb = GetMax(parameters.p_cb, parameters.g_cb, parameters.y_cb);

                // Match Windows semantics where q and x have same length
                int qx_cb = GetMax(parameters.q_cb, parameters.x_cb);

                DSAParameters dsaParameters = new DSAParameters
                {
                    P = Crypto.ExtractBignum(parameters.p_bn, pgy_cb)!,
                    Q = Crypto.ExtractBignum(parameters.q_bn, qx_cb)!,
                    G = Crypto.ExtractBignum(parameters.g_bn, pgy_cb)!,
                    Y = Crypto.ExtractBignum(parameters.y_bn, pgy_cb)!,
                };

                if (includePrivateParameters)
                {
                    dsaParameters.X = Crypto.ExtractBignum(parameters.x_bn, qx_cb);
                }

                return dsaParameters;
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_GetDsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDsaParameters(SafeDsaHandle key, out AndroidDSAParameters parameters);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_DsaKeyCreateByExplicitParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DsaKeyCreateByExplicitParameters(
            out SafeDsaHandle dsa,
            byte[] p,
            int pLength,
            byte[] q,
            int qLength,
            byte[] g,
            int gLength,
            byte[] y,
            int yLength,
            byte[]? x,
            int xLength);
    }
}

namespace System.Security.Cryptography
{
    internal sealed class SafeDsaHandle : SafeKeyHandle
    {
        public SafeDsaHandle()
        {
        }

        internal SafeDsaHandle(IntPtr ptr)
        {
            SetHandle(ptr);
        }

        protected override bool ReleaseHandle()
        {
            Interop.JObjectLifetime.DeleteGlobalReference(handle);
            SetHandle(IntPtr.Zero);
            return true;
        }

        internal static SafeDsaHandle DuplicateHandle(IntPtr handle)
        {
            Debug.Assert(handle != IntPtr.Zero);

            return new SafeDsaHandle(Interop.JObjectLifetime.NewGlobalReference(handle));
        }

        internal override SafeDsaHandle DuplicateHandle() => DuplicateHandle(DangerousGetHandle());
    }
}
