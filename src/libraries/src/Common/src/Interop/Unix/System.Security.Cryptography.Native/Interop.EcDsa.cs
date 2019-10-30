// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal static bool EcDsaSign(ReadOnlySpan<byte> dgst, Span<byte> sig, [In, Out] ref int siglen, SafeEcKeyHandle ecKey) =>
            EcDsaSign(ref MemoryMarshal.GetReference(dgst), dgst.Length, ref MemoryMarshal.GetReference(sig), ref siglen, ecKey);

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EcDsaSign(ref byte dgst, int dlen, ref byte sig, [In, Out] ref int siglen, SafeEcKeyHandle ecKey);

        internal static int EcDsaVerify(ReadOnlySpan<byte> dgst, ReadOnlySpan<byte> sigbuf, SafeEcKeyHandle ecKey)
        {
            int ret = EcDsaVerify(
                ref MemoryMarshal.GetReference(dgst),
                dgst.Length,
                ref MemoryMarshal.GetReference(sigbuf),
                sigbuf.Length,
                ecKey);

            if (ret < 0)
            {
                ErrClearError();
            }

            return ret;
        }

        /*-
         * returns
         *      1: correct signature
         *      0: incorrect signature
         *     -1: error
         */
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaVerify")]
        private static extern int EcDsaVerify(ref byte dgst, int dgst_len, ref byte sigbuf, int sig_len, SafeEcKeyHandle ecKey);

        // returns the maximum length of a DER encoded ECDSA signature created with this key.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaSize")]
        private static extern int CryptoNative_EcDsaSize(SafeEcKeyHandle ecKey);

        internal static int EcDsaSize(SafeEcKeyHandle ecKey)
        {
            int ret = CryptoNative_EcDsaSize(ecKey);

            if (ret == 0)
            {
                throw CreateOpenSslCryptographicException();
            }

            return ret;
        }
    }
}
