// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Security.Cryptography
{
    internal sealed partial class MLKemImplementation : MLKem
    {
        /// <summary>
        ///   Duplicates an ML-KEM private key by export/import.
        ///   Only intended to be used when the key type is unknown.
        /// </summary>
        internal static MLKemImplementation DuplicatePrivateKey(MLKem key)
        {
            // The implementation type and any platform types (e.g. MLKemOpenSsl)
            // should inherently know how to clone themselves without the crudeness
            // of export/import.
            Debug.Assert(key is not (MLKemImplementation or MLKemOpenSsl));

            MLKemAlgorithm alg = key.Algorithm;
            byte[] rented = CryptoPool.Rent(alg.DecapsulationKeySizeInBytes);
            int size = 0;

            try
            {
                size = alg.PrivateSeedSizeInBytes;
                Span<byte> buffer = rented.AsSpan(0, size);
                key.ExportPrivateSeed(buffer);
                return ImportPrivateSeedImpl(alg, buffer);
            }
            catch (CryptographicException)
            {
                size = alg.DecapsulationKeySizeInBytes;
                Span<byte> buffer = rented.AsSpan(0, size);
                key.ExportDecapsulationKey(buffer);
                return ImportDecapsulationKeyImpl(alg, buffer);
            }
            finally
            {
                CryptoPool.Return(rented, size);
            }
        }
    }
}
