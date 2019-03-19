// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.NativeCrypto;
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

        /// <summary>
        /// Is the curve named, or once of the special nist curves
        /// </summary>
        internal static bool IsECNamedCurve(string algorithm)
        {
            return (algorithm == BCryptNative.AlgorithmName.ECDH ||
                    algorithm == BCryptNative.AlgorithmName.ECDsa);
        }

        /// <summary>
        /// Maps algorithm to curve name accounting for the special nist curves
        /// </summary>
        internal static string SpecialNistAlgorithmToCurveName(string algorithm, out string oidValue)
        {
            switch (algorithm)
            {
                case BCryptNative.AlgorithmName.ECDHP256:
                case BCryptNative.AlgorithmName.ECDsaP256:
                    oidValue = Oids.secp256r1;
                    return "nistP256";
                case BCryptNative.AlgorithmName.ECDHP384:
                case BCryptNative.AlgorithmName.ECDsaP384:
                    oidValue = Oids.secp384r1;
                    return "nistP384";
                case BCryptNative.AlgorithmName.ECDHP521:
                case BCryptNative.AlgorithmName.ECDsaP521:
                    oidValue = Oids.secp521r1;
                    return "nistP521";
            }
            
            Debug.Fail($"Unknown curve {algorithm}");
            throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, algorithm));
        }
    }
}
