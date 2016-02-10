// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal partial class Interop
{
    //
    // These structures define the layout of CNG key blobs passed to NCryptImportKey
    //
    internal partial class BCrypt
    {
        /// <summary>
        ///     Magic numbers identifying blob types
        /// </summary>
        internal enum KeyBlobMagicNumber : int
        {
            BCRYPT_ECDH_PUBLIC_P256_MAGIC = 0x314B4345,
            BCRYPT_ECDH_PUBLIC_P384_MAGIC = 0x334B4345,
            BCRYPT_ECDH_PUBLIC_P521_MAGIC = 0x354B4345,
            BCRYPT_ECDSA_PUBLIC_P256_MAGIC = 0x31534345,
            BCRYPT_ECDSA_PUBLIC_P384_MAGIC = 0x33534345,
            BCRYPT_ECDSA_PUBLIC_P521_MAGIC = 0x35534345,
            BCRYPT_RSAPUBLIC_MAGIC = 0x31415352,
            BCRYPT_RSAPRIVATE_MAGIC = 0x32415352,
            BCRYPT_RSAFULLPRIVATE_MAGIC = 0x33415352,
            BCRYPT_KEY_DATA_BLOB_MAGIC = 0x4d42444b,
        }


        /// <summary>
        ///     Well known key blob types
        /// </summary>
        internal static class KeyBlobType
        {
            internal const string BCRYPT_RSAFULLPRIVATE_BLOB = "RSAFULLPRIVATEBLOB";
            internal const string BCRYPT_RSAPRIVATE_BLOB = "RSAPRIVATEBLOB";
            internal const string BCRYPT_PUBLIC_KEY_BLOB = "RSAPUBLICBLOB";
        }


        /// <summary>
        ///     The BCRYPT_RSAKEY_BLOB structure is used as a header for an RSA public key or private key BLOB in memory.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct BCRYPT_RSAKEY_BLOB
        {
            internal KeyBlobMagicNumber Magic;
            internal int BitLength;
            internal int cbPublicExp;
            internal int cbModulus;
            internal int cbPrime1;
            internal int cbPrime2;
        }
    }
}
