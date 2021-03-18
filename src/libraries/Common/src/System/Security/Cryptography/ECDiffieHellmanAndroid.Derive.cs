// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    internal static partial class ECDiffieHellmanImplementation
    {
        public sealed partial class ECDiffieHellmanAndroid : ECDiffieHellman
        {
            /// <summary>
            /// Given a second party's public key, derive shared key material
            /// </summary>
            public override byte[] DeriveKeyMaterial(ECDiffieHellmanPublicKey otherPartyPublicKey) =>
                DeriveKeyFromHash(otherPartyPublicKey, HashAlgorithmName.SHA256, null, null);

            public override byte[] DeriveKeyFromHash(
                ECDiffieHellmanPublicKey otherPartyPublicKey,
                HashAlgorithmName hashAlgorithm,
                byte[]? secretPrepend,
                byte[]? secretAppend)
            {
                if (otherPartyPublicKey == null)
                    throw new ArgumentNullException(nameof(otherPartyPublicKey));
                if (string.IsNullOrEmpty(hashAlgorithm.Name))
                    throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

                ThrowIfDisposed();

                return ECDiffieHellmanDerivation.DeriveKeyFromHash(
                    otherPartyPublicKey,
                    hashAlgorithm,
                    secretPrepend,
                    secretAppend,
                    (pubKey, hasher) => DeriveSecretAgreement(pubKey, hasher));
            }

            public override byte[] DeriveKeyFromHmac(
                ECDiffieHellmanPublicKey otherPartyPublicKey,
                HashAlgorithmName hashAlgorithm,
                byte[]? hmacKey,
                byte[]? secretPrepend,
                byte[]? secretAppend)
            {
                if (otherPartyPublicKey == null)
                    throw new ArgumentNullException(nameof(otherPartyPublicKey));
                if (string.IsNullOrEmpty(hashAlgorithm.Name))
                    throw new ArgumentException(SR.Cryptography_HashAlgorithmNameNullOrEmpty, nameof(hashAlgorithm));

                ThrowIfDisposed();

                return ECDiffieHellmanDerivation.DeriveKeyFromHmac(
                    otherPartyPublicKey,
                    hashAlgorithm,
                    hmacKey,
                    secretPrepend,
                    secretAppend,
                    (pubKey, hasher) => DeriveSecretAgreement(pubKey, hasher));
            }

            public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
            {
                if (otherPartyPublicKey == null)
                    throw new ArgumentNullException(nameof(otherPartyPublicKey));
                if (prfLabel == null)
                    throw new ArgumentNullException(nameof(prfLabel));
                if (prfSeed == null)
                    throw new ArgumentNullException(nameof(prfSeed));

                ThrowIfDisposed();

                return ECDiffieHellmanDerivation.DeriveKeyTls(
                    otherPartyPublicKey,
                    prfLabel,
                    prfSeed,
                    (pubKey, hasher) => DeriveSecretAgreement(pubKey, hasher));
            }

            /// <summary>
            /// Get the secret agreement generated between two parties
            /// </summary>
            private byte[]? DeriveSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey, IncrementalHash? hasher)
            {
                Debug.Assert(otherPartyPublicKey != null);

                // Ensure that this ECDH object contains a private key by attempting a parameter export
                // which will throw an OpenSslCryptoException if no private key is available
                ECParameters thisKeyExplicit = ExportExplicitParameters(true);
                bool thisIsNamed = Interop.AndroidCrypto.EcKeyHasCurveName(_key.Value);
                ECDiffieHellmanAndroidPublicKey? otherKey = otherPartyPublicKey as ECDiffieHellmanAndroidPublicKey;
                bool disposeOtherKey = false;

                if (otherKey == null)
                {
                    disposeOtherKey = true;

                    ECParameters otherParameters =
                        thisIsNamed
                            ? otherPartyPublicKey.ExportParameters()
                            : otherPartyPublicKey.ExportExplicitParameters();

                    otherKey = new ECDiffieHellmanAndroidPublicKey(otherParameters);
                }

                bool otherIsNamed = otherKey.HasCurveName;

                SafeEcKeyHandle? ourKey = null;
                SafeEcKeyHandle? theirKey = null;
                byte[]? rented = null;
                // Calculate secretLength in bytes.
                int secretLength = AsymmetricAlgorithmHelpers.BitsToBytes(KeySize);

                try
                {
                    if (otherKey.KeySize != KeySize)
                    {
                        throw new ArgumentException(SR.Cryptography_ArgECDHKeySizeMismatch, nameof(otherPartyPublicKey));
                    }

                    if (otherIsNamed == thisIsNamed)
                    {
                        ourKey = _key.UpRefKeyHandle();
                        theirKey = otherKey.DuplicateKeyHandle();
                    }
                    else if (otherIsNamed)
                    {
                        ourKey = _key.UpRefKeyHandle();

                        using (ECAndroid tmp = new ECAndroid(otherKey.ExportExplicitParameters()))
                        {
                            theirKey = tmp.UpRefKeyHandle();
                        }
                    }
                    else
                    {
                        using (ECAndroid tmp = new ECAndroid(thisKeyExplicit))
                        {
                            ourKey = tmp.UpRefKeyHandle();
                        }

                        theirKey = otherKey.DuplicateKeyHandle();
                    }

                    // Indicate that secret can hold stackallocs from nested scopes
                    Span<byte> secret = stackalloc byte[0];

                    // Arbitrary limit. But it covers secp521r1, which is the biggest common case.
                    const int StackAllocMax = 66;

                    if (secretLength > StackAllocMax)
                    {
                        rented = CryptoPool.Rent(secretLength);
                        secret = new Span<byte>(rented, 0, secretLength);
                    }
                    else
                    {
                        secret = stackalloc byte[secretLength];
                    }

                    if (!Interop.AndroidCrypto.EcdhDeriveKey(ourKey, theirKey, secret, out int usedBufferLength))
                    {
                        throw new CryptographicException();
                    }

                    Debug.Assert(secretLength == usedBufferLength, $"Expected secret length {secretLength} does not match actual secret length {usedBufferLength}.");

                    if (hasher == null)
                    {
                        return secret.ToArray();
                    }
                    else
                    {
                        hasher.AppendData(secret);
                        return null;
                    }
                }
                finally
                {
                    theirKey?.Dispose();
                    ourKey?.Dispose();

                    if (disposeOtherKey)
                    {
                        otherKey.Dispose();
                    }

                    if (rented != null)
                    {
                        CryptoPool.Return(rented, secretLength);
                    }
                }
            }
        }
    }
}
