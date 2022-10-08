// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static partial class CapiHelper
    {
        // For backwards compat with CapiHelper.ObjToHashAlgorithm, use "hashAlg" as name
        internal static HashAlgorithmName ObjToHashAlgorithmName(object hashAlg)
        {
            ArgumentNullException.ThrowIfNull(hashAlg);

            return hashAlg switch
            {
                string hashAlgString => NameOrOidToHashAlgorithmName(hashAlgString),
                HashAlgorithm hashAlgorithm => AlgorithmToHashAlgorithmName(hashAlgorithm),
                Type hashAlgType => HashAlgorithmTypeToHashAlgorithmName(hashAlgType),
                _ => throw new ArgumentException(SR.Argument_InvalidValue),
            };
        }

        internal static HashAlgorithmName NameOrOidToHashAlgorithmName(string? nameOrOid)
        {
            if (nameOrOid is null)
            {
                // Default Algorithm Id is CALG_SHA1
                return HashAlgorithmName.SHA1;
            }

            // Fall back to the input if MapNameToOID returns null; it is probably an OID value.
            string oidValue = CryptoConfig.MapNameToOID(nameOrOid) ?? nameOrOid;
            return OidToHashAlgorithmName(oidValue);
        }

        /// <summary>
        /// Map HashAlgorithm type to HashAlgorithmName without using CryptoConfig. Throws if not found.
        /// </summary>
        private static HashAlgorithmName AlgorithmToHashAlgorithmName(HashAlgorithm hashAlgorithm)
        {
            return hashAlgorithm switch
            {
                SHA256 => HashAlgorithmName.SHA256,
                SHA1 => HashAlgorithmName.SHA1,
                SHA384 => HashAlgorithmName.SHA384,
                SHA512 => HashAlgorithmName.SHA512,
                MD5 => HashAlgorithmName.MD5,
                _ => throw new ArgumentException(SR.Argument_InvalidValue),
            };
        }

        private static HashAlgorithmName OidToHashAlgorithmName(string oid)
        {
            return oid switch
            {
                Oids.Sha256 => HashAlgorithmName.SHA256,
                Oids.Sha1 => HashAlgorithmName.SHA1,
                Oids.Sha384 => HashAlgorithmName.SHA384,
                Oids.Sha512 => HashAlgorithmName.SHA512,
                Oids.Md5 => HashAlgorithmName.MD5,
                _ => throw new CryptographicException(SR.Cryptography_InvalidOID),
            };
        }

        private static HashAlgorithmName HashAlgorithmTypeToHashAlgorithmName(Type hashAlgType)
        {
            if (typeof(SHA1).IsAssignableFrom(hashAlgType))
                return HashAlgorithmName.SHA1;
            if (typeof(SHA256).IsAssignableFrom(hashAlgType))
                return HashAlgorithmName.SHA256;
            if (typeof(SHA384).IsAssignableFrom(hashAlgType))
                return HashAlgorithmName.SHA384;
            if (typeof(SHA512).IsAssignableFrom(hashAlgType))
                return HashAlgorithmName.SHA512;
            if (typeof(MD5).IsAssignableFrom(hashAlgType))
                return HashAlgorithmName.MD5;

            throw new ArgumentException(SR.Argument_InvalidValue);
        }

        internal static CryptographicException GetBadDataException()
        {
            const int NTE_BAD_DATA = unchecked((int)CryptKeyError.NTE_BAD_DATA);
            return new CryptographicException(NTE_BAD_DATA);
        }

        internal static CryptographicException GetEFailException()
        {
            return new CryptographicException(E_FAIL);
        }
    }
}
