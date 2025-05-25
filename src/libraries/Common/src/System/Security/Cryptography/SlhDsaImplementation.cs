// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    internal sealed partial class SlhDsaImplementation : SlhDsa
    {
        internal static partial bool SupportsAny();

        internal static partial SlhDsaImplementation GenerateKeyCore(SlhDsaAlgorithm algorithm);
        internal static partial SlhDsaImplementation ImportPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial SlhDsaImplementation ImportPkcs8PrivateKeyValue(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial SlhDsaImplementation ImportSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        /// <summary>
        ///   Duplicates an SLH-DSA private key by export/import.
        ///   Only intended to be used when the key type is unknown.
        /// </summary>
        internal static SlhDsaImplementation DuplicatePrivateKey(SlhDsa key)
        {
            Debug.Assert(key is not SlhDsaImplementation);
            Debug.Assert(key.Algorithm.SecretKeySizeInBytes <= 128);

            Span<byte> secretKey = (stackalloc byte[128])[..key.Algorithm.SecretKeySizeInBytes];
            key.ExportSlhDsaSecretKey(secretKey);
            try
            {
                return ImportSecretKey(key.Algorithm, secretKey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(secretKey);
            }
        }
    }
}
