// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        internal const int NID_undef = 0;

        internal const int NID_X9_62_prime256v1 = 415; // NIST P-256
        internal const int NID_secp224r1 = 713; // NIST P-224
        internal const int NID_secp384r1 = 715; // NIST P-384
        internal const int NID_secp521r1 = 716; // NIST P-521

        [DllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ObjSn2Nid", CharSet = CharSet.Ansi)]
        internal static extern int ObjSn2Nid(string sn);
    }
}
