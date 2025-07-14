// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    public partial class MLDsa
    {
        // Returns a CNG hash algorithm identifier for an OID.
        // restrictedHashAlgorithmCombination is true if the hash algorithm is known, but Windows does not permit
        // it to be used with the ML-DSA algorithm.
        // Trial and error shows
        // * ML-DSA-44 works with all SHA-2, SHA-3, SHAKE128 and SHAKE256 hash algorithms.
        // * ML-DSA-65 works with SHA-2-384, SHA-2-512, SHA-3-384, SHA-3-512, and SHAKE256.
        // * ML-DSA-87 works with SHA-2-512, SHA-3-512, and SHAKE256.
        private protected string? MapOidToCngHashAlgorithmIdentifer(
            string hashOid,
            out bool restrictedHashAlgorithmCombination)
        {
            int hashLambda;
            string hashAlgorithmIdentifier;

            switch (hashOid)
            {
                case Oids.Md5:
                    restrictedHashAlgorithmCombination = true;
                    return HashAlgorithmNames.MD5;
                case Oids.Sha1:
                    restrictedHashAlgorithmCombination = true;
                    return HashAlgorithmNames.SHA1;
                case Oids.Sha256:
                    hashLambda = 256 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA256;
                case Oids.Sha3_256:
                    hashLambda = 256 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA3_256;
                case Oids.Sha384:
                    hashLambda = 384 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA384;
                case Oids.Sha3_384:
                    hashLambda = 384 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA3_384;
                case Oids.Sha512:
                    hashLambda = 512 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA512;
                case Oids.Sha3_512:
                    hashLambda = 512 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHA3_512;
                case Oids.Shake128: // SHAKE-128 with 256-bits of output
                    hashLambda = 256 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHAKE128;
                case Oids.Shake256: // SHAKE-256 with 512-bits of output
                    hashLambda = 512 / 2;
                    hashAlgorithmIdentifier = HashAlgorithmNames.SHAKE256;
                default:
                    restrictedHashAlgorithmCombination = false;
                    return null;
            }

            restrictedHashAlgorithmCombination = hashLambda < Algorithm.LambdaCollisionStrength;
            return hashAlgorithmIdentifier;
        }
    }
}
