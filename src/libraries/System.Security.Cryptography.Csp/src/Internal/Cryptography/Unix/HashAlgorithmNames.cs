// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static class HashAlgorithmNames
    {
        public const string MD5 = "MD5";
        public const string SHA1 = "SHA1";
        public const string SHA256 = "SHA256";
        public const string SHA384 = "SHA384";
        public const string SHA512 = "SHA512";

        private const string OID_MD5 = "1.2.840.113549.2.5";
        private const string OID_SHA1 = "1.3.14.3.2.26";
        private const string OID_SHA256 = "2.16.840.1.101.3.4.2.1";
        private const string OID_SHA384 = "2.16.840.1.101.3.4.2.2";
        private const string OID_SHA512 = "2.16.840.1.101.3.4.2.3";

        // For backwards compat with CapiHelper.ObjToHashAlgorithm, use "hashAlg" as name
        public static HashAlgorithmName ObjToHashAlgorithmName(object hashAlg)
        {
            if (hashAlg == null)
                throw new ArgumentNullException(nameof(hashAlg));

            HashAlgorithmName? name = null;

            if (hashAlg is string)
            {
                name = NameOrOidToHashAlgorithmName((string)hashAlg);
            }
            else if (hashAlg is HashAlgorithm)
            {
                name = ((HashAlgorithm)hashAlg).ToHashAlgorithmName();
            }
            else if (hashAlg is Type)
            {
                name = HashAlgorithmTypeToHashAlgorithmName((Type)hashAlg);
            }

            if (name.HasValue)
            {
                return name.Value;
            }

            throw new ArgumentException(SR.Argument_InvalidValue);
        }

        public static HashAlgorithmName NameOrOidToHashAlgorithmName(string nameOrOid)
        {
            HashAlgorithmName? name = null;

            if (nameOrOid == null)
            {
                // Default Algorithm Id is CALG_SHA1
                name = HashAlgorithmName.SHA1;
            }
            else
            {
                string oidValue = CryptoConfig.MapNameToOID(nameOrOid);
                if (oidValue == null)
                    oidValue = nameOrOid; // we were probably passed an OID value directly

                name = OidToHashAlgorithmName(oidValue);
            }

            if (!name.HasValue)
            {
                throw new CryptographicException(SR.Cryptography_InvalidOID);
            }

            return name.Value;
        }

        /// <summary>
        /// Map HashAlgorithm type to HashAlgorithmName without using CryptoConfig. Returns null if not found.
        /// </summary>
        public static HashAlgorithmName? ToHashAlgorithmName(this HashAlgorithm hashAlgorithm)
        {
            if (hashAlgorithm is SHA1)
                return HashAlgorithmName.SHA1;
            if (hashAlgorithm is SHA256)
                return HashAlgorithmName.SHA256;
            if (hashAlgorithm is SHA384)
                return HashAlgorithmName.SHA384;
            if (hashAlgorithm is SHA512)
                return HashAlgorithmName.SHA512;
            if (hashAlgorithm is MD5)
                return HashAlgorithmName.MD5;

            return null;
        }

        public static HashAlgorithmName? OidToHashAlgorithmName(string oid)
        {
            switch (oid)
            {
                case OID_SHA1:
                    return HashAlgorithmName.SHA1;
                case OID_SHA256:
                    return HashAlgorithmName.SHA256;
                case OID_SHA384:
                    return HashAlgorithmName.SHA384;
                case OID_SHA512:
                    return HashAlgorithmName.SHA512;
                case OID_MD5:
                    return HashAlgorithmName.MD5;
                default:
                    return null;
            }
        }

        public static HashAlgorithmName? HashAlgorithmTypeToHashAlgorithmName(Type hashAlgType)
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

            return null;
        }
    }
}
