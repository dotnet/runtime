// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal static class CngAlgorithmExtensions
    {
        private static CngAlgorithm? _mlDsaCngAlgorithm;
        private static CngAlgorithm? _mlKemCngAlgorithm;

        extension(CngAlgorithm)
        {
            internal static CngAlgorithm MLDsa =>
                _mlDsaCngAlgorithm ??= new CngAlgorithm("ML-DSA");  // BCRYPT_MLDSA_ALGORITHM

            internal static CngAlgorithm MLKem =>
                _mlKemCngAlgorithm ??= new CngAlgorithm("ML-KEM");  // BCRYPT_MLKEM_ALGORITHM
        }
    }

    internal static class CngAlgorithmGroupExtensions
    {
        private static CngAlgorithmGroup? _mlDsaCngAlgorithmGroup;
        private static CngAlgorithmGroup? _mlKemCngAlgorithmGroup;

        extension(CngAlgorithmGroup)
        {
            internal static CngAlgorithmGroup MLDsa =>
                _mlDsaCngAlgorithmGroup ??= new CngAlgorithmGroup("MLDSA"); // NCRYPT_MLDSA_ALGORITHM_GROUP

            internal static CngAlgorithmGroup MLKem =>
                _mlKemCngAlgorithmGroup ??= new CngAlgorithmGroup("MLKEM"); // NCRYPT_MLKEM_ALGORITHM_GROUP
        }
    }

    internal static class CngKeyBlobFormatExtensions
    {
        private static CngKeyBlobFormat? _mlKemPublicBlob;
        private static CngKeyBlobFormat? _mlKemPrivateBlob;
        private static CngKeyBlobFormat? _mlKemPrivateSeedBlob;
        private static CngKeyBlobFormat? _pqDsaPublicBlob;
        private static CngKeyBlobFormat? _pqDsaPrivateBlob;
        private static CngKeyBlobFormat? _pqDsaPrivateSeedBlob;

        extension(CngKeyBlobFormat)
        {
            internal static CngKeyBlobFormat MLKemPublicBlob =>
                _mlKemPublicBlob ??= new CngKeyBlobFormat("MLKEMPUBLICBLOB"); // BCRYPT_MLKEM_PUBLIC_BLOB

            internal static CngKeyBlobFormat MLKemPrivateBlob =>
                _mlKemPrivateBlob ??= new CngKeyBlobFormat("MLKEMPRIVATEBLOB"); // BCRYPT_MLKEM_PRIVATE_BLOB

            internal static CngKeyBlobFormat MLKemPrivateSeedBlob =>
                _mlKemPrivateSeedBlob ??= new CngKeyBlobFormat("MLKEMPRIVATESEEDBLOB"); // BCRYPT_MLKEM_PRIVATE_SEED_BLOB

            internal static CngKeyBlobFormat PQDsaPublicBlob =>
                _pqDsaPublicBlob ??= new CngKeyBlobFormat("PQDSAPUBLICBLOB"); // BCRYPT_PQDSA_PUBLIC_BLOB

            internal static CngKeyBlobFormat PQDsaPrivateBlob =>
                _pqDsaPrivateBlob ??= new CngKeyBlobFormat("PQDSAPRIVATEBLOB"); // BCRYPT_PQDSA_PRIVATE_BLOB

            internal static CngKeyBlobFormat PQDsaPrivateSeedBlob =>
                _pqDsaPrivateSeedBlob ??= new CngKeyBlobFormat("PQDSAPRIVATESEEDBLOB"); // BCRYPT_PQDSA_PRIVATE_SEED_BLOB
        }
    }
}
