// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography.Tests
{
    internal static partial class MLDsaTestHelpers
    {
        internal static MLDsaCng ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB,
                blob => CngKey.Import(blob.ToArray(), CngKeyBlobFormat.PQDsaPublicBlob));

            return new MLDsaCng(key);
        }

        internal static MLDsaCng GenerateKey(MLDsaAlgorithm algorithm, CngExportPolicies exportPolicies)
        {
            CngProperty parameterSet = GetCngProperty(algorithm);

            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);
            creationParams.ExportPolicy = exportPolicies;

            CngKey key = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParams);
            return new MLDsaCng(key);
        }

        internal static MLDsaCng ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source, CngExportPolicies exportPolicies)
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB,
                blob =>
                {
                    CngProperty mldsaBlob = new CngProperty(
                        Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_SEED_BLOB,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(mldsaBlob);
                    creationParams.ExportPolicy = exportPolicies;

                    CngKey key = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParams);
                    return key;
                });

            return new MLDsaCng(key);
        }

        internal static MLDsaCng ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source, CngExportPolicies exportPolicies)
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetMLDsaParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                blob =>
                {
                    CngProperty mldsaBlob = new CngProperty(
                        Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(mldsaBlob);
                    creationParams.ExportPolicy = exportPolicies;

                    CngKey key = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParams);
                    return key;
                });

            return new MLDsaCng(key);
        }
    }
}
