// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal sealed partial class MLDsaImplementation : MLDsa
    {
        private MLDsaImplementation(MLDsaAlgorithm algorithm)
            : base(algorithm)
        {
            ThrowIfNotSupported();
        }

        internal static partial bool SupportsAny();

        internal static partial MLDsaImplementation GenerateKeyImpl(MLDsaAlgorithm algorithm);
        internal static partial MLDsaImplementation ImportPublicKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsaImplementation ImportPkcs8PrivateKeyValue(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsaImplementation ImportSecretKey(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);
        internal static partial MLDsaImplementation ImportSeed(MLDsaAlgorithm algorithm, ReadOnlySpan<byte> source);

        internal static MLDsaImplementation DuplicatePrivateKey(MLDsa key)
        {
            MLDsaAlgorithm alg = key.Algorithm;
            byte[] rented = CryptoPool.Rent(alg.SecretKeySizeInBytes);
            int written = 0;

            try
            {
                written = key.ExportMLDsaPrivateSeed(rented);
                return ImportSeed(alg, new ReadOnlySpan<byte>(rented, 0, written));
            }
            catch (CryptographicException)
            {
                written = key.ExportMLDsaSecretKey(rented);
                return ImportSecretKey(alg, new ReadOnlySpan<byte>(rented, 0, written));
            }
            finally
            {
                CryptoPool.Return(rented, written);
            }
        }
    }
}
