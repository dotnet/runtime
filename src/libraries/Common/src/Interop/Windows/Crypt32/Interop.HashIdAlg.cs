// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypt32
    {
        /// <summary>
        /// Version used for a buffer containing a scalar integer (not an IntPtr)
        /// </summary>
        [LibraryImport(Libraries.Crypt32)]
        private static unsafe partial CRYPT_OID_INFO* CryptFindOIDInfo(CryptOidInfoKeyType dwKeyType, void* pvKey, OidGroup group);

        public static unsafe CRYPT_OID_INFO FindAlgIdOidInfo(Interop.BCrypt.ECC_CURVE_ALG_ID_ENUM algId)
        {
            CRYPT_OID_INFO* fullOidInfo = CryptFindOIDInfo(
                CryptOidInfoKeyType.CRYPT_OID_INFO_ALGID_KEY,
                &algId,
                OidGroup.HashAlgorithm);

            if (fullOidInfo != null)
            {
                return *fullOidInfo;
            }

            // Otherwise the lookup failed.
            return new CRYPT_OID_INFO() { AlgId = -1 };
        }
    }
}
