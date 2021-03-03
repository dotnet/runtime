// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class AndroidCrypto
    {
        internal static bool EcDsaSign(ReadOnlySpan<byte> dgst, Span<byte> sig, out int siglen, SafeEcKeyHandle ecKey) =>
            EcDsaSign(ref MemoryMarshal.GetReference(dgst), dgst.Length, ref MemoryMarshal.GetReference(sig), out siglen, ecKey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcDsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EcDsaSign(ref byte dgst, int dlen, ref byte sig, out int siglen, SafeEcKeyHandle ecKey);

        internal static int EcDsaVerify(ReadOnlySpan<byte> dgst, ReadOnlySpan<byte> sigbuf, SafeEcKeyHandle ecKey)
        {
            int ret = EcDsaVerify(
                ref MemoryMarshal.GetReference(dgst),
                dgst.Length,
                ref MemoryMarshal.GetReference(sigbuf),
                sigbuf.Length,
                ecKey);

            return ret;
        }

        /*-
         * returns
         *      1: correct signature
         *      0: incorrect signature
         *     -1: error
         */
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcDsaVerify")]
        private static extern int EcDsaVerify(ref byte dgst, int dgst_len, ref byte sigbuf, int sig_len, SafeEcKeyHandle ecKey);

        // returns the maximum length of a DER encoded ECDSA signature created with this key.
        [DllImport(Libraries.CryptoNative, EntryPoint = "AndroidCryptoNative_EcDsaSize")]
        private static extern int AndroidCryptoNative_EcDsaSize(SafeEcKeyHandle ecKey);

        internal static int EcDsaSize(SafeEcKeyHandle ecKey)
        {
            int ret = AndroidCryptoNative_EcDsaSize(ecKey);

            if (ret == 0)
            {
                throw new CryptographicException();
            }

            return ret;
        }
    }
}
