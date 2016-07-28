// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal partial class Interop
{
    internal static partial class Crypt32
    {
        /// <summary>
        /// Version used for a buffer containing a scalar integer (not an IntPtr)
        /// </summary>
        [DllImport(Libraries.Crypt32, CharSet = CharSet.Unicode)]
        private static extern IntPtr CryptFindOIDInfo(CryptOidInfoKeyType dwKeyType, ref int pvKey, OidGroup group);

        public static CRYPT_OID_INFO FindAlgIdOidInfo(Interop.BCrypt.ECC_CURVE_ALG_ID_ENUM algId)
        {
            int intAlgId = (int)algId;
            IntPtr fullOidInfo = CryptFindOIDInfo(
                CryptOidInfoKeyType.CRYPT_OID_INFO_ALGID_KEY,
                ref intAlgId,
                OidGroup.HashAlgorithm);

            if (fullOidInfo != IntPtr.Zero)
            {
                return Marshal.PtrToStructure<CRYPT_OID_INFO>(fullOidInfo);
            }

            // Otherwise the lookup failed.
            return new CRYPT_OID_INFO() { AlgId = -1 };
        }
    }
}
