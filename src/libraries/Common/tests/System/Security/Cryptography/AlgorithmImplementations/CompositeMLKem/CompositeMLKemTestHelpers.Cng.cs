// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Xunit;
using Xunit.Sdk;

namespace System.Security.Cryptography.Tests
{
    internal static partial class CompositeMLKemTestHelpers
    {
        // .NET Framework uses a derived exception to preserve both the HRESULT and message.
        internal static void AssertThrowsCryptographicExceptionWithHResult(Action export)
        {
            const int NTE_NOT_SUPPORTED = unchecked((int)0x80090029);

            CryptographicException ce = Assert.ThrowsAny<CryptographicException>(export);
            Assert.Equal(NTE_NOT_SUPPORTED, ce.HResult);
        }

        internal static CngKey GenerateCngKey(
            CompositeMLKemAlgorithm algorithm,
            CngExportPolicies exportPolicies,
            string? keyName = null)
        {
            CngKeyCreationParameters creationParameters = new()
            {
                ExportPolicy = exportPolicies,
                KeyCreationOptions = keyName is null
                    ? CngKeyCreationOptions.None
                    : CngKeyCreationOptions.OverwriteExistingKey,
            };

            creationParameters.Parameters.Add(GetCngProperty(algorithm));
            return CngKey.Create(CngAlgorithm.CompositeMLKem, keyName, creationParameters);
        }

        internal static CngKey ImportCngEncapsulationKey(
            CompositeMLKemAlgorithm algorithm,
            ReadOnlySpan<byte> source)
        {
            return PqcBlobHelpers.EncodeCompositeMLKemBlob(
                GetCngParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PUBLIC_BLOB,
                default(object),
                static (_, blobKind, blob) => CngKey.Import(blob.ToArray(), new CngKeyBlobFormat(blobKind)));
        }

        internal static CngKey ImportCngDecapsulationKey(
            CompositeMLKemAlgorithm algorithm,
            ReadOnlySpan<byte> source,
            CngExportPolicies exportPolicies,
            string? keyName = null)
        {
            return PqcBlobHelpers.EncodeCompositeMLKemBlob(
                GetCngParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB,
                (exportPolicies, keyName),
                static (state, blobKind, blob) =>
                {
                    CngKeyCreationParameters creationParameters = new()
                    {
                        ExportPolicy = state.exportPolicies,
                        KeyCreationOptions = state.keyName is null
                            ? CngKeyCreationOptions.None
                            : CngKeyCreationOptions.OverwriteExistingKey,
                    };

                    creationParameters.Parameters.Add(
                        new CngProperty(
                            blobKind,
                            blob.ToArray(),
                            CngPropertyOptions.None));

                    return CngKey.Create(CngAlgorithm.CompositeMLKem, state.keyName, creationParameters);
                });
        }

        private static CngProperty GetCngProperty(CompositeMLKemAlgorithm algorithm)
        {
            byte[] value = Encoding.Unicode.GetBytes(GetCngParameterSet(algorithm) + '\0');
            return new CngProperty("ParameterSetName", value, CngPropertyOptions.None);
        }

        private static string GetCngParameterSet(CompositeMLKemAlgorithm algorithm)
        {
            if (algorithm == CompositeMLKemAlgorithm.MLKem768WithECDiffieHellmanP256)
            {
                return PqcBlobHelpers.BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_P256;
            }
            else if (algorithm == CompositeMLKemAlgorithm.MLKem768WithX25519)
            {
                return PqcBlobHelpers.BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_768_X25519;
            }
            else if (algorithm == CompositeMLKemAlgorithm.MLKem1024WithECDiffieHellmanP384)
            {
                return PqcBlobHelpers.BCRYPT_COMPOSITE_MLKEM_PARAMETER_SET_1024_P384;
            }

            throw new XunitException($"Unsupported algorithm: {algorithm.Name}.");
        }
    }
}
