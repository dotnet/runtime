// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanOpenSsl : ECDiffieHellman
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
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            ThrowIfDisposed();

            return ECDiffieHellmanDerivation.DeriveKeyFromHash(
                otherPartyPublicKey,
                hashAlgorithm,
                secretPrepend,
                secretAppend,
                DeriveSecretAgreement);
        }

        public override byte[] DeriveKeyFromHmac(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[]? hmacKey,
            byte[]? secretPrepend,
            byte[]? secretAppend)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            ThrowIfDisposed();

            return ECDiffieHellmanDerivation.DeriveKeyFromHmac(
                otherPartyPublicKey,
                hashAlgorithm,
                hmacKey,
                secretPrepend,
                secretAppend,
                DeriveSecretAgreement);
        }

        public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
            ArgumentNullException.ThrowIfNull(prfLabel);
            ArgumentNullException.ThrowIfNull(prfSeed);

            ThrowIfDisposed();

            return ECDiffieHellmanDerivation.DeriveKeyTls(
                otherPartyPublicKey,
                prfLabel,
                prfSeed,
                DeriveSecretAgreement);
        }

        /// <summary>
        /// Get the secret agreement generated between two parties
        /// </summary>
        private byte[]? DeriveSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey, IncrementalHash? hasher)
        {
            Debug.Assert(otherPartyPublicKey != null);
            Debug.Assert(_key is not null); // Callers should validate prior.

            // Ensure that this ECDH object contains a private key by attempting a parameter export
            // which will throw an OpenSslCryptoException if no private key is available
            ECParameters thisKeyExplicit = ExportExplicitParameters(true);
            bool thisIsNamed = Interop.Crypto.EcKeyHasCurveName(_key.Value);
            ECDiffieHellmanOpenSslPublicKey? otherKey = otherPartyPublicKey as ECDiffieHellmanOpenSslPublicKey;
            bool disposeOtherKey = false;

            if (otherKey == null)
            {
                disposeOtherKey = true;

                ECParameters otherParameters =
                    thisIsNamed
                        ? otherPartyPublicKey.ExportParameters()
                        : otherPartyPublicKey.ExportExplicitParameters();

                otherKey = new ECDiffieHellmanOpenSslPublicKey(otherParameters);
            }

            bool otherIsNamed = otherKey.HasCurveName;

            SafeEvpPKeyHandle? ourKey = null;
            SafeEvpPKeyHandle? theirKey = null;
            byte[]? rented = null;
            int secretLength = 0;

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

                    using (ECOpenSsl tmp = new ECOpenSsl(otherKey.ExportExplicitParameters()))
                    {
                        theirKey = tmp.UpRefKeyHandle();
                    }
                }
                else
                {
                    using (ECOpenSsl tmp = new ECOpenSsl(thisKeyExplicit))
                    {
                        ourKey = tmp.UpRefKeyHandle();
                    }

                    theirKey = otherKey.DuplicateKeyHandle();
                }

                using (SafeEvpPKeyCtxHandle ctx = Interop.Crypto.EvpPKeyCtxCreate(ourKey, theirKey, out uint secretLengthU))
                {
                    if (ctx == null || ctx.IsInvalid || secretLengthU == 0 || secretLengthU > int.MaxValue)
                    {
                        throw Interop.Crypto.CreateOpenSslCryptographicException();
                    }

                    secretLength = (int)secretLengthU;

                    // Indicate that secret can hold stackallocs from nested scopes
                    scoped Span<byte> secret;

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

                    Interop.Crypto.EvpPKeyDeriveSecretAgreement(ctx, secret);

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
