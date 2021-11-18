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
        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaUpRef")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DsaUpRef(IntPtr dsa);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaDestroy")]
        internal static partial void DsaDestroy(IntPtr dsa);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaGenerateKey")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool DsaGenerateKey(out SafeDsaHandle dsa, int bits);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaSizeSignature")]
        private static partial int DsaSizeSignature(SafeDsaHandle dsa);

        /// <summary>
        /// Return the maximum size of the DER-encoded key in bytes.
        /// </summary>
        internal static int DsaEncodedSignatureSize(SafeDsaHandle dsa)
        {
            int size = DsaSizeSignature(dsa);
            return size;
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaSizeQ")]
        private static partial int DsaSizeQ(SafeDsaHandle dsa);

        /// <summary>
        /// Return the size of the 'r' or 's' signature fields in bytes.
        /// </summary>
        internal static int DsaSignatureFieldSize(SafeDsaHandle dsa)
        {
            int size = DsaSizeQ(dsa);
            Debug.Assert(size * 2 < DsaEncodedSignatureSize(dsa));
            return size;
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaSizeP")]
        private static partial int DsaSizeP(SafeDsaHandle dsa);

        /// <summary>
        /// Return the size of the key in bytes.
        /// </summary>
        internal static int DsaKeySize(SafeDsaHandle dsa)
        {
            int keySize = DsaSizeP(dsa);

            // Assume an even multiple of 8 bytes \ 64 bits (OpenSsl also makes the same assumption)
            keySize = (keySize + 7) / 8 * 8;
            return keySize;
        }

        internal static bool DsaSign(SafeDsaHandle dsa, ReadOnlySpan<byte> hash, Span<byte> refSignature, out int outSignatureLength) =>
            DsaSign(dsa, ref MemoryMarshal.GetReference(hash), hash.Length, ref MemoryMarshal.GetReference(refSignature), out outSignatureLength);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DsaSign(SafeDsaHandle dsa, ref byte hash, int hashLength, ref byte refSignature, out int outSignatureLength);

        internal static bool DsaVerify(SafeDsaHandle dsa, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature)
        {
            bool ret = DsaVerify(
                dsa,
                ref MemoryMarshal.GetReference(hash),
                hash.Length,
                ref MemoryMarshal.GetReference(signature),
                signature.Length);

            // Error queue already cleaned on the native function.

            return ret;
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaVerify")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool DsaVerify(SafeDsaHandle dsa, ref byte hash, int hashLength, ref byte signature, int signatureLength);

        internal static DSAParameters ExportDsaParameters(SafeDsaHandle key, bool includePrivateParameters)
        {
            Debug.Assert(
                key != null && !key.IsInvalid,
                "Callers should check the key is invalid and throw an exception with a message");

            if (key == null || key.IsInvalid)
            {
                throw new CryptographicException();
            }

            IntPtr p_bn, q_bn, g_bn, y_bn, x_bn; // these are not owned
            int    p_cb, q_cb, g_cb, y_cb, x_cb;

            bool refAdded = false;
            try
            {
                key.DangerousAddRef(ref refAdded); // Protect access to the *_bn variables

                if (!GetDsaParameters(key,
                    out p_bn, out p_cb,
                    out q_bn, out q_cb,
                    out g_bn, out g_cb,
                    out y_bn, out y_cb,
                    out x_bn, out x_cb))
                {
                    throw new CryptographicException();
                }

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
            finally
            {
                if (refAdded)
                    key.DangerousRelease();
            }
        }

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_GetDsaParameters")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool GetDsaParameters(
            SafeDsaHandle key,
            out IntPtr p, out int p_cb,
            out IntPtr q, out int q_cb,
            out IntPtr g, out int g_cb,
            out IntPtr y, out int y_cb,
            out IntPtr x, out int x_cb);

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_DsaKeyCreateByExplicitParameters")]
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

        /// <summary>
        /// Return the maximum value in the array; assumes non-negative values.
        /// </summary>
        private static int GetMax(int[] values)
        {
            int max = 0;

            foreach (var i in values)
            {
                Debug.Assert(i >= 0);
                if (i > max)
                    max = i;
            }

            return max;
        }

        /// <summary>
        /// Return the maximum value in the array; assumes non-negative values.
        /// </summary>
        private static int GetMax(int value1, int value2)
        {
            Debug.Assert(value1 >= 0);
            Debug.Assert(value2 >= 0);
            return (value1 > value2 ? value1 : value2);
        }

        /// <summary>
        /// Return the maximum value in the array; assumes non-negative values.
        /// </summary>
        private static int GetMax(int value1, int value2, int value3)
        {
            return GetMax(GetMax(value1, value2), value3);
        }
    }
}
