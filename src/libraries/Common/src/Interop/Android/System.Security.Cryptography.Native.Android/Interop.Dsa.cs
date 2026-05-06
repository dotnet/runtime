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
        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaGenerateKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DsaGenerateKey(out SafeDsaHandle dsa, int bits);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaSizeSignature")]
        private static partial int DsaSizeSignature(SafeDsaHandle dsa);

        /// <summary>
        /// Return the maximum size of the DER-encoded key in bytes.
        /// </summary>
        internal static int DsaEncodedSignatureSize(SafeDsaHandle dsa)
        {
            int size = DsaSizeSignature(dsa);
            return size;
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaSignatureFieldSize")]
        private static partial int AndroidCryptoNative_DsaSignatureFieldSize(SafeDsaHandle dsa);

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

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaSizeP")]
        private static partial int DsaSizeP(SafeDsaHandle dsa);

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

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DsaSign(SafeDsaHandle dsa, ref byte hash, int hashLength, ref byte refSignature, out int outSignatureLength);

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

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaVerify")]
        private static partial int DsaVerify(SafeDsaHandle dsa, ref byte hash, int hashLength, ref byte signature, int signatureLength);

        internal static DSAParameters ExportDsaParameters(SafeDsaHandle key, bool includePrivateParameters)
        {
            Debug.Assert(
                key != null && !key.IsInvalid,
                "Callers should check the key is invalid and throw an exception with a message");

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException();
            }

            SafeBignumHandle p_bn, q_bn, g_bn, y_bn, x_bn;
            int    p_cb, q_cb, g_cb, y_cb, x_cb;

            if (!GetDsaParameters(key,
                out p_bn, out p_cb,
                out q_bn, out q_cb,
                out g_bn, out g_cb,
                out y_bn, out y_cb,
                out x_bn, out x_cb))
            {
                p_bn.Dispose();
                q_bn.Dispose();
                g_bn.Dispose();
                y_bn.Dispose();
                x_bn.Dispose();
                throw new CryptographicException();
            }

            using (p_bn)
            using (q_bn)
            using (g_bn)
            using (y_bn)
            using (x_bn)
            {
                // Match Windows semantics where p, g and y have same length
                int pgy_cb = GetMax(p_cb, g_cb, y_cb);

                // Match Windows semantics where q and x have same length
                int qx_cb = GetMax(q_cb, x_cb);

                DSAParameters dsaParameters = new DSAParameters
                {
                    P = Crypto.ExtractBignum(p_bn, pgy_cb)!,
                    Q = Crypto.ExtractBignum(q_bn, qx_cb)!,
                    G = Crypto.ExtractBignum(g_bn, pgy_cb)!,
                    Y = Crypto.ExtractBignum(y_bn, pgy_cb)!,
                };

                if (includePrivateParameters)
                {
                    dsaParameters.X = Crypto.ExtractBignum(x_bn, qx_cb);
                }

                return dsaParameters;
            }
        }

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_GetDsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetDsaParameters(
            SafeDsaHandle key,
            out SafeBignumHandle p, out int p_cb,
            out SafeBignumHandle q, out int q_cb,
            out SafeBignumHandle g, out int g_cb,
            out SafeBignumHandle y, out int y_cb,
            out SafeBignumHandle x, out int x_cb);

        [LibraryImport(Libraries.AndroidCryptoNative, EntryPoint = "AndroidCryptoNative_DsaKeyCreateByExplicitParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DsaKeyCreateByExplicitParameters(
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

            var duplicate = new SafeDsaHandle();
            duplicate.SetHandle(Interop.JObjectLifetime.NewGlobalReference(handle));
            return duplicate;
        }

        internal override SafeDsaHandle DuplicateHandle() => DuplicateHandle(DangerousGetHandle());
    }
}
