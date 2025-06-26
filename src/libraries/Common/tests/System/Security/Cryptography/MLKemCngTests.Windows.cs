// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

using Xunit;
using Xunit.Sdk;

using KeyBlobMagicNumber = Interop.BCrypt.KeyBlobMagicNumber;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLKem), nameof(MLKem.IsSupported))]
    public sealed class MLKemCngTests : MLKemBaseTests
    {
        public override MLKem GenerateKey(MLKemAlgorithm algorithm)
        {
            CngProperty parameterSet = GetCngProperty(algorithm);

            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);
            creationParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;

            using (CngKey key = CngKey.Create(CngAlgorithm.MLKem, keyName: null, creationParams))
            {
                return new MLKemCng(key);
            }
        }

        public override MLKem ImportPrivateSeed(MLKemAlgorithm algorithm, ReadOnlySpan<byte> seed)
        {
            using (CngKey key = ImportMLKemKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_SEED_MAGIC, algorithm, seed))
            {
                return new MLKemCng(key);
            }
        }

        public override MLKem ImportDecapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using (CngKey key = ImportMLKemKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PRIVATE_MAGIC, algorithm, source))
            {
                return new MLKemCng(key);
            }
        }

        public override MLKem ImportEncapsulationKey(MLKemAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            using (CngKey key = ImportMLKemKey(KeyBlobMagicNumber.BCRYPT_MLKEM_PUBLIC_MAGIC, algorithm, source))
            {
                return new MLKemCng(key);
            }
        }

        private static CngProperty GetCngProperty(MLKemAlgorithm algorithm)
        {
            string cngParameterSet;

            if (algorithm == MLKemAlgorithm.MLKem512)
            {
                cngParameterSet = "512\0";
            }
            else if (algorithm == MLKemAlgorithm.MLKem768)
            {
                cngParameterSet = "768\0";
            }
            else if (algorithm == MLKemAlgorithm.MLKem1024)
            {
                cngParameterSet = "1024\0";
            }
            else
            {
                throw new XunitException($"Unknown MLKemAlgorithm '{algorithm}'.");
            }

            byte[] byteValue = Encoding.Unicode.GetBytes(cngParameterSet);
            return new CngProperty("ParameterSetName", byteValue, CngPropertyOptions.None);
        }

        private static CngKey ImportMLKemKey(KeyBlobMagicNumber kind, MLKemAlgorithm algorithm, ReadOnlySpan<byte> key)
        {
            return PqcBlobHelpers.EncodeMLKemBlob(
                kind,
                algorithm,
                key,
                (object)null,
                static (_, blobKind, blob) =>
                {
                    if (blobKind == CngKeyBlobFormat.MLKemPublicBlob.Format)
                    {
                        return CngKey.Import(blob.ToArray(), CngKeyBlobFormat.MLKemPublicBlob);
                    }
                    else
                    {
                        CngProperty blobProperty = new CngProperty(
                            blobKind,
                            blob.ToArray(),
                            CngPropertyOptions.None);

                        CngKeyCreationParameters creationParams = new();
                        creationParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;
                        creationParams.Parameters.Add(blobProperty);
                        return CngKey.Create(CngAlgorithm.MLKem, keyName: null, creationParams);
                    }
                });
        }
    }
}
