// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static class CngAlgorithmExtensions
    {
        private static CngAlgorithm? _mlDsaCngAlgorithm;

        extension (CngAlgorithm)
        {
            internal static CngAlgorithm MLDsa =>
                _mlDsaCngAlgorithm ??= new CngAlgorithm("ML-DSA");  // BCRYPT_MLDSA_ALGORITHM
        }
    }

    internal static class CngAlgorithmGroupExtensions
    {
        private static CngAlgorithmGroup? _mlDsaCngAlgorithmGroup;

        extension (CngAlgorithmGroup)
        {
            internal static CngAlgorithmGroup MLDsa =>
                _mlDsaCngAlgorithmGroup ??= new CngAlgorithmGroup("MLDSA"); // NCRYPT_MLDSA_ALGORITHM_GROUP
        }
    }

    internal static class CngKeyBlobFormatExtensions
    {
        private static CngKeyBlobFormat? _pqDsaPublicBlob;
        private static CngKeyBlobFormat? _pqDsaPrivateBlob;
        private static CngKeyBlobFormat? _pqDsaPrivateSeedBlob;

        extension (CngKeyBlobFormat)
        {
            internal static CngKeyBlobFormat PQDsaPublicBlob =>
                _pqDsaPublicBlob ??= new CngKeyBlobFormat("PQDSAPUBLICBLOB"); // BCRYPT_PQDSA_PUBLIC_BLOB

            internal static CngKeyBlobFormat PQDsaPrivateBlob =>
                _pqDsaPrivateBlob ??= new CngKeyBlobFormat("PQDSAPRIVATEBLOB"); // BCRYPT_PQDSA_PRIVATE_BLOB

            internal static CngKeyBlobFormat PQDsaPrivateSeedBlob =>
                _pqDsaPrivateSeedBlob ??= new CngKeyBlobFormat("PQDSAPRIVATESEEDBLOB"); // BCRYPT_PQDSA_PRIVATE_SEED_BLOB
        }
    }
}
