// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
    {
        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng() : this(521) { }

        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng(int keySize)
        {
            KeySize = keySize;
        }

        [SupportedOSPlatform("windows")]
        public ECDiffieHellmanCng(ECCurve curve)
        {
            // GenerateKey will already do all of the validation we need.
            GenerateKey(curve);
        }

        public override int KeySize
        {
            get
            {
                return base.KeySize;
            }
            set
            {
                if (KeySize == value)
                {
                    return;
                }

                // Set the KeySize before DisposeKey so that an invalid value doesn't throw away the key
                base.KeySize = value;

                DisposeKey();
                // Key will be lazily re-created
            }
        }

        /// <summary>
        /// Set the KeySize without validating against LegalKeySizes.
        /// </summary>
        /// <param name="newKeySize">The value to set the KeySize to.</param>
        private void ForceSetKeySize(int newKeySize)
        {
            // In the event that a key was loaded via ImportParameters, curve name, or an IntPtr/SafeHandle
            // it could be outside of the bounds that we currently represent as "legal key sizes".
            // Since that is our view into the underlying component it can be detached from the
            // component's understanding.  If it said it has opened a key, and this is the size, trust it.
            KeySizeValue = newKeySize;
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                // Return the three sizes that can be explicitly set (for backwards compatibility)
                return new[] {
                    new KeySizes(minSize: 256, maxSize: 384, skipSize: 128),
                    new KeySizes(minSize: 521, maxSize: 521, skipSize: 0),
                };
            }
        }

        public override byte[] DeriveKeyFromHash(
            ECDiffieHellmanPublicKey otherPartyPublicKey,
            HashAlgorithmName hashAlgorithm,
            byte[]? secretPrepend,
            byte[]? secretAppend)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            using (SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey))
            {
                return Interop.NCrypt.DeriveKeyMaterialHash(
                    secretAgreement,
                    hashAlgorithm.Name,
                    secretPrepend,
                    secretAppend,
                    Interop.NCrypt.SecretAgreementFlags.None);
            }
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

            using (SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey))
            {
                Interop.NCrypt.SecretAgreementFlags flags = hmacKey == null ?
                    Interop.NCrypt.SecretAgreementFlags.UseSecretAsHmacKey :
                    Interop.NCrypt.SecretAgreementFlags.None;

                return Interop.NCrypt.DeriveKeyMaterialHmac(
                    secretAgreement,
                    hashAlgorithm.Name,
                    hmacKey,
                    secretPrepend,
                    secretAppend,
                    flags);
            }
        }

        public override byte[] DeriveKeyTls(ECDiffieHellmanPublicKey otherPartyPublicKey, byte[] prfLabel, byte[] prfSeed)
        {
            ArgumentNullException.ThrowIfNull(otherPartyPublicKey);
            ArgumentNullException.ThrowIfNull(prfLabel);
            ArgumentNullException.ThrowIfNull(prfSeed);

            using (SafeNCryptSecretHandle secretAgreement = DeriveSecretAgreementHandle(otherPartyPublicKey))
            {
                return Interop.NCrypt.DeriveKeyMaterialTls(
                    secretAgreement,
                    prfLabel,
                    prfSeed,
                    Interop.NCrypt.SecretAgreementFlags.None);
            }
        }
    }
}
