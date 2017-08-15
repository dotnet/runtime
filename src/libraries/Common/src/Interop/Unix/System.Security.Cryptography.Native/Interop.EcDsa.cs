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
        internal static unsafe bool EcDsaSign(ReadOnlySpan<byte> dgst, int dlen, Span<byte> sig, [In, Out] ref int siglen, SafeEcKeyHandle ecKey)
        {
            fixed (byte* dgstPtr = &dgst.DangerousGetPinnableReference())
            fixed (byte* sigPtr = &sig.DangerousGetPinnableReference())
            {
                return EcDsaSign(dgstPtr, dlen, sigPtr, ref siglen, ecKey);
            }
        }

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern unsafe bool EcDsaSign([In] byte* dgst, int dlen, [Out] byte* sig, [In, Out] ref int siglen, SafeEcKeyHandle ecKey);

        internal static unsafe int EcDsaVerify(ReadOnlySpan<byte> dgst, int dgst_len, ReadOnlySpan<byte> sigbuf, int sig_len, SafeEcKeyHandle ecKey)
        {
            fixed (byte* dgstPtr = &dgst.DangerousGetPinnableReference())
            fixed (byte* sigbufPtr = &sigbuf.DangerousGetPinnableReference())
            {
                return EcDsaVerify(dgstPtr, dgst_len, sigbufPtr, sig_len, ecKey);
            }
        }

        /*-
         * returns
         *      1: correct signature
         *      0: incorrect signature
         *     -1: error
         */
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaVerify")]
        private static extern unsafe int EcDsaVerify(byte* dgst, int dgst_len, byte* sigbuf, int sig_len, SafeEcKeyHandle ecKey);

        // returns the maximum length of a DER encoded ECDSA signature created with this key.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaSize")]
        internal static extern int EcDsaSize(SafeEcKeyHandle ecKey);
    }
}
