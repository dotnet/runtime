// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;

namespace System.Security.Cryptography.Tests
{
    [ConditionalClass(typeof(MLDsa), nameof(MLDsa.IsSupported))]
    public sealed class MLDsaCngTests : MLDsaTestsBase
    {
        protected override MLDsa GenerateKey(MLDsaAlgorithm algorithm)
        {
            string parameterSetValue = PqcBlobHelpers.GetParameterSet(algorithm);
            byte[] byteValue = new byte[(parameterSetValue.Length + 1) * 2]; // Null terminator
            Assert.Equal(2 * parameterSetValue.Length, Encoding.Unicode.GetBytes(parameterSetValue, byteValue));

            CngProperty parameterSet = new CngProperty(
                Interop.BCrypt.BCryptPropertyStrings.BCRYPT_PARAMETER_SET_NAME,
                byteValue,
                CngPropertyOptions.None);

            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);
            creationParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;

            CngKey key = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParams);
            return new MLDsaCng(key);
        }

        protected override MLDsa ImportPrivateSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            CngKey key =  PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetParameterSet(algorithm),
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
                    creationParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;

                    CngKey key = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParams);
                    return key;
                });

            return new MLDsaCng(key);
        }

        protected override MLDsa ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetParameterSet(algorithm),
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
                    creationParams.ExportPolicy = CngExportPolicies.AllowExport | CngExportPolicies.AllowPlaintextExport;

                    CngKey key = CngKey.Create(CngAlgorithm.MLDsa, keyName: null, creationParams);
                    return key;
                });

            return new MLDsaCng(key);
        }

        protected override MLDsa ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            CngKey key = PqcBlobHelpers.EncodeMLDsaBlob(
                PqcBlobHelpers.GetParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB,
                blob => CngKey.Import(blob.ToArray(), CngKeyBlobFormat.PQDsaPublicBlob));

            return new MLDsaCng(key);
        }

        [Fact]
        public void MLDsaCng_Ctor_ArgValidation()
        {
            AssertExtensions.Throws<ArgumentNullException>("key", static () => new MLDsaCng(null));
        }

        [Fact]
        public void MLDsaCng_WrongAlgorithm()
        {
            using RSACng rsa = new RSACng();
            using CngKey key = rsa.Key;
            Assert.Throws<ArgumentException>(() => new MLDsaCng(key));
        }

        // TODO MLDsaCng doesn't have a public DuplicateHandle like OpenSSL since CngKey does that
        // internally with CngKey.Handle. Is there something else we should test for copy/duplication?
    }
}
