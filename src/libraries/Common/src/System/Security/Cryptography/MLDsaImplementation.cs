// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId, UrlFormat = Experimentals.SharedUrlFormat)]
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        internal static partial bool SupportsAny();
        internal static partial bool IsAlgorithmSupported(MLDsaAlgorithm algorithm);

        internal static partial MLDsaImplementation GenerateKeyImpl(MLDsaAlgorithm algorithm);
        internal static partial MLDsaImplementation ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsaImplementation ImportPrivateKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsaImplementation ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        /// <summary>
        ///   Duplicates an ML-DSA private key by export/import.
        ///   Only intended to be used when the key type is unknown.
        /// </summary>
        internal static MLDsaImplementation DuplicatePrivateKey(MLDsa key)
        {
            // The implementation type and any platform types (e.g. MLDsaOpenSsl)
            // should inherently know how to clone themselves without the crudeness
            // of export/import.
            Debug.Assert(key is not MLDsaImplementation);

            MLDsaAlgorithm alg = key.Algorithm;
            Debug.Assert(alg.PrivateKeySizeInBytes > alg.PrivateSeedSizeInBytes);
            byte[] rented = CryptoPool.Rent(alg.PrivateKeySizeInBytes);

            try
            {
                Span<byte> seedSpan = rented.AsSpan(0, alg.PrivateSeedSizeInBytes);
                key.ExportMLDsaPrivateSeed(seedSpan);
                return ImportSeed(alg, seedSpan);
            }
            catch (CryptographicException)
            {
                // Rented array may still be larger but we expect exact length
                Span<byte> skSpan = rented.AsSpan(0, alg.PrivateKeySizeInBytes);
                key.ExportMLDsaPrivateKey(skSpan);
                return ImportPrivateKey(alg, skSpan);
            }
            finally
            {
                CryptoPool.Return(rented);
            }
        }
    }
}
