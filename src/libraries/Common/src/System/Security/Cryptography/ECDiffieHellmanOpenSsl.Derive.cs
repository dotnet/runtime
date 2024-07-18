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

        /// <inheritdoc />
        public override byte[] DeriveRawSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
            ThrowIfDisposed();

            byte[]? secretAgreement = DeriveSecretAgreement(otherPartyPublicKey, hasher: null);
            Debug.Assert(secretAgreement is not null);
            return secretAgreement;
        }

        /// <summary>
        /// Get the secret agreement generated between two parties
        /// </summary>
        private byte[]? DeriveSecretAgreement(ECDiffieHellmanPublicKey otherPartyPublicKey, IncrementalHash? hasher)
        {
            Debug.Assert(otherPartyPublicKey != null);
            Debug.Assert(_key is not null); // Callers should validate prior.

            bool thisIsNamed;

            using (SafeEcKeyHandle ecKey = Interop.Crypto.EvpPkeyGetEcKey(_key.Value))
            {
                thisIsNamed = Interop.Crypto.EcKeyHasCurveName(ecKey);
            }

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

            // We need to always duplicate handle in case this operation is done by multiple threads and one of them disposes the handle
            SafeEvpPKeyHandle? ourKey = _key.Value;
            bool disposeOurKey = false;

            SafeEvpPKeyHandle? theirKey = null;

            // secp521r1 which is the biggest common case maxes out at 66 bytes so 128 should always be enough.
            const int StackAllocMax = 128;
            Span<byte> secret = stackalloc byte[StackAllocMax];

            try
            {
                if (otherKey.KeySize != KeySize)
                {
                    throw new ArgumentException(SR.Cryptography_ArgECDHKeySizeMismatch, nameof(otherPartyPublicKey));
                }

                if (otherIsNamed == thisIsNamed)
                {
                    theirKey = otherKey.DuplicateKeyHandle();
                }
                else if (otherIsNamed)
                {
                    using (ECOpenSsl tmp = new ECOpenSsl(otherKey.ExportExplicitParameters()))
                    {
                        theirKey = tmp.CreateKeyHandle();
                    }
                }
                else
                {
                    try
                    {
                        // This is generally not expected to fail except:
                        // - when key can't be accessed but is available (i.e. TPM)
                        // - private key is actually missing
                        using (ECOpenSsl tmp = new ECOpenSsl(ExportExplicitParameters(true)))
                        {
                            ourKey = tmp.CreateKeyHandle();
                            disposeOurKey = true;
                        }
                    }
                    catch (CryptographicException)
                    {
                        // In both cases of failure we'll report lack of private key
                        throw new CryptographicException(SR.Cryptography_CSP_NoPrivateKey);
                    }

                    theirKey = otherKey.DuplicateKeyHandle();
                }

                int written = Interop.Crypto.EvpPKeyDeriveSecretAgreement(ourKey, theirKey, secret);
                secret = secret.Slice(0, written);

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
                CryptographicOperations.ZeroMemory(secret);

                theirKey?.Dispose();

                if (disposeOtherKey)
                {
                    otherKey.Dispose();
                }

                if (disposeOurKey)
                {
                    ourKey.Dispose();
                }
            }
        }
    }
}
