// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaSign")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EcDsaSign([In] byte[] dgst, int dlen, [Out] byte[] sig, [In, Out] ref int siglen, SafeEcKeyHandle ecKey);

        /*-
         * returns
         *      1: correct signature
         *      0: incorrect signature
         *     -1: error
         */
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaVerify")]
        internal static extern int EcDsaVerify([In] byte[] dgst, int dgst_len, [In] byte[] sigbuf, int sig_len, SafeEcKeyHandle ecKey);

        // returns the maximum length of a DER encoded ECDSA signature created with this key.
        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_EcDsaSize")]
        internal static extern int EcDsaSize(SafeEcKeyHandle ecKey);
    }
}
