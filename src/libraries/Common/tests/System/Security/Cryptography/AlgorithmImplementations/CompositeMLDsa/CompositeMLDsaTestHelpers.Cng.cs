// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static partial class CompositeMLDsaTestHelpers
    {
        private const int NTE_NOT_SUPPORTED = unchecked((int)0x80090029);

        internal static CompositeMLDsaCng ImportPublicKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            CngKey key = PqcBlobHelpers.EncodeCompositeMLDsaBlob(
                PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(algorithm, out var parameterSet)
                    ? parameterSet
                    : throw new XunitException($"Unsupported algorithm: {algorithm.Name}."),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PUBLIC_BLOB,
                blob => CngKey.Import(blob.ToArray(), CngKeyBlobFormat.PQDsaPublicBlob));

            return new CompositeMLDsaCng(key);
        }

        internal static CompositeMLDsaCng GenerateKey(CompositeMLDsaAlgorithm algorithm, CngExportPolicies exportPolicies)
        {
            CngProperty parameterSet = GetCngProperty(algorithm);

            CngKeyCreationParameters creationParams = new();
            creationParams.Parameters.Add(parameterSet);
            creationParams.ExportPolicy = exportPolicies;

            CngKey key = CngKey.Create(CngAlgorithm.CompositeMLDsa, keyName: null, creationParams);
            return new CompositeMLDsaCng(key);
        }

        internal static CompositeMLDsaCng ImportPrivateKey(CompositeMLDsaAlgorithm algorithm, ReadOnlySpan<byte> source, CngExportPolicies exportPolicies)
        {
            CngKey key = PqcBlobHelpers.EncodeCompositeMLDsaBlob(
                PqcBlobHelpers.TryGetCompositeMLDsaParameterSet(algorithm, out var parameterSet)
                    ? parameterSet
                    : throw new XunitException($"Unsupported algorithm: {algorithm.Name}."),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                blob =>
                {
                    CngProperty dsaBlob = new CngProperty(
                        Interop.BCrypt.KeyBlobType.BCRYPT_PQDSA_PRIVATE_BLOB,
                        blob.ToArray(),
                        CngPropertyOptions.None);

                    CngKeyCreationParameters creationParams = new();
                    creationParams.Parameters.Add(dsaBlob);
                    creationParams.ExportPolicy = exportPolicies;

                    CngKey key = CngKey.Create(CngAlgorithm.CompositeMLDsa, keyName: null, creationParams);
                    return key;
                });

            return new CompositeMLDsaCng(key);
        }

        internal static CngProperty GetCngProperty(CompositeMLDsaAlgorithm algorithm)
        {
            string parameterSetValue = algorithm.Name switch
            {
                "MLDSA44-ECDSA-P256-SHA256" => "44-ECDSA-P256-SHA256",
                "MLDSA65-ECDSA-P256-SHA512" => "65-ECDSA-P256-SHA512",
                "MLDSA65-ECDSA-P384-SHA512" => "65-ECDSA-P384-SHA512",
                "MLDSA87-ECDSA-P384-SHA512" => "87-ECDSA-P384-SHA512",
                _ => throw new XunitException("Unknown algorithm."),
            };

            byte[] byteValue = new byte[(parameterSetValue.Length + 1) * 2]; // Null terminator
            int written = Encoding.Unicode.GetBytes(parameterSetValue, 0, parameterSetValue.Length, byteValue, 0);
            Assert.Equal(byteValue.Length - 2, written);

            return new CngProperty(
                "ParameterSetName",
                byteValue,
                CngPropertyOptions.None);
        }

        // CryptographicException can only have both HRESULT and Message set starting in .NET Core 3.0+.
        // To work around this, the product code throws an exception derived from CryptographicException
        // that has both set. This assert checks for that instead.
        internal static void AssertThrowsCryptographicExceptionWithHResult(Action export)
        {
            CryptographicException ce = Assert.ThrowsAny<CryptographicException>(export);
            Assert.Equal(NTE_NOT_SUPPORTED, ce.HResult);
        }
    }
}
