// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ECDSA_sign(int type, [In] byte[] dgst, int dlen, [Out] byte[] sig, [In, Out] ref int siglen, SafeEcKeyHandle ecKey);

        /*-
         * returns
         *      1: correct signature
         *      0: incorrect signature
         *     -1: error
         */
        [DllImport(Libraries.LibCrypto)]
        internal static extern int ECDSA_verify(int type, [In] byte[] dgst, int dgst_len, [In] byte[] sigbuf, int sig_len, SafeEcKeyHandle ecKey);

        [DllImport(Libraries.LibCrypto)]
        internal static extern int ECDSA_size(SafeEcKeyHandle ecKey);  // returns the maximum length of a DER encoded ECDSA signature created with this key.
    }
}
