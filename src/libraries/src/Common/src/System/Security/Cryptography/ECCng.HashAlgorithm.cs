// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using static Interop.Crypt32;

namespace System.Security.Cryptography
{
    internal static partial class ECCng
    {
        /// <summary>
        /// Get the ALG_ID from the given HashAlgorithmName
        /// </summary>
        internal static Interop.BCrypt.ECC_CURVE_ALG_ID_ENUM GetHashAlgorithmId(HashAlgorithmName? name)
        {
            if (name.HasValue == false || string.IsNullOrEmpty(name.Value.Name))
            {
                return Interop.BCrypt.ECC_CURVE_ALG_ID_ENUM.BCRYPT_NO_CURVE_GENERATION_ALG_ID;
            }

            CRYPT_OID_INFO oid = Interop.Crypt32.FindOidInfo(
                CryptOidInfoKeyType.CRYPT_OID_INFO_NAME_KEY,
                name.Value.Name,
                OidGroup.HashAlgorithm,
                false);

            if (oid.AlgId == -1)
            {
                throw new CryptographicException(SR.Cryptography_UnknownHashAlgorithm, name.Value.Name);
            }

            return (Interop.BCrypt.ECC_CURVE_ALG_ID_ENUM)oid.AlgId;
        }

        /// <summary>
        /// Get the HashAlgorithmName from the given ALG_ID
        /// </summary>
        internal static HashAlgorithmName? GetHashAlgorithmName(Interop.BCrypt.ECC_CURVE_ALG_ID_ENUM hashId)
        {
            CRYPT_OID_INFO oid = Interop.Crypt32.FindAlgIdOidInfo(hashId);
            if (oid.AlgId == -1)
            {
                // The original hash algorithm may not be found and is optional
                return null;
            }

            return new HashAlgorithmName(oid.Name);
        }
    }
}
