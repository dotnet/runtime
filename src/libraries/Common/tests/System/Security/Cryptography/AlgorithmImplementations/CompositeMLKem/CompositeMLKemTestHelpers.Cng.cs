// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using Xunit.Sdk;

using ErrorCode = Interop.NCrypt.ErrorCode;

namespace System.Security.Cryptography.Tests
{
    internal static partial class CompositeMLKemTestHelpers
    {
        private static readonly Lazy<bool> s_lazyIsCngSupported = new(CheckCngSupport);

        // Remove this separate CNG flag once supported Windows versions consistently provide Composite ML-KEM through NCrypt.
        internal static bool IsCngSupported => s_lazyIsCngSupported.Value;

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
            CngExportPolicies exportPolicies)
        {
            return PqcBlobHelpers.EncodeCompositeMLKemBlob(
                GetCngParameterSet(algorithm),
                source,
                Interop.BCrypt.KeyBlobType.BCRYPT_COMPOSITE_MLKEM_PRIVATE_BLOB,
                exportPolicies,
                static (exportPoliciesArg, blobKind, blob) =>
                {
                    CngKeyCreationParameters creationParameters = new()
                    {
                        ExportPolicy = exportPoliciesArg,
                    };

                    creationParameters.Parameters.Add(
                        new CngProperty(
                            blobKind,
                            blob.ToArray(),
                            CngPropertyOptions.None));

                    return CngKey.Create(CngAlgorithm.CompositeMLKem, keyName: null, creationParameters);
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

        private static bool CheckCngSupport()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            ErrorCode error = Interop.NCrypt.NCryptOpenStorageProvider(
                out SafeNCryptProviderHandle provider,
                CngProvider.MicrosoftSoftwareKeyStorageProvider.Provider,
                0);

            using (provider)
            {
                if (error != ErrorCode.ERROR_SUCCESS)
                {
                    throw error.ToCryptographicException();
                }

                error = NCryptIsAlgSupported(provider, CngAlgorithm.CompositeMLKem.Algorithm, 0);

                return error switch
                {
                    ErrorCode.ERROR_SUCCESS => true,
                    ErrorCode.NTE_NOT_SUPPORTED => false,
                    _ => throw error.ToCryptographicException(),
                };
            }
        }

        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        [LibraryImport(Interop.Libraries.NCrypt, StringMarshalling = StringMarshalling.Utf16)]
        private static partial ErrorCode NCryptIsAlgSupported(
            SafeNCryptProviderHandle hProvider,
            string pszAlgId,
            int dwFlags);
    }
}
